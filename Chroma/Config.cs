using Dalamud.Configuration;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Chroma;

public class Config : IPluginConfiguration
{
    public bool Enabled = true;
    public Vector4 OmenColor = new(1.0f);
    public bool IncludeFriendly = false;
    public bool RainbowMode = false;
    public float Speed = 0.05f;
    public Dictionary<ushort, DutyEntries> DutyColors = [];
    public bool TestOmenEnabled = false;
    public float TestCircleRadius = 1f;
    public bool TestCircleActive = false;
    public bool TestConeActive = false;
    public float TestConeRadius = 1f;
    public float TestConeRotation = 1f;
    public int TestConeAngleWidth = 1;
    public bool TestLineActive = false;
    public float TestLineLength = 1f;
    public float TestLineWidth = 1f;
    public float TestLineRotation = 1f;
    public bool TestDonutActive = false;
    public float TestDonutInnerRadius = 1f;
    public float TestDonutOuterRadius = 1f;
    public bool TestCustomActive = false;
    //public bool TestLockOnActive = false;
    //public float TestLockOnScale = 1f;

    public int Version { get; set; } = 1;
    public int SelectedOmenIndex { get; set; } = 0;
    public class DutyEntries
    {
        public bool Enabled = true;
        public Vector4 OmenColor = new(1.0f);
        public ushort DutyId;
        public ushort TerritoryTypeId;
        public string Name = "";
    }

    public Vector4 GetActiveColor(ushort territoryType)
    {
        foreach (var entries in DutyColors)
        {
            var entry = entries.Value;
            if (entry.Enabled && entry.TerritoryTypeId == territoryType)
            {
                return entry.OmenColor;
            }
        }

        return OmenColor;
    }
}
public class ConfigManager : IDisposable
{
    private readonly IUiBuilder _ui;
    private readonly WindowSystem _windows = new();
    private readonly ConfigWindow _cfgWindow;
    private readonly DutyWindow _dutyWindow;

    public ConfigManager(ConfigWindow cfgWindow, DutyWindow dutyWindow, IUiBuilder ui)
    {
        _cfgWindow = cfgWindow;
        _dutyWindow = dutyWindow;
        _windows.AddWindow(_cfgWindow);
        _windows.AddWindow(_dutyWindow);
        _ui = ui;
        _ui.Draw += _windows.Draw;
        _ui.OpenMainUi += ToggleChromaWindow;
        _ui.OpenConfigUi += ToggleChromaWindow;
    }

    public void ToggleChromaWindow()
    {
        _cfgWindow.Toggle();
    }

    public void ToggleDutyWindow()
    {
        _dutyWindow.Toggle();
    }

    public void Dispose()
    {
        _ui.Draw -= _windows.Draw;
        _ui.OpenMainUi -= ToggleChromaWindow;
        _ui.OpenConfigUi -= ToggleDutyWindow;
        GC.SuppressFinalize(this);
    }
}

