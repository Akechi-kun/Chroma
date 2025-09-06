using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Chroma;

//Structs
[StructLayout(LayoutKind.Explicit)]
public struct VFXResource
{
    [FieldOffset(0xA0)] public Vector4 Color;
}

[StructLayout(LayoutKind.Explicit)]
public struct VFXData
{
    [FieldOffset(0x1B8)] public unsafe VFXResource* Instance;
}

//Hooks
public class Hooks : IDisposable
{
    public bool Enabled => CreateHook.IsEnabled;

    public event VFXSpawnEvent? OnSpawn;
    public unsafe delegate void VFXSpawnEvent(VFXResource* instance);

    [Signature("E8 ?? ?? ?? ?? 48 89 84 FB ?? ?? ?? ?? 48 85 C0 74 53", DetourName = nameof(CreateDetour))]
    private readonly Hook<CreateDelegate> CreateHook = null!;
    private unsafe delegate VFXData* CreateDelegate(uint a1, nint a2, nint a3, float a4, int a5, int a6, float a7, int a8, char isEnemy, char a10);

    public Hooks(IGameInteropProvider interop) => interop.InitializeFromAttributes(this);

    public void SetEnabled(bool enable)
    {
        if (enable)
        {
            CreateHook.Enable();
        }
        else
        {
            CreateHook.Disable();
        }
    }

    private unsafe VFXData* CreateDetour(uint a1, nint a2, nint a3, float a4, int a5, int a6, float a7, int a8, char isEnemy, char a10)
    {
        VFXData* VFX = CreateHook.Original(a1, a2, a3, a4, a5, a6, a7, a8, isEnemy, a10);
        if (isEnemy == 1 && VFX != null)
        {
            VFXResource* instance = VFX->Instance;
            if (instance != null)
            {
                OnSpawn?.Invoke(instance);
            }
        }
        return VFX;
    }

    public void Dispose()
    {
        CreateHook.Disable();
        CreateHook.Dispose();
        GC.SuppressFinalize(this);
    }
}
