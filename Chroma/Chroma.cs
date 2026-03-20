using Chroma.Core;
using Chroma.UI;
using Dalamud.Bindings.ImGui;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using Pictomancy;
using System.Numerics;

namespace Chroma;

public sealed class Chroma : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    private readonly Config _cfg;
    private readonly Manager _mgr;
    private readonly Commands _cmds;
    private readonly Main _ui;

    public Chroma(IGameInteropProvider interop)
    {
        ECommonsMain.Init(PluginInterface, this);
        PictoService.Initialize(PluginInterface);
        _cfg = PluginInterface.GetPluginConfig() as Config ?? new Config();
        Util Util = new(interop, _cfg);
        _mgr = new Manager(_cfg, Util);
        _mgr.Initialize();
        DutyOverride dutyOverride = new();
        DutyWindow dutyWindow = new(_cfg, dutyOverride);
        ChromaWindow cfgWindow = new(_cfg, _mgr, dutyWindow);
        _ui = new Main(cfgWindow, dutyWindow, PluginInterface.UiBuilder);
        _cmds = new Commands(_ui, _mgr, _cfg);
        _cmds.Register();
        Svc.Framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework framework)
    {
        if (!_cfg.Enabled && !_cfg.TestingEnabled)
            return;

        if (!_cfg.WhiteMode)
        {
            _cfg.GlobalColor.W = _cfg.Alpha;
        }

        if (_cfg.RainbowMode)
        {
            var t = (float)ImGui.GetTime();
            var up = (t * _cfg.Speed) % 1.0f;
            var down = (up + 0.5f) % 1.0f;
            var h = RGB(up, 1.0f, 1.0f);
            h.W = _cfg.Alpha;
            _cfg.GlobalColor = h;

            var nh = RGB(down, 1.0f, 1.0f);
            nh.W = _cfg.Alpha;
            _cfg.NonHostileColor = nh;
        }
        else if (_cfg.WhiteMode)
        {
            var white = new Vector4(10f, 10f, 10f, 10f);
            _cfg.GlobalColor = white;

            if (_cfg.NonHostile)
            {
                _cfg.NonHostileColor = white;
            }
        }
    }
    public void Dispose()
    {
        ECommonsMain.Dispose();
        PictoService.Dispose();
        _mgr.Dispose();
        _ui.Dispose();
        PluginInterface.SavePluginConfig(_cfg);
        Svc.Framework.Update -= OnUpdate;
    }

    private static Vector4 RGB(float h, float s, float v)
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