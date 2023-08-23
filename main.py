from owxr.core import Core
import argparse

if __name__ == "__main__":
    parser = argparse.ArgumentParser(prog="OpenWiXR", description="OpenWirelessXR-Core")
    parser.add_argument("--fps", type=int, default=90)
    parser.add_argument("--disable-renderer", action="store_true")
    parser.add_argument("-d", "--display", type=str, default=":0")
    parser.add_argument("-v", "--verbose", action="store_true")

    args = parser.parse_args()
    if args.verbose:
        print("Arguments: ")
        for key, value in vars(args).items():
            print(f"\t{key}: {value}")

    core = Core(args=args)
    core.start()
