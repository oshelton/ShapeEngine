namespace ShapeEngine.Avalonia;

/// <summary>A shared no-op <see cref="IDisposable"/>, for APIs that require a disposable scope.</summary>
internal sealed class EmptyDisposable : IDisposable
{
    public static readonly EmptyDisposable Instance = new();

    private EmptyDisposable() { }

    public void Dispose() { }
}
