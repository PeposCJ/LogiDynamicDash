#pragma once

#include <cstdint>

#if defined(RS50_BRIDGE_EXPORTS)
#define RS50_BRIDGE_API __declspec(dllexport)
#else
#define RS50_BRIDGE_API __declspec(dllimport)
#endif

extern "C"
{
    struct rs50_display_handle;

    enum rs50_display_status : std::int32_t
    {
        RS50_DISPLAY_OK = 0,
        RS50_DISPLAY_INVALID_ARGUMENT = 1,
        RS50_DISPLAY_INVALID_OWNER_WINDOW = 2,
        RS50_DISPLAY_DRIVER_REGISTRATION_MISSING = 3,
        RS50_DISPLAY_DRIVER_HASH_MISMATCH = 4,
        RS50_DISPLAY_DRIVER_SIGNATURE_INVALID = 5,
        RS50_DISPLAY_DIRECTINPUT_CREATE_FAILED = 6,
        RS50_DISPLAY_ENUMERATION_FAILED = 7,
        RS50_DISPLAY_DEVICE_NOT_FOUND = 8,
        RS50_DISPLAY_DEVICE_AMBIGUOUS = 9,
        RS50_DISPLAY_COOPERATIVE_LEVEL_FAILED = 10,
        RS50_DISPLAY_QUERY_ALREADY_ATTEMPTED = 11,
        RS50_DISPLAY_ESCAPE_FAILED = 12,
        RS50_DISPLAY_QUERY_OUTPUT_INVALID = 13,
        RS50_DISPLAY_INTERNAL_ERROR = 14,
        RS50_DISPLAY_ACQUIRE_FAILED = 15,
        RS50_DISPLAY_UNACQUIRE_FAILED = 16,
        RS50_DISPLAY_DATA_FORMAT_FAILED = 17,
        RS50_DISPLAY_OPERATION_ALREADY_ATTEMPTED = 18,
        RS50_DISPLAY_SESSION_NOT_STARTED = 19,
        RS50_DISPLAY_SESSION_ALREADY_STARTED = 20,
        RS50_DISPLAY_FRAME_INVALID = 21,
        RS50_DISPLAY_SESSION_FAILED = 22
    };

    struct rs50_display_query_result
    {
        std::uint32_t struct_size;
        std::uint32_t vendor_id;
        std::uint32_t product_id;
        std::int32_t cooperative_level_hresult;
        std::int32_t data_format_hresult;
        std::int32_t acquire_hresult;
        std::int32_t escape_hresult;
        std::int32_t unacquire_hresult;
        std::uint32_t output_capacity_before;
        std::uint32_t output_capacity_after;
        std::uint8_t supported;
        std::uint8_t output_value;
        std::uint8_t acquired;
        std::uint8_t inner_command;
        std::uint8_t output_bytes[12];
        wchar_t product_name[260];
    };

    struct rs50_display_static_layout_j_result
    {
        std::uint32_t struct_size;
        std::uint32_t vendor_id;
        std::uint32_t product_id;
        std::int32_t cooperative_level_hresult;
        std::int32_t data_format_hresult;
        std::int32_t acquire_hresult;
        std::int32_t escape_hresult;
        std::int32_t unacquire_hresult;
        std::uint8_t acquired;
        std::uint8_t inner_command;
        std::uint8_t reserved[2];
        wchar_t product_name[260];
    };

    struct rs50_display_layout_j_frame
    {
        std::uint32_t struct_size;
        std::uint8_t row1_length;
        std::uint8_t row2_length;
        std::uint8_t row3_length;
        std::uint8_t row4_length;
        char row1[19];
        char row2[10];
        char row3[19];
        char row4[10];
        std::uint8_t reserved[2];
    };

    struct rs50_display_stream_result
    {
        std::uint32_t struct_size;
        std::uint32_t vendor_id;
        std::uint32_t product_id;
        std::int32_t cooperative_level_hresult;
        std::int32_t data_format_hresult;
        std::int32_t acquire_hresult;
        std::int32_t escape_hresult;
        std::int32_t unacquire_hresult;
        std::uint8_t acquired;
        std::uint8_t inner_command;
        std::uint8_t transmitted;
        std::uint8_t unchanged;
        std::uint8_t rate_limited;
        std::uint8_t reserved[3];
        wchar_t product_name[260];
    };

    RS50_BRIDGE_API std::uint32_t rs50_display_abi_version() noexcept;

    RS50_BRIDGE_API rs50_display_status rs50_display_open(
        std::uintptr_t owner_window, rs50_display_handle **handle) noexcept;

    RS50_BRIDGE_API rs50_display_status
    rs50_display_query_support(rs50_display_handle *handle,
                               rs50_display_query_result *result) noexcept;

    RS50_BRIDGE_API rs50_display_status rs50_display_query_layout_j_support(
        rs50_display_handle *handle,
        rs50_display_query_result *result) noexcept;

    RS50_BRIDGE_API rs50_display_status rs50_display_set_static_layout_j(
        rs50_display_handle *handle,
        rs50_display_static_layout_j_result *result) noexcept;

    RS50_BRIDGE_API rs50_display_status
    rs50_display_begin_layout_j_stream(
        rs50_display_handle *handle,
        rs50_display_stream_result *result) noexcept;

    RS50_BRIDGE_API rs50_display_status rs50_display_set_layout_j_frame(
        rs50_display_handle *handle,
        const rs50_display_layout_j_frame *frame,
        rs50_display_stream_result *result) noexcept;

    RS50_BRIDGE_API rs50_display_status rs50_display_end_layout_j_stream(
        rs50_display_handle *handle,
        rs50_display_stream_result *result) noexcept;

    RS50_BRIDGE_API const wchar_t *rs50_display_status_message(
        rs50_display_status status) noexcept;

    RS50_BRIDGE_API void rs50_display_close(
        rs50_display_handle *handle) noexcept;
}
