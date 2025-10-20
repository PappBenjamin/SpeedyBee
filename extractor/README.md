# Redis -> PostgreSQL extractor

This small project provides three producers and one consumer:

- `producer_http.py` — HTTP endpoint that accepts JSON POSTs and pushes them to Redis.
- `producer_ble.py` — BLE notification listener (optional, uses `bleak`).
- `producer_hardcoded.py` — synthetic payload generator for testing.
- `consumer.py` — BRPOP from Redis `sensor_queue` and insert into Postgres `sensors` table.

Environment variables (defaults shown):

- REDIS_HOST=localhost
- REDIS_PORT=6379
- PG_HOST=localhost
- PG_PORT=5432
- PG_DB=postgres
- PG_USER=postgres
- PG_PASSWORD=
- HTTP_PORT=5000
- BLE_ADDRESS, BLE_CHAR_UUID (for BLE producer)

Quick run examples (macOS / zsh):

1) Install dependencies into a venv:

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

2) Start the consumer (stores into Postgres):

```bash
python consumer.py
```

3) Start an HTTP producer and send a sample payload:

```bash
python producer_http.py &
curl -X POST -H "Content-Type: application/json" \
  -d '{"sensor_id":"s1","value":42}' http://localhost:5000/sensor
```

4) Start the hardcoded producer:

```bash
python producer_hardcoded.py
```

Check stored rows in Postgres:

```sql
SELECT id, payload, received_at FROM sensors ORDER BY received_at DESC LIMIT 50;
```
from flask import Flask, request
import redis
import json
import os

r = redis.Redis(host=os.getenv('REDIS_HOST', 'localhost'), port=int(os.getenv('REDIS_PORT', '6379')), decode_responses=True)
app = Flask(__name__)

@app.route('/sensor', methods=['POST'])
def receive_sensor():
    """Receive JSON sensor payload over HTTP and push to Redis list `sensor_queue`.

    Expected JSON in POST body. Returns 200 and stores raw JSON.
    """
    data = request.get_json(force=True)
    # push as compact JSON string
    r.lpush('sensor_queue', json.dumps(data))
    return {'status': 'buffered'}, 200

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=int(os.getenv('HTTP_PORT', '5000')))

