"""Read a table of relative or absolute 32-bit PE code addresses."""

from __future__ import annotations

import argparse
import struct

import pefile


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pe_path")
    parser.add_argument("rva", type=lambda value: int(value, 0))
    parser.add_argument("count", type=int)
    parser.add_argument("--first-index", type=int, default=0)
    parser.add_argument(
        "--absolute",
        action="store_true",
        help="Treat entries as absolute 32-bit virtual addresses.",
    )
    arguments = parser.parse_args()

    pe = pefile.PE(arguments.pe_path, fast_load=True)
    data = bytes(pe.__data__)
    file_offset = pe.get_offset_from_rva(arguments.rva)
    image_base = pe.OPTIONAL_HEADER.ImageBase

    for index in range(arguments.count):
        raw_address = struct.unpack_from(
            "<I" if arguments.absolute else "<i",
            data,
            file_offset + index * 4,
        )[0]
        virtual_address = (
            raw_address if arguments.absolute else image_base + raw_address
        )
        print(
            f"{arguments.first_index + index}: "
            f"0x{virtual_address:X}"
        )


if __name__ == "__main__":
    main()
