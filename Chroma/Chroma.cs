using Chroma.Omens;
using Chroma.Windows;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using Pictomancy;

namespace Chroma;

public sealed class Chroma : IDalamudPlugin
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly PctContext _pictomancy;
    private readonly Config _config;
    private readonly OmenHook _omenHook;
    private readonly OmenTester _omenTester;
    private readonly OmenUpdater _omenUpdater;
    private readonly WindowSystem _windowSystem = new("Chroma");
    private readonly MainWindow _mainWindow;
    private readonly DutyWindow _dutyWindow;
    private readonly Commands _commands;

    public Chroma(IDalamudPluginInterface pluginInterface)
    {
        _pluginInterface = pluginInterface;
        ECommonsMain.Init(pluginInterface, this);
        _pictomancy = PctService.Initialize(pluginInterface, new PctOptions { EnableDxRenderer = false, EnableKtkOutput = false });

        _config = Config.Load();
        _omenHook = new OmenHook(_config);
        _omenUpdater = new OmenUpdater(_config, _omenHook);
        _omenTester = new OmenTester(_config);

        _dutyWindow = new DutyWindow(_config);
        _mainWindow = new MainWindow(_config, _omenHook, _omenTester, _dutyWindow);
        RegisterWindows();

        _commands = new Commands(_config, _omenHook, _mainWindow, _dutyWindow);
    }

    public void Dispose()
    {
        _commands.Dispose();
        UnregisterWindows();
        _omenTester.Dispose();
        _omenUpdater.Dispose();
        _omenHook.Dispose();
        _config.Save();
        _pictomancy.Dispose();
        ECommonsMain.Dispose();
    }

    private void RegisterWindows()
    {
        _windowSystem.AddWindow(_mainWindow);
        _windowSystem.AddWindow(_dutyWindow);
        _pluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        _pluginInterface.UiBuilder.OpenMainUi += _mainWindow.Toggle;
        _pluginInterface.UiBuilder.OpenConfigUi += _mainWindow.Toggle;
    }

    private void UnregisterWindows()
    {
        _pluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        _pluginInterface.UiBuilder.OpenMainUi -= _mainWindow.Toggle;
        _pluginInterface.UiBuilder.OpenConfigUi -= _mainWindow.Toggle;
        _windowSystem.RemoveAllWindows();
    }
}
