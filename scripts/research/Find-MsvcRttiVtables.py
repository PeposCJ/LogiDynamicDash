"""Locate x64 MSVC RTTI complete-object locators and their vtables.

This helper performs static PE parsing only. It is useful for finding virtual
methods of anonymous lambdas and other classes whose names survive in RTTI.
Install the ``pefile`` and ``capstone`` packages before use.
"""

from __future__ import annotations

import argparse
import struct

import capstone
import pefile


def find_all(data: bytes, needle: bytes):
    offset = 0

    while True:
        offset = data.find(needle, offset)

        if offset < 0:
            return

        yield offset
        offset += 1


def executable_section_for(pe: pefile.PE, virtual_address: int):
    rva = virtual_address - pe.OPTIONAL_HEADER.ImageBase

    for section in pe.sections:
        start = section.VirtualAddress
        end = start + max(section.Misc_VirtualSize, section.SizeOfRawData)

        if start <= rva < end and section.Characteristics & 0x20000000:
            return section

    return None


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pe_path")
    parser.add_argument("type_substring")
    parser.add_argument("--method-count", type=int, default=6)
    parser.add_argument("--instruction-count", type=int, default=8)
    arguments = parser.parse_args()

    pe = pefile.PE(arguments.pe_path, fast_load=True)
    data = bytes(pe.__data__)
    image_base = pe.OPTIONAL_HEADER.ImageBase
    needle = arguments.type_substring.encode("ascii")
    disassembler = capstone.Cs(capstone.CS_ARCH_X86, capstone.CS_MODE_64)

    seen_name_offsets: set[int] = set()

    for match_offset in find_all(data, needle):
        name_offset = data.rfind(b"\0", 0, match_offset) + 1
        name_end = data.find(b"\0", match_offset)

        if (
            name_end < 0
            or name_offset in seen_name_offsets
            or not data[name_offset:name_offset + 3].startswith(b".?A")
        ):
            continue

        seen_name_offsets.add(name_offset)

        name = data[name_offset:name_end].decode("ascii", errors="replace")

        # An x64 MSVC TypeDescriptor stores two pointers immediately before
        # its NUL-terminated decorated name.
        descriptor_offset = name_offset - 16

        if descriptor_offset < 0:
            continue

        descriptor_rva = pe.get_rva_from_offset(descriptor_offset)
        print(
            f"type {name}\n"
            f"  descriptor file=0x{descriptor_offset:X} "
            f"rva=0x{descriptor_rva:X}"
        )

        for type_rva_offset in find_all(data, struct.pack("<I", descriptor_rva)):
            locator_offset = type_rva_offset - 12

            if locator_offset < 0 or locator_offset + 24 > len(data):
                continue

            signature, _, _, referenced_type_rva, _, self_rva = (
                struct.unpack_from("<6I", data, locator_offset)
            )

            try:
                locator_rva = pe.get_rva_from_offset(locator_offset)
            except pefile.PEFormatError:
                continue

            if (
                signature != 1
                or referenced_type_rva != descriptor_rva
                or self_rva != locator_rva
            ):
                continue

            locator_va = image_base + locator_rva
            print(
                f"  locator file=0x{locator_offset:X} "
                f"rva=0x{locator_rva:X} va=0x{locator_va:X}"
            )

            for locator_pointer_offset in find_all(
                data,
                struct.pack("<Q", locator_va),
            ):
                vtable_offset = locator_pointer_offset + 8
                vtable_rva = pe.get_rva_from_offset(vtable_offset)
                print(
                    f"    vtable file=0x{vtable_offset:X} "
                    f"rva=0x{vtable_rva:X}"
                )

                for method_index in range(arguments.method_count):
                    pointer_offset = vtable_offset + method_index * 8
                    method_va = struct.unpack_from("<Q", data, pointer_offset)[0]
                    section = executable_section_for(pe, method_va)

                    if section is None:
                        break

                    method_rva = method_va - image_base
                    method_offset = pe.get_offset_from_rva(method_rva)
                    instructions = list(
                        disassembler.disasm(
                            data[
                                method_offset:
                                method_offset + arguments.instruction_count * 16
                            ],
                            method_va,
                            count=arguments.instruction_count,
                        )
                    )
                    preview = "; ".join(
                        f"{instruction.mnemonic} {instruction.op_str}".strip()
                        for instruction in instructions
                    )
                    print(
                        f"      [{method_index}] 0x{method_va:X}: {preview}"
                    )


if __name__ == "__main__":
    main()
