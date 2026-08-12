using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>
/// A panel of real Avalonia controls, used by most of the Avalonia examples.
/// </summary>
/// <remarks>
/// Every control here covers a part of the integration that is easy to get wrong: a translucent
/// background (premultiplied alpha), rounded corners (Skia's stencil buffer), a <see cref="TextBox"/>
/// (text input, focus arbitration, I-beam cursor), a <see cref="ComboBox"/> (overlay popups) and an
/// indeterminate <see cref="ProgressBar"/> (the animation clock).
/// <para>
/// The whole content sits in a <see cref="ScrollViewer"/> rather than a bare <see cref="StackPanel"/> -
/// <see cref="AvaloniaFullWindowExample"/>'s <c>DockPanel</c> centre can end up shorter than this content
/// wants, and a <c>StackPanel</c> alone neither scrolls nor clips when that happens, it just overflows.
/// There used to be a <see cref="ListBox"/> here too; removed rather than fixed, since nothing in this
/// panel actually needed a log.
/// </para>
/// </remarks>
public sealed class AvaloniaDemoPanel : ViewBase
{
    private readonly string title;
    private readonly string description;

    private TextBlock clickCountText = null!;
    private TextBlock statusText = null!;
    private int clickCount;

    public AvaloniaDemoPanel(string title, string description)
    {
        this.title = title;
        this.description = description;

        Initialize();
    }

    protected override object Build()
        => new Border()
            .Background(new SolidColorBrush(Color.FromArgb(220, 24, 24, 34)))
            .BorderBrush(new SolidColorBrush(Color.FromArgb(255, 90, 90, 130)))
            .BorderThickness(new Thickness(1))
            .CornerRadius(new CornerRadius(12))
            .Padding(new Thickness(18))
            .Child(
                // Scrolls rather than overflows when the container - the DockPanel centre in
                // AvaloniaFullWindowExample can be quite short - is shorter than the content wants.
                new ScrollViewer()
                    .Content(
                        new StackPanel()
                            .Spacing(10)
                            .Children(
                                new TextBlock()
                                    .Text(title)
                                    .FontSize(22)
                                    .FontWeight(FontWeight.SemiBold)
                                    .TextWrapping(TextWrapping.Wrap)
                                    .Foreground(Brushes.White),
                                new TextBlock()
                                    .Text(description)
                                    .TextWrapping(TextWrapping.Wrap)
                                    .Foreground(Brushes.DarkGray),
                                new Button()
                                    .Content("Click me")
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .HorizontalContentAlignment(HorizontalAlignment.Center)
                                    .OnClick(_ => RegisterClick()),
                                new TextBlock()
                                    .Ref(out clickCountText)
                                    .Text("Not clicked yet")
                                    .Foreground(Brushes.Gainsboro),
                                new TextBox()
                                    .PlaceholderText("Type here - the game stops seeing the keyboard"),
                                new ComboBox()
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .PlaceholderText("Pick a shape (overlay popup)")
                                    .ItemsSource(new[] { "Circle", "Rect", "Triangle", "Polygon", "Polyline", "Segment" }),
                                new ProgressBar()
                                    .IsIndeterminate(true),
                                new TextBlock()
                                    .Ref(out statusText)
                                    .TextWrapping(TextWrapping.Wrap)
                                    .Foreground(Brushes.Gainsboro))));

    /// <summary>Shows the surface's live state, updated by the scene each frame.</summary>
    public void SetStatus(string status) => statusText.Text = status;

    private void RegisterClick()
    {
        clickCount++;
        clickCountText.Text = $"Clicked {clickCount} time{(clickCount == 1 ? String.Empty : "s")}";
    }
}
