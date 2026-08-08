namespace ShapeEngine.Avalonia.Controls;

/// <summary>
/// Displays ShapeEngine drawing that does not animate, redrawing only when it has to.
/// </summary>
/// <remarks>
/// Draws once when it first gets a size, again whenever it is resized, and otherwise only when
/// <see cref="ShapeEngineTextureView.InvalidateContent"/> is called. Between those it costs nothing per
/// frame, which makes it the right choice for anything static - an emblem, a map, a generated
/// background, a chart that changes when the data does.
/// <para>
/// Call <see cref="ShapeEngineTextureView.InvalidateContent"/> after changing anything the drawing
/// depends on, otherwise the old image stays on screen.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var view = new ShapeEngineStaticTextureView
/// {
///     Height = 120,
///     DrawContent = bounds => new Circle(bounds.Center, 40f).Draw(ColorRgba.White)
/// };
///
/// // later, when the data behind the drawing changes
/// view.InvalidateContent();
/// </code>
/// </example>
public sealed class ShapeEngineStaticTextureView : ShapeEngineTextureView
{
    /// <inheritdoc/>
    protected override bool ShouldRedraw(float deltaTime, bool contentIsDirty) => contentIsDirty;
}
