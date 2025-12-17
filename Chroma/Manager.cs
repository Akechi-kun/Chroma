using Dalamud.Plugin.Services;
using SourOmen.Structs;
using System;

namespace Chroma;

public class Manager(Config config, Util Util, IObjectTable objectTable, IClientState clientState) : IDisposable
{
    private readonly Config _config = config;
    private readonly Util _util = Util;
    private readonly IObjectTable _objectTable = objectTable;
	private readonly IClientState _clientState = clientState;

    public bool Enabled
    {
        get => _util.Enabled;
        set
        {
            _config.Enabled = value;
            _util.SetEnabled(value);
        }
    }

    public unsafe void Initialize()
    {
        _util.SetEnabled(_config.Enabled);
        _util.OnSpawn += OnSpawn;
    }

    private unsafe void OnSpawn(VfxResourceInstance* instance)
    {
        if (!_config.Enabled)
        {
            return;
        }

        var territory = _clientState.TerritoryType;
        var color = _config.GetActiveColor(territory);
        instance->Color = color;
    }

    public void Dispose()
    {
        _util.Dispose();
        GC.SuppressFinalize(this);
    }
}