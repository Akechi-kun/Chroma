using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Chroma;

public class ConfigWindow : Window
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Config _config;
    private readonly Manager _manager;
    private readonly DutyWindow _dutyWindow;
    private bool rainbow = false;
    private float speed = 0.05f;

    public ConfigWindow(IDalamudPluginInterface pluginInterface, Config config, Manager manager, DutyWindow dutyWindow) : base($"Chroma v{pluginInterface.Manifest.AssemblyVersion}")
    {
        _pluginInterface = pluginInterface;
        _config = config;
        _manager = manager;
        _dutyWindow = dutyWindow;
        Size = new Vector2(350, 200);
        Flags |= ImGuiWindowFlags.AlwaysAutoResize;
    }

    public override void OnClose() => _pluginInterface.SavePluginConfig(_config);

    public override void Draw()
    {
        if (ImGui.Checkbox("Enable Chroma", ref _config.Enabled))
        {
            _manager.Enabled = _config.Enabled;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.ColorEdit4("Global Omen Color", ref _config.Color, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaPreview);
        ImGui.Spacing();
        if (ImGui.Button("Red"))
        {
            rainbow = false;
            _config.Color = new Vector4(1, 0, 0, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Green"))
        {
            rainbow = false;
            _config.Color = new Vector4(0, 1, 0, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Blue"))
        {
            rainbow = false;
            _config.Color = new Vector4(0, 0, 1, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Yellow"))
        {
            rainbow = false;
            _config.Color = new Vector4(1, 1, 0, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Purple"))
        {
            rainbow = false;
            _config.Color = new Vector4(0.5f, 0, 0.5f, 1);
        }
        ImGui.SameLine();
        if (ImGui.Button("Pink"))
        {
            rainbow = false;
            _config.Color = new Vector4(1, 0, 1, 1);
        }
        ImGui.Spacing();
        if (ImGui.Checkbox("Rainbow", ref rainbow))
        {
            _config.RainbowMode = rainbow;
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "Override the color for a specific duty.\n" +
            "When a duty entry is added, enabled, and you are inside that duty, " +
            "its assigned color will take precedence over the global color setting."
        );
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        ImGui.SliderFloat("Speed", ref speed, 0.01f, 0.4f, "%.2fx");
        if (rainbow)
        {
            float t = (float)ImGui.GetTime();
            float hue = (t * speed) % 1.0f;
            float a = _config.Color.W;
            Vector4 rgb = HsvToRgb(hue, 1.0f, 1.0f);
            rgb.W = a;
            _config.Color = rgb;
        }
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        if (ImGui.Button("Duty Overrides"))
        {
            _dutyWindow.Toggle();
            _pluginInterface.SavePluginConfig(_config);
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "Override the color for a specific duty.\n" +
            "When a duty entry is added, enabled, and you are inside that duty, " +
            "its assigned color will take precedence over the global color setting."
        );
    }

    private static Vector4 HsvToRgb(float h, float s, float v)
    {
        int i = (int)(h * 6f);
        float f = h * 6f - i;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);

        return (i % 6) switch
        {
            0 => new Vector4(v, t, p, 1),
            1 => new Vector4(q, v, p, 1),
            2 => new Vector4(p, v, t, 1),
            3 => new Vector4(p, q, v, 1),
            4 => new Vector4(t, p, v, 1),
            _ => new Vector4(v, p, q, 1),
        };
    }
}

public class DutyWindow : Window
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly Config _config;
    private readonly DutyOverride _dutyOverride;
    public ushort _newDutyId;
    private Config.DutyEntries _newEntry = new();
    private readonly IClientState _clientState;

    public DutyWindow(IDalamudPluginInterface pluginInterface, Config config, DutyOverride dutyOverride, IClientState clientState) : base("Duty Overrides")
    {
        _pluginInterface = pluginInterface;
        _config = config;
        _dutyOverride = dutyOverride;
        _clientState = clientState;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(666, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void OnClose() => _pluginInterface.SavePluginConfig(_config);

    public override void Draw()
    {
        ImGui.TextColored(0xFFFFDCEB, $"Jobs");
        ImGuiComponents.HelpMarker("Select the jobs you wish to use Orbwalker's movement locking features on. Not all jobs have cast times, but if you have the extra features enabled for the general actions it will apply to those jobs.");
        ImGui.Separator();
        using ImRaii.IEndObject table = ImRaii.Table("##DutyTable", 4,
            ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
            ImGuiTableFlags.NoSavedSettings);

        if (!table.Success)
        {
            return;
        }
        ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableSetupColumn("Duty", ImGuiTableColumnFlags.WidthStretch, 2f);
        ImGui.TableSetupColumn("Color", ImGuiTableColumnFlags.WidthStretch, 2.5f);
        ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 100);
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("Hold Ctrl and click to delete");

        ImGui.TableHeadersRow();

        float colorComponentWidth = 60;

        foreach ((ushort id, Config.DutyEntries entry) in _config.DutyColors.ToList())
        {
            using ImRaii.Id imId = ImRaii.PushId($"##DutyEntry_{id}");
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.Checkbox($"##Enabled_{id}", ref entry.Enabled);

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(_dutyOverride.GetDutyName(id));

            ImGui.TableSetColumnIndex(2);
            ImGui.SetNextItemWidth(colorComponentWidth * 4);
            ImGui.ColorEdit4($"##Color_{id}", ref entry.Color, ImGuiColorEditFlags.Float);

            ImGui.TableSetColumnIndex(3);
            bool safety = ImGui.IsKeyDown(ImGuiKey.ModCtrl);
            using (ImRaii.IEndObject _ = ImRaii.Disabled(!safety))
            {
                if (ImGui.Button("Remove##Btn", new Vector2(-1, 0)))
                {
                    _config.DutyColors.Remove(id);
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
        ImGui.ColorEdit4("##NewDutyColor", ref _newEntry.Color);

        if (_newDutyId != 0)
        {
            ushort addedId = _newDutyId;
            Lumina.Excel.Sheets.ContentFinderCondition? row = _dutyOverride.GetDutyRow(addedId);
            string name = row?.Name.ExtractText() ?? "Unknown";
            _newEntry.DutyId = addedId;
            _newEntry.Name = name;
            _newEntry.TerritoryTypeId = ((ushort)row!.Value.TerritoryType.Value.RowId);
            _config.DutyColors[addedId] = _newEntry;
            _newDutyId = 0;
            _newEntry = new Config.DutyEntries
            {
                Color = _newEntry.Color
            };

            _pluginInterface.SavePluginConfig(_config);
        }
    }

    public ushort? GetTerritoryType()
    {
        ushort territoryId = _clientState.TerritoryType;
        if (territoryId == 0)
        {
            return null;
        }

        return _clientState.TerritoryType;
    }
}