using System.Numerics;
using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;
using ShapeEngine.Screen;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// Animated Avalonia controls and content running over a moving game scene.
/// </summary>
/// <remarks>
/// Avalonia has no render thread here: its animation clock is advanced by the surface's per-frame render
/// tick, so this is the scene that shows the clock actually working. The scene also drives the
/// transition-based animations directly, which shows game state animating Avalonia content.
/// </remarks>
public class AvaloniaAnimationExample : AvaloniaExampleSceneBase
{
    private const float TransitionInterval = 1.6f;

    private static readonly Vector2 AnchorStretch = new(0.36f, 0.82f);
    private static readonly Vector2 AnchorPosition = new(0.04f, 0.55f);

    private AvaloniaSurface? surface;
    private AvaloniaAnimationPanel? panel;
    private float transitionTimer;

    public AvaloniaAnimationExample()
    {
        Title = "Avalonia - Animation";
        Description = "Keyframe animations, property transitions and cross-faded content driven by the game loop";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaAnimationPanel();

        var placement = new ScreenTexture(AnchorStretch, AnchorPosition, ShaderSupportType.None);

        surface = new AvaloniaSurface(panel, placement);
        transitionTimer = 0f;

        return [surface];
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null || panel is null) return;

        transitionTimer += time.Delta;
        if (transitionTimer >= TransitionInterval)
        {
            transitionTimer -= TransitionInterval;
            panel.AdvanceTransitions();
        }

        var rect = surface.DestinationRect;
        panel.SetStatus(
            $"""
             Drawn at {rect.Width:0}x{rect.Height:0}, next transition in {TransitionInterval - transitionTimer:0.0}s
             WantsPointer: {surface.WantsPointer}   WantsKeyboard: {surface.WantsKeyboard}
             """);
    }
}
