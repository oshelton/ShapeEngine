using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// Dragging data out of a plain Avalonia panel and dropping it onto a rectangle ShapeEngine draws itself,
/// on a second, entirely separate surface.
/// </summary>
/// <remarks>
/// Avalonia's drag-and-drop system doesn't care who owns the drop target's rendering - only that the
/// target is a real control with <c>DragDrop.AllowDrop</c> set and its own routed event handlers. Here
/// that control is a <c>ShapeEngineDirectView</c>: hit-testing, drag-over feedback and the drop itself all
/// go through ordinary Avalonia routed events, while what's actually on screen is raylib drawing a
/// rectangle that only changes colour and gains a shape glyph once something lands on it.
/// <para>
/// The two panels are two separate <see cref="AvaloniaSurface"/>s, each with its own top level - so the
/// drag genuinely crosses from one window-like surface into a completely different one mid-gesture, not
/// just from one control to another within the same panel. That crossing is what
/// <c>ShapeEngineDragSource</c> in <c>ShapeEngine.Avalonia</c> exists to handle: Avalonia's own
/// <c>DragDrop.DoDragDropAsync</c> resolves to nothing without a registered <c>IPlatformDragSource</c>,
/// and the one here tracks the drag as plain state rather than tying it to whichever surface it started
/// on, since a real platform drag source would normally get this for free from the OS.
/// </para>
/// </remarks>
public class AvaloniaDragDropExample : AvaloniaExampleSceneBase
{
    private static readonly AvaloniaSurfaceAnchor SourceAnchor =
        AvaloniaExampleLayout.Region(AvaloniaExampleLayout.Inset, AvaloniaExampleLayout.PaddedTop, 0.45f, AvaloniaExampleLayout.PaddedHeight);

    private static readonly AvaloniaSurfaceAnchor TargetAnchor =
        AvaloniaExampleLayout.Region(0.52f, AvaloniaExampleLayout.PaddedTop, 0.45f, AvaloniaExampleLayout.PaddedHeight);

    private AvaloniaSurface? sourceSurface;
    private AvaloniaSurface? targetSurface;
    private AvaloniaDragDropSourcePanel? sourcePanel;
    private AvaloniaDragDropTargetPanel? targetPanel;

    public AvaloniaDragDropExample()
    {
        Title = "Avalonia - Drag and Drop";
        Description = "Drag a chip from one surface and drop it on the ShapeEngine-drawn rectangle in another";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        sourcePanel = new AvaloniaDragDropSourcePanel();
        sourceSurface = new AvaloniaSurface(sourcePanel, SourceAnchor, scaleContent: true);

        targetPanel = new AvaloniaDragDropTargetPanel();
        targetSurface = new AvaloniaSurface(targetPanel, TargetAnchor, scaleContent: true);

        return [sourceSurface, targetSurface];
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (sourceSurface is { } source && sourcePanel is { } sp) sp.SetStatus(Status(source));
        if (targetSurface is { } target && targetPanel is { } tp) tp.SetStatus(Status(target));
    }

    private static string Status(AvaloniaSurface surface)
    {
        var rect = surface.DestinationRect;
        return
            $"""
             Drawn at {rect.Width:0}x{rect.Height:0}
             WantsPointer: {surface.WantsPointer}   WantsKeyboard: {surface.WantsKeyboard}
             """;
    }
}
