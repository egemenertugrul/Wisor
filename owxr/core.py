import os
import subprocess
import select
import json
import time
import logging
import numpy as np
from pathlib import Path
from queue import Queue

from owxr.modules.clock import Clock

# from owxr.modules.sensor_mpu9250 import MPU9250
from owxr.modules.sensor_sensehat import SenseHat
from owxr.modules.qr import QRScanner
from owxr.modules.wifi import Wifi


class Core:
    status = None

    def __init__(self, args):
        self.args = args
        self.FPS = self.args.fps
        os.environ["DISPLAY"] = self.args.display

        self.clock = Clock(self.FPS)

        self.isRunning = False
        self.isRendererReady = False

        self.imu_sensor = SenseHat()

        self.renderer_process = None
        self.to_core_pipe_fd = None
        self.to_renderer_pipe_fd = None

        self.in_message_queue = Queue()
        self.out_message_queue = Queue()

        self.wifi = Wifi()
        self.qrScanner = QRScanner(duration=10)

        self.commands_dict = {
            "Begin": self.set_renderer_ready,
            "QRScan": self.qrScanner.start_scanning,
            "Shutdown": self.shutdown,
        }

    def set_renderer_ready(self):
        self.isRendererReady = True

    def get_wifi_state(self):
        return len(self.wifi.get_wifi_networks()) > 0

    def get_wifi_ssid(self):
        res = self.wifi.get_connected_wifi_ssid()
        return res if res is not None else ""

    def get_imu_state(self):
        return self.imu_sensor.is_initialized != None

    def get_camera_state(self):  # TODO: Get camera state from camera.py?
        return self.imu_sensor.is_initialized != None

    def update_status(self):
        self.status = status = {
            "is_wifi_connected": self.get_wifi_state(),
            "ssid": self.get_wifi_ssid(),
            "is_imu_connected": self.get_imu_state(),
            "is_camera_connected": self.get_camera_state(),
        }
        return {
            "type": "Status",
            "data": {
                "wifi": status["is_wifi_connected"],
                "ssid": status["ssid"],
                "imu": status["is_imu_connected"],
                "camera": status["is_camera_connected"],
            },
        }

    def start(self):
        if self.isRunning:
            logging.log("Is already running!")
            return

        # region Setup pipes and renderer

        to_core_pipe_path = "/tmp/to_core"
        if os.path.exists(to_core_pipe_path):
            os.remove(to_core_pipe_path)
        os.mkfifo(to_core_pipe_path)
        self.to_core_pipe_fd = os.open(to_core_pipe_path, os.O_RDWR | os.O_NONBLOCK)

        to_renderer_pipe_path = "/tmp/to_renderer"
        if os.path.exists(to_renderer_pipe_path):
            os.remove(to_renderer_pipe_path)
        os.mkfifo(to_renderer_pipe_path)
        self.to_renderer_pipe_fd = os.open(
            to_renderer_pipe_path, os.O_RDWR | os.O_NONBLOCK
        )

        # Start the renderer program as a child process
        if not self.args.disable_renderer:
            renderer_path_bin = Path(os.path.normpath("../OpenWiXR-Renderer")).resolve()
            renderer_filepath = renderer_path_bin.joinpath("openwixr_renderer")
            if not renderer_filepath.is_file():
                logging.error(
                    f"Renderer executable at {renderer_filepath} does not seem to exist.\nMake sure to build the renderer."
                )
                return

            self.renderer_process = subprocess.Popen(
                renderer_filepath, cwd=renderer_path_bin
            )

        # endregion

        self.out_message_queue.put(self.update_status())
        time.sleep(1)

        self.isRunning = True

        # Run the raylib loop
        while self.isRunning:
            # Check if the pipe is ready for reading or writing
            read_ready, write_ready, _ = select.select(
                [self.to_core_pipe_fd], [self.to_renderer_pipe_fd], [], 0
            )

            if read_ready:
                # Read the message from the renderer
                message = os.read(self.to_core_pipe_fd, 1024)
                messages = list(
                    filter(lambda s: len(s) > 0, message.decode().split("\0"))
                )
                for m in messages:
                    if m:
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
                if not self.out_message_queue.empty():
                    message = self.out_message_queue.get(block=False)
                    if message["data"] is not None and self.isRendererReady:
                        msg = json.dumps(message) + "\n"
                        encoded_msg = msg.encode()
                        # print(f"Sending:{encoded_msg}")
                        os.write(
                            self.to_renderer_pipe_fd,
                            encoded_msg,
                        )

            self.update()
            time.sleep(float(1 / self.FPS))
            # self.clock.sleep()

        # Close the pipe
        os.close(self.to_core_pipe_fd)
        os.close(self.to_renderer_pipe_fd)
        self.renderer_process.wait()

    def update(self):
        if self.qrScanner.process:
            data = self.qrScanner.get_qr_data()
            if data is not None:
                print(data)
                connect_res, log = self.wifi.connect_to(data["S"], data["P"])
                self.qrScanner.stop_scanning()
                self.out_message_queue.put(self.update_status())

        if self.imu_sensor:
            self.out_message_queue.put(
                {"type": "Sensor", "data": self.imu_sensor.get_data()}
            )

    def shutdown(self):
        logging.log("Shutting down..")
        self.isRunning = False
