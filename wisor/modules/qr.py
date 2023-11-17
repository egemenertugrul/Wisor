# import cv2
from pyzbar import pyzbar
from datetime import datetime
import multiprocessing as mp
from threading import Thread
from queue import Queue

import time
import logging

import cv2
import numpy as np

class QRScanner:
    isRunning = False
    def __init__(self, capture_src = None, duration:int = None, preview:bool=False):
        self.qr_data = Queue()
        self.thread = None

        self.capture_src = capture_src
        if duration:
            self.qr_scan_duration = duration or 10
        self.preview = preview


    def scan_qr_code(self):
        if not self.capture_src:
            logging.error("Capture source is not defined.")

        cap = self.capture_src
        self.isRunning = True
        start_t = datetime.now()
        while self.isRunning:
            current_t = datetime.now()
            diff_t = (current_t - start_t).total_seconds()
            if diff_t > self.qr_scan_duration:
                self.isRunning = False

            ret, frame = cap.read()

            if not ret:
                logging.error("Failed to capture frame")
                break
            frame = np.array(frame)
            # frame = cv.undistort(frame, mtx, dist, None, newcameramtx)

            for d in pyzbar.decode(frame):
                if self.preview:
                    frame = cv2.rectangle(frame, (d.rect.left, d.rect.top),
                                        (d.rect.left + d.rect.width, d.rect.top + d.rect.height), (255, 0, 0), 2)
                    frame = cv2.polylines(frame, [np.array(d.polygon)], True, (0, 255, 0), 2)
                    frame = cv2.putText(frame, d.data.decode(), (d.rect.left, d.rect.top + d.rect.height),
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 1, cv2.LINE_AA)
                if d:
                    self.qr_data.put(self.parse_wifi_information(d.data.decode()))

            if self.preview:
                cv2.imshow("QR Code Scanner", frame)
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break
        if self.preview:
            cap.release()
            cv2.destroyAllWindows()

        logging.info("QRScanner thread ended..")

    def start_scanning(self):
        self.stop_scanning()
        time.sleep(1)
        logging.info("Starting QRScanner thread..")
        # self.process = mp.Process(target=self.scan_qr_code, daemon=False)
        self.thread = Thread(target=self.scan_qr_code) # [BUG] picamera2 doesn't work as a process. #424 https://github.com/raspberrypi/picamera2/issues/424
        self.thread.start()

    def stop_scanning(self):
        if self.thread is not None and self.thread.is_alive():
            logging.info("Stopping QRScanner thread..")
            self.isRunning = False
            self.thread.join()

        self.thread = None

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

        # logging.info(f"Wi-Fi Type: {wifi_type}")
        # logging.info(f"Wi-Fi SSID: {wifi_ssid}")
        # logging.info(f"Wi-Fi Password: {wifi_password}")
        # logging.info(f"Wi-Fi Hidden: {wifi_hidden}")
        return wifi_data

if __name__ == "__main__":
    import os
    os.environ["DISPLAY"] = ":0"
    import sys
    sys.path.append('.')
    from camera import PiCamera2CaptureSource

    qr = QRScanner(capture_src=PiCamera2CaptureSource(), duration=10, preview=True)
    # qr.scan_qr_code()
    qr.start_scanning()
    # qr.process.join()
    pass