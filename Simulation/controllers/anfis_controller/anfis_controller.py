from controller import Supervisor
import json
import math
from pathlib import Path

NUM_SENSORS = 16
MAX_SPEED = 6.28
BASE_SPEED = 4.0
WHITE_VALUE = 1000.0
MODEL_FILE = "../../python/anfis_model.json"
START_TRANS = [-0.33, 0.18, -6.39633e-05]
START_ROT = [-0.001052140210489292, 0.0007020571404522981, -0.9999992000580545, -5.307179586466759e-06]


class RuntimeAnfisModel:
    def __init__(self, payload):
        self.feature_columns = tuple(payload["feature_columns"])
        self.target_column = payload["target_column"]
        self.feature_stats = payload["feature_stats"]
        self.target_stats = payload["target_stats"]
        self.rule_indices = [tuple(rule) for rule in payload["rule_indices"]]
        self.centers = payload["centers"]
        self.sigmas = payload["sigmas"]
        self.consequents = payload["consequents"]

    @classmethod
    def load(cls, path):
        path = Path(path)
        if not path.exists():
            raise FileNotFoundError(
                f"Model file not found: {path}. Train the fuzzy controller first with Simulation/python/anfis_train.py."
            )
        with path.open("r", encoding="utf-8") as handle:
            return cls(json.load(handle))

    def _scale(self, value, minimum, maximum):
        if maximum == minimum:
            return 0.0
        return 2.0 * ((value - minimum) / (maximum - minimum)) - 1.0

    def _unscale(self, value, minimum, maximum):
        if maximum == minimum:
            return minimum
        return minimum + ((value + 1.0) * 0.5) * (maximum - minimum)

    def _gaussian(self, value, center, sigma):
        sigma = max(float(sigma), 1e-6)
        return math.exp(-0.5 * ((value - center) / sigma) ** 2)

    def predict(self, middle_error, delta_time):
        x_values = [float(middle_error), float(delta_time)]
        x_scaled = []
        for value, column in zip(x_values, self.feature_columns):
            stats = self.feature_stats[column]
            x_scaled.append(self._scale(value, stats["min"], stats["max"]))

        weights = []
        for rule in self.rule_indices:
            firing = 1.0
            for dimension, mf_index in enumerate(rule):
                firing *= self._gaussian(
                    x_scaled[dimension],
                    self.centers[dimension][mf_index],
                    self.sigmas[dimension][mf_index],
                )
            weights.append(firing)

        weight_total = sum(weights)
        if weight_total <= 1e-12:
            normalized = [1.0 / len(weights)] * len(weights)
        else:
            normalized = [weight / weight_total for weight in weights]

        output_scaled = 0.0
        for normalized_weight, consequent in zip(normalized, self.consequents):
            linear = consequent[0] * x_scaled[0] + consequent[1] * x_scaled[1] + consequent[2]
            output_scaled += normalized_weight * linear

        stats = self.target_stats
        return self._unscale(output_scaled, stats["min"], stats["max"])


def compute_middle_error(sensor_values):
    middle_start = (NUM_SENSORS // 2) - 2
    middle_end = (NUM_SENSORS // 2) + 2
    weighted_sum = 0.0
    total_intensity = 0.0

    for index in range(middle_start, middle_end):
        intensity = max(0.0, WHITE_VALUE - sensor_values[index])
        weighted_sum += intensity * (index - (NUM_SENSORS - 1) / 2.0)
        total_intensity += intensity

    if total_intensity > 0:
        return weighted_sum / total_intensity
    return 0.0


def main():
    model = RuntimeAnfisModel.load(MODEL_FILE)
    robot = Supervisor()
    timestep = int(robot.getBasicTimeStep())

    robot_node = robot.getFromDef("SpeedyBee")
    if robot_node is None:
        print("WARNING: Could not find DEF 'SpeedyBee'. Teleporting will not work!")

    sensors = []
    for index in range(NUM_SENSORS):
        sensor = robot.getDevice(f"ir{index}")
        sensor.enable(timestep)
        sensors.append(sensor)

    left_motor = robot.getDevice("left wheel motor")
    right_motor = robot.getDevice("right wheel motor")
    left_motor.setPosition(float("inf"))
    right_motor.setPosition(float("inf"))
    left_motor.setVelocity(0.0)
    right_motor.setVelocity(0.0)

    last_time = robot.getTime()
    while robot.step(timestep) != -1:
        sensor_values = [sensor.getValue() for sensor in sensors]
        middle_error = compute_middle_error(sensor_values)
        current_time = robot.getTime()
        delta_time = max(current_time - last_time, 1e-6)
        last_time = current_time

        steering_output = model.predict(middle_error, delta_time)
        left_speed = max(min(BASE_SPEED + steering_output, MAX_SPEED), -MAX_SPEED)
        right_speed = max(min(BASE_SPEED - steering_output, MAX_SPEED), -MAX_SPEED)

        left_motor.setVelocity(right_speed)
        right_motor.setVelocity(left_speed)


if __name__ == "__main__":
    main()
