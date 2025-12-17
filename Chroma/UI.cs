using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Pictomancy;
using System;
using System.Linq;
using System.Numerics;
using Icon = Dalamud.Interface.FontAwesomeIcon;

namespace Chroma;

public class ConfigWindow : Window
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Config _config;
    private readonly Manager _manager;
    private readonly DutyWindow _dutyWindow;
    private readonly IObjectTable _objectTable;
    private readonly IDataManager _data;
    private readonly IPluginLog _log;

    private bool rainbow = false;
    private float speed = 0.05f;
    private bool includeFriendly = false;
    private bool testEnabled = false;
    private float testCircleRadius = 1f;
    private bool testCircleActive = false;
    private bool testConeActive = false;
    private float testConeRadius = 1f;
    private float testConeRotation = 1f;
    private readonly int[] AllowedAngles = [10, 15, 20, 30, 45, 60, 80, 90, 120, 130, 135, 150, 180, 210, 225, 270, 360];
    private int angleIndex = 0;
    private int testConeAngleWidth = 0;
    private bool testLineActive = false;
    private float testLineLength = 1f;
    private float testLineWidth = 1f;
    private float testLineRotation = 1f;
    private bool testDonutActive = false;
    private float testDonutInnerRadius = 0.1f;
    private float testDonutOuterRadius = 0.2f;
    private bool testCustomActive = false;
    private Vector4 titleColor = new(0.678f, 0.847f, 0.902f, 1);
    //private bool testLockOnActive = false;
    //private float testLockOnScale = 1f;

    public ConfigWindow(IDalamudPluginInterface pluginInterface, Config config, Manager manager, DutyWindow dutyWindow, IObjectTable objectTable, IDataManager data, IPluginLog log) : base($"Chroma v{pluginInterface.Manifest.AssemblyVersion}")
    {
        _pluginInterface = pluginInterface;
        _config = config;
        _manager = manager;
        _dutyWindow = dutyWindow;
        _objectTable = objectTable;
        _data = data;
        _log = log;
        Size = new Vector2(340, 510);
        Flags |= ImGuiWindowFlags.AlwaysAutoResize;
        rainbow = _config.RainbowMode;
        speed = _config.Speed;
        includeFriendly = _config.IncludeFriendly;
        testEnabled = _config.TestOmenEnabled;
        testCircleRadius = _config.TestCircleRadius;
        testCircleActive = _config.TestCircleActive;
        testConeActive = _config.TestConeActive;
        testConeRadius = _config.TestConeRadius;
        testConeRotation = _config.TestConeRotation;
        testConeAngleWidth = _config.TestConeAngleWidth;
        testLineActive = _config.TestLineActive;
        testLineLength = _config.TestLineLength;
        testLineWidth = _config.TestLineWidth;
        testLineRotation = _config.TestLineRotation;
        testDonutActive = _config.TestDonutActive;
        testDonutInnerRadius = _config.TestDonutInnerRadius;
        testDonutOuterRadius = _config.TestDonutOuterRadius;
        testCustomActive = _config.TestCustomActive;
        //testLockOnActive = _config.TestLockOnActive;
        //testLockOnScale = _config.TestLockOnScale;
    }

    public override void Draw()
    {
        if (ImGui.Checkbox("Enable Chroma", ref _config.Enabled))
        {
            _manager.Enabled = _config.Enabled;
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
        DrawRainbowMode();
        ImGuiGroup.EndGroupBox();
        ImGui.Spacing();
        ImGui.TextColored(titleColor, "Duty-specific Overrides");
        ImGuiGroup.BeginGroupBox();
        DrawDutyOverrides();
        ImGuiGroup.EndGroupBox();
        ImGui.Spacing();
        ImGui.TextColored(titleColor, "Testing");
        ImGuiGroup.BeginGroupBox();
        DrawOmenTesting();
        ImGuiGroup.EndGroupBox();
    }
    private void DrawColorMenu()
    {
        ImGui.ColorEdit4("Global Omen Color", ref _config.OmenColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
        ImGui.Spacing();
        if (ImGui.Button("Red"))
        {
            rainbow = false;
            _config.OmenColor = new Vector4(1, 0, 0, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Green"))
        {
            rainbow = false;
            _config.OmenColor = new Vector4(0, 1, 0, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Blue"))
        {
            rainbow = false;
            _config.OmenColor = new Vector4(0, 0, 1, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Yellow"))
        {
            rainbow = false;
            _config.OmenColor = new Vector4(1, 1, 0, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Purple"))
        {
            rainbow = false;
            _config.OmenColor = new Vector4(0.5f, 0, 0.5f, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Pink"))
        {
            rainbow = false;
            _config.OmenColor = new Vector4(1, 0, 1, 1);
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.Checkbox("Include Friendly Omens", ref includeFriendly))
        {
            _config.IncludeFriendly = includeFriendly;
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("If enabled, non-hostile omens/indicators (from self, party, alliance, etc.) will also be colored.");
    }
    private void DrawRainbowMode()
    {
        if (ImGui.Checkbox("Rainbow Mode", ref rainbow))
        {
            _config.RainbowMode = rainbow;
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("This will randomize the colors of your omens/indicators.");
        if (rainbow)
        {
            ImGui.Indent();
            ImGui.SetNextItemWidth(150);
            if (ImGui.SliderFloat("Speed", ref speed, 0.01f, 0.4f, "%.2f"))
            {
                _config.Speed = speed;
            }
            ImGui.Unindent();
        }
    }
    private void DrawDutyOverrides()
    {
        if (ImGuiComponents.IconButtonWithText(Icon.Bullseye, "Open Duty Overrides Window"))
        {
            _dutyWindow.Toggle();
            _pluginInterface.SavePluginConfig(_config);
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Override the omen color for a specific duty.\nWhen a duty entry is added, enabled, and you are inside that duty, its assigned color will take precedence over the global color setting.");
    }
    private void DrawOmenTesting()
    {
        if (ImGui.Checkbox("Enable Omen Testing", ref testEnabled))
        {
            _config.TestOmenEnabled = testEnabled;
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("This is for testing how your omens will appear.\nIf enabled, the omen will appear on your character until cleared with the 'Clear' button, or a different omen is selected.");
        if (_config.TestOmenEnabled && (testCircleActive == true || testConeActive == true || testLineActive == true || testDonutActive == true || testCustomActive == true))
        {
            ImGui.SameLine(230, 0);
            if (ImGuiComponents.IconButtonWithText(Icon.Explosion, "Clear"))
            {
                testCircleActive = false;
                testLineActive = false;
                testConeActive = false;
                testDonutActive = false;
                //testLockOnActive = false;
                testCustomActive = false;
                PictoService.VfxRenderer.Dispose();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("If you can see this 'Clear' button, it means there is currently an omen active from testing.\nUse this button to clear any active omens that are currently being displayed.");
        }
        ImGui.Spacing();
        if (_config.TestOmenEnabled)
        {
            if (ImGui.Button("Circle"))
            {
                testCircleActive = true;
                testConeActive = false;
                testLineActive = false;
                testDonutActive = false;
                //testLockOnActive = false;
                testCustomActive = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Line"))
            {
                testLineActive = true;
                testCircleActive = false;
                testConeActive = false;
                testDonutActive = false;
                //testLockOnActive = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Cone"))
            {
                testConeActive = true;
                testCircleActive = false;
                testLineActive = false;
                testDonutActive = false;
                //testLockOnActive = false;
                testCustomActive = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Donut"))
            {
                testDonutActive = true;
                testCircleActive = false;
                testConeActive = false;
                testLineActive = false;
                //testLockOnActive = false;
                testCustomActive = false;
            }
            ImGui.SameLine();
            if (ImGui.Button("Custom"))
            {
                testCustomActive = true;
                testDonutActive = false;
                testCircleActive = false;
                testConeActive = false;
                testLineActive = false;
                //testLockOnActive = false;
            }
            /*
            ImGui.SameLine();
            if (ImGui.Button("Lock-On"))
            {
                testLockOnActive = true;
                testCircleActive = false;
                testConeActive = false;
                testLineActive = false;
                testDonutActive = false;
            }
            */
            var player = _objectTable.LocalPlayer;
            if (testCircleActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Length", ref testCircleRadius, 1f, 15f, "%.1f"))
                {
                    _config.TestCircleRadius = testCircleRadius;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The length the omen being tested.");
                PictoService.VfxRenderer.AddCircle($"{player!.EntityId}", player.Position, testCircleRadius, _config.OmenColor);
            }
            if (testConeActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Length", ref testConeRadius, 1f, 35f, "%.1f"))
                {
                    _config.TestConeRadius = testConeRadius;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The length the omen being tested.");
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Rotation", ref testConeRotation, 1f, 7.3f, "%.001f"))
                {
                    _config.TestConeRotation = testConeRotation;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The direction the omen being tested is facing.");
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderInt("Angle Width", ref angleIndex, 1, AllowedAngles.Length - 1))
                {
                    testConeAngleWidth = AllowedAngles[angleIndex];
                    _config.TestConeAngleWidth = testConeAngleWidth;
                }
                ImGui.SameLine(242, 0);
                ImGui.Text($"({AllowedAngles[angleIndex]}°)");
                ImGuiComponents.HelpMarker("The cone-angle width of the omen being tested.");
                PictoService.VfxRenderer.AddCone($"{player!.EntityId}", player.Position, testConeRadius, testConeRotation, testConeAngleWidth, _config.OmenColor);
            }
            if (testLineActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Length", ref testLineLength, 1f, 50, "%.1f"))
                {
                    _config.TestLineLength = testLineLength;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The length the omen being tested.");
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Width", ref testLineWidth, 1f, 15f, "%.1f"))
                {
                    _config.TestLineWidth = testLineWidth;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The width the omen being tested.");
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Rotation", ref testLineRotation, 1f, 7.3f, "%.001f"))
                {
                    _config.TestLineRotation = testLineRotation;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The direction the omen being tested is facing.");
                PictoService.VfxRenderer.AddLine($"{player!.EntityId}", player.Position, testLineLength, testLineWidth, testLineRotation, _config.OmenColor);
            }
            if (testDonutActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Scale", ref testDonutInnerRadius, 0.1f, 25f, "%.1f"))
                {
                    testDonutOuterRadius = testDonutInnerRadius * 2f;
                    _config.TestDonutInnerRadius = testDonutInnerRadius;
                    _config.TestDonutOuterRadius = testDonutOuterRadius;
                }
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The scaling of the omen being tested.\nDue to there being multiple instances where the omen will just not show, the inner and outer radiuses are locked into a scaling where it is always visible.");
                ImGui.Text($"Inner Radius: {testDonutInnerRadius:0.0}");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The inner radius of the omen being tested (inner safe zone).");
                ImGui.Text($"Outer Radius: {testDonutOuterRadius:0.0}");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("The outer radius of the omen being tested (outer unsafe zone).");
                PictoService.VfxRenderer.AddDonut($"{player!.EntityId}", player.Position, testDonutInnerRadius, testDonutOuterRadius, _config.OmenColor);
            }
            if (testCustomActive)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                var omenKeys = _data.GetExcelSheet<Omen>().Select(o => o.Path.ToMacroString()).ToArray();
                var selectedIndex = (_config.SelectedOmenIndex >= 0 && _config.SelectedOmenIndex < omenKeys.Length) ? _config.SelectedOmenIndex : 0;
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
                            _config.SelectedOmenIndex = i;
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
                PictoService.VfxRenderer.AddOmen(player!.EntityId.ToString(), selectedOmenName, player.Position, new Vector3(3), 0, _config.OmenColor);
            }

            /* TODO: fix - this crashes the game xdd
            if (testLockOnActive)
            {
                ImGui.Spacing();
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Scale", ref testLockOnScale, 0.1f, 10f, "%.1f"))
                {
                    _config.TestLockOnScale = testLockOnScale;
                }
                PictoService.VfxRenderer.AddLockon($"{player!.EntityId}", "tank_lockon01i", player, color: _config.OmenColor);
            }
            */
        }
        else
        {
            PictoService.VfxRenderer.Dispose();
        }

    }
    private void DrawSupport()
    {
        if (ImGuiComponents.IconButton(Icon.Bug))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/Akechi-kun/Chroma/issues", UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to open GitHub Issues page: {ex.Message}");
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
                _log.Error($"Failed to open GitHub link: {ex.Message}");
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
                _log.Error($"Failed to open Sponsor link: {ex.Message}");
            }
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Sponsor");
        }
    }
}

public class DutyWindow : Window
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Config _config;
    private readonly DutyOverride _dutyOverride;
    public ushort _newDutyId;
    private Config.DutyEntries _newEntry = new();
    private readonly IObjectTable _objectTable;
    private readonly IPluginLog _log;

    public DutyWindow(IDalamudPluginInterface pluginInterface, Config config, DutyOverride dutyOverride, IObjectTable objectTable, IPluginLog log) : base("Duty Overrides")
    {
        _pluginInterface = pluginInterface;
        _config = config;
        _dutyOverride = dutyOverride;
        _objectTable = objectTable;
        _log = log;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(666, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void OnClose() => _pluginInterface.SavePluginConfig(_config);

    public override void Draw()
    {
        using var table = ImRaii.Table("##DutyTable", 4,
            ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
            ImGuiTableFlags.NoSavedSettings);

        if (!table.Success)
        {
            _log.Error("Failed to create Duty Overrides table.");
            return;
        }
        ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Duty", ImGuiTableColumnFlags.WidthStretch, 2f);
        ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthStretch, 2.5f);
        ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Hold Ctrl and click to delete");
        ImGui.TableHeadersRow();

        var colorComponentWidth = 60;
        foreach ((var id, var entry) in _config.DutyColors.ToList())
        {
            using var imId = ImRaii.PushId($"##DutyEntry_{id}");
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Checkbox($"##Enabled_{id}", ref entry.Enabled);
            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(_dutyOverride.GetDutyName(id));
            ImGui.TableSetColumnIndex(2);
            ImGui.SetNextItemWidth(colorComponentWidth * 4);
            ImGui.ColorEdit4($"##Color_{id}", ref entry.OmenColor, ImGuiColorEditFlags.Float);
            ImGui.TableSetColumnIndex(3);
            var safety = ImGui.IsKeyDown(ImGuiKey.ModCtrl);
            using (var _ = ImRaii.Disabled(!safety))
            {
                if (ImGui.Button("Remove##Btn", new Vector2(-1, 0)))
                {
                    _config.DutyColors.Remove(id);
                    _log.Information($"Removed duty override for '{entry.Name}' (Duty ID: {entry.DutyId}, Territory Type ID: {entry.TerritoryTypeId})");
                    return;
                }
            }
            if (!safety && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Hold Ctrl and click to delete.");
            }
        }

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(1);
        _dutyOverride.Draw(ref _newDutyId);

        ImGui.TableSetColumnIndex(2);
        ImGui.SetNextItemWidth(colorComponentWidth * 4);
        ImGui.ColorEdit4("##NewDutyColor", ref _newEntry.OmenColor);
        if (_newDutyId != 0)
        {
            var addedId = _newDutyId;
            var row = _dutyOverride.GetDutyRow(addedId);
            var name = row?.Name.ExtractText() ?? "Unknown";
            _newEntry.DutyId = addedId;
            _newEntry.Name = name;
            _newEntry.TerritoryTypeId = ((ushort)row!.Value.TerritoryType.Value.RowId);
            _config.DutyColors[addedId] = _newEntry;
            _newDutyId = 0;
            _newEntry = new Config.DutyEntries
            {
                OmenColor = _newEntry.OmenColor
            };

            _pluginInterface.SavePluginConfig(_config);
        }
    }
}

