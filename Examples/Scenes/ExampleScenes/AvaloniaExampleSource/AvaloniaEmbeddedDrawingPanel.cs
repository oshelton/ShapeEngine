using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using ShapeEngine.Avalonia.Controls;
using ShapeEngine.Color;
using ShapeEngine.Geometry.CircleDef;
using AvSlider = Avalonia.Controls.Slider;
using SeRect = ShapeEngine.Geometry.RectDef.Rect;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>
/// An Avalonia panel hosting ShapeEngine drawing, with Avalonia controls steering it.
/// </summary>
/// <remarks>
/// The artwork sits in the control tree like any other control - it scales with the surface, is clipped
/// by its parent, and has Avalonia content layered over it. All three view kinds are here: an animated
/// texture view for the orbits, a static one for the emblem, and a direct view for the bars.
/// </remarks>
public sealed class AvaloniaEmbeddedDrawingPanel : ViewBase
{
    private static readonly ColorRgba[] OrbitColors =
    [
        new(120, 200, 255, 255),
        new(160, 255, 170, 255),
        new(255, 190, 120, 255),
        new(230, 130, 200, 255)
    ];

    private TextBlock statusText = null!;
    private AvSlider speedSlider = null!;
    private ToggleSwitch orbitRingsToggle = null!;
    private ShapeEngineStaticTextureView emblemView = null!;

    private float elapsed;
    private int emblemSeed = 1;

    public AvaloniaEmbeddedDrawingPanel() => Initialize();

    /// <summary>How fast the artwork animates, driven by the slider.</summary>
    private double Speed => speedSlider.Value;

    /// <summary>Whether the orbit paths are drawn behind the moving shapes.</summary>
    public bool ShowOrbitRings
    {
        get => orbitRingsToggle.IsChecked == true;
        set => orbitRingsToggle.IsChecked = value;
    }

