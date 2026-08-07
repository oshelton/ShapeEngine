using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;

namespace ShapeEngine.Avalonia.Gpu;

/// <summary>Render target over a <see cref="RaylibGlSurface"/>.</summary>
internal sealed class RaylibGlRenderTarget : IGlPlatformSurfaceRenderTarget
{
    private readonly RaylibGlSurface surface;
    private readonly RaylibGlContext context;

    public RaylibGlRenderTarget(RaylibGlSurface surface, RaylibGlContext context)
    {
        this.surface = surface;
        this.context = context;
    }

    /// <summary>
    /// The surface is recreated rather than resized, so a target outlives its surface only when the
    /// window size or DPI changed. Reporting that as corrupted makes Avalonia ask for a fresh one.
    /// </summary>
    public PlatformRenderTargetState State
        => surface.IsDisposed ? PlatformRenderTargetState.Corrupted : PlatformRenderTargetState.Ready;

    public IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo sceneInfo)
    {
        // Take the context away from raylib for the duration of the session, then bind the surface's
        // framebuffer. Avalonia reads the current binding to build its Skia render target, so this
        // has to happen before the session is handed back.
        var gl = context.GlInterface;
        var stateGuard = RlglStateGuard.Enter(gl);

        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, surface.FramebufferId);
        gl.Viewport(0, 0, surface.Size.Width, surface.Size.Height);

        return new RaylibGlRenderingSession(context, surface, stateGuard);
    }

    public void Dispose() { }
}
