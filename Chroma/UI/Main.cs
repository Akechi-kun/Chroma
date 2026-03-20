using Chroma.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Lumina.Excel.Sheets;
using Pictomancy;
using System;
using System.Linq;
using System.Numerics;
using Icon = Dalamud.Interface.FontAwesomeIcon;

namespace Chroma.UI;

public class ChromaWindow : Window
{
    private readonly Config _cfg;
    private readonly Manager _mgr;
    private readonly DutyWindow _dutyWindow;

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
    private Vector4 TitleColor = new(1f, 0.4f, 0.4f, 1f);
    private Vector4 DefaultColor = new(1f, 1f, 1f, 1.2f);
    private readonly int[] AllowedAngles = [10, 15, 20, 30, 45, 60, 80, 90, 120, 130, 135, 150, 180, 210, 225, 270, 360];

    public ChromaWindow(Config config, Manager manager, DutyWindow dutyWindow) : base($"Chroma v{Svc.PluginInterface.Manifest.AssemblyVersion}")
    {
        _cfg = config;
        _mgr = manager;
        _dutyWindow = dutyWindow;
        Size = new Vector2(330, 500);
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
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(330, 200),
            new Vector2(330, 600)
        );
    }

    public override void Draw()
    {
        //Chroma
        if (ImGui.Checkbox("Enable Chroma", ref _cfg.Enabled))
        {
            _mgr.Enabled = _cfg.Enabled;
        }

        ImGui.SameLine(230, 0);
        DrawSupport();

        Separator();

        ImGui.BeginDisabled(!_cfg.Enabled && !_cfg.TestingEnabled);

        //Main Settings
        ImGui.TextColored(TitleColor, "Chroma Settings");
        ImGuiGroup.BeginGroupBox();
        DrawColors();
        Separator();
        DrawNonHostile();
        Separator();
        DrawRainbowMode();
        Separator();
        DrawWhiteMode();
        ImGuiGroup.EndGroupBox();

        Separator();

        //Duty Overrides
        ImGui.TextColored(TitleColor, "Duty-specific Overrides");
        ImGuiGroup.BeginGroupBox();
        DrawDutyOverrides();
        ImGuiGroup.EndGroupBox();

        Separator();

        ImGui.EndDisabled();

        //Testing
        ImGui.TextColored(TitleColor, "Testing");
        ImGuiGroup.BeginGroupBox();
        DrawOmenTesting();
        ImGuiGroup.EndGroupBox();

    }
    private void DrawColors()
    {
        #region Color Editor + Alpha
        var occupied = !_cfg.WhiteMode && !_cfg.RainbowMode;
        var hideIfDisabled = (!_cfg.Enabled && !_cfg.TestingEnabled) || _cfg.WhiteMode || _cfg.RainbowMode;

        ImGui.BeginDisabled(!occupied);
        ImGui.ColorEdit4
            (
                "Global Omen Color", 
                ref _cfg.GlobalColor, 
                ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview
            );
        ImGui.SameLine();
        DrawTooltip("Adjust your color of your omens manually using a simple color editor.\nDefault: #FFFFFFFF", hideIfDisabled);
        ImGui.EndDisabled();

        ImGui.Spacing();

        ImGui.BeginDisabled(_cfg.WhiteMode);
        ImGui.Indent();
        ImGui.SetNextItemWidth(150);
        if (ImGui.SliderFloat("Alpha", ref AlphaValue, 0.2f, 10f, "%.1f"))
        {
            _cfg.Alpha = AlphaValue;
        }
        ImGui.SameLine();
        DrawTooltip
            (
                "Adjust the Alpha (color intensity) of your omens.\n" +
                "Lower values increase transparency; higher values enhance color intensity.\n" +
                "Default Value: 1.2f", 
                hideIfDisabled
            );
        ImGui.Unindent();
        ImGui.EndDisabled();

        ImGui.Spacing();
        #endregion

        #region Color Buttons
        var presetColors = new (string Name, Vector4 Color)[]
        {
            ("Red", new Vector4(1f, 0f, 0f, AlphaValue)),
            ("Orange", new Vector4(1f, 0.5f, 0f, AlphaValue)),
            ("Yellow", new Vector4(1f, 1f, 0f, AlphaValue)),
            ("Green", new Vector4(0f, 1f, 0f, AlphaValue)),
            ("Blue", new Vector4(0f, 0.12f, 1f, AlphaValue)),
            ("Purple", new Vector4(0.1f, 0f, 1f, AlphaValue)),
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
            ResetToDefault();
        }
        ImGui.EndDisabled();
        ImGui.SameLine();
        DrawTooltip
            (
                "Reset all custom configurations to default with this button.", 
                isDefault
            );
        ImGui.Spacing();

        #endregion
    }
    private static void DrawSupport()
    {
        if (ImGuiEx.IconButton(Icon.Bug))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/Akechi-kun/Chroma/issues", UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"Failed to open GitHub Issues page: {ex.Message}");
            }
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Report Issue");
        }
        ImGui.SameLine();
        if (ImGuiEx.IconButton(Icon.Star))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/Akechi-kun/Chroma", UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"Failed to open GitHub link: {ex.Message}");
            }
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Star on GitHub");
        }
        ImGui.SameLine();
        if (ImGuiEx.IconButton(Icon.Heart))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://ko-fi.com/akechikun", UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Svc.Log.Error($"Failed to open Sponsor link: {ex.Message}");
            }
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Sponsor");
        }
    }
    private void DrawRainbowMode()
    {
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
                ResetToDefault();
            }
        }
        ImGui.SameLine();
        DrawTooltip
            (
                "This will randomize the colors of your omens/indicators.\n" +
                "NOTE: Enabling this will automatically disable White Mode and you will not be able to use the Color Editor.",
                !RainbowActive,
                true
            );

        if (RainbowActive)
        {
            ImGui.Indent();
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("Speed", ref CurrentSpeed, 0.01f, 0.4f, "%.2f"))
            {
                _cfg.Speed = CurrentSpeed;
            }
            ImGui.SameLine();
            DrawTooltip("Adjust the speed of the color cycle.", !RainbowActive);
            ImGui.Unindent();
        }
    }
    private void DrawWhiteMode()
    {
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
                ResetToDefault();
            }
        }
        ImGui.SameLine();
        DrawTooltip
            (
                "This will make all omens as white as possible.\n" +
                "This could cause some slight rendering issues, so use at your own risk.\n" +
                "NOTE: Enabling this will automatically disable Rainbow Mode and you will not be able to use the Color Editor or adjust Alpha.",
                !WhiteActive,
                true
            );
    }
    private void DrawNonHostile()
    {
        if (ImGui.Checkbox("Include Non-Hostile Omens", ref NonHostileActive))
            _cfg.NonHostile = NonHostileActive;
        ImGui.SameLine();
        DrawTooltip(
            "If enabled, non-hostile omens/indicators (from self, party, alliance, etc.) will also be colored.",
            !NonHostileActive,
            true
            );
        ImGui.BeginDisabled(_cfg.WhiteMode || _cfg.RainbowMode);
        if (NonHostileActive)
        {
            ImGui.Indent();
            ImGui.ColorEdit4("Non-Hostile Omen Color", ref _cfg.NonHostileColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
            ImGui.Unindent();
        }
        ImGui.EndDisabled();
    }
    private void DrawDutyOverrides()
    {
        if (ImGuiEx.IconButtonWithText(Icon.Bullseye, "Open Duty Overrides Window"))
        {
            _dutyWindow.Toggle();
            Svc.PluginInterface.SavePluginConfig(_cfg);
        }
        ImGui.SameLine();
        ImGuiEx.HelpMarker(
            "Override the omen color for a specific duty.\n" +
            "When a duty entry is added, enabled, and you are inside that duty, its assigned color will take precedence over the global color setting.",
            DefaultColor
            );
    }
    private void DrawOmenTesting()
    {
        if (ImGui.Checkbox("Enable Omen Testing", ref TestEnabled))
        {
            _cfg.TestingEnabled = TestEnabled;
        }
        ImGui.SameLine();
        DrawTooltip(
            "This is for testing how your omens will appear.\n" +
            "If enabled, the omen will appear on your character until cleared with the 'Clear' button or a different omen is selected.",
            !TestEnabled,
            true
        );
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
            DrawTooltip(
                "If you can see this 'Clear' button, it means there is currently an omen active from Testing.\n" +
                "Use this button to clear any active omens that are currently being displayed.",
                !active
            );
        }

        ImGui.Spacing();

        if (_cfg.TestingEnabled)
        {
            DrawTestingButtons();
            DrawTestingOmens();
        }
        else
        {
            PictoService.VfxRenderer.Dispose();
        }
    }
    private void DrawTestingButtons()
    {
        if (ImGui.Button("Circle"))
            SetOmen("Circle");
        ImGui.SameLine();
        if (ImGui.Button("Line"))
            SetOmen("Line");
        ImGui.SameLine();
        if (ImGui.Button("Cone"))
            SetOmen("Cone");
        ImGui.SameLine();
        if (ImGui.Button("Donut"))
            SetOmen("Donut");
        ImGui.SameLine();
        if (ImGui.Button("Custom"))
            SetOmen("Custom");
    }
    private void DrawTestingOmens()
    {
        var player = Svc.Objects.LocalPlayer;
        if (player == null) return;

        if (TestCircleActive)
        {
            Separator();
            DrawSlider("Length", ref TestCircleRadius, 1f, 15f, "%.1f", "The length the omen being Tested.", v => _cfg.TestCircleRadius = v);
            PictoService.VfxRenderer.AddCircle($"{player.EntityId}", player.Position, TestCircleRadius, _cfg.GlobalColor);
        }

        if (TestConeActive)
        {
            Separator();
            DrawSlider("Length", ref TestConeRadius, 1f, 35f, "%.1f", "The length the omen being Tested.", v => _cfg.TestConeRadius = v);
            DrawSlider("Rotation", ref TestConeRotation, 1f, 7.3f, "%.001f", "The direction the omen being Tested is facing.", v => _cfg.TestConeRotation = v);
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderInt("Angle Width", ref angleIndex, 1, AllowedAngles.Length - 1))
            {
                TestConeAngleWidth = AllowedAngles[angleIndex];
                _cfg.TestConeAngleWidth = TestConeAngleWidth;
            }
            ImGui.SameLine(242, 0);
            ImGui.Text($"({AllowedAngles[angleIndex]}°)");
            ImGuiEx.HelpMarker("The cone-angle width of the omen being Tested.", DefaultColor);
            PictoService.VfxRenderer.AddCone($"{player.EntityId}", player.Position, TestConeRadius, TestConeRotation, TestConeAngleWidth, _cfg.GlobalColor);
        }

        if (TestLineActive)
        {
            Separator();
            DrawSlider("Length", ref TestLineLength, 1f, 50f, "%.1f", "The length the omen being Tested.", v => _cfg.TestLineLength = v);
            DrawSlider("Width", ref TestLineWidth, 1f, 15f, "%.1f", "The width the omen being Tested.", v => _cfg.TestLineWidth = v);
            DrawSlider("Rotation", ref TestLineRotation, 1f, 7.3f, "%.001f", "The direction the omen being Tested is facing.", v => _cfg.TestLineRotation = v);
            PictoService.VfxRenderer.AddLine($"{player.EntityId}", player.Position, TestLineLength, TestLineWidth, TestLineRotation, _cfg.GlobalColor);
        }

        if (TestDonutActive)
        {
            Separator();
            DrawSlider("Scale", ref TestDonutInnerRadius, 0.1f, 25f, "%.1f", "Scaling of t he omen being Tested.", v =>
            {
                TestDonutOuterRadius = TestDonutInnerRadius * 2f;
                _cfg.TestDonutInnerRadius = TestDonutInnerRadius;
                _cfg.TestDonutOuterRadius = TestDonutOuterRadius;
            });
            ImGui.Text($"Inner Radius: {TestDonutInnerRadius:0.0}");
            ImGui.SameLine();
            ImGuiEx.HelpMarker("Inner radius of the omen being Tested (inner safe zone).", DefaultColor);
            ImGui.Text($"Outer Radius: {TestDonutOuterRadius:0.0}");
            ImGui.SameLine();
            ImGuiEx.HelpMarker("Outer radius of the omen being Tested (outer unsafe zone).", DefaultColor);
            PictoService.VfxRenderer.AddDonut($"{player.EntityId}", player.Position, TestDonutInnerRadius, TestDonutOuterRadius, _cfg.GlobalColor);
        }

        if (TestCustomActive)
        {
            Separator();
            var omenKeys = Svc.Data.GetExcelSheet<Omen>().Select(o => o.Path.ToMacroString()).ToArray();
            var selectedIndex = Math.Clamp(_cfg.SelectedOmenIndex, 0, omenKeys.Length - 1);
            var selectedOmenName = omenKeys[selectedIndex];

            ImGui.SetNextItemWidth(175);
            if (ImGui.BeginCombo("##OmenSelect", selectedOmenName))
            {
                for (var i = 0; i < omenKeys.Length; i++)
                {
                    var isSelected = i == selectedIndex;
                    if (ImGui.Selectable(omenKeys[i], isSelected))
                    {
                        selectedIndex = i;
                        _cfg.SelectedOmenIndex = i;
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.Text("Omen Selection");
            ImGui.SameLine();
            ImGuiEx.HelpMarker("The actual names of these omens are unknown, so keys are used to select them.", DefaultColor);
            PictoService.VfxRenderer.AddOmen(player.EntityId.ToString(), selectedOmenName, player.Position, new Vector3(3), 0, _cfg.GlobalColor);
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
    private void DrawSlider(string label, ref float value, float min, float max, string format, string helpText, Action<float>? onChange = null)
    {
        ImGui.SetNextItemWidth(150);
        if (ImGui.SliderFloat(label, ref value, min, max, format))
            onChange?.Invoke(value);
        ImGui.SameLine();
        ImGuiEx.HelpMarker(helpText, DefaultColor);
    }
    private void ResetToDefault()
    {
        RainbowActive = false;
        _cfg.RainbowMode = RainbowActive;

        WhiteActive = false;
        _cfg.WhiteMode = WhiteActive;

        _cfg.NonHostile = false;
        NonHostileActive = _cfg.NonHostile;
        _cfg.NonHostileColor = DefaultColor;

        _cfg.GlobalColor = DefaultColor;

        AlphaValue = 1.2f;
        _cfg.Alpha = AlphaValue;
    }
    private void DrawTooltip(string text, bool disabled, bool forceShow = false)
    {
        var isDisabled = disabled && !forceShow;

        ImGui.BeginDisabled(isDisabled);
        ImGuiEx.HelpMarker(
            text,
            isDisabled || disabled && forceShow ? null : DefaultColor
        );
        ImGui.EndDisabled();
    }
    private static void Separator()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
}

