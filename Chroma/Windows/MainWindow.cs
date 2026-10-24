using Chroma.Omens;
using Chroma.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using System;
using System.Linq;
using System.Numerics;
using Icon = Dalamud.Interface.FontAwesomeIcon;

namespace Chroma.Windows;

public sealed class MainWindow : ConfigWindow
{
    private const string IssuesUrl = "https://github.com/Akechi-kun/Chroma/issues";
    private const string RepositoryUrl = "https://github.com/Akechi-kun/Chroma";
    private const string SponsorUrl = "https://ko-fi.com/akechikun";

    private const float WindowWidth = 330f;
    private const float SupportButtonSpacing = 5f;
    private const int PresetColumns = 4;

    private static readonly string[] TitleLetters = ["C", "h", "r", "o", "m", "a"];
    private static readonly TestShape[] TestShapes = [TestShape.Circle, TestShape.Line, TestShape.Cone, TestShape.Donut, TestShape.Custom];
    private static readonly string[] TestShapeLabels = [.. TestShapes.Select(shape => shape.ToString())];
    private static readonly string[] ConeAngleLabels = [.. OmenTester.ConeAngles.Select(angle => $"{angle} degrees")];
    private static readonly string AlphaHelp =
        "Adjust the Alpha (color intensity) of your omens.\n" +
        "Lower values increase transparency; higher values enhance color intensity.\n" +
        "This also applies to Non-Hostile omens and Duty Overrides.\n" +
        $"Default Value: {Colors.DefaultAlpha}";

    private readonly OmenHook _omenHook;
    private readonly OmenTester _omenTester;
    private readonly DutyWindow _dutyWindow;
    private readonly SearchCombo<string> _omenSearch;
    private readonly Func<string, bool> _isSelectedOmen;
    private readonly ColorPicker _globalColorPicker = new("##GlobalColor");
    private readonly ColorPicker _nonHostileColorPicker = new("##NonHostileColor");
    private readonly ColorPicker _castWarningColorPicker = new("##CastProgressColor");

    private float _supportButtonsWidth;
    private float _resetButtonWidth;
    private float _clearButtonWidth;
    private (float OuterRadius, int Hole, string Text) _donutInnerLabel = (-1f, -1, string.Empty);

