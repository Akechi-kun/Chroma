using Chroma.Omens;
using Chroma.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Icon = Dalamud.Interface.FontAwesomeIcon;

namespace Chroma.Windows;

public sealed class DutyWindow : ConfigWindow
{
    private readonly record struct Duty(ushort Id, ushort Territory, string Name, string Category, string Label);

    private const float WindowWidth = 420f;
    private const int MaxVisibleRows = 8;
    private const int MaxFilterLength = 100;

    private static readonly uint[] SupportedContentTypes = [2, 3, 4, 5, 9, 10, 21, 26, 28, 30];

    private readonly SearchCombo<Duty> _dutySearch;
    private readonly Func<Duty, bool> _hasOverride;
    private readonly List<ushort> _visibleOverrides = [];
    private readonly ColorPicker _newColorPicker = new("##NewDutyColor");
    private Duty[]? _allDuties;
    private Dictionary<ushort, Duty>? _dutiesById;
    private Vector4 _newOverrideColor = new(1f, 0f, 0f, 1f);
    private (uint Territory, Duty Duty) _currentDutyCache = (uint.MaxValue, default);
    private (ushort DutyId, string Text) _addCurrentLabel = (ushort.MaxValue, string.Empty);
    private string _filter = string.Empty;
    private (int Count, uint Territory, string Filter)? _visibleOverridesKey;

    public DutyWindow(Config config) : base("Duty Overrides###ChromaDuties", config)
    {
        _dutySearch = new(() => AllDuties, duty => duty.Label);
        _hasOverride = HasOverride;
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
    }

    private Duty[] AllDuties => _allDuties ??= LoadDuties();

    private Dictionary<ushort, Duty> DutiesById => _dutiesById ??= AllDuties.ToDictionary(duty => duty.Id);

    private Duty CurrentDuty
    {
        get
        {
            var territory = Svc.ClientState.TerritoryType;
            if (_currentDutyCache.Territory != territory)
                _currentDutyCache = (territory, AllDuties.FirstOrDefault(duty => duty.Territory == territory));
            return _currentDutyCache.Duty;
        }
    }

