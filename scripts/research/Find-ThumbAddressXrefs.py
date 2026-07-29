"""Find direct Thumb branch or immediate references to an image address."""

from __future__ import annotations

import argparse

import capstone
from capstone.arm import ARM_OP_IMM


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("image_path")
    parser.add_argument("address", type=lambda value: int(value, 0))
    parser.add_argument("--base", type=lambda value: int(value, 0), required=True)
    parser.add_argument(
        "--wrapper-size",
        type=lambda value: int(value, 0),
        default=0,
    )
    arguments = parser.parse_args()

    image = open(arguments.image_path, "rb").read()[arguments.wrapper_size:]
    target = arguments.address & ~1
    disassembler = capstone.Cs(
        capstone.CS_ARCH_ARM,
        capstone.CS_MODE_THUMB | capstone.CS_MODE_LITTLE_ENDIAN,
    )
    disassembler.detail = True
    disassembler.skipdata = True

    for instruction in disassembler.disasm(image, arguments.base):
        if instruction.id == 0:
            continue

        if any(
            operand.type == ARM_OP_IMM and (operand.imm & ~1) == target
            for operand in instruction.operands
        ):
            print(
                f"0x{instruction.address:08X}: "
                f"{instruction.mnemonic} {instruction.op_str}"
            )


if __name__ == "__main__":
    main()
