using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// Four independent Avalonia surfaces on screen at once, each with its own anchor and settings.
/// </summary>
/// <remarks>
/// Every surface is a separate Avalonia top level with its own control tree, focus and input state, all
/// sharing raylib's single OpenGL context and each owning its own screen texture. Each keeps its own
/// input capture, so typing into one panel's text box does not disturb the others or the game.
/// <para>
/// The three anchored surfaces differ in whether they scale their content, so the effect is visible side
/// by side; the fourth covers the window and draws over them.
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
        // No anchor, so this one covers the window. A higher order draws it over the others.
        var overlayPanel = new AvaloniaHudPanel(
            "Full window overlay",
            Color.FromRgb(255, 140, 200),
            new TextBlock()
                .Text("No anchor given, so this surface covers the window and draws over the rest.")
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
            CreatePanel("Top left", Color.FromRgb(120, 200, 255), new AvaloniaSurfaceAnchor(0.26f, 0.3f, 0.03f, 0.16f), scaleContent: false),
            CreatePanel("Bottom left, scaled", Color.FromRgb(160, 255, 160), new AvaloniaSurfaceAnchor(0.26f, 0.3f, 0.03f, 0.84f), scaleContent: true),
            CreatePanel("Right", Color.FromRgb(255, 190, 120), new AvaloniaSurfaceAnchor(0.24f, 0.34f, 0.97f, 0.5f), scaleContent: false),
            overlay
        ];
    }

    private AvaloniaSurface CreatePanel(string title, Color accent, AvaloniaSurfaceAnchor anchor, bool scaleContent)
    {
        var panel = new AvaloniaHudPanel(
            title,
            accent,
            new TextBox().PlaceholderText("focus me").FontSize(12));

        var surface = new AvaloniaSurface(panel, anchor, scaleContent);

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