    public MainWindow(Config config, OmenHook omenHook, OmenTester omenTester, DutyWindow dutyWindow)
        : base($"Chroma v{Svc.PluginInterface.Manifest.AssemblyVersion}###ChromaMain", config)
    {
        _omenHook = omenHook;
        _omenTester = omenTester;
        _dutyWindow = dutyWindow;
        _omenSearch = new(() => _omenTester.OmenKeys, key => key);
        _isSelectedOmen = key => key == Config.TestCustomOmen;
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    private bool SettingsLocked => !Config.Enabled && !Config.TestingEnabled;

    private bool ColorModeActive => Config.WhiteMode || Config.RainbowMode;

    public override void PreDraw()
    {
        var width = Widgets.Scaled(WindowWidth);
        ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0), new Vector2(width, float.MaxValue));
    }

    protected override void DrawContents()
    {
        DrawHeader();
        Widgets.SpacedSeparator();

        using (ImRaii.Disabled(SettingsLocked))
        {
            DrawMainSettings();
            ImGui.Spacing();
            DrawDutyOverrides();
        }

        ImGui.Spacing();
        DrawTesting();
    }

    private void DrawHeader()
    {
        var enabled = Config.Enabled;
        if (Widgets.Toggle("##EnableChroma", ref enabled))
        {
            _omenHook.Enabled = enabled;
            UnsavedChanges = true;
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(enabled ? "Disable Chroma" : "Enable Chroma");

        ImGui.SameLine();
        Widgets.GradientTitle(TitleLetters, Config.Enabled);
        ImGui.SameLine();
        if (Config.Enabled)
            ImGui.TextColored(ImGuiColors.HealerGreen, "Enabled");
        else
            ImGui.TextColored(ImGuiColors.DalamudRed, "Disabled");

        DrawSupportButtons();
    }

    private void DrawSupportButtons()
    {
        Widgets.SameLineRight(_supportButtonsWidth);
        var left = ImGui.GetCursorScreenPos().X;
        SupportButton(Icon.Bug, IssuesUrl, "Report Issue");
        ImGui.SameLine(0, SupportButtonSpacing);
        SupportButton(Icon.Star, RepositoryUrl, "Star on GitHub");
        ImGui.SameLine(0, SupportButtonSpacing);
        SupportButton(Icon.Heart, SponsorUrl, "Sponsor");
        _supportButtonsWidth = ImGui.GetItemRectMax().X - left;
    }

    private static void SupportButton(Icon icon, string url, string tooltip)
    {
        if (ImGuiEx.IconButton(icon))
            GenericHelpers.ShellStart(url);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
    }

    private void DrawMainSettings()
    {
        Widgets.SectionTitle(Icon.Palette, "Main Settings");
        DrawResetButton();

        Layout.BeginBox();
        DrawColorPreview();
        DrawGlobalColor();
        DrawAlpha();
        DrawPresets();
        Layout.Divider();
        DrawNonHostile();
        DrawRainbowMode();
        DrawWhiteMode();
        Layout.Divider();
        DrawCastProgress();
        Layout.EndBox();
    }

    private void DrawResetButton()
    {
        var alreadyDefault = Config.HasDefaultColors;
        using (ImRaii.Disabled(alreadyDefault))
        {
            if (Widgets.RightAlignedIconButton(Icon.Ambulance, "Reset", ref _resetButtonWidth))
            {
                Config.ResetColors();
                UnsavedChanges = true;
                Toast("All colors and modes have been reset to default.");
            }
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(alreadyDefault ? "Everything is already at default." : "Reset all colors, Alpha, Speed and modes to default.");
    }

    private void DrawColorPreview()
    {
        var territory = Svc.ClientState.TerritoryType;
        var hostile = Colors.GetOmenColor(Config, hostile: true, territory);
        Vector4? nonHostile = Config.NonHostile ? Colors.GetOmenColor(Config, hostile: false, territory) : null;
        Widgets.ColorPreview(hostile, nonHostile);
    }

    private void DrawGlobalColor()
    {
        Layout.Label("Omen Color");
        using (ImRaii.Disabled(ColorModeActive))
        {
            UnsavedChanges |= _globalColorPicker.Draw(ref Config.GlobalColor, out var editFinished);
            if (editFinished)
                Toast($"Omen color set to {Colors.ToHex(Config.GlobalColor)}.");
        }
        Layout.Help("Adjust the color of your omens manually using a simple color editor.");
    }

    private void DrawAlpha()
    {
        Layout.Label("Color Alpha");
        using (ImRaii.Disabled(Config.WhiteMode))
        {
            ImGui.SetNextItemWidth(Layout.ControlWidth);
            UnsavedChanges |= ImGui.SliderFloat("##Alpha", ref Config.Alpha, Colors.MinAlpha, Colors.MaxAlpha, "%.1f", ImGuiSliderFlags.AlwaysClamp);
        }
        Layout.Help(AlphaHelp);

        if (Colors.IsLowAlpha(Config))
            Layout.Note(Icon.ExclamationTriangle, Colors.LowAlphaWarning, ImGuiColors.DalamudOrange);
    }

    private void DrawPresets()
    {
        using var spacing = Layout.ButtonGroupSpacing();
        var cellWidth = Layout.GridCellWidth(PresetColumns);
        using (ImRaii.Disabled(ColorModeActive))
        {
            for (var i = 0; i < Colors.Presets.Length; i++)
            {
                var (name, color) = Colors.Presets[i];
                var selected = !ColorModeActive && Colors.SameRgb(Config.GlobalColor, color);
                Layout.GridNext(i, PresetColumns);
                if (Widgets.ColorButton(name, color, selected, cellWidth))
                {
                    Config.SetColor(color);
                    UnsavedChanges = true;
                    Toast($"Omen color set to {name}.");
                }
            }
        }
    }

    private void DrawNonHostile()
    {
        Layout.Label("Non-Hostile");
        UnsavedChanges |= Widgets.Toggle("##NonHostile", ref Config.NonHostile);
        if (Config.NonHostile)
        {
            ImGui.SameLine();
            using (ImRaii.Disabled(ColorModeActive))
            {
                UnsavedChanges |= _nonHostileColorPicker.Draw(ref Config.NonHostileColor, out var editFinished);
                if (editFinished)
                    Toast($"Non-Hostile color set to {Colors.ToHex(Config.NonHostileColor)}.");
            }
        }
        Layout.Help("If enabled, non-hostile omens (from self, party, alliance, etc.) will also be colored, using the color picker next to the switch.");
    }

    private void DrawRainbowMode()
    {
        Layout.Label("Rainbow Mode");
        var rainbow = Config.RainbowMode;
        if (Widgets.Toggle("##RainbowMode", ref rainbow))
        {
            Config.SetRainbowMode(rainbow);
            UnsavedChanges = true;
            Toast($"Rainbow Mode {(rainbow ? "enabled" : "disabled")}.");
        }

        if (Config.RainbowMode)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(Layout.RemainingControlWidth);
            UnsavedChanges |= ImGui.SliderFloat("##Speed", ref Config.Speed, Colors.MinSpeed, Colors.MaxSpeed, "Speed: %.2f", ImGuiSliderFlags.AlwaysClamp);
        }

        Layout.Help(
            "This will cycle the colors of your omens through the color spectrum.\n" +
            "Non-Hostile omens use the opposite side of the spectrum.\n" +
            "The slider next to the switch adjusts the speed of the color cycle.\n" +
            "NOTE: Enabling this will automatically disable White Mode and you will not be able to use the Color Editor.");
    }

    private void DrawWhiteMode()
    {
        Layout.Label("White Mode");
        var white = Config.WhiteMode;
        if (Widgets.Toggle("##WhiteMode", ref white))
        {
            Config.SetWhiteMode(white);
            UnsavedChanges = true;
            Toast($"White Mode {(white ? "enabled" : "disabled")}.");
        }
        Layout.Help(
            "This will make all omens as white as possible.\n" +
            "This could cause some slight rendering issues, so use at your own risk.\n" +
            "NOTE: Enabling this will automatically disable Rainbow Mode and you will not be able to use the Color Editor or adjust Alpha.");

        if (Config.WhiteMode)
            Layout.Note(Icon.ExclamationTriangle, Colors.WhiteModeWarning, ImGuiColors.DalamudOrange);
    }

    private void DrawCastProgress()
    {
        Layout.Label("Cast Warning");
        UnsavedChanges |= Widgets.Toggle("##CastProgress", ref Config.CastProgress);
        if (Config.CastProgress)
        {
            ImGui.SameLine();
            UnsavedChanges |= _castWarningColorPicker.Draw(ref Config.CastProgressColor, out var editFinished);
            if (editFinished)
                Toast($"Cast Warning color set to {Colors.ToHex(Config.CastProgressColor)}.");
        }
        Layout.Help(
            "Hostile omens gradually shift toward the color next to the switch as the enemy's cast nears completion.\n" +
            "Omens that are not tied to a cast keep their normal color.");
    }

    private void DrawDutyOverrides()
    {
        Widgets.SectionTitle(Icon.MapMarkedAlt, "Duty Overrides");
        Widgets.RightAlignedTextDisabled(Widgets.SavedCountLabel(Config.DutyColors.Count));

        Layout.BeginBox();
        Layout.Label("Overrides");
        if (ImGui.Button("Manage Overrides", new Vector2(Layout.ControlWidth, 0)))
            _dutyWindow.Toggle();
        Layout.Help(
            "Override omen colors for any specific duty.\n" +
            "When a duty entry is added, enabled, and you are inside that duty, its assigned color will take precedence over the global color setting.");

        Layout.Label("Active");
        DrawActiveDuty();
        Layout.EndBox();
    }

    private void DrawActiveDuty()
    {
        var activeDuty = Config.FindDuty(Svc.ClientState.TerritoryType);
        if (activeDuty == null)
        {
            ImGui.TextDisabled("None");
            return;
        }

        Layout.WrappedValue(activeDuty.Name, ImGuiColors.HealerGreen);
    }

    private void DrawTesting()
    {
        Widgets.SectionTitle(Icon.Flask, "Testing");
        DrawClearButton();

        Layout.BeginBox();
        DrawTestingToggle();
        if (Config.TestingEnabled)
        {
            DrawShapeButtons();
            DrawPreviewAs();
            if (_omenTester.Shape != TestShape.None)
            {
                Layout.Divider();
                DrawShapeSettings();
            }
        }
        Layout.EndBox();
    }

    private void DrawClearButton()
    {
        if (!Config.TestingEnabled || _omenTester.Shape == TestShape.None)
            return;

        if (Widgets.RightAlignedIconButton(Icon.Explosion, "Clear", ref _clearButtonWidth))
            _omenTester.Shape = TestShape.None;
    }

    private void DrawTestingToggle()
    {
        Layout.Label("Omen Testing");
        if (Widgets.Toggle("##OmenTesting", ref Config.TestingEnabled))
        {
            if (!Config.TestingEnabled)
                _omenTester.Shape = TestShape.None;
            UnsavedChanges = true;
        }
        Layout.Help(
            "This is for testing how your omens will appear.\n" +
            "If enabled, the omen will appear on your character until cleared with the 'Clear' button or a different omen is selected.");
    }

    private void DrawShapeButtons()
    {
        using var spacing = Layout.ButtonGroupSpacing();
        var cellWidth = Layout.GridCellWidth(TestShapes.Length);
        for (var i = 0; i < TestShapes.Length; i++)
        {
            var shape = TestShapes[i];
            var selected = _omenTester.Shape == shape;
            Layout.GridNext(i, TestShapes.Length);
            if (Widgets.SelectableButton(TestShapeLabels[i], selected, cellWidth))
                _omenTester.Shape = selected ? TestShape.None : shape;
        }
    }

    private void DrawPreviewAs()
    {
        Layout.Label("Preview As");
        using (Layout.ButtonGroupSpacing())
        {
            var segmentWidth = Layout.SplitControlWidth(2);
            if (Widgets.SelectableButton("Hostile", !Config.TestNonHostile, segmentWidth))
            {
                Config.TestNonHostile = false;
                UnsavedChanges = true;
            }
            ImGui.SameLine();
            if (Widgets.SelectableButton("Non-Hostile", Config.TestNonHostile, segmentWidth))
            {
                Config.TestNonHostile = true;
                UnsavedChanges = true;
            }
        }
        Layout.Help(
            "Preview the omen using the hostile or the Non-Hostile omen color.\n" +
            "The current duty's override (if any) is used for hostile previews.");
    }

    private void DrawShapeSettings()
    {
        switch (_omenTester.Shape)
        {
            case TestShape.Circle:
                DrawCircleSettings();
                break;
            case TestShape.Line:
                DrawLineSettings();
                break;
            case TestShape.Cone:
                DrawConeSettings();
                break;
            case TestShape.Donut:
                DrawDonutSettings();
                break;
            case TestShape.Custom:
                DrawCustomSettings();
                break;
        }
    }

    private void DrawCircleSettings()
    {
        Slider("Radius", ref Config.TestCircleRadius, 1f, 30f, "The radius of the omen being tested.");
    }

    private void DrawLineSettings()
    {
        Slider("Length", ref Config.TestLineLength, 1f, 50f, "The length of the omen being tested.");
        Slider("Width", ref Config.TestLineWidth, 1f, 30f, "The width of the omen being tested.");
        RotationSlider(ref Config.TestLineRotation);
    }

    private void DrawConeSettings()
    {
        Slider("Length", ref Config.TestConeRadius, 1f, 40f, "The length of the omen being tested.");
        RotationSlider(ref Config.TestConeRotation);

        var angle = Math.Max(Array.IndexOf(OmenTester.ConeAngles, Config.TestConeAngleWidth), 0);
        if (StepSlider("Angle", ref angle, OmenTester.ConeAngles.Length, ConeAngleLabels[angle], "The cone-angle width of the omen being tested."))
            Config.TestConeAngleWidth = OmenTester.ConeAngles[angle];
    }

    private void DrawDonutSettings()
    {
        Slider("Outer Radius", ref Config.TestDonutOuterRadius, 1f, 40f, "Outer radius of the omen being tested (outer unsafe zone).");

        var hole = OmenTester.NearestDonutHole(Config.TestDonutRatio);
        var help =
            "Inner radius of the omen being tested (inner safe zone).\n" +
            "The game only has donut omens for certain hole sizes, so this steps through those.\n" +
            "The hole scales with the Outer Radius.";
        if (StepSlider("Inner Radius", ref hole, OmenTester.DonutHoles.Length, DonutInnerLabel(hole), help))
            Config.TestDonutRatio = OmenTester.DonutHoles[hole].Ratio;
    }

    private void DrawCustomSettings()
    {
        Layout.Label("Omen");
        if (_omenSearch.Draw("##OmenSelect", Config.TestCustomOmen, Layout.ControlWidth, _isSelectedOmen, out var omenKey))
        {
            Config.TestCustomOmen = omenKey!;
            UnsavedChanges = true;
        }
        Layout.Help(
            "The actual names of these omens are unknown, so their file keys are used instead.\n" +
            "Type in the box to search.");

        Slider("Scale", ref Config.TestCustomScale, 0.5f, 30f, "Scaling of the omen being tested.");
    }

    private void Slider(string label, ref float value, float min, float max, string help)
    {
        using var id = ImRaii.PushId(label);
        Layout.Label(label);
        ImGui.SetNextItemWidth(Layout.ControlWidth);
        UnsavedChanges |= ImGui.SliderFloat("##Value", ref value, min, max, "%.1f", ImGuiSliderFlags.AlwaysClamp);
        Layout.Help(help);
    }

    private void RotationSlider(ref float radians)
    {
        Layout.Label("Rotation");
        ImGui.SetNextItemWidth(Layout.ControlWidth);
        UnsavedChanges |= ImGui.SliderAngle("##Rotation", ref radians, -180f, 180f, "%.0f degrees");
        Layout.Help("The direction the omen being tested is facing, relative to your character.");
    }

    private bool StepSlider(string label, ref int step, int stepCount, string valueLabel, string help)
    {
        using var id = ImRaii.PushId(label);
        Layout.Label(label);
        ImGui.SetNextItemWidth(Layout.ControlWidth);
        var changed = ImGui.SliderInt("##Value", ref step, 0, stepCount - 1, valueLabel);
        step = Math.Clamp(step, 0, stepCount - 1);
        Layout.Help(help);

        UnsavedChanges |= changed;
        return changed;
    }

    private static void Toast(string message) => Notify.Info(message);

    private string DonutInnerLabel(int hole)
    {
        var outerRadius = Config.TestDonutOuterRadius;
        if (_donutInnerLabel.OuterRadius != outerRadius || _donutInnerLabel.Hole != hole)
            _donutInnerLabel = (outerRadius, hole, $"{outerRadius * OmenTester.DonutHoles[hole].Ratio:0.0}");
        return _donutInnerLabel.Text;
    }
}
