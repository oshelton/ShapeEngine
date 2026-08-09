using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using ShapeEngine.Avalonia.Controls;
using ShapeEngine.Color;
using ShapeEngine.Core.Structs;
using ShapeEngine.Geometry.CircleDef;
using ShapeEngine.Geometry.PolygonDef;
using ShapeEngine.Geometry.TriangleDef;
using AvSlider = Avalonia.Controls.Slider;
using SeRect = ShapeEngine.Geometry.RectDef.Rect;
using SeSize = ShapeEngine.Core.Structs.Size;
using SeVec2 = System.Numerics.Vector2;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>
/// A single panel mixing plain Avalonia controls, buttons with ShapeEngine-rendered icons, and
/// ShapeEngine content used as plain, non-interactive images.
/// </summary>
/// <remarks>
/// Exists to stress the integration rather than to demonstrate any one feature: a <c>TabControl</c>,
/// <c>ScrollViewer</c>s, text input, a radio group and a checkbox all share the surface with five kinds
/// of ShapeEngine icon, plus a static, an animated and a direct view used purely as images - all at once,
/// each hit-testable or not exactly as its plain Avalonia counterpart would be.
/// </remarks>
public sealed class AvaloniaMixedContentPanel : ViewBase
{
    private enum IconShape { Circle, Square, Triangle, Star, Ring }

    private static readonly IconShape[] IconShapes =
        [IconShape.Circle, IconShape.Square, IconShape.Triangle, IconShape.Star, IconShape.Ring];

    /// <summary>One fixed colour per icon shape, so a shape reads the same wherever it appears.</summary>
    private static readonly ColorRgba[] IconColors =
    [
        new(120, 200, 255, 255),
        new(160, 255, 170, 255),
        new(255, 190, 120, 255),
        new(230, 130, 200, 255),
        new(255, 230, 120, 255)
    ];

