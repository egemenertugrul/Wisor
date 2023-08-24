import os
import subprocess
import select
import json
import time
import logging
import numpy as np
from pathlib import Path
from queue import Queue
from enum import Enum

# from owxr.modules.clock import Clock

# from owxr.modules.sensor_mpu9250 import MPU9250
from owxr.modules.sensor_sensehat import SenseHat
from owxr.modules.qr import QRScanner
from owxr.modules.wifi import Wifi

from owxr.modules.duplex_socket import DuplexWebsocketsServerProcess

from owxr.utils.process_helper import ProcessHelper
from owxr.utils.evalue import EValue


class OpMode(Enum):
    NONE = 1
    STANDALONE = 2
    REMOTE = 3


class Core:
    status = None
    isRunning = False
    isRendererReady = False
    renderer_process = None
    socket_process = None
    to_core_pipe_fd = None
    to_renderer_pipe_fd = None

    heartbeat_timeout_secs = 2
    omx_cmd = "omxplayer -o hdmi udp://127.0.0.1:5000 --timeout 0 --no-keys --fps 60 --aspect-mode stretch --live"
    omx_player_process = None

    def __init__(self, args):
        self.args = args
        self.FPS = self.args.fps or 90
        os.environ["DISPLAY"] = self.args.display or ":0"
        self.opMode = EValue(OpMode.STANDALONE)
        self.opMode.on("change", self.on_opmode_change)

        # self.clock = Clock(self.FPS)
        self.imu_sensor = SenseHat()

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

    def on_opmode_change(self, mode: OpMode):
        logging.info(f"OpMode changed: {mode}")
        if mode == OpMode.REMOTE:
            ProcessHelper.stop_process(self.omx_player_process)
            logging.debug("Starting omxplayer..")
            self.omx_player_process = ProcessHelper.start_command_process(self.omx_cmd)
        elif mode == OpMode.STANDALONE:
            ProcessHelper.stop_process(self.omx_player_process)
            logging.debug("Stopping omxplayer..")

    def heartbeat(self):
        logging.debug(f"Heartbeat received from renderer.")
        self.heartbeat_remaining = self.heartbeat_timeout_secs

    def update_fps(self, new_fps):
        if self.args.dynamic_fps:
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

    def open_pipe(self, path):
        if os.path.exists(path):
            os.remove(path)
        os.mkfifo(path)
        return os.open(path, os.O_RDWR | os.O_NONBLOCK)

    def on_remote_connection_begin(self):
        self.opMode.value = OpMode.REMOTE

    def on_remote_connection_end(self):
        self.opMode.value = OpMode.STANDALONE

    def start(self):
        if self.isRunning:
            logging.log("Is already running!")
            return

        self.socket_process = DuplexWebsocketsServerProcess(daemon=False)
        self.socket_process.on("Connect", self.on_remote_connection_begin)
        self.socket_process.on("Disconnect", self.on_remote_connection_end)
        self.socket_process.start()

        # region Setup pipes and renderer

        self.to_core_pipe_fd = self.open_pipe("/tmp/to_core")
        self.to_renderer_pipe_fd = self.open_pipe("/tmp/to_renderer")

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

        ProcessHelper.stop_process(self.renderer_process)
        ProcessHelper.stop_process(self.socket_process)
        ProcessHelper.stop_process(self.omx_player_process)

    def start_renderer(self):
        ProcessHelper.stop_process(self.renderer_process)

        if not self.args.disable_renderer:
            self.renderer_process = ProcessHelper.start_process(
                self.renderer_full_filepath, self.renderer_wd
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
        time.sleep(0.5)
        self.isRunning = False
