using System.Numerics;
using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;
using ShapeEngine.Screen;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// Animated ShapeEngine drawing hosted inside an Avalonia control.
/// </summary>
/// <remarks>
/// The reverse of the other scenes: instead of Avalonia drawing over the game, the game's own drawing
/// functions produce content that sits inside the Avalonia control tree. It goes through a render
/// texture rather than handing the OpenGL context between the two renderers, which costs a per-frame
/// read back but keeps raylib and Skia entirely out of each other's way.
/// </remarks>
public class AvaloniaEmbeddedDrawingExample : AvaloniaExampleSceneBase
{
    private static readonly Dimensions DesignSize = new(340, 570);
    private static readonly Vector2 AnchorStretch = new(0.36f, 0.78f);
    private static readonly Vector2 AnchorPosition = new(0.04f, 0.56f);

    private AvaloniaSurface? surface;
    private AvaloniaEmbeddedDrawingPanel? panel;

    public AvaloniaEmbeddedDrawingExample()
    {
        Title = "Avalonia - Embedded Drawing";
        Description = "ShapeEngine shape drawing animated inside an Avalonia control";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaEmbeddedDrawingPanel();

        var placement = new ScreenTexture(AnchorStretch, AnchorPosition, ShaderSupportType.None);

        // Scales the whole panel, artwork included: the texture view sizes its render texture from the
        // surface's render scaling, so the drawing is rasterized at the scaled size rather than
        // magnified.
        surface = new AvaloniaSurface(panel, placement) { DesignSize = DesignSize };

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
