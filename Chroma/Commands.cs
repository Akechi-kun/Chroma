using Chroma.Core;
using Dalamud.Game.Command;
using ECommons.DalamudServices;
using System;
using System.Numerics;

namespace Chroma;

public class Commands(Main ui, Manager mgr, Config cfg) : IDisposable
{
    private const string Name = "/chroma";
    private readonly Main _ui = ui;
    private readonly Manager _mgr = mgr;
    private readonly Config _cfg = cfg;

    public void Register()
    {
        Svc.Commands.AddHandler(Name, new CommandInfo(HandleCommand)
        {
            HelpMessage = "Toggles Chroma menu. \n" +
            "/chroma duties -> Toggles Duty Overrides menu.\n" +
            "/chroma enable -> Enables Chroma. \n" +
            "/chroma disable -> Disables Chroma. \n" +
            "/chroma friendly enable -> Enables Non-Hostile omens.\n" +
            "/chroma friendly disable -> Disables Non-Hostile omens.\n" +
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
                _mgr.Enabled = true;
                Svc.Log.Information("Chroma enabled.");
                break;

            case "disable":
                _mgr.Enabled = false;
                _cfg.RainbowMode = false;
                Svc.Log.Information("Chroma disabled.");
                break;

            case "red":
                SetColor(1f, 0f, 0f);
                Svc.Log.Information("Global color set to red.");
                break;

            case "green":
                SetColor(0f, 1f, 0f);
                Svc.Log.Information("Global color set to green.");
                break;

            case "blue":
                SetColor(0f, 0f, 1f);
                Svc.Log.Information("Global color set to blue.");
                break;

            case "yellow":
                SetColor(1f, 1f, 0f);
                Svc.Log.Information("Global color set to yellow.");
                break;

            case "purple":
                SetColor(0.5f, 0f, 0.5f);
                Svc.Log.Information("Global color set to purple.");
                break;

            case "pink":
                SetColor(1f, 0.75f, 0.8f);
                Svc.Log.Information("Global color set to pink.");
                break;

            case "white":
                SetColor(1f, 1f, 1f);
                Svc.Log.Information("Global color set to white.");
                break;

            case "rainbow":
                _mgr.Enabled = true;
                _cfg.RainbowMode = true;
                Svc.Log.Information("Rainbow mode enabled.");
                break;

            case "duties":
                _ui.ToggleDuty();
                break;

            case "friendly enable":
                _cfg.NonHostile = true;
                Svc.Log.Information($"Non-Hostile omens enabled.");
                break;

            case "friendly disable":
                _cfg.NonHostile = false;
                Svc.Log.Information($"Non-Hostile omens disabled.");
                break;

            case "":
            case null:
                _ui.ToggleMain();
                break;
        }
    }

    private void SetColor(float r, float g, float b)
    {
        _mgr.Enabled = true;
        _cfg.RainbowMode = false;
        _cfg.GlobalColor = new Vector4(r, g, b, 1f);
    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler(Name);
        GC.SuppressFinalize(this);
    }
}
