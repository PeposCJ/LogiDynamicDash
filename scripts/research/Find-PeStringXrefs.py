"""Find direct x64 RIP-relative references to a string in a PE file.

This is an offline research helper. It reads an executable but never loads or
executes it. Install the `pefile` and `capstone` Python packages before use.
"""

from __future__ import annotations

import argparse
from collections.abc import Iterable
import struct

import capstone
from capstone.x86 import X86_OP_MEM, X86_REG_RIP
import pefile


def find_all(data: bytes, needle: bytes) -> Iterable[int]:
    offset = 0

    while True:
        offset = data.find(needle, offset)

        if offset < 0:
            return

        yield offset
        offset += 1


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pe_path")
    parser.add_argument("text")
    arguments = parser.parse_args()

    pe = pefile.PE(arguments.pe_path, fast_load=True)
    data = bytes(pe.__data__)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    targets: dict[int, str] = {}

    encodings = {
        "ASCII": arguments.text.encode("ascii") + b"\0",
        "UTF-16LE": arguments.text.encode("utf-16le") + b"\0\0",
    }

    for encoding_name, encoded_text in encodings.items():
        for file_offset in find_all(data, encoded_text):
            rva = pe.get_rva_from_offset(file_offset)
            target = image_base + rva
            targets[target] = encoding_name
            print(
                f"string {encoding_name}: file=0x{file_offset:X} "
                f"rva=0x{rva:X} va=0x{target:X}"
            )

    if not targets:
        print("string not found")
        return

    # MSVC binaries commonly keep a pointer to a string in a descriptor or
    # message table and reference that slot from code. Include one level of
    # indirection so those call sites are visible without loading the PE.
    indirect_targets: dict[int, str] = {}

    for target, encoding_name in list(targets.items()):
        encoded_pointer = struct.pack("<Q", target)

        for file_offset in find_all(data, encoded_pointer):
            rva = pe.get_rva_from_offset(file_offset)
            pointer_address = image_base + rva
            description = f"pointer to {encoding_name} string"
            indirect_targets[pointer_address] = description
            print(
                f"pointer {encoding_name}: file=0x{file_offset:X} "
                f"rva=0x{rva:X} va=0x{pointer_address:X}"
            )

    targets.update(indirect_targets)

    disassembler = capstone.Cs(
        capstone.CS_ARCH_X86,
        capstone.CS_MODE_64,
    )
    disassembler.detail = True
    disassembler.skipdata = True

    for section in pe.sections:
        if not (section.Characteristics & 0x20000000):
            continue

        section_data = section.get_data()
        section_address = image_base + section.VirtualAddress

        for instruction in disassembler.disasm(
            section_data,
            section_address,
        ):
            if instruction.id == 0:
                continue

            for operand in instruction.operands:
                if (
                    operand.type != X86_OP_MEM
                    or operand.mem.base != X86_REG_RIP
                ):
                    continue

                referenced_address = (
                    instruction.address
                    + instruction.size
                    + operand.mem.disp
                )

                if referenced_address not in targets:
                    continue

                print(
                    f"xref {targets[referenced_address]}: "
                    f"0x{instruction.address:X} "
                    f"{instruction.mnemonic} {instruction.op_str}"
                )


if __name__ == "__main__":
    main()
