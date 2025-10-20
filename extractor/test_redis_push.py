"""Quick test: push a sample JSON into Redis `sensor_queue` and read it back.

Usage: python test_redis_push.py
"""
import os
import json
from datetime import datetime

import redis

REDIS_HOST = os.getenv('REDIS_HOST', 'localhost')
REDIS_PORT = int(os.getenv('REDIS_PORT', '6379'))

r = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, decode_responses=True)

payload = {"test": "ping", "ts": datetime.utcnow().isoformat() + "Z"}
raw = json.dumps(payload)

print(f"Connecting to Redis {REDIS_HOST}:{REDIS_PORT}...")
try:
    r.ping()
except Exception as e:
    print("Redis ping failed:", e)
    raise

r.lpush('sensor_queue', raw)
print("Pushed payload to sensor_queue:")
print(raw)

# Read back the most recent element
items = r.lrange('sensor_queue', 0, 0)
print('LRANGE 0 0 ->', items)

