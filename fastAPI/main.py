import redis
from fastapi import FastAPI
from fastapi.responses import JSONResponse
from serial_reader import read_serial_line, read_and_parse_imu
from db_pusher import read_serial_and_push_to_db

REDIS_HOST = 'localhost'
REDIS_PORT = 6379
REDIS_KEY = 'imu_data'

app = FastAPI()
r = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, db=0)

@app.post("/push")
def push_to_redis():
    """Push raw serial data to Redis key (legacy endpoint)"""
    data = read_serial_line()
    if data:
        r.set(REDIS_KEY, data)
        return {"status": "ok", "data": data}
    else:
        return JSONResponse(content={"status": "no data"}, status_code=404)

@app.post("/push-to-db")
def push_to_database():
    """Read serial data and push to database queue for processing"""
    result = read_serial_and_push_to_db()

    if result["status"] == "ok":
        return result
    else:
        return JSONResponse(content=result, status_code=404)

@app.post("/push-parsed")
def push_parsed_to_redis():
    """Read and parse serial data, then push to Redis key"""
    imu_data = read_and_parse_imu()
    if imu_data:
        r.set(REDIS_KEY, str(imu_data))
        return {"status": "ok", "data": imu_data}
    else:
        return JSONResponse(content={"status": "no data"}, status_code=404)


@app.post("/push-dummy")
def push_dummy_to_redis():
    """Push dummy IMU data to Redis key"""
    from generate_dummy_data import create_dummy_imu_data

    imu_data = create_dummy_imu_data()
    r.set(REDIS_KEY, str(imu_data))
    return {"status": "ok", "data": imu_data}


@app.get("/get")
def get_from_redis():
    """Get data from Redis key"""
    data = r.get(REDIS_KEY)
    if data:
        return {"data": data.decode('utf-8')}
    else:
        return JSONResponse(content={"status": "no data"}, status_code=404)

