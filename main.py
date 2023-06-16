from queue import Queue
import subprocess
import os
import select
import json
import time
from pathlib import Path
import logging

import cv2
from sensor import IMU_Sensor
import multiprocessing as mp

os.environ['DISPLAY'] = ":0"

class QRHelper:
    def __init__(self):
        self.qr_data = None
        self.process = None

    def scan_qr_code(self):
        cap = cv2.VideoCapture(0)

        while True:
            ret, frame = cap.read()

            if not ret:
                print("Failed to capture frame")
                break

            detector = cv2.QRCodeDetector()
            data, bbox, _ = detector.detectAndDecodeMulti(frame)

            if bbox is not None:
                self.qr_data = data

            cv2.imshow("QR Code Scanner", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

        cap.release()
        cv2.destroyAllWindows()

    def start_scanning(self):
        self.process = mp.Process(target=self.scan_qr_code)
        self.process.start()

    def stop_scanning(self):
        if self.process is not None and self.process.is_alive():
            self.process.terminate()
            self.process.join()

        self.process = None

    def get_qr_data(self):
        return self.qr_data

class WifiHelper:
    def __init__(self) -> None:
        pass

    def get_wifi_networks(self):
        try:
            output = subprocess.check_output(["iwlist", "wlan0", "scan"])
            output = output.decode("utf-8")
            
            ssids = []
            lines = output.split("\n")
            
            for line in lines:
                line = line.strip()
                if line.startswith("ESSID:"):
                    ssid = line.split(":")[1].strip().strip('"')
                    ssids.append(ssid)
            return ssids
        except subprocess.CalledProcessError as e:
            logging.error("Error:", e)
            return []
        except Exception as e:
            logging.error(e)

        
    def connect_to(self, ssid, password):
        try:
            subprocess.run(["nmcli", "device", "wifi", "connect", ssid, "password", password], check=True)
            logging.log(f"Connected to: {ssid}")
        except subprocess.CalledProcessError as e:
            logging.error(f"Error connecting to: {ssid}")
            logging.error(e)

imu_sensor = IMU_Sensor()

in_message_queue = Queue()
out_message_queue = Queue()

wh = WifiHelper()

def get_wifi_state():
    return len(wh.get_wifi_networks()) > 0

def get_imu_state():
    return imu_sensor.get_data() != None

def get_camera_state():
    return imu_sensor.get_data() != None

if __name__ == "__main__":

    status = {
        "is_wifi_connected": get_wifi_state(),
        "is_imu_connected": get_imu_state(),
        "is_camera_connected": get_camera_state()
    }

    # region Setup pipes and renderer
    # Start the renderer program as a child process
    renderer_path = Path(os.path.normpath("../OpenWiXR-Renderer/")).resolve()
    renderer_path_bin = renderer_path.joinpath("_bin/Debug/")
    renderer_process = subprocess.Popen(os.path.join(renderer_path_bin, "OpenWiXR-Renderer"), cwd=renderer_path)

    to_core_pipe_path = "/tmp/to_core"
    if os.path.exists(to_core_pipe_path):
        os.remove(to_core_pipe_path)
    os.mkfifo(to_core_pipe_path)
    to_core_pipe_fd = os.open(to_core_pipe_path, os.O_RDWR | os.O_NONBLOCK)

    to_renderer_pipe_path = "/tmp/to_renderer"
    if os.path.exists(to_renderer_pipe_path):
        os.remove(to_renderer_pipe_path)
    os.mkfifo(to_renderer_pipe_path)
    to_renderer_pipe_fd = os.open(to_renderer_pipe_path, os.O_RDWR | os.O_NONBLOCK)
    # endregion

    out_message_queue.put({
        "type": "Status",
        "data": {
            "wifi": status["is_wifi_connected"],
            "imu": status["is_imu_connected"],
            "camera": status["is_camera_connected"],
        }
    })
    # time.sleep(1)

    # Run the raylib loop
    while True:
        # Check if the pipe is ready for reading or writing
        read_ready, write_ready, _ = select.select([to_core_pipe_fd], [to_renderer_pipe_fd], [], 0)

        if read_ready:
            # Read the message from the renderer
            message = os.read(to_core_pipe_fd, 1024)
            messages = list(filter(lambda s: len(s) > 0, message.decode().split("\0")))
            for m in messages:
                if m:
                    # Process the received message
                    try:
                        message = json.loads(m)
                    except Exception as e:
                        print(e)
                        continue
                    
                    if message["type"] == "Command":
                        if message["data"] == "QRScan":
                            QRHelper.start_scanning()

                    print("==CORE== Received message from Renderer:")
                    print("\tType:", message["type"])
                    print("\tData:", message["data"])

        if write_ready:
            # Send a message to the renderer
            # message = {
            #     "type": "Greeting",
            #     "data": "Hello, Renderer!"
            # }
            if not out_message_queue.empty():
                message = out_message_queue.get(block=False)
                print(f"Sending: {message}")
                os.write(to_renderer_pipe_fd, json.dumps(message).encode())

        # data = imu_sensor.get_data()
        # message = {
        #     "type": "Sensor",
        #     "data": data
        # }
        # out_message_queue.put(message)

        time.sleep(float(1/60))
        # Add any necessary synchronization mechanisms if needed

    # Close the pipe
    os.close(to_renderer_pipe_fd)

    # Wait for the renderer process to finish
    renderer_process.wait()
