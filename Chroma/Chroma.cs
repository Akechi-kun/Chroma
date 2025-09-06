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

    public Chroma(IDalamudPluginInterface pluginInterface, IGameInteropProvider interop, ICommandManager cmd)
    {
        Hooks hooks = new Hooks(interop);

        _pluginInterface = pluginInterface;
        _config = pluginInterface.GetPluginConfig() as Config ?? new Config();
        _manager = new Manager(_config, hooks);
        _manager.Initialize();

        ConfigWindow cfgWindow = new ConfigWindow(_pluginInterface, _config, _manager);
        _ui = new ConfigManager(cfgWindow, pluginInterface.UiBuilder);
        _cmds = new Commands(cmd, _ui, _manager, _config);
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
