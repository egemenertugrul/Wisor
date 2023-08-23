import random
import time
from sense_hat import SenseHat as SenseHatLib
from owxr.modules.sensor import IMU_Sensor, IMU_Data
import logging


class SenseHat(IMU_Sensor):
    def __init__(self) -> None:
        super().__init__()

    def initialize_imu(self) -> bool:
        try:
            self.imu = SenseHatLib()
            self.imu.set_imu_config(True, True, True)
        except Exception as e:
            logging.error(e)
            return False
        else:
            return True

    def get_data(self) -> dict:
        while not self.is_initialized:
            self.is_initialized = self.initialize_imu()
        imu = self.imu

        acc = imu.get_accelerometer_raw()
        gyro = imu.get_gyroscope_raw()
        orientation = imu.get_orientation_radians()

        self.accelVals = [acc["x"], acc["y"], acc["z"]]
        self.gyroVals = [gyro["x"], gyro["y"], gyro["z"]]
        self.orientationVals = [
            orientation["pitch"],
            orientation["yaw"],
            orientation["roll"],
        ]

        now = float(time.time())
        data = IMU_Data(
            self.accelVals, self.gyroVals, self.orientationVals, now
        ).to_dict()
        return data


if __name__ == "__main__":
    imu_sensor = SenseHat()

    while True:
        data = imu_sensor.get_data()
        print(data)
