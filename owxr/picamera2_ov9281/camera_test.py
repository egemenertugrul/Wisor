import time

from picamera2 import Picamera2, Preview
import cv2

picam2 = Picamera2()
#picam2.start_preview(Preview.QTGL)

# preview_config = picam2.create_preview_configuration()
video_config = picam2.create_still_configuration()
picam2.configure(video_config)

picam2.start()
time.sleep(1)

# picam2.set_controls({"AfMode": 1, "LensPosition": 425})

# If your libcamera-dev version is 0.0.10, use the following code.
# AfMode Set the AF mode (manual, auto, continuous)
# For example, single focus: picam2.set_controls({"AfMode": 1 ,"AfTrigger": 0})
#              continuous focus: picam2.set_controls({"AfMode": 2 ,"AfTrigger": 0})

try:
    for frame_number in range(10):  # Capture 10 frames in this example
        # Capture a frame
        image = picam2.capture_image()
        
        # Add some delay between captures if needed
        time.sleep(1)
        
        image.show()
        
finally:
    picam2.stop()
    picam2.close()