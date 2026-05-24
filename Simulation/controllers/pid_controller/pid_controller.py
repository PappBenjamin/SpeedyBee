from controller import Robot
import csv
import math

# 1. Initialize Robot & Time Step
robot = Robot()
timestep = int(robot.getBasicTimeStep())

# 2. Initialize 16 Line Sensors (Assumed names: ir0, ir1, ..., ir15)
NUM_SENSORS = 16
gs_sensors = []
for i in range(NUM_SENSORS):
    sensor_name = f'ir{i}'  # Change 'ir' to match your Webots device names
    sensor = robot.getDevice(sensor_name)
    sensor.enable(timestep)
    gs_sensors.append(sensor)

# Symmetrical weights for 16 sensors: [-7.5, -6.5, ..., 6.5, 7.5]
weights = [i - (NUM_SENSORS - 1) / 2.0 for i in range(NUM_SENSORS)]

# 3. Initialize Motors
left_motor = robot.getDevice('left wheel motor')
right_motor = robot.getDevice('right wheel motor')
left_motor.setPosition(float('inf'))
right_motor.setPosition(float('inf'))

# 4. Motor Limits & PID Constants (Calculated from 5000 RPM)
MAX_SPEED = 523.6   # 5000 RPM in rad/s
BASE_SPEED = 4.0   # Safe starting speed (adjust up as your PID gets better)
WHITE_VALUE = 1000.0 # Typical Webots max reflection value (adjust if needed)

Kp = 2.0            # Lower proportional gain to prevent wild oscillation
Ki = 0.0
Kd = 0.5
last_error = 0
integral = 0

# Open CSV for 16-sensor ANFIS dataset logging
csv_file = open('anfis_16_sensor_data.csv', mode='w', newline='')
csv_writer = csv.writer(csv_file)
# Dynamic header generation: sensor0, sensor1, ..., sensor15, steering_output
headers = [f'sensor{i}' for i in range(NUM_SENSORS)] + ['steering_output']
csv_writer.writerow(headers)

# --- Main Simulation Loop ---
while robot.step(timestep) != -1:
    
    # Read sensor values and calculate weighted averages
    sensor_values = []
    weighted_sum = 0.0
    total_intensity = 0.0
    
    for i in range(NUM_SENSORS):
        val = gs_sensors[i].getValue()
        sensor_values.append(val)
        
        # Invert reading so the black line has the highest intensity value
        intensity = max(0.0, WHITE_VALUE - val)
        
        weighted_sum += intensity * weights[i]
        total_intensity += intensity

    # Calculate cross-track error using the centroid formula
    if total_intensity > 0:
        error = weighted_sum / total_intensity
    else:
        error = 0.0 # Lost the line completely
        
    # PID Mathematics
    derivative = error - last_error
    integral += error
    steering_output = (Kp * error) + (Ki * integral) + (Kd * derivative)
    last_error = error
    
    # Log data for ANFIS training (all 16 inputs + 1 steering output target)
    csv_writer.writerow(sensor_values + [steering_output])
    
    # Apply differential steering configurations
    left_speed = BASE_SPEED + steering_output
    right_speed = BASE_SPEED - steering_output
    
    # Constrain to your physical 5000 RPM limits
    left_speed = max(min(left_speed, MAX_SPEED), -MAX_SPEED)
    right_speed = max(min(right_speed, MAX_SPEED), -MAX_SPEED)
    
    # Swapped motor assignments based on your physical hardware test preference
    left_motor.setVelocity(right_speed)
    right_motor.setVelocity(left_speed)

# Clean up file on exit
csv_file.close()