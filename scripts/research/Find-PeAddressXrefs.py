"""Find direct x86/x64 references to a PE virtual address."""

from __future__ import annotations

import argparse
import struct

import capstone
from capstone.x86 import X86_OP_IMM, X86_OP_MEM, X86_REG_RIP
import pefile


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pe_path")
    parser.add_argument("address", type=lambda value: int(value, 0))
    parser.add_argument("--data-pointers", action="store_true")
    arguments = parser.parse_args()

    pe = pefile.PE(arguments.pe_path, fast_load=True)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    target = (
        arguments.address
        if arguments.address >= image_base
        else image_base + arguments.address
    )
    mode = (
        capstone.CS_MODE_64
        if pe.OPTIONAL_HEADER.Magic == 0x20B
        else capstone.CS_MODE_32
    )
    disassembler = capstone.Cs(capstone.CS_ARCH_X86, mode)
    disassembler.detail = True
    disassembler.skipdata = True

    if arguments.data_pointers:
        pointer_size = 8 if mode == capstone.CS_MODE_64 else 4
        pointer = struct.pack("<Q" if pointer_size == 8 else "<I", target)

        for section in pe.sections:
            if section.Characteristics & 0x20000000:
                continue

            section_data = section.get_data()
            offset = section_data.find(pointer)

            while offset >= 0:
                print(
                    f"data pointer 0x{image_base + section.VirtualAddress + offset:X}"
                )
                offset = section_data.find(pointer, offset + pointer_size)

    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue

        section_data = section.get_data()
        section_address = image_base + section.VirtualAddress

        for instruction in disassembler.disasm(section_data, section_address):
            if instruction.id == 0:
                continue

            matched = False

            for operand in instruction.operands:
                if operand.type == X86_OP_IMM and operand.imm == target:
                    matched = True
                elif operand.type == X86_OP_MEM:
                    referenced_address = operand.mem.disp

                    if operand.mem.base == X86_REG_RIP:
                        referenced_address += (
                            instruction.address + instruction.size
                        )

                    if referenced_address == target:
                        matched = True

            if matched:
                print(
                    f"0x{instruction.address:X}: "
                    f"{instruction.mnemonic} {instruction.op_str}"
                )


if __name__ == "__main__":
    main()
