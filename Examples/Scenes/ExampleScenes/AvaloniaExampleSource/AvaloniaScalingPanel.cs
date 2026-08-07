using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>
/// Controls for switching a surface between the content sizing and rasterization options at runtime.
/// </summary>
/// <remarks>
/// The text samples are there to judge the result: with content scaling on, they grow with the surface;
/// with it off, they keep their size and the layout simply gets more room.
/// </remarks>
public sealed class AvaloniaScalingPanel : ViewBase
{
    private TextBlock statusText = null!;

    /// <summary>Raised when the user asks for the content to scale rather than the layout to expand.</summary>
    public event Action<bool>? ScaleContentChanged;

    /// <summary>Raised when the user switches between rasterizing at texture size and at screen size.</summary>
    public event Action<bool>? NativeDensityChanged;

    public AvaloniaScalingPanel() => Initialize();

    protected override object Build()
        => new Border()
            .Background(new SolidColorBrush(Color.FromArgb(220, 24, 24, 34)))
            .BorderBrush(new SolidColorBrush(Color.FromArgb(255, 90, 90, 130)))
            .BorderThickness(new Thickness(1))
            .CornerRadius(new CornerRadius(12))
            .Padding(new Thickness(18))
            .VerticalAlignment(VerticalAlignment.Top)
            .Child(
                new StackPanel()
                    .Spacing(10)
                    .Children(
                        new TextBlock()
                            .Text("Layout vs content scaling")
                            .FontSize(22)
                            .FontWeight(FontWeight.SemiBold)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.White),
                        new TextBlock()
                            .Text("Resize the window and watch what changes: the layout area, or the size of everything in it.")
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.DarkGray),
                        new ToggleSwitch()
                            .Content("Content sizing")
                            .OffContent("Layout expands to fill")
                            .OnContent("Content scales to design size")
                            .OnIsCheckedChanged(e => ScaleContentChanged?.Invoke(((ToggleSwitch)e.Source!).IsChecked == true)),
                        new ToggleSwitch()
                            .Content("Rasterization")
                            .OffContent("MatchTexture (shaders apply)")
                            .OnContent("NativeDensity (crisper)")
                            .OnIsCheckedChanged(e => NativeDensityChanged?.Invoke(((ToggleSwitch)e.Source!).IsChecked == true)),
                        new TextBlock()
                            .Text("Sample body text at 14 point")
                            .FontSize(14)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.Gainsboro),
                        new TextBlock()
                            .Text("Heading at 28 point")
                            .FontSize(28)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.White),
                        new Button()
                            .Content("A button for scale reference"),
                        new TextBlock()
                            .Ref(out statusText)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.Gainsboro)));

    /// <summary>Shows the surface's live state, updated by the scene each frame.</summary>
    public void SetStatus(string status) => statusText.Text = status;
}
