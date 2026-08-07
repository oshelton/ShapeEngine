using Avalonia;
using Avalonia.Layout;
using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// A surface covering the whole window, with the panel positioned by Avalonia layout alone.
/// </summary>
/// <remarks>
/// The simplest arrangement: no placement texture, so the surface is the window and the UI is
/// positioned the way it would be in any Avalonia application. Being a custom event rather than a
/// screen texture, it also draws after the game's own UI, on top of everything.
/// </remarks>
public class AvaloniaFullWindowExample : AvaloniaExampleSceneBase
{
    private AvaloniaSurface? surface;
    private AvaloniaDemoPanel? panel;

    public AvaloniaFullWindowExample()
    {
        Title = "Avalonia - Full Window";
        Description = "Surface covers the window; the panel is placed by Avalonia layout";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaDemoPanel(
            "Full window surface",
            "The surface is the whole window. Margin and alignment place the panel, exactly as they would in a desktop Avalonia app.")
        {
            Width = 380,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(24, 90, 0, 0)
        };

        surface = new AvaloniaSurface(panel);
        return [surface];
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null) return;

        var rect = surface.DestinationRect;
        panel?.SetStatus(
            $"""
             Surface {rect.Width:0}x{rect.Height:0} (window)
             WantsPointer: {surface.WantsPointer}   WantsKeyboard: {surface.WantsKeyboard}
             """);
    }
}
