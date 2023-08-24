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
from owxr.utils.math import clamp

MIN_FPS = 1
MAX_FPS = 100
STALE_DATA_THRESHOLD_SEC = 0.2


class OpMode(Enum):
    NONE = 1
    STANDALONE = 2
    REMOTE = 3


class Core:
    status = None
    update = None
    isRunning = False
    isRendererReady = False
    renderer_process = None
    socket_process = None
    to_core_pipe_fd = None
    to_renderer_pipe_fd = None

    renderer_full_filepath = ""
    renderer_wd = ""

    heartbeat_timeout_secs = 2
    omx_cmd = "omxplayer -o hdmi udp://127.0.0.1:5000 --timeout 0 --no-keys --fps 60 --aspect-mode stretch --live"
    omx_player_process = None

    def __init__(self, args):
        self.args = args
        self.FPS = self.args.fps or 90
        self.FPS = clamp(self.FPS, MIN_FPS, MAX_FPS)
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

        self.reset_remaining_heartbeat()

    def reset_remaining_heartbeat(self):
        self.heartbeat_remaining = self.heartbeat_timeout_secs

    def on_opmode_change(self, mode: OpMode):
        logging.info(f"OpMode changed: {mode}")
        if mode == OpMode.STANDALONE:
            ProcessHelper.stop_process(self.omx_player_process)
            logging.debug("Stopping omxplayer..")

            self.update = self.standalone_update
            self.start_renderer()

        elif mode == OpMode.REMOTE:
            self.stop_renderer()

            self.update = self.remote_update
            ProcessHelper.stop_process(self.omx_player_process)
            logging.debug("Starting omxplayer..")
            self.omx_player_process = ProcessHelper.start_command_process(self.omx_cmd)

        time.sleep(2)

    def heartbeat(self):
        logging.debug(f"Heartbeat received from renderer.")
        self.reset_remaining_heartbeat()

    def update_fps(self, new_fps):
        if self.args.dynamic_fps:
            new_fps = clamp(int(new_fps), MIN_FPS, MAX_FPS)
            logging.debug(f"Updating FPS to: {new_fps}")
            self.FPS = new_fps

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
            "topic": "Status",
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

    def setup_pipes(self):
        self.to_core_pipe_fd = self.open_pipe("/tmp/to_core")
        self.to_renderer_pipe_fd = self.open_pipe("/tmp/to_renderer")

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

        self.renderer_wd = Path(os.path.normpath("../OpenWiXR-Renderer")).resolve()
        self.renderer_full_filepath = self.renderer_wd.joinpath("openwixr_renderer")
        if not self.renderer_full_filepath.is_file():
            logging.error(
                f"Renderer executable at {self.renderer_full_filepath} does not seem to exist.\nMake sure to build the renderer."
            )
            return

        # endregion

        self.start_renderer()
        self.update = self.standalone_update
        self.sleepTime = float(1 / self.FPS)

        self.isRunning = True

        # Run the raylib loop
        while self.isRunning:
            self.update()
            self.common_update()

            self.sleepTime = float(1 / self.FPS)
            time.sleep(self.sleepTime)
            # self.clock.sleep()

        # Close the pipe
        os.close(self.to_core_pipe_fd)
        os.close(self.to_renderer_pipe_fd)

        ProcessHelper.stop_process(self.renderer_process)
        ProcessHelper.stop_process(self.socket_process)
        ProcessHelper.stop_process(self.omx_player_process)

    def standalone_update(self):
        if self.heartbeat_remaining <= 0 and not self.args.disable_renderer:
            logging.warn(
                f"No heartbeat from the renderer process for the last {self.heartbeat_timeout_secs} seconds."
            )
            logging.warn(f"Restarting renderer..")
            self.start_renderer()

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
                        logging.debug("Received message from Renderer:")
                        message_topic = message["topic"]
                        logging.debug(f"\Topic: {message_topic}")

                        cmd = self.commands_dict.get(message_topic)

                        if cmd:
                            logging.debug(f"\tData: {message.get('data')}")
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
                topic = message["topic"]
                data = message["data"]
                is_stale_data = False
                ts = data.get("time")
                if ts:
                    diff = time.time() - ts
                    is_stale_data = diff > STALE_DATA_THRESHOLD_SEC
                    logging.debug(f"Skipping stale data.. {-diff}")

                if data is not None and self.isRendererReady and not is_stale_data:
                    msg = json.dumps(message) + "\n"
                    encoded_msg = msg.encode()
                    # print(f"Sending:{encoded_msg}")
                    os.write(
                        self.to_renderer_pipe_fd,
                        encoded_msg,
                    )

        if self.isRendererReady:
            self.heartbeat_remaining -= self.sleepTime

    def remote_update(self):
        pass

    def start_renderer(self):
        self.stop_renderer()
        if not self.args.disable_renderer:
            self.setup_pipes()
            self.reset_remaining_heartbeat()
            self.renderer_process = ProcessHelper.start_process(
                self.renderer_full_filepath, self.renderer_wd
            )

    def stop_renderer(self):
        self.isRendererReady = False
        ProcessHelper.stop_process(self.renderer_process)

    def common_update(self):
        if self.socket_process:
            if not self.socket_process.message_queue.empty():
                msg = self.socket_process.message_queue.get()
                topic = msg.get("topic")
                data = msg.get("data")
                if data:
                    self.socket_process.emit(topic, data)
                else:
                    self.socket_process.emit(topic)

        if self.qrScanner.process:
            data = self.qrScanner.get_qr_data()
            if data is not None:
                logging.debug(f"QRData: {data}")
                connect_res, log = self.wifi.connect_to(data["S"], data["P"])
                self.qrScanner.stop_scanning()
                self.out_message_queue.put(self.update_status())

        if self.imu_sensor:
            self.out_message_queue.put(
                {"topic": "Sensor", "data": self.imu_sensor.get_data()}
            )

    def shutdown(self):
        logging.info("Shutting down..")
        time.sleep(0.5)
        self.isRunning = False
