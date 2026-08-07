using Avalonia.OpenGL;
using Raylib_cs;

namespace ShapeEngine.Avalonia.Gpu;

/// <summary>
/// Hands the OpenGL context over to Skia for the duration of a <c>using</c> block and puts raylib's
/// state back afterwards.
/// </summary>
/// <remarks>
/// raylib and Skia both assume they own the GL context. raylib caches a lot of that state in rlgl and
/// only re-applies it when it thinks it changed, so anything Skia touches has to be restored by hand.
/// This is the OpenGL counterpart to the image barriers a Vulkan bridge would need.
/// </remarks>
internal readonly struct RlglStateGuard : IDisposable
{
    private const int GlViewport = 0x0BA2;

    private readonly GlInterface gl;
    private readonly int previousFramebuffer;
    private readonly int[] previousViewport;

    private RlglStateGuard(GlInterface gl, int previousFramebuffer, int[] previousViewport)
    {
        this.gl = gl;
        this.previousFramebuffer = previousFramebuffer;
        this.previousViewport = previousViewport;
    }

    /// <summary>
    /// Flushes raylib's pending geometry and records the state that has to survive the Skia pass.
    /// </summary>
    /// <remarks>
    /// The framebuffer and viewport are read back from OpenGL rather than from rlgl. rlgl's
    /// <c>GetFramebufferWidth</c> keeps reporting the last render texture's size after that texture's
    /// draw pass has ended, so restoring from it shrinks everything drawn afterwards into a corner.
    /// </remarks>
    public static RlglStateGuard Enter(GlInterface gl)
    {
        // Anything still queued in rlgl would otherwise be drawn later with Skia's shader,
        // blend function and framebuffer binding rather than raylib's.
        Rlgl.DrawRenderBatchActive();

        int framebuffer;
        gl.GetIntegerv(GlConsts.GL_FRAMEBUFFER_BINDING, out framebuffer);

        // GL_VIEWPORT writes four integers, so the query needs a buffer with room for all of them.
        var viewport = new int[4];
        gl.GetIntegerv(GlViewport, out viewport[0]);

        return new RlglStateGuard(gl, framebuffer, viewport);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // raylib re-binds its shader, VAO and textures on the next batch draw, so those need no help.
        // Everything below is state raylib either caches or never sets defensively.
        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, previousFramebuffer);
        gl.Viewport(previousViewport[0], previousViewport[1], previousViewport[2], previousViewport[3]);

        Rlgl.DisableScissorTest();
        Rlgl.DisableDepthTest();
        Rlgl.DisableTexture();
        Rlgl.EnableColorBlend();

        // rlSetBlendMode short-circuits when the requested mode equals the cached one. Skia changed
        // the actual glBlendFunc behind rlgl's back, so force a real change to make raylib re-issue it.
        Rlgl.SetBlendMode(BlendMode.Additive);
        Rlgl.SetBlendMode(BlendMode.Alpha);
    }
}
