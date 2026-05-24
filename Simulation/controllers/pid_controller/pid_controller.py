from controller import Supervisor
import csv

# 1. Initialize Robot as a SUPERVISOR
robot = Supervisor()
timestep = int(robot.getBasicTimeStep())

# ==========================================
# ⚙️ OPERATION MODE
# ==========================================
# Set to True to find perfect PID values. Set to False to log ANFIS data.
AUTO_TUNE = True  

# Teleport Coordinates (Replace these with the numbers from your Webots Scene Tree)
START_TRANS = [-0.33, 0.18, -6.39633e-05]  # X, Y, Z
START_ROT = [-0.001052140210489292, 0.0007020571404522981, -0.9999992000580545, -5.307179586466759e-06] # X, Y, Z, Angle

# Grab the robot node for God-mode teleporting
robot_node = robot.getFromDef("SpeedyBee")  # Make sure this matches the DEF name in your Webots world file
if robot_node is None:
    print("WARNING: Could not find DEF 'MY_ROBOT'. Teleporting will not work!")
# ==========================================

# 2. Initialize 16 Line Sensors
NUM_SENSORS = 16
gs_sensors = []
for i in range(NUM_SENSORS):
    sensor_name = f'ir{i}'
    sensor = robot.getDevice(sensor_name)
    sensor.enable(timestep)
    gs_sensors.append(sensor)

weights = [i - (NUM_SENSORS - 1) / 2.0 for i in range(NUM_SENSORS)]

# 3. Initialize Motors
left_motor = robot.getDevice('left wheel motor')
right_motor = robot.getDevice('right wheel motor')
left_motor.setPosition(float('inf'))
right_motor.setPosition(float('inf'))
left_motor.setVelocity(0.0)
right_motor.setVelocity(0.0)

# 4. Motor Limits & Constants
MAX_SPEED = 6.28
BASE_SPEED = 4.0   
WHITE_VALUE = 1000.0 

# Base PID Constants (Will be overwritten if AUTO_TUNE is True)
Kp = 2.5            
Ki = 0.0
Kd = 0.5

def reset_robot():
    """Teleports the robot back to the starting line."""
    if robot_node:
        robot_node.getField("translation").setSFVec3f(START_TRANS)
        robot_node.getField("rotation").setSFRotation(START_ROT)
        robot_node.resetPhysics()

def run_lap(current_Kp, current_Ki, current_Kd, log_data=False):
    """Runs a single lap. Returns the total error accumulated (lower is better)."""
    reset_robot()
    print(f"Running lap with Kp={current_Kp:.3f}, Ki={current_Ki:.3f}, Kd={current_Kd:.3f}")
    last_error = 0.0
    integral = 0.0
    total_lap_error = 0.0
    step_count = 0
    max_steps = 2500 # Adjust this to match how many steps it takes to finish 1 lap
    
    # Open CSV only if we are in data collection mode
    if log_data:
        csv_file = open('anfis_16_sensor_data.csv', mode='w', newline='')
        csv_writer = csv.writer(csv_file)
        # Added 'error' and 'derivative' to the logged headers
        headers = [f'sensor{i}' for i in range(NUM_SENSORS)] + ['error', 'derivative', 'steering_output']
        csv_writer.writerow(headers)

    while robot.step(timestep) != -1 and step_count < max_steps:
        sensor_values = []
        weighted_sum = 0.0
        total_intensity = 0.0
        
        for i in range(NUM_SENSORS):
            val = gs_sensors[i].getValue()
            sensor_values.append(val)
            intensity = max(0.0, WHITE_VALUE - val)
            weighted_sum += intensity * weights[i]
            total_intensity += intensity

        # Calculate cross-track error
        if total_intensity > 0:
            error = weighted_sum / total_intensity
        else:
            error = 0.0
            
        # Accumulate error for the Twiddle algorithm to judge this lap's performance
        total_lap_error += abs(error)
            
        # PID Mathematics
        derivative = error - last_error
        integral += error
        steering_output = (current_Kp * error) + (current_Ki * integral) + (current_Kd * derivative)
        last_error = error
        
        # Log data for ANFIS if enabled
        if log_data:
            csv_writer.writerow(sensor_values + [error, derivative, steering_output])
        
        # Apply differential steering (Keeping your confirmed swapped motors)
        left_speed = BASE_SPEED + steering_output
        right_speed = BASE_SPEED - steering_output
        
        left_speed = max(min(left_speed, MAX_SPEED), -MAX_SPEED)
        right_speed = max(min(right_speed, MAX_SPEED), -MAX_SPEED)
        
        left_motor.setVelocity(right_speed)
        right_motor.setVelocity(left_speed)
        
        step_count += 1
        
    if log_data:
        csv_file.close()
        
    # Stop motors at the end of the lap
    left_motor.setVelocity(0.0)
    right_motor.setVelocity(0.0)
    
    return total_lap_error

# ==========================================
# MAIN EXECUTION
# ==========================================
if not AUTO_TUNE:
    print(f"Running Data Collection Lap... (Kp={Kp}, Ki={Ki}, Kd={Kd})")
    run_lap(Kp, Ki, Kd, log_data=True)
    print("Data collection complete! Check anfis_16_sensor_data.csv")
    
else:
    print("Starting Twiddle Auto-Tuning...")
    p = [Kp, Ki, Kd]
    dp = [0.5, 0.0, 0.1] # Initial nudge amounts
    
    best_error = run_lap(p[0], p[1], p[2], log_data=False)
    print(f"Baseline Error: {best_error:.2f}")

    while sum(dp) > 0.01: # Stop when nudges get very small
        for i in range(len(p)):
            p[i] += dp[i]
            err = run_lap(p[0], p[1], p[2], log_data=False)
            
            if err < best_error:
                best_error = err
                dp[i] *= 1.1 # Nudge bigger next time
            else:
                p[i] -= 2 * dp[i] # Try the other direction
                err = run_lap(p[0], p[1], p[2], log_data=False)
                
                if err < best_error:
                    best_error = err
                    dp[i] *= 1.1
                else:
                    p[i] += dp[i] # Revert
                    dp[i] *= 0.9  # Shrink nudge
                    
        print(f"Current Best PID: Kp={p[0]:.3f}, Ki={p[1]:.3f}, Kd={p[2]:.3f} | Total Error: {best_error:.2f}")

    print("\n--- TUNING COMPLETE ---")
    print(f"Final Optimized PID: Kp={p[0]:.3f}, Ki={p[1]:.3f}, Kd={p[2]:.3f}")
    print("Update the variables at the top of your script and change AUTO_TUNE to False!")