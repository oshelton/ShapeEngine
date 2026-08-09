using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// Switches a surface between laying its content out at the surface's size and scaling it to fit.
/// </summary>
/// <remarks>
/// Off, a wider window gives controls more room and text keeps its size. On, everything grows together -
/// the resolution-independent option for a game UI. Resize the window with each setting to compare.
/// </remarks>
public class AvaloniaContentScalingExample : AvaloniaExampleSceneBase
{
    private static readonly AvaloniaSurfaceAnchor Anchor = AvaloniaExampleLayout.LeftColumn(0.36f);

    private AvaloniaSurface? surface;
    private AvaloniaScalingPanel? panel;

    public AvaloniaContentScalingExample()
    {
        Title = "Avalonia - Content Scaling";
        Description = "Toggle between expanding the layout and scaling the content to fit the surface";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaScalingPanel();
        surface = new AvaloniaSurface(panel, Anchor);

        panel.ScaleContentChanged += scaleContent =>
        {
            if (surface is not null) surface.ScaleContent = scaleContent;
        };

        return [surface];
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null) return;

        var rect = surface.DestinationRect;
        var sizing = surface.ScaleContent ? "content scales to fit" : "layout expands";

        panel?.SetStatus(
            $"""
             {sizing}
             Drawn at {rect.Width:0}x{rect.Height:0}
             """);
    }
}
