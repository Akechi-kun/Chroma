using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Chroma.Windows.Components;

internal sealed class SearchCombo<T>(Func<IReadOnlyList<T>> getItems, Func<T, string> getLabel)
{
    private const int MaxQueryLength = 100;
    private const float ListHeight = 250f;

    private string _query = string.Empty;
    private string? _filteredQuery;
    private List<T> _filteredItems = [];

    public bool Draw(string id, string preview, float width, Func<T, bool> isSelected, out T? picked)
    {
        picked = default;
        ImGui.SetNextItemWidth(width);
        if (!ImGui.BeginCombo(id, preview, ImGuiComboFlags.HeightLargest))
            return false;

        DrawSearchBox();
        RefreshFilter();
        var wasPicked = DrawList(isSelected, out picked);

        if (wasPicked)
            ImGui.CloseCurrentPopup();
        ImGui.EndCombo();
        return wasPicked;
    }

    private void DrawSearchBox()
    {
        if (ImGui.IsWindowAppearing())
        {
            _query = string.Empty;
            ImGui.SetKeyboardFocusHere();
        }
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##Search", "Search...", ref _query, MaxQueryLength);
    }

    private void RefreshFilter()
    {
        if (_filteredQuery == _query)
            return;

        _filteredQuery = _query;
        _filteredItems = _query.Length == 0
            ? [.. getItems()]
            : [.. getItems().Where(item => getLabel(item).Contains(_query, StringComparison.OrdinalIgnoreCase))];
    }

    private bool DrawList(Func<T, bool> isSelected, out T? picked)
    {
        T? clicked = default;
        var wasClicked = false;
        if (ImGui.BeginChild("##List", new Vector2(0, ListHeight * ImGuiHelpers.GlobalScale)))
        {
            ImGuiClip.ClippedDraw(_filteredItems, item =>
            {
                if (!ImGui.Selectable(getLabel(item), isSelected(item)))
                    return;

                clicked = item;
                wasClicked = true;
            }, ImGui.GetTextLineHeightWithSpacing());
        }
        ImGui.EndChild();

        picked = clicked;
        return wasClicked;
    }
}
