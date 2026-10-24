using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using SourOmen.Structs;
using System;
using System.Numerics;

namespace Chroma.Omens;

public sealed unsafe class OmenHook : IDisposable
{
    private const string CreateOmenSignature = "E8 ?? ?? ?? ?? 48 89 84 FB ?? ?? ?? ?? 48 85 C0 74 53";

    private delegate VfxData* CreateOmenDelegate(uint a1, nint a2, nint a3, float a4, int a5, int a6, float a7, int a8, byte isEnemy, byte a10);

    [Signature(CreateOmenSignature, DetourName = nameof(CreateOmenDetour))]
    private readonly Hook<CreateOmenDelegate> _createOmenHook = null!;

    private readonly Config _config;

    public OmenHook(Config config)
    {
        _config = config;
        Svc.Hook.InitializeFromAttributes(this);
        Enabled = config.Enabled;
    }

    public bool Enabled
    {
        get => _createOmenHook.IsEnabled;
        set
        {
            _config.Enabled = value;
            if (value)
                _createOmenHook.Enable();
            else
                _createOmenHook.Disable();
        }
    }

    public delegate void OmenRecoloredHandler(nint omen, bool hostile, Vector4 color);

    public event OmenRecoloredHandler? OmenRecolored;

    public void Dispose() => _createOmenHook.Dispose();

    private VfxData* CreateOmenDetour(uint a1, nint a2, nint a3, float a4, int a5, int a6, float a7, int a8, byte isEnemy, byte a10)
    {
        var omen = _createOmenHook.Original(a1, a2, a3, a4, a5, a6, a7, a8, isEnemy, a10);
        try
        {
            Recolor(omen, hostile: isEnemy == 1);
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Failed to recolor omen.");
        }
        return omen;
    }

    private void Recolor(VfxData* omen, bool hostile)
    {
        if (omen == null || omen->Instance == null || (!hostile && !_config.NonHostile))
            return;

        var color = Colors.GetOmenColor(_config, hostile, Svc.ClientState.TerritoryType);
        omen->Instance->Color = color;
        OmenRecolored?.Invoke((nint)omen, hostile, color);
    }
}
