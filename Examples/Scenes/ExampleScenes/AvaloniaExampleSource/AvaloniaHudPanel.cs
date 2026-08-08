using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>A compact panel, used where a scene needs several small surfaces rather than one big one.</summary>
public sealed class AvaloniaHudPanel : ViewBase
{
    private readonly string title;
    private readonly Color accent;
    private readonly Control[] extraContent;

    private TextBlock statusText = null!;

    public AvaloniaHudPanel(string title, Color accent, params Control[] extraContent)
    {
        this.title = title;
        this.accent = accent;
        this.extraContent = extraContent;

        Initialize();
    }

    protected override object Build()
    {
        var panel = new StackPanel()
            .Spacing(8)
            .Children(
                new TextBlock()
                    .Text(title)
                    .FontSize(16)
                    .FontWeight(FontWeight.SemiBold)
                    .Foreground(new SolidColorBrush(accent)));

        foreach (var control in extraContent) panel.Children.Add(control);

        panel.Children.Add(
            new TextBlock()
                .Ref(out statusText)
                .FontSize(12)
                .TextWrapping(TextWrapping.Wrap)
                .Foreground(Brushes.Gainsboro));

        return new Border()
            .Width(220)
            .Background(new SolidColorBrush(Color.FromArgb(210, 20, 20, 30)))
            .BorderBrush(new SolidColorBrush(accent))
            .BorderThickness(new Thickness(1))
            .CornerRadius(new CornerRadius(10))
            .Padding(new Thickness(12))
            .VerticalAlignment(VerticalAlignment.Top)
            .Child(panel);
    }

    /// <summary>Shows the surface's live state, updated by the scene each frame.</summary>
    public void SetStatus(string status) => statusText.Text = status;
}
