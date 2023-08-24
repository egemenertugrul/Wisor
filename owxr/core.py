import os
import subprocess
import select
import json
import time
import logging
import numpy as np
from pathlib import Path
from queue import Queue

# from owxr.modules.clock import Clock

# from owxr.modules.sensor_mpu9250 import MPU9250
from owxr.modules.sensor_sensehat import SenseHat
from owxr.modules.qr import QRScanner
from owxr.modules.wifi import Wifi


class Core:
    status = None
    heartbeat_timeout_secs = 2

    def __init__(self, args):
        self.args = args
        self.FPS = self.args.fps
        os.environ["DISPLAY"] = self.args.display

        # self.clock = Clock(self.FPS)

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
            "FPSUpdate": self.update_fps,
            "Heartbeat": self.heartbeat,
            "Shutdown": self.shutdown,
        }

        self.heartbeat_remaining = (
            self.heartbeat_timeout_secs * 5
        )  # Give some time to launch

    def heartbeat(self):
        logging.debug(f"Heartbeat received from renderer.")
        self.heartbeat_remaining = self.heartbeat_timeout_secs

    def update_fps(self, new_fps):
        logging.debug(f"Updating FPS to: {new_fps}")
        self.FPS = int(new_fps)

    def set_renderer_ready(self):
        self.isRendererReady = True
        self.out_message_queue.put(self.update_status())

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

        self.renderer_wd = Path(os.path.normpath("../OpenWiXR-Renderer")).resolve()
        self.renderer_full_filepath = self.renderer_wd.joinpath("openwixr_renderer")
        if not self.renderer_full_filepath.is_file():
            logging.error(
                f"Renderer executable at {self.renderer_full_filepath} does not seem to exist.\nMake sure to build the renderer."
            )
            return
        
        self.start_renderer()

        # endregion

        self.isRunning = True

        # Run the raylib loop
        while self.isRunning:
            if self.heartbeat_remaining <= 0 and not self.args.disable_renderer:
                logging.warn(
                    f"No heartbeat from the renderer process for the last {self.heartbeat_timeout_secs} seconds."
                )
                self.isRendererReady = False
                self.heartbeat_remaining = self.heartbeat_timeout_secs * 5
                self.start_renderer()
                logging.warn(f"Restarting..")

            # Check if the pipe is ready for reading or writing
            read_ready, write_ready, _ = select.select(
                [self.to_core_pipe_fd], [self.to_renderer_pipe_fd], [], 0
            )

            if read_ready:
                # Read the message from the renderer
                message = os.read(self.to_core_pipe_fd, 4096)
                try:
                    messages = list(
                        filter(lambda s: len(s) > 0, message.decode().split("\n"))
                    )
                except Exception as e:
                    logging.exception(e)
                else:
                    for m in messages:
                        if m:
                            try:
                                message = json.loads(m)
                            except Exception as e:
                                logging.exception(e)
                                continue

                        try:
                            logging.info("==CORE== Received message from Renderer:")
                            message_type = message["type"]
                            logging.info(f"\tType: {message_type}")

                            cmd = self.commands_dict.get(message_type)

                            if cmd:
                                logging.info(f"\tData: {message.get('data')}")
                                data = message.get("data")
                        except KeyError as err:
                            logging.exception(err)
                        finally:
                            if cmd:
                                if data:
                                    cmd(data)
                                else:
                                    cmd()

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
            sleepTime = float(1 / self.FPS)
            if self.isRendererReady:
                self.heartbeat_remaining -= sleepTime

            time.sleep(sleepTime)
            # self.clock.sleep()

        # Close the pipe
        os.close(self.to_core_pipe_fd)
        os.close(self.to_renderer_pipe_fd)

        if self.renderer_process:
            self.renderer_process.wait()

    def start_process(self, wd, executable_full_filepath):
        process = subprocess.Popen(executable_full_filepath, cwd=wd)
        return process

    def start_renderer(self):
        if self.renderer_process:
            if self.renderer_process and isinstance(
                self.renderer_process, subprocess.Popen
            ):
                self.renderer_process.kill()

        if not self.args.disable_renderer:
            self.renderer_process = self.start_process(
                self.renderer_wd, self.renderer_full_filepath
            )

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
        logging.info("Shutting down..")
        self.isRunning = False
