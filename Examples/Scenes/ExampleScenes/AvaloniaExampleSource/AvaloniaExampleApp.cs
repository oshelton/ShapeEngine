using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>
/// The Avalonia <see cref="Application"/> hosted inside the example. Built in code so the Examples
/// project needs no XAML compilation; a real game would normally use an <c>App.axaml</c>.
/// </summary>
public sealed class AvaloniaExampleApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
    }
}
