#define DIRECTINPUT_VERSION 0x0800

#include "rs50_display_bridge.h"

#include <Windows.h>
#include <bcrypt.h>
#include <dinput.h>
#include <softpub.h>
#include <wintrust.h>

#include <array>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <new>
#include <string>
#include <utility>
#include <vector>

#pragma comment(lib, "advapi32.lib")
#pragma comment(lib, "bcrypt.lib")
#pragma comment(lib, "dinput8.lib")
#pragma comment(lib, "dxguid.lib")
#pragma comment(lib, "wintrust.lib")

namespace
{
constexpr std::uint32_t AbiVersion = 5;
constexpr std::uint16_t LogitechVendorId = 0x046D;
constexpr std::uint16_t Rs50ProductId = 0xC276;
constexpr DWORD DisplayEscapeCommand = 4;
constexpr std::uint32_t DisplayRequestVersion = 1;
constexpr std::uint8_t QueryDisplaySupportCommand = 2;
constexpr std::uint8_t QueryLayoutJSupportCommand = 12;
constexpr std::uint8_t StaticLayoutJCommand = 22;
constexpr std::uint8_t OutputSentinel = 0xA5;
constexpr DWORD GeneralSupportOutputCapacity = 1;
constexpr DWORD LayoutJSupportOutputCapacity = 10;

constexpr wchar_t DriverRegistryPath[] =
    L"CLSID\\{62B43F0E-E7DB-4329-8C13-A966D84A289F}\\InprocServer32";

constexpr GUID ExpectedForceFeedbackDriver = {
    0x62B43F0E,
    0xE7DB,
    0x4329,
    {0x8C, 0x13, 0xA9, 0x66, 0xD8, 0x4A, 0x28, 0x9F}};

constexpr std::array<std::uint8_t, 32> ExpectedDriverSha256 = {
    0x17, 0xAB, 0x8F, 0xBB, 0x23, 0xFD, 0x54, 0x9C, 0xCC, 0xCD, 0xB7,
    0x2A, 0x50, 0x2C, 0x3B, 0xDC, 0xD9, 0x84, 0xF8, 0x0B, 0x6C, 0x40,
    0xE4, 0x80, 0x27, 0xC2, 0x55, 0x12, 0xD0, 0x40, 0x5A, 0x7F};

#pragma pack(push, 4)
struct DisplayRequest
{
    std::uint32_t size;
    std::uint32_t version;
    std::uint8_t command;
    std::uint8_t reserved[3];
};

struct StaticLayoutJRequest
{
    DisplayRequest header;
    std::string firstText;
    std::string secondText;
    std::string thirdText;
    std::string fourthText;
};
#pragma pack(pop)

static_assert(sizeof(DisplayRequest) == 12);
static_assert(offsetof(DisplayRequest, command) == 8);
static_assert(sizeof(std::string) == 32);
static_assert(sizeof(StaticLayoutJRequest) == 140);
static_assert(offsetof(StaticLayoutJRequest, firstText) == 12);
static_assert(offsetof(StaticLayoutJRequest, secondText) == 44);
static_assert(offsetof(StaticLayoutJRequest, thirdText) == 76);
static_assert(offsetof(StaticLayoutJRequest, fourthText) == 108);
static_assert(sizeof(DIEFFESCAPE) == 40);
static_assert(offsetof(DIEFFESCAPE, dwCommand) == 4);
static_assert(sizeof(rs50_display_query_result) == 576);
static_assert(offsetof(rs50_display_query_result, product_name) == 56);
static_assert(sizeof(rs50_display_static_layout_j_result) == 556);
static_assert(
    offsetof(rs50_display_static_layout_j_result, product_name) == 36);

template <typename T> class ComPointer final
{
  public:
    ComPointer() = default;

    ~ComPointer()
    {
        Reset();
    }

    ComPointer(const ComPointer &) = delete;
    ComPointer &operator=(const ComPointer &) = delete;

    ComPointer(ComPointer &&other) noexcept
        : value_(std::exchange(other.value_, nullptr))
    {
    }

    ComPointer &operator=(ComPointer &&other) noexcept
    {
        if (this != &other)
        {
            Reset();
            value_ = std::exchange(other.value_, nullptr);
        }

        return *this;
    }

    T *Get() const noexcept
    {
        return value_;
    }

    T **Put() noexcept
    {
        Reset();
        return &value_;
    }

    void Reset() noexcept
    {
        if (value_ != nullptr)
        {
            value_->Release();
            value_ = nullptr;
        }
    }

  private:
    T *value_ = nullptr;
};

class FileHandle final
{
  public:
    explicit FileHandle(HANDLE value) noexcept : value_(value)
    {
    }

