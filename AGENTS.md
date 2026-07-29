# Codex Project Instructions

## Legal, IP, Reverse-Engineering, and Repository Safety Rules

This project is an independent interoperability project for Logitech racing hardware. Treat legal/IP hygiene as a first-class engineering constraint.

### Primary objective

Implement functionality using independently written code and experimentally verified protocol behavior.

Prefer documented or physically observed device behavior over copying vendor implementations.

### Logitech proprietary material

Do NOT commit, redistribute, embed, or reproduce proprietary Logitech material, including:

- G HUB binaries or DLLs
- Logitech firmware or DFU files
- proprietary SDK binaries
- extracted firmware assets
- fonts or glyph bitmap data extracted from firmware
- graphics or icons extracted from Logitech software/firmware
- large disassembly or decompilation listings
- substantial reconstructed vendor source code
- raw proprietary resources
- complete USB captures that may contain unrelated or sensitive traffic

Local proprietary files may be inspected for interoperability research when necessary, but they must remain outside the repository.

Document findings as independently written technical descriptions, protocol structures, behavioral observations, hashes, addresses when useful for reproducibility, and small factual byte sequences when needed to explain interoperability.

Do not turn decompiled vendor implementation into source code by transcription.

### Clean implementation principle

When implementing a discovered protocol:

1. derive the public implementation from documented protocol behavior;
2. write new source code from scratch;
3. expose only the minimum functionality required by LogiDynamicDash;
4. do not copy vendor class structures, naming, algorithms, or implementation details unless they are necessary functional interface facts;
5. separate evidence/research from production implementation.

Facts such as feature IDs, function IDs, packet formats, field lengths, layout IDs, acknowledgement behavior, USB interfaces, timing constraints, and observed device behavior may be documented as interoperability information.

### RS50 OLED protocol

The currently confirmed interoperability target is public HID++ feature:

`0x8130 — Display Game Data`

Do not assume a runtime feature index. Always discover `0x8130` through the HID++ Root feature before using it.

The confirmed protocol currently includes typed firmware-rendered layouts rather than a host framebuffer.

Do not claim arbitrary pixel/framebuffer control unless independently demonstrated.

Do not probe undocumented or unknown HID++ functions by guessing.

Do not fuzz the RS50.

Do not perform firmware writes, firmware modification, bootloader operations, arbitrary HID writes, or speculative commands.

### Hardware safety

Hardware experiments must remain narrowly scoped and fail closed.

Do not introduce a generic raw HID command interface.

Do not allow callers to choose arbitrary:

- feature IDs
- runtime indices
- HID++ functions
- device indices
- report IDs
- USB interfaces
- raw payload bytes

Use typed operations with strict validation.

For OLED communication, avoid operations that interfere with Force Feedback, TRUEFORCE, LEDs, profiles, or firmware.

Feature `0x8123` Force Feedback must not be invoked as part of OLED transport.

The previous exclusive DirectInput path demonstrated destructive FFB side effects and must not be treated as the production OLED transport.

Prefer the validated shared HID++ path over DirectInput acquisition.

Physical testing must progress in bounded stages and must not move to driving or full-lap testing until the previous coexistence stage has passed.

### Research evidence

Preserve enough evidence to reproduce conclusions without publishing proprietary source material.

Good repository evidence includes:

- sanitized protocol descriptions
- USB request/response summaries
- hashes of locally inspected files
- independently written diagrams
- experimental procedures
- physical observations
- decoded protocol fields
- timings
- test results

Keep raw PCAP/PCAPNG, firmware, DLLs, extracted assets, and large binary dumps local and ignored by Git.

### Third-party open-source projects

Technical findings from third-party projects may be used as references.

Do not copy or adapt third-party source code without checking its license first.

In particular, code from `mescon/logitech-trueforce-linux-driver` may be GPL/LGPL licensed depending on the component.

Distinguish clearly between:

- learning a protocol fact from another project; and
- copying or adapting that project's implementation.

If code is adapted, identify the source and license before committing it.

### Trademark / affiliation

Treat Logitech, Logitech G, G HUB, RS50, PRO Racing Wheel, TRUEFORCE, and related names as third-party trademarks.

Do not imply that LogiDynamicDash is an official Logitech product.

Project-facing documentation should describe the project as independent and unofficial when appropriate.

Do not use Logitech logos or proprietary visual assets in the project without explicit permission.

### Documentation language

Repository code, comments, commits, README content, technical documentation, issues, pull requests, and other public-facing project material should be written in English.

Explain technical work to me conversationally in Spanish.

Use first-person singular ("I") rather than "we" when drafting messages or statements on my behalf.

### Claims

Separate these evidence levels:

- Confirmed: physically or independently verified.
- Likely: supported by multiple observations but not fully verified.
- Unknown: insufficient evidence.

Do not upgrade an inference into a confirmed fact.

Do not claim that reverse engineering establishes ownership of Logitech technology.

The goal is interoperability with independently written software.

### Before committing

Before adding research-derived material to Git, check that the change does not include:

- vendor binaries
- firmware
- extracted proprietary assets
- decompiled source
- copyrighted font or bitmap data
- raw USB captures
- secrets or personal identifiers
- third-party source copied without license compliance

When uncertain whether material is appropriate to publish, stop and flag the issue instead of committing it.

### Design preference

Whenever two implementations are technically equivalent, prefer the one that:

1. uses independently written code;
2. relies on standard OS/HID interfaces;
3. uses the smallest verified protocol surface;
4. minimizes dependency on Logitech proprietary software;
5. avoids redistributing vendor material;
6. provides clear attribution for external research;
7. is easiest to audit for hardware safety and interoperability.

These constraints apply to all future RS50, OLED, HID++, G HUB, firmware, DirectInput, TRUEFORCE, and protocol-research work in this repository.

Do not "clean up" or reimplement code by translating decompiled Logitech logic line-by-line. First reduce any reverse-engineering finding to a functional interoperability specification, then implement independently from that specification.
