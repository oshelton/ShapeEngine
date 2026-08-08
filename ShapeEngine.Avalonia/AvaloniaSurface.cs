using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Raylib_cs;
using ShapeEngine.Avalonia.Input;
using ShapeEngine.Core.GameDef;
using ShapeEngine.Core.Structs;
using ShapeEngine.Input;
using ShapeEngine.Screen;
using AvControl = Avalonia.Controls.Control;
using RlColor = Raylib_cs.Color;
using SeRect = ShapeEngine.Geometry.RectDef.Rect;

namespace ShapeEngine.Avalonia;

/// <summary>
/// Hosts Avalonia UI inside a ShapeEngine game, rendered onto the game's OpenGL surface.
/// </summary>
/// <remarks>
/// Register it with <c>Game.AddCustomEvent</c> and dispose it when done; Avalonia must already be
/// configured with <see cref="AppBuilderExtensions.UseShapeEngine"/>.
/// <para>
/// The surface owns the <see cref="ScreenTexture"/> it renders through, so the texture's
/// <see cref="ScreenTexture.Shaders"/> post-process the interface. Screen textures composite before the
/// game's <c>DrawUI</c>, so anything the game draws there covers it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// AppBuilder.Configure&lt;MyApp&gt;().UseShapeEngine().SetupWithoutStarting();
///
/// Game.Instance.AddCustomEvent(new AvaloniaSurface(new MyMenuView()));
/// </code>
/// </example>
public sealed class AvaloniaSurface : Game.CustomEvent, IDisposable
{
    private readonly ShapeEngineTopLevelImpl impl;
    private readonly AvaloniaInputPump inputPump;
    private readonly ScreenTexture placement;
    private readonly Viewbox scaleBox = new() { Stretch = Stretch.Uniform };

    /// <summary>Drives Avalonia's animation clock, independent of the game's own time scaling.</summary>
    private readonly Stopwatch renderClock = Stopwatch.StartNew();

    private AvControl? content;
    private bool scaleContent;
    private MouseCursor currentCursor = MouseCursor.Default;
    private bool hasLockedMouse;
    private bool hasLockedKeyboard;
    private bool isDisposed;

    /// <summary>Creates a surface and the screen texture it renders through.</summary>
    /// <param name="content">The Avalonia control tree to display.</param>
    /// <param name="anchor">Where the surface sits on screen. Defaults to the whole window.</param>
    /// <param name="scaleContent">
    /// Scales the content to fit the surface instead of laying it out at the surface's size.
    /// </param>
    /// <param name="order">
    /// Execution order relative to other custom events. Lower values run - and draw - first.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Avalonia has not been configured with <see cref="AppBuilderExtensions.UseShapeEngine"/>.
    /// </exception>
    public AvaloniaSurface(
        AvControl? content = null,
        AvaloniaSurfaceAnchor? anchor = null,
        bool scaleContent = false,
        int order = 0)
        : base(order)
    {
        if (!ShapeEnginePlatform.IsInitialized)
        {
            throw new InvalidOperationException(
                $"Avalonia isn't set up yet. Call AppBuilder.Configure<...>().{nameof(AppBuilderExtensions.UseShapeEngine)}().SetupWithoutStarting() before creating an AvaloniaSurface.");
        }

        var placementAnchor = anchor ?? AvaloniaSurfaceAnchor.FullScreen;

        // Multi shader support up front, so post-processing the interface never means rebuilding it.
        placement = new ScreenTexture(placementAnchor.Stretch, placementAnchor.Position, ShaderSupportType.Multi);
        placement.Initialize(Game.Instance.Window.CurScreenSize, Raylib.GetMousePosition());
        placement.OnDrawGame += OnPlacementDraw;

        Game.Instance.AddScreenTexture(placement);

        impl = new ShapeEngineTopLevelImpl(
            ShapeEnginePlatform.PlatformGraphics,
            new ShapeEngineClipboard(),
            ShapeEnginePlatform.Compositor);

        impl.CursorChanged += OnCursorChanged;

        SyncSize();

        TopLevel = new ShapeEngineTopLevel(impl)
        {
            // No background: it would hide the game and hit test, stealing the pointer from it.
            Background = null,
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None]
        };

        inputPump = new AvaloniaInputPump(impl);

        this.content = content;
        this.scaleContent = scaleContent;
        ApplyContent();

