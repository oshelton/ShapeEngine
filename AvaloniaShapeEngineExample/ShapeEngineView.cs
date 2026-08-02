using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.VisualTree;
using AvaloniaShapeEngineExample.Win32;
using Raylib_cs;
using ShapeEngine.Core;
using ShapeEngine.Core.GameDef;

namespace AvaloniaShapeEngineExample;

/// <summary>
/// Hosts a ShapeEngine <see cref="GameDef.Game"/>'s raylib window as a native child window
/// embedded in the Avalonia visual tree, modeled on LibVLCSharp's VideoView. Windows-only.
/// Everything runs on the Avalonia UI thread: the game is constructed here, and ticked via a raw
/// Win32 <see cref="NativeMethods.SetTimer"/> callback — delivered by the same thread's message
/// loop, but entirely independent of Avalonia's own Dispatcher.
/// </summary>
[SupportedOSPlatform("windows")]
public class ShapeEngineView : NativeControlHost
{
    /// <summary>
    /// Defines the <see cref="Content"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<ShapeEngineView, object?>(nameof(Content));

    /// <summary>
    /// Builds the <see cref="Game"/> instance to host. Invoked synchronously on the UI thread
    /// when the native control is created. Must be set before the control attaches.
    /// </summary>
    public Func<Game>? GameFactory { get; set; }

    /// <summary>
    /// Optionally builds the <see cref="Scene"/> to activate once the game starts, via
    /// <see cref="Game.GoToScene"/> — ShapeEngine's normal per-frame update/draw hook. If unset,
    /// the game keeps its default <see cref="SceneEmpty"/>.
    /// </summary>
    public Func<Scene>? SceneFactory { get; set; }

    /// <summary>
    /// The running game instance, once created. Null before attachment and after teardown.
    /// </summary>
    public Game? Game { get; private set; }

    /// <summary>
    /// Target interval between <see cref="Game.Tick"/> calls, in milliseconds. Windows clamps this
    /// up to <see cref="NativeMethods.USER_TIMER_MINIMUM"/> (10ms, i.e. ~100Hz) regardless of what's
    /// requested.
    /// </summary>
    public uint TickIntervalMs { get; set; } = NativeMethods.USER_TIMER_MINIMUM;

    /// <summary>
    /// Content overlaid on top of the embedded raylib surface. Native child windows always
    /// composite above Avalonia-drawn pixels in the same screen area, so this is rendered via a
    /// separate transparent floating <see cref="Window"/> positioned over this control's bounds —
    /// the same approach LibVLCSharp's VideoView uses.
    /// </summary>
    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    private TopLevel? _topLevel;
    private IntPtr _raylibHwnd;
    private NativeMethods.TimerProc? _timerProc;
    private UIntPtr _timerId;
    private bool _closed;

    private Window? _floatingContent;
    private IDisposable? _contentChangedHandler;
    private IDisposable? _isVisibleChangedHandler;
    private IDisposable? _floatingContentChangedHandler;

    public ShapeEngineView()
    {
        _contentChangedHandler = ContentProperty.Changed.AddClassHandler<ShapeEngineView>((s, _) => s.UpdateOverlayPosition());
        _isVisibleChangedHandler = IsVisibleProperty.Changed.AddClassHandler<ShapeEngineView>((s, _) => s.ShowNativeOverlay(s.IsVisible));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        var parent = this.GetVisualParent();
        if (parent != null) parent.DetachedFromVisualTree += Parent_DetachedFromVisualTree;

        base.OnAttachedToVisualTree(e);

        InitializeNativeOverlay();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        var parent = this.GetVisualParent();
        if (parent != null) parent.DetachedFromVisualTree -= Parent_DetachedFromVisualTree;

        base.OnDetachedFromVisualTree(e);

        ShowNativeOverlay(false);
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException($"{nameof(ShapeEngineView)} currently only supports Windows.");
        }

        var hostHandle = base.CreateNativeControlCore(parent);
        SizeChanged += OnControlSizeChanged;

        // Create the raylib window fully hidden at the OS level (not just transparent) so there
        // is no window for the OS to briefly composite before it gets reparented and shown as an
        // embedded child. SetConfigFlags is additive, so this combines with whatever flags
        // GameWindow's own constructor sets when InitWindow runs.
        Raylib.SetConfigFlags(ConfigFlags.HiddenWindow);

        Game = (GameFactory ?? throw new InvalidOperationException($"{nameof(GameFactory)} must be set before {nameof(ShapeEngineView)} is attached."))
            .Invoke();
        Game.StartGameloop();

        if (SceneFactory != null)
        {
            Game.GoToScene(SceneFactory());
        }

        unsafe
        {
            _raylibHwnd = (IntPtr)Raylib.GetWindowHandle();
        }

        _topLevel = TopLevel.GetTopLevel(this)
            ?? throw new InvalidOperationException($"{nameof(ShapeEngineView)} must be attached to a TopLevel.");

        ReparentRaylibWindow(hostHandle.Handle);

        // Keep a strong reference to the delegate: SetTimer only holds a raw function pointer, so
        // an unheld delegate would be free to be GC'd out from under the native callback.
        _timerProc = OnTimerTick;
        _timerId = NativeMethods.SetTimer(IntPtr.Zero, UIntPtr.Zero, TickIntervalMs, _timerProc);

        return hostHandle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        _contentChangedHandler?.Dispose();
        _isVisibleChangedHandler?.Dispose();
        _floatingContentChangedHandler?.Dispose();

        if (_floatingContent != null)
        {
            _floatingContent.PointerEntered -= FloatingContentOnPointerEvent;
            _floatingContent.PointerExited -= FloatingContentOnPointerEvent;
            _floatingContent.PointerPressed -= FloatingContentOnPointerEvent;
            _floatingContent.PointerReleased -= FloatingContentOnPointerEvent;
            _floatingContent.LayoutUpdated -= FloatingContent_LayoutUpdated;
            _floatingContent.Close();
            _floatingContent = null;
        }

        Game?.Quit();
        CloseGame();

        base.DestroyNativeControlCore(control);

        SizeChanged -= OnControlSizeChanged;
        _topLevel = null;
        _raylibHwnd = IntPtr.Zero;
        Game = null;
    }

    private void OnControlSizeChanged(object? sender, SizeChangedEventArgs e) => ResizeRaylibChild();

    private void OnTimerTick(IntPtr hWnd, uint uMsg, UIntPtr idEvent, uint dwTime) => OnFrame();

    private void OnFrame()
    {
        if (Game is null || _closed) return;

        Game.Tick();

        if (Game.IsQuitRequested)
        {
            CloseGame();
        }
    }

    private void CloseGame()
    {
        if (_closed) return;
        _closed = true;

        if (_timerId != UIntPtr.Zero)
        {
            NativeMethods.KillTimer(IntPtr.Zero, _timerId);
            _timerId = UIntPtr.Zero;
        }
        _timerProc = null;

        Game?.EndGameloop();
        Raylib.CloseWindow();
    }

    private void ReparentRaylibWindow(IntPtr hostHandle)
    {
        long style = NativeMethods.GetWindowLongPtr(_raylibHwnd, NativeMethods.GWL_STYLE);
        style &= ~(NativeMethods.WS_POPUP | NativeMethods.WS_CAPTION | NativeMethods.WS_THICKFRAME
            | NativeMethods.WS_SYSMENU | NativeMethods.WS_MINIMIZEBOX | NativeMethods.WS_MAXIMIZEBOX);
        style |= NativeMethods.WS_CHILD;
        NativeMethods.SetWindowLongPtr(_raylibHwnd, NativeMethods.GWL_STYLE, style);

        NativeMethods.SetParent(_raylibHwnd, hostHandle);

        // The window was created with ConfigFlags.HiddenWindow (never mapped by the OS as a
        // top-level window); unhide it now that it's reparented as an embedded child. The
        // following ResizeRaylibChild() call shows the window via SWP_SHOWWINDOW.
        Raylib.ClearWindowState(ConfigFlags.HiddenWindow);
        ResizeRaylibChild();
    }

    private void ResizeRaylibChild()
    {
        if (_raylibHwnd == IntPtr.Zero) return;

        var scaling = _topLevel?.RenderScaling ?? 1.0;
        var width = Math.Max(1, (int)(Bounds.Width * scaling));
        var height = Math.Max(1, (int)(Bounds.Height * scaling));

        NativeMethods.SetWindowPos(_raylibHwnd, IntPtr.Zero, 0, 0, width, height,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    // ---- Content overlay (ported from LibVLCSharp.Avalonia.VideoView) ----

    private void InitializeNativeOverlay()
    {
        if (!this.IsAttachedToVisualTree()) return;

        if (TopLevel.GetTopLevel(this) is not Window visualRoot) return;

        if (_floatingContent == null && Content != null)
        {
            var overlayContent = new ContentControl();
            _floatingContentChangedHandler = overlayContent.Bind(ContentControl.ContentProperty, this.GetObservable(ContentProperty));

            // If the game renders at a fixed virtual resolution, size the overlay content to that
            // same resolution and scale it uniformly via a Viewbox. ScreenTexture.GetDestinationRect
            // letterboxes/pillarboxes the raylib surface the same way, so both stay pixel-aligned to
            // the same visual bounds no matter what size or aspect ratio the window is resized to.
            Control rootContent = overlayContent;
            if (Game?.GameTexture.FixedDimensions is { } fixedDimensions && fixedDimensions.IsValid())
            {
                overlayContent.Width = fixedDimensions.Width;
                overlayContent.Height = fixedDimensions.Height;
                rootContent = new Viewbox { Stretch = Stretch.Uniform, Child = overlayContent };
            }

            _floatingContent = new Window
            {
                TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
                Background = Brushes.Transparent,
                SizeToContent = SizeToContent.WidthAndHeight,
                CanResize = false,
                ShowInTaskbar = false,
                WindowDecorations = WindowDecorations.None,
                ZIndex = int.MaxValue,
                Opacity = 1.0,
                DataContext = DataContext,
                Content = rootContent
            };
            _floatingContent.PointerEntered += FloatingContentOnPointerEvent;
            _floatingContent.PointerExited += FloatingContentOnPointerEvent;
            _floatingContent.PointerPressed += FloatingContentOnPointerEvent;
            _floatingContent.PointerReleased += FloatingContentOnPointerEvent;

            // SizeToContent means _floatingContent.Bounds is (0,0) until its own first layout
            // pass measures its content, so the very first UpdateOverlayPosition() call below
            // (and the one from ShowNativeOverlay()) centers/aligns against a stale zero size.
            // Re-run once its own layout settles so alignment uses the real content size.
            _floatingContent.LayoutUpdated += FloatingContent_LayoutUpdated;

            visualRoot.LayoutUpdated += VisualRoot_UpdateOverlayPosition;
            visualRoot.PositionChanged += VisualRoot_UpdateOverlayPosition;
        }

        ShowNativeOverlay(IsEffectivelyVisible);
    }

    private void VisualRoot_UpdateOverlayPosition(object? sender, EventArgs e) => UpdateOverlayPosition();

    private void FloatingContent_LayoutUpdated(object? sender, EventArgs e) => UpdateOverlayPosition();

    private void FloatingContentOnPointerEvent(object? sender, Avalonia.Input.PointerEventArgs e) => RaiseEvent(e);

    private void Parent_DetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window visualRoot) return;

        visualRoot.LayoutUpdated -= VisualRoot_UpdateOverlayPosition;
        visualRoot.PositionChanged -= VisualRoot_UpdateOverlayPosition;
    }

    private void ShowNativeOverlay(bool show)
    {
        if (_floatingContent == null || _floatingContent.IsVisible == show || TopLevel.GetTopLevel(this) is not Window visualRoot)
            return;

        if (show && this.IsAttachedToVisualTree())
            _floatingContent.Show(visualRoot);
        else
            _floatingContent.Hide();
    }

    private void UpdateOverlayPosition()
    {
        if (_floatingContent == null || !IsVisible) return;

        bool forceSetWidth = false, forceSetHeight = false;
        var topLeft = new Point();
        var child = _floatingContent.Presenter?.Child;

        if (child?.IsArrangeValid == true)
        {
            switch (child.HorizontalAlignment)
            {
                case HorizontalAlignment.Right:
                    topLeft = topLeft.WithX(Bounds.Width - _floatingContent.Bounds.Width);
                    break;
                case HorizontalAlignment.Center:
                    topLeft = topLeft.WithX((Bounds.Width - _floatingContent.Bounds.Width) / 2);
                    break;
                case HorizontalAlignment.Stretch:
                    forceSetWidth = true;
                    break;
                case HorizontalAlignment.Left:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (child.VerticalAlignment)
            {
                case VerticalAlignment.Bottom:
                    topLeft = topLeft.WithY(Bounds.Height - _floatingContent.Bounds.Height);
                    break;
                case VerticalAlignment.Center:
                    topLeft = topLeft.WithY((Bounds.Height - _floatingContent.Bounds.Height) / 2);
                    break;
                case VerticalAlignment.Stretch:
                    forceSetHeight = true;
                    break;
                case VerticalAlignment.Top:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        _floatingContent.SizeToContent = (forceSetWidth, forceSetHeight) switch
        {
            (true, true) => SizeToContent.Manual,
            (false, true) => SizeToContent.Width,
            (true, false) => SizeToContent.Height,
            _ => SizeToContent.Manual
        };

        _floatingContent.Width = forceSetWidth ? Bounds.Width : double.NaN;
        _floatingContent.Height = forceSetHeight ? Bounds.Height : double.NaN;

        _floatingContent.MaxWidth = Bounds.Width;
        _floatingContent.MaxHeight = Bounds.Height;

        if (!this.IsAttachedToVisualTree()) return;

        var newPosition = this.PointToScreen(topLeft);
        if (newPosition != _floatingContent.Position)
        {
            _floatingContent.Position = newPosition;
        }

        var visualRoot = TopLevel.GetTopLevel(this);
        if (_floatingContent.Content is Visual content && visualRoot != null && child != null)
        {
            content.Clip = GetVisibleRegionAsGeometry(visualRoot, this, child.Margin);
        }
    }

    private static RectangleGeometry? GetVisibleRegionAsGeometry(Visual parent, Visual child, Thickness childMargin)
    {
        var childPosition = child.TranslatePoint(new Point(0, 0), parent);
        if (!childPosition.HasValue) return null;

        var topDistance = childPosition.Value.Y + childMargin.Top;
        var leftDistance = childPosition.Value.X + childMargin.Left;
        var bottomDistance = parent.Bounds.Height - (childPosition.Value.Y + child.Bounds.Height + childMargin.Bottom);
        var rightDistance = parent.Bounds.Width - (childPosition.Value.X + child.Bounds.Width + childMargin.Right);

        var region = new Rect(0, 0, child.Bounds.Width, child.Bounds.Height);

        if (topDistance < 0)
        {
            region = new Rect(region.X, region.Y - topDistance, region.Width, region.Height + topDistance);
        }

        if (leftDistance < 0)
        {
            region = new Rect(region.X - leftDistance, region.Y, region.Width + leftDistance, region.Height);
        }

        if (rightDistance < 0)
        {
            region = region.WithWidth(region.Width + rightDistance);
        }

        if (bottomDistance < 0)
        {
            region = region.WithHeight(region.Height + bottomDistance);
        }

        return new RectangleGeometry(region);
    }
}
