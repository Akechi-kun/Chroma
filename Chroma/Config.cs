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
    public bool Enabled = true;
    public float Alpha = 0f;
    public Vector4 GlobalColor = new(1f, 1f, 1f, 1f);
    public bool NonHostile = false;
    public Vector4 NonHostileColor = new(1f, 1f, 1f, 1f);
    public bool RainbowMode = false;
    public bool WhiteMode = false;
    public float Speed = 0.05f;
    public Dictionary<ushort, DutyEntries> DutyColors = [];
    public bool TestingEnabled = false;
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

