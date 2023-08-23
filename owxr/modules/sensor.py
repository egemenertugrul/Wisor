import random
import time
from abc import ABC, abstractmethod

DEG2RAD = 0.0174532925


class IMU_Data:
    def __init__(
        self, acc: list, gyroscope: list, orientation: list, timestamp: float
    ) -> None:
        self.acc = acc
        self.gyroscope = gyroscope
        self.orientation = orientation
        self.timestamp = timestamp

    def to_dict(self) -> dict:
        return dict(
            {
                "acceleration": self.acc,
                "gyroscope": self.gyroscope,
                "orientation": self.orientation,
                "time": self.timestamp,
            }
        )

    def __str__(self) -> str:
        acc = self.acc
        gyro = self.gyro
        orientation = self.orientation

        acc_str = "".join(list(map(str, acc)))
        gyro_str = "".join(list(map(str, gyro)))
        orientation_str = "".join(list(map(str, orientation)))

        return f"Acceleration: {acc_str}\nGyroscope: {gyro_str}\nOrientation: {orientation_str}"


class IMU_Sensor(ABC):
    imu = None
    accelVals = [0.0, 0.0, 9.81]
    gyroVals = [0, 0, 0]
    orientationVals = [0, 0, 0]

    def __init__(self) -> None:
        self.is_initialized = self.initialize_imu()

    @abstractmethod
    def initialize_imu(self) -> bool:
        pass

    def get_data_dummy(self) -> dict:
        now = float(time.time())
        self.accelVals = [0.0, 0.0, 9.81]  # dummy data
        self.gyroVals = list(map(lambda n: n + 0.01 * random.random(), self.gyroVals))
        self.orientationVals = list(
            map(lambda n: n + 0.01 * random.random(), self.orientationVals)
        )

        data = IMU_Data(
            self.accelVals, self.gyroVals, self.orientationVals, now
        ).to_dict()
        return data

    @abstractmethod
    def get_data(self) -> dict:
        pass