    public override void PreDraw()
    {
        var width = Widgets.Scaled(WindowWidth);
        ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0), new Vector2(width, float.MaxValue));
    }

    protected override void DrawContents()
    {
        Widgets.SectionTitle(Icon.Plus, "Add Override");
        Layout.BeginBox();
        DrawNewColor();
        DrawDutySearch();
        DrawAddCurrentDuty();
        Layout.EndBox();

        ImGui.Spacing();

        Widgets.SectionTitle(Icon.List, "Saved Overrides");
        Widgets.RightAlignedTextDisabled(Widgets.SavedCountLabel(Config.DutyColors.Count));
        Layout.BeginBox();
        DrawSavedOverrides();
        Layout.EndBox();
    }

    private void DrawNewColor()
    {
        Layout.Label("Color");
        UnsavedChanges |= _newColorPicker.Draw(ref _newOverrideColor, out _);
        Layout.Help("The color new overrides are added with. You can change each override's color later in the list below.");
    }

    private void DrawDutySearch()
    {
        Layout.Label("Duty");
        if (_dutySearch.Draw("##AddDuty", "Search duties...", Layout.ControlWidth, _hasOverride, out var duty))
            AddOverride(duty);
        Layout.Help(
            "Pick any dungeon, trial, raid or other duty to add it with the selected color.\n" +
            "You can also search by duty type, such as \"Trial\" or \"Raid\".");
    }

    private void DrawAddCurrentDuty()
    {
        var duty = CurrentDuty;
        var inDuty = duty.Id != 0;
        var canAdd = inDuty && !HasOverride(duty);

        Layout.Label("Current");
        using (ImRaii.Disabled(!canAdd))
        {
            if (ImGui.Button(AddCurrentLabel(duty), new Vector2(Layout.ControlWidth, 0)))
                AddOverride(duty);
        }
        Layout.Help(inDuty
            ? "Adds the duty you are currently in with the selected color."
            : "You are not currently inside a supported duty.");
    }

    private void DrawSavedOverrides()
    {
        if (Config.DutyColors.Count == 0)
        {
            ImGui.TextDisabled("No duty overrides yet. Add one above.");
            return;
        }

        DrawListToolbar();
        RefreshVisibleOverrides();

        if (_visibleOverrides.Count == 0)
        {
            ImGui.TextDisabled("No overrides match your filter.");
            return;
        }

        var style = ImGui.GetStyle();
        var rowHeight = ImGui.GetFrameHeight() + style.ItemSpacing.Y;
        var visibleRows = Math.Min(_visibleOverrides.Count, MaxVisibleRows);
        var listSize = new Vector2(Layout.ContentWidth, visibleRows * rowHeight - style.ItemSpacing.Y);

        ushort? overrideToRemove = null;
        if (ImGui.BeginChild("##SavedOverrides", listSize))
        {
            var territory = Svc.ClientState.TerritoryType;
            var ctrlHeld = ImGui.GetIO().KeyCtrl;
            foreach (var dutyId in _visibleOverrides)
            {
                var entry = Config.DutyColors[dutyId];
                if (DrawOverrideRow(dutyId, entry, entry.TerritoryTypeId == territory, ctrlHeld))
                    overrideToRemove = dutyId;
            }
        }
        ImGui.EndChild();

        if (overrideToRemove is { } removedId)
            RemoveOverride(removedId);
    }

    private void DrawListToolbar()
    {
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var enableWidth = ImGuiEx.CalcIconSize(Icon.ToggleOn, isButton: true).X;
        var disableWidth = ImGuiEx.CalcIconSize(Icon.ToggleOff, isButton: true).X;

        ImGui.SetNextItemWidth(Layout.ContentWidth - enableWidth - disableWidth - spacing * 2);
        ImGui.InputTextWithHint("##Filter", "Filter by name or type...", ref _filter, MaxFilterLength);

        ImGui.SameLine();
        if (ImGuiEx.IconButton(Icon.ToggleOn))
            SetAllEnabled(true);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Enable all overrides.");

        ImGui.SameLine();
        if (ImGuiEx.IconButton(Icon.ToggleOff))
            SetAllEnabled(false);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Disable all overrides.");
    }

    private bool DrawOverrideRow(ushort dutyId, Config.DutyEntries entry, bool isCurrentDuty, bool ctrlHeld)
    {
        using var id = ImRaii.PushId(dutyId);
        var style = ImGui.GetStyle();
        var spacing = style.ItemSpacing.X;
        var rowRight = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        var removeLeft = rowRight - ImGuiEx.CalcIconSize(Icon.Trash, isButton: true).X;
        var colorLeft = removeLeft - spacing - ImGui.GetFrameHeight();
        var category = CategoryOf(dutyId);
        var categoryWidth = category.Length > 0 ? ImGui.CalcTextSize(category).X : 0f;
        var categoryLeft = colorLeft - spacing - categoryWidth;

        UnsavedChanges |= Widgets.Toggle("##Enabled", ref entry.Enabled);
        ImGui.SameLine();

        var nameLeft = ImGui.GetCursorScreenPos().X;
        var nameColor = isCurrentDuty ? ImGuiColors.HealerGreen : style.Colors[(int)(entry.Enabled ? ImGuiCol.Text : ImGuiCol.TextDisabled)];
        Widgets.ClippedText(DisplayName(dutyId, entry), nameColor, categoryLeft - spacing - nameLeft);

        if (category.Length > 0)
        {
            Layout.SameLineAt(categoryLeft);
            Widgets.ClippedText(category, style.Colors[(int)ImGuiCol.TextDisabled], categoryWidth);
        }

        Layout.SameLineAt(colorLeft);
        UnsavedChanges |= ImGui.ColorEdit4("##Color", ref entry.OmenColor, Widgets.ColorPickerFlags);

        Layout.SameLineAt(removeLeft);
        bool removeClicked;
        using (ImRaii.Disabled(!ctrlHeld))
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed, ctrlHeld))
            removeClicked = ImGuiEx.IconButton(Icon.Trash);
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            ImGui.SetTooltip(ctrlHeld ? "Remove this override." : "Hold Ctrl and click to remove.");

        return removeClicked;
    }

    private void RefreshVisibleOverrides()
    {
        var territory = Svc.ClientState.TerritoryType;
        var key = (Config.DutyColors.Count, territory, _filter);
        if (_visibleOverridesKey == key)
            return;

        _visibleOverridesKey = key;
        _visibleOverrides.Clear();
        _visibleOverrides.AddRange(Config.DutyColors
            .Where(pair => MatchesFilter(pair.Key, pair.Value))
            .OrderByDescending(pair => pair.Value.TerritoryTypeId == territory)
            .ThenBy(pair => DisplayName(pair.Key, pair.Value), StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Key));
    }

    private bool MatchesFilter(ushort dutyId, Config.DutyEntries entry) =>
        _filter.Length == 0 ||
        DisplayName(dutyId, entry).Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
        CategoryOf(dutyId).Contains(_filter, StringComparison.OrdinalIgnoreCase);

    private string DisplayName(ushort dutyId, Config.DutyEntries entry) =>
        DutiesById.TryGetValue(dutyId, out var duty) ? duty.Name : Capitalize(entry.Name);

    private string CategoryOf(ushort dutyId) =>
        DutiesById.TryGetValue(dutyId, out var duty) ? duty.Category : string.Empty;

    private bool HasOverride(Duty duty) => Config.DutyColors.ContainsKey(duty.Id);

    private void AddOverride(Duty duty)
    {
        Config.DutyColors[duty.Id] = new Config.DutyEntries
        {
            DutyId = duty.Id,
            TerritoryTypeId = duty.Territory,
            Name = duty.Name,
            OmenColor = Colors.WithAlpha(_newOverrideColor, 1f)
        };
        _visibleOverridesKey = null;
        UnsavedChanges = true;
        Notify.Info($"Added a duty override for {duty.Name}.");
    }

    private void RemoveOverride(ushort dutyId)
    {
        if (!Config.DutyColors.Remove(dutyId, out var removed))
            return;

        Svc.Log.Information($"Removed duty override for '{removed.Name}' (Duty ID: {removed.DutyId}, Territory Type ID: {removed.TerritoryTypeId})");
        _visibleOverridesKey = null;
        UnsavedChanges = true;
        Notify.Info($"Removed the duty override for {DisplayName(dutyId, removed)}.");
    }

    private void SetAllEnabled(bool enabled)
    {
        foreach (var entry in Config.DutyColors.Values)
            entry.Enabled = enabled;
        UnsavedChanges = true;
        Notify.Info(enabled ? "All duty overrides enabled." : "All duty overrides disabled.");
    }

    private string AddCurrentLabel(Duty duty)
    {
        if (_addCurrentLabel.DutyId != duty.Id)
            _addCurrentLabel = (duty.Id, duty.Id == 0 ? "Not in a duty" : $"Add {duty.Name}");
        return HasOverride(duty) ? "Already added" : _addCurrentLabel.Text;
    }

    private static Duty[] LoadDuties() => [.. Svc.Data.GetExcelSheet<ContentFinderCondition>()
        .Where(IsSupportedDuty)
        .Select(ToDuty)
        .OrderBy(duty => duty.Name, StringComparer.OrdinalIgnoreCase)];

    private static Duty ToDuty(ContentFinderCondition row)
    {
        var name = Capitalize(row.Name.ExtractText());
        var category = row.ContentType.ValueNullable?.Name.ExtractText() ?? string.Empty;
        var label = category.Length > 0 ? $"{name}  ·  {category}" : name;
        return new Duty((ushort)row.RowId, (ushort)row.TerritoryType.RowId, name, category, label);
    }

    private static bool IsSupportedDuty(ContentFinderCondition row) =>
        !row.Name.IsEmpty && SupportedContentTypes.Contains(row.ContentType.RowId);

    private static string Capitalize(string name) =>
        name.Length > 0 && char.IsLower(name[0]) ? char.ToUpper(name[0]) + name[1..] : name;
}
