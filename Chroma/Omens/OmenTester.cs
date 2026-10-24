using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using Pictomancy;
using System;
using System.Linq;
using System.Numerics;

namespace Chroma.Omens;

public sealed class OmenTester : IDisposable
{
    public const int DefaultConeAngle = 90;

    public static readonly int[] ConeAngles = [15, 20, 30, 45, 60, 80, 90, 120, 130, 135, 150, 180, 210, 225, 270, 360];

    public static readonly DonutHole[] DonutHoles = [.. VfxRendererExtensions.OmenDonutHoleSizes
        .Where(hole => hole.Item2 < 1f)
        .OrderBy(hole => hole.Item2)
        .Select(hole => new DonutHole(hole.Item1, hole.Item2))];

    private const string VfxId = "ChromaTest";
    private const float GroundOmenHeight = 7f;

    private readonly Config _config;
    private string[]? _omenKeys;

    public OmenTester(Config config)
    {
        _config = config;
        Svc.Framework.Update += OnFrameworkUpdate;
    }

    public TestShape Shape { get; set; } = TestShape.None;

    public string[] OmenKeys => _omenKeys ??= [.. Svc.Data.GetExcelSheet<Omen>()
        .Select(omen => omen.Path.ExtractText())
        .Where(path => path.Length > 0)
        .Distinct()
        .Order(StringComparer.OrdinalIgnoreCase)];

    public static int NearestDonutHole(float ratio)
    {
        var nearest = 0;
        for (var i = 1; i < DonutHoles.Length; i++)
        {
            if (MathF.Abs(DonutHoles[i].Ratio - ratio) < MathF.Abs(DonutHoles[nearest].Ratio - ratio))
                nearest = i;
        }
        return nearest;
    }

    public void Dispose() => Svc.Framework.Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!_config.TestingEnabled || Shape == TestShape.None || Svc.Objects.LocalPlayer is not { } player)
            return;

        var color = Colors.GetOmenColor(_config, hostile: !_config.TestNonHostile, Svc.ClientState.TerritoryType);
        DrawShape(player.Position, player.Rotation, color);
    }

    private void DrawShape(Vector3 position, float facing, Vector4 color)
    {
        var renderer = PctService.VfxRenderer;
        switch (Shape)
        {
            case TestShape.Circle:
                renderer.AddCircle(VfxId, position, _config.TestCircleRadius, color);
                break;

            case TestShape.Line:
                var halfWidth = _config.TestLineWidth / 2f;
                renderer.AddLine(VfxId, position, _config.TestLineLength, halfWidth, facing + _config.TestLineRotation, color);
                break;

            case TestShape.Cone:
                renderer.AddCone(VfxId, position, _config.TestConeRadius, facing + _config.TestConeRotation, _config.TestConeAngleWidth, color);
                break;

            case TestShape.Donut:
                var hole = DonutHoles[NearestDonutHole(_config.TestDonutRatio)];
                var outerRadius = _config.TestDonutOuterRadius;
                renderer.AddOmen(VfxId, hole.Omen, position, new Vector3(outerRadius, GroundOmenHeight, outerRadius), 0f, color);
                break;

            case TestShape.Custom:
                renderer.AddOmen(VfxId, _config.TestCustomOmen, position, new Vector3(_config.TestCustomScale), facing, color);
                break;
        }
    }
}
