#include "rs50_display_bridge.h"
#include "rs50_layout_j_frame.h"

#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <iostream>
#include <string_view>

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

rs50_display_layout_j_frame MakeFrame(
    std::string_view row1, std::string_view row2,
    std::string_view row3, std::string_view row4)
{
    rs50_display_layout_j_frame frame{};
    frame.struct_size = sizeof(frame);

    const auto copyText = [](std::string_view source, char *destination,
                             std::uint8_t &length)
    {
        std::copy(source.begin(), source.end(), destination);
        length = static_cast<std::uint8_t>(source.size());
    };

    copyText(row1, frame.row1, frame.row1_length);
    copyText(row2, frame.row2, frame.row2_length);
    copyText(row3, frame.row3, frame.row3_length);
    copyText(row4, frame.row4, frame.row4_length);
    return frame;
}
} // namespace

int wmain()
{
    Expect(rs50_display_abi_version() == 6, L"ABI version is six");

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

    rs50_display_stream_result streamResult{};
    streamResult.struct_size = sizeof(streamResult);
    Expect(rs50_display_begin_layout_j_stream(nullptr, &streamResult) ==
               RS50_DISPLAY_INVALID_ARGUMENT,
           L"stream begin rejects a null handle");
    Expect(rs50_display_end_layout_j_stream(nullptr, &streamResult) ==
               RS50_DISPLAY_INVALID_ARGUMENT,
           L"stream end rejects a null handle");

    rs50_display_layout_j_frame frame =
        MakeFrame("SPEED", "123 KMH", "GEAR", "4");
    Expect(rs50_display_set_layout_j_frame(nullptr, &frame, &streamResult) ==
               RS50_DISPLAY_INVALID_ARGUMENT,
           L"stream frame rejects a null handle");

    Expect(sizeof(rs50_display_layout_j_frame) == 68,
           L"Layout J frame ABI size is 68 bytes");
    Expect(offsetof(rs50_display_layout_j_frame, row1) == 8,
           L"row 1 starts at offset 8");
    Expect(offsetof(rs50_display_layout_j_frame, row2) == 27,
           L"row 2 starts at offset 27");
    Expect(offsetof(rs50_display_layout_j_frame, row3) == 37,
           L"row 3 starts at offset 37");
    Expect(offsetof(rs50_display_layout_j_frame, row4) == 56,
           L"row 4 starts at offset 56");
    Expect(sizeof(rs50_display_stream_result) == 560,
           L"stream result ABI size is 560 bytes");
    Expect(rs50::layout_j::IsValidFrame(frame),
           L"canonical visual frame is valid");

    std::array<std::string, 4> arguments =
        rs50::layout_j::MakeDriverArguments(frame);
    Expect(arguments[0] == "123 KMH" && arguments[1] == "SPEED" &&
               arguments[2] == "4" && arguments[3] == "GEAR",
           L"visual rows map to driver arguments 2/1/4/3");

    rs50_display_layout_j_frame invalid = frame;
    invalid.row2_length = 11;
    Expect(!rs50::layout_j::IsValidFrame(invalid),
           L"oversize row is rejected");
    invalid = frame;
    invalid.row1[0] = '\n';
    Expect(!rs50::layout_j::IsValidFrame(invalid),
           L"control character is rejected");
    invalid = frame;
    invalid.row1[frame.row1_length] = 'X';
    Expect(!rs50::layout_j::IsValidFrame(invalid),
           L"nonzero canonical tail is rejected");
    invalid = frame;
    invalid.reserved[0] = 1;
    Expect(!rs50::layout_j::IsValidFrame(invalid),
           L"nonzero reserved byte is rejected");

    Expect(rs50::layout_j::DecideFrame(nullptr, 0, frame, 1000) ==
               rs50::layout_j::GateDecision::Transmit,
           L"first frame transmits");
    Expect(rs50::layout_j::DecideFrame(&frame, 1000, frame, 1001) ==
               rs50::layout_j::GateDecision::Unchanged,
           L"identical frame is suppressed before rate limiting");
    rs50_display_layout_j_frame changed =
        MakeFrame("SPEED", "124 KMH", "GEAR", "4");
    Expect(rs50::layout_j::DecideFrame(&frame, 1000, changed, 1199) ==
               rs50::layout_j::GateDecision::RateLimited,
           L"changed frame inside 200 ms is rate limited");
    Expect(rs50::layout_j::DecideFrame(&frame, 1000, changed, 1200) ==
               rs50::layout_j::GateDecision::Transmit,
           L"changed frame at 200 ms transmits");

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
    Expect(rs50_display_status_message(RS50_DISPLAY_SESSION_NOT_STARTED) !=
               nullptr,
           L"inactive stream has a stable message");
    Expect(rs50_display_status_message(RS50_DISPLAY_FRAME_INVALID) != nullptr,
           L"invalid frame has a stable message");
    Expect(rs50_display_status_message(RS50_DISPLAY_SESSION_FAILED) != nullptr,
           L"failed stream has a stable message");

    rs50_display_close(nullptr);

    if (failures != 0)
    {
        return 1;
    }

    std::wcout << L"Guarded bridge safety tests passed.\n"
               << L"No DirectInput object or HID device was opened.\n";
    return 0;
}
