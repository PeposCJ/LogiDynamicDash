#include "rs50_display_bridge.h"

#include <Windows.h>

#include <cstdint>
#include <iomanip>
#include <iostream>
#include <string_view>

namespace
{
constexpr wchar_t WindowClassName[] = L"LogiDynamicDash.Rs50DirectInputQuery";

enum class QueryMode
{
    GeneralSupport,
    LayoutJSupport,
    StaticLayoutJ
};

LRESULT CALLBACK WindowProcedure(HWND window, UINT message, WPARAM wParam,
                                 LPARAM lParam)
{
    return DefWindowProcW(window, message, wParam, lParam);
}

HWND CreateOwnerWindow()
{
    WNDCLASSW windowClass{};
    windowClass.lpfnWndProc = WindowProcedure;
    windowClass.hInstance = GetModuleHandleW(nullptr);
    windowClass.lpszClassName = WindowClassName;

    if (RegisterClassW(&windowClass) == 0 &&
        GetLastError() != ERROR_CLASS_ALREADY_EXISTS)
    {
        return nullptr;
    }

    return CreateWindowExW(0, WindowClassName, L"RS50 display support query",
                           WS_OVERLAPPED, CW_USEDEFAULT, CW_USEDEFAULT, 320,
                           200, nullptr, nullptr, windowClass.hInstance,
                           nullptr);
}

void PrintUsage()
{
    std::wcout
        << L"This executable contains one fixed Layout J setter and no "
        << L"arbitrary display API.\n"
        << L"It will transmit only when every mode-specific confirmation "
        << L"argument is present.\n\n"
        << L"Safe metadata mode:\n"
        << L"  Rs50DirectInputQuery.exe --describe\n\n"
        << L"Transmitting mode (requires explicit approval and capture):\n"
        << L"  Rs50DirectInputQuery.exe "
        << L"--query-display-support --confirm-standard-data-format "
        << L"--confirm-exclusive-acquire " << L"--confirm-transmit-query\n"
        << L"  Rs50DirectInputQuery.exe "
        << L"--query-layout-j-support --confirm-standard-data-format "
        << L"--confirm-exclusive-acquire "
        << L"--confirm-transmit-layout-query\n"
        << L"  Rs50DirectInputQuery.exe "
        << L"--set-static-layout-j --confirm-standard-data-format "
        << L"--confirm-exclusive-acquire "
        << L"--confirm-layout-j-static-text "
        << L"--confirm-transmit-static-setter\n";
}

void PrintUtcTimestamp(std::wstring_view label)
{
    SYSTEMTIME utc{};
    GetSystemTime(&utc);

    std::wcout << label << L": " << std::setfill(L'0') << std::setw(4)
               << utc.wYear << L"-" << std::setw(2) << utc.wMonth << L"-"
               << std::setw(2) << utc.wDay << L"T" << std::setw(2) << utc.wHour
               << L":" << std::setw(2) << utc.wMinute << L":" << std::setw(2)
               << utc.wSecond << L"." << std::setw(3) << utc.wMilliseconds
               << L"Z\n";
}
} // namespace

