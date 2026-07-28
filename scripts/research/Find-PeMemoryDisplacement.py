"""Find instructions using a selected memory displacement in a PE."""

from __future__ import annotations

import argparse

import capstone
from capstone.x86 import X86_OP_MEM
import pefile


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pe_path")
    parser.add_argument("displacement", type=lambda value: int(value, 0))
    arguments = parser.parse_args()

    pe = pefile.PE(arguments.pe_path, fast_load=True)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    mode = (
        capstone.CS_MODE_64
        if pe.OPTIONAL_HEADER.Magic == 0x20B
        else capstone.CS_MODE_32
    )
    disassembler = capstone.Cs(capstone.CS_ARCH_X86, mode)
    disassembler.detail = True
    disassembler.skipdata = True

    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue

        for instruction in disassembler.disasm(
            section.get_data(),
            image_base + section.VirtualAddress,
        ):
            if instruction.id == 0:
                continue

            if any(
                operand.type == X86_OP_MEM
                and operand.mem.disp == arguments.displacement
                for operand in instruction.operands
            ):
                print(
                    f"0x{instruction.address:X}: "
                    f"{instruction.mnemonic} {instruction.op_str}"
                )


if __name__ == "__main__":
    main()
