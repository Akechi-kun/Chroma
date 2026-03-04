using Chroma.UI;
using Dalamud.Configuration;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Chroma;

public class Config : IPluginConfiguration
{
    public bool Enabled = true; //global enable/disable for plugin
    public Vector4 GlobalColor = new(1.0f); //global color
    public bool NonHostile = false; //option select to include Non-Hostile omens in the color override
    public Vector4 NonHostileColor = new(1.0f); //friendly color will be global color by default
    public bool RainbowMode = false; //rainbow mode for omens - basically just cycles through the hue spectrum
    public float Speed = 0.05f; //default speed of the hue change while in rainbow mode
    public Dictionary<ushort, DutyEntries> DutyColors = []; //dict for duty specific colors - key for duty's territoryid
    public bool TestingEnabled = false; //testing options for pre-rendering omens in the config menu
    public float TestCircleRadius = 1f; //default radius for circle omen test render
    public bool TestCircleActive = false; //testing circle omen is currently active
    public bool TestConeActive = false; //testing cone omen is currently active
    public float TestConeRadius = 1f; //default radius for cone omen test render
    public float TestConeRotation = 1f; //default rotation for cone omen test render
    public int TestConeAngleWidth = 1; //default angle width for cone omen test render
    public bool TestLineActive = false; //testing line omen is currently active
    public float TestLineLength = 1f; //default length for line omen test render
    public float TestLineWidth = 1f; //default width for line omen test render
    public float TestLineRotation = 1f; //default rotation for line omen test render
    public bool TestDonutActive = false; //testing donut omen is currently active
    public float TestDonutInnerRadius = 1f; //default inner radius for donut omen test render 
    public float TestDonutOuterRadius = 1f; //default outer radius for donut omen test render
    public bool TestCustomActive = false; //testing custom omen is currently active
    public bool ExtraRender = false; //option select for increasing thickness with ImGui
    public float Thickness = 0.1f; //default thickness for ImGui rendering when ExtraRender is enabled

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

        return GlobalColor;
    }
}
public class Main : IDisposable
{
    private readonly IUiBuilder _ui;
    private readonly WindowSystem _windows = new();
    private readonly ChromaWindow _cfgWindow;
    private readonly DutyWindow _dutyWindow;

    public Main(ChromaWindow cfgWindow, DutyWindow dutyWindow, IUiBuilder ui)
    {
        _cfgWindow = cfgWindow;
        _dutyWindow = dutyWindow;
        _windows.AddWindow(_cfgWindow);
        _windows.AddWindow(_dutyWindow);
        _ui = ui;
        _ui.Draw += _windows.Draw;
        _ui.OpenMainUi += ToggleMain;
        _ui.OpenConfigUi += ToggleMain;
    }

    public void ToggleMain() => _cfgWindow.Toggle();
    public void ToggleDuty() => _dutyWindow.Toggle();

    public void Dispose()
    {
        _ui.Draw -= _windows.Draw;
        _ui.OpenMainUi -= ToggleMain;
        _ui.OpenConfigUi -= ToggleDuty;
        GC.SuppressFinalize(this);
    }
}

