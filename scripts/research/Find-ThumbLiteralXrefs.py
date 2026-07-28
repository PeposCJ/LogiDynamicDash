"""Find Thumb literal loads whose pool word equals a selected value."""

from __future__ import annotations

import argparse
import struct

import capstone
from capstone.arm import ARM_OP_MEM, ARM_REG_PC


def find_all(data: bytes, needle: bytes):
    offset = 0

    while True:
        offset = data.find(needle, offset)

        if offset < 0:
            return

        yield offset
        offset += 1


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("image_path")
    parser.add_argument("value", type=lambda value: int(value, 0))
    parser.add_argument("--base", type=lambda value: int(value, 0), required=True)
    parser.add_argument("--wrapper-size", type=lambda value: int(value, 0), default=0)
    arguments = parser.parse_args()

    data = open(arguments.image_path, "rb").read()
    image = data[arguments.wrapper_size:]
    disassembler = capstone.Cs(
        capstone.CS_ARCH_ARM,
        capstone.CS_MODE_THUMB | capstone.CS_MODE_LITTLE_ENDIAN,
    )
    disassembler.detail = True

    pool_addresses = {
        arguments.base + offset
        for offset in find_all(image, struct.pack("<I", arguments.value))
    }

    for offset in range(0, len(image) - 4, 2):
        address = arguments.base + offset
        instructions = list(disassembler.disasm(image[offset:offset + 4], address, count=1))

        if not instructions:
            continue

        instruction = instructions[0]

        for operand in instruction.operands:
            if operand.type != ARM_OP_MEM or operand.mem.base != ARM_REG_PC:
                continue

            literal_address = ((instruction.address + 4) & ~3) + operand.mem.disp

            if literal_address in pool_addresses:
                print(
                    f"0x{instruction.address:08X} -> 0x{literal_address:08X}: "
                    f"{instruction.mnemonic} {instruction.op_str}"
                )


if __name__ == "__main__":
    main()
