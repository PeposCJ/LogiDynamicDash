# RS50 Build G Preflight Capture

## Outcome

- Date: 2026-07-28
- Capture: `2026-07-28_rs50_build_g_shared_hidpp_oneshot.pcapng`
- RS50 USB address: `1.3`
- Executable result: failed closed before stream open
- Operator-observed physical changes: none

Build G rejected COL01/COL03 because their Windows device paths had different
instance segments. Descriptor enumeration occurred, but neither collection was
opened.

## Offline USBPcap Result

The capture contains:

- 10,441 device-to-host reports;
- zero host-to-device reports carrying `usb.data_fragment`, `usb.capdata`, or
  `usbhid.data`;
- no Root discovery request;
- no Layout J setter;
- no `0x8123`, `0x807A`, or `0x807B` operation.

This independently supports the code-path result: the preflight failed before
the first HID write.

## Design Correction

Windows top-level HID collections cannot be proven to belong to the same
physical device by replacing `col01`/`col03` and comparing the remaining device
path. Their instance segments may legitimately differ.

The corrected gate still requires:

- exact VID `0x046D` and PID `0xC276`;
- exactly one `mi_01&col01` and one `mi_01&col03`;
- exactly one expected usage on each collection;
- exact input/output report lengths.

A second attached RS50 would create duplicate qualifying collections, causing
the adapter to reject before opening either stream.
