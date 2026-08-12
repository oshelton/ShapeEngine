using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>
/// The card shell both drag-and-drop panels sit in - title, description, their own distinct middle
/// content, an activity log and a status line - so neither <see cref="AvaloniaDragDropSourcePanel"/> nor
/// <see cref="AvaloniaDragDropTargetPanel"/> has to repeat it.
/// </summary>
internal static class AvaloniaDragDropPanelChrome
{
    public static Border Build(string title, string description, Control content, ObservableCollection<string> log, out TextBlock statusText)
        => new Border()
            .Width(320)
            .Background(new SolidColorBrush(Color.FromArgb(220, 24, 24, 34)))
            .BorderBrush(new SolidColorBrush(Color.FromArgb(255, 90, 90, 130)))
            .BorderThickness(new Thickness(1))
            .CornerRadius(new CornerRadius(12))
            .Padding(new Thickness(18))
            .VerticalAlignment(VerticalAlignment.Top)
            .Child(
                new StackPanel()
                    .Spacing(12)
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
                        content,
                        new ListBox()
                            .ItemsSource(log)
                            .Height(110),
                        new TextBlock()
                            .Ref(out statusText)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.Gainsboro)));
}