    ~FileHandle()
    {
        if (value_ != INVALID_HANDLE_VALUE)
        {
            CloseHandle(value_);
        }
    }

    FileHandle(const FileHandle &) = delete;
    FileHandle &operator=(const FileHandle &) = delete;

    HANDLE Get() const noexcept
    {
        return value_;
    }

  private:
    HANDLE value_;
};

class AlgorithmHandle final
{
  public:
    ~AlgorithmHandle()
    {
        if (value != nullptr)
        {
            BCryptCloseAlgorithmProvider(value, 0);
        }
    }

    BCRYPT_ALG_HANDLE value = nullptr;
};

class HashHandle final
{
  public:
    ~HashHandle()
    {
        if (value != nullptr)
        {
            BCryptDestroyHash(value);
        }
    }

    BCRYPT_HASH_HANDLE value = nullptr;
};

class ExclusiveSrwLock final
{
  public:
    explicit ExclusiveSrwLock(SRWLOCK &lock) noexcept : lock_(lock)
    {
        AcquireSRWLockExclusive(&lock_);
    }

    ~ExclusiveSrwLock()
    {
        ReleaseSRWLockExclusive(&lock_);
    }

    ExclusiveSrwLock(const ExclusiveSrwLock &) = delete;
    ExclusiveSrwLock &operator=(const ExclusiveSrwLock &) = delete;

