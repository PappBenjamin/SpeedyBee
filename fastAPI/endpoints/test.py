from fastapi import APIRouter
from fastapi.responses import JSONResponse
from services.db import read_serial_to_redis, read_redis_to_postgres
from services.serial import read_and_parse_imu

import redis
import logging

logger = logging.getLogger(__name__)

REDIS_HOST = 'localhost'
REDIS_PORT = 6379
REDIS_KEY = 'imu_data'

r = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, db=0)

router = APIRouter()


@router.post("/serial-to-redis")
def serial_to_redis():
    """Read from serial and push to Redis queue"""
    result = read_serial_to_redis()

    if result["status"] == "ok":
        return result
    else:
        return JSONResponse(content=result, status_code=500)


@router.post("/redis-to-postgres")
def redis_to_postgres():
    """Read from Redis queue and push to PostgreSQL"""
    result = read_redis_to_postgres()

    if result["status"] == "ok":
        return result
    elif result["status"] == "no_data":
        return JSONResponse(content=result, status_code=404)
    else:
        return JSONResponse(content=result, status_code=500)


@router.get("/get-from-serial-to-redis")
def get_from_serial_to_redis():
    """Get data from Redis key"""
    data = r.get(REDIS_KEY)
    if data:
        return {"data": data.decode('utf-8')}
    else:
        return JSONResponse(content={"status": "no data"}, status_code=404)


@router.get("/get-from-serial")
def get_from_serial():
    """Read and parse IMU data from serial"""
    imu_data = read_and_parse_imu()
    if imu_data:
        return {"data": imu_data}
    else:
        return JSONResponse(content={"status": "no data"}, status_code=404)
