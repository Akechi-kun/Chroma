using Chroma.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
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

    private bool RainbowActive = false;
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
    private Vector4 titleColor = new(0.678f, 0.847f, 0.902f, 1);
    private readonly int[] AllowedAngles = [10, 15, 20, 30, 45, 60, 80, 90, 120, 130, 135, 150, 180, 210, 225, 270, 360];

    public ChromaWindow(Config config, Manager manager, DutyWindow dutyWindow) : base($"Chroma v{Svc.PluginInterface.Manifest.AssemblyVersion}")
    {
        _cfg = config;
        _mgr = manager;
        _dutyWindow = dutyWindow;
        Size = new Vector2(340, 535);
        Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        RainbowActive = _cfg.RainbowMode;
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

    public override void Draw()
    {
        if (ImGui.Checkbox("Enable Chroma", ref _cfg.Enabled))
        {
            _mgr.Enabled = _cfg.Enabled;
        }
        ImGui.SameLine(240, 0);
        DrawSupport();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(titleColor, "Chroma Settings");
        ImGuiGroup.BeginGroupBox();
        DrawColorMenu();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawRainbowMode();
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        DrawNonLethals();
        ImGuiGroup.EndGroupBox();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(titleColor, "Duty-specific Overrides");
        ImGuiGroup.BeginGroupBox();
        DrawDutyOverrides();
        ImGuiGroup.EndGroupBox();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(titleColor, "Testing");
        ImGuiGroup.BeginGroupBox();
        DrawOmenTesting();
        ImGuiGroup.EndGroupBox();
    }
    private void DrawColorMenu()
    {
        ImGui.ColorEdit4("Global Omen Color", ref _cfg.GlobalColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);

        ImGui.Spacing();

        DrawColorButton("Red", new Vector4(1f, 0f, 0f, 1f));
        ImGui.SameLine();
        DrawColorButton("Green", new Vector4(0f, 1f, 0f, 1f));
        ImGui.SameLine();
        DrawColorButton("Blue", new Vector4(0f, 0f, 1f, 1f));
        ImGui.SameLine();
        DrawColorButton("Yellow", new Vector4(1f, 1f, 0f, 1f));
        ImGui.SameLine();
        DrawColorButton("Purple", new Vector4(0.5f, 0f, 0.5f, 1f));
        ImGui.SameLine();
        DrawColorButton("Pink", new Vector4(1f, 0f, 1f, 1f));
    }
    private void DrawColorButton(string name, Vector4 color)
    {
        if (!ImGui.Button(name))
            return;

        RainbowActive = false;
        _cfg.GlobalColor = color;
    }
    private void DrawRainbowMode()
    {
        if (ImGui.Checkbox("Rainbow Mode", ref RainbowActive))
        {
            _cfg.RainbowMode = RainbowActive;
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("This will randomize the colors of your omens/indicators.");
        if (RainbowActive == true)
        {
            ImGui.Indent();
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("Speed", ref CurrentSpeed, 0.01f, 0.4f, "%.2f"))
            {
                _cfg.Speed = CurrentSpeed;
            }
            ImGui.Unindent();
        }
    }
    private void DrawNonLethals()
    {
        if (ImGui.Checkbox("Include Non-Hostile Omens", ref NonHostileActive))
            _cfg.NonHostile = NonHostileActive;
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("If enabled, non-hostile omens/indicators (from self, party, alliance, etc.) will also be colored.");

        if (NonHostileActive == true)
        {
            ImGui.Indent();
            ImGui.ColorEdit4("Non-Hostile Omen Color", ref _cfg.NonHostileColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
            ImGui.Unindent();
        }
    }
    private void DrawDutyOverrides()
    {
        if (ImGuiComponents.IconButtonWithText(Icon.Bullseye, "Open Duty Overrides Window"))
        {
            _dutyWindow.Toggle();
            Svc.PluginInterface.SavePluginConfig(_cfg);
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Override the omen color for a specific duty.\nWhen a duty entry is added, enabled, and you are inside that duty, its assigned color will take precedence over the global color setting.");
    }
    private void DrawOmenTesting()
    {
        if (ImGui.Checkbox("Enable Omen Testing", ref TestEnabled))
        {
            _cfg.TestingEnabled = TestEnabled;
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("This is for Testing how your omens will appear.\nIf enabled, the omen will appear on your character until cleared with the 'Clear' button, or a different omen is selected.");
        if (_cfg.TestingEnabled && (TestCircleActive == true || TestConeActive == true || TestLineActive == true || TestDonutActive == true || TestCustomActive == true))
        {
            ImGui.SameLine(230, 0);
            if (ImGuiComponents.IconButtonWithText(Icon.Explosion, "Clear"))
            {
                TestCircleActive = false;
                TestLineActive = false;
                TestConeActive = false;
                TestDonutActive = false;
                TestCustomActive = false;
                PictoService.VfxRenderer.Dispose();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("If you can see this 'Clear' button, it means there is currently an omen active from Testing.\nUse this button to clear any active omens that are currently being displayed.");
        }
        ImGui.Spacing();
        if (_cfg.TestingEnabled)
        {
            if (ImGui.Button("Circle"))
            {
                TestCircleActive = true;
                TestConeActive = false;
                TestLineActive = false;
                TestDonutActive = false;
                TestCustomActive = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Line"))
            {
                TestLineActive = true;
                TestCircleActive = false;
                TestConeActive = false;
                TestDonutActive = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Cone"))
            {
                TestConeActive = true;
                TestCircleActive = false;
                TestLineActive = false;
                TestDonutActive = false;
                TestCustomActive = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Donut"))
            {
                TestDonutActive = true;
                TestCircleActive = false;
                TestConeActive = false;
                TestLineActive = false;
                TestCustomActive = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Custom"))
            {
                TestCustomActive = true;
                TestDonutActive = false;
                TestCircleActive = false;
                TestConeActive = false;
                TestLineActive = false;
            }
            var player = Svc.Objects.LocalPlayer;
            if (TestCircleActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Length", ref TestCircleRadius, 1f, 15f, "%.1f"))
                {
                    _cfg.TestCircleRadius = TestCircleRadius;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The length the omen being Tested.");
                PictoService.VfxRenderer.AddCircle($"{player!.EntityId}", player.Position, TestCircleRadius, _cfg.GlobalColor);
            }
            if (TestConeActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Length", ref TestConeRadius, 1f, 35f, "%.1f"))
                {
                    _cfg.TestConeRadius = TestConeRadius;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The length the omen being Tested.");
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Rotation", ref TestConeRotation, 1f, 7.3f, "%.001f"))
                {
                    _cfg.TestConeRotation = TestConeRotation;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The direction the omen being Tested is facing.");
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderInt("Angle Width", ref angleIndex, 1, AllowedAngles.Length - 1))
                {
                    TestConeAngleWidth = AllowedAngles[angleIndex];
                    _cfg.TestConeAngleWidth = TestConeAngleWidth;
                }
                ImGui.SameLine(242, 0);
                ImGui.Text($"({AllowedAngles[angleIndex]}°)");
                ImGuiComponents.HelpMarker("The cone-angle width of the omen being Tested.");
                PictoService.VfxRenderer.AddCone($"{player!.EntityId}", player.Position, TestConeRadius, TestConeRotation, TestConeAngleWidth, _cfg.GlobalColor);
            }
            if (TestLineActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Length", ref TestLineLength, 1f, 50, "%.1f"))
                {
                    _cfg.TestLineLength = TestLineLength;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The length the omen being Tested.");
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Width", ref TestLineWidth, 1f, 15f, "%.1f"))
                {
                    _cfg.TestLineWidth = TestLineWidth;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The width the omen being Tested.");
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Rotation", ref TestLineRotation, 1f, 7.3f, "%.001f"))
                {
                    _cfg.TestLineRotation = TestLineRotation;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The direction the omen being Tested is facing.");
                PictoService.VfxRenderer.AddLine($"{player!.EntityId}", player.Position, TestLineLength, TestLineWidth, TestLineRotation, _cfg.GlobalColor);
            }
            if (TestDonutActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Scale", ref TestDonutInnerRadius, 0.1f, 25f, "%.1f"))
                {
                    TestDonutOuterRadius = TestDonutInnerRadius * 2f;
                    _cfg.TestDonutInnerRadius = TestDonutInnerRadius;
                    _cfg.TestDonutOuterRadius = TestDonutOuterRadius;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The scaling of the omen being Tested.\nDue to there being multiple instances where the omen will just not show, the inner and outer radiuses are locked into a scaling where it is always visible.");
                ImGui.Text($"Inner Radius: {TestDonutInnerRadius:0.0}");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The inner radius of the omen being Tested (inner safe zone).");
                ImGui.Text($"Outer Radius: {TestDonutOuterRadius:0.0}");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The outer radius of the omen being Tested (outer unsafe zone).");
                PictoService.VfxRenderer.AddDonut($"{player!.EntityId}", player.Position, TestDonutInnerRadius, TestDonutOuterRadius, _cfg.GlobalColor);
            }
            if (TestCustomActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                var omenKeys = Svc.Data.GetExcelSheet<Omen>().Select(o => o.Path.ToMacroString()).ToArray();
                var selectedIndex = (_cfg.SelectedOmenIndex >= 0 && _cfg.SelectedOmenIndex < omenKeys.Length) ? _cfg.SelectedOmenIndex : 0;
                var selectedOmenName = omenKeys[selectedIndex];
                ImGui.SetNextItemWidth(175);

                if (ImGui.BeginCombo("##OmenSelect", omenKeys[selectedIndex]))
                {
                    for (var i = 0; i < omenKeys.Length; i++)
                    {
                        var isSelected = (i == selectedIndex);
                        if (ImGui.Selectable(omenKeys[i], isSelected))
                        {
                            selectedIndex = i;
                            _cfg.SelectedOmenIndex = i;
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                ImGui.Text("Omen Selection");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The actual names of these omens are unknown, so unfortunately we're left with these keys as our only source of searching through to find any specific omens.");
                PictoService.VfxRenderer.AddOmen(player!.EntityId.ToString(), selectedOmenName, player.Position, new Vector3(3), 0, _cfg.GlobalColor);
            }
        }
        else
        {
            PictoService.VfxRenderer.Dispose();
        }

    }
    private static void DrawSupport()
    {
        if (ImGuiComponents.IconButton(Icon.Bug))
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
        if (ImGuiComponents.IconButton(Icon.Star))
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
        if (ImGuiComponents.IconButton(Icon.Heart))
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
}

