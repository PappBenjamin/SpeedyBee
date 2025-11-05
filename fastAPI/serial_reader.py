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


def read_serial_line(port=SERIAL_PORT, baud_rate=BAUD_RATE, timeout=1):
    """
    Read a single line from the serial port.

    Args:
        port: Serial port path
        baud_rate: Baud rate for serial communication
        timeout: Read timeout in seconds

    Returns:
        str: The line read from serial, or None if no data
    """
    try:
        with serial.Serial(port, baud_rate, timeout=timeout) as ser:
            line = ser.readline().decode('utf-8').strip()
            return line if line else None
    except Exception as e:
        logger.error(f"Error reading from serial: {e}")
        return None


def parse_imu_data(raw_data):
    """
    Parse raw IMU data string into structured format.
    Expects comma-separated values: accel_x,accel_y,accel_z,rot_x,rot_y,rot_z

    Args:
        raw_data: Raw string from serial

    Returns:
        dict: Parsed IMU data with timestamp, or None if parsing fails
    """
    if not raw_data:
        return None

    try:
        # Try to parse as JSON first
        data = json.loads(raw_data)
        # Add timestamp if not present
        if 'timestamp' not in data:
            data['timestamp'] = datetime.now(timezone.utc).isoformat()
        return data
    except json.JSONDecodeError:
        # If not JSON, try parsing as CSV
        try:
            values = raw_data.split(',')
            if len(values) >= 6:
                data = {
                    "timestamp": datetime.now(timezone.utc).isoformat(),
                    "accel_x": float(values[0]),
                    "accel_y": float(values[1]),
                    "accel_z": float(values[2]),
                    "rot_x": float(values[3]),
                    "rot_y": float(values[4]),
                    "rot_z": float(values[5]),
                }
                return data
        except (ValueError, IndexError) as e:
            logger.error(f"Error parsing CSV data: {e}")
            return None

    return None


def read_and_parse_imu(port=SERIAL_PORT, baud_rate=BAUD_RATE):
    """
    Read and parse IMU data from serial in one call.

    Returns:
        dict: Parsed IMU data with timestamp, or None if no valid data
    """
    raw_data = read_serial_line(port, baud_rate)
    return parse_imu_data(raw_data)

