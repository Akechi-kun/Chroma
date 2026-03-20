using Chroma.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Lumina.Excel.Sheets;
using Pictomancy;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Icon = Dalamud.Interface.FontAwesomeIcon;

namespace Chroma.UI;

public class ChromaWindow : Window
{
    private readonly Config _cfg;
    private readonly Manager _mgr;
    private readonly DutyWindow _dutyWindow;

    private static readonly string GitHubIssuesUrl = "https://github.com/Akechi-kun/Chroma/issues";
    private static readonly string GitHubRepoUrl = "https://github.com/Akechi-kun/Chroma";
    private static readonly string SponsorUrl = "https://ko-fi.com/akechikun";

    private float AlphaValue = 1.2f;
    private bool RainbowActive = false;
    private bool WhiteActive = false;
    private float CurrentSpeed = 0.05f;
    private bool NonHostileActive = false;
    private bool TestEnabled = false;
    private float TestCircleRadius = 1f;
    private bool TestCircleActive = false;
    private bool TestConeActive = false;
    private float TestConeRadius = 1f;
    private float TestConeRotation = 1f;
    private int angleIndex = 0;
    private int TestConeAngleWidth = 0;
    private bool TestLineActive = false;
    private float TestLineLength = 1f;
    private float TestLineWidth = 1f;
    private float TestLineRotation = 1f;
    private bool TestDonutActive = false;
    private float TestDonutInnerRadius = 0.1f;
    private float TestDonutOuterRadius = 0.2f;
    private bool TestCustomActive = false;
    private Vector4 TitleColor = new(0.6f, 0.9f, 0.8f, 1f);
    private Vector4 DefaultColor = new(1f, 1f, 1f, 1.2f);
    private readonly int[] AllowedAngles = [10, 15, 20, 30, 45, 60, 80, 90, 120, 130, 135, 150, 180, 210, 225, 270, 360];

    public ChromaWindow(Config config, Manager manager, DutyWindow dutyWindow) : base($"Chroma v{Svc.PluginInterface.Manifest.AssemblyVersion}")
    {
        _cfg = config;
        _mgr = manager;
        _dutyWindow = dutyWindow;
        Size = new Vector2(330, 658);
        SizeCondition = ImGuiCond.FirstUseEver;
        AlphaValue = _cfg.Alpha;
        RainbowActive = _cfg.RainbowMode;
        WhiteActive = _cfg.WhiteMode;
        CurrentSpeed = _cfg.Speed;
        NonHostileActive = _cfg.NonHostile;
        TestEnabled = _cfg.TestingEnabled;
        TestCircleRadius = _cfg.TestCircleRadius;
        TestCircleActive = _cfg.TestCircleActive;
        TestConeActive = _cfg.TestConeActive;
        TestConeRadius = _cfg.TestConeRadius;
        TestConeRotation = _cfg.TestConeRotation;
        TestConeAngleWidth = _cfg.TestConeAngleWidth;
        TestLineActive = _cfg.TestLineActive;
        TestLineLength = _cfg.TestLineLength;
        TestLineWidth = _cfg.TestLineWidth;
        TestLineRotation = _cfg.TestLineRotation;
        TestDonutActive = _cfg.TestDonutActive;
        TestDonutInnerRadius = _cfg.TestDonutInnerRadius;
        TestDonutOuterRadius = _cfg.TestDonutOuterRadius;
        TestCustomActive = _cfg.TestCustomActive;
    }
    public override void PreDraw()
    {
        ImGui.SetNextWindowSizeConstraints
        (
            new Vector2(330, 200),
            new Vector2(330, 658)
        );
    }
    public override void Draw()
    {
        //Chroma
        if (ImGui.Checkbox("Enable Chroma", ref _cfg.Enabled))
        {
            _mgr.Enabled = _cfg.Enabled;
        }

        ImGui.SameLine(220, 0);
        DrawSupport();

        Separator();

        ImGui.BeginDisabled(!_cfg.Enabled && !_cfg.TestingEnabled);

        //Main Settings
        ImGuiEx.Icon(Icon.Wrench);
        ImGui.SameLine();
        ImGuiEx.Text(TitleColor, "Main Settings");
        ImGuiGroup.BeginGroupBox();
        DrawColorEditing();
        Separator();
        DrawNonHostile();
        Separator();
        DrawRainbowMode();
        Separator();
        DrawWhiteMode();
        ImGuiGroup.EndGroupBox();

        Separator();

        //Duty Overrides
        ImGuiEx.Icon(Icon.BookOpen);
        ImGui.SameLine();
        ImGuiEx.Text(TitleColor, "Duty-specific Overrides");
        ImGuiGroup.BeginGroupBox();
        DrawDutyOverrides();
        ImGuiGroup.EndGroupBox();

        Separator();

        ImGui.EndDisabled();

        //Testing
        ImGuiEx.Icon(Icon.Code);
        ImGui.SameLine();
        ImGuiEx.Text(TitleColor, "Testing ");
        ImGuiGroup.BeginGroupBox();
        DrawOmenTesting();
        ImGuiGroup.EndGroupBox();
    }

