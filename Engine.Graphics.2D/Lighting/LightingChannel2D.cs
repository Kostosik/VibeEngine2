namespace Engine.Graphics2D.Lighting;

[Flags]
public enum LightingChannel2D
{
    None = 0,
    Direct = 1 << 0,
    Ambient = 1 << 1,
    Indirect = 1 << 2,
    Emission = 1 << 3,

    All = Direct | Ambient | Indirect | Emission
}

internal static class LightingChannelValidation2D
{
    public static bool IsValidSupportedChannels(
        LightingChannel2D channels)
    {
        return channels != LightingChannel2D.None &&
               (channels & ~LightingChannel2D.All) == 0;
    }

    public static bool IsSingleChannel(
        LightingChannel2D channel)
    {
        var value = (int)channel;

        return value != 0 &&
               (channel & ~LightingChannel2D.All) == 0 &&
               (value & (value - 1)) == 0;
    }
}