#pragma once

#include "rs50_display_bridge.h"

#include <array>
#include <cstddef>
#include <cstdint>
#include <string>

namespace rs50::layout_j
{
constexpr std::uint64_t MinimumFrameIntervalMilliseconds = 200;

enum class GateDecision
{
    Transmit,
    Unchanged,
    RateLimited
};

inline bool IsCanonicalText(const char *text, std::size_t capacity,
                            std::uint8_t length) noexcept
{
    if (length > capacity)
    {
        return false;
    }

    for (std::size_t index = 0; index < capacity; ++index)
    {
        unsigned char value = static_cast<unsigned char>(text[index]);
        if (index < length)
        {
            if (value < 0x20 || value > 0x7F)
            {
                return false;
            }
        }
        else if (value != 0)
        {
            return false;
        }
    }

    return true;
}

inline bool IsValidFrame(const rs50_display_layout_j_frame &frame) noexcept
{
    return frame.struct_size == sizeof(rs50_display_layout_j_frame) &&
           frame.reserved[0] == 0 && frame.reserved[1] == 0 &&
           IsCanonicalText(frame.row1, std::size(frame.row1),
                           frame.row1_length) &&
           IsCanonicalText(frame.row2, std::size(frame.row2),
                           frame.row2_length) &&
           IsCanonicalText(frame.row3, std::size(frame.row3),
                           frame.row3_length) &&
           IsCanonicalText(frame.row4, std::size(frame.row4),
                           frame.row4_length);
}

inline std::array<std::string, 4>
MakeDriverArguments(const rs50_display_layout_j_frame &frame)
{
    // Physical Build C proved that the Logitech DirectInput adapter maps
    // game-facing arguments 1/2/3/4 to visual rows 2/1/4/3.
    return {
        std::string(frame.row2, frame.row2_length),
        std::string(frame.row1, frame.row1_length),
        std::string(frame.row4, frame.row4_length),
        std::string(frame.row3, frame.row3_length)};
}

inline bool FramesEqual(const rs50_display_layout_j_frame &left,
                        const rs50_display_layout_j_frame &right) noexcept
{
    if (left.row1_length != right.row1_length ||
        left.row2_length != right.row2_length ||
        left.row3_length != right.row3_length ||
        left.row4_length != right.row4_length)
    {
        return false;
    }

    const auto equalText = [](const char *first, const char *second,
                              std::uint8_t length) noexcept
    {
        for (std::uint8_t index = 0; index < length; ++index)
        {
            if (first[index] != second[index])
            {
                return false;
            }
        }
        return true;
    };

    return equalText(left.row1, right.row1, left.row1_length) &&
           equalText(left.row2, right.row2, left.row2_length) &&
           equalText(left.row3, right.row3, left.row3_length) &&
           equalText(left.row4, right.row4, left.row4_length);
}

inline GateDecision DecideFrame(
    const rs50_display_layout_j_frame *lastFrame,
    std::uint64_t lastTransmissionMilliseconds,
    const rs50_display_layout_j_frame &candidate,
    std::uint64_t nowMilliseconds) noexcept
{
    if (lastFrame != nullptr && FramesEqual(*lastFrame, candidate))
    {
        return GateDecision::Unchanged;
    }

    if (lastFrame != nullptr &&
        nowMilliseconds - lastTransmissionMilliseconds <
            MinimumFrameIntervalMilliseconds)
    {
        return GateDecision::RateLimited;
    }

    return GateDecision::Transmit;
}
} // namespace rs50::layout_j
