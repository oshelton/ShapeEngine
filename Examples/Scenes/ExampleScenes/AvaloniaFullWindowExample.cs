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
/// A surface covering the whole window, laid out with a <see cref="DockPanel"/>: a strip docked to each
/// side, and a full panel filling whatever space is left in the centre.
/// </summary>
/// <remarks>
/// The surface covers the app's own interface as well as the game, so the <c>DockPanel</c> itself sits
/// inside a grid whose star rows match the bands in <see cref="AvaloniaExampleLayout"/> - star sizing is
/// the point: a fixed pixel margin that clears the title bar in a window clears far too little of it full
/// screen. The four side strips are added before the centre panel, in dock order (top, bottom, left,
/// right), so each claims its slice before the last child fills whatever remains.
/// </remarks>
public class AvaloniaFullWindowExample : AvaloniaExampleSceneBase
{
    private AvaloniaSurface? surface;
    private AvaloniaDemoPanel? panel;

    public AvaloniaFullWindowExample()
    {
        Title = "Avalonia - Full Window";
        Description = "Surface covers the window; a DockPanel arranges a strip on each side around the panel";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        var top = BuildDockStrip("Top", "DockPanel.Dock = Top", Color.FromRgb(255, 140, 200)).Height(44);
        var bottom = BuildDockStrip("Bottom", "DockPanel.Dock = Bottom", Color.FromRgb(160, 255, 170)).Height(44);
        var left = BuildDockStrip("Left", "DockPanel.Dock = Left", Color.FromRgb(120, 200, 255)).Width(130);
        var right = BuildDockStrip("Right", "DockPanel.Dock = Right", Color.FromRgb(255, 190, 120)).Width(130);

        DockPanel.SetDock(top, Dock.Top);
        DockPanel.SetDock(bottom, Dock.Bottom);
        DockPanel.SetDock(left, Dock.Left);
        DockPanel.SetDock(right, Dock.Right);

        panel = new AvaloniaDemoPanel(
            "Full window surface",
            "The centre of a DockPanel - whatever space the four docked strips around it leave behind.")
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        // Order matters: each side is carved off in turn, and only what's left goes to the last child.
        var dock = new DockPanel { LastChildFill = true, Children = { top, bottom, left, right, panel } };

        surface = new AvaloniaSurface(AvaloniaExampleLayout.SafeArea(dock));
        return [surface];
    }

    /// <summary>A small labeled strip for one side of the dock, styled the same regardless of which.</summary>
    private static Border BuildDockStrip(string label, string description, Color accent)
        => new Border()
            .Background(new SolidColorBrush(Color.FromArgb(200, 24, 24, 34)))
            .BorderBrush(new SolidColorBrush(accent))
            .BorderThickness(new Thickness(1))
            .CornerRadius(new CornerRadius(8))
            .Padding(new Thickness(10))
            .Margin(new Thickness(3))
            .Child(
                new StackPanel()
                    .Spacing(2)
                    .Children(
                        new TextBlock()
                            .Text(label)
                            .FontSize(13)
                            .FontWeight(FontWeight.SemiBold)
                            .Foreground(Brushes.White),
                        new TextBlock()
                            .Text(description)
                            .FontSize(10)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.DarkGray)));

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null) return;

        var rect = surface.DestinationRect;
        panel?.SetStatus(
            $"""
             Surface {rect.Width:0}x{rect.Height:0} (window)
             WantsPointer: {surface.WantsPointer}   WantsKeyboard: {surface.WantsKeyboard}
             """);
    }
}
