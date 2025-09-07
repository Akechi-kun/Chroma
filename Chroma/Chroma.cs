using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Chroma;

public sealed class Chroma : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Config _config;
    private readonly Manager _manager;
    private readonly ConfigManager _ui;
    private readonly Commands _cmds;

    public Chroma(IDalamudPluginInterface pluginInterface, IGameInteropProvider interop, ICommandManager cmds, IDataManager data, IClientState clientState)
    {
        Hooks hooks = new(interop);
        _pluginInterface = pluginInterface;
        _config = pluginInterface.GetPluginConfig() as Config ?? new Config();
        _manager = new Manager(_config, hooks, clientState);
        _manager.Initialize();
        DutyOverride dutyOverride = new(data);
        DutyWindow dutyWindow = new(pluginInterface, _config, dutyOverride, clientState);
        ConfigWindow cfgWindow = new(pluginInterface, _config, _manager, dutyWindow);
        _ui = new ConfigManager(cfgWindow, dutyWindow, pluginInterface.UiBuilder);
        _cmds = new Commands(cmds, _ui, _manager, _config);
        _cmds.Register();
    }

    public void Dispose()
    {
        _manager.Dispose();
        _ui.Dispose();
        _cmds.Dispose();
        _pluginInterface.SavePluginConfig(_config);
    }
}