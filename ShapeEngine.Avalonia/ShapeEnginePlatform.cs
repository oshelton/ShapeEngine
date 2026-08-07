using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using ShapeEngine.Avalonia.Gpu;
using ShapeEngine.Avalonia.Input;
using AvCompositor = Avalonia.Rendering.Composition.Compositor;

namespace ShapeEngine.Avalonia;

/// <summary>Wires Avalonia's platform services up to ShapeEngine and raylib.</summary>
internal static class ShapeEnginePlatform
{
    private static RaylibPlatformGraphics? platformGraphics;
    private static ManualRenderTimer? renderTimer;
    private static ShapeEngineDispatcherImpl? dispatcher;
    private static AvCompositor? compositor;

    public static AvCompositor Compositor
        => compositor ?? throw new InvalidOperationException($"{nameof(ShapeEnginePlatform)} hasn't been initialized.");

    public static RaylibPlatformGraphics PlatformGraphics
        => platformGraphics ?? throw new InvalidOperationException($"{nameof(ShapeEnginePlatform)} hasn't been initialized.");

    public static bool IsInitialized => compositor is not null;

    /// <remarks>
    /// Must run on the game loop thread, after the window (and therefore the OpenGL context) exists.
    /// </remarks>
    public static void Initialize()
    {
        // Avalonia installs its synchronization context on this thread, which ShapeEngine leaves unset.
        // Without it, anything routing continuations through the current context fails outright -
        // Animation.RunAsync throws, and awaits inside event handlers resume off the game thread.
        // Work posted to it is drained by PumpDispatcher once per frame.
        AvaloniaSynchronizationContext.AutoInstall = true;

        var graphics = new RaylibPlatformGraphics();
        var timer = new ManualRenderTimer();
        var dispatcherImpl = new ShapeEngineDispatcherImpl(Thread.CurrentThread);

        AvaloniaLocator.CurrentMutable
            .Bind<IClipboard>().ToConstant(new ShapeEngineClipboard())
            .Bind<ICursorFactory>().ToConstant(new ShapeEngineCursorFactory())
            .Bind<IDispatcherImpl>().ToConstant(dispatcherImpl)
            .Bind<IKeyboardDevice>().ToConstant(ShapeEngineDevices.Keyboard)
            .Bind<IPlatformGraphics>().ToConstant(graphics)
            .Bind<IPlatformIconLoader>().ToConstant(new StubPlatformIconLoader())
            .Bind<IPlatformSettings>().ToConstant(new DefaultPlatformSettings())
            .Bind<IRenderTimer>().ToConstant(timer)
            .Bind<IRenderLoop>().ToConstant(RenderLoop.FromTimer(timer))
            .Bind<PlatformHotkeyConfiguration>().ToConstant(
                new PlatformHotkeyConfiguration(commandModifiers: KeyModifiers.Control));

        platformGraphics = graphics;
        renderTimer = timer;
        dispatcher = dispatcherImpl;

        // Commit on the game loop thread: it owns the OpenGL context, and there is no separate
        // Avalonia render thread to hand work to.
        compositor = new AvCompositor(graphics, useUiThreadForSynchronousCommits: true);
    }

    /// <summary>Runs pending dispatcher work. Call once per frame before rendering.</summary>
    public static void PumpDispatcher() => dispatcher?.Pump();

    /// <summary>Drives one Avalonia render pass. Call once per frame.</summary>
    public static void TriggerRenderTick(TimeSpan elapsed) => renderTimer?.TriggerTick(elapsed);
}