        TopLevel.Prepare();
        TopLevel.StartRendering();
    }

    /// <summary>The Avalonia root hosting <see cref="Content"/>.</summary>
    public ShapeEngineTopLevel TopLevel { get; }

    /// <summary>The Avalonia control tree drawn over the game.</summary>
    public AvControl? Content
    {
        get => content;
        set
        {
            if (ReferenceEquals(content, value)) return;

            content = value;
            ApplyContent();
        }
    }

    /// <summary>
    /// Whether the content is scaled to fit the surface rather than laid out at the surface's size.
    /// </summary>
    /// <remarks>
    /// Off, a larger surface gives controls more room and text keeps its size. On, everything grows
    /// together - scaled through the visual tree, so text stays crisp and hit testing follows.
    /// <para>
    /// Scaled content is measured unconstrained, so give it an intrinsic size - usually a <c>Width</c> on
    /// the root control. Without one, wrapping text never wraps and the runaway natural width scales
    /// everything down to nothing.
    /// </para>
    /// </remarks>
    public bool ScaleContent
    {
        get => scaleContent;
        set
        {
            if (scaleContent == value) return;

            scaleContent = value;
            ApplyContent();
        }
    }

    /// <summary>
    /// The screen texture this surface renders through, for attaching shaders or changing the draw order.
    /// Owned by the surface and unloaded with it.
    /// </summary>
    public ScreenTexture PlacementTexture => placement;

    /// <summary>The area of the window the UI is drawn into, in screen coordinates.</summary>
    public SeRect DestinationRect => placement.GetDestinationRect();

    /// <summary>Whether the cursor is currently over a hit-testable Avalonia control.</summary>
    public bool WantsPointer { get; private set; }

    /// <summary>Whether an Avalonia control is currently accepting typed characters.</summary>
    public bool WantsKeyboard { get; private set; }

    /// <summary>
    /// Whether ShapeEngine's own input devices are locked while the UI has capture. Default is true.
    /// </summary>
    /// <remarks>
    /// Turn this off to route input yourself - <see cref="WantsPointer"/> and
    /// <see cref="WantsKeyboard"/> stay accurate either way. Locking takes effect on the next
    /// <c>InputSystem</c> update, so each transition leaves one frame of overlap: enough to matter for
    /// click-through, not for held input.
    /// </remarks>
    public bool CaptureGameInput { get; set; } = true;

    #region Game loop hooks

    /// <inheritdoc/>
    /// <remarks>
    /// Runs after the engine has updated the screen textures, so the placement texture's size and scaled
    /// mouse position are already current.
    /// </remarks>
    protected override void PreHandleInput(GameTime time, Vector2 mousePosGame, Vector2 mousePosGameUi, Vector2 mousePosUi)
    {
        if (isDisposed) return;

        SyncSize();
        UpdateCapture();
        inputPump.Pump(GetPointerPosition(), WantsPointer || hasLockedMouse, WantsKeyboard || hasLockedKeyboard);
        ApplyInputLocks();
    }

    /// <summary>
    /// Renders the UI and blits it into the placement texture, from inside the texture's draw pass.
    /// </summary>
    /// <remarks>
    /// The game pass rather than the UI one, because the texture applies its shaders between the two -
    /// drawing in <c>OnDrawUI</c> would put the interface past them. Running the Skia pass inside the
    /// texture's render target is safe because <c>RlglStateGuard</c> restores whichever framebuffer was
    /// bound.
    /// </remarks>
    private void OnPlacementDraw(ScreenInfo info, ScreenTexture texture)
    {
        if (isDisposed) return;

        RenderAvalonia();
        Present(new Rectangle(0, 0, texture.Width, texture.Height));
    }

    /// <summary>Advances Avalonia by one frame and rasterizes it into the surface framebuffer.</summary>
    /// <remarks>
    /// The order matters: draining the jobs the tick queues before painting keeps layout and animation
    /// changes in this frame rather than the next.
    /// </remarks>
    private void RenderAvalonia()
    {
        ShapeEnginePlatform.PumpDispatcher();
        ShapeEnginePlatform.TriggerRenderTick(renderClock.Elapsed);
        Dispatcher.UIThread.RunJobs();

        impl.OnDraw(new Rect(impl.ClientSize));
    }

    /// <summary>Draws the rendered UI into the currently bound render target.</summary>
    private void Present(Rectangle destination)
    {
        if (impl.TryGetSurface() is not { IsDisposed: false } surface) return;

        // Avalonia's output is premultiplied; raylib's default alpha blending would darken the edges.
        Raylib.BeginBlendMode(BlendMode.AlphaPremultiply);
        Raylib.DrawTexturePro(
            surface.Texture,
            new Rectangle(0, 0, surface.Texture.Width, surface.Texture.Height),
            destination,
            Vector2.Zero,
            0f,
            RlColor.White);
        Raylib.EndBlendMode();
    }

    #endregion

    #region Content, sizing, input arbitration and cursor

    /// <summary>Puts the content into the top level, wrapped for scaling when asked for.</summary>
    /// <remarks>
    /// Detached before reattaching, because a control cannot be added to a new parent while the old one
    /// still holds it.
    /// </remarks>
    private void ApplyContent()
    {
        scaleBox.Child = null;
        TopLevel.Content = null;

        if (content is null) return;

        if (scaleContent)
        {
            scaleBox.Child = content;
            TopLevel.Content = scaleBox;
        }
        else
        {
            TopLevel.Content = content;
        }
    }

    /// <summary>Matches the surface framebuffer to the placement texture.</summary>
    /// <remarks>
    /// The texture is sized in physical pixels, so the DPI scale is what leaves Avalonia laying out in
    /// device independent pixels while rasterizing at full resolution.
    /// </remarks>
    private void SyncSize()
    {
        var size = new PixelSize(Math.Max(placement.Width, 1), Math.Max(placement.Height, 1));

        var scaling = Raylib.GetWindowScaleDPI().X;
        if (scaling <= 0f || Single.IsNaN(scaling)) scaling = 1f;

        impl.SetRenderSize(size, scaling);
    }

    /// <summary>The cursor position in Avalonia's client coordinate space.</summary>
    /// <remarks>
    /// The engine has already mapped the mouse into the texture's pixel space for its anchor, so all that
    /// remains is the conversion to device independent pixels.
    /// </remarks>
    private Point GetPointerPosition()
    {
        var position = placement.GameUiScreenInfo.MousePos;
        var scaling = impl.RenderScaling;

        return new Point(position.X / scaling, position.Y / scaling);
    }

    private void UpdateCapture()
    {
        // A hit result of the top level itself means the cursor is over empty space rather than over
        // actual UI, so the game should keep the pointer.
        var hit = TopLevel.InputHitTest(GetPointerPosition());
        WantsPointer = hit is not null && !ReferenceEquals(hit, TopLevel);

        // Focus alone is a bad signal - a focused button would swallow WASD forever. Text input being
        // active means a control genuinely needs the keys.
        WantsKeyboard = impl.TextInputMethod.IsActive;
    }

    private void ApplyInputLocks()
    {
        var input = Game.Instance.Input;
        SetLock(input.Mouse, CaptureGameInput && WantsPointer, ref hasLockedMouse);
        SetLock(input.Keyboard, CaptureGameInput && WantsKeyboard, ref hasLockedKeyboard);
    }

    private void ReleaseInputLocks()
    {
        var input = Game.Instance.Input;
        SetLock(input.Mouse, false, ref hasLockedMouse);
        SetLock(input.Keyboard, false, ref hasLockedKeyboard);
    }

    /// <remarks>
    /// Tracked per surface, so several surfaces can each lock and release without unlocking on another's
    /// behalf.
    /// </remarks>
    private static void SetLock(InputDevice device, bool locked, ref bool isLocked)
    {
        if (locked == isLocked) return;

        isLocked = locked;
        if (locked) device.Lock();
        else device.Unlock();
    }

    private void OnCursorChanged(MouseCursor cursor)
    {
        currentCursor = cursor;
        Raylib.SetMouseCursor(cursor);
    }

    #endregion

    /// <summary>Tears down the Avalonia top level and the screen texture, releasing their GPU resources.</summary>
    /// <remarks>Remove the surface from the game with <c>Game.RemoveCustomEvent</c> first.</remarks>
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;

        ReleaseInputLocks();

        if (currentCursor != MouseCursor.Default) Raylib.SetMouseCursor(MouseCursor.Default);

        placement.OnDrawGame -= OnPlacementDraw;
        Game.Instance.RemoveScreenTexture(placement);
        placement.Unload();

        impl.CursorChanged -= OnCursorChanged;
        TopLevel.StopRendering();
        TopLevel.Dispose();
        impl.Dispose();
    }
}
