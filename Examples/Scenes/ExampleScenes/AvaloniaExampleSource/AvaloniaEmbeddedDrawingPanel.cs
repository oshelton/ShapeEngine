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
/// An Avalonia panel hosting animated ShapeEngine drawing, with Avalonia controls steering it.
/// </summary>
/// <remarks>
/// The artwork is drawn with ShapeEngine's own shape functions into a
/// <see cref="ShapeEngineTextureView"/>, so it sits in the control tree like any other control - it
/// scales with the surface, is clipped by its parent, and has Avalonia content layered over it.
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

    private float elapsed;

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
                            .Text("The artwork below is drawn with ShapeEngine's shape functions into a render texture, then displayed like any other Avalonia content.")
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
                new ShapeEngineTextureView
                {
                    DrawContent = DrawArtwork
                });

    /// <summary>
    /// Draws the artwork with ShapeEngine's shape functions.
    /// </summary>
    /// <remarks>
    /// Runs inside the game's frame with the render texture bound, so these are ordinary ShapeEngine
    /// draw calls in texture pixel coordinates.
    /// </remarks>
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
}
