"""
Dummy data generator for testing - replaces the extractor entirely.
Generates random IMU data and pushes it directly to PostgreSQL database.

How to use:
  python generate_dummy_data.py -c 10          # Generate 10 items
  python generate_dummy_data.py --count 100    # Generate 100 items
"""

import os
import random
import json
import argparse
from datetime import datetime, timezone
import redis
import psycopg2

# Redis settings
REDIS_HOST = os.getenv('REDIS_HOST', 'localhost')
REDIS_PORT = int(os.getenv('REDIS_PORT', '6379'))
REDIS_QUEUE = os.getenv('QUEUE', 'imu_queue')

# Postgres settings
PG_HOST = os.getenv('PG_HOST', 'localhost')
PG_PORT = int(os.getenv('PG_PORT', '5432'))
PG_DB = os.getenv('PG_DB', 'postgres')
PG_USER = os.getenv('PG_USER', 'user')
PG_PASSWORD = os.getenv('PG_PASSWORD', 'password')

# Connect to Redis
redis_client = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, decode_responses=True)


def create_dummy_imu_data():
    """Create one random IMU data item."""
    current_time = datetime.now(timezone.utc).isoformat()

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


def generate_dummy_data(count=10):
    """
    Generate dummy IMU data and push to Redis queue.

    Args:
        count: Number of items to generate

    Returns:
        int: Number of items created
    """
    print(f"Generating {count} dummy IMU data items...")

    for i in range(count):
        # Create a dummy data item
        data = create_dummy_imu_data()

        # Convert to JSON string
        json_data = json.dumps(data)

        # Add to Redis queue
        redis_client.lpush(REDIS_QUEUE, json_data)

        print(f"Created item {i+1}/{count}: {json_data}")

    print(f"✅ Finished generating {count} dummy data items!")
    return count


def generate_and_push_to_db(count=10):
    """
    Generate dummy IMU data and push directly to PostgreSQL database.

    Args:
        count: Number of items to generate

    Returns:
        int: Number of items successfully saved
    """
    print(f"Generating {count} dummy IMU data items and saving to database...")

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
        print("✅ Connected to PostgreSQL")
    except Exception as e:
        print(f"❌ Could not connect to PostgreSQL: {e}")
        return 0

    saved_count = 0

    try:
        for i in range(count):
            # Create a dummy data item
            data = create_dummy_imu_data()

            # Extract the data fields
            timestamp = data['timestamp']
            accel_x = data['accel_x']
            accel_y = data['accel_y']
            accel_z = data['accel_z']
            rot_x = data['rot_x']
            rot_y = data['rot_y']
            rot_z = data['rot_z']

            # Insert directly into PostgreSQL
            try:
                cursor.execute(
                    "INSERT INTO imu_data (timestamp, accel_x, accel_y, accel_z, rot_x, rot_y, rot_z) VALUES (%s,%s,%s,%s,%s,%s,%s)",
                    (timestamp, accel_x, accel_y, accel_z, rot_x, rot_y, rot_z)
                )
                conn.commit()
                saved_count += 1
                print(f"Created and saved item {saved_count}/{count}")
            except Exception as e:
                conn.rollback()
                print(f"❌ Error saving item {i+1}: {e}")

    except KeyboardInterrupt:
        print("\n⚠️  Process stopped by user")
    finally:
        cursor.close()
        conn.close()
        print(f"✅ Database connection closed")

    print(f"✅ Finished! Generated and saved {saved_count}/{count} dummy data items to database!")
    return saved_count


def main():
    """Process command line arguments and generate data."""
    parser = argparse.ArgumentParser(
        description='Generate dummy IMU data and save to database'
    )

    parser.add_argument(
        '-c', '--count',
        type=int,
        default=10,
        help='Number of items to generate (default: 10)'
    )

    parser.add_argument(
        '--queue-only',
        action='store_true',
        help='Only push to Redis queue (old behavior, requires extractor)'
    )

    args = parser.parse_args()

    # Generate the data
    if args.queue_only:
        print("📝 Queue-only mode: pushing to Redis queue...")
        generated = generate_dummy_data(args.count)
        print(f"\n💡 To process this data, run:")
        print(f"   cd extractor && python redisToPg.py process")
    else:
        print("🚀 Direct mode: generating and saving to database...")
        saved = generate_and_push_to_db(args.count)
        print(f"\n🎉 All done! No need to run the extractor!")


if __name__ == '__main__':
    main()