    /// <summary>Colours the radio group chooses between, applied to the gallery's hero and direct tiles.</summary>
    private static readonly (string Name, ColorRgba Color)[] Accents =
    [
        ("Blue", new ColorRgba(120, 200, 255, 255)),
        ("Green", new ColorRgba(160, 255, 170, 255)),
        ("Amber", new ColorRgba(255, 190, 120, 255))
    ];

    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));

    /// <summary>Fixed across all three tabs, so switching tabs doesn't resize the panel.</summary>
    private const float TabContentHeight = 420f;

    private readonly ObservableCollection<string> log = [];
    private readonly List<Button> iconButtons = [];

    private TextBlock statusText = null!;
    private TextBlock selectionText = null!;
    private AvSlider speedSlider = null!;
    private CheckBox highlightCheckBox = null!;
    private ComboBox gallerySeedCombo = null!;
    private TabControl tabControl = null!;
    private ShapeEngineStaticTextureView heroView = null!;

    private int selectedIcon = -1;
    private int accentIndex;
    private IconShape gallerySeed = IconShape.Star;
    private float elapsed;

    public AvaloniaMixedContentPanel()
    {
        Initialize();

        gallerySeedCombo.SelectionChanged += (_, _) =>
        {
            var index = gallerySeedCombo.SelectedIndex;
            if (index < 0) return;

            gallerySeed = IconShapes[index];
            heroView.InvalidateContent();
            AppendLog($"Gallery shape -> {gallerySeed}");
        };
    }

    private double AnimationSpeed => speedSlider.Value;

    protected override object Build()
        => new Border()
            .Width(680)
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
                            .Text("Mixed content")
                            .FontSize(22)
                            .FontWeight(FontWeight.SemiBold)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.White),
                        new TextBlock()
                            .Text("Native controls, buttons with ShapeEngine icons, and ShapeEngine content used as plain images, all on one surface.")
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.DarkGray),
                        BuildTabs(),
                        new TextBlock()
                            .Ref(out statusText)
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.Gainsboro)));

    /// <summary>Advances the gallery's animated and direct content. Called by the scene each frame.</summary>
    public void Advance(float deltaTime) => elapsed += deltaTime * (float)AnimationSpeed;

    /// <summary>Shows the surface's live state, updated by the scene each frame.</summary>
    public void SetStatus(string status) => statusText.Text = status;

    /// <summary>Selects a tab by index (0 = Controls, 1 = Icon buttons, 2 = Gallery). TEMP verification.</summary>
    public void SelectTab(int index) => tabControl.SelectedIndex = index;

    private Control BuildTabs()
    {
        var tabs = new List<TabItem>
        {
            new() { Header = "Controls", Content = Scrollable(BuildControlsTab()) },
            new() { Header = "Icon buttons", Content = Scrollable(BuildIconButtonsTab()) },
            new() { Header = "Gallery", Content = Scrollable(BuildGalleryTab()) }
        };

        return new TabControl().Ref(out tabControl).ItemsSource(tabs);
    }

    /// <summary>Wraps a tab's content so it scrolls rather than pushing the panel taller.</summary>
    private static Control Scrollable(Control content) => new ScrollViewer().Height(TabContentHeight).Content(content);

    /// <summary>Plain Avalonia controls only - nothing on this tab is drawn by ShapeEngine.</summary>
    private Control BuildControlsTab()
        => new StackPanel()
            .Spacing(10)
            .Children(
                new TextBox()
                    .PlaceholderText("Notes - the game stops seeing the keyboard"),
                new TextBlock().Text("Gallery hero shape").FontSize(12).Foreground(Brushes.DarkGray),
                new ComboBox()
                    .Ref(out gallerySeedCombo)
                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                    .ItemsSource(IconShapes.Select(shape => shape.ToString()).ToArray())
                    .SelectedIndex(Array.IndexOf(IconShapes, gallerySeed)),
                new TextBlock().Text("Accent").FontSize(12).Foreground(Brushes.DarkGray),
                BuildAccentRow(),
                new CheckBox()
                    .Ref(out highlightCheckBox)
                    .Content("Highlight selected icon")
                    .IsChecked(true)
                    .OnIsCheckedChanged(_ => RefreshHighlights()),
                new TextBlock().Text("Gallery animation speed").FontSize(12).Foreground(Brushes.DarkGray),
                new AvSlider()
                    .Ref(out speedSlider)
                    .Minimum(0)
                    .Maximum(3)
                    .Value(1),
                new ListBox()
                    .ItemsSource(log)
                    .Height(110),
                new Expander()
                    .Header("About this tab")
                    .Content(
                        new TextBlock()
                            .Text("Every control here is a plain Avalonia control - nothing on this tab is drawn by ShapeEngine.")
                            .TextWrapping(TextWrapping.Wrap)
                            .Foreground(Brushes.DarkGray)));

    private Control BuildAccentRow()
    {
        var buttons = new Control[Accents.Length];

        for (var i = 0; i < Accents.Length; i++)
        {
            var index = i;
            buttons[i] = new RadioButton()
                .GroupName("accent")
                .Content(Accents[index].Name)
                .IsChecked(index == accentIndex)
                .OnIsCheckedChanged(e =>
                {
                    if (((RadioButton)e.Source!).IsChecked != true) return;

                    accentIndex = index;
                    heroView.InvalidateContent();
                    AppendLog($"Accent -> {Accents[index].Name}");
                });
        }

        return new StackPanel().Orientation(Orientation.Horizontal).Spacing(12).Children(buttons);
    }

    /// <summary>
    /// Buttons whose content is a small ShapeEngine render rather than an icon font or bitmap asset.
    /// </summary>
    /// <remarks>
    /// Drawn directly rather than through a texture view - a button icon redraws on every hover and
    /// click, and at this size there is no texture allocation or read back to avoid paying for anyway.
    /// </remarks>
    private Control BuildIconButtonsTab()
    {
        iconButtons.Clear();

        var buttons = new Control[IconShapes.Length];

        for (var i = 0; i < IconShapes.Length; i++)
        {
            var index = i;
            var shape = IconShapes[i];

            var button = new Button()
                .Padding(new Thickness(10))
                .Content(
                    new StackPanel()
                        .Spacing(4)
                        .Children(
                            new ShapeEngineDirectView { DrawContent = bounds => DrawIcon(bounds, shape, IconColors[index]) }
                                .Width(28)
                                .Height(28),
                            new TextBlock()
                                .Text(shape.ToString())
                                .FontSize(11)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Foreground(Brushes.Gainsboro)))
                .OnClick(_ => SelectIcon(index));

            iconButtons.Add(button);
            buttons[i] = button;
        }

        return new StackPanel()
            .Spacing(10)
            .Children(
                new TextBlock()
                    .Text("Each button's content is a small ShapeEngine render, not an image asset.")
                    .TextWrapping(TextWrapping.Wrap)
                    .Foreground(Brushes.DarkGray),
                new WrapPanel().Children(buttons),
                new TextBlock()
                    .Ref(out selectionText)
                    .Text("Nothing selected yet")
                    .Foreground(Brushes.Gainsboro));
    }

    private void SelectIcon(int index)
    {
        selectedIcon = index;
        selectionText.Text = $"Selected: {IconShapes[index]}";
        RefreshHighlights();
        AppendLog($"Icon clicked -> {IconShapes[index]}");
    }

    /// <summary>Marks the selected button, when the checkbox on the Controls tab asks for it.</summary>
    private void RefreshHighlights()
    {
        var highlightOn = highlightCheckBox.IsChecked == true;

        for (var i = 0; i < iconButtons.Count; i++)
        {
            iconButtons[i].Background = highlightOn && i == selectedIcon ? HighlightBrush : null;
        }
    }

    /// <summary>ShapeEngine content displayed as plain images - none of it is a button.</summary>
    private Control BuildGalleryTab()
        => new StackPanel()
            .Spacing(12)
            .Children(
                new TextBlock()
                    .Text("Everything below is drawn by ShapeEngine and shown as a plain image.")
                    .TextWrapping(TextWrapping.Wrap)
                    .Foreground(Brushes.DarkGray),
                BuildHero(),
                new TextBlock().Text("One static view per shape, drawn once").FontSize(12).Foreground(Brushes.DarkGray),
                BuildShapeRow(),
                new TextBlock().Text("Animated view, redrawn continuously").FontSize(12).Foreground(Brushes.DarkGray),
                BuildAnimatedTile(),
                new TextBlock().Text("Direct view - no texture, rotated").FontSize(12).Foreground(Brushes.DarkGray),
                BuildDirectTile());

    /// <summary>Reflects the Controls tab's combo box and radio group - the cross-tab proof point.</summary>
    private Control BuildHero()
        => new Border()
            .Height(120)
            .CornerRadius(new CornerRadius(8))
            .ClipToBounds(true)
            .Child(
                new ShapeEngineStaticTextureView { DrawContent = bounds => DrawIcon(bounds, gallerySeed, Accents[accentIndex].Color) }
                    .Ref(out heroView));

    private static Control BuildShapeRow()
    {
        var tiles = new Control[IconShapes.Length];

        for (var i = 0; i < IconShapes.Length; i++)
        {
            var shape = IconShapes[i];
            var color = IconColors[i];

            tiles[i] = new Border()
                .Width(48)
                .Height(48)
                .CornerRadius(new CornerRadius(6))
                .Background(new SolidColorBrush(Color.FromArgb(140, 40, 40, 56)))
                .Child(new ShapeEngineStaticTextureView { DrawContent = bounds => DrawIcon(bounds, shape, color) });
        }

        return new StackPanel().Orientation(Orientation.Horizontal).Spacing(8).Children(tiles);
    }

    private Control BuildAnimatedTile()
        => new Border()
            .Height(70)
            .CornerRadius(new CornerRadius(8))
            .ClipToBounds(true)
            .Child(new ShapeEngineAnimatedTextureView { DrawContent = DrawAnimatedTile });

    /// <summary>Three orbiting dots in the current accent colour, fading from front to back.</summary>
    private void DrawAnimatedTile(SeRect bounds)
    {
        var center = bounds.Center;
        var unit = Math.Min(bounds.Width, bounds.Height);
        var accent = Accents[accentIndex].Color;

        for (var i = 0; i < 3; i++)
        {
            var angle = elapsed * (1f + i * 0.4f) + i * MathF.Tau / 3f;
            var position = center + new SeVec2(MathF.Cos(angle), MathF.Sin(angle)) * unit * 0.32f;

            new Circle(position, unit * 0.09f).Draw(accent.SetAlpha((byte)(255 - i * 60)));
        }
    }

    private Control BuildDirectTile()
        => new Border()
            .Height(70)
            .CornerRadius(new CornerRadius(8))
            .ClipToBounds(true)
            .Child(
                new ShapeEngineDirectView
                {
                    DrawContent = DrawDirectTile,
                    RenderTransform = new RotateTransform(-6)
                });

    /// <summary>The same hero shape, drawn straight into Avalonia's framebuffer and gently pulsing.</summary>
    private void DrawDirectTile(SeRect bounds)
    {
        var pulse = 0.5f + 0.5f * MathF.Sin(elapsed * 2.2f);
        var color = Accents[accentIndex].Color.SetAlpha((byte)(160 + pulse * 90));

        DrawIcon(bounds, gallerySeed, color);
    }

    private void AppendLog(string entry) => log.Insert(0, entry);

    private static void DrawIcon(SeRect bounds, IconShape shape, ColorRgba color)
    {
        var center = bounds.Center;
        var unit = Math.Min(bounds.Width, bounds.Height);

        switch (shape)
        {
            case IconShape.Circle:
                new Circle(center, unit * 0.42f).Draw(color);
                break;

            case IconShape.Square:
                new SeRect(center, new SeSize(unit * 0.72f), new AnchorPoint(0.5f)).Draw(color);
                break;

            case IconShape.Triangle:
                DrawTriangle(center, unit * 0.46f, color);
                break;

            case IconShape.Star:
                MakeStar(center, unit * 0.46f, unit * 0.2f, 5).Draw(color);
                break;

            case IconShape.Ring:
                new Circle(center, unit * 0.4f).DrawLines(unit * 0.09f, color, 4f);
                break;
        }
    }

    private static void DrawTriangle(SeVec2 center, float radius, ColorRgba color)
    {
        SeVec2 Point(float angle) => center + new SeVec2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        const float top = -MathF.PI / 2f;
        new Triangle(Point(top), Point(top + MathF.Tau / 3f), Point(top + 2f * MathF.Tau / 3f)).Draw(color);
    }

    /// <summary>Builds an n-pointed star as a polygon, alternating between the outer and inner radius.</summary>
    private static Polygon MakeStar(SeVec2 center, float outerRadius, float innerRadius, int points)
    {
        var vertices = new List<SeVec2>(points * 2);
        var step = MathF.PI / points;
        var angle = -MathF.PI / 2f;

        for (var i = 0; i < points * 2; i++)
        {
            var radius = i % 2 == 0 ? outerRadius : innerRadius;
            vertices.Add(center + new SeVec2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
            angle += step;
        }

        return new Polygon(vertices);
    }
}