  private:
    SRWLOCK &lock_;
};

bool IsSuccessful(NTSTATUS status) noexcept
{
    return status >= 0;
}

bool ReadRegisteredDriverPath(std::wstring &path)
{
    DWORD requiredBytes = 0;
    LSTATUS status = RegGetValueW(HKEY_CLASSES_ROOT, DriverRegistryPath,
                                  nullptr, RRF_RT_REG_SZ | RRF_RT_REG_EXPAND_SZ,
                                  nullptr, nullptr, &requiredBytes);

    if (status != ERROR_SUCCESS || requiredBytes < sizeof(wchar_t))
    {
        return false;
    }

    std::vector<wchar_t> buffer(requiredBytes / sizeof(wchar_t));
    status = RegGetValueW(HKEY_CLASSES_ROOT, DriverRegistryPath, nullptr,
                          RRF_RT_REG_SZ | RRF_RT_REG_EXPAND_SZ, nullptr,
                          buffer.data(), &requiredBytes);

    if (status != ERROR_SUCCESS || buffer.empty() || buffer[0] == L'\0')
    {
        return false;
    }

    path.assign(buffer.data());
    return true;
}

bool ComputeSha256(const std::wstring &path,
                   std::array<std::uint8_t, 32> &digest)
{
    FileHandle file(CreateFileW(
        path.c_str(), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE,
        nullptr, OPEN_EXISTING,
        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, nullptr));

    if (file.Get() == INVALID_HANDLE_VALUE)
    {
        return false;
    }

    AlgorithmHandle algorithm;
    if (!IsSuccessful(BCryptOpenAlgorithmProvider(
            &algorithm.value, BCRYPT_SHA256_ALGORITHM, nullptr, 0)))
    {
        return false;
    }

    DWORD objectLength = 0;
    DWORD copied = 0;
    if (!IsSuccessful(BCryptGetProperty(algorithm.value, BCRYPT_OBJECT_LENGTH,
                                        reinterpret_cast<PUCHAR>(&objectLength),
                                        sizeof(objectLength), &copied, 0)) ||
        objectLength == 0)
    {
        return false;
    }

    std::vector<std::uint8_t> hashObject(objectLength);
    HashHandle hash;
    if (!IsSuccessful(BCryptCreateHash(
            algorithm.value, &hash.value, hashObject.data(),
            static_cast<ULONG>(hashObject.size()), nullptr, 0, 0)))
    {
        return false;
    }

    std::array<std::uint8_t, 8 * 1024> input{};
    for (;;)
    {
        DWORD bytesRead = 0;
        if (!ReadFile(file.Get(), input.data(),
                      static_cast<DWORD>(input.size()), &bytesRead, nullptr))
        {
            return false;
        }

        if (bytesRead == 0)
        {
            break;
        }

        if (!IsSuccessful(
                BCryptHashData(hash.value, input.data(), bytesRead, 0)))
        {
            return false;
        }
    }

    return IsSuccessful(BCryptFinishHash(hash.value, digest.data(),
                                         static_cast<ULONG>(digest.size()), 0));
}

bool HasValidAuthenticodeSignature(const std::wstring &path)
{
    WINTRUST_FILE_INFO fileInfo{};
    fileInfo.cbStruct = sizeof(fileInfo);
    fileInfo.pcwszFilePath = path.c_str();

    WINTRUST_DATA trustData{};
    trustData.cbStruct = sizeof(trustData);
    trustData.dwUIChoice = WTD_UI_NONE;
    trustData.fdwRevocationChecks = WTD_REVOKE_NONE;
    trustData.dwUnionChoice = WTD_CHOICE_FILE;
    trustData.pFile = &fileInfo;
    trustData.dwStateAction = WTD_STATEACTION_VERIFY;
    trustData.dwProvFlags =
        WTD_CACHE_ONLY_URL_RETRIEVAL | WTD_REVOCATION_CHECK_NONE;

    GUID action = WINTRUST_ACTION_GENERIC_VERIFY_V2;
    LONG result = WinVerifyTrust(nullptr, &action, &trustData);

    trustData.dwStateAction = WTD_STATEACTION_CLOSE;
    WinVerifyTrust(nullptr, &action, &trustData);
    return result == ERROR_SUCCESS;
}

rs50_display_status ValidateInstalledDriver()
{
    std::wstring driverPath;
    if (!ReadRegisteredDriverPath(driverPath))
    {
        return RS50_DISPLAY_DRIVER_REGISTRATION_MISSING;
    }

    std::array<std::uint8_t, 32> digest{};
    if (!ComputeSha256(driverPath, digest) || digest != ExpectedDriverSha256)
    {
        return RS50_DISPLAY_DRIVER_HASH_MISMATCH;
    }

    if (!HasValidAuthenticodeSignature(driverPath))
    {
        return RS50_DISPLAY_DRIVER_SIGNATURE_INVALID;
    }

    return RS50_DISPLAY_OK;
}

bool IsValidOwnerWindow(HWND window)
{
    if (window == nullptr || !IsWindow(window) ||
        GetAncestor(window, GA_ROOT) != window)
    {
        return false;
    }

    DWORD processId = 0;
    GetWindowThreadProcessId(window, &processId);
    return processId == GetCurrentProcessId();
}

void CopySanitizedProductName(wchar_t (&destination)[260],
                              const wchar_t *source) noexcept
{
    std::size_t index = 0;
    for (; index + 1 < std::size(destination) && source[index] != L'\0';
         ++index)
    {
        wchar_t character = source[index];
        destination[index] =
            character >= 0x20 && character <= 0x7E ? character : L'?';
    }

    destination[index] = L'\0';
}

struct Candidate
{
    ComPointer<IDirectInputDevice8W> device;
    DIDEVICEINSTANCEW instance{};
};

struct EnumerationContext
{
    IDirectInput8W *directInput = nullptr;
    std::vector<Candidate> candidates;
    bool internalFailure = false;
};

BOOL CALLBACK EnumerateDevices(const DIDEVICEINSTANCEW *instance,
                               void *contextValue)
{
    auto &context = *static_cast<EnumerationContext *>(contextValue);

    ComPointer<IDirectInputDevice8W> device;
    HRESULT result = context.directInput->CreateDevice(instance->guidInstance,
                                                       device.Put(), nullptr);

    if (FAILED(result))
    {
        return DIENUM_CONTINUE;
    }

    DIPROPDWORD vidPid{};
    vidPid.diph.dwSize = sizeof(vidPid);
    vidPid.diph.dwHeaderSize = sizeof(vidPid.diph);
    vidPid.diph.dwHow = DIPH_DEVICE;

    result = device.Get()->GetProperty(DIPROP_VIDPID, &vidPid.diph);

    if (FAILED(result))
    {
        return DIENUM_CONTINUE;
    }

    std::uint16_t vendor = LOWORD(vidPid.dwData);
    std::uint16_t product = HIWORD(vidPid.dwData);

    if (vendor != LogitechVendorId || product != Rs50ProductId ||
        !IsEqualGUID(instance->guidFFDriver, ExpectedForceFeedbackDriver))
    {
        return DIENUM_CONTINUE;
    }

    try
    {
        Candidate candidate;
        candidate.device = std::move(device);
        candidate.instance = *instance;
        context.candidates.push_back(std::move(candidate));
    }
    catch (...)
    {
        context.internalFailure = true;
        return DIENUM_STOP;
    }

    return DIENUM_CONTINUE;
}
} // namespace

struct rs50_display_handle
{
    ComPointer<IDirectInput8W> directInput;
    ComPointer<IDirectInputDevice8W> device;
    DIDEVICEINSTANCEW instance{};
    HRESULT cooperativeLevelResult = E_UNEXPECTED;
    SRWLOCK queryLock = SRWLOCK_INIT;
    bool operationAttempted = false;
    bool acquired = false;
};

namespace
{
struct EscapeLifecycleResult
{
    HRESULT dataFormat = E_UNEXPECTED;
    HRESULT acquire = E_UNEXPECTED;
    HRESULT escape = E_UNEXPECTED;
    HRESULT unacquire = E_UNEXPECTED;
    DWORD outputCapacity = 0;
    bool acquired = false;
};

rs50_display_status ExecuteEscape(rs50_display_handle *handle,
                                  void *inputBuffer, DWORD inputCapacity,
                                  void *outputBuffer, DWORD outputCapacity,
                                  EscapeLifecycleResult &result) noexcept
{
    ExclusiveSrwLock lock(handle->queryLock);

    if (handle->operationAttempted)
    {
        return RS50_DISPLAY_OPERATION_ALREADY_ATTEMPTED;
    }

    handle->operationAttempted = true;

    DIEFFESCAPE escape{};
    escape.dwSize = sizeof(escape);
    escape.dwCommand = DisplayEscapeCommand;
    escape.lpvInBuffer = inputBuffer;
    escape.cbInBuffer = inputCapacity;
    escape.lpvOutBuffer = outputBuffer;
    escape.cbOutBuffer = outputCapacity;
    result.outputCapacity = outputCapacity;

    result.dataFormat = handle->device.Get()->SetDataFormat(&c_dfDIJoystick2);
    if (FAILED(result.dataFormat))
    {
        return RS50_DISPLAY_DATA_FORMAT_FAILED;
    }

    result.acquire = handle->device.Get()->Acquire();
    if (FAILED(result.acquire))
    {
        return RS50_DISPLAY_ACQUIRE_FAILED;
    }

    handle->acquired = true;
    result.acquired = true;
    result.escape = handle->device.Get()->Escape(&escape);
    result.outputCapacity = escape.cbOutBuffer;
    result.unacquire = handle->device.Get()->Unacquire();

    if (SUCCEEDED(result.unacquire))
    {
        handle->acquired = false;
    }
    else
    {
        return RS50_DISPLAY_UNACQUIRE_FAILED;
    }

    if (FAILED(result.escape))
    {
        return RS50_DISPLAY_ESCAPE_FAILED;
    }

    return RS50_DISPLAY_OK;
}

rs50_display_status ExecuteQuery(rs50_display_handle *handle,
                                 rs50_display_query_result *result,
                                 std::uint8_t innerCommand,
                                 DWORD outputCapacity) noexcept
{
    if (handle == nullptr || result == nullptr ||
        result->struct_size != sizeof(rs50_display_query_result) ||
        (innerCommand != QueryDisplaySupportCommand &&
         innerCommand != QueryLayoutJSupportCommand) ||
        (outputCapacity != GeneralSupportOutputCapacity &&
         outputCapacity != LayoutJSupportOutputCapacity))
    {
        return RS50_DISPLAY_INVALID_ARGUMENT;
    }

    rs50_display_query_result local{};
    local.struct_size = sizeof(local);
    local.vendor_id = LogitechVendorId;
    local.product_id = Rs50ProductId;
    local.cooperative_level_hresult = handle->cooperativeLevelResult;
    local.data_format_hresult = E_UNEXPECTED;
    local.acquire_hresult = E_UNEXPECTED;
    local.escape_hresult = E_UNEXPECTED;
    local.unacquire_hresult = E_UNEXPECTED;
    local.output_capacity_before = outputCapacity;
    local.output_capacity_after = outputCapacity;
    local.acquired = 0;
    local.output_value = OutputSentinel;
    local.inner_command = innerCommand;
    for (std::uint8_t &value : local.output_bytes)
    {
        value = OutputSentinel;
    }

    CopySanitizedProductName(local.product_name,
                             handle->instance.tszProductName);

    DisplayRequest request{};
    request.size = sizeof(request);
    request.version = DisplayRequestVersion;
    request.command = innerCommand;

    EscapeLifecycleResult lifecycle;
    rs50_display_status status =
        ExecuteEscape(handle, &request, sizeof(request), local.output_bytes,
                      outputCapacity, lifecycle);

    local.data_format_hresult = lifecycle.dataFormat;
    local.acquire_hresult = lifecycle.acquire;
    local.escape_hresult = lifecycle.escape;
    local.unacquire_hresult = lifecycle.unacquire;
    local.output_capacity_after = lifecycle.outputCapacity;
    local.acquired = lifecycle.acquired ? 1 : 0;
    local.output_value = local.output_bytes[0];
    local.supported = local.output_value == 1 ? 1 : 0;
    *result = local;

    if (status != RS50_DISPLAY_OK)
    {
        return status;
    }

    if (local.output_capacity_after != outputCapacity ||
        local.output_value > 1)
    {
        return RS50_DISPLAY_QUERY_OUTPUT_INVALID;
    }

    for (DWORD index = 1; index < outputCapacity; ++index)
    {
        if (local.output_bytes[index] != OutputSentinel)
        {
            return RS50_DISPLAY_QUERY_OUTPUT_INVALID;
        }
    }

    return RS50_DISPLAY_OK;
}

rs50_display_status ExecuteStaticLayoutJ(
    rs50_display_handle *handle,
    rs50_display_static_layout_j_result *result) noexcept
{
    if (handle == nullptr || result == nullptr ||
        result->struct_size != sizeof(rs50_display_static_layout_j_result))
    {
        return RS50_DISPLAY_INVALID_ARGUMENT;
    }

    rs50_display_static_layout_j_result local{};
    local.struct_size = sizeof(local);
    local.vendor_id = LogitechVendorId;
    local.product_id = Rs50ProductId;
    local.cooperative_level_hresult = handle->cooperativeLevelResult;
    local.data_format_hresult = E_UNEXPECTED;
    local.acquire_hresult = E_UNEXPECTED;
    local.escape_hresult = E_UNEXPECTED;
    local.unacquire_hresult = E_UNEXPECTED;
    local.inner_command = StaticLayoutJCommand;
    CopySanitizedProductName(local.product_name,
                             handle->instance.tszProductName);

    try
    {
        StaticLayoutJRequest request{
            {sizeof(StaticLayoutJRequest), DisplayRequestVersion,
             StaticLayoutJCommand, {}},
            "LOGIDYNAMICDASH",
            "RS50",
            "OLED LINK",
            "TEST 1"};

        EscapeLifecycleResult lifecycle;
        rs50_display_status status =
            ExecuteEscape(handle, &request, sizeof(request), nullptr, 0,
                          lifecycle);

        local.data_format_hresult = lifecycle.dataFormat;
        local.acquire_hresult = lifecycle.acquire;
        local.escape_hresult = lifecycle.escape;
        local.unacquire_hresult = lifecycle.unacquire;
        local.acquired = lifecycle.acquired ? 1 : 0;
        *result = local;
        return status;
    }
    catch (...)
    {
        *result = local;
        return RS50_DISPLAY_INTERNAL_ERROR;
    }
}
} // namespace

