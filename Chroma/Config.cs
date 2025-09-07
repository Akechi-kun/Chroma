using Dalamud.Configuration;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Chroma;

public class Config : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Enabled = true;
    public bool GlobalColor = false;
    public bool ShowDutyWindow = false;
    public bool RainbowMode = false;
    public Vector4 Color = new(1.0f);
    public Dictionary<ushort, DutyEntries> DutyColors = [];

    public class DutyEntries
    {
        public bool Enabled = true;
        public Vector4 Color = new(1.0f);
        public ushort DutyId;
        public ushort TerritoryTypeId;
        public string Name = "";
    }
    public Vector4 GetActiveColor(ushort territoryType)
    {
        foreach (KeyValuePair<ushort, DutyEntries> entries in DutyColors)
        {
            DutyEntries entry = entries.Value;
            if (entry.Enabled && entry.TerritoryTypeId == territoryType)
            {
                return entry.Color;
            }
        }

        return Color;
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
        _ui.OpenMainUi += ToggleMainWindow;
        _ui.OpenConfigUi += ToggleMainWindow;
    }

    public void ToggleMainWindow()
    {
        _cfgWindow.Toggle();
    }

    public void ToggleConfigWindow()
    {
        _dutyWindow.Toggle();
    }

    public void Dispose()
    {
        _ui.Draw -= _windows.Draw;
        _ui.OpenMainUi -= ToggleMainWindow;
        _ui.OpenConfigUi -= ToggleConfigWindow;
        GC.SuppressFinalize(this);
    }
}

