# Static Research Helpers

These scripts reproduce narrow reverse-engineering checks used by the RS50
OLED research. They parse binary files as data and never load a DLL, execute a
firmware image, open HID hardware, or transmit a report.

## Requirements

- Python 3.10 or later
- `pefile` and `capstone` for PE helpers

Keep proprietary Logitech binaries, firmware, captures, and extracted files
outside the repository. Pass their local path explicitly on each invocation.

## PE Helpers

- `Disassemble-PeAddress.py`: disassemble a bounded virtual-address range.
- `Find-MsvcRttiVtables.py`: locate x64 MSVC RTTI vtables by type substring.
- `Find-PeAddressXrefs.py`: find code/data references to one address.
- `Find-PeImmediate.py`: find instructions containing one immediate value.
- `Find-PeLoadCompare.py`: find a field load followed by a comparison, useful
  for locating switch dispatchers in both x86 and x64 images.
- `Find-PeMemoryDisplacement.py`: find memory operands with one displacement.
- `Find-PeMnemonic.py`: find one instruction mnemonic.
- `Find-PeStringXrefs.py`: find direct x64 references to ASCII/UTF-16 strings.
- `Read-PeRelativeJumpTable.py`: read image-relative jump tables; use
  `--absolute` for x86 tables containing full virtual addresses.

Example, with a locally extracted driver:

```powershell
python scripts/research/Find-PeLoadCompare.py DRIVER.dll 4 5
python scripts/research/Read-PeRelativeJumpTable.py DRIVER.dll 0x26789C 6 --absolute
```

## Firmware Helpers

- `Disassemble-ThumbImage.py`: disassemble a bounded Thumb-2 image range.
- `Find-ThumbAddressXrefs.py`: find materialized references to an address.
- `Find-ThumbLiteralXrefs.py`: find literal-pool references to a value.

All reported addresses are static evidence. They do not by themselves
authorize a hardware command or establish runtime behavior.
