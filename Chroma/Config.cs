using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using System;
using System.Numerics;

namespace Chroma;

public class Config : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Enabled = true;
    public bool GlobalColor = false;
    public Vector4 Color = new(1.0f);
}
public class ConfigManager : IDisposable
{
    private readonly IUiBuilder _ui;
    private readonly WindowSystem _windows = new();
    private readonly ConfigWindow _cfgWindow;

    public ConfigManager(ConfigWindow cfgWindow, IUiBuilder ui)
    {
        _cfgWindow = cfgWindow;
        _windows.AddWindow(_cfgWindow);
        _ui = ui;
        _ui.Draw += _windows.Draw;
        _ui.OpenConfigUi += ToggleConfigWindow;
    }

    public void ToggleConfigWindow()
    {
        _cfgWindow.Toggle();
    }

    public void Dispose()
    {
        _ui.Draw -= _windows.Draw;
        _ui.OpenConfigUi -= ToggleConfigWindow;
        GC.SuppressFinalize(this);
    }
}
public class ConfigWindow : Window
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Config _config;
    private readonly Manager _manager;

    public ConfigWindow(IDalamudPluginInterface pluginInterface, Config config, Manager chromas)
        : base($"Chroma v{pluginInterface.Manifest.AssemblyVersion}")
    {
        _pluginInterface = pluginInterface;
        _config = config;
        _manager = chromas;
        Size = new Vector2(200, 120);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 120),
            MaximumSize = new Vector2(200, 120)
        };
        Flags |= ImGuiWindowFlags.NoResize;
    }

    public override void OnClose() => _pluginInterface.SavePluginConfig(_config);

    public override void Draw()
    {
        if (ImGui.Checkbox("Enable Chroma", ref _config.Enabled))
        {
            _manager.Enabled = _config.Enabled;
        }

        ImGui.ColorEdit4("Chroma Color", ref _config.Color, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
    }
}
