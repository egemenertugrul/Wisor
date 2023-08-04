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
from pyzbar import pyzbar
import numpy as np
from datetime import datetime

os.environ['DISPLAY'] = ":0"

QR_SCAN_DURATION = 10

class QRHelper:
    def __init__(self):
        self.qr_data = mp.Queue()
        self.process = None

    def scan_qr_code(self):
        cap = cv2.VideoCapture(0)

        start_t = datetime.now()
        while True:
            current_t = datetime.now()
            diff_t = (current_t - start_t).total_seconds()
            if diff_t > QR_SCAN_DURATION:
                break

            ret, frame = cap.read()

            if not ret:
                print("Failed to capture frame")
                break

            # detector = cv2.QRCodeDetector()
            # data, points, _ = detector.detectAndDecode(frame)

            # if points is not None:
            #     print(data)

            #     if data:
            #         self.qr_data.put(data)

            #     nrOfPoints = len(points)
            #     for i in range(len(points)):
            #         cv2.line(frame, tuple(points[i][0]), tuple(points[(i+1) % len(points)][0]), color=(255, 0, 255), thickness=2)
                
            #     cv2.putText(frame, data, (int(points[0][0][0]), int(points[0][0][1]) - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)
                
            #     cv2.imshow("QR Code Scanner", frame)
            #     if cv2.waitKey(1) & 0xFF == ord('q'):
            #         break
            #     time.sleep(1)
            for d in pyzbar.decode(frame):
                frame = cv2.rectangle(frame, (d.rect.left, d.rect.top),
                                    (d.rect.left + d.rect.width, d.rect.top + d.rect.height), (255, 0, 0), 2)
                frame = cv2.polylines(frame, [np.array(d.polygon)], True, (0, 255, 0), 2)
                frame = cv2.putText(frame, d.data.decode(), (d.rect.left, d.rect.top + d.rect.height),
                                cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 1, cv2.LINE_AA)
                if d:
                    self.qr_data.put(self.parse_wifi_information(d.data.decode()))

            cv2.imshow("QR Code Scanner", frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
            # time.sleep(1)

        cap.release()
        cv2.destroyAllWindows()

    def start_scanning(self):
        self.stop_scanning()
        time.sleep(1)
        self.process = mp.Process(target=self.scan_qr_code, daemon=True)
        self.process.start()

    def stop_scanning(self):
        if self.process is not None and self.process.is_alive():
            self.process.terminate()
            self.process.join()

        self.process = None

    def get_qr_data(self):
        if not self.qr_data.empty():
            return self.qr_data.get()
        else:
            return None
        
    def parse_wifi_information(self, qr_data):
        fields = qr_data.split(";")

        wifi_data = {}
        for field in fields:
            parts = field.split(":", 1)  # Split into a maximum of two parts
            key = parts[0]
            # print(key)
            if not len(key):
                continue
            value = parts[1] if len(parts) > 1 else ""
            wifi_data[key] = value

        # Extract the relevant information
        wifi_type = wifi_data.get("WIFI")
        if not wifi_type:
            return None
        
        # wifi_ssid = wifi_data.get("S")
        # wifi_password = wifi_data.get("P")
        # wifi_hidden = wifi_data.get("H")

        # print("Wi-Fi Type:", wifi_type)
        # print("Wi-Fi SSID:", wifi_ssid)
        # print("Wi-Fi Password:", wifi_password)
        # print("Wi-Fi Hidden:", wifi_hidden)
        return wifi_data

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

    def get_connected_wifi_ssid(self):
        try:
            # Execute the 'iwgetid' command to get the connected Wi-Fi information
            output = subprocess.check_output(["iwgetid", "-r"])
            ssid = output.decode("utf-8").strip()  # Convert bytes to string and remove trailing newline
            return ssid
        except subprocess.CalledProcessError:
            # Handle the case when the 'iwgetid' command fails or no Wi-Fi connection is available
            return None

        
    def connect_to(self, ssid, password):
        msg = ""
        # try:
        #     subprocess.run(["nmcli", "device", "wifi", "connect", ssid, "password", password], check=True)
        #     log = f"Connected to: {ssid}"
        #     logging.info(log)
        #     return True, log
        # except subprocess.CalledProcessError as e:
        #     logging.error(f"Error connecting to: {ssid}")
        #     logging.error(e)
        #     return False, log
        msg = f"Connected to: {ssid}"
        logging.info(msg)
        return True, msg

imu_sensor = IMU_Sensor()

in_message_queue = Queue()
out_message_queue = Queue()

wh = WifiHelper()
qh = QRHelper()

def get_wifi_state():
    return len(wh.get_wifi_networks()) > 0

def get_wifi_ssid():
    res = wh.get_connected_wifi_ssid()
    return res if res is not None else ""

def get_imu_state():
    return imu_sensor.get_data() != None

def get_camera_state():
    return imu_sensor.get_data() != None

status = None

def update_status():
    global status
    status = {
        "is_wifi_connected": get_wifi_state(),
        "ssid": get_wifi_ssid(),
        "is_imu_connected": get_imu_state(),
        "is_camera_connected": get_camera_state()
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

if __name__ == "__main__":
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

    out_message_queue.put(update_status())
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
                            qh.start_scanning()

                    print("==CORE== Received message from Renderer:")
                    print("\tType:", message["type"])
                    print("\tData:", message["data"])

        if write_ready:
            # Send a message to the renderer
            # message = {
            #     "type": "Greeting",
            #     "data": "Hello, Renderer!"
            # }
            if qh.process is not None:
                data = qh.get_qr_data()
                if data is not None:
                    print(data)
                    connect_res, log = wh.connect_to(data["S"], data["P"])
                    qh.stop_scanning()
                    out_message_queue.put(update_status())

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
