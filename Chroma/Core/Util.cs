using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using SourOmen.Structs;
using System;

namespace Chroma.Core;

public class Util : IDisposable
{
    public bool Enabled => CreateHook.IsEnabled;
    private readonly Config _cfg;
    public unsafe delegate void VfxSpawnEvent(VfxResourceInstance* instance, bool isEnemy);
    public event VfxSpawnEvent? OnSpawn;

    [Signature("E8 ?? ?? ?? ?? 48 89 84 FB ?? ?? ?? ?? 48 85 C0 74 53", DetourName = nameof(CreateDetour))]
    private readonly Hook<CreateDelegate> CreateHook = null!;
    private unsafe delegate VfxData* CreateDelegate(uint a1, nint a2, nint a3, float a4, int a5, int a6, float a7, int a8, char isEnemy, char a10);

    public Util(IGameInteropProvider interop, Config config)
    {
        interop.InitializeFromAttributes(this);
        _cfg = config;
    }

    public void SetEnabled(bool enable)
    {
        if (enable)
            CreateHook.Enable();
        else
            CreateHook.Disable();
    }

    private unsafe VfxData* CreateDetour(uint a1, nint a2, nint a3, float a4, int a5, int a6, float a7, int a8, char isEnemy, char a10)
    {
        var vfx = CreateHook.Original(a1, a2, a3, a4, a5, a6, a7, a8, isEnemy, a10);
        if (vfx != null)
        {
            var instance = vfx->Instance;
            if (instance != null)
            {
                OnSpawn?.Invoke(instance, isEnemy == 1);
            }
        }
        return vfx;
    }

    public void Dispose()
    {
        CreateHook.Disable();
        CreateHook.Dispose();
        GC.SuppressFinalize(this);
    }
}