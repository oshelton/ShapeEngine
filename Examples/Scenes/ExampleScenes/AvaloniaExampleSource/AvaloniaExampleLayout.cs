using System.Numerics;
using Avalonia.Controls;
using ShapeEngine.Avalonia;
using Grid = Avalonia.Controls.Grid;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>
/// The part of the window the Examples app leaves free, and anchors that stay inside it.
/// </summary>
/// <remarks>
/// The app draws its title across the top and the input device readout across the bottom, and screen
/// textures composite before both, so a surface reaching into either band is drawn over. These mirror
/// the bands <c>GameloopExamples.UIRects</c> lays out, rounded outwards for clearance.
/// </remarks>
public static class AvaloniaExampleLayout
{
    /// <summary>Below the title bar and its rule.</summary>
    public const float SafeTop = 0.12f;

    /// <summary>Above the input device readout.</summary>
    public const float SafeBottom = 0.88f;

    public const float SafeHeight = SafeBottom - SafeTop;

    /// <summary>Keeps a panel off the window edge.</summary>
    public const float Inset = 0.03f;

    /// <summary>Extra clearance beyond <see cref="SafeTop"/> and <see cref="SafeBottom"/>, so a panel
    /// doesn't sit flush against the title rule or the device readout.</summary>
    public const float Padding = 0.02f;

    /// <summary>Top edge a panel should actually align to - <see cref="SafeTop"/> plus <see cref="Padding"/>.</summary>
    public const float PaddedTop = SafeTop + Padding;

    /// <summary>Bottom edge a panel should actually align to - <see cref="SafeBottom"/> minus <see cref="Padding"/>.</summary>
    public const float PaddedBottom = SafeBottom - Padding;

    public const float PaddedHeight = PaddedBottom - PaddedTop;

    /// <summary>
    /// An anchor for the given rectangle, in fractions of the window measured from the top left.
    /// </summary>
    /// <remarks>
    /// <see cref="AvaloniaSurfaceAnchor"/> pins by a fraction that doubles as the surface's own origin,
    /// which survives a resize but is hard to read back as a rectangle. An edge lands at
    /// <c>position * (1 - stretch)</c>, so the position is that edge over the space left around it.
    /// </remarks>
    public static AvaloniaSurfaceAnchor Region(float left, float top, float width, float height)
    {
        var x = width < 1f ? left / (1f - width) : 0f;
        var y = height < 1f ? top / (1f - height) : 0f;

        return new AvaloniaSurfaceAnchor(new Vector2(width, height), new Vector2(x, y));
    }

    /// <summary>A column filling the safe band down the left of the window.</summary>
    public static AvaloniaSurfaceAnchor LeftColumn(float width) => Region(Inset, PaddedTop, width, PaddedHeight);

    /// <summary>A column filling the safe band, centered horizontally in the window.</summary>
    public static AvaloniaSurfaceAnchor CenteredColumn(float width) => Region((1f - width) / 2f, PaddedTop, width, PaddedHeight);

    /// <summary>
    /// Wraps content in a grid that confines it to the safe band, for a surface covering the window.
    /// </summary>
    /// <param name="width">Fraction of the window the content may occupy. Defaults to all of it.</param>
    /// <remarks>
    /// Star sizing rather than pixel margins: the app's interface is sized in fractions and grows with
    /// the display, so a margin that clears the title bar in a window clears a sliver of it full screen.
    /// </remarks>
    public static Control SafeArea(Control content, float width = 1f)
    {
        // Capped at 1 - 2 * Inset, not 1 - Inset, so the default width leaves a matching margin on the
        // right rather than a right column of exactly zero.
        var contentWidth = Math.Min(width, 1f - 2f * Inset);
        var remainder = 1f - Inset - contentWidth;

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions($"{PaddedTop}*,{PaddedHeight}*,{1f - PaddedBottom}*"),
            ColumnDefinitions = new ColumnDefinitions($"{Inset}*,{contentWidth}*,{remainder}*")
        };

        Grid.SetRow(content, 1);
        Grid.SetColumn(content, 1);
        grid.Children.Add(content);

        return grid;
    }
}
