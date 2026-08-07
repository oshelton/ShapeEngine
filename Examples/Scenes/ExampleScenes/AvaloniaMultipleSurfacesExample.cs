using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;
using ShapeEngine.Screen;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// Four independent Avalonia surfaces on screen at once, each with its own placement and settings.
/// </summary>
/// <remarks>
/// Every surface is a separate Avalonia top level with its own control tree, focus and input state,
/// all sharing raylib's single OpenGL context. Each keeps its own input capture, so typing into one
/// panel's text box does not disturb the others or the game.
/// <para>
/// The three placed surfaces use different scaling settings so the differences are visible side by
/// side; the fourth has no placement texture, which puts it on top of the others.
/// </para>
/// </remarks>
public class AvaloniaMultipleSurfacesExample : AvaloniaExampleSceneBase
{
    private readonly List<(AvaloniaSurface Surface, AvaloniaHudPanel Panel)> panels = [];

    public AvaloniaMultipleSurfacesExample()
    {
        Title = "Avalonia - Multiple Surfaces";
        Description = "Four independent Avalonia surfaces sharing one OpenGL context";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        // No placement texture, so this one covers the window and draws after the others.
        var overlayPanel = new AvaloniaHudPanel(
            "Full window overlay",
            Color.FromRgb(255, 140, 200),
            new TextBlock()
                .Text("No placement texture, so this surface covers the window and draws last.")
                .TextWrapping(TextWrapping.Wrap)
                .FontSize(12)
                .Foreground(Brushes.Gainsboro))
        {
            Width = 260,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 90, 0, 0)
        };

        var overlay = new AvaloniaSurface(overlayPanel, order: 1);
        panels.Add((overlay, overlayPanel));

        return
        [
            CreatePanel(
                "Top left",
                Color.FromRgb(120, 200, 255),
                new Vector2(0.26f, 0.3f),
                new Vector2(0.03f, 0.16f),
                AvaloniaSurfaceScaling.MatchTexture,
                designSize: null),
            CreatePanel(
                "Bottom left, scaled",
                Color.FromRgb(160, 255, 160),
                new Vector2(0.26f, 0.3f),
                new Vector2(0.03f, 0.84f),
                AvaloniaSurfaceScaling.MatchTexture,
                designSize: new Dimensions(220, 200)),
            CreatePanel(
                "Right, native density",
                Color.FromRgb(255, 190, 120),
                new Vector2(0.24f, 0.34f),
                new Vector2(0.97f, 0.5f),
                AvaloniaSurfaceScaling.NativeDensity,
                designSize: null),
            overlay
        ];
    }

    private AvaloniaSurface CreatePanel(
        string title,
        Color accent,
        Vector2 anchorStretch,
        Vector2 anchorPosition,
        AvaloniaSurfaceScaling scaling,
        Dimensions? designSize)
    {
        var panel = new AvaloniaHudPanel(
            title,
            accent,
            new TextBox().PlaceholderText("focus me").FontSize(12));

        var placement = new ScreenTexture(anchorStretch, anchorPosition, ShaderSupportType.None);
        var surface = new AvaloniaSurface(panel, placement, scaling) { DesignSize = designSize };

        panels.Add((surface, panel));
        return surface;
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        foreach (var (surface, panel) in panels)
        {
            var rect = surface.DestinationRect;
            panel.SetStatus(
                $"{rect.Width:0}x{rect.Height:0}   pointer: {surface.WantsPointer}   keys: {surface.WantsKeyboard}");
        }
    }

    protected override void OnDeactivate()
    {
        panels.Clear();
        base.OnDeactivate();
    }
}
