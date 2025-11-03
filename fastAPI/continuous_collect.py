"""
Continuous data collection script - Tests the /push-to-db endpoint.
Sends requests in a loop to collect IMU data from serial and save to database.

How to use:
  python continuous_collect.py              # Collect forever (Ctrl+C to stop)
  python continuous_collect.py -n 100       # Collect 100 samples
  python continuous_collect.py -i 0.5       # Collect with 0.5 second interval
  python continuous_collect.py -n 50 -i 1   # Collect 50 samples, 1 second apart
"""

import requests
import time
import argparse
from datetime import datetime


def collect_data(max_samples=None, interval=0.1, api_url="http://localhost:8000/push-to-db"):
    """
    Continuously collect data from the FastAPI endpoint.

    Args:
        max_samples: Maximum number of samples to collect (None = infinite)
        interval: Time between requests in seconds
        api_url: The API endpoint URL
    """
    print(f"   Starting continuous data collection...")
    print(f"   Endpoint: {api_url}")
    print(f"   Interval: {interval}s")
    print(f"   Max samples: {max_samples if max_samples else 'Unlimited'}")
    print(f"   Press Ctrl+C to stop\n")

    success_count = 0
    error_count = 0
    no_data_count = 0
    sample_count = 0

    try:
        while True:
            # Check if we've reached max samples
            if max_samples and sample_count >= max_samples:
                print(f"\n✅ Reached target of {max_samples} samples. Stopping.")
                break

            sample_count += 1
            timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]

            try:
                # Send POST request to the endpoint
                response = requests.post(api_url, timeout=5)

                if response.status_code == 200:
                    data = response.json()
                    success_count += 1

                    # Extract sensor values
                    imu_data = data.get('data', {})
                    accel_x = imu_data.get('accel_x', 'N/A')
                    accel_y = imu_data.get('accel_y', 'N/A')
                    accel_z = imu_data.get('accel_z', 'N/A')

                    print(f"[{timestamp}] ✅ #{sample_count} SUCCESS | "
                          f"Accel: ({accel_x}, {accel_y}, {accel_z}) | "
                          f"Total: {success_count} ok, {error_count} err, {no_data_count} no data")

                elif response.status_code == 404:
                    data = response.json()
                    if "No data available" in data.get('message', ''):
                        no_data_count += 1
                        print(f"[{timestamp}] ⚠️  #{sample_count} NO DATA | "
                              f"Serial port not sending | "
                              f"Total: {success_count} ok, {error_count} err, {no_data_count} no data")
                    else:
                        error_count += 1
                        print(f"[{timestamp}] ❌ #{sample_count} ERROR | {data}")

                else:
                    error_count += 1
                    print(f"[{timestamp}] ❌ #{sample_count} HTTP {response.status_code} | {response.text}")

            except requests.exceptions.ConnectionError:
                error_count += 1
                print(f"[{timestamp}] ❌ #{sample_count} CONNECTION ERROR | Is the FastAPI server running?")

            except requests.exceptions.Timeout:
                error_count += 1
                print(f"[{timestamp}] ❌ #{sample_count} TIMEOUT | Request took too long")

            except Exception as e:
                error_count += 1
                print(f"[{timestamp}] ❌ #{sample_count} ERROR | {e}")

            # Wait before next request
            time.sleep(interval)

    except KeyboardInterrupt:
        print(f"\n\n⚠️  Stopped by user (Ctrl+C)")

    # Print summary
    print(f"\n" + "="*60)
    print(f"📊 COLLECTION SUMMARY")
    print(f"="*60)
    print(f"Total requests:     {sample_count}")
    print(f"✅ Successful:      {success_count} ({success_count/sample_count*100:.1f}%)")
    print(f"⚠️  No data:         {no_data_count} ({no_data_count/sample_count*100:.1f}%)")
    print(f"❌ Errors:          {error_count} ({error_count/sample_count*100:.1f}%)")
    print(f"="*60)


def main():
    """Parse command line arguments and start collection."""
    parser = argparse.ArgumentParser(
        description='Continuously collect IMU data from FastAPI endpoint'
    )

    parser.add_argument(
        '-n', '--num-samples',
        type=int,
        default=None,
        help='Number of samples to collect (default: unlimited)'
    )

    parser.add_argument(
        '-i', '--interval',
        type=float,
        default=0.1,
        help='Interval between requests in seconds (default: 0.1)'
    )

    parser.add_argument(
        '-u', '--url',
        type=str,
        default='http://localhost:8000/push-to-db',
        help='API endpoint URL (default: http://localhost:8000/push-to-db)'
    )

    args = parser.parse_args()

    # Start collecting
    collect_data(
        max_samples=args.num_samples,
        interval=args.interval,
        api_url=args.url
    )


if __name__ == '__main__':
    main()

