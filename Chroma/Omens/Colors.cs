using System;
using System.Globalization;
using System.Numerics;

namespace Chroma.Omens;

public static class Colors
{
    public const float DefaultAlpha = 1.2f;
    public const float MinAlpha = 0.2f;
    public const float MaxAlpha = 10f;

    public const float DefaultSpeed = 0.05f;
    public const float MinSpeed = 0.01f;
    public const float MaxSpeed = 0.4f;

    private const float OppositeHue = 0.5f;

    public static readonly Vector4 NoTint = Vector4.One;
    public static readonly Vector4 WhiteModeColor = new(10f);
    public static readonly Vector4 DefaultCastProgressColor = new(1f, 0f, 0f, 1f);

    public static readonly string LowAlphaWarning = $"This can cause omen visibility issues when set below the default value of {DefaultAlpha}.";
    public const string WhiteModeWarning = "This may cause slight rendering issues.";

    public static readonly (string Name, Vector4 Color)[] Presets =
    [
        ("Red", new(1f, 0f, 0f, 1f)),
        ("Orange", new(1f, 0.5f, 0f, 1f)),
        ("Yellow", new(1f, 1f, 0f, 1f)),
        ("Green", new(0f, 1f, 0f, 1f)),
        ("Blue", new(0f, 0.12f, 1f, 1f)),
        ("Purple", new(0.2f, 0f, 1f, 1f)),
        ("Pink", new(0.8f, 0.25f, 1f, 1f)),
        ("Black", new(0f, 0f, 0f, 1f)),
    ];

    public static Vector4 GetOmenColor(Config config, bool hostile, uint territory)
    {
        if (hostile && config.FindDuty(territory) is { } duty)
            return WithAlpha(duty.OmenColor, config.Alpha);

        if (config.WhiteMode)
            return WhiteModeColor;

        if (config.RainbowMode)
            return WithAlpha(RainbowColor(config.Speed, hostile ? 0f : OppositeHue), config.Alpha);

        return WithAlpha(hostile ? config.GlobalColor : config.NonHostileColor, config.Alpha);
    }

    public static Vector4 WithAlpha(Vector4 color, float alpha) => color with { W = alpha };

    public static bool SameRgb(Vector4 a, Vector4 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    public static string ToHex(Vector4 color) => $"#{ToByte(color.X):X2}{ToByte(color.Y):X2}{ToByte(color.Z):X2}";

    public static bool IsLowAlpha(Config config) => !config.WhiteMode && config.Alpha < DefaultAlpha;

    public static bool IsOverbright(Vector4 color) => color.X > 1f || color.Y > 1f || color.Z > 1f;

    public static Vector4 Hue(float hue)
    {
        var scaled = hue * 6f;
        var sector = (int)scaled;
        var rising = scaled - sector;
        var falling = 1f - rising;
        return sector switch
        {
            0 => new(1f, rising, 0f, 1f),
            1 => new(falling, 1f, 0f, 1f),
            2 => new(0f, 1f, rising, 1f),
            3 => new(0f, falling, 1f, 1f),
            4 => new(rising, 0f, 1f, 1f),
            _ => new(1f, 0f, falling, 1f),
        };
    }

    public static bool TryParse(string text, out string name, out Vector4 color) =>
        TryParsePreset(text, out name, out color) || TryParseHex(text, out name, out color);

    private static bool TryParsePreset(string text, out string name, out Vector4 color)
    {
        foreach (var preset in Presets)
        {
            if (!preset.Name.Equals(text, StringComparison.OrdinalIgnoreCase))
                continue;

            (name, color) = preset;
            return true;
        }

        name = string.Empty;
        color = default;
        return false;
    }

    private static bool TryParseHex(string text, out string name, out Vector4 color)
    {
        var hex = text.TrimStart('#');
        if (hex.Length != 6 || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            name = string.Empty;
            color = default;
            return false;
        }

        name = $"#{hex.ToUpperInvariant()}";
        color = new(Channel(rgb, 16), Channel(rgb, 8), Channel(rgb, 0), 1f);
        return true;
    }

    private static float Channel(uint rgb, int shift) => ((rgb >> shift) & 0xFF) / 255f;

    private static int ToByte(float channel) => (int)MathF.Round(Math.Clamp(channel, 0f, 1f) * 255f);

    private static Vector4 RainbowColor(float speed, float hueOffset)
    {
        var seconds = Environment.TickCount64 / 1000.0;
        var hue = (float)((seconds * speed + hueOffset) % 1.0);
        return Hue(hue);
    }
}
