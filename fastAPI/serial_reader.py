"""
Shared module for reading serial data from IMU sensor.
"""
import serial
import json
import logging
from datetime import datetime, timezone

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

SERIAL_PORT = '/dev/cu.usbmodem2101'  # Update as needed
BAUD_RATE = 115200



def read_serial_line(port=SERIAL_PORT, baud_rate=BAUD_RATE):
    """
    Read a single line from the serial port.

    Args:
        port (str): Serial port to read from.
        baud_rate (int): Baud rate for serial communication.
    Returns:
        str: The raw line read from serial, or None if error.
    """
    try:
        with serial.Serial(port, baud_rate, timeout=1) as ser:
            line = ser.readline().decode('utf-8').strip()
            logger.info(f"Read line from serial: {line}")
            return line
    except serial.SerialException as e:
        logger.error(f"Serial error: {e}")
        return None

def parse_imu_data(raw_data):
    """
    Parse raw IMU data string into a structured dictionary.

    Args:
        raw_data (str): Raw data string from IMU.
    Returns:
        dict: Parsed IMU data with timestamp, or None if parsing fails.
    """
    if not raw_data:
        return None

    try:
        parts = raw_data.split(',')
        if len(parts) != 6:
            logger.error(f"Unexpected data format: {raw_data} (expected 6 values, got {len(parts)})")
            return None

        imu_data = {
            "timestamp": datetime.now(timezone.utc).isoformat(),
            "accel_x": float(parts[0]),
            "accel_y": float(parts[1]),
            "accel_z": float(parts[2]),
            "gyro_x": float(parts[3]),
            "gyro_y": float(parts[4]),
            "gyro_z": float(parts[5])
        }
        logger.info(f"Parsed IMU data: {imu_data}")
        return imu_data

    except (ValueError, IndexError) as e:
        logger.error(f"Error parsing IMU data: {e}")
        return None

def read_and_parse_imu(port=SERIAL_PORT, baud_rate=BAUD_RATE):
    """
    Read and parse IMU data from serial in one call.

    Returns:
        dict: Parsed IMU data with timestamp, or None if no valid data
    """

    raw_data = read_serial_line(port, baud_rate)
    return parse_imu_data(raw_data)
