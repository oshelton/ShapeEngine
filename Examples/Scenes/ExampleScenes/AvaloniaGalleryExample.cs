using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// A single surface mixing native Avalonia controls, buttons with ShapeEngine-rendered icons, ShapeEngine
/// content used as plain images, and Avalonia's own animation system.
/// </summary>
/// <remarks>
/// Where the other examples each isolate one feature, this one piles several onto the same surface: a
/// <c>TabControl</c> holding native controls on one tab, buttons whose content is a small ShapeEngine
/// render on another, static, animated and direct ShapeEngine views used as plain images on a third, and
/// Avalonia's own keyframe animations, property transitions and cross-fades on a fourth - proving they
/// coexist rather than demonstrating any one of them in isolation. The ShapeEngine tab's animated and direct
/// views carry the more elaborate drawing - concentric orbit rings and a bar chart - alongside a seeded
/// static emblem that regenerates on its own button, independent of the hero above it. The Animation tab's
/// transitions are triggered by this scene on a timer, rather than by a timer inside the view, so the
/// motion is visibly driven by the game rather than by Avalonia running independently.
/// </remarks>
public class AvaloniaGalleryExample : AvaloniaExampleSceneBase
{
    private const float TransitionInterval = 1.6f;

    private static readonly AvaloniaSurfaceAnchor Anchor = AvaloniaExampleLayout.CenteredColumn(0.86f);

    private AvaloniaSurface? surface;
    private AvaloniaGalleryPanel? panel;
    private float transitionTimer;

    public AvaloniaGalleryExample()
    {
        Title = "Avalonia - Gallery";
        Description = "Native controls, ShapeEngine icon buttons, ShapeEngine images and Avalonia animations on one surface";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaGalleryPanel();
        surface = new AvaloniaSurface(panel, Anchor, scaleContent: true);
        transitionTimer = 0f;

        return [surface];
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null || panel is null) return;

        panel.Advance(time.Delta);

        transitionTimer += time.Delta;
        if (transitionTimer >= TransitionInterval)
        {
            transitionTimer -= TransitionInterval;
            panel.AdvanceTransitions();
        }

        var rect = surface.DestinationRect;
        panel.SetStatus(
            $"""
             Drawn at {rect.Width:0}x{rect.Height:0}
             WantsPointer: {surface.WantsPointer}   WantsKeyboard: {surface.WantsKeyboard}
             """);
    }
}
