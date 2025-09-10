using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Chroma;

public class DutyOverride(IDataManager data)
{
    private readonly ExcelSheet<ContentFinderCondition> _content = data.GetExcelSheet<ContentFinderCondition>()!;
    private List<ContentFinderCondition>? _duties;
    private readonly PopupList<ContentFinderCondition> _popupList = new PopupList<ContentFinderCondition>("##DutyOverride", DrawItem).WithSearch(SearchPredicate);

    public void Draw(ref ushort id)
    {
        var name = id == 0 ? "Select duty..." : GetDutyName(id);
        bool opening;
        using (var combo = ImRaii.Combo("##DutyOverride", name))
        {
            opening = combo.Success;
            if (opening)
            {
                ImGui.CloseCurrentPopup();
            }
        }
        if (opening)
        {
            _popupList.Open();
        }

        _duties ??= [.. GetDuties()];
        if (_popupList.Draw(_duties, out var selected))
        {
            id = (ushort)selected!.RowId;
        }
    }

    public string GetDutyName(ushort id)
    {
        var row = GetDutyRow(id);
        return row?.RowId == default ? "Unknown" : FormatDutyName(row!.Value.Name.ExtractText());
    }

    public ContentFinderCondition? GetDutyRow(ushort id) => _content.GetRow(id);

    private IEnumerable<ContentFinderCondition> GetDuties() => _content.Where(entry =>
    {
        var type = entry.ContentType.RowId;
        return type is (>= 2 and <= 5) or 9 or 10 or 21 or 26 or 28 or 30;
    });

    private static string FormatDutyName(string rawName) => rawName.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase) ? char.ToUpper(rawName[0]) + rawName[1..] : rawName;

    private static bool DrawItem(ContentFinderCondition row, bool focus)
    {
        var name = FormatDutyName(row.Name.ExtractText());
        return ImGui.Selectable(name, focus);
    }
    private static bool SearchPredicate(ContentFinderCondition row, string query) => row.Name.ExtractText().Contains(query, StringComparison.InvariantCultureIgnoreCase);
}
public class PopupList<T>(string id, Func<T, bool, bool> drawItem)
{
    private readonly string _id = id;
    private readonly Func<T, bool, bool> _drawItem = drawItem;
    private Func<T, string, bool>? _searchPredicate;
    private bool _isOpen;
    private string _searchQuery = string.Empty;

    public PopupList<T> WithSearch(Func<T, string, bool> searchPredicate)
    {
        _searchPredicate = searchPredicate;
        return this;
    }

    public void Open()
    {
        _isOpen = true;
        _searchQuery = string.Empty;
        ImGui.OpenPopup(_id);
    }

    public bool Draw(IEnumerable<T> items, out T? selected)
    {
        selected = default;
        var result = false;

        if (_isOpen && ImGui.BeginPopup(_id))
        {
            if (_searchPredicate != null)
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##Search", "Search...", ref _searchQuery, 100);
            }

            var filteredItems = _searchPredicate != null && !string.IsNullOrEmpty(_searchQuery) ? items.Where(item => _searchPredicate(item, _searchQuery)) : items;
            if (ImGui.BeginChild("##List", new Vector2(300, 200), false))
            {
                foreach (var item in filteredItems)
                {
                    if (_drawItem(item, false))
                    {
                        selected = item;
                        result = true;
                        _isOpen = false;
                        ImGui.CloseCurrentPopup();
                        break;
                    }
                }
                ImGui.EndChild();
            }

            if (ImGui.Button("Close") || ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                _isOpen = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        return result;
    }
}
