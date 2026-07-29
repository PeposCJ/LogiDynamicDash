"""Find selected x86/x64 instruction mnemonics in a PE file."""

from __future__ import annotations

import argparse

import capstone
import pefile


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pe_path")
    parser.add_argument("mnemonics", nargs="+")
    arguments = parser.parse_args()

    requested = {mnemonic.lower() for mnemonic in arguments.mnemonics}
    pe = pefile.PE(arguments.pe_path, fast_load=True)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    mode = (
        capstone.CS_MODE_64
        if pe.OPTIONAL_HEADER.Magic == 0x20B
        else capstone.CS_MODE_32
    )
    disassembler = capstone.Cs(capstone.CS_ARCH_X86, mode)
    disassembler.skipdata = True

    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue

        for instruction in disassembler.disasm(
            section.get_data(),
            image_base + section.VirtualAddress,
        ):
            if instruction.id != 0 and instruction.mnemonic in requested:
                print(
                    f"0x{instruction.address:X}: "
                    f"{instruction.mnemonic} {instruction.op_str}"
                )


if __name__ == "__main__":
    main()
