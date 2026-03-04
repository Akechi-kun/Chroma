using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Chroma.UI;

public class DutyWindow : Window
{
    private readonly Config _cfg;
    private readonly DutyOverride _do;
    public ushort _newDutyId;
    private Config.DutyEntries _newEntry = new();

    public DutyWindow(Config config, DutyOverride dutyOverride) : base("Duty Overrides")
    {
        _cfg = config;
        _do = dutyOverride;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(666, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void OnClose() => Svc.PluginInterface.SavePluginConfig(_cfg);

    public override void Draw()
    {
        using var table = ImRaii.Table("##DutyTable", 4,
            ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg |
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV |
            ImGuiTableFlags.NoSavedSettings);

        if (!table.Success)
        {
            Svc.Log.Error("Failed to create Duty Overrides table.");
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
        foreach ((var id, var entry) in _cfg.DutyColors.ToList())
        {
            using var imId = ImRaii.PushId($"##DutyEntry_{id}");
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Checkbox($"##Enabled_{id}", ref entry.Enabled);
            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(_do.GetDutyName(id));
            ImGui.TableSetColumnIndex(2);
            ImGui.SetNextItemWidth(colorComponentWidth * 4);
            ImGui.ColorEdit4($"##Color_{id}", ref entry.OmenColor, ImGuiColorEditFlags.Float);
            ImGui.TableSetColumnIndex(3);
            var safety = ImGui.IsKeyDown(ImGuiKey.ModCtrl);
            using (var _ = ImRaii.Disabled(!safety))
            {
                if (ImGui.Button("Remove##Btn", new Vector2(-1, 0)))
                {
                    _cfg.DutyColors.Remove(id);
                    Svc.Log.Information($"Removed duty override for '{entry.Name}' (Duty ID: {entry.DutyId}, Territory Type ID: {entry.TerritoryTypeId})");
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
        _do.Draw(ref _newDutyId);

        ImGui.TableSetColumnIndex(2);
        ImGui.SetNextItemWidth(colorComponentWidth * 4);
        ImGui.ColorEdit4("##NewDutyColor", ref _newEntry.OmenColor);
        if (_newDutyId != 0)
        {
            var addedId = _newDutyId;
            var row = _do.GetDutyRow(addedId);
            var name = row?.Name.ExtractText() ?? "Unknown";
            _newEntry.DutyId = addedId;
            _newEntry.Name = name;
            _newEntry.TerritoryTypeId = ((ushort)row!.Value.TerritoryType.Value.RowId);
            _cfg.DutyColors[addedId] = _newEntry;
            _newDutyId = 0;
            _newEntry = new Config.DutyEntries
            {
                OmenColor = _newEntry.OmenColor
            };

            Svc.PluginInterface.SavePluginConfig(_cfg);
        }
    }
}
public class DutyOverride
{
    private readonly ExcelSheet<ContentFinderCondition> _cfcSheet = Svc.Data.GetExcelSheet<ContentFinderCondition>()!;
    private List<ContentFinderCondition>? _duties;
    private readonly PopupList<ContentFinderCondition> _list = new PopupList<ContentFinderCondition>("##DutyOverride", DrawItem).WithSearch(Search);

    public void Draw(ref ushort id)
    {
        var name = id == 0 ? "Select duty..." : GetDutyName(id);
        bool opening;
        using (var combo = ImRaii.Combo("##DutyOverride", name))
        {
            opening = combo.Success;
            if (opening)
                ImGui.CloseCurrentPopup();
        }
        if (opening)
            _list.Open();

        _duties ??= [.. GetDuties()];

        if (_list.Draw(_duties, out var selected))
        {
            id = (ushort)selected!.RowId;
        }
    }

    private static string Format(string name) => name.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase) ? char.ToUpper(name[0]) + name[1..] : name;
    public ContentFinderCondition? GetDutyRow(ushort id) => _cfcSheet.GetRow(id);
    public string GetDutyName(ushort id) => GetDutyRow(id)?.RowId == default ? "Unknown" : Format(GetDutyRow(id)!.Value.Name.ExtractText());
    private IEnumerable<ContentFinderCondition> GetDuties() => _cfcSheet.Where(entry => entry.ContentType.RowId is (>= 2 and <= 5) or 9 or 10 or 21 or 26 or 28 or 30);
    private static bool DrawItem(ContentFinderCondition row, bool focus) => ImGui.Selectable(Format(row.Name.ExtractText()), focus);
    private static bool Search(ContentFinderCondition row, string query) => row.Name.ExtractText().Contains(query, StringComparison.InvariantCultureIgnoreCase);
}
public class PopupList<T>(string id, Func<T, bool, bool> drawItem)
{
    private readonly string _id = id;
    private readonly Func<T, bool, bool> _drawItem = drawItem;
    private Func<T, string, bool>? Search;
    private bool IsOpen;
    private string Query = string.Empty;

    public PopupList<T> WithSearch(Func<T, string, bool> search)
    {
        Search = search;
        return this;
    }

    public void Open()
    {
        IsOpen = true;
        Query = string.Empty;
        ImGui.OpenPopup(_id);
    }

    public bool Draw(IEnumerable<T> items, out T? selected)
    {
        selected = default;
        var result = false;

        if (IsOpen && ImGui.BeginPopup(_id))
        {
            if (Search != null)
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##Search", "Search...", ref Query, 100);
            }

            var filteredItems = Search != null && !string.IsNullOrEmpty(Query) ? items.Where(item => Search(item, Query)) : items;
            if (ImGui.BeginChild("##List", new Vector2(300, 200), false))
            {
                foreach (var item in filteredItems)
                {
                    if (_drawItem(item, false))
                    {
                        selected = item;
                        result = true;
                        IsOpen = false;
                        ImGui.CloseCurrentPopup();
                        break;
                    }
                }
                ImGui.EndChild();
            }

            if (ImGui.Button("Close") || ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                IsOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        return result;
    }
}
