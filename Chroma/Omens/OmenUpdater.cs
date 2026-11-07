using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Pictomancy.VfxDraw;
using SourOmen.Structs;
using System;
using System.Collections.Generic;
using System.Numerics;

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
        var characters = CharacterManager.Instance();
        if (characters == null)
            return;

        var territory = Svc.ClientState.TerritoryType;
        var hostileColor = Colors.GetOmenColor(_config, hostile: true, territory);
        var nonHostileColor = Colors.GetOmenColor(_config, hostile: false, territory);

        foreach (var pointer in characters->BattleCharas)
        {
            var caster = pointer.Value;
            if (caster == null)
                continue;

            var omen = (nint)caster->Vfx.VfxData[OmenSlot].Value;
            if (omen == 0 || !_trackedOmens.TryGetValue(omen, out var tracked))
                continue;

            _visibleOmens.Add(omen);
            if (!tracked.Hostile && !_config.NonHostile)
                continue;

            var color = tracked.Hostile ? WithCastWarning(hostileColor, caster) : nonHostileColor;
            if (color != tracked.AppliedColor && Apply((VfxData*)omen, color))
                _trackedOmens[omen] = tracked with { AppliedColor = color };
        }
    }

    private Vector4 WithCastWarning(Vector4 baseColor, BattleChara* caster)
    {
        if (!_config.CastProgress)
            return baseColor;

        var warningColor = Colors.WithAlpha(_config.CastProgressColor, baseColor.W);
        return Vector4.Lerp(baseColor, warningColor, CastProgress(caster));
    }

    private void ForgetExpiredOmens()
    {
        if (_visibleOmens.Count == _trackedOmens.Count)
            return;

        _expiredOmens.Clear();
        foreach (var omen in _trackedOmens.Keys)
        {
            if (!_visibleOmens.Contains(omen))
                _expiredOmens.Add(omen);
        }

        foreach (var omen in _expiredOmens)
            _trackedOmens.Remove(omen);
    }

    private static float CastProgress(BattleChara* caster)
    {
        var cast = caster->CastInfo;
        return cast.IsCasting && cast.TotalCastTime > 0f ? Math.Clamp(cast.CurrentCastTime / cast.TotalCastTime, 0f, 1f) : 0f;
    }

    private static bool Apply(VfxData* omen, Vector4 color)
    {
        var instance = omen->Instance;
        if (instance == null)
            return false;

        VfxFunctions.UpdateVfxColor(instance, color.X, color.Y, color.Z, color.W);
        return true;
    }
}
