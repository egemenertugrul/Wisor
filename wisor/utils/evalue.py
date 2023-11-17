from pyee import EventEmitter
from enum import Enum


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

    def __eq__(self, other):
        if isinstance(other, type(self._value)):
            return self._value == other
        return False
