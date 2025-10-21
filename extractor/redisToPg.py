"""
Simple tool to move IMU data between Redis and PostgreSQL.

How to use:
  1. Generate data:    python redisToPg.py generate -c 10
  2. Process data:     python redisToPg.py process
  3. Do both at once:  python redisToPg.py both -c 10

  To open redis CLI: redis-cli
  To list queues: LRANGE imu_queue 0 -1
  To list keys: KEYS *
  To delete a key: DEL imu_queue

Environment variables:
  REDIS_HOST, REDIS_PORT, QUEUE (default imu_queue)
  PG_HOST, PG_PORT, PG_DB, PG_USER, PG_PASSWORD
"""

import os
import random
import time
import json
import logging
import argparse
from datetime import datetime, timezone

import redis
import psycopg2


logging.basicConfig(level=logging.INFO, format='%(asctime)s %(levelname)s %(message)s')

# Redis settings
REDIS_HOST = os.getenv('REDIS_HOST', 'localhost')
REDIS_PORT = int(os.getenv('REDIS_PORT', '6379'))
QUEUE = os.getenv('QUEUE', 'imu_queue')

# Postgres settings
PG_HOST = os.getenv('PG_HOST', 'localhost')
PG_PORT = int(os.getenv('PG_PORT', '5432'))
PG_DB = os.getenv('PG_DB', 'postgres')
PG_USER = os.getenv('PG_USER', 'postgres')
PG_PASSWORD = os.getenv('PG_PASSWORD', '')

# Connect to Redis
redis_client = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, decode_responses=True)


# Create a single data item
def create_data_item():
    """Create one random data item."""
    # Get current time
    current_time = datetime.now(timezone.utc).isoformat()

    # Create random sensor data
    data = {
        "timestamp": current_time,
        "accel_x": round(random.uniform(-2.0, 2.0), 6),
        "accel_y": round(random.uniform(-2.0, 2.0), 6),
        "accel_z": round(random.uniform(-2.0, 2.0), 6),
        "rot_x": round(random.uniform(-180.0, 180.0), 6),
        "rot_y": round(random.uniform(-180.0, 180.0), 6),
        "rot_z": round(random.uniform(-180.0, 180.0), 6),
    }

    return data

# Create data and put it in Redis
def create_data(count=10):
    """Create data and put it in Redis."""
    print(f"Creating {count} data items...")

    # Generate the specified number of items
    for i in range(count):
        # Create a data item
        data = create_data_item()

        # Convert to JSON string
        json_data = json.dumps(data)

        # Add to Redis queue
        redis_client.lpush(QUEUE, json_data)

        print(f"Created item {i+1}/{count}")

    print(f"Finished creating {count} data items!")
    return count

# Get data from Redis and put it in PostgreSQL
def process_data(max_items=0):
    """Get data from Redis and save it to PostgreSQL."""
    print("Starting to process data from Redis to PostgreSQL...")

    # Connect to PostgreSQL
    try:
        conn = psycopg2.connect(
            host=PG_HOST,
            port=PG_PORT,
            dbname=PG_DB,
            user=PG_USER,
            password=PG_PASSWORD
        )
        cursor = conn.cursor()
    except Exception as e:
        print(f"Could not connect to PostgreSQL: {e}")
        return 0

    processed_count = 0

    try:
        # Process items until we reach max_items or run out of data
        while True:
            # Try to get an item from Redis (wait up to 5 seconds)
            result = redis_client.brpop(QUEUE, timeout=5)

            # If no data found, check if we should continue
            if not result:
                print("No more data in Redis queue.")
                break

            # Get the data from result
            if isinstance(result, (list, tuple)) and len(result) >= 2:
                json_data = result[1]
            else:
                json_data = result

            # Convert JSON to Python dictionary
            try:
                data = json.loads(json_data)
            except Exception as e:
                print(f"Error parsing JSON: {e}")
                continue

            # Extract the data fields
            try:
                timestamp = data['timestamp']
                accel_x = float(data['accel_x'])
                accel_y = float(data['accel_y'])
                accel_z = float(data['accel_z'])
                rot_x = float(data['rot_x'])
                rot_y = float(data['rot_y'])
                rot_z = float(data['rot_z'])
            except Exception as e:
                print(f"Missing or invalid data: {e}")
                continue

            # Insert data into PostgreSQL
            try:
                cursor.execute(
                    "INSERT INTO imu_data (timestamp, accel_x, accel_y, accel_z, rot_x, rot_y, rot_z) VALUES (%s,%s,%s,%s,%s,%s,%s)",
                    (timestamp, accel_x, accel_y, accel_z, rot_x, rot_y, rot_z)
                )
                conn.commit()
                processed_count += 1
                print(f"Saved item #{processed_count} to database")

                # Check if we've processed enough items
                if max_items > 0 and processed_count >= max_items:
                    print(f"Reached target of {max_items} items. Stopping.")
                    break

            except Exception as e:
                conn.rollback()
                print(f"Error saving to database: {e}")
                # Put the data back in Redis to try again later
                redis_client.lpush(QUEUE, json_data)
                time.sleep(1)

    except KeyboardInterrupt:
        print("Process stopped by user")

    finally:
        # Clean up
        cursor.close()
        conn.close()

    print(f"Processed {processed_count} items in total")
    return processed_count

# Generate data and process it in one go
def create_and_process(count=10):
    """Create data and then process it in one go."""
    print(f"Running complete pipeline: create {count} items then process them")

    # First create the data
    created = create_data(count)
    print(f"Created {created} items, now processing...")

    # Then process the data
    processed = process_data(count)
    print(f"Done! Created {created} items and processed {processed} items")

    return processed

# Main function to handle command line arguments
def main():
    """Process command line arguments and run the appropriate function."""
    parser = argparse.ArgumentParser(description='Redis to PostgreSQL data tool')

    # Define the actions
    parser.add_argument('action', choices=['generate', 'process', 'both'],
                        help='What to do: generate (create data), process (save to database), or both')

    # Options
    parser.add_argument('-c', '--count', type=int, default=10,
                        help='How many items to create (default: 10)')
    parser.add_argument('-m', '--max-items', type=int, default=0,
                        help='Maximum items to process (0 = all available)')

    # Parse arguments
    args = parser.parse_args()

    # Run the appropriate function
    if args.action == 'generate':
        create_data(args.count)
    elif args.action == 'process':
        process_data(args.max_items)
    elif args.action == 'both':
        create_and_process(args.count)

