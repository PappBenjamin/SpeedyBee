"""Very small IMU hardcoded producer.
Sends JSON messages to a Redis list.
Environment variables:
  REDIS_HOST (default localhost)
  REDIS_PORT (default 6379)
  QUEUE (default imu_queue)
  INTERVAL (seconds between messages, default 1.0)
  COUNT (optional, 0 = run forever)
"""
import os
import time
import json
import random
import redis
from datetime import datetime, timezone

REDIS_HOST = os.getenv('REDIS_HOST', 'localhost')
REDIS_PORT = int(os.getenv('REDIS_PORT', '6379'))
QUEUE = os.getenv('QUEUE', 'imu_queue')
INTERVAL = float(os.getenv('INTERVAL', '1.0'))
COUNT = int(os.getenv('COUNT', '0'))  # 0 means run forever

r = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, decode_responses=True)


def generate():
    i = 0
    try:
        while True:
            ts = datetime.now(timezone.utc).isoformat()
            payload = {
                "timestamp": ts,
                "accel_x": round(random.uniform(-2.0, 2.0), 6),
                "accel_y": round(random.uniform(-2.0, 2.0), 6),
                "accel_z": round(random.uniform(-2.0, 2.0), 6),
                "rot_x": round(random.uniform(-180.0, 180.0), 6),
                "rot_y": round(random.uniform(-180.0, 180.0), 6),
                "rot_z": round(random.uniform(-180.0, 180.0), 6),
            }
            r.lpush(QUEUE, json.dumps(payload))
            i += 1
            if COUNT > 0 and i >= COUNT:
                print(f"Sent {i} messages to '{QUEUE}' and exiting.")
                break
            time.sleep(INTERVAL)
    except KeyboardInterrupt:
        print("Producer stopped by user")


if __name__ == '__main__':
    generate()
