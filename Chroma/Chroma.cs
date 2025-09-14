using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Pictomancy;
using System.Numerics;

namespace Chroma;

public sealed class Chroma : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IFramework _framework;
    private readonly Config _config;
    private readonly Manager _manager;
    private readonly ConfigManager _ui;
    private readonly Commands _cmds;

    public Chroma(IDalamudPluginInterface pluginInterface, IFramework framework, IGameInteropProvider interop, ICommandManager cmds, IDataManager data, IClientState clientState, IPluginLog log)
    {
        PictoService.Initialize(pluginInterface);
        _framework = framework;
        _config = pluginInterface.GetPluginConfig() as Config ?? new Config();
        Util Util = new(interop, _config);
        _pluginInterface = pluginInterface;
        _manager = new Manager(_config, Util, clientState);
        _manager.Initialize();
        DutyOverride dutyOverride = new(data);
        DutyWindow dutyWindow = new(pluginInterface, _config, dutyOverride, clientState, log);
        ConfigWindow cfgWindow = new(pluginInterface, _config, _manager, dutyWindow, clientState, data, log);
        _ui = new ConfigManager(cfgWindow, dutyWindow, pluginInterface.UiBuilder);
        _cmds = new Commands(cmds, _ui, _manager, _config);
        _cmds.Register();
        _framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        if (_config.RainbowMode)
        {
            var a = _config.OmenColor.W;
            var t = (float)ImGui.GetTime();
            var hue = (t * _config.Speed) % 1.0f;
            var rgb = HsvToRgb(hue, 1.0f, 1.0f);
            rgb.W = a;
            _config.OmenColor = rgb;
        }
    }

    public void Dispose()
    {
        PictoService.Dispose();
        _manager.Dispose();
        _ui.Dispose();
        _cmds.Dispose();
        _pluginInterface.SavePluginConfig(_config);
        _framework.Update -= OnUpdate;
    }

    private static Vector4 HsvToRgb(float h, float s, float v)
    {
        var i = (int)(h * 6);
        var f = h * 6 - i;
        var p = v * (1 - s);
        var q = v * (1 - f * s);
        var t = v * (1 - (1 - f) * s);

        return (i % 6) switch
        {
            0 => new Vector4(v, t, p, 1),
            1 => new Vector4(q, v, p, 1),
            2 => new Vector4(p, v, t, 1),
            3 => new Vector4(p, q, v, 1),
            4 => new Vector4(t, p, v, 1),
            _ => new Vector4(v, p, q, 1),
        };
    }
}