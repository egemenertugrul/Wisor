import cv2
from pyzbar import pyzbar
from datetime import datetime
import multiprocessing as mp
import time

class QRScanner:
    qr_scan_duration = 10

    def __init__(self, duration = None):
        self.qr_data = mp.Queue()
        self.process = None
        if duration:
            self.qr_scan_duration = duration

    def scan_qr_code(self):
        cap = cv2.VideoCapture(0)

        start_t = datetime.now()
        while True:
            current_t = datetime.now()
            diff_t = (current_t - start_t).total_seconds()
            if diff_t > self.qr_scan_duration:
                break

            ret, frame = cap.read()

            if not ret:
                print("Failed to capture frame")
                break

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
