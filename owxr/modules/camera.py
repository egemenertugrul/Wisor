from abc import ABC, abstractmethod
from picamera2 import Picamera2, Preview
import logging


class CaptureSource(ABC):
    @abstractmethod
    def read(self):
        """
        Abstract method for reading frames from the capture source.

        Returns:
            Tuple[bool, any]: A tuple containing a success flag (True if successful, False otherwise)
            and the captured frame or data.
        """
        pass

    @abstractmethod
    def release(self):
        """
        Abstract method for releasing the capture source.
        This method should be implemented by subclasses.
        """
        pass


class PiCamera2CaptureSource(CaptureSource):
    picam2 = None

    def __init__(self):
        self.picam2 = Picamera2()
        config = self.picam2.create_video_configuration()
        # self.picam2.configure(config)
        # config = self.picam2.create_video_configuration(main={"size": (640, 480)})
        self.picam2.configure(config)

        self.picam2.start()

    def read(self):
        try:
            img = self.picam2.capture_image()
        except Exception as e:
            logging.error(e)
        else:
            if img is not None:
                return True, img
        return False, None

    def release(self):
        try:
            self.picam2.stop()
            self.picam2.close()
        except Exception as e:
            logging.error(e)

    def __del__(self):
        self.release()