std::uint32_t rs50_display_abi_version() noexcept
{
    return AbiVersion;
}

rs50_display_status rs50_display_open(
    std::uintptr_t ownerWindowValue,
    rs50_display_handle **outputHandle) noexcept
{
    if (outputHandle == nullptr)
    {
        return RS50_DISPLAY_INVALID_ARGUMENT;
    }

    *outputHandle = nullptr;
    HWND ownerWindow = reinterpret_cast<HWND>(ownerWindowValue);
    if (!IsValidOwnerWindow(ownerWindow))
    {
        return RS50_DISPLAY_INVALID_OWNER_WINDOW;
    }

    try
    {
        rs50_display_status driverStatus = ValidateInstalledDriver();
        if (driverStatus != RS50_DISPLAY_OK)
        {
            return driverStatus;
        }

        std::unique_ptr<rs50_display_handle> handle(new (std::nothrow)
                                                        rs50_display_handle());
        if (!handle)
        {
            return RS50_DISPLAY_INTERNAL_ERROR;
        }

        HRESULT result = DirectInput8Create(
            GetModuleHandleW(nullptr), DIRECTINPUT_VERSION, IID_IDirectInput8W,
            reinterpret_cast<void **>(handle->directInput.Put()), nullptr);

        if (FAILED(result))
        {
            return RS50_DISPLAY_DIRECTINPUT_CREATE_FAILED;
        }

        EnumerationContext context;
        context.directInput = handle->directInput.Get();
        result = handle->directInput.Get()->EnumDevices(
            DI8DEVCLASS_GAMECTRL, EnumerateDevices, &context,
            DIEDFL_ATTACHEDONLY);

        if (FAILED(result))
        {
            return RS50_DISPLAY_ENUMERATION_FAILED;
        }

        if (context.internalFailure)
        {
            return RS50_DISPLAY_INTERNAL_ERROR;
        }

        if (context.candidates.empty())
        {
            return RS50_DISPLAY_DEVICE_NOT_FOUND;
        }

        if (context.candidates.size() != 1)
        {
            return RS50_DISPLAY_DEVICE_AMBIGUOUS;
        }

        handle->device = std::move(context.candidates[0].device);
        handle->instance = context.candidates[0].instance;
        handle->cooperativeLevelResult =
            handle->device.Get()->SetCooperativeLevel(
                ownerWindow, DISCL_EXCLUSIVE | DISCL_FOREGROUND);

        if (FAILED(handle->cooperativeLevelResult))
        {
            return RS50_DISPLAY_COOPERATIVE_LEVEL_FAILED;
        }

        *outputHandle = handle.release();
        return RS50_DISPLAY_OK;
    }
    catch (...)
    {
        return RS50_DISPLAY_INTERNAL_ERROR;
    }
}

