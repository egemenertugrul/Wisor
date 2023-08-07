import os
import subprocess
import select
import json
import time
import logging
import numpy as np
from pathlib import Path
from queue import Queue

from owxr.modules.sensor import IMU_Sensor
from owxr.modules.qr import QRScanner
from owxr.modules.wifi import Wifi

class Core:
    status = None

    def __init__(self):
        # os.environ['DISPLAY'] = ":0"

        self.isRunning = False

        self.imu_sensor = IMU_Sensor()

        self.renderer_process = None
        self.to_core_pipe_fd = None
        self.to_renderer_pipe_fd = None

        self.in_message_queue = Queue()
        self.out_message_queue = Queue()

        self.wifi = Wifi()
        self.qrScanner = QRScanner(duration=10)

        self.commands_dict = {
            "QRScan": self.qrScanner.start_scanning,
            "Shutdown": self.shutdown
        }

    def get_wifi_state(self):
        return len(self.wifi.get_wifi_networks()) > 0

    def get_wifi_ssid(self):
        res = self.wifi.get_connected_wifi_ssid()
        return res if res is not None else ""

    def get_imu_state(self):
        return self.imu_sensor.get_data() != None

    def get_camera_state(self):  # TODO: Get camera state from camera.py?
        return self.imu_sensor.get_data() != None

    def update_status(self):
        self.status = status = {
            "is_wifi_connected": self.get_wifi_state(),
            "ssid": self.get_wifi_ssid(),
            "is_imu_connected": self.get_imu_state(),
            "is_camera_connected": self.get_camera_state()
        }
        return {
            "type": "Status",
            "data": {
                "wifi": status["is_wifi_connected"],
                "ssid": status["ssid"],
                "imu": status["is_imu_connected"],
                "camera": status["is_camera_connected"],
            }
        }

    def start(self):
        if self.isRunning:
            logging.log("Is already running!")
            return

        # region Setup pipes and renderer
        # Start the renderer program as a child process
        renderer_path = Path(os.path.normpath("../OpenWiXR-Renderer/")).resolve()
        renderer_path_bin = renderer_path.joinpath("_bin/Debug/")
        self.renderer_process = subprocess.Popen(os.path.join(renderer_path_bin, "OpenWiXR-Renderer"), cwd=renderer_path)

        to_core_pipe_path = "/tmp/to_core"
        if os.path.exists(to_core_pipe_path):
            os.remove(to_core_pipe_path)
        os.mkfifo(to_core_pipe_path)
        self.to_core_pipe_fd = os.open(to_core_pipe_path, os.O_RDWR | os.O_NONBLOCK)

        to_renderer_pipe_path = "/tmp/to_renderer"
        if os.path.exists(to_renderer_pipe_path):
            os.remove(to_renderer_pipe_path)
        os.mkfifo(to_renderer_pipe_path)
        self.to_renderer_pipe_fd = os.open(to_renderer_pipe_path, os.O_RDWR | os.O_NONBLOCK)
        # endregion

        self.out_message_queue.put(self.update_status())
        # time.sleep(1)

        self.isRunning = True

        # Run the raylib loop
        while self.isRunning:
            # Check if the pipe is ready for reading or writing
            read_ready, write_ready, _ = select.select([self.to_core_pipe_fd], [self.to_renderer_pipe_fd], [], 0)

            if read_ready:
                # Read the message from the renderer
                message = os.read(self.to_core_pipe_fd, 1024)
                messages = list(filter(lambda s: len(s) > 0, message.decode().split("\0")))
                for m in messages:
                    if m:
                        # Process the received message
                        try:
                            message = json.loads(m)
                        except Exception as e:
                            logging.error(e)
                            continue
                        
                        if message["type"] == "Command":
                            try:
                                cmd = self.commands_dict[message["data"]]
                            except KeyError as err:
                                logging.error(err)
                            else:
                                cmd()

                        logging.debug("==CORE== Received message from Renderer:")
                        logging.debug("\tType:", message["type"])
                        logging.debug("\tData:", message["data"])

            if write_ready:
                # Send a message to the renderer
                # message = {
                #     "type": "Greeting",
                #     "data": "Hello, Renderer!"
                # }
                if self.qrScanner.process is not None:
                    data = self.qrScanner.get_qr_data()
                    if data is not None:
                        print(data)
                        connect_res, log = self.wifi.connect_to(data["S"], data["P"])
                        self.qrScanner.stop_scanning()
                        self.out_message_queue.put(self.update_status())

                if not self.out_message_queue.empty():
                    message = self.out_message_queue.get(block=False)
                    # print(f"Sending: {message}")
                    os.write(self.to_renderer_pipe_fd, json.dumps(message).encode())

            data = self.imu_sensor.get_data()
            message = {
                "type": "Sensor",
                "data": data
            }
            self.out_message_queue.put(message)

            time.sleep(float(1/60))
            # Add any necessary synchronization mechanisms if needed

        # Close the pipe
        os.close(self.to_renderer_pipe_fd)

        # Wait for the renderer process to finish
        renderer_process.wait()

    def shutdown(self):
        logging.log("Shutting down..")
        self.isRunning = False