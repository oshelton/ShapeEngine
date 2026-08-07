using System.Numerics;
using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;
using ShapeEngine.Screen;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// Switches a surface between the two content sizing options, and the two rasterization options,
/// at runtime.
/// </summary>
/// <remarks>
/// Both sizing options are supported: leaving <c>DesignSize</c> null lets the layout expand into the
/// surface, while setting it pins the layout and scales everything uniformly to fit. The rasterization
/// toggle switches between drawing through the placement texture and drawing at the on-screen pixel
/// size. Resize the window with each combination to see the difference.
/// </remarks>
public class AvaloniaContentScalingExample : AvaloniaExampleSceneBase
{
    private static readonly Dimensions DesignSize = new(320, 420);
    private static readonly Vector2 AnchorStretch = new(0.36f, 0.7f);
    private static readonly Vector2 AnchorPosition = new(0.04f, 0.6f);

    private AvaloniaSurface? surface;
    private AvaloniaScalingPanel? panel;

    public AvaloniaContentScalingExample()
    {
        Title = "Avalonia - Content Scaling";
        Description = "Toggle between expanding the layout and scaling the content, and between rasterization modes";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaScalingPanel();

        var placement = new ScreenTexture(AnchorStretch, AnchorPosition, ShaderSupportType.None);
        surface = new AvaloniaSurface(panel, placement);

        panel.ScaleContentChanged += scaleContent =>
        {
            if (surface is not null) surface.DesignSize = scaleContent ? DesignSize : null;
        };

        panel.NativeDensityChanged += nativeDensity =>
        {
            if (surface is not null)
            {
                surface.Scaling = nativeDensity
                    ? AvaloniaSurfaceScaling.NativeDensity
                    : AvaloniaSurfaceScaling.MatchTexture;
            }
        };

        return [surface];
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null) return;

        var rect = surface.DestinationRect;
        var sizing = surface.DesignSize is { } design
            ? $"content scales from {design.Width}x{design.Height}"
            : "layout expands";

        panel?.SetStatus(
            $"""
             {sizing}
             {surface.Scaling}, drawn at {rect.Width:0}x{rect.Height:0}
             """);
    }
}
