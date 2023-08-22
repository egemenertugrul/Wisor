import random
import time

from imusensor.MPU9250 import MPU9250
from imusensor.filters import kalman, madgwick
import smbus

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
    _sensorfusion = None

    newTime = None
    currTime = None
    DEG2RAD = 0.0174532925
    
    def __init__(self) -> None:
        self.accelVals = [1.0, 1.0, 9.81] # dummy data
        self.rotVals = list(map(lambda n: n * random.random(), [3.14, 3.14, 3.14]))
        
        self.is_initialized = self.initialize_imu()

    def initialize_imu(self) -> bool:
        try:
            address = 0x68
            bus = smbus.SMBus(1)
            self._imu = MPU9250.MPU9250(bus, address)
            self._imu.begin()
            self._imu.loadCalibDataFromFile("./owxr/imu_only/calib.json")

            # self._sensorfusion = kalman.Kalman()
            # self._sensorfusion_fn = self._sensorfusion.updateRollPitchYaw
            self._sensorfusion = madgwick.Madgwick(0.5)
            self._sensorfusion_fn = self._sensorfusion.updateRollPitchYaw
        except Exception as e:
            print(e)
            return False
        else:
            return True

    def get_data_dummy(self) -> dict:
        #imu.readSensor()
	    #imu.computeOrientation()
	
        now = float(time.time())
        
        # imu.AccelVals[0], imu.AccelVals[1], imu.AccelVals[2], imu.GyroVals[0], imu.GyroVals[1], imu.GyroVals[2]
        self.accelVals = [1.0, 1.0, 9.81] # dummy data
        self.rotVals = list(map(lambda n: n + 0.01 * random.random(), self.rotVals))
        # if print_count == 5:
        # 	# print ("roll: {0} ; pitch : {1} ; yaw : {2}".format(imu.roll, imu.pitch, imu.yaw))
        # 	# print("Kalmanroll:{0} KalmanPitch:{1} KalmanYaw:{2} ".format(kalman_filter.roll, kalman_filter.pitch, kalman_filter.yaw))

        # 	md = dict(topic = 'orientation', acc = list(map(float, imu.AccelVals)), gyro = list(map(float, imu.GyroVals)), time = str(now))
        # 	socket.send_json(md)
        # 	# print_count = 0

        data = IMU_Data(self.accelVals, self.rotVals, now).data
        return data

    def get_data(self) -> dict:
        while not self.is_initialized:
            self.is_initialized = self.initialize_imu()

        imu = self._imu
        sensorfusion = self._sensorfusion

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

        self._sensorfusion_fn(imu.AccelVals[0], imu.AccelVals[1], imu.AccelVals[2], imu.GyroVals[0], imu.GyroVals[1], imu.GyroVals[2],\
												imu.MagVals[0], imu.MagVals[1], imu.MagVals[2], dt)

        # print("Kalmanroll:{0} KalmanPitch:{1} KalmanYaw:{2} ".format(sensorfusion.roll, sensorfusion.pitch, sensorfusion.yaw))

        self.accelVals = [imu.AccelVals[0], imu.AccelVals[1], imu.AccelVals[2]]
        self.rotVals = [sensorfusion.pitch * self.DEG2RAD, -sensorfusion.yaw * self.DEG2RAD, sensorfusion.roll * self.DEG2RAD]
        # self.rotVals[1] = 0
        # self.rotVals[2] = 0
        
        now = float(time.time())
        data = IMU_Data(self.accelVals, self.rotVals, now).data
        return data

if __name__ == "__main__":
    imu_sensor = IMU_Sensor()

    while True:
        data = imu_sensor.get_data()
        print(data)