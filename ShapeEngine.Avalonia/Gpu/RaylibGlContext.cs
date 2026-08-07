using Avalonia.OpenGL;

namespace ShapeEngine.Avalonia.Gpu;

/// <summary>
/// Presents the OpenGL context raylib created to Avalonia as an <see cref="IGlContext"/>.
/// </summary>
/// <remarks>
/// There is no context management to do here. raylib makes its context current on the thread running
/// the game loop and keeps it current for the lifetime of the window, and Avalonia is only ever driven
/// from that same thread, so making current and ensuring current are both no-ops.
/// </remarks>
internal sealed class RaylibGlContext : IGlContext
{
    /// <summary>raylib targets OpenGL 3.3 core on desktop platforms.</summary>
    public GlVersion Version { get; } = new(GlProfileType.OpenGL, 3, 3);

    public GlInterface GlInterface { get; }

    /// <summary>No multisampling: the surface is rendered at native resolution and composited 1:1.</summary>
    public int SampleCount => 1;

    /// <summary>
    /// Reported as 8 because <see cref="RaylibGlSurface"/> attaches a packed depth24/stencil8
    /// renderbuffer. Skia needs a stencil buffer for clipping and anti-aliasing.
    /// </summary>
    public int StencilSize => 8;

    public bool CanCreateSharedContext => false;

    public bool IsLost => false;

    public IDisposable MakeCurrent() => EmptyDisposable.Instance;

    public IDisposable EnsureCurrent() => EmptyDisposable.Instance;

    public bool IsSharedWith(IGlContext context) => ReferenceEquals(this, context);

    public IGlContext CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null)
        => throw new NotSupportedException("raylib's OpenGL context cannot be shared.");

    public object? TryGetFeature(Type featureType) => null;

    public RaylibGlContext()
    {
        GlInterface = new GlInterface(Version, GlProcAddress.Get);
    }

    /// <summary>Does nothing: raylib owns the context and destroys it when the window closes.</summary>
    public void Dispose() { }
}
