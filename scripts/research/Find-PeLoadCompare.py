"""Find PE code that loads a field and soon compares it with an immediate.

This static-analysis helper is intended for locating switch dispatchers such as
``mov eax, [object + 4]`` followed by ``cmp eax, 5``. It never loads or invokes
the inspected binary. Install the ``pefile`` and ``capstone`` packages first.
"""

from __future__ import annotations

import argparse

import capstone
import pefile
from capstone.x86 import X86_OP_IMM, X86_OP_MEM, X86_OP_REG


def canonical_register(disassembler: capstone.Cs, register: int) -> str:
    """Return a stable name for common x86 register-width aliases."""
    name = disassembler.reg_name(register)

    legacy_aliases = {
        "rax": "a", "eax": "a", "ax": "a", "al": "a", "ah": "a",
        "rbx": "b", "ebx": "b", "bx": "b", "bl": "b", "bh": "b",
        "rcx": "c", "ecx": "c", "cx": "c", "cl": "c", "ch": "c",
        "rdx": "d", "edx": "d", "dx": "d", "dl": "d", "dh": "d",
        "rsi": "si", "esi": "si", "si": "si", "sil": "si",
        "rdi": "di", "edi": "di", "di": "di", "dil": "di",
        "rbp": "bp", "ebp": "bp", "bp": "bp", "bpl": "bp",
        "rsp": "sp", "esp": "sp", "sp": "sp", "spl": "sp",
    }

    if name in legacy_aliases:
        return legacy_aliases[name]

    if name.startswith("r") and name[-1:] in {"b", "d", "w"}:
        return name[:-1]

    return name


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("pe_path")
    parser.add_argument("displacement", type=lambda value: int(value, 0))
    parser.add_argument("immediate", type=lambda value: int(value, 0))
    parser.add_argument("--lookahead", type=int, default=8)
    parser.add_argument("--context", type=int, default=4)
    arguments = parser.parse_args()

    pe = pefile.PE(arguments.pe_path, fast_load=True)
    mode = (
        capstone.CS_MODE_64
        if pe.OPTIONAL_HEADER.Magic == 0x20B
        else capstone.CS_MODE_32
    )
    disassembler = capstone.Cs(capstone.CS_ARCH_X86, mode)
    disassembler.detail = True
    disassembler.skipdata = True
    image_base = pe.OPTIONAL_HEADER.ImageBase

    for section in pe.sections:
        if not section.Characteristics & 0x20000000:
            continue

        code = section.get_data()
        section_va = image_base + section.VirtualAddress
        instructions = [
            instruction
            for instruction in disassembler.disasm(code, section_va)
            if instruction.id != 0
        ]

        for index, instruction in enumerate(instructions):
            if instruction.mnemonic not in {"mov", "movzx", "movsxd"}:
                continue

            operands = instruction.operands

            if (
                len(operands) != 2
                or operands[0].type != X86_OP_REG
                or operands[1].type != X86_OP_MEM
                or operands[1].mem.disp != arguments.displacement
            ):
                continue

            loaded_register = operands[0].reg
            loaded_family = canonical_register(disassembler, loaded_register)
            match_index = None

            for candidate_index in range(
                index + 1,
                min(index + 1 + arguments.lookahead, len(instructions)),
            ):
                candidate = instructions[candidate_index]
                candidate_operands = candidate.operands

                if (
                    candidate.mnemonic == "cmp"
                    and len(candidate_operands) == 2
                    and candidate_operands[0].type == X86_OP_REG
                    and canonical_register(
                        disassembler,
                        candidate_operands[0].reg,
                    ) == loaded_family
                    and candidate_operands[1].type == X86_OP_IMM
                    and candidate_operands[1].imm == arguments.immediate
                ):
                    match_index = candidate_index
                    break

                written_registers = candidate.regs_access()[1]

                if any(
                    canonical_register(disassembler, written) == loaded_family
                    for written in written_registers
                ):
                    break

            if match_index is None:
                continue

            start = max(index - arguments.context, 0)
            end = min(match_index + arguments.context + 1, len(instructions))
            print(f"match 0x{instruction.address:X}")

            for preview_index in range(start, end):
                preview = instructions[preview_index]
                marker = ">" if index <= preview_index <= match_index else " "
                rendered = f"{preview.mnemonic} {preview.op_str}".rstrip()
                print(f" {marker} 0x{preview.address:X}: {rendered}")


if __name__ == "__main__":
    main()
