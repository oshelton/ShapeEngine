using Avalonia.Controls;
using Avalonia.Layout;
using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// A surface covering the whole window, with the panel positioned by Avalonia layout alone. The default
/// arrangement when no anchor is given.
/// </summary>
/// <remarks>
/// The surface covers the app's own interface as well as the game, so the panel is placed with a grid
/// whose star rows match the bands in <see cref="AvaloniaExampleLayout"/>. Star sizing is the point: a
/// fixed pixel margin that clears the title bar in a window clears far too little of it full screen.
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
            "The surface fills the window. A grid of star-sized rows and columns keeps the panel clear of the title above and the device readout below, at any resolution.")
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };

        surface = new AvaloniaSurface(AvaloniaExampleLayout.SafeArea(panel, 0.34f));
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
