import asyncio
import websockets
import json
import time


class TestWebsocketsClient:
    def __init__(self):
        asyncio.run(self.main())

    async def main(self):
        async with websockets.connect("ws://127.0.0.1:8765") as websocket:
            # await websocket.send(json.dumps({"topic": "Connect"}))
            while True:
                await asyncio.sleep(0.5)
                await websocket.send(
                    json.dumps(
                        {
                            "topic": "SetIMUTopics",
                            "data": ["acceleration", "gyroscope", "magnetometer"],
                        }
                    )
                )


if __name__ == "__main__":
    websocketsClient = TestWebsocketsClient()
