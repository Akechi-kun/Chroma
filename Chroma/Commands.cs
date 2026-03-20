using Chroma.Core;
using Dalamud.Game.Command;
using ECommons.DalamudServices;
using System;
using System.Linq;
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
            "/chroma enable -> Enables Chroma. \n" +
            "/chroma disable -> Disables Chroma. \n" +
            "/chroma duties -> Toggles Duty Overrides menu.\n" +
            "/chroma friendly enable -> Enables Non-Hostile omens.\n" +
            "/chroma friendly disable -> Disables Non-Hostile omens.\n" +
            "/chroma red -> Sets global omen color to red. \n" +
            "/chroma orange -> Sets global omen color to orange. \n" +
            "/chroma yellow -> Sets global omen color to yellow. \n" +
            "/chroma green -> Sets global omen color to green. \n" +
            "/chroma blue -> Sets global omen color to blue. \n" +
            "/chroma purple -> Sets global omen color to purple. \n" +
            "/chroma pink -> Sets global omen color to pink. \n" +
            "/chroma black -> Sets global omen color to black.\n" +
            "/chroma white -> Enables White Mode.\n" +
            "/chroma rainbow -> Enables Rainbow Mode."
        });
    }

    private void HandleCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            _ui.ToggleMain();
            return;
        }

        var arguments = args.ToLowerInvariant();
        var presets = new (string Name, Vector3 Color)[]
        {
            ("red",    new Vector3(1f, 0f, 0f)),
            ("orange", new Vector3(1f, 0.5f, 0f)),
            ("yellow", new Vector3(1f, 1f, 0f)),
            ("green",  new Vector3(0f, 1f, 0f)),
            ("blue",   new Vector3(0f, 0.12f, 1f)),
            ("purple", new Vector3(0.2f, 0f, 1f)),
            ("pink",   new Vector3(0.8f, 0.25f, 1f)),
            ("black",  new Vector3(0f, 0f, 0f))
        };

        var matched = presets.FirstOrDefault(c => c.Name == arguments);
        if (matched != default)
        {
            SetColor(matched.Color);
            Svc.Log.Information($"Global omen color set to {arguments}.");
            return;
        }

        switch (arguments)
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

            case "white":
                _mgr.Enabled = true;
                _cfg.WhiteMode = true;
                _cfg.RainbowMode = false;
                Svc.Log.Information("White mode enabled.");
                break;

            case "rainbow":
                _mgr.Enabled = true;
                _cfg.RainbowMode = true;
                _cfg.WhiteMode = false;
                Svc.Log.Information("Rainbow mode enabled.");
                break;

            case "duties":
                _ui.ToggleDuty();
                break;

            case "friendly enable":
                _cfg.NonHostile = true;
                Svc.Log.Information("Non-Hostile omens enabled.");
                break;

            case "friendly disable":
                _cfg.NonHostile = false;
                Svc.Log.Information("Non-Hostile omens disabled.");
                break;

            default:
                Svc.Log.Warning($"Unknown command: {args}");
                break;
        }
    }

    private void SetColor(Vector3 rgb)
    {
        _mgr.Enabled = true;
        _cfg.RainbowMode = false;
        _cfg.WhiteMode = false;
        _cfg.GlobalColor = new Vector4(rgb, _cfg.Alpha);
    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler(Name);
        GC.SuppressFinalize(this);
    }
}