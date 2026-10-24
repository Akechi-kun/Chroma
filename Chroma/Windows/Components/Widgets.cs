using Chroma.Omens;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.ImGuiMethods;
using System;
using System.Numerics;

namespace Chroma.Windows.Components;

internal static class Widgets
{
    private const float ToggleWidthRatio = 1.8f;
    private const float ToggleKnobInset = 3f;
    private const float SelectedBorderSize = 2f;
    private const float ButtonFillAlpha = 0.75f;
    private const float ButtonHoverAlpha = 0.9f;
    private const float PreviewRounding = 4f;
    private const float TitleHueSpeed = 0.1f;
    private const float TitlePastel = 0.35f;

    public const ImGuiColorEditFlags ColorPickerFlags = ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha;

    public static readonly Vector4 AccentColor = new(0.3f, 0.75f, 0.65f, 1f);

    private static readonly Vector4 SectionTitleColor = new(0.6f, 0.9f, 0.8f, 1f);
    private static readonly Vector4 DarkText = new(0f, 0f, 0f, 1f);
    private static readonly Vector4 LightText = Vector4.One;

    private static (int Count, string Text) _savedCountLabel = (-1, string.Empty);

    public static float Scaled(float pixels) => pixels * ImGuiHelpers.GlobalScale;

    public static void SpacedSeparator()
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    public static void SectionTitle(FontAwesomeIcon icon, string title)
    {
        ImGui.AlignTextToFramePadding();
        ImGuiEx.Icon(SectionTitleColor, icon);
        ImGui.SameLine();
        ImGuiEx.Text(SectionTitleColor, title);
    }

    public static void GradientTitle(string[] letters, bool vivid)
    {
        var shift = (float)(Environment.TickCount64 / 1000.0 * TitleHueSpeed % 1.0);
        for (var i = 0; i < letters.Length; i++)
        {
            if (i > 0)
                ImGui.SameLine(0, 0);

            var hue = (shift + (float)i / letters.Length) % 1f;
            var color = vivid ? Vector4.Lerp(Colors.Hue(hue), LightText, TitlePastel) : ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled];
            ImGui.TextColored(color, letters[i]);
        }
    }

    public static void SameLineRight(float itemWidth)
    {
        ImGui.SameLine();
        var rightEdge = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
        var cursor = ImGui.GetCursorScreenPos();
        var alignedX = rightEdge - itemWidth;
        if (alignedX > cursor.X)
            ImGui.SetCursorScreenPos(cursor with { X = alignedX });
    }

    public static bool RightAlignedIconButton(FontAwesomeIcon icon, string label, ref float lastWidth)
    {
        SameLineRight(lastWidth);
        var clicked = ImGuiEx.IconButtonWithText(icon, label);
        lastWidth = ImGui.GetItemRectSize().X;
        return clicked;
    }

    public static void RightAlignedTextDisabled(string text)
    {
        SameLineRight(ImGui.CalcTextSize(text).X);
        ImGui.TextDisabled(text);
    }

    public static string SavedCountLabel(int count)
    {
        if (_savedCountLabel.Count != count)
            _savedCountLabel = (count, $"{count} saved");
        return _savedCountLabel.Text;
    }

    public static void ClippedText(string text, Vector4 color, float width)
    {
        var height = ImGui.GetFrameHeight();
        var min = ImGui.GetCursorScreenPos();
        var max = min + new Vector2(Math.Max(width, 0f), height);
        ImGui.Dummy(max - min);

        var textSize = ImGui.CalcTextSize(text);
        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(min, max, true);
        drawList.AddText(min with { Y = min.Y + (height - textSize.Y) / 2f }, ImGui.GetColorU32(color), text);
        drawList.PopClipRect();

        if (textSize.X > width && ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }

    public static bool Toggle(string id, ref bool value)
    {
        var height = ImGui.GetFrameHeight();
        var size = new Vector2(height * ToggleWidthRatio, height);
        var min = ImGui.GetCursorScreenPos();

        var clicked = ImGui.InvisibleButton(id, size);
        if (clicked)
            value = !value;

        var track = value ? AccentColor : ImGui.GetStyle().Colors[(int)(ImGui.IsItemHovered() ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg)];
        var radius = height / 2f;
        var knobCenter = new Vector2(value ? min.X + size.X - radius : min.X + radius, min.Y + radius);

        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, min + size, ImGui.GetColorU32(track), radius);
        drawList.AddCircleFilled(knobCenter, radius - Scaled(ToggleKnobInset), ImGui.GetColorU32(ImGuiCol.Text));
        return clicked;
    }

    public static bool SelectableButton(string label, bool selected, float width)
    {
        using var color = ImRaii.PushColor(ImGuiCol.Button, AccentColor with { W = ButtonFillAlpha }, selected)
            .Push(ImGuiCol.ButtonHovered, AccentColor with { W = ButtonHoverAlpha }, selected)
            .Push(ImGuiCol.Text, DarkText, selected);
        return ImGui.Button(label, new Vector2(width, 0));
    }

    public static bool ColorButton(string label, Vector4 color, bool selected, float width)
    {
        var fill = Colors.WithAlpha(color, 1f);
        using var colors = ImRaii.PushColor(ImGuiCol.Button, Colors.WithAlpha(fill, ButtonFillAlpha))
            .Push(ImGuiCol.ButtonHovered, Colors.WithAlpha(fill, ButtonHoverAlpha))
            .Push(ImGuiCol.ButtonActive, fill)
            .Push(ImGuiCol.Text, ReadableTextColor(fill))
            .Push(ImGuiCol.Border, ImGui.GetStyle().Colors[(int)ImGuiCol.Text], selected);
        using var border = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, Scaled(SelectedBorderSize), selected);
        return ImGui.Button(label, new Vector2(width, 0));
    }

    public static void ColorPreview(Vector4 hostile, Vector4? nonHostile)
    {
        var min = ImGui.GetCursorScreenPos();
        var size = new Vector2(Layout.ContentWidth, ImGui.GetFrameHeight());
        ImGui.Dummy(size);

        if (nonHostile is not { } friendly)
        {
            PreviewSegment(min, size, hostile, "Hostile Omens", ImDrawFlags.RoundCornersAll);
            return;
        }

        var half = size with { X = size.X / 2f };
        PreviewSegment(min, half, hostile, "Hostile", ImDrawFlags.RoundCornersLeft);
        PreviewSegment(min with { X = min.X + half.X }, half, friendly, "Non-Hostile", ImDrawFlags.RoundCornersRight);
    }

    private static void PreviewSegment(Vector2 min, Vector2 size, Vector4 color, string label, ImDrawFlags corners)
    {
        var fill = Displayable(color);
        var textPosition = min + (size - ImGui.CalcTextSize(label)) / 2f;
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, min + size, ImGui.GetColorU32(fill), Scaled(PreviewRounding), corners);
        drawList.AddText(textPosition, ImGui.GetColorU32(ReadableTextColor(fill)), label);
    }

    private static Vector4 Displayable(Vector4 color) => Colors.WithAlpha(Vector4.Clamp(color, Vector4.Zero, Vector4.One), 1f);

    private static Vector4 ReadableTextColor(Vector4 background) => Luminance(background) > 0.5f ? DarkText : LightText;

    private static float Luminance(Vector4 color) => 0.299f * color.X + 0.587f * color.Y + 0.114f * color.Z;
}
