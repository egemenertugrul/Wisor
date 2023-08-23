from time import sleep, perf_counter


class Clock:
    def __init__(self, fps):
        self.start = perf_counter()
        self.frame_length = 1 / fps

    @property
    def tick(self):
        return int((perf_counter() - self.start) / self.frame_length)

    def sleep(self):
        r = self.tick + 1
        while self.tick < r:
            sleep(1 / 1000)
