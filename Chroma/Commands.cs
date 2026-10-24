using Chroma.Omens;
using Chroma.Windows;
using Dalamud.Game.Command;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using System;
using System.Globalization;

namespace Chroma;

public sealed class Commands : IDisposable
{
    private const string CommandName = "/chroma";
    private const string HelpMessage =
        "Toggles Chroma menu.\n" +
        "/chroma on | off | toggle → Enables, disables or toggles Chroma.\n" +
        "/chroma duties → Toggles Duty Overrides menu.\n" +
        "/chroma friendly [on | off] → Enables, disables or toggles Non-Hostile omens.\n" +
        "/chroma warning [on | off] → Enables, disables or toggles Cast Warning.\n" +
        "/chroma <red | orange | yellow | green | blue | purple | pink | black> → Sets global omen color to a preset.\n" +
        "/chroma #RRGGBB → Sets global omen color to a hex color.\n" +
        "/chroma alpha <0.2-10> → Sets omen Alpha (color intensity).\n" +
        "/chroma white → Enables White Mode.\n" +
        "/chroma rainbow → Enables Rainbow Mode.\n" +
        "/chroma reset → Resets all colors and modes to default.";

    private readonly Config _config;
    private readonly OmenHook _omenHook;
    private readonly MainWindow _mainWindow;
    private readonly DutyWindow _dutyWindow;

    public Commands(Config config, OmenHook omenHook, MainWindow mainWindow, DutyWindow dutyWindow)
    {
        _config = config;
        _omenHook = omenHook;
        _mainWindow = mainWindow;
        _dutyWindow = dutyWindow;
        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = HelpMessage });
    }

    public void Dispose() => Svc.Commands.RemoveHandler(CommandName);

    private void OnCommand(string command, string arguments)
    {
        var (subcommand, value) = SplitArguments(arguments);
        switch (subcommand)
        {
            case "":
                _mainWindow.Toggle();
                return;
            case "duties":
                _dutyWindow.Toggle();
                return;
        }

        Print(Run(subcommand, value));
        _config.Save();
    }

    private string Run(string subcommand, string value) => subcommand switch
    {
        "on" or "enable" => SetEnabled(true),
        "off" or "disable" => SetEnabled(false),
        "toggle" => SetEnabled(!_omenHook.Enabled),
        "friendly" => SetNonHostile(value),
        "warning" => SetCastProgress(value),
        "white" => EnableWhiteMode(),
        "rainbow" => EnableRainbowMode(),
        "alpha" => SetAlpha(value),
        "reset" => ResetColors(),
        _ => SetColor(subcommand),
    };

    private string SetEnabled(bool enabled)
    {
        _omenHook.Enabled = enabled;
        return $"Chroma {EnabledText(enabled)}.";
    }

    private string SetNonHostile(string value)
    {
        _config.NonHostile = ParseToggle(value, _config.NonHostile);
        return $"Non-Hostile omens {EnabledText(_config.NonHostile)}.";
    }

    private string SetCastProgress(string value)
    {
        _config.CastProgress = ParseToggle(value, _config.CastProgress);
        return $"Cast Warning {EnabledText(_config.CastProgress)}.";
    }

    private string EnableWhiteMode()
    {
        _omenHook.Enabled = true;
        _config.SetWhiteMode(true);
        return Toast($"White Mode enabled. {Colors.WhiteModeWarning}");
    }

    private string EnableRainbowMode()
    {
        _omenHook.Enabled = true;
        _config.SetRainbowMode(true);
        return Toast("Rainbow Mode enabled.");
    }

    private string SetAlpha(string value)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var alpha))
            return $"Alpha is currently {_config.Alpha:0.0}. Usage: /chroma alpha <0.2-10>";

        _config.Alpha = Math.Clamp(alpha, Colors.MinAlpha, Colors.MaxAlpha);
        return Colors.IsLowAlpha(_config)
            ? $"Alpha set to {_config.Alpha:0.0}. {Colors.LowAlphaWarning}"
            : $"Alpha set to {_config.Alpha:0.0}.";
    }

    private string ResetColors()
    {
        _config.ResetColors();
        return Toast("All colors and modes have been reset to default.");
    }

    private string SetColor(string text)
    {
        if (!Colors.TryParse(text, out var name, out var color))
            return $"Unknown command: {text}. Type /xlhelp to see all Chroma commands.";

        _omenHook.Enabled = true;
        _config.SetColor(color);
        return Toast($"Global omen color set to {name}.");
    }

    private static (string Subcommand, string Value) SplitArguments(string arguments)
    {
        var parts = arguments.ToLowerInvariant().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var subcommand = parts.Length > 0 ? parts[0] : string.Empty;
        var value = parts.Length > 1 ? parts[1] : string.Empty;
        return (subcommand, value);
    }

    private static bool ParseToggle(string value, bool current) => value switch
    {
        "on" or "enable" => true,
        "off" or "disable" => false,
        _ => !current,
    };

    private static string EnabledText(bool enabled) => enabled ? "enabled" : "disabled";

    private static string Toast(string message)
    {
        Notify.Info(message);
        return message;
    }

    private static void Print(string message) => Svc.Chat.Print(message, "Chroma");
}
