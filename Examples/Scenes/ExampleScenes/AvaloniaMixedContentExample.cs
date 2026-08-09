using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// A single surface mixing native Avalonia controls, buttons with ShapeEngine-rendered icons, and
/// ShapeEngine content used as plain images.
/// </summary>
/// <remarks>
/// Where the other examples each isolate one feature, this one piles several onto the same surface: a
/// <c>TabControl</c> holding native controls on one tab, buttons whose content is a small ShapeEngine
/// render on another, and static, animated and direct ShapeEngine views used as plain images on a third -
/// proving they coexist rather than demonstrating any one of them in isolation.
/// </remarks>
public class AvaloniaMixedContentExample : AvaloniaExampleSceneBase
{
    private static readonly AvaloniaSurfaceAnchor Anchor = AvaloniaExampleLayout.LeftColumn(0.86f);

    private AvaloniaSurface? surface;
    private AvaloniaMixedContentPanel? panel;

    public AvaloniaMixedContentExample()
    {
        Title = "Avalonia - Mixed Content";
        Description = "Native controls, ShapeEngine icon buttons and ShapeEngine images on one surface";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaMixedContentPanel();
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
