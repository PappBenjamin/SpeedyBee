import redis
import asyncio
import json
import logging
from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.responses import JSONResponse
from serial_reader import read_and_parse_imu
from redisAndPg import read_postgres, read_serial_to_redis, read_redis_to_postgres

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