rs50_display_status rs50_display_query_support(
    rs50_display_handle *handle, rs50_display_query_result *result) noexcept
{
    return ExecuteQuery(handle, result, QueryDisplaySupportCommand,
                        GeneralSupportOutputCapacity);
}

rs50_display_status rs50_display_query_layout_j_support(
    rs50_display_handle *handle, rs50_display_query_result *result) noexcept
{
    return ExecuteQuery(handle, result, QueryLayoutJSupportCommand,
                        LayoutJSupportOutputCapacity);
}

rs50_display_status rs50_display_set_static_layout_j(
    rs50_display_handle *handle,
    rs50_display_static_layout_j_result *result) noexcept
{
    return ExecuteStaticLayoutJ(handle, result);
}

const wchar_t *rs50_display_status_message(rs50_display_status status) noexcept
{
    switch (status)
    {
    case RS50_DISPLAY_OK:
        return L"OK";
    case RS50_DISPLAY_INVALID_ARGUMENT:
        return L"Invalid argument or ABI size";
    case RS50_DISPLAY_INVALID_OWNER_WINDOW:
        return L"Owner must be a top-level window owned by this process";
    case RS50_DISPLAY_DRIVER_REGISTRATION_MISSING:
        return L"Expected Logitech force-feedback driver is not registered";
    case RS50_DISPLAY_DRIVER_HASH_MISMATCH:
        return L"Installed Logitech driver hash differs from the audited build";
    case RS50_DISPLAY_DRIVER_SIGNATURE_INVALID:
        return L"Installed Logitech driver signature is not valid";
    case RS50_DISPLAY_DIRECTINPUT_CREATE_FAILED:
        return L"DirectInput8Create failed";
    case RS50_DISPLAY_ENUMERATION_FAILED:
        return L"DirectInput device enumeration failed";
    case RS50_DISPLAY_DEVICE_NOT_FOUND:
        return L"Exactly supported RS50 device was not found";
    case RS50_DISPLAY_DEVICE_AMBIGUOUS:
        return L"More than one matching RS50 device was found";
    case RS50_DISPLAY_COOPERATIVE_LEVEL_FAILED:
        return L"Exclusive foreground cooperative level failed";
    case RS50_DISPLAY_ACQUIRE_FAILED:
        return L"Exclusive foreground device acquisition failed";
    case RS50_DISPLAY_DATA_FORMAT_FAILED:
        return L"Standard joystick data format setup failed";
    case RS50_DISPLAY_UNACQUIRE_FAILED:
        return L"Device release failed after the support query";
    case RS50_DISPLAY_QUERY_ALREADY_ATTEMPTED:
        return L"Support query was already attempted on this handle";
    case RS50_DISPLAY_OPERATION_ALREADY_ATTEMPTED:
        return L"A display operation was already attempted on this handle";
    case RS50_DISPLAY_ESCAPE_FAILED:
        return L"Documented display operation failed";
    case RS50_DISPLAY_QUERY_OUTPUT_INVALID:
        return L"Support query returned an unexpected output shape or value";
    default:
        return L"Internal bridge error";
    }
}

void rs50_display_close(rs50_display_handle *handle) noexcept
{
    if (handle != nullptr && handle->acquired &&
        handle->device.Get() != nullptr)
    {
        handle->device.Get()->Unacquire();
        handle->acquired = false;
    }

    delete handle;
}