    protected override object Build()
        => new Border()
            .Width(340)
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
                            .Text("ShapeEngine drawing in a control")
                            .FontSize(22)
                            .FontWeight(FontWeight.SemiBold)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.White),
                        new TextBlock()
                            .Text("Everything below is drawn with ShapeEngine's shape functions, sitting in the control tree like any other Avalonia content.")
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.DarkGray),
                        BuildArtwork(),
                        new TextBlock()
                            .Text("Animation speed")
                            .FontSize(12)
                            .Foreground(Brushes.DarkGray),
                        new AvSlider()
                            .Ref(out speedSlider)
                            .Minimum(0)
                            .Maximum(3)
                            .Value(1),
                        new ToggleSwitch()
                            .Ref(out orbitRingsToggle)
                            .Content("Orbit paths")
                            .IsChecked(true),
                        new TextBlock()
                            .Text("Static view - drawn once, not per frame")
                            .FontSize(12)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.DarkGray),
                        BuildEmblem(),
                        new Button()
                            .Content("Redraw emblem")
                            .HorizontalAlignment(HorizontalAlignment.Stretch)
                            .HorizontalContentAlignment(HorizontalAlignment.Center)
                            .OnClick(_ => RegenerateEmblem()),
                        new TextBlock()
                            .Text("Direct view - no texture, rotated and clipped")
                            .FontSize(12)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.DarkGray),
                        BuildDirectArtwork(),
                        new TextBlock()
                            .Ref(out statusText)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.Gainsboro)));

    /// <summary>Advances the artwork's animation clock. Called by the scene each frame.</summary>
    public void Advance(float deltaTime) => elapsed += deltaTime * (float)Speed;

    /// <summary>Shows the surface's live state, updated by the scene each frame.</summary>
    public void SetStatus(string status) => statusText.Text = status;

    /// <remarks>
    /// No background: the texture view clears to transparent, so the panel - and the game behind it -
    /// show through wherever the artwork does not draw.
    /// </remarks>
    private Control BuildArtwork()
        => new Border()
            .Height(190)
            .CornerRadius(new CornerRadius(8))
            .ClipToBounds(true)
            .Child(
                new ShapeEngineAnimatedTextureView
                {
                    DrawContent = DrawArtwork
                });

    /// <summary>
    /// The static counterpart to the animated artwork above: drawn once, then left alone. The button
    /// below it changes the seed and calls <c>InvalidateContent</c>, the only thing that redraws it.
    /// </summary>
    private Control BuildEmblem()
        => new Border()
            .Height(90)
            .CornerRadius(new CornerRadius(8))
            .ClipToBounds(true)
            .Child(
                new ShapeEngineStaticTextureView
                {
                    DrawContent = DrawEmblem
                }.Ref(out emblemView));

    /// <summary>
    /// Draws the artwork with ShapeEngine's shape functions. Runs inside the game's frame with the
    /// render texture bound, so these are ordinary draw calls in texture pixel coordinates.
    /// </summary>
    private void DrawArtwork(SeRect bounds)
    {
        var center = bounds.Center;
        var unit = Math.Min(bounds.Width, bounds.Height);

        for (var ring = 0; ring < OrbitColors.Length; ring++)
        {
            var radius = unit * (0.14f + ring * 0.09f);
            var color = OrbitColors[ring];

            if (ShowOrbitRings)
            {
                new Circle(center, radius).DrawLines(2f, color.SetAlpha(150), 4f);
            }

            // Each ring turns a little slower than the one inside it, and every other one reverses.
            var direction = ring % 2 == 0 ? 1f : -1f;
            var angle = elapsed * direction * (1.6f - ring * 0.28f);

            for (var i = 0; i < 3; i++)
            {
                var offset = angle + i * MathF.Tau / 3f;
                var position = center + new System.Numerics.Vector2(
                    MathF.Cos(offset) * radius,
                    MathF.Sin(offset) * radius);

                new Circle(position, unit * (0.045f - ring * 0.005f)).Draw(color,1.0f);
            }
        }

        // A pulsing core, so something is moving even with the rings turned off.
        var pulse = 0.5f + 0.5f * MathF.Sin(elapsed * 2.4f);
        new Circle(center, unit * (0.05f + pulse * 0.03f)).Draw(new ColorRgba(255, 255, 255, 200), 1.0f);
    }

    /// <summary>
    /// Drawing straight into Avalonia's framebuffer, with no texture in between.
    /// </summary>
    /// <remarks>
    /// Deliberately awkward placement - the panel is inside a <c>Viewbox</c>, this sits inside a clipping
    /// border, and the view itself is rotated. Any error in the transform or clip mapping shows up as
    /// bars at the wrong size, in the wrong place, or spilling outside the border.
    /// </remarks>
    private Control BuildDirectArtwork()
        => new Border()
            .Height(90)
            .CornerRadius(new CornerRadius(8))
            .ClipToBounds(true)
            .Child(
                new ShapeEngineDirectView
                {
                    DrawContent = DrawDirectArtwork,
                    RenderTransform = new RotateTransform(-8)
                });

    /// <summary>Draws a row of bars in the control's own coordinate space.</summary>
    private void DrawDirectArtwork(SeRect bounds)
    {
        const int barCount = 14;

        var barWidth = bounds.Width / (barCount * 1.6f);

        for (var i = 0; i < barCount; i++)
        {
            var phase = elapsed * 2f + i * 0.45f;
            var height = bounds.Height * (0.25f + 0.35f * (0.5f + 0.5f * MathF.Sin(phase)));
            var x = bounds.X + bounds.Width * (i + 0.5f) / barCount;

            var color = OrbitColors[i % OrbitColors.Length];
            new SeRect(x - barWidth * 0.5f, bounds.Bottom - height, barWidth, height).Draw(color.SetAlpha(220));
        }
    }

    /// <summary>Picks a new emblem and asks the static view to draw it.</summary>
    private void RegenerateEmblem()
    {
        emblemSeed++;
        emblemView.InvalidateContent();
    }

    /// <summary>
    /// Draws a fixed arrangement of shapes from the current seed. Nothing here reads the animation
    /// clock, so the result only changes when the seed does - the case the static view exists for.
    /// </summary>
    private void DrawEmblem(SeRect bounds)
    {
        var random = new Random(emblemSeed);
        var center = bounds.Center;
        var unit = Math.Min(bounds.Width, bounds.Height);

        for (var i = 0; i < 9; i++)
        {
            var angle = (float)random.NextDouble() * MathF.Tau;
            var distance = unit * (0.1f + (float)random.NextDouble() * 0.55f);
            var position = center + new System.Numerics.Vector2(
                MathF.Cos(angle) * distance * (bounds.Width / unit),
                MathF.Sin(angle) * distance);

            var color = OrbitColors[random.Next(OrbitColors.Length)];
            new Circle(position, unit * (0.05f + (float)random.NextDouble() * 0.1f)).Draw(color.SetAlpha(190), 1.0f);
        }

        new Circle(center, unit * 0.36f).DrawLines(2f, new ColorRgba(255, 255, 255, 120), 4f);
    }
}
