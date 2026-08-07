using Avalonia;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;

namespace ShapeEngine.Avalonia.Gpu;

/// <summary>One Avalonia render pass into a <see cref="RaylibGlSurface"/>.</summary>
internal sealed class RaylibGlRenderingSession : IGlPlatformSurfaceRenderingSession
{
    private readonly RaylibGlSurface surface;
    private readonly RlglStateGuard stateGuard;

    public IGlContext Context { get; }

    public PixelSize Size => surface.Size;

    public double Scaling => surface.RenderScaling;

    /// <summary>
    /// True so the UI lands the right way up once raylib samples the texture.
    /// </summary>
    /// <remarks>
    /// This flag describes the framebuffer, not the desired output: it tells Avalonia the target uses
    /// OpenGL's bottom-left origin, and Avalonia compensates. raylib samples textures top-down, so
    /// leaving it false renders the whole surface upside down.
    /// </remarks>
    public bool IsYFlipped => true;

    public RaylibGlRenderingSession(RaylibGlContext context, RaylibGlSurface surface, RlglStateGuard stateGuard)
    {
        Context = context;
        this.surface = surface;
        this.stateGuard = stateGuard;
    }

    public void Dispose()
    {
        // Make sure everything Avalonia queued has actually reached the texture before raylib samples it.
        Context.GlInterface.Flush();

        stateGuard.Dispose();
    }
}