    private void DrawColorEditing()
    {
        #region Color Editor + Alpha
        var occupied = !_cfg.WhiteMode && !_cfg.RainbowMode;
        var hideIfDisabled = (!_cfg.Enabled && !_cfg.TestingEnabled) || _cfg.WhiteMode || _cfg.RainbowMode;

        ImGui.BeginDisabled(!occupied);
        ImGui.ColorEdit4
            (
                " Global Omen Color", 
                ref _cfg.GlobalColor, 
                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview
            );
        ImGui.SameLine();
        SetTooltip
            (
                "Adjust your color of your omens manually using a simple color editor.",
                hideIfDisabled
            );
        ImGui.EndDisabled();

        ImGui.BeginDisabled(_cfg.WhiteMode);
        ImGui.SetNextItemWidth(210);
        if (ImGui.SliderFloat(" Alpha", ref AlphaValue, 0.2f, 10f, "%.1f"))
        {
            _cfg.Alpha = AlphaValue;
        }
        ImGui.SameLine();
        SetTooltip
            (
                "Adjust the Alpha (color intensity) of your omens.\n" +
                "Lower values increase transparency; higher values enhance color intensity.\n" +
                "Default Value: 1.2f",
                (!_cfg.Enabled && !_cfg.TestingEnabled) || _cfg.WhiteMode
            );
        ImGui.EndDisabled();
        #endregion

        ImGui.Spacing();

        #region Color Buttons
        var presetColors = new (string Name, Vector4 Color)[]
        {
            ("Red", new Vector4(1f, 0f, 0f, AlphaValue)),
            ("Orange", new Vector4(1f, 0.5f, 0f, AlphaValue)),
            ("Yellow", new Vector4(1f, 1f, 0f, AlphaValue)),
            ("Green", new Vector4(0f, 1f, 0f, AlphaValue)),
            ("Blue", new Vector4(0f, 0.12f, 1f, AlphaValue)),
            ("Purple", new Vector4(0.2f, 0f, 1f, AlphaValue)),
            ("Pink", new Vector4(0.8f, 0.25f, 1f, AlphaValue)),
            ("Black", new Vector4(0f, 0f, 0f, AlphaValue))
        };

        ImGui.BeginDisabled(!occupied);
        for (var i = 0; i < presetColors.Length; i++)
        {
            if (ImGuiEx.Button(presetColors[i].Name))
            {
                _cfg.RainbowMode = false;
                _cfg.WhiteMode = false;
                _cfg.GlobalColor = presetColors[i].Color;
            }

            if (i == 4)
            {
                ImGui.Spacing();
            }
            else if (i != presetColors.Length - 1)
            {
                ImGui.SameLine();
            }
        }
        ImGui.EndDisabled();

        var isDefault = _cfg.GlobalColor == DefaultColor &&
                         AlphaValue.ApproximatelyEquals(1.2f) &&
                         !_cfg.RainbowMode && !_cfg.WhiteMode && !_cfg.NonHostile;
        ImGui.SameLine();
        ImGui.BeginDisabled(isDefault);
        if (ImGuiEx.IconButtonWithText(Icon.Ambulance, "Reset"))
        {
            Restore(false);
        }
        ImGui.EndDisabled();
        ImGui.SameLine();
        SetTooltip
            (
                "Reset all custom configurations to default with this button.", 
                isDefault
            );
        ImGui.Spacing();
        #endregion
    }
    private static void DrawSupport()
    {
        SetSupportButton(Icon.Bug, GitHubIssuesUrl, "Report Issue");
        ImGui.SameLine(0, 5);
        SetSupportButton(Icon.Star, GitHubRepoUrl, "Star on GitHub");
        ImGui.SameLine(0, 5);
        SetSupportButton(Icon.Heart, SponsorUrl, "Sponsor");
    }
    private void DrawRainbowMode()
    {
        #region Toggle
        if (ImGui.Checkbox("Rainbow Mode", ref RainbowActive))
        {
            if (RainbowActive)
            {
                WhiteActive = false;
                _cfg.WhiteMode = false;
                _cfg.RainbowMode = true;
            }
            else
            {
                Restore(true);
            }
        }
        ImGui.SameLine();
        SetTooltip
            (
                "This will randomize the colors of your omens.\n" +
                "NOTE: Enabling this will automatically disable White Mode and you will not be able to use the Color Editor.",
                !RainbowActive,
                true
            );
        #endregion

        #region Speed
        if (RainbowActive)
        {
            ImGui.SetNextItemWidth(210);
            if (ImGui.SliderFloat(" Speed", ref CurrentSpeed, 0.01f, 0.4f, "%.2f"))
            {
                _cfg.Speed = CurrentSpeed;
            }
            ImGui.SameLine();
            SetTooltip
                (
                    "Adjust the speed of the color cycle.",
                    !RainbowActive
                );
        }
        #endregion
    }
    private void DrawWhiteMode()
    {
        #region Toggle
        if (ImGui.Checkbox("White Mode", ref WhiteActive))
        {
            if (WhiteActive)
            {
                RainbowActive = false;
                _cfg.RainbowMode = false;
                _cfg.WhiteMode = true;
            }
            else
            {
                Restore(true);
            }
        }
        ImGui.SameLine();
        SetTooltip
            (
                "This will make all omens as white as possible.\n" +
                "This could cause some slight rendering issues, so use at your own risk.\n" +
                "NOTE: Enabling this will automatically disable Rainbow Mode and you will not be able to use the Color Editor or adjust Alpha.",
                !WhiteActive,
                true
            );
        #endregion
    }
    private void DrawNonHostile()
    {
        #region Toggle
        if (ImGui.Checkbox("Include Non-Hostile Omens", ref NonHostileActive))
        {
            _cfg.NonHostile = NonHostileActive;
        }
        ImGui.SameLine();
        SetTooltip
            (
                "If enabled, non-hostile omens (from self, party, alliance, etc.) will also be colored.",
                !NonHostileActive,
                true
            );
        #endregion

        #region Color
        ImGui.BeginDisabled(_cfg.WhiteMode || _cfg.RainbowMode);
        if (NonHostileActive)
        {
            ImGui.Indent();
            ImGui.ColorEdit4("Non-Hostile Omen Color", ref _cfg.NonHostileColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
            ImGui.Unindent();
        }
        ImGui.EndDisabled();
        #endregion
    }
    private void DrawDutyOverrides()
    {
        if (ImGuiEx.IconButtonWithText(Icon.Bullseye, "Open Duty Overrides Window"))
        {
            _dutyWindow.Toggle();
            Svc.PluginInterface.SavePluginConfig(_cfg);
        }
        ImGui.SameLine();
        ImGuiEx.HelpMarker
            (
                "Override omen colors for any specific duty.\n" +
                "When a duty entry is added, enabled, and you are inside that duty, its assigned color will take precedence over the global color setting.",
                DefaultColor
            );
    }
    private void DrawOmenTesting()
    {
        #region Toggle
        if (ImGui.Checkbox("Omen Testing", ref TestEnabled))
        {
            _cfg.TestingEnabled = TestEnabled;
        }
        ImGui.SameLine();
        SetTooltip
            (
                "This is for testing how your omens will appear.\n" +
                "If enabled, the omen will appear on your character until cleared with the 'Clear' button or a different omen is selected.",
                !TestEnabled,
                true
            );
        #endregion

        #region Clear
        var active = _cfg.TestingEnabled && (TestCircleActive || TestConeActive || TestLineActive || TestDonutActive || TestCustomActive);
        if (active)
        {
            ImGui.SameLine(200, 0);
            if (ImGuiEx.IconButtonWithText(Icon.Explosion, "Clear"))
            {
                SetOmen("");
                PictoService.VfxRenderer.Dispose();
            }
            ImGui.SameLine();
            SetTooltip
                (
                    "If you can see this 'Clear' button, it means there is currently an omen active from Testing.\n" +
                    "Use this button to clear any active omens that are currently being displayed.",
                    !active
                );
        }
        #endregion

        ImGui.Spacing();

        if (_cfg.TestingEnabled)
        {
            #region Testing Options
            if (ImGuiEx.Button("Circle"))
            {
                SetOmen("Circle");
            }
            ImGui.SameLine();
            if (ImGuiEx.Button("Line"))
            {
                SetOmen("Line");
            }
            ImGui.SameLine();
            if (ImGuiEx.Button("Cone"))
            {
                SetOmen("Cone");
            }
            ImGui.SameLine();
            if (ImGuiEx.Button("Donut"))
            {
                SetOmen("Donut");
            }
            ImGui.SameLine();
            if (ImGuiEx.Button("Custom"))
            {
                SetOmen("Custom");
            }
            #endregion

            #region Test Drawing
            var player = Svc.Objects.LocalPlayer;
            if (player == null)
            {
                Svc.Log.Error("Failed to retrieve player.");
                return;
            }

            if (TestCircleActive)
            {
                Separator();
                SetTestingSlider
                    (
                        "Length",
                        ref TestCircleRadius,
                        1f, 15f,
                        "%.1f",
                        "The length the omen being tested.",
                        v => _cfg.TestCircleRadius = v
                    );
                PictoService.VfxRenderer.AddCircle
                    (
                        $"{player.EntityId}",
                        player.Position,
                        TestCircleRadius,
                        _cfg.GlobalColor
                    );
            }

            if (TestConeActive)
            {
                Separator();
                SetTestingSlider
                    (
                        "Length",
                        ref TestConeRadius,
                        1f, 35f,
                        "%.1f",
                        "The length the omen being tested.",
                        v => _cfg.TestConeRadius = v
                    );
                SetTestingSlider
                    (
                        "Rotation",
                        ref TestConeRotation,
                        1f, 7.3f,
                        "%.001f",
                        "The direction the omen being tested is facing.",
                        v => _cfg.TestConeRotation = v
                    );
                ImGui.SetNextItemWidth(170);
                if (ImGui.SliderInt("Angle", ref angleIndex, 1, AllowedAngles.Length - 1))
                {
                    TestConeAngleWidth = AllowedAngles[angleIndex];
                    _cfg.TestConeAngleWidth = TestConeAngleWidth;
                }
                ImGui.SameLine();
                ImGuiEx.Text($"({AllowedAngles[angleIndex]}°)");
                ImGuiEx.HelpMarker
                    (
                        "The cone-angle width of the omen being tested.",
                        DefaultColor
                    );
                PictoService.VfxRenderer.AddCone
                    (
                        $"{player.EntityId}",
                        player.Position,
                        TestConeRadius,
                        TestConeRotation,
                        TestConeAngleWidth,
                        _cfg.GlobalColor
                    );
            }

            if (TestLineActive)
            {
                Separator();
                SetTestingSlider
                    (
                        "Length",
                        ref TestLineLength,
                        1f, 50f,
                        "%.1f",
                        "The length the omen being tested.",
                        v => _cfg.TestLineLength = v
                    );
                SetTestingSlider
                    (
                        "Width",
                        ref TestLineWidth,
                        1f, 15f,
                        "%.1f",
                        "The width the omen being tested.",
                        v => _cfg.TestLineWidth = v
                    );
                SetTestingSlider
                    (
                        "Rotation",
                        ref TestLineRotation,
                        1f, 7.3f,
                        "%.001f",
                        "The direction the omen being tested is facing.",
                        v => _cfg.TestLineRotation = v
                    );
                PictoService.VfxRenderer.AddLine
                    (
                        $"{player.EntityId}",
                        player.Position,
                        TestLineLength,
                        TestLineWidth,
                        TestLineRotation,
                        _cfg.GlobalColor
                    );
            }

            if (TestDonutActive)
            {
                Separator();
                SetTestingSlider
                    (
                        "Scale",
                        ref TestDonutInnerRadius,
                        0.1f, 25f,
                        "%.1f",
                        "Scaling of the omen being tested.",
                        v =>
                        {
                            TestDonutOuterRadius = TestDonutInnerRadius * 2f;
                            _cfg.TestDonutInnerRadius = TestDonutInnerRadius;
                            _cfg.TestDonutOuterRadius = TestDonutOuterRadius;
                        }
                    );
                ImGuiEx.Text($"Inner Radius: {TestDonutInnerRadius:0.0}");
                ImGui.SameLine();
                ImGuiEx.HelpMarker
                    (
                        "Inner radius of the omen being tested (inner safe zone).",
                        DefaultColor
                    );
                ImGuiEx.Text($"Outer Radius: {TestDonutOuterRadius:0.0}");
                ImGui.SameLine();
                ImGuiEx.HelpMarker
                    (
                        "Outer radius of the omen being tested (outer unsafe zone).",
                        DefaultColor
                    );
                PictoService.VfxRenderer.AddDonut
                    (
                        $"{player.EntityId}",
                        player.Position,
                        TestDonutInnerRadius,
                        TestDonutOuterRadius,
                        _cfg.GlobalColor
                    );
            }

            if (TestCustomActive)
            {
                Separator();

                var keys = Svc.Data.GetExcelSheet<Omen>().Select(o => o.Path.ToMacroString()).ToArray();
                var index = Math.Clamp(_cfg.SelectedOmenIndex, 0, keys.Length - 1);
                var name = keys[index];

                ImGui.SetNextItemWidth(150);
                if (ImGui.BeginCombo("##OmenSelect", name))
                {
                    for (var i = 0; i < keys.Length; i++)
                    {
                        var selected = i == index;
                        if (ImGui.Selectable(keys[i], selected))
                        {
                            index = i;
                            _cfg.SelectedOmenIndex = i;
                        }
                        if (selected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                ImGuiEx.Text("Omen Selection");
                ImGui.SameLine();
                ImGuiEx.HelpMarker
                    (
                        "The actual names of these omens are unknown, so keys are used instead to select them.",
                        DefaultColor
                    );
                PictoService.VfxRenderer.AddOmen
                    (
                        player.EntityId.ToString(),
                        name,
                        player.Position,
                        new Vector3(3),
                        0,
                        _cfg.GlobalColor
                    );
            }
        }
        else
        {
            PictoService.VfxRenderer.Dispose();
        }
            #endregion
    }

    private static void Separator()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
    private void SetTooltip(string text, bool disabled, bool forceShow = false)
    {
        var isDisabled = disabled && !forceShow;

        ImGui.BeginDisabled(isDisabled);
        ImGuiEx.HelpMarker(
            text,
            isDisabled || disabled && forceShow ? null : DefaultColor
        );
        ImGui.EndDisabled();
    }
    private void SetTestingSlider(string label, ref float cfg, float min, float max, string increments, string tooltip, Action<float>? onChange = null)
    {
        ImGui.SetNextItemWidth(175);
        if (ImGui.SliderFloat(label, ref cfg, min, max, increments))
            onChange?.Invoke(cfg);
        ImGui.SameLine();
        ImGuiEx.HelpMarker(tooltip, DefaultColor);
    }
    private static void SetSupportButton(Icon icon, string url, string tooltip)
    {
        if (ImGuiEx.IconButton(icon))
        {
            try
            {
                using (Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }))
                { }
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"Failed to open URL '{url}': {ex.Message}");
            }
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
    private void SetOmen(string omenType)
    {
        TestCircleActive = omenType == "Circle";
        TestConeActive = omenType == "Cone";
        TestLineActive = omenType == "Line";
        TestDonutActive = omenType == "Donut";
        TestCustomActive = omenType == "Custom";
    }
    private void Restore(bool exceptNHOs)
    {
        RainbowActive = false;
        _cfg.RainbowMode = RainbowActive;

        WhiteActive = false;
        _cfg.WhiteMode = WhiteActive;

        if (!exceptNHOs)
        {
            _cfg.NonHostile = false;
            NonHostileActive = _cfg.NonHostile;
        }

        _cfg.NonHostileColor = DefaultColor;

        _cfg.GlobalColor = DefaultColor;

        AlphaValue = 1.2f;
        _cfg.Alpha = AlphaValue;
    }
}

