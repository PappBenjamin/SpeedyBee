"""
Module for pushing IMU data directly to PostgreSQL database.
"""

import os
import json
import logging
import psycopg2
import redis
from serial_reader import read_and_parse_imu

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Postgres settings
PG_HOST = os.getenv('PG_HOST', 'localhost')
PG_PORT = int(os.getenv('PG_PORT', '5432'))
PG_DB = os.getenv('PG_DB', 'postgres')
PG_USER = os.getenv('PG_USER', 'user')
PG_PASSWORD = os.getenv('PG_PASSWORD', 'password')

# Redis settings
REDIS_HOST = os.getenv('REDIS_HOST', 'localhost')
REDIS_PORT = int(os.getenv('REDIS_PORT', '6379'))
QUEUE = os.getenv('QUEUE', 'imu_queue')

# Connect to Redis
redis_client = redis.Redis(host=REDIS_HOST, port=REDIS_PORT, decode_responses=True)

# Push IMU data to PostgreSQL database
def push_imu_to_database(imu_data):

    """
    Push parsed IMU data directly to PostgreSQL database.

    Args:
        imu_data: Dictionary containing IMU data with timestamp

    Returns:
        bool: True if successful, False otherwise
    """

    if not imu_data:
        logger.warning("No IMU data to push")
        return False

    try:
        # Connect to PostgreSQL
        conn = psycopg2.connect(
            host=PG_HOST,
            port=PG_PORT,
            dbname=PG_DB,
            user=PG_USER,
            password=PG_PASSWORD
        )
        cursor = conn.cursor()

        # Extract the data fields
        timestamp = imu_data['timestamp']
        accel_x = float(imu_data['accel_x'])
        accel_y = float(imu_data['accel_y'])
        accel_z = float(imu_data['accel_z'])
        rot_x = float(imu_data['rot_x'])
        rot_y = float(imu_data['rot_y'])
        rot_z = float(imu_data['rot_z'])

        # Insert data into PostgreSQL



        cursor.execute(
            "INSERT INTO imu_data (timestamp, accel_x, accel_y, accel_z, rot_x, rot_y, rot_z) VALUES (%s,%s,%s,%s,%s,%s,%s)",
            (timestamp, accel_x, accel_y, accel_z, rot_x, rot_y, rot_z)
        )
        conn.commit()

        # Clean up
        cursor.close()
        conn.close()

        logger.info(f"Successfully saved IMU data to database: {json.dumps(imu_data)}")
        return True

    except Exception as e:
        logger.error(f"Error saving to database: {e}")
        return False

# Read IMU data from PostgreSQL database
def read_postgres(limit=100):
    """
    Read IMU data from PostgreSQL database.

    Args:
        limit (int): Number of records to fetch
    Returns:
        dict: Result with status, message, and data
    """

    try:
        # Connect to PostgreSQL
        conn = psycopg2.connect(
            host=PG_HOST,
            port=PG_PORT,
            dbname=PG_DB,
            user=PG_USER,
            password=PG_PASSWORD
        )
        cursor = conn.cursor()

        # Fetch data
        cursor.execute(
            "SELECT timestamp, accel_x, accel_y, accel_z, rot_x, rot_y, rot_z FROM imu_data ORDER BY timestamp DESC LIMIT %s",
            (limit,)
        )
        rows = cursor.fetchall()

        # Convert to list of dicts
        data = []
        for row in rows:
            data.append({
                "timestamp": row[0],
                "accel_x": row[1],
                "accel_y": row[2],
                "accel_z": row[3],
                "rot_x": row[4],
                "rot_y": row[5],
                "rot_z": row[6]
            })

        # Clean up
        cursor.close()
        conn.close()

        return {
            "status": "ok",
            "message": f"Fetched {len(data)} records from database",
            "data": data
        }

    except Exception as e:
        logger.error(f"Error reading from database: {e}")
        return {
            "status": "error",
            "message": str(e),
            "data": []
        }

# Read from serial and push to Redis
def read_serial_to_redis():
    """
    Read IMU data from serial and push to Redis queue.

    Returns:
        dict: Result with status, message, and data info
    """

    # Read and parse IMU data from serial
    imu_data = read_and_parse_imu()

    if not imu_data:
        return {"status": "error", "message": "No data available from serial"}

    try:
        # Convert to JSON and push to Redis queue
        json_data = json.dumps(imu_data)
        redis_client.lpush(QUEUE, json_data)


        logger.info(f"Pushed to Redis queue: {json_data}")

        return {
            "status": "ok",
            "message": "Data pushed to Redis queue",
            "data": imu_data
        }

    except Exception as e:
        logger.error(f"Error in read_serial_to_redis: {e}")
        return {
            "status": "error",
            "message": str(e),
            "data": imu_data
        }

# Read from Redis and push to PostgreSQL
def read_redis_to_postgres():
    """
    Read IMU data from Redis queue and push to PostgreSQL.

    Returns:
        dict: Result with status, message, and count of processed items
    """

    try:
        # Try to get an item from Redis (non-blocking)
        result = redis_client.rpop(QUEUE)

        if not result:
            return {
                "status": "no_data",
                "message": "No data in Redis queue",
                "processed": 0
            }

        # Parse the JSON data
        try:
            imu_data = json.loads(result)
        except json.JSONDecodeError as e:
            logger.error(f"Error parsing JSON from Redis: {e}")
            return {
                "status": "error",
                "message": f"Failed to parse data: {e}",
                "processed": 0
            }

        # Push to PostgreSQL
        success = push_imu_to_database(imu_data)

        if success:
            return {
                "status": "ok",
                "message": "Data moved from Redis to PostgreSQL",
                "data": imu_data,
                "processed": 1
            }
        else:
            # Put data back in Redis if database operation failed
            redis_client.lpush(QUEUE, result)
            return {
                "status": "error",
                "message": "Failed to save to database, data returned to Redis",
                "data": imu_data,
                "processed": 0
            }

    except Exception as e:
        logger.error(f"Error in read_redis_to_postgres: {e}")
        return {
            "status": "error",
            "message": str(e),
            "processed": 0
        }





