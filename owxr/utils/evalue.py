from pyee import EventEmitter


class EValue(EventEmitter):
    def __init__(self, value=None):
        super().__init__()
        self._value = value
        self._callbacks = []

    @property
    def value(self):
        return self._value

    @value.setter
    def value(self, value):
        if value != self._value:
            self._value = value
            self.emit("change", self._value)
