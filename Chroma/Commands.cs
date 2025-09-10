using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using System;
using System.Numerics;

namespace Chroma;

public class Commands(ICommandManager cmd, ConfigManager ui, Manager mgr, Config cfg) : IDisposable
{
    private const string CommandName = "/chroma";

    private readonly ICommandManager _cmds = cmd;
    private readonly ConfigManager _ui = ui;
    private readonly Manager _manager = mgr;
    private readonly Config _config = cfg;

    public void Register()
    {
        _cmds.AddHandler(CommandName, new CommandInfo(HandleCommand)
        {
            HelpMessage = "Opens the UI. \n" +
            "/chroma duties -> Opens the Duty Overrides menu.\n" +
            "/chroma enable -> Enables Chroma. \n" +
            "/chroma disable -> Disables Chroma. \n" +
            "/chroma red -> Sets global color to red. \n" +
            "/chroma green -> Sets global color to green. \n" +
            "/chroma blue -> Sets global color to blue. \n" +
            "/chroma yellow -> Sets global color to yellow. \n" +
            "/chroma purple -> Sets global color to purple. \n" +
            "/chroma pink -> Sets global color to pink. \n" +
            "/chroma white -> Sets global color to white.\n" +
            "/chroma rainbow -> Sets global color to rainbow."
        });
    }

    private void HandleCommand(string command, string args)
    {
        if (args == "enable")
        {
            _manager.Enabled = true;
            return;
        }

        if (args == "disable")
        {
            _manager.Enabled = false;
            _config.RainbowMode = false;
            return;
        }
        if (args == "duties")
        {
            _ui.ToggleDutyWindow();
            return;
        }
        if (args == "red")
        {
            _manager.Enabled = true;
            _config.RainbowMode = false;
            _config.OmenColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            return;
        }
        if (args == "green")
        {
            _manager.Enabled = true;
            _config.RainbowMode = false;
            _config.OmenColor = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            return;
        }
        if (args == "blue")
        {
            _manager.Enabled = true;
            _config.RainbowMode = false;
            _config.OmenColor = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
            return;
        }
        if (args == "yellow")
        {
            _manager.Enabled = true;
            _config.RainbowMode = false;
            _config.OmenColor = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            return;
        }
        if (args == "purple")
        {
            _manager.Enabled = true;
            _config.RainbowMode = false;
            _config.OmenColor = new Vector4(0.5f, 0.0f, 0.5f, 1.0f);
            return;
        }
        if (args == "pink")
        {
            _manager.Enabled = true;
            _config.RainbowMode = false;
            _config.OmenColor = new Vector4(1.0f, 0.75f, 0.8f, 1.0f);
            return;
        }
        if (args == "white")
        {
            _manager.Enabled = true;
            _config.RainbowMode = false;
            _config.OmenColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            return;
        }
        if (args == "rainbow")
        {
            _manager.Enabled = true;
            _config.RainbowMode = false;
            _config.RainbowMode = true;
            return;
        }

        if (string.IsNullOrEmpty(args))
        {
            _ui.ToggleChromaWindow();
            return;
        }
    }
    public void Dispose()
    {
        _cmds.RemoveHandler(CommandName);
        GC.SuppressFinalize(this);
    }
}
