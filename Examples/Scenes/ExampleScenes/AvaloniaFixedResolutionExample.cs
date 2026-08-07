using Avalonia;
using Avalonia.Layout;
using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;
using ShapeEngine.Screen;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// A surface rendered at a fixed design resolution and letterboxed into the window.
/// </summary>
/// <remarks>
/// The classic "design once, scale everywhere" arrangement. The UI always lays out against 960x540, so
/// a layout that fits at one window size fits at every window size; the texture is then scaled to fit
/// the window with its aspect ratio preserved.
/// </remarks>
public class AvaloniaFixedResolutionExample : AvaloniaExampleSceneBase
{
    private static readonly Dimensions DesignResolution = new(960, 540);

    private AvaloniaSurface? surface;
    private AvaloniaDemoPanel? panel;

    public AvaloniaFixedResolutionExample()
    {
        Title = "Avalonia - Fixed Resolution";
        Description = $"UI laid out at {DesignResolution.Width}x{DesignResolution.Height} and letterboxed into the window";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaDemoPanel(
            "Fixed design resolution",
            $"Laid out against {DesignResolution.Width}x{DesignResolution.Height} whatever the window size, then scaled to fit with the aspect ratio preserved.")
        {
            Width = 400,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(24, 24, 0, 0)
        };

        // ScreenTextureMode.Fixed keeps the texture at these dimensions and letterboxes it into the
        // window, which is what makes the layout resolution independent.
        var placement = new ScreenTexture(DesignResolution, ShaderSupportType.None);

        surface = new AvaloniaSurface(panel, placement);
        return [surface];
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null) return;

        var rect = surface.DestinationRect;
        panel?.SetStatus(
            $"""
             Layout {DesignResolution.Width}x{DesignResolution.Height}, drawn at {rect.Width:0}x{rect.Height:0}
             WantsPointer: {surface.WantsPointer}   WantsKeyboard: {surface.WantsKeyboard}
             """);
    }
}
