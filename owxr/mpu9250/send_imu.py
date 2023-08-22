import os
import sys
import time
import smbus
import numpy as np
import zmq

from imusensor.MPU9250 import MPU9250
from imusensor.filters import kalman


# initializing publisher
host = '*'
port = 8358
url = 'tcp://'+host+':'+str(port)
context = zmq.Context()
socket = context.socket(zmq.PUB)
socket.setsockopt(zmq.SNDHWM, 1000)
socket.bind(url)

kalman_filter = kalman.Kalman()

# address = 0x68
# bus = smbus.SMBus(1)
# imu = MPU9250.MPU9250(bus, address)
# imu.begin()

# imu.loadCalibDataFromFile("./calib.json")

# imu.readSensor()
# imu.computeOrientation()
# kalman_filter.roll = imu.roll
# kalman_filter.pitch = imu.pitch
# kalman_filter.yaw = imu.yaw

# print_count = 0
# sensor_count = 0
# currTime = time.time()
# kal_currTime = time.time()
# imu.readSensor()
# while True:

# 	imu.readSensor()
# 	imu.computeOrientation()
	
# 	kal_newTime = time.time()
# 	kal_dt = kal_newTime - kal_currTime
# 	kal_currTime = kal_newTime
	
# 	kalman_filter.computeAndUpdateRollPitchYaw(imu.AccelVals[0], imu.AccelVals[1], imu.AccelVals[2], imu.GyroVals[0], imu.GyroVals[1], imu.GyroVals[2],\
# 												imu.MagVals[0], imu.MagVals[1], imu.MagVals[2], kal_dt)

# 	if print_count == 5:
# 		print ("roll: {0} ; pitch : {1} ; yaw : {2}".format(imu.roll, imu.pitch, imu.yaw))
# 		print("Kalmanroll:{0} KalmanPitch:{1} KalmanYaw:{2} ".format(kalman_filter.roll, kalman_filter.pitch, kalman_filter.yaw))

# 		md = dict(topic = 'orientation', normal = str([imu.roll, imu.pitch, imu.yaw]), kalman = str([kalman_filter.roll, kalman_filter.pitch, kalman_filter.yaw]))
# 		socket.send_json(md)
# 		print_count = 0

# 	print_count = print_count + 1
# 	sensor_count = sensor_count + 1
# 	time.sleep(0.001)
currTime = time.time()
# sensorfusion = kalman.Kalman()

from imusensor.filters import madgwick
sensorfusion = madgwick.Madgwick(1)


while True:
	try:
		imu.readSensor()
		imu.computeOrientation()
		imu.loadCalibDataFromFile("./owxr/imu_only/calib.json")
	except:
		try:
			address = 0x68
			bus = smbus.SMBus(1)
			imu = MPU9250.MPU9250(bus, address)
			imu.begin()
			continue
		except:
			continue

	now = float(time.time())
	
	# imu.AccelVals[0], imu.AccelVals[1], imu.AccelVals[2], imu.GyroVals[0], imu.GyroVals[1], imu.GyroVals[2]

	# if print_count == 5:
		# print ("roll: {0} ; pitch : {1} ; yaw : {2}".format(imu.roll, imu.pitch, imu.yaw))
		# print("Kalmanroll:{0} KalmanPitch:{1} KalmanYaw:{2} ".format(kalman_filter.roll, kalman_filter.pitch, kalman_filter.yaw))

		# md = dict(topic = 'orientation', acc = list(map(float, imu.AccelVals)), gyro = list(map(float, imu.GyroVals)), time = str(now))
		# socket.send_json(md)
		# print_count = 0

	# md = dict(topic = 'orientation', acc = list(map(float, imu.AccelVals)), gyro = list(map(float, imu.GyroVals)), time = float(now))

	newTime = time.time()
	dt = newTime - currTime
	currTime = newTime

	sensorfusion.updateRollPitchYaw(imu.AccelVals[0], imu.AccelVals[1], imu.AccelVals[2], imu.GyroVals[0], imu.GyroVals[1], imu.GyroVals[2],\
												imu.MagVals[0], imu.MagVals[1], imu.MagVals[2], dt)

	# print("Kalmanroll:{0} KalmanPitch:{1} KalmanYaw:{2} ".format(sensorfusion.roll, sensorfusion.pitch, sensorfusion.yaw))

	md = dict(topic = 'orientation', acc = list(map(float, imu.AccelVals)), gyro = list(map(float, [sensorfusion.roll, sensorfusion.pitch, sensorfusion.yaw])), time = float(now))
	socket.send_json(md)

	# print_count = print_count + 1
	# sensor_count = sensor_count + 1
	
	# time.sleep(0.01)
