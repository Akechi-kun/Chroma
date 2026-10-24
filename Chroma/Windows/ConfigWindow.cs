using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Chroma.Windows;

public abstract class ConfigWindow(string name, Config config) : Window(name)
{
    protected readonly Config Config = config;
    protected bool UnsavedChanges;

    public sealed override void Draw()
    {
        DrawContents();

        if (UnsavedChanges && !ImGui.IsAnyItemActive())
            SaveChanges();
    }

    public override void OnClose() => SaveChanges();

    protected abstract void DrawContents();

    private void SaveChanges()
    {
        Config.Save();
        UnsavedChanges = false;
    }
}
