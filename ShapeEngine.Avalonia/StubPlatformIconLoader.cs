using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ShapeEngine.Avalonia;

/// <summary>
/// An <see cref="IPlatformIconLoader"/> that keeps icon data around without ever displaying it.
/// </summary>
/// <remarks>
/// Avalonia resolves this service when an application sets a window icon. The Avalonia content here is
/// hosted inside the raylib window, whose icon is set through ShapeEngine, so there is nothing to do.
/// </remarks>
internal sealed class StubPlatformIconLoader : IPlatformIconLoader
{
    public IWindowIconImpl LoadIcon(string fileName)
    {
        using var stream = File.OpenRead(fileName);
        return LoadIcon(stream);
    }

    public IWindowIconImpl LoadIcon(Stream stream)
    {
        var memoryStream = new MemoryStream(stream.CanSeek ? (int)stream.Length : 0);
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0L;
        return new StubWindowIconImpl(memoryStream);
    }

    public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
    {
        var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, PngBitmapEncoderOptions.Default);
        memoryStream.Position = 0L;
        return new StubWindowIconImpl(memoryStream);
    }
}

/// <summary>An icon that is never displayed, but can still be saved back out.</summary>
internal sealed class StubWindowIconImpl : IWindowIconImpl
{
    private readonly MemoryStream data;

    public StubWindowIconImpl(MemoryStream data) => this.data = data;

    public void Save(Stream outputStream)
    {
        data.Position = 0L;
        data.CopyTo(outputStream);
    }
}
