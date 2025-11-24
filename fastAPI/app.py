import redis
import asyncio
import json
import logging
from contextlib import asynccontextmanager
from fastapi import FastAPI
from services.serial import read_and_parse_imu
from endpoints import prod

REDIS_HOST = 'localhost'
REDIS_PORT = 6379
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
                await redis_client.lpush(QUEUE, json_data)
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
app.include_router(prod.router)
