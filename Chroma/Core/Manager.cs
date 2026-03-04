using ECommons.DalamudServices;
using SourOmen.Structs;
using System;
using System.Linq;

namespace Chroma.Core;

public class Manager(Config config, Util util) : IDisposable
{
    private readonly Config _cfg = config;
    private readonly Util _util = util;

    public bool Enabled
    {
        get => _util.Enabled;
        set
        {
            _cfg.Enabled = value;
            _util.SetEnabled(value);
        }
    }

    public unsafe void Initialize()
    {
        _util.SetEnabled(_cfg.Enabled);
        _util.OnSpawn += OnVfxSpawn;
    }

    private unsafe void OnVfxSpawn(VfxResourceInstance* instance, bool isEnemy)
    {
        if (!_cfg.Enabled)
            return;

        var color = _cfg.GlobalColor;
        if (isEnemy)
        {
            var dutyOverride = _cfg.DutyColors.Values.FirstOrDefault(e => e.Enabled && e.TerritoryTypeId == Svc.ClientState.TerritoryType);
            color = dutyOverride != null ? dutyOverride.OmenColor : _cfg.GlobalColor;
        }
        else
        {
            color = _cfg.NonHostileColor;
        }

        instance->Color = color;
    }
    public void Dispose()
    {
        _util.Dispose();
        GC.SuppressFinalize(this);
    }
}