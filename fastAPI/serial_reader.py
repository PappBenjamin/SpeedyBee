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

def read_and_parse_imu(port=SERIAL_PORT, baud_rate=BAUD_RATE):
    """
    Read and parse IMU data from serial in one call.

    Returns:
        dict: Parsed IMU data with timestamp, or None if no valid data
    """

    raw_data = read_serial_line(port, baud_rate)
    return parse_imu_data(raw_data)

