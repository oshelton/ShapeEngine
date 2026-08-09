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
/// Every surface is a separate Avalonia top level with its own control tree, focus, input capture and
/// screen texture, all sharing raylib's single OpenGL context - so typing into one panel's text box
/// disturbs neither the others nor the game. Three are anchored and differ in whether they scale their
/// content; the fourth covers the window and draws over them.
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
            VerticalAlignment = VerticalAlignment.Top
        };

        // Centred in the safe band's top half, which keeps it under the title and clear of the anchored
        // panels either side.
        var overlay = new AvaloniaSurface(
            overlayPanel,
            AvaloniaExampleLayout.Region(0.34f, AvaloniaExampleLayout.PaddedTop, 0.32f, 0.34f),
            order: 1);

        panels.Add((overlay, overlayPanel));

        const float panelHeight = 0.3f;

        return
        [
            CreatePanel(
                "Top left",
                Color.FromRgb(120, 200, 255),
                AvaloniaExampleLayout.Region(AvaloniaExampleLayout.Inset, AvaloniaExampleLayout.PaddedTop, 0.26f, panelHeight),
                scaleContent: false),
            CreatePanel(
                "Bottom left, scaled",
                Color.FromRgb(160, 255, 160),
                AvaloniaExampleLayout.Region(AvaloniaExampleLayout.Inset, AvaloniaExampleLayout.PaddedBottom - panelHeight, 0.26f, panelHeight),
                scaleContent: true),
            // Below the palette box rather than beside it, since that occupies the right edge from 0.25
            // to 0.35 and composites after every surface.
            CreatePanel(
                "Right",
                Color.FromRgb(255, 190, 120),
                AvaloniaExampleLayout.Region(0.71f, 0.45f, 0.26f, panelHeight),
                scaleContent: false),
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
