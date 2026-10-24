using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ECommons.ImGuiMethods;
using System;
using System.Numerics;

namespace Chroma.Windows.Components;

internal static class Layout
{
    private const float BoxPadding = 8f;
    private const float BoxRounding = 6f;
    private const float AccentWidth = 3f;
    private const float AccentInset = 2f;
    private const float LabelColumnWidth = 100f;
    private const float BoxBackgroundAlpha = 0.45f;
    private const float ButtonGroupGap = 4f;

    private static Vector2 _boxStart;
    private static float _boxRight;
    private static float _contentLeft;
    private static float _contentRight;
    private static float _controlLeft;
    private static float _helpLeft;

    public static float ContentWidth => _contentRight - _contentLeft;

    public static float ControlWidth { get; private set; }

    public static float RemainingControlWidth => _controlLeft + ControlWidth - ImGui.GetCursorScreenPos().X;

    private static float Indent => Widgets.Scaled(BoxPadding + AccentWidth);

    public static void BeginBox()
    {
        var padding = Widgets.Scaled(BoxPadding);
        _boxStart = ImGui.GetCursorScreenPos();
        _boxRight = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        _contentLeft = _boxStart.X + Indent;
        _contentRight = _boxRight - padding;
        _controlLeft = _contentLeft + Widgets.Scaled(LabelColumnWidth);
        _helpLeft = _contentRight - ImGuiEx.CalcIconSize(FontAwesomeIcon.InfoCircle).X;
        ControlWidth = _helpLeft - ImGui.GetStyle().ItemSpacing.X - _controlLeft;

        var drawList = ImGui.GetWindowDrawList();
        drawList.ChannelsSplit(2);
        drawList.ChannelsSetCurrent(1);

        ImGui.BeginGroup();
        ImGui.Dummy(new Vector2(0, Math.Max(padding - ImGui.GetStyle().ItemSpacing.Y, 0)));
        ImGui.Indent(Indent);
    }

    public static void EndBox()
    {
        var padding = Widgets.Scaled(BoxPadding);
        ImGui.Unindent(Indent);
        ImGui.Dummy(new Vector2(0, Math.Max(padding - ImGui.GetStyle().ItemSpacing.Y, 0)));
        ImGui.EndGroup();

        var min = _boxStart;
        var max = new Vector2(_boxRight, ImGui.GetItemRectMax().Y);
        var rounding = Widgets.Scaled(BoxRounding);
        var inset = Widgets.Scaled(AccentInset);
        var accentMin = new Vector2(min.X + inset, min.Y + rounding);
        var accentMax = new Vector2(min.X + inset + Widgets.Scaled(AccentWidth), max.Y - rounding);

        var drawList = ImGui.GetWindowDrawList();
        drawList.ChannelsSetCurrent(0);
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.FrameBg, BoxBackgroundAlpha), rounding);
        drawList.AddRectFilled(accentMin, accentMax, ImGui.GetColorU32(Widgets.AccentColor), rounding);
        drawList.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border), rounding);
        drawList.ChannelsMerge();
    }

    public static void Label(string text)
    {
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(text);
        SameLineAt(_controlLeft);
    }

    public static void Help(string text)
    {
        SameLineAt(_helpLeft);
        ImGuiEx.HelpMarker(text, sameLine: false);
    }

    public static void WrappedValue(string text, Vector4 color)
    {
        ImGui.PushTextWrapPos(_contentRight - ImGui.GetWindowPos().X);
        ImGui.TextColored(color, text);
        ImGui.PopTextWrapPos();
    }

    public static void Note(FontAwesomeIcon icon, string text, Vector4 color)
    {
        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = _contentLeft });
        ImGuiEx.Icon(color, icon);
        ImGui.SameLine();
        WrappedValue(text, color);
    }

    public static IDisposable ButtonGroupSpacing() =>
        ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = Widgets.Scaled(ButtonGroupGap) });

    public static float GridCellWidth(int columns) =>
        (ContentWidth - ImGui.GetStyle().ItemSpacing.X * (columns - 1)) / columns;

    public static float SplitControlWidth(int parts) =>
        (ControlWidth - ImGui.GetStyle().ItemSpacing.X * (parts - 1)) / parts;

    public static void GridNext(int index, int columns)
    {
        if (index % columns != 0)
            ImGui.SameLine();
    }

    public static void Divider()
    {
        var spacing = ImGui.GetStyle().ItemSpacing.Y;
        var y = ImGui.GetCursorScreenPos().Y + spacing / 2f;
        ImGui.GetWindowDrawList().AddLine(new Vector2(_contentLeft, y), new Vector2(_contentRight, y), ImGui.GetColorU32(ImGuiCol.Separator));
        ImGui.Dummy(new Vector2(0, spacing));
    }

    public static void SameLineAt(float x)
    {
        ImGui.SameLine();
        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = x });
    }
}
