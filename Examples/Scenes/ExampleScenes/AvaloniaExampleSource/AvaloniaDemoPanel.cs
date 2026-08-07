using System.Collections.ObjectModel;
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
/// Deliberately exercises the parts of the integration that are easy to get wrong: a translucent
/// background (premultiplied alpha), rounded corners (Skia's stencil buffer), a <see cref="TextBox"/>
/// (text input, focus arbitration, I-beam cursor), a <see cref="ComboBox"/> (overlay popups), a
/// scrolling <see cref="ListBox"/> (mouse wheel) and an indeterminate <see cref="ProgressBar"/> (the
/// animation clock).
/// </remarks>
public sealed class AvaloniaDemoPanel : ViewBase
{
    private readonly ObservableCollection<string> logEntries = [];
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
                            .PlaceholderText("Type here - the game stops seeing the keyboard")
                            .OnTextChanged(e => logEntries.Insert(0, $"Text: \"{((TextBox)e.Source!).Text}\"")),
                        new ComboBox()
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .PlaceholderText("Pick a shape (overlay popup)")
                            .ItemsSource(new[] { "Circle", "Rect", "Triangle", "Polygon", "Polyline", "Segment" }),
                        new ListBox()
                            .ItemsSource(logEntries)
                            .Height(110),
                        new ProgressBar()
                            .IsIndeterminate(true),
                        new TextBlock()
                            .Ref(out statusText)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.Gainsboro)));

    /// <summary>Shows the surface's live state, updated by the scene each frame.</summary>
    public void SetStatus(string status) => statusText.Text = status;

    private void RegisterClick()
    {
        clickCount++;
        clickCountText.Text = $"Clicked {clickCount} time{(clickCount == 1 ? String.Empty : "s")}";
        logEntries.Insert(0, $"Click #{clickCount} at {DateTime.Now:HH:mm:ss}");
    }
}
