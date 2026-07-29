"""Disassemble bytes at a virtual address in a PE without loading it."""

from __future__ import annotations

import argparse

import capstone
import pefile


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pe_path")
    parser.add_argument("address", type=lambda value: int(value, 0))
    parser.add_argument("--count", type=int, default=100)
    parser.add_argument("--skipdata", action="store_true")
    arguments = parser.parse_args()

    pe = pefile.PE(arguments.pe_path, fast_load=True)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    address = arguments.address
    rva = address - image_base if address >= image_base else address
    virtual_address = image_base + rva
    file_offset = pe.get_offset_from_rva(rva)
    data = bytes(pe.__data__)[
        file_offset:file_offset + arguments.count * 16
    ]
    mode = (
        capstone.CS_MODE_64
        if pe.OPTIONAL_HEADER.Magic == 0x20B
        else capstone.CS_MODE_32
    )
    disassembler = capstone.Cs(capstone.CS_ARCH_X86, mode)
    disassembler.skipdata = arguments.skipdata

    for instruction in disassembler.disasm(
        data,
        virtual_address,
        count=arguments.count,
    ):
        print(
            f"0x{instruction.address:X}: "
            f"{instruction.mnemonic:<8} {instruction.op_str}"
        )


if __name__ == "__main__":
    main()
