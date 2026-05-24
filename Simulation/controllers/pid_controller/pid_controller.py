from controller import Robot
import csv

# 1. Initialize Robot & Time Step
robot = Robot()
timestep = int(robot.getBasicTimeStep())

# 2. Initialize Ground Sensors (gs0=Right, gs1=Center, gs2=Left)
gs_names = ['gs0', 'gs1', 'gs2']
gs_sensors = []
for name in gs_names:
    sensor = robot.getDevice(name)
    sensor.enable(timestep)
    gs_sensors.append(sensor)

# 3. Initialize Differential Drive Wheels
left_motor = robot.getDevice('left wheel motor')
right_motor = robot.getDevice('right wheel motor')
left_motor.setPosition(float('inf'))
right_motor.setPosition(float('inf'))

# 4. PID Controller Variables
MAX_SPEED = 6.28  # Rad/sec max speed for e-puck
BASE_SPEED = 4.0
Kp, Ki, Kd = 0.05, 0.0, 0.01  # You will tune these!
last_error = 0
integral = 0

# Open a CSV file to collect training data for your ANFIS network later
csv_file = open('anfis_training_data.csv', mode='w', newline='')
csv_writer = csv.writer(csv_file)
csv_writer.writerow(['sensor_left', 'sensor_center', 'sensor_right', 'steering_output'])

# --- Main Simulation Loop ---
while robot.step(timestep) != -1:
    # Read IR Sensor Values (Higher values mean lighter surface, Lower means dark line)
    right_val = gs_sensors[0].getValue()
    center_val = gs_sensors[1].getValue()
    left_val = gs_sensors[2].getValue()
    
    # Calculate cross-track error using the difference between left and right sensors
    error = left_val - right_val
    
    # PID Math
    derivative = error - last_error
    integral += error
    steering_output = (Kp * error) + (Ki * integral) + (Kd * derivative)
    last_error = error
    
    # Log data ONLY if the robot is actually tracking the line nicely
    # This data will teach ANFIS how to mimic your PID controller
    csv_writer.writerow([left_val, center_val, right_val, steering_output])
    
    # Apply differential steering adjustment
    left_speed = BASE_SPEED + steering_output
    right_speed = BASE_SPEED - steering_output
    
    # Constrain speeds to physical limits
    left_speed = max(min(left_speed, MAX_SPEED), -MAX_SPEED)
    right_speed = max(min(right_speed, MAX_SPEED), -MAX_SPEED)
    
    left_motor.setVelocity(right_speed)
    right_motor.setVelocity(left_speed)

# Clean up file when simulation ends
csv_file.close()