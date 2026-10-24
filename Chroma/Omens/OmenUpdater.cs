using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using Pictomancy.VfxDraw;
using SourOmen.Structs;
using System;
using System.Collections.Generic;
using System.Numerics;
using NativeCharacter = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace Chroma.Omens;

public sealed unsafe class OmenUpdater : IDisposable
{
    private const int OmenSlot = 6;

    private record struct TrackedOmen(bool Hostile, Vector4 AppliedColor);

    private readonly Config _config;
    private readonly OmenHook _omenHook;
    private readonly Dictionary<nint, TrackedOmen> _trackedOmens = [];
    private readonly HashSet<nint> _visibleOmens = [];
    private readonly List<nint> _expiredOmens = [];

    public OmenUpdater(Config config, OmenHook omenHook)
    {
        _config = config;
        _omenHook = omenHook;
        _omenHook.OmenRecolored += Track;
        Svc.Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnFrameworkUpdate;
        _omenHook.OmenRecolored -= Track;
    }

    private void Track(nint omen, bool hostile, Vector4 color) => _trackedOmens[omen] = new TrackedOmen(hostile, color);

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!_config.Enabled)
        {
            _trackedOmens.Clear();
            return;
        }

        if (_trackedOmens.Count == 0)
            return;

        try
        {
            UpdateVisibleOmens();
            ForgetExpiredOmens();
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Failed to update omen colors.");
        }
    }

    private void UpdateVisibleOmens()
    {
        _visibleOmens.Clear();
        var territory = Svc.ClientState.TerritoryType;
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject is not IBattleChara caster)
                continue;

            var omen = (nint)CurrentOmen(caster);
            if (omen == 0 || !_trackedOmens.TryGetValue(omen, out var tracked))
                continue;

            _visibleOmens.Add(omen);
            if (!tracked.Hostile && !_config.NonHostile)
                continue;

            var color = TargetColor(caster, tracked, territory);
            if (color != tracked.AppliedColor && Apply((VfxData*)omen, color))
                _trackedOmens[omen] = tracked with { AppliedColor = color };
        }
    }

    private Vector4 TargetColor(IBattleChara caster, TrackedOmen tracked, uint territory)
    {
        var baseColor = Colors.GetOmenColor(_config, tracked.Hostile, territory);
        if (!_config.CastProgress || !tracked.Hostile)
            return baseColor;

        var warningColor = Colors.WithAlpha(_config.CastProgressColor, baseColor.W);
        return Vector4.Lerp(baseColor, warningColor, CastProgress(caster));
    }

    private void ForgetExpiredOmens()
    {
        _expiredOmens.Clear();
        foreach (var omen in _trackedOmens.Keys)
        {
            if (!_visibleOmens.Contains(omen))
                _expiredOmens.Add(omen);
        }

        foreach (var omen in _expiredOmens)
            _trackedOmens.Remove(omen);
    }

    private static VfxData* CurrentOmen(IBattleChara caster)
    {
        var character = (NativeCharacter*)caster.Address;
        return character == null ? null : (VfxData*)character->Vfx.VfxData[OmenSlot].Value;
    }

    private static float CastProgress(IBattleChara caster) =>
        caster.IsCasting && caster.TotalCastTime > 0f ? Math.Clamp(caster.CurrentCastTime / caster.TotalCastTime, 0f, 1f) : 0f;

    private static bool Apply(VfxData* omen, Vector4 color)
    {
        var instance = omen->Instance;
        if (instance == null)
            return false;

        VfxFunctions.UpdateVfxColor(instance, color.X, color.Y, color.Z, color.W);
        return true;
    }
}