int wmain(int argumentCount, wchar_t *arguments[])
{
    if (argumentCount == 2 && std::wstring_view(arguments[1]) == L"--describe")
    {
        std::wcout << L"RS50 guarded DirectInput bridge ABI "
                   << rs50_display_abi_version()
                   << L"\nNo hardware was enumerated or opened.\n";
        return 0;
    }

    bool commonArgumentsValid =
        argumentCount == 5 &&
        std::wstring_view(arguments[2]) == L"--confirm-standard-data-format" &&
        std::wstring_view(arguments[3]) == L"--confirm-exclusive-acquire";
    bool isGeneralQuery =
        commonArgumentsValid &&
        std::wstring_view(arguments[1]) == L"--query-display-support" &&
        std::wstring_view(arguments[4]) == L"--confirm-transmit-query";
    bool isLayoutJQuery =
        commonArgumentsValid &&
        std::wstring_view(arguments[1]) == L"--query-layout-j-support" &&
        std::wstring_view(arguments[4]) == L"--confirm-transmit-layout-query";
    bool isStaticLayoutJ =
        argumentCount == 6 &&
        std::wstring_view(arguments[1]) == L"--set-static-layout-j" &&
        std::wstring_view(arguments[2]) == L"--confirm-standard-data-format" &&
        std::wstring_view(arguments[3]) == L"--confirm-exclusive-acquire" &&
        std::wstring_view(arguments[4]) ==
            L"--confirm-layout-j-static-text" &&
        std::wstring_view(arguments[5]) ==
            L"--confirm-transmit-static-setter";

    if (!isGeneralQuery && !isLayoutJQuery && !isStaticLayoutJ)
    {
        PrintUsage();
        return 2;
    }

    QueryMode queryMode = isStaticLayoutJ
                              ? QueryMode::StaticLayoutJ
                              : (isLayoutJQuery ? QueryMode::LayoutJSupport
                                                : QueryMode::GeneralSupport);

    HWND ownerWindow = CreateOwnerWindow();
    if (ownerWindow == nullptr)
    {
        std::wcerr << L"Could not create the process-owned window.\n";
        return 3;
    }

    ShowWindow(ownerWindow, SW_SHOWNORMAL);
    UpdateWindow(ownerWindow);
    SetForegroundWindow(ownerWindow);

    if (GetForegroundWindow() != ownerWindow)
    {
        std::wcerr << L"Could not make the process-owned window foreground.\n";
        DestroyWindow(ownerWindow);
        return 3;
    }

    rs50_display_handle *handle = nullptr;
    rs50_display_status status = rs50_display_open(
        reinterpret_cast<std::uintptr_t>(ownerWindow), &handle);

    if (status != RS50_DISPLAY_OK)
    {
        std::wcerr << L"Open failed: " << rs50_display_status_message(status)
                   << L"\n";
        DestroyWindow(ownerWindow);
        return 4;
    }

    if (queryMode == QueryMode::StaticLayoutJ)
    {
        rs50_display_static_layout_j_result result{};
        result.struct_size = sizeof(result);
        PrintUtcTimestamp(L"Static setter call started UTC");
        status = rs50_display_set_static_layout_j(handle, &result);
        PrintUtcTimestamp(L"Static setter call completed UTC");

        std::wcout
            << L"Operation: fixed Layout J static text\n"
            << L"Text: LOGIDYNAMICDASH | RS50 | OLED LINK | TEST 1\n"
            << L"Product: " << result.product_name << L"\n"
            << L"VID: 0x" << std::hex << result.vendor_id << L" PID: 0x"
            << result.product_id << std::dec << L"\n"
            << L"Acquired: " << static_cast<unsigned>(result.acquired)
            << L"\nCooperative HRESULT: 0x" << std::hex
            << static_cast<std::uint32_t>(
                   result.cooperative_level_hresult)
            << L"\nData format HRESULT: 0x"
            << static_cast<std::uint32_t>(result.data_format_hresult)
            << L"\nAcquire HRESULT: 0x"
            << static_cast<std::uint32_t>(result.acquire_hresult)
            << L"\nEscape HRESULT: 0x"
            << static_cast<std::uint32_t>(result.escape_hresult)
            << L"\nUnacquire HRESULT: 0x"
            << static_cast<std::uint32_t>(result.unacquire_hresult)
            << std::dec << L"\nInner command: "
            << static_cast<unsigned>(result.inner_command) << L"\n";
    }
    else
    {
        rs50_display_query_result result{};
        result.struct_size = sizeof(result);
        PrintUtcTimestamp(L"Query call started UTC");
        status = queryMode == QueryMode::LayoutJSupport
                     ? rs50_display_query_layout_j_support(handle, &result)
                     : rs50_display_query_support(handle, &result);
        PrintUtcTimestamp(L"Query call completed UTC");

        std::wcout << L"Query: "
               << (queryMode == QueryMode::LayoutJSupport
                       ? L"Layout J support"
                       : L"General display support")
               << L"\nProduct: " << result.product_name << L"\n"
               << L"VID: 0x" << std::hex << result.vendor_id << L" PID: 0x"
               << result.product_id << std::dec << L"\n"
               << L"Acquired: " << static_cast<unsigned>(result.acquired)
               << L"\n"
               << L"Cooperative HRESULT: 0x" << std::hex
               << static_cast<std::uint32_t>(result.cooperative_level_hresult)
               << L"\nData format HRESULT: 0x"
               << static_cast<std::uint32_t>(result.data_format_hresult)
               << L"\nAcquire HRESULT: 0x"
               << static_cast<std::uint32_t>(result.acquire_hresult)
               << L"\nEscape HRESULT: 0x"
               << static_cast<std::uint32_t>(result.escape_hresult) << std::dec
               << L"\nUnacquire HRESULT: 0x" << std::hex
               << static_cast<std::uint32_t>(result.unacquire_hresult)
               << std::dec << L"\nOutput capacity: "
               << result.output_capacity_before << L" -> "
               << result.output_capacity_after << L"\nInner command: "
               << static_cast<unsigned>(result.inner_command)
               << L"\nOutput byte: 0x" << std::hex
               << static_cast<unsigned>(result.output_value)
               << L"\nOutput bytes:";

        std::uint32_t displayedOutputSize = result.output_capacity_before < 12
                                                ? result.output_capacity_before
                                                : 12;
        for (std::uint32_t index = 0; index < displayedOutputSize; ++index)
        {
            std::wcout << L" " << std::setw(2)
                       << static_cast<unsigned>(result.output_bytes[index]);
        }

        std::wcout << std::dec << L"\nSupported: "
                   << static_cast<unsigned>(result.supported) << L"\n";
    }

    if (status != RS50_DISPLAY_OK)
    {
        std::wcerr << L"Query failed: " << rs50_display_status_message(status)
                   << L"\n";
    }

    rs50_display_close(handle);
    DestroyWindow(ownerWindow);
    return status == RS50_DISPLAY_OK ? 0 : 5;
}
