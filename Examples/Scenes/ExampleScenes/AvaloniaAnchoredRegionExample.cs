using System.Numerics;
using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;
using ShapeEngine.Screen;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// A surface occupying a region of the screen defined as a fraction of the window.
/// </summary>
/// <remarks>
/// Anchor mode sizes the surface to a proportion of the window and pins it to a relative position, so
/// the UI keeps its place and proportions as the window resizes. The usual choice for a HUD panel that
/// should own one corner of the screen rather than the whole thing.
/// </remarks>
public class AvaloniaAnchoredRegionExample : AvaloniaExampleSceneBase
{
    private static readonly Vector2 AnchorStretch = new(0.34f, 0.66f);
    private static readonly Vector2 AnchorPosition = new(0.04f, 0.62f);

    private AvaloniaSurface? surface;
    private AvaloniaDemoPanel? panel;

    public AvaloniaAnchoredRegionExample()
    {
        Title = "Avalonia - Anchored Region";
        Description = "Surface occupies a fraction of the window, anchored to a relative position";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaDemoPanel(
            "Anchored region",
            "The surface is a fraction of the window pinned to a relative position, and the panel fills it. Resize the window and the region keeps its proportions.");

        var placement = new ScreenTexture(AnchorStretch, AnchorPosition, ShaderSupportType.None);

        surface = new AvaloniaSurface(panel, placement);
        return [surface];
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null) return;

        var rect = surface.DestinationRect;
        panel?.SetStatus(
            $"""
             Region {rect.Width:0}x{rect.Height:0} at ({rect.X:0}, {rect.Y:0})
             WantsPointer: {surface.WantsPointer}   WantsKeyboard: {surface.WantsKeyboard}
             """);
    }
}
