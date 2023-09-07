import subprocess
import logging
from typing import Tuple

class Wifi:
    def __init__(self, interface = "wlan0") -> None:
        self.interface = interface

    def get_wifi_networks(self):
        try:
            output = subprocess.check_output(["iwlist", self.interface, "scan"])
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
        
    def get_ip(self):
        try:
            # Execute the 'ip' command to get the connected Wi-Fi information
            command = ["/sbin/ip", "-o", "-4", "addr", "list", self.interface]
            ip_output = subprocess.check_output(command)
            
            # Now, you can process the output using Python
            ip_lines = ip_output.decode("utf-8").splitlines()
            for line in ip_lines:
                parts = line.split()
                if "inet" in parts:
                    index = parts.index("inet")
                    if index + 1 < len(parts):
                        ip = parts[index + 1].split('/')[0]
                        return ip

            # If no IP address is found, return None
            return None
        except subprocess.CalledProcessError:
            # Handle the case when the 'ip' command fails or no IP address is available
            return None

        
    def connect_to(self, ssid, password) -> Tuple[bool, str]:
        try:
            subprocess.run(["nmcli", "device", "wifi", "connect", ssid, "password", password], check=True)
            log = f"Connected to: {ssid}"
            logging.info(log)
            return True, log
        except subprocess.CalledProcessError as e:
            logging.error(f"Error connecting to: {ssid}")
            logging.error(e)
            return False, ""
