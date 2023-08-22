import random
import time
from sense_hat import SenseHat 

class IMU_Data:
    data = None

    def __init__(self, acc: list, rot: list, time: float) -> None:
        self.data = dict({"acc": acc, "rot": rot, "time": time})

    def __str__(self) -> str:
        acc = self.data["acc"]
        rot = self.data["rot"]

        acc_str = "".join(list(map(str, acc)))
        rot_str = "".join(list(map(str, rot)))

        return f"Acc: {acc_str}\nRot: {rot_str}"

class IMU_Sensor:
    _imu = None
    DEG2RAD = 0.0174532925
    
    def __init__(self) -> None:
        self.accelVals = [1.0, 1.0, 9.81] # dummy data
        self.rotVals = list(map(lambda n: n * random.random(), [3.14, 3.14, 3.14]))
        
        self.is_initialized = self.initialize_imu()

    def initialize_imu(self) -> bool:
        try:
            self._imu = SenseHat()
            self._imu.clear()
        except Exception as e:
            return False
        else:
            return True

    def get_data(self) -> dict:
        while not self.is_initialized:
            self.is_initialized = self.initialize_imu()
        imu = self._imu

        rawAccel = imu.get_accelerometer_raw()
        orientation = imu.get_orientation_radians()
        self.accelVals = [rawAccel["x"], rawAccel["y"], rawAccel["z"]]
        self.rotVals = [orientation["pitch"], orientation["yaw"], orientation["roll"]]
        now = float(time.time())
        data = IMU_Data(self.accelVals, self.rotVals, now).data
        return data

if __name__ == "__main__":
    imu_sensor = IMU_Sensor()

    while True:
        data = imu_sensor.get_data()
        print(data)