import redis
import asyncio
import json
import logging
from contextlib import asynccontextmanager
from fastapi import FastAPI, Body
from fastapi.responses import JSONResponse
from serial_reader import read_and_parse_imu
from redisAndPg import read_postgres, read_serial_to_redis, read_redis_to_postgres, upload_run_to_postgres, query_run_by_name, push_run_to_redis

REDIS_HOST = 'localhost'
REDIS_PORT = 6379
REDIS_KEY = 'imu_data'
QUEUE = 'imu_queue'

logger = logging.getLogger(__name__)

# Background serial reader
async def serial_push_loop():
    redis_client = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, decode_responses=True, db=0)
    while True:
        try:
            data = read_and_parse_imu()
            if data:
                json_data = json.dumps(data)
                redis_client.lpush(QUEUE, json_data)
                logger.info(f"Pushed IMU data to Redis queue")
        except Exception as e:
            logger.error(f"Error in serial push loop: {e}")
        await asyncio.sleep(0.05)  # 50ms interval

# read from postgres for replay
async def postgres_replay_loop(run_name: str = None):
    """
    Replay recorded runs from PostgreSQL.
    If run_name is provided, replays that specific run.
    Otherwise, replays the most recent data.
    """
    redis_client = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, decode_responses=True, db=0)
    while True:
        try:
            if run_name:
                result = query_run_by_name(run_name)
            else:
                result = read_postgres(limit=1)

            if result["status"] == "ok" and result["data"]:
                if run_name:
                    # If querying by run name, get frames from the run data
                    run_data = result["data"]
                    frames = run_data.get("frames", [])
                    logger.info(f"Replaying run: {run_data.get('name', 'unknown')} with {len(frames)} frames")
                else:
                    # If getting recent data, just use it as is
                    data = result["data"][0] if isinstance(result["data"], list) else result["data"]
                    frames = [data]

                # Replay each frame
                for frame in frames:
                    json_frame = json.dumps(frame)
                    redis_client.lpush(QUEUE, json_frame)
                    await asyncio.sleep(0.05)  # 50ms between frames
        except Exception as e:
            logger.error(f"Error in postgres replay loop: {e}")
        await asyncio.sleep(0.1)  # 100ms interval between run cycles

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup: Start background serial reader
    logger.info("Starting background serial reader")
    task = asyncio.create_task(serial_push_loop())
    yield
    # Shutdown: Cancel background task
    logger.info("Stopping background serial reader")
    task.cancel()
    try:
        await task
    except asyncio.CancelledError:
        pass

app = FastAPI(lifespan=lifespan)
r = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, db=0)


@app.post("/serial-to-redis")
def serial_to_redis():
    """Read from serial and push to Redis queue"""
    result = read_serial_to_redis()

    if result["status"] == "ok":
        return result
    else:
        return JSONResponse(content=result, status_code=500)


@app.post("/redis-to-postgres")
def redis_to_postgres():
    """Read from Redis queue and push to PostgreSQL"""
    result = read_redis_to_postgres()

    if result["status"] == "ok":
        return result
    elif result["status"] == "no_data":
        return JSONResponse(content=result, status_code=404)
    else:
        return JSONResponse(content=result, status_code=500)


@app.get("/get-from-postgres")
def get_from_postgres(limit: int = 100):
    """Read IMU data from PostgreSQL database and return via HTTP"""
    result = read_postgres(limit=limit)

    if result["status"] == "ok":
        return result
    else:
        return JSONResponse(content=result, status_code=500)


@app.get("/get-from-serial-to-redis")
def get_from_serial_to_redis():
    """Get data from Redis key"""
    data = r.get(REDIS_KEY)
    if data:
        return {"data": data.decode('utf-8')}
    else:
        return JSONResponse(content={"status": "no data"}, status_code=404)


@app.get("/get-from-serial")
def get_from_serial():
    """Read and parse IMU data from serial"""
    imu_data = read_and_parse_imu()
    if imu_data:
        return {"data": imu_data}
    else:
        return JSONResponse(content={"status": "no data"}, status_code=404)


@app.post("/upload-run")
def upload_run(run_data: dict = Body(...)):
    """
    Upload a recorded run from WPF app to PostgreSQL.

    Expected request body:
    {
        "name": "run_name",
        "timestamp": "2024-01-15T10:30:00Z",
        "frames": [
            {"timestamp": "...", "accel_x": ..., "accel_y": ..., "accel_z": ..., "rot_x": ..., "rot_y": ..., "rot_z": ...},
            ...
        ]
    }
    """
    result = upload_run_to_postgres(run_data)

    if result["status"] == "ok":
        return result
    else:
        return JSONResponse(content=result, status_code=400)


@app.get("/query-run")
def query_run(run_name: str):
    """
    Query PostgreSQL for a specific run by name.
    Returns the run with all its frames and metadata.

    Usage: GET /query-run?run_name=my_run
    """
    result = query_run_by_name(run_name)

    if result["status"] == "ok":
        return result
    elif result["status"] == "not_found":
        return JSONResponse(content=result, status_code=404)
    else:
        return JSONResponse(content=result, status_code=500)


@app.post("/replay-run")
def replay_run(run_name: str = Body(..., embed=True)):
    """
    Push a specific run's frames to Redis queue for replay.

    Request body: { "run_name": "my_run" }
    """
    result = push_run_to_redis(run_name)

    if result["status"] == "ok":
        return result
    else:
        return JSONResponse(content=result, status_code=400)
