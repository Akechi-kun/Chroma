using Chroma.Omens;
using Dalamud.Configuration;
using ECommons.DalamudServices;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Chroma;

public class Config : IPluginConfiguration
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;
    public bool Enabled = true;
    public Vector4 GlobalColor = Colors.NoTint;
    public float Alpha = Colors.DefaultAlpha;
    public bool NonHostile = false;
    public Vector4 NonHostileColor = Colors.NoTint;
    public bool RainbowMode = false;
    public bool WhiteMode = false;
    public float Speed = Colors.DefaultSpeed;
    public bool CastProgress = false;
    public Vector4 CastProgressColor = Colors.DefaultCastProgressColor;
    public Dictionary<ushort, DutyEntries> DutyColors = [];

    public bool TestingEnabled = false;
    public bool TestNonHostile = false;
    public float TestCircleRadius = 5f;
    public float TestConeRadius = 10f;
    public float TestConeRotation = 0f;
    public int TestConeAngleWidth = OmenTester.DefaultConeAngle;
    public float TestLineLength = 20f;
    public float TestLineWidth = 5f;
    public float TestLineRotation = 0f;
    public float TestDonutOuterRadius = 10f;
    public float TestDonutRatio = 0.5f;
    public string TestCustomOmen = "general_1bf";
    public float TestCustomScale = 3f;

    public class DutyEntries
    {
        public bool Enabled = true;
        public Vector4 OmenColor = Colors.NoTint;
        public ushort DutyId;
        public ushort TerritoryTypeId;
        public string Name = "";
    }

    public bool HasDefaultColors =>
        GlobalColor == Colors.NoTint &&
        NonHostileColor == Colors.NoTint &&
        Alpha == Colors.DefaultAlpha &&
        Speed == Colors.DefaultSpeed &&
        !RainbowMode && !WhiteMode && !NonHostile;

    public DutyEntries? FindDuty(uint territory)
    {
        foreach (var entry in DutyColors.Values)
        {
            if (entry.Enabled && entry.TerritoryTypeId == territory)
                return entry;
        }
        return null;
    }

    public void SetColor(Vector4 color)
    {
        GlobalColor = Colors.WithAlpha(color, 1f);
        RainbowMode = false;
        WhiteMode = false;
    }

    public void SetRainbowMode(bool enabled)
    {
        RainbowMode = enabled;
        if (enabled)
            WhiteMode = false;
    }

    public void SetWhiteMode(bool enabled)
    {
        WhiteMode = enabled;
        if (enabled)
            RainbowMode = false;
    }

    public void ResetColors()
    {
        GlobalColor = Colors.NoTint;
        NonHostileColor = Colors.NoTint;
        Alpha = Colors.DefaultAlpha;
        Speed = Colors.DefaultSpeed;
        RainbowMode = false;
        WhiteMode = false;
        NonHostile = false;
    }

    public void Save() => Svc.PluginInterface.SavePluginConfig(this);

    public static Config Load()
    {
        var config = Svc.PluginInterface.GetPluginConfig() as Config ?? new Config();
        if (config.Version < CurrentVersion)
        {
            config.Migrate();
            config.Save();
        }
        return config;
    }

    private void Migrate()
    {
        if (Alpha < Colors.MinAlpha)
            Alpha = Colors.DefaultAlpha;
        if (Colors.IsOverbright(GlobalColor))
            GlobalColor = Colors.NoTint;
        if (Colors.IsOverbright(NonHostileColor))
            NonHostileColor = Colors.NoTint;
        if (!OmenTester.ConeAngles.Contains(TestConeAngleWidth))
            TestConeAngleWidth = OmenTester.DefaultConeAngle;

        GlobalColor = Colors.WithAlpha(GlobalColor, 1f);
        NonHostileColor = Colors.WithAlpha(NonHostileColor, 1f);
        Version = CurrentVersion;
    }
}
