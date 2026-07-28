namespace LogiDynamicExplorer.Decoders;

internal static class HidppFeatureNames
{
    public static string Format(ushort featureId)
    {
        return featureId switch
        {
            0x00C3 => " (SecureDFU)",
            0x807A => " (RPM Indicator; G HUB static name)",
            0x807B => " (RPM LED Pattern; G HUB static name)",
            0x8091 => " (per-key/LED matrix)",
            0x80A4 => " (Axis Response Curve; G HUB static name)",
            0x80D0 => " (Combined Pedals; G HUB static name)",
            0x8120 => " (Gaming Attachments; G HUB static name)",
            0x8123 => " (Force Feedback; G HUB static name)",
            0x8127 => " (Dual Clutch; G HUB static name)",
            0x8130 => " (Display Game Data; G HUB static name)",
            0x8132 => " (Axis Mapping; G HUB static name)",
            0x8133 => " (Global Damping; G HUB static name)",
            0x8134 => " (Brake Force; G HUB static name)",
            0x8135 => " (Pedal Status; G HUB static name)",
            0x8136 => " (Torque Limit; G HUB static name)",
            0x8137 => " (Configuration Profiles; G HUB static name)",
            0x8138 => " (Operating Range; G HUB static name)",
            0x8139 => " (TRUEFORCE; G HUB static name)",
            0x8140 => " (FFB Filter; G HUB static name)",
            _ => string.Empty
        };
    }
}
