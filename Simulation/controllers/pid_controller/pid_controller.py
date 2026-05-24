from controller import Supervisor
import csv

# 1. Initialize Robot as a SUPERVISOR
robot = Supervisor()
timestep = int(robot.getBasicTimeStep())

# ==========================================
# ⚙️ OPERATION MODE
# ==========================================
# Set to True to find perfect PID values. Set to False to log ANFIS data.
AUTO_TUNE = False  

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
Kp = 12.813           
Ki = 0.0
Kd = 0.785

# ANFIS data-collection settings
ANFIS_TARGET_ROWS = 7000
ANFIS_OUTPUT_FILE = 'anfis_16_sensor_data.csv'
ANFIS_MIN_LOOP_STEPS = 1200
ANFIS_RETURN_THRESHOLD = 0.15


def compute_middle_error(sensor_values):
    """Compute a reduced cross-track error from the middle sensors only."""
    middle_start = (NUM_SENSORS // 2) - 2
    middle_end = (NUM_SENSORS // 2) + 2
    weighted_sum = 0.0
    total_intensity = 0.0

    for index in range(middle_start, middle_end):
        intensity = max(0.0, WHITE_VALUE - sensor_values[index])
        weighted_sum += intensity * weights[index]
        total_intensity += intensity

    if total_intensity > 0:
        return weighted_sum / total_intensity
    return 0.0

def reset_robot():
    """Teleports the robot back to the starting line."""
    if robot_node:
        robot_node.getField("translation").setSFVec3f(START_TRANS)
        robot_node.getField("rotation").setSFRotation(START_ROT)
        robot_node.resetPhysics()

def run_lap(current_Kp, current_Ki, current_Kd, log_data=False, csv_writer=None, row_state=None):
    """Runs a single lap.

    Returns a tuple: (score, lap_completed)
    - score: total error adjusted by a reward if the robot completes a full loop
    - lap_completed: boolean indicating whether a loop (return to start) was detected
    Lower score is better.
    """
    reset_robot()
    print(f"Running lap with Kp={current_Kp:.3f}, Ki={current_Ki:.3f}, Kd={current_Kd:.3f}")
    last_error = 0.0
    integral = 0.0
    total_lap_error = 0.0
    rows_written = 0
    step_count = 0
    max_steps = 2500 # Adjust this to match how many steps it takes to finish 1 lap
    # Loop-detection parameters
    lap_completed = False
    # Grab the starting translation to detect when we return close to it
    if robot_node:
        start_pos = robot_node.getField("translation").getSFVec3f()
    else:
        start_pos = START_TRANS
    min_steps_before_check = ANFIS_MIN_LOOP_STEPS if log_data else int(0.1 * max_steps)
    return_threshold = ANFIS_RETURN_THRESHOLD
    
    if log_data and csv_writer is None:
        raise ValueError("csv_writer is required when log_data=True")

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

        # Calculate full cross-track error for control.
        if total_intensity > 0:
            error = weighted_sum / total_intensity
        else:
            error = 0.0

        middle_error = compute_middle_error(sensor_values)
        current_time = robot.getTime()
        if row_state is None:
            row_state = {"last_time": current_time}
        delta_time = current_time - row_state["last_time"]
        row_state["last_time"] = current_time
            
        # Accumulate error for the Twiddle algorithm to judge this lap's performance
        total_lap_error += abs(error)
            
        # PID Mathematics
        derivative = error - last_error
        integral += error
        steering_output = (current_Kp * error) + (current_Ki * integral) + (current_Kd * derivative)
        last_error = error
        
        # Log reduced ANFIS data if enabled
        if log_data:
            csv_writer.writerow([
                current_time,
                delta_time,
                middle_error,
                steering_output,
                error,
                derivative,
            ])
            rows_written += 1
        
        # Apply differential steering (Keeping your confirmed swapped motors)
        left_speed = BASE_SPEED + steering_output
        right_speed = BASE_SPEED - steering_output
        
        left_speed = max(min(left_speed, MAX_SPEED), -MAX_SPEED)
        right_speed = max(min(right_speed, MAX_SPEED), -MAX_SPEED)
        
        left_motor.setVelocity(right_speed)
        right_motor.setVelocity(left_speed)
        
        # Check if we've come back close to the starting position (loop completed)
        if robot_node and step_count > min_steps_before_check and not lap_completed:
            cur_pos = robot_node.getField("translation").getSFVec3f()
            dx = cur_pos[0] - start_pos[0]
            dy = cur_pos[1] - start_pos[1]
            dz = cur_pos[2] - start_pos[2]
            dist_sq = dx*dx + dy*dy + dz*dz
            if dist_sq <= return_threshold * return_threshold:
                lap_completed = True
                # Optionally break early — a completed loop is a valid termination
                # break

        step_count += 1
        
    # Stop motors at the end of the lap
    left_motor.setVelocity(0.0)
    right_motor.setVelocity(0.0)
    
    # Compute a final score. If lap completed, apply a reward that reduces the score
    # (twiddle expects lower scores to be better). The reward scales with how
    # quickly the loop was completed (fewer steps -> larger reward).
    reward = 0.0
    if lap_completed:
        reward = (max_steps - step_count) * 0.02  # tuning constant

    final_score = max(0.0, total_lap_error - reward)
    return final_score, lap_completed, rows_written

# ==========================================
# MAIN EXECUTION
# ==========================================
if not AUTO_TUNE:
    print(f"Running Data Collection Laps... (Kp={Kp}, Ki={Ki}, Kd={Kd})")
    total_rows = 0
    lap_index = 0
    with open(ANFIS_OUTPUT_FILE, mode='w', newline='') as csv_file:
        csv_writer = csv.writer(csv_file)
        headers = ['timestamp', 'delta_time', 'middle_error', 'steering_output', 'full_error', 'derivative']
        csv_writer.writerow(headers)

        while total_rows < ANFIS_TARGET_ROWS:
            lap_index += 1
            row_state = {"last_time": robot.getTime()}
            lap_score, lap_completed, lap_rows = run_lap(Kp, Ki, Kd, log_data=True, csv_writer=csv_writer, row_state=row_state)
            total_rows += lap_rows
            print(
                f"Lap {lap_index}: rows={lap_rows}, total_rows={total_rows}, "
                f"score={lap_score:.2f}, loop_completed={lap_completed}"
            )

    print(f"Data collection complete! Wrote {total_rows} rows to {ANFIS_OUTPUT_FILE}")
    
else:
    print("Starting Twiddle Auto-Tuning...")
    p = [Kp, Ki, Kd]
    dp = [0.5, 0.0, 0.1] # Initial nudge amounts
    
    best_score, best_lap, _ = run_lap(p[0], p[1], p[2], log_data=False)
    print(f"Baseline Score: {best_score:.2f} | Lap Completed: {best_lap}")

    while sum(dp) > 0.01: # Stop when nudges get very small
        for i in range(len(p)):
            p[i] += dp[i]
            err_score, lap_done, _ = run_lap(p[0], p[1], p[2], log_data=False)

            if err_score < best_score:
                best_score = err_score
                best_lap = lap_done
                dp[i] *= 1.1 # Nudge bigger next time
            else:
                p[i] -= 2 * dp[i] # Try the other direction
                err_score, lap_done, _ = run_lap(p[0], p[1], p[2], log_data=False)

                if err_score < best_score:
                    best_score = err_score
                    best_lap = lap_done
                    dp[i] *= 1.1
                else:
                    p[i] += dp[i] # Revert
                    dp[i] *= 0.9  # Shrink nudge
                    
        print(f"Current Best PID: Kp={p[0]:.3f}, Ki={p[1]:.3f}, Kd={p[2]:.3f} | Best Score: {best_score:.2f} | Lap: {best_lap}")

    print("\n--- TUNING COMPLETE ---")
    print(f"Final Optimized PID: Kp={p[0]:.3f}, Ki={p[1]:.3f}, Kd={p[2]:.3f}")
    print("Update the variables at the top of your script and change AUTO_TUNE to False!")