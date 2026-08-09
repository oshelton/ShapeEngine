using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// ShapeEngine drawing hosted inside Avalonia controls.
/// </summary>
/// <remarks>
/// The reverse of the other scenes: instead of Avalonia drawing over the game, the game's own drawing
/// functions produce content that sits inside the Avalonia control tree. Shows both routes - the texture
/// views, which keep raylib and Skia out of each other's way, and the direct view, which hands raylib
/// Avalonia's framebuffer.
/// </remarks>
public class AvaloniaEmbeddedDrawingExample : AvaloniaExampleSceneBase
{
    private static readonly AvaloniaSurfaceAnchor Anchor = AvaloniaExampleLayout.LeftColumn(0.36f);

    private AvaloniaSurface? surface;
    private AvaloniaEmbeddedDrawingPanel? panel;

    public AvaloniaEmbeddedDrawingExample()
    {
        Title = "Avalonia - Embedded Drawing";
        Description = "ShapeEngine shape drawing inside Avalonia controls, through a texture and direct";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaEmbeddedDrawingPanel();

        // Scales the whole panel, artwork included. Both view kinds take their resolution from the
        // surface's render scaling, so the drawing is rasterized at the scaled size, not magnified.
        surface = new AvaloniaSurface(panel, Anchor, scaleContent: true);

        return [surface];
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null || panel is null) return;

        panel.Advance(time.Delta);

        var rect = surface.DestinationRect;
        panel.SetStatus(
            $"""
             Drawn at {rect.Width:0}x{rect.Height:0}
             WantsPointer: {surface.WantsPointer}   WantsKeyboard: {surface.WantsKeyboard}
             """);
    }
}
