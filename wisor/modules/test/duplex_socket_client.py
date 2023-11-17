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
            await asyncio.sleep(0.5)
            await websocket.send(
                json.dumps(
                    {
                        "topic": "SetIMUTopics",
                        "data": ["orientation"],
                    }
                )
            )
            while True:
                for message in websocket:
                    print(f"Received: {message}")


if __name__ == "__main__":
    websocketsClient = TestWebsocketsClient()
