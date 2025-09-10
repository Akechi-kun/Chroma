using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Pictomancy;

namespace Chroma;

public sealed class Chroma : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Config _config;
    private readonly Manager _manager;
    private readonly ConfigManager _ui;
    private readonly Commands _cmds;

    public Chroma(IDalamudPluginInterface pluginInterface, IGameInteropProvider interop, ICommandManager cmds, IDataManager data, IClientState clientState, IPluginLog log)
    {
        PictoService.Initialize(pluginInterface);
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
    }

    public void Dispose()
    {
        PictoService.Dispose();
        _manager.Dispose();
        _ui.Dispose();
        _cmds.Dispose();
        _pluginInterface.SavePluginConfig(_config);
    }
}