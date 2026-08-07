using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
/// Register it with <c>Game.AddCustomEvent</c> and set <see cref="Content"/>. Avalonia must already be
/// configured - see <see cref="AppBuilderExtensions.UseShapeEngine"/>.
/// <para>
/// By default the surface covers the whole window at native resolution. Pass a <see cref="ScreenTexture"/>
/// to place and scale it instead: the texture's <c>ScreenTextureMode</c> decides the surface resolution,
/// where on screen it lands, and how the mouse maps into it - the same rules the game texture follows.
/// Use <see cref="Order"/> (from <c>Game.CustomEvent</c>) to control where it draws relative to other
/// custom events.
/// </para>
/// <para>
/// Layering differs between the two arrangements, because a screen texture composites earlier in the
/// frame than a custom event does. Without a placement texture - and with one in
/// <see cref="AvaloniaSurfaceScaling.NativeDensity"/> - the UI is drawn after the game's <c>DrawUI</c>,
/// so it sits on top of everything. With a placement texture in
/// <see cref="AvaloniaSurfaceScaling.MatchTexture"/> the engine draws it alongside the other screen
/// textures, which is before <c>DrawUI</c>, so anything the game draws there covers the UI. Order
/// against other screen textures is then controlled by the texture's <c>DrawToScreenOrder</c>.
/// </para>
/// </remarks>
/// <example>
/// Full window:
/// <code>
/// AppBuilder.Configure&lt;MyApp&gt;().UseShapeEngine().SetupWithoutStarting();
///
/// Game.Instance.AddCustomEvent(new AvaloniaSurface(new MyMenuView()));
/// </code>
/// Placed and scaled by a screen texture - here a fixed 1920x1080 design resolution that letterboxes
/// into the window, with a shader applied over the UI:
/// <code>
/// var placement = new ScreenTexture(new Dimensions(1920, 1080), ShaderSupportType.Single);
/// var surface = new AvaloniaSurface(new MyMenuView(), placement);
///
/// Game.Instance.AddScreenTexture(placement);  // drives sizing, placement and shaders
/// Game.Instance.AddCustomEvent(surface);      // drives input and Avalonia rendering
/// </code>
/// </example>
public sealed class AvaloniaSurface : Game.CustomEvent, IDisposable
{
    private readonly ShapeEngineTopLevelImpl impl;
    private readonly AvaloniaInputPump inputPump;
    private readonly ScreenTexture? placement;

    /// <summary>Drives Avalonia's animation clock, independent of the game's own time scaling.</summary>
    private readonly Stopwatch renderClock = Stopwatch.StartNew();

    private Vector2 pointerScale = Vector2.One;
    private MouseCursor currentCursor = MouseCursor.Default;
    private bool hasLockedMouse;
    private bool hasLockedKeyboard;
    private bool isDisposed;

    /// <summary>Creates a surface that covers the whole window at native resolution.</summary>
    /// <param name="content">The Avalonia control tree to display.</param>
    /// <param name="order">
    /// Execution order relative to other custom events. Lower values run - and draw - first.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Avalonia has not been configured with <see cref="AppBuilderExtensions.UseShapeEngine"/>.
    /// </exception>
    public AvaloniaSurface(AvControl? content = null, int order = 0)
        : this(content, null, AvaloniaSurfaceScaling.MatchTexture, order) { }

    /// <summary>Creates a surface placed and scaled by a <see cref="ScreenTexture"/>.</summary>
    /// <param name="content">The Avalonia control tree to display.</param>
    /// <param name="placementTexture">
    /// The texture whose <c>ScreenTextureMode</c> decides the surface resolution, its destination
    /// rectangle on screen and the mouse mapping. Register it with <c>Game.AddScreenTexture</c> as
    /// well - the engine has to update and draw it.
    /// </param>
    /// <param name="scaling">How the UI is rasterized when the texture is drawn at a different size.</param>
    /// <param name="order">
    /// Execution order relative to other custom events. Lower values run - and draw - first.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Avalonia has not been configured with <see cref="AppBuilderExtensions.UseShapeEngine"/>.
    /// </exception>
    public AvaloniaSurface(
        AvControl? content,
        ScreenTexture? placementTexture,
        AvaloniaSurfaceScaling scaling = AvaloniaSurfaceScaling.MatchTexture,
        int order = 0)
        : base(order)
    {
        if (!ShapeEnginePlatform.IsInitialized)
        {
            throw new InvalidOperationException(
                $"Avalonia isn't set up yet. Call AppBuilder.Configure<...>().{nameof(AppBuilderExtensions.UseShapeEngine)}().SetupWithoutStarting() before creating an AvaloniaSurface.");
        }

        impl = new ShapeEngineTopLevelImpl(
            ShapeEnginePlatform.PlatformGraphics,
            new ShapeEngineClipboard(),
            ShapeEnginePlatform.Compositor);

        impl.CursorChanged += OnCursorChanged;

        placement = placementTexture;
        Scaling = scaling;

        if (placement is not null)
        {
            // The texture self-initializes on its first Update, but the surface needs its dimensions
            // now to size the framebuffer. Initialize is a no-op if the game got there first.
            placement.Initialize(Game.Instance.Window.CurScreenSize, Raylib.GetMousePosition());
            placement.OnDrawUI += OnPlacementDrawUi;
        }

        SyncSize();

        TopLevel = new ShapeEngineTopLevel(impl)
        {
            // No background of its own: the game has to show through everywhere the content doesn't
            // draw, and a hit test on the background would also steal the pointer from the game.
            Background = null,
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent, WindowTransparencyLevel.None],
            Content = content
        };

        inputPump = new AvaloniaInputPump(impl);

        TopLevel.Prepare();
        TopLevel.StartRendering();
    }

    /// <summary>The Avalonia root hosting <see cref="Content"/>.</summary>
    public ShapeEngineTopLevel TopLevel { get; }

    /// <summary>The Avalonia control tree drawn over the game.</summary>
    public AvControl? Content
    {
        get => TopLevel.Content as AvControl;
        set => TopLevel.Content = value;
    }

    /// <summary>
    /// The screen texture that places and scales this surface, or <c>null</c> when it covers the window.
    /// </summary>
    /// <remarks>
    /// Change its mode, dimensions, anchor or shaders to move and resize the UI - the surface follows
    /// on the next frame.
    /// </remarks>
    public ScreenTexture? PlacementTexture => placement;

    /// <summary>How the UI is rasterized when the placement texture is drawn at a different size.</summary>
    /// <remarks>Has no effect without a <see cref="PlacementTexture"/>.</remarks>
    public AvaloniaSurfaceScaling Scaling { get; set; }

    /// <summary>
    /// The logical size the UI is authored for. Setting it scales the content to fill the surface
    /// instead of reflowing the layout into a bigger or smaller area.
    /// </summary>
    /// <remarks>
    /// Left <c>null</c>, a wider surface simply gives controls more room and text keeps its physical
    /// size. Scaling is uniform and fits, so a surface whose aspect ratio differs from the design
    /// leaves slack on one axis for the content's own alignment to take up.
    /// <para>
    /// This drives Avalonia's <c>RenderScaling</c>, so content is re-rasterized at the scaled size
    /// rather than magnified as a bitmap - text stays crisp at any scale.
    /// </para>
    /// </remarks>
    public Dimensions? DesignSize { get; set; }

    /// <summary>An additional uniform scale applied to the content. Defaults to 1.</summary>
    /// <remarks>
    /// Combines with <see cref="DesignSize"/>, and works on its own as a HUD-size or accessibility knob.
    /// </remarks>
    public double ContentScale { get; set; } = 1.0;

    /// <summary>The area of the window the UI is drawn into, in screen coordinates.</summary>
    public SeRect DestinationRect
        => placement is not null
            ? placement.GetDestinationRect()
            : new SeRect(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight());

    /// <summary>Whether the cursor is currently over a hit-testable Avalonia control.</summary>
    public bool WantsPointer { get; private set; }

    /// <summary>Whether an Avalonia control is currently accepting typed characters.</summary>
    public bool WantsKeyboard { get; private set; }

    /// <summary>
    /// Whether ShapeEngine's own input devices are locked while the UI has capture. Default is true.
    /// </summary>
    /// <remarks>
    /// Turn this off to route input yourself - <see cref="WantsPointer"/> and
    /// <see cref="WantsKeyboard"/> stay accurate either way.
    /// <para>
    /// Locking takes effect on the next <c>InputSystem</c> update, so the frame in which the cursor
    /// crosses onto the UI is still visible to the game. That is one frame of overlap on each
    /// transition, which matters for click-through but not for held input.
    /// </para>
    /// </remarks>
    public bool CaptureGameInput { get; set; } = true;

    #region Game loop hooks

    /// <inheritdoc/>
    /// <remarks>
    /// Runs after the engine has updated the screen textures, so a placement texture's dimensions and
    /// scaled mouse position are already current for this frame.
    /// </remarks>
    protected override void PreHandleInput(GameTime time, Vector2 mousePosGame, Vector2 mousePosGameUi, Vector2 mousePosUi)
    {
        if (isDisposed) return;

        SyncSize();
        UpdateCapture();
        inputPump.Pump(GetPointerPosition(), WantsPointer || hasLockedMouse, WantsKeyboard || hasLockedKeyboard);
        ApplyInputLocks();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Rendering happens here rather than in an update hook because <c>PreDrawUi</c> is the only game
    /// loop hook guaranteed to run exactly once per frame - with a fixed framerate or dynamic
    /// substepping, the update hooks run anywhere from zero to several times.
    /// <para>
    /// When the surface renders into a placement texture this is skipped: the work happens in the
    /// texture's own draw pass instead, which the engine runs earlier in the frame.
    /// </para>
    /// </remarks>
    protected override void PreDrawUi(ScreenInfo info)
    {
        if (isDisposed || RendersIntoPlacementTexture) return;

        RenderAvalonia();
    }

    /// <inheritdoc/>
    protected override void PostDrawUi(ScreenInfo info)
    {
        if (isDisposed || RendersIntoPlacementTexture) return;

        var destination = DestinationRect;
        Present(new Rectangle(destination.X, destination.Y, destination.Width, destination.Height));
    }

    /// <summary>
    /// Renders the UI and blits it into the placement texture, from inside the texture's draw pass.
    /// </summary>
    /// <remarks>
    /// Running here rather than in <c>PreDrawUi</c> keeps the UI a frame fresh: the engine draws screen
    /// textures before it begins the window's draw pass, so a render scheduled later would be blitted
    /// one frame late. The Skia pass is safe inside the texture's render target because
    /// <c>RlglStateGuard</c> restores whichever framebuffer was bound.
    /// </remarks>
    private void OnPlacementDrawUi(ScreenInfo info, ScreenTexture texture)
    {
        if (isDisposed || !RendersIntoPlacementTexture) return;

        RenderAvalonia();
        Present(new Rectangle(0, 0, texture.Width, texture.Height));
    }

    /// <summary>Whether the UI is blitted into the placement texture rather than straight to screen.</summary>
    private bool RendersIntoPlacementTexture
        => placement is not null && Scaling == AvaloniaSurfaceScaling.MatchTexture;

    /// <summary>Advances Avalonia by one frame and rasterizes it into the surface framebuffer.</summary>
    /// <remarks>
    /// The order matters: dispatcher work can invalidate layout, and draining the jobs the tick queues
    /// before painting keeps layout and animation changes in this frame rather than the next.
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

    #region Sizing, input arbitration and cursor

    /// <summary>
    /// Matches the surface framebuffer to whatever it is currently being drawn into, and works out the
    /// scaling that turns it into Avalonia's layout space.
    /// </summary>
    /// <remarks>
    /// Without a design size the two scaling modes produce the same layout space - the placement
    /// texture's resolution, or the window size when there is no placement texture - and differ only in
    /// rasterization. A design size overrides that and makes the layout space fixed instead.
    /// </remarks>
    private void SyncSize()
    {
        PixelSize renderSize;
        double renderScaling;

        // The coordinate space incoming mouse positions arrive in, which is not necessarily the space
        // Avalonia lays out in once DesignSize or ContentScale are involved.
        Vector2 pointerSpace;

        if (placement is null)
        {
            var renderWidth = Raylib.GetRenderWidth();
            var renderHeight = Raylib.GetRenderHeight();
            var screenWidth = Raylib.GetScreenWidth();
            var screenHeight = Raylib.GetScreenHeight();

            // Derived from the framebuffer/window ratio rather than GetWindowScaleDPI so it stays 1.0
            // when ShapeEngine's high DPI window flag is off, which is when raylib renders at logical
            // size.
            renderSize = new PixelSize(Math.Max(renderWidth, 1), Math.Max(renderHeight, 1));
            renderScaling = screenWidth > 0 ? renderWidth / (double)screenWidth : 1.0;
            pointerSpace = new Vector2(Math.Max(screenWidth, 1), Math.Max(screenHeight, 1));
        }
        else
        {
            var textureSize = new PixelSize(Math.Max(placement.Width, 1), Math.Max(placement.Height, 1));
            pointerSpace = new Vector2(textureSize.Width, textureSize.Height);

            if (Scaling == AvaloniaSurfaceScaling.MatchTexture)
            {
                // One Avalonia pixel per texture pixel; the texture handles the scaling to the screen.
                renderSize = textureSize;
                renderScaling = 1.0;
            }
            else
            {
                // Rasterize at the size actually shown on screen, then scale layout back down so the
                // UI still lays out in the texture's coordinate space.
                var destination = placement.GetDestinationRect();
                var dpi = Raylib.GetWindowScaleDPI();

                renderSize = new PixelSize(
                    Math.Max((int)MathF.Round(destination.Width * dpi.X), 1),
                    Math.Max((int)MathF.Round(destination.Height * dpi.Y), 1));
                renderScaling = renderSize.Width / (double)textureSize.Width;
            }
        }

        // A design size pins layout to fixed dimensions and scales the content to fill the surface,
        // instead of letting the layout expand into it. Uniform, and fitting rather than filling, so
        // nothing is cropped when the aspect ratios disagree.
        if (DesignSize is { } design && design.Width > 0 && design.Height > 0)
        {
            renderScaling = Math.Min(
                renderSize.Width / (double)design.Width,
                renderSize.Height / (double)design.Height);
        }

        renderScaling *= ContentScale;

        if (renderScaling <= 0.0 || Double.IsNaN(renderScaling)) renderScaling = 1.0;

        impl.SetRenderSize(renderSize, renderScaling);

        // Avalonia's client size is the framebuffer divided by the scaling, so it only matches the
        // pointer space when nothing is scaling the content. Cache the correction for the pointer.
        pointerScale = new Vector2(
            (float)(impl.ClientSize.Width / pointerSpace.X),
            (float)(impl.ClientSize.Height / pointerSpace.Y));
    }

    /// <summary>The cursor position in Avalonia's client coordinate space.</summary>
    /// <remarks>
    /// With a placement texture the engine has already mapped the window mouse position into texture
    /// space for the texture's mode, letterbox offsets included. Positions outside the texture simply
    /// fail to hit test, which is the behaviour we want.
    /// </remarks>
    private Point GetPointerPosition()
    {
        var position = placement is not null
            ? placement.GameUiScreenInfo.MousePos
            : Raylib.GetMousePosition();

        return new Point(position.X * pointerScale.X, position.Y * pointerScale.Y);
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
    /// Tracked per surface rather than read back from the device, so several surfaces can each lock and
    /// release without unlocking on another's behalf.
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

    /// <summary>Tears down the Avalonia top level and releases its GPU resources.</summary>
    /// <remarks>Remove the surface from the game with <c>Game.RemoveCustomEvent</c> first.</remarks>
    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;

        ReleaseInputLocks();

        if (currentCursor != MouseCursor.Default) Raylib.SetMouseCursor(MouseCursor.Default);

        if (placement is not null) placement.OnDrawUI -= OnPlacementDrawUi;

        impl.CursorChanged -= OnCursorChanged;
        TopLevel.StopRendering();
        TopLevel.Dispose();
        impl.Dispose();
    }
}
