using Chroma.Omens;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace Chroma.Windows.Components;

internal sealed class ColorPicker(string id)
{
    private Vector4 _hexColor = new(-1f);
    private string _hexText = string.Empty;

    public bool Draw(ref Vector4 color, out bool editFinished)
    {
        var changed = ImGui.ColorEdit4(id, ref color, Widgets.ColorPickerFlags);
        editFinished = ImGui.IsItemDeactivatedAfterEdit();
        ImGui.SameLine();
        ImGui.TextDisabled(HexText(color));
        return changed;
    }

    private string HexText(Vector4 color)
    {
        if (_hexColor != color)
            (_hexColor, _hexText) = (color, Colors.ToHex(color));
        return _hexText;
    }
}
