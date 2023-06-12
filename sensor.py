import random
import time

class IMU_Data:
    data = None
    def __init__(self, acc: list, rot: list, time: float) -> None:
        self.data = dict({"acc": acc, "rot": rot, "time": time})

class IMU_Sensor:
    def __init__(self) -> None:
        self.accelVals = [1.0, 1.0, 9.81] # dummy data
        self.rotVals = list(map(lambda n: n * random.random(), [3.14, 3.14, 3.14]))

    def get_data(self) -> dict:
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