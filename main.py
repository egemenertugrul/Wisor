from owxr.core import Core
import argparse
import logging
import atexit

from owxr.modules.duplex_socket import DuplexWebsocketsServerProcess

if __name__ == "__main__":
    parser = argparse.ArgumentParser(prog="OpenWiXR", description="OpenWirelessXR")
    group = parser.add_mutually_exclusive_group()
    group.add_argument("--fps", type=int, default=90, help="Set a fixed FPS")
    group.add_argument("--dynamic-fps", action="store_true", help="Use dynamic FPS")

    parser.add_argument("--disable-renderer", action="store_true")
    parser.add_argument("-d", "--display", type=str, default=":0")
    parser.add_argument("--verbose", "-v", action="count", default=1)

    args = parser.parse_args()

    args.verbose = 40 - (10 * args.verbose) if args.verbose > 0 else 0
    print(f"Verbose level: {logging.getLevelName(args.verbose)}")
    logging.basicConfig(
        level=args.verbose,
        format="%(asctime)s %(levelname)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )

    core = Core(args=args)
    atexit.register(core.shutdown)
    core.start()