import random
import time
import logging

from imusensor.MPU9250 import MPU9250 as MPU9250Lib
from imusensor.filters import kalman, madgwick
import smbus

from sensor import IMU_Sensor, IMU_Data


class MPU9250:
    imu = None
    sensorfusion = None

    newTime = None
    currTime = None

    def __init__(self) -> None:
        super().__init__()

    def initialize_imu(self) -> bool:
        try:
            address = 0x68
            bus = smbus.SMBus(1)
            self.imu = MPU9250Lib.MPU9250(bus, address)
            self.imu.begin()
            self.imu.loadCalibDataFromFile("./owxr/mpu9250/calib.json")

            # self.sensorfusion = kalman.Kalman()
            # self.sensorfusion_fn = self.sensorfusion.updateRollPitchYaw

            self.sensorfusion = madgwick.Madgwick(0.5)
            self.sensorfusion_fn = self.sensorfusion.updateRollPitchYaw
        except Exception as e:
            logging.error(e)
            return False
        else:
            return True

    def get_data(self) -> dict:
        while not self.is_initialized:
            self.is_initialized = self.initialize_imu()

        imu = self.imu
        sensorfusion = self.sensorfusion

        try:
            imu.readSensor()
            imu.computeOrientation()
        except OSError as e:
            print(e)
            return None

        # sensorfusion.roll = imu.roll
        # sensorfusion.pitch = imu.pitch
        # sensorfusion.yaw = imu.yaw

        self.newTime = time.time()
        if self.currTime is None:
            self.currTime = self.newTime
            return None
        dt = self.newTime - self.currTime
        self.currTime = self.newTime

        self.sensorfusion_fn(
            imu.AccelVals[0],
            imu.AccelVals[1],
            imu.AccelVals[2],
            imu.GyroVals[0],
            imu.GyroVals[1],
            imu.GyroVals[2],
            imu.MagVals[0],
            imu.MagVals[1],
            imu.MagVals[2],
            dt,
        )
        self.accelVals = [imu.AccelVals[0], imu.AccelVals[1], imu.AccelVals[2]]
        self.rotVals = [
            sensorfusion.pitch * self.DEG2RAD,
            -sensorfusion.yaw * self.DEG2RAD,
            sensorfusion.roll * self.DEG2RAD,
        ]
        now = float(time.time())
        data = IMU_Data(self.accelVals, self.rotVals, now).to_dict()
        return data


if __name__ == "__main__":
    imu_sensor = IMU_Sensor()

    while True:
        data = imu_sensor.get_data()
        print(data)
