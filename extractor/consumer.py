"""
Very small consumer that moves IMU JSON messages from Redis into Postgres.

Environment variables (simple):
  REDIS_HOST, REDIS_PORT, QUEUE (default imu_queue)
  PG_HOST, PG_PORT, PG_DB, PG_USER, PG_PASSWORD
  CONSUMER_MAX_ITEMS - optional integer: exit after processing this many items (0 = run forever)

This consumer expects messages like the producer produces:
  {"timestamp":"...","accel_x":..,"accel_y":..,"accel_z":..,"rot_x":..,"rot_y":..,"rot_z":..}

"""

import os
import time
import json
import logging

import redis
import psycopg2

logging.basicConfig(level=logging.INFO, format='%(asctime)s %(levelname)s %(message)s')

# Redis config
REDIS_HOST = os.getenv('REDIS_HOST', 'localhost')
REDIS_PORT = int(os.getenv('REDIS_PORT', '6379'))
QUEUE = os.getenv('QUEUE', 'imu_queue')

# Postgres config
PG_HOST = os.getenv('PG_HOST', 'localhost')
PG_PORT = int(os.getenv('PG_PORT', '5432'))
PG_DB = os.getenv('PG_DB', 'postgres')
PG_USER = os.getenv('PG_USER', 'postgres')
PG_PASSWORD = os.getenv('PG_PASSWORD', '')

# Optional stop after N items (0 = run forever)
CONSUMER_MAX_ITEMS = int(os.getenv('CONSUMER_MAX_ITEMS', '100'))

r = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, decode_responses=True)

# Postgres connection helper
def pg_connect():
    return psycopg2.connect(host=PG_HOST, port=PG_PORT, dbname=PG_DB, user=PG_USER, password=PG_PASSWORD)

# Create table if not exists
def ensure_table(conn):
    """Create a simple table matching the IMU_DATA schema if it doesn't exist."""
    with conn.cursor() as cur:
        cur.execute("""
        CREATE TABLE IF NOT EXISTS imu_data (
            id SERIAL PRIMARY KEY,
            timestamp TIMESTAMP NOT NULL,
            accel_x FLOAT NOT NULL,
            accel_y FLOAT NOT NULL,
            accel_z FLOAT NOT NULL,
            rot_x FLOAT NOT NULL,
            rot_y FLOAT NOT NULL,
            rot_z FLOAT NOT NULL,
            received_at TIMESTAMP WITH TIME ZONE DEFAULT now()
        );
        """)
        conn.commit()


def main():

    # Connect to Postgres (retry until available)
    conn = None
    while conn is None:
        try:
            conn = pg_connect()
        except Exception as e:
            logging.error("Postgres connect failed: %s", e)
            time.sleep(2)

    # Cursor for executing inserts
    cur = conn.cursor()
    logging.info("Consumer started, listening on Redis queue '%s'", QUEUE)

    processed = 0

    try:
        while True:
            item = r.brpop(QUEUE, timeout=5)
            if not item:
                # no item, loop again
                continue

            # brpop can return a (queue, value) pair or sometimes a single value depending on client.
            if isinstance(item, (list, tuple)) and len(item) >= 2:
                raw = item[1]
            else:
                raw = item

            # parse JSON
            try:
                payload = json.loads(raw)
            except Exception as e:
                logging.exception("Failed to parse JSON, skipping: %s", e)
                continue

            # extract required fields
            try:
                ts = payload['timestamp']
                ax = float(payload['accel_x'])
                ay = float(payload['accel_y'])
                az = float(payload['accel_z'])
                rx = float(payload['rot_x'])
                ry = float(payload['rot_y'])
                rz = float(payload['rot_z'])
            except Exception as e:
                logging.exception("Payload missing or invalid fields, skipping: %s", e)
                continue

            # Insert into Postgres
            try:
                cur.execute(
                    "INSERT INTO imu_data (timestamp, accel_x, accel_y, accel_z, rot_x, rot_y, rot_z) VALUES (%s,%s,%s,%s,%s,%s,%s)",
                    (ts, ax, ay, az, rx, ry, rz),
                )
                conn.commit()
                processed += 1
                logging.info("Inserted IMU row (total=%d)", processed)

                if CONSUMER_MAX_ITEMS > 0 and processed >= CONSUMER_MAX_ITEMS:
                    logging.info("Reached CONSUMER_MAX_ITEMS=%d, exiting", CONSUMER_MAX_ITEMS)
                    break

            except Exception as e:
                conn.rollback()
                logging.exception("Insert failed, re-queueing and sleeping: %s", e)
                # push back to the queue for retry
                try:
                    r.lpush(QUEUE, raw)
                except Exception:
                    logging.exception("Failed to requeue message")
                time.sleep(1)

    except KeyboardInterrupt:
        logging.info("Consumer stopped by user")
    finally:
        try:
            cur.close()
            conn.close()
        except Exception:
            pass


if __name__ == '__main__':
    main()
