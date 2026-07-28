#include "rs50_display_bridge.h"

#include <cstdint>
#include <iostream>

namespace
{
int failures = 0;

void Expect(bool condition, const wchar_t *description)
{
    if (!condition)
    {
        std::wcerr << L"FAILED: " << description << L"\n";
        ++failures;
    }
}
} // namespace

int wmain()
{
    Expect(rs50_display_abi_version() == 5, L"ABI version is five");

    Expect(rs50_display_open(0, nullptr) == RS50_DISPLAY_INVALID_ARGUMENT,
           L"open rejects a null output pointer before any hardware work");

    rs50_display_handle *handle =
        reinterpret_cast<rs50_display_handle *>(static_cast<std::uintptr_t>(1));
    Expect(rs50_display_open(0, &handle) == RS50_DISPLAY_INVALID_OWNER_WINDOW,
           L"open rejects a null owner window before driver or device access");
    Expect(handle == nullptr, L"failed open clears the output handle");

    rs50_display_query_result result{};
    result.struct_size = sizeof(result);
    Expect(rs50_display_query_support(nullptr, &result) ==
               RS50_DISPLAY_INVALID_ARGUMENT,
           L"query rejects a null handle");
    Expect(rs50_display_query_layout_j_support(nullptr, &result) ==
               RS50_DISPLAY_INVALID_ARGUMENT,
           L"Layout J query rejects a null handle");

    rs50_display_static_layout_j_result setterResult{};
    setterResult.struct_size = sizeof(setterResult);
    Expect(rs50_display_set_static_layout_j(nullptr, &setterResult) ==
               RS50_DISPLAY_INVALID_ARGUMENT,
           L"static Layout J setter rejects a null handle");
    Expect(rs50_display_set_static_layout_j(nullptr, nullptr) ==
               RS50_DISPLAY_INVALID_ARGUMENT,
           L"static Layout J setter rejects null arguments");

    Expect(rs50_display_status_message(RS50_DISPLAY_QUERY_ALREADY_ATTEMPTED) !=
               nullptr,
           L"every public failure exposes a stable message");
    Expect(rs50_display_status_message(RS50_DISPLAY_QUERY_OUTPUT_INVALID) !=
               nullptr,
           L"unexpected query output has a stable failure message");
    Expect(rs50_display_status_message(RS50_DISPLAY_ACQUIRE_FAILED) != nullptr,
           L"acquisition failure has a stable failure message");
    Expect(rs50_display_status_message(RS50_DISPLAY_UNACQUIRE_FAILED) !=
               nullptr,
           L"release failure has a stable failure message");
    Expect(rs50_display_status_message(RS50_DISPLAY_DATA_FORMAT_FAILED) !=
               nullptr,
           L"data format failure has a stable failure message");
    Expect(rs50_display_status_message(
               RS50_DISPLAY_OPERATION_ALREADY_ATTEMPTED) != nullptr,
           L"repeated operation failure has a stable message");

    rs50_display_close(nullptr);

    if (failures != 0)
    {
        return 1;
    }

    std::wcout << L"Guarded bridge safety tests passed.\n"
               << L"No DirectInput object or HID device was opened.\n";
    return 0;
}
