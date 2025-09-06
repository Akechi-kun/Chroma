using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using System;
using System.Numerics;

namespace Chroma;

public class Manager(Config config, Hooks hooks) : IDisposable
{
    private readonly Config _config = config;
    private readonly Hooks _hooks = hooks;

    public bool Enabled
    {
        get => _hooks.Enabled;
        set
        {
            _config.Enabled = value;
            _hooks.SetEnabled(value);
        }
    }

    public unsafe void Initialize()
    {
        _hooks.SetEnabled(_config.Enabled);
        _hooks.OnSpawn += OnSpawn;
    }

    private unsafe void OnSpawn(VFXResource* instance)
    {
        if (instance->Color.Equals(Vector4.One) && _config.Enabled)
        {
            instance->Color = _config.Color;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _hooks.Dispose();
    }
}

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
            "/chroma enable -> Enables Chroma \n" +
            "/chroma disable -> Disables Chroma \n" +
            "/chroma red -> Sets color to red \n" +
            "/chroma green -> Sets color to green \n" +
            "/chroma blue -> Sets color to blue \n" +
            "/chroma yellow -> Sets color to blue \n" +
            "/chroma purple -> Sets color to blue \n" +
            "/chroma pink -> Sets color to blue \n" +
            "/chroma rainbow -> Sets color to rainbow \n" +
            "/chroma white -> Sets color to white"
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
            return;
        }
        if (args == "red")
        {
            _manager.Enabled = true;
            _config.Color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            return;
        }
        if (args == "green")
        {
            _manager.Enabled = true;
            _config.Color = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            return;
        }
        if (args == "blue")
        {
            _manager.Enabled = true;
            _config.Color = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
            return;
        }
        if (args == "yellow")
        {
            _manager.Enabled = true;
            _config.Color = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            return;
        }
        if (args == "purple")
        {
            _manager.Enabled = true;
            _config.Color = new Vector4(0.5f, 0.0f, 0.5f, 1.0f);
            return;
        }
        if (args == "pink")
        {
            _manager.Enabled = true;
            _config.Color = new Vector4(1.0f, 0.75f, 0.8f, 1.0f);
            return;
        }
        if (args == "rainbow")
        {
            _manager.Enabled = true;
            _config.Color = new Vector4(1.0f, 0.5f, 0.0f, 1.0f);
            return;
        }
        if (args == "white")
        {
            _manager.Enabled = true;
            _config.Color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            return;
        }

        if (string.IsNullOrEmpty(args))
        {
            _ui.ToggleConfigWindow();
            return;
        }
    }
    public void Dispose()
    {
        _cmds.RemoveHandler(CommandName);
        GC.SuppressFinalize(this);
    }
}
