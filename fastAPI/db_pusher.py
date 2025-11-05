"""
Module for pushing IMU data directly to PostgreSQL database.
"""
import os
import json
import logging
import psycopg2
from serial_reader import read_and_parse_imu

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Postgres settings
PG_HOST = os.getenv('PG_HOST', 'localhost')
PG_PORT = int(os.getenv('PG_PORT', '5432'))
PG_DB = os.getenv('PG_DB', 'postgres')
PG_USER = os.getenv('PG_USER', 'user')
PG_PASSWORD = os.getenv('PG_PASSWORD', 'password')


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


def read_serial_and_push_to_db():
    """
    Read from serial and push directly to PostgreSQL database.
    This combines serial reading with database pushing.

    Returns:
        dict: Result with status and data info
    """
    # Read and parse IMU data from serial
    imu_data = read_and_parse_imu()

    if not imu_data:
        return {"status": "error", "message": "No data available from serial"}

    # Push directly to PostgreSQL database
    success = push_imu_to_database(imu_data)

    if success:
        return {
            "status": "ok",
            "message": "Data saved to database",
            "data": imu_data
        }
    else:
        return {
            "status": "error",
            "message": "Failed to save to database",
            "data": imu_data
        }
