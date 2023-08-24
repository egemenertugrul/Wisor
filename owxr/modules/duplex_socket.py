import asyncio
import multiprocessing as mp
import logging
from websockets.server import serve
from websockets.exceptions import ConnectionClosedOK, ConnectionClosedError
import json
import time
from pyee import EventEmitter


class DuplexWebsocketsServerProcess(mp.Process, EventEmitter):
    def __init__(self, *args, **kwargs):
        mp.Process.__init__(self, *args, **kwargs)  # Call the constructor of mp.Process
        EventEmitter.__init__(self)
        self._send_queue = mp.Queue()
        self.message_queue = mp.Queue()

    def run(self):
        asyncio.run(self.main())

    def send(self, item):
        self._send_queue.put(item)

    async def consumer_handler(self, websocket):
        async for message in websocket:
            msg = json.loads(message)
            self.message_queue.put(msg)

    async def producer_handler(self, websocket):
        while True:
            message = await asyncio.get_event_loop().run_in_executor(
                None, self._send_queue.get
            )
            logging.debug(f"Sending: {message}")
            try:
                await websocket.send(message)
            except ConnectionClosedOK:
                self.on_disconnect()
                break

    def on_connect(self):
        self.message_queue.put({"topic": "Connect"})
        logging.info("Connection established..")

    def on_disconnect(self):
        self.message_queue.put({"topic": "Disconnect"})
        logging.info("Connection closed..")

    async def handler(self, websocket):
        self.on_connect()

        closed = asyncio.ensure_future(websocket.wait_closed())
        closed.add_done_callback(lambda task: self.on_disconnect())
        await asyncio.gather(
            self.consumer_handler(websocket), self.producer_handler(websocket)
        )

    async def main(self):
        self._loop = asyncio.get_event_loop()
        async with serve(
            self.handler, "0.0.0.0", 8765, ping_interval=1, ping_timeout=1
        ) as server:
            await asyncio.Future()


if __name__ == "__main__":
    socket_process = DuplexWebsocketsServerProcess(daemon=True)
    socket_process.start()

    time.sleep(2)
    for i in range(10):
        socket_process.send(f"Message from server-{i}")
        print(f"--\n")
        time.sleep(4)

    socket_process.join()
