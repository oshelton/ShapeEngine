using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Avalonia.Styling;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>
/// Animated Avalonia controls and content, driven entirely by the game loop.
/// </summary>
/// <remarks>
/// Avalonia's animation clock is advanced by the surface's render tick, so everything here tests that
/// plumbing - if the clock stalls or jumps, the motion stutters visibly. Three kinds are covered:
/// keyframe animations on render transforms, property transitions triggered by state changes, and the
/// built-in animations of <see cref="ProgressBar"/> and <see cref="TransitioningContentControl"/>.
/// </remarks>
public sealed class AvaloniaAnimationPanel : ViewBase
{
    private static readonly string[] RotatingMessages =
    [
        "Cross-fading content",
        "Driven by the game loop",
        "Skia on raylib's context",
        "No Avalonia render thread"
    ];

    private TextBlock statusText = null!;
    private ProgressBar transitionedBar = null!;
    private Border pulsingPanel = null!;
    private TransitioningContentControl rotatingContent = null!;

    private int messageIndex;
    private bool highlighted;

    public AvaloniaAnimationPanel() => Initialize();

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
                    .Spacing(12)
                    .Children(
                        new TextBlock()
                            .Text("Animated controls and content")
                            .FontSize(22)
                            .FontWeight(FontWeight.SemiBold)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.White),
                        new TextBlock()
                            .Text("Every animation below is advanced by the surface's render tick, not by an Avalonia render thread.")
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.DarkGray),
                        BuildTransformRow(),
                        new TextBlock()
                            .Text("Keyframe animations: rotate, pulse, slide")
                            .FontSize(12)
                            .Foreground(Brushes.DarkGray),
                        new ProgressBar()
                            .IsIndeterminate(true),
                        BuildTransitionedBar(),
                        BuildPulsingPanel(),
                        BuildRotatingContent(),
                        new TextBlock()
                            .Ref(out statusText)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.Gainsboro)));

    /// <summary>Advances the state that the transition-based animations react to.</summary>
    /// <remarks>
    /// Called by the scene rather than a timer inside the view, so the transitions are visibly driven by
    /// the game rather than by Avalonia running independently.
    /// </remarks>
    public void AdvanceTransitions()
    {
        highlighted = !highlighted;

        transitionedBar.Value = highlighted ? 100 : 0;
        pulsingPanel.Opacity = highlighted ? 1.0 : 0.35;
        pulsingPanel.Background = new SolidColorBrush(
            highlighted ? Color.FromRgb(90, 140, 220) : Color.FromRgb(60, 60, 90));

        messageIndex = (messageIndex + 1) % RotatingMessages.Length;
        rotatingContent.Content = new TextBlock()
            .Text(RotatingMessages[messageIndex])
            .FontSize(14)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Foreground(Brushes.White);
    }

    /// <summary>Shows the surface's live state, updated by the scene each frame.</summary>
    public void SetStatus(string status) => statusText.Text = status;

    private static Control BuildTransformRow()
    {
        var rotating = CreateShape(Color.FromRgb(120, 200, 255), new CornerRadius(8), new RotateTransform());
        var pulsing = CreateShape(Color.FromRgb(160, 255, 170), new CornerRadius(24), new ScaleTransform());
        var sliding = CreateShape(Color.FromRgb(255, 190, 120), new CornerRadius(4), new TranslateTransform());

        Loop(rotating, TimeSpan.FromSeconds(3), new LinearEasing(), PlaybackDirection.Normal,
            (RotateTransform.AngleProperty, 0d, 360d));

        Loop(pulsing, TimeSpan.FromSeconds(1.2), new CubicEaseInOut(), PlaybackDirection.Alternate,
            (ScaleTransform.ScaleXProperty, 0.55d, 1d),
            (ScaleTransform.ScaleYProperty, 0.55d, 1d));

        Loop(sliding, TimeSpan.FromSeconds(1.8), new SineEaseInOut(), PlaybackDirection.Alternate,
            (TranslateTransform.XProperty, -26d, 26d));

        return new StackPanel()
            .Orientation(Orientation.Horizontal)
            .Spacing(28)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .Height(64)
            .Children(rotating, pulsing, sliding);
    }

    private static Border CreateShape(Color color, CornerRadius cornerRadius, ITransform transform)
        => new Border()
            .Width(46)
            .Height(46)
            .VerticalAlignment(VerticalAlignment.Center)
            .Background(new SolidColorBrush(color))
            .CornerRadius(cornerRadius)
            .RenderTransform(transform);

    private Control BuildTransitionedBar()
        => new ProgressBar()
            .Ref(out transitionedBar)
            .Minimum(0)
            .Maximum(100)
            .Value(0)
            .Transitions(
            [
                new DoubleTransition
                {
                    Property = RangeBase.ValueProperty,
                    Duration = TimeSpan.FromSeconds(0.9),
                    Easing = new CubicEaseInOut()
                }
            ]);

    private Control BuildPulsingPanel()
        => new Border()
            .Ref(out pulsingPanel)
            .Height(44)
            .CornerRadius(new CornerRadius(8))
            .Opacity(0.35)
            .Background(new SolidColorBrush(Color.FromRgb(60, 60, 90)))
            .Transitions(
            [
                new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromSeconds(0.6) },
                new BrushTransition { Property = Border.BackgroundProperty, Duration = TimeSpan.FromSeconds(0.6) }
            ])
            .Child(
                new TextBlock()
                    .Text("Opacity and brush transitions")
                    .FontSize(12)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Foreground(Brushes.White));

    private Control BuildRotatingContent()
        => new TransitioningContentControl()
            .Ref(out rotatingContent)
            .Height(34)
            .HorizontalContentAlignment(HorizontalAlignment.Center)
            .VerticalContentAlignment(VerticalAlignment.Center)
            .PageTransition(new CrossFade(TimeSpan.FromSeconds(0.5)))
            .Content(
                new TextBlock()
                    .Text(RotatingMessages[0])
                    .FontSize(14)
                    .HorizontalAlignment(HorizontalAlignment.Center)
                    .Foreground(Brushes.White));

    /// <summary>Runs a looping keyframe animation between the given start and end values.</summary>
    /// <remarks>
    /// Transform properties are animated against the control, not its <c>RenderTransform</c>: Avalonia's
    /// transform animator resolves the transform itself, and handing it one directly throws.
    /// </remarks>
    private static void Loop(
        Animatable target,
        TimeSpan duration,
        Easing easing,
        PlaybackDirection direction,
        params (AvaloniaProperty Property, double From, double To)[] properties)
    {
        var start = new KeyFrame { Cue = new Cue(0d) };
        var end = new KeyFrame { Cue = new Cue(1d) };

        foreach (var (property, from, to) in properties)
        {
            start.Setters.Add(new Setter(property, from));
            end.Setters.Add(new Setter(property, to));
        }

        var animation = new Animation
        {
            Duration = duration,
            Easing = easing,
            PlaybackDirection = direction,
            IterationCount = IterationCount.Infinite,
            Children = { start, end }
        };

        // Not awaited: an infinite animation never completes, and it stops when the surface is disposed.
        _ = animation.RunAsync(target);
    }
}
