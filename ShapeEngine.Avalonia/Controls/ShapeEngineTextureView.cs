using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Raylib_cs;
using ShapeEngine.Color;
using ShapeEngine.Core.GameDef;
using ShapeEngine.Core.Structs;
using SeRect = ShapeEngine.Geometry.RectDef.Rect;

namespace ShapeEngine.Avalonia.Controls;

/// <summary>
/// An Avalonia control that displays content drawn with ShapeEngine's own drawing functions.
/// </summary>
/// <remarks>
/// The content is rendered into a private raylib render texture during the game's frame, then copied
/// into a bitmap the control draws. Nothing hands the OpenGL context between raylib and Skia, so this
/// costs a texture and a per-frame read back rather than any risk of the two renderers corrupting each
/// other's state.
/// <para>
/// Read back is the expensive part: it stalls the GPU pipeline, and the cost scales with the control's
/// area. Keep the control small, or raise <see cref="RefreshInterval"/> to redraw less often than every
/// frame. For full-size or high-frequency content, drawing with raylib directly is the better tool.
/// </para>
/// <para>
/// The redraw happens in the engine's UI drawing pass. A surface that composites earlier in the frame -
/// one placed by a screen texture in <see cref="AvaloniaSurfaceScaling.MatchTexture"/> - therefore shows
/// what was drawn last frame, so expect up to one frame of latency.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// new ShapeEngineTextureView
/// {
///     Width = 300,
///     Height = 180,
///     DrawContent = bounds => new Circle(bounds.Center, 40f).Draw(ColorRgba.White)
/// }
/// </code>
/// </example>
public sealed class ShapeEngineTextureView : Control
{
    private readonly FramePump pump;

    private RenderTexture2D renderTexture;
    private WriteableBitmap? bitmap;
    private PixelSize textureSize;
    private double refreshTimer;
    private bool hasTexture;

    public ShapeEngineTextureView() => pump = new FramePump(this);

    /// <summary>
    /// Draws the content, in texture pixel coordinates. Called during the game's frame, so ShapeEngine's
    /// drawing functions can be used directly.
    /// </summary>
    public Action<SeRect>? DrawContent { get; set; }

    /// <summary>The colour the texture is cleared to before each draw. Transparent by default.</summary>
    public ColorRgba ClearColor { get; set; } = ColorRgba.Transparent;

    /// <summary>
    /// Minimum seconds between redraws. Zero redraws every frame; raise it to trade freshness for the
    /// cost of the read back.
    /// </summary>
    public double RefreshInterval { get; set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Game.Instance.AddCustomEvent(pump);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Game.Instance.RemoveCustomEvent(pump);
        Release();

        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        if (bitmap is not null) context.DrawImage(bitmap, new Rect(Bounds.Size));
    }

    /// <summary>Redraws the texture and copies it into the bitmap.</summary>
    private void RenderFrame(float deltaTime)
    {
        if (DrawContent is not { } drawContent) return;

        if (RefreshInterval > 0.0)
        {
            refreshTimer -= deltaTime;
            if (refreshTimer > 0.0) return;

            refreshTimer = RefreshInterval;
        }

        if (!EnsureTexture()) return;

        Raylib.BeginTextureMode(renderTexture);
        Raylib.ClearBackground(ClearColor.ToRayColor());
        drawContent(new SeRect(0, 0, textureSize.Width, textureSize.Height));
        Raylib.EndTextureMode();

        CopyToBitmap();
        InvalidateVisual();
    }

    /// <summary>Creates or resizes the render texture to match the control's size in physical pixels.</summary>
    private bool EnsureTexture()
    {
        var scaling = (VisualRoot as TopLevel)?.RenderScaling ?? 1.0;
        var size = new PixelSize(
            Math.Max((int)Math.Round(Bounds.Width * scaling), 1),
            Math.Max((int)Math.Round(Bounds.Height * scaling), 1));

        if (Bounds.Width <= 0.0 || Bounds.Height <= 0.0) return false;
        if (hasTexture && size == textureSize) return true;

        Release();

        renderTexture = Raylib.LoadRenderTexture(size.Width, size.Height);
        textureSize = size;
        hasTexture = true;

        bitmap = new WriteableBitmap(size, new Vector(96, 96), global::Avalonia.Platform.PixelFormat.Rgba8888, AlphaFormat.Unpremul);
        return true;
    }

    private unsafe void CopyToBitmap()
    {
        if (bitmap is null) return;

        var image = Raylib.LoadImageFromTexture(renderTexture.Texture);

        // Render textures come back bottom-up because that is how OpenGL stores them.
        Raylib.ImageFlipVertical(ref image);

        using (var locked = bitmap.Lock())
        {
            var rowBytes = textureSize.Width * 4;
            var source = (byte*)image.Data;
            var destination = (byte*)locked.Address;

            for (var y = 0; y < textureSize.Height; y++)
            {
                Buffer.MemoryCopy(source + y * rowBytes, destination + y * locked.RowBytes, rowBytes, rowBytes);
            }
        }

        Raylib.UnloadImage(image);
    }

    private void Release()
    {
        if (hasTexture)
        {
            Raylib.UnloadRenderTexture(renderTexture);
            hasTexture = false;
        }

        bitmap?.Dispose();
        bitmap = null;
    }

    /// <summary>Drives <see cref="RenderFrame"/> once per frame from the game loop.</summary>
    /// <remarks>
    /// <c>PreDrawUi</c> is the engine's drawing hook that runs exactly once per frame and is not already
    /// inside a render target, so the view's texture is never bound inside another one.
    /// </remarks>
    private sealed class FramePump : Game.CustomEvent
    {
        private readonly ShapeEngineTextureView view;

        public FramePump(ShapeEngineTextureView view) => this.view = view;

        protected override void PreDrawUi(ScreenInfo info) => view.RenderFrame(Game.Instance.Time.Delta);
    }
}
