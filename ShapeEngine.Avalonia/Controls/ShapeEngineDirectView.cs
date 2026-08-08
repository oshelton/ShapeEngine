using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Raylib_cs;
using ShapeEngine.Avalonia.Gpu;
using SeRect = ShapeEngine.Geometry.RectDef.Rect;
using SkMatrix = SkiaSharp.SKMatrix;
using SkRect = SkiaSharp.SKRectI;

namespace ShapeEngine.Avalonia.Controls;

/// <summary>
/// Draws ShapeEngine content straight into Avalonia's framebuffer, with no intermediate texture.
/// </summary>
/// <remarks>
/// Where <see cref="ShapeEngineTextureView"/> renders into its own texture and copies the result back
/// through system memory, this hands raylib the framebuffer Avalonia is already rendering into. There is
/// no texture, no read back and no frame of latency, which makes it the better choice for large or
/// full-surface drawing. It works because Avalonia's Skia backend is running on raylib's own OpenGL
/// context, so there is nothing to share between them.
/// <para>
/// The trade is that raylib draws outside Skia's knowledge. The control's transform and clip are
/// honoured - they are read from the canvas and applied to rlgl - but Avalonia's opacity and any render
/// effects are not, because those are Skia compositing steps this drawing bypasses. Content is clipped
/// to the control's bounds, so nothing escapes, but a fading parent will not fade it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// new ShapeEngineDirectView
/// {
///     Height = 190,
///     DrawContent = bounds => new Circle(bounds.Center, 40f).Draw(ColorRgba.White)
/// }
/// </code>
/// </example>
public sealed class ShapeEngineDirectView : Control
{
    /// <summary>
    /// Draws the content, in the control's own coordinate space. Called during Avalonia's render pass,
    /// so ShapeEngine's drawing functions can be used directly.
    /// </summary>
    /// <remarks>
    /// Coordinates run from the origin to the control's width and height. Scaling from a parent
    /// <c>Viewbox</c>, the window's DPI and the control's position on screen are all applied for you.
    /// </remarks>
    public Action<SeRect>? DrawContent { get; set; }

    public override void Render(DrawingContext context)
    {
        if (DrawContent is not { } drawContent) return;
        if (Bounds.Width <= 0.0 || Bounds.Height <= 0.0) return;

        context.Custom(new DrawOperation(new Rect(Bounds.Size), drawContent));
    }

    /// <summary>Hands the OpenGL context to raylib for the duration of Avalonia's render pass.</summary>
    private sealed class DrawOperation : ICustomDrawOperation
    {
        private const int GlStencilTest = 0x0B71;
        private const int GlBlend = 0x0BE2;

        private readonly Action<SeRect> drawContent;

        public DrawOperation(Rect bounds, Action<SeRect> drawContent)
        {
            Bounds = bounds;
            this.drawContent = drawContent;
        }

        public Rect Bounds { get; }

        /// <summary>Hit testable across the control, matching how a plain control behaves.</summary>
        public bool HitTest(Point p) => Bounds.Contains(p);

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature feature) return;

            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;

            var clip = canvas.DeviceClipBounds;
            if (clip.Width <= 0 || clip.Height <= 0) return;

            // Everything Skia has queued has to reach the framebuffer before raylib starts issuing its
            // own calls into the same one.
            canvas.Flush();

            var gl = ShapeEnginePlatform.PlatformGraphics.GetSharedContext().GlInterface;

            using (var guard = RlglStateGuard.Enter(gl))
            {
                PrepareRaylibState(gl);

                // Saved and reassigned rather than pushed and popped. rlgl's matrix stack redirects to
                // its internal transform when pushed in modelview mode, so a push/pop pair does not
                // restore the projection - it leaks, and every later raylib frame is drawn skewed.
                var savedProjection = Rlgl.GetMatrixProjection();
                var savedModelview = Rlgl.GetMatrixModelview();

                // The render target Avalonia hands over is bottom-origin, so Skia's device coordinates
                // and OpenGL's already agree - hence the plain bottom-left projection and the scissor
                // below taking the clip's top edge unflipped. Both matrices are transposed because
                // raylib's Matrix is column-vector while System.Numerics is row-vector, and Raylib-cs
                // copies the memory straight across.
                Rlgl.SetMatrixProjection(Matrix4x4.Transpose(
                    Matrix4x4.CreateOrthographicOffCenter(0f, guard.ViewportWidth, 0f, guard.ViewportHeight, -1f, 1f)));
                Rlgl.SetMatrixModelView(Matrix4x4.Transpose(ToMatrix(canvas.TotalMatrix)));

                SetClip(clip);
                drawContent(new SeRect(0, 0, (float)Bounds.Width, (float)Bounds.Height));

                // Flush while our matrices and scissor are still in force, then hand raylib its own back.
                Rlgl.DrawRenderBatchActive();

                Rlgl.DisableScissorTest();
                Rlgl.SetMatrixProjection(savedProjection);
                Rlgl.SetMatrixModelView(savedModelview);
            }

            // raylib has changed program, buffers and blend state behind Skia's back.
            lease.GrContext.ResetContext();
        }

        /// <summary>
        /// Puts the OpenGL state into the shape raylib assumes before handing it the framebuffer.
        /// </summary>
        /// <remarks>
        /// The stencil test is the one that matters: Skia clips with the stencil buffer and leaves the
        /// test enabled, so raylib's geometry is silently rejected - no error, no output, nothing to
        /// debug from. Depth and face culling are cleared for the same reason, and the blend mode is
        /// toggled to force rlgl to reissue <c>glBlendFunc</c>, which it otherwise skips because its
        /// cached mode still looks correct.
        /// </remarks>
        private static void PrepareRaylibState(GlInterface gl)
        {
            gl.Disable(GlStencilTest);
            gl.Disable(GlConsts.GL_DEPTH_TEST);
            gl.Disable(GlConsts.GL_CULL_FACE);
            gl.Enable(GlBlend);

            Rlgl.SetBlendMode(BlendMode.Additive);
            Rlgl.SetBlendMode(BlendMode.Alpha);
        }

        /// <summary>Converts Skia's 2D canvas transform into the 4x4 rlgl expects.</summary>
        /// <remarks>
        /// This is what places and scales the control: it folds together the control's position, the
        /// window's DPI scale, any parent <c>Viewbox</c> scale and any render transform.
        /// </remarks>
        private static Matrix4x4 ToMatrix(SkMatrix matrix)
            => new(
                matrix.ScaleX, matrix.SkewY, 0f, 0f,
                matrix.SkewX, matrix.ScaleY, 0f, 0f,
                0f, 0f, 1f, 0f,
                matrix.TransX, matrix.TransY, 0f, 1f);

        /// <summary>Turns Avalonia's clip into a GL scissor box.</summary>
        /// <remarks>
        /// Avalonia's clip lives in Skia and does not constrain raylib's calls, so without this the
        /// drawing spills outside the control and over whatever else the surface is showing.
        /// </remarks>
        private static void SetClip(SkRect clip)
        {
            Rlgl.EnableScissorTest();
            Rlgl.Scissor(clip.Left, clip.Top, clip.Width, clip.Height);
        }

        public void Dispose() { }
    }
}
