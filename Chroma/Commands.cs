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
        switch (args)
        {
            case "enable":
                _manager.Enabled = true;
                break;

            case "disable":
                _manager.Enabled = false;
                _config.RainbowMode = false;
                break;

            case "duties":
                _ui.ToggleDutyWindow();
                break;

            case "red":
                SetColor(1f, 0f, 0f);
                break;

            case "green":
                SetColor(0f, 1f, 0f);
                break;

            case "blue":
                SetColor(0f, 0f, 1f);
                break;

            case "yellow":
                SetColor(1f, 1f, 0f);
                break;

            case "purple":
                SetColor(0.5f, 0f, 0.5f);
                break;

            case "pink":
                SetColor(1f, 0.75f, 0.8f);
                break;

            case "white":
                SetColor(1f, 1f, 1f);
                break;

            case "rainbow":
                _manager.Enabled = true;
                _config.RainbowMode = true;
                break;

            case "":
            case null:
                _ui.ToggleChromaWindow();
                break;
        }
    }

    private void SetColor(float r, float g, float b)
    {
        _manager.Enabled = true;
        _config.RainbowMode = false;
        _config.OmenColor = new Vector4(r, g, b, 1f);
    }

    public void Dispose()
    {
        _cmds.RemoveHandler(CommandName);
        GC.SuppressFinalize(this);
    }
}
