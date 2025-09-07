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
        string name = id == 0 ? "Select duty..." : GetDutyName(id);

        bool opening;
        using (ImRaii.IEndObject combo = ImRaii.Combo("##DutyOverride", name))
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
        if (_popupList.Draw(_duties, out ContentFinderCondition selected))
        {
            id = (ushort)selected!.RowId;
        }
    }

    public string GetDutyName(ushort id)
    {
        ContentFinderCondition? row = GetDutyRow(id);
        if (row!.Value.RowId == default)
        {
            return "Unknown";
        }

        string name = row.Value.Name.ExtractText();
        if (name.StartsWith("The ", StringComparison.InvariantCultureIgnoreCase))
        {
            name = char.ToUpper(name[0]) + name[1..];
        }

        return name;
    }
    public ContentFinderCondition? GetDutyRow(ushort id) => _content.GetRow(id);

    private IEnumerable<ContentFinderCondition> GetDuties() => _content.Where(entry =>
    {
        uint type = entry.ContentType.RowId;
        return type is (>= 2 and <= 5) or 9 or 10 or 21 or 26 or 28 or 30;
    });

    private static bool DrawItem(ContentFinderCondition row, bool focus) => ImGui.Selectable(row.Name.ExtractText(), focus);
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
        bool result = false;

        if (_isOpen && ImGui.BeginPopup(_id))
        {
            if (_searchPredicate != null)
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##Search", "Search...", ref _searchQuery, 100);
            }

            IEnumerable<T> filteredItems = _searchPredicate != null && !string.IsNullOrEmpty(_searchQuery) ? items.Where(item => _searchPredicate(item, _searchQuery)) : items;
            if (ImGui.BeginChild("##List", new Vector2(300, 200), false))
            {
                foreach (T? item in filteredItems)
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
