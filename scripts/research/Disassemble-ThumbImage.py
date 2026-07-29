"""Disassemble Thumb code from a raw image with an optional file wrapper."""

from __future__ import annotations

import argparse

import capstone


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("image_path")
    parser.add_argument("address", type=lambda value: int(value, 0))
    parser.add_argument("--base", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--wrapper-size", type=lambda value: int(value, 0), default=0)
    parser.add_argument("--count", type=int, default=100)
    parser.add_argument("--skipdata", action="store_true")
    arguments = parser.parse_args()

    address = arguments.address & ~1
    file_offset = arguments.wrapper_size + address - arguments.base
    data = open(arguments.image_path, "rb").read()
    code = data[file_offset:file_offset + arguments.count * 4]
    disassembler = capstone.Cs(
        capstone.CS_ARCH_ARM,
        capstone.CS_MODE_THUMB | capstone.CS_MODE_LITTLE_ENDIAN,
    )
    disassembler.skipdata = arguments.skipdata

    for instruction in disassembler.disasm(code, address, count=arguments.count):
        print(
            f"0x{instruction.address:08X}: "
            f"{instruction.mnemonic:<8} {instruction.op_str}"
        )


if __name__ == "__main__":
    main()
