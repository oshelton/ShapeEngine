using R3;
using R3.Collections;

namespace ShapeEngine.Timing.R3;

public class ReactiveFrameProvider : FrameProvider
{
    public static readonly ReactiveFrameProvider Update = new();

    readonly object gate = new();
    FreeListCore<IFrameRunnerWorkItem> list;
    long frameCount;
    bool disposed;

    // frame loop is delayed until first register
    bool running;

    private ReactiveFrameProvider()
    {
        list = new FreeListCore<IFrameRunnerWorkItem>(gate);
    }

    public override long GetFrameCount()
    {
        ThrowIfDisposed();
        return frameCount;
    }

    public override void Register(IFrameRunnerWorkItem callback)
    {
        ThrowIfDisposed();
        lock (gate)
        {
            running = true;
            list.Add(callback, out _);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            list.Dispose();
        }
    }

    public void Tick()
    {
        if (!running) return;

        frameCount++;

        var span = list.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            ref readonly var item = ref span[i];
            if (item != null)
            {
                try
                {
                    if (!item.MoveNext(frameCount))
                    {
                        list.Remove(i);
                    }
                }
                catch (Exception ex)
                {
                    list.Remove(i);
                    try
                    {
                        ObservableSystem.GetUnhandledExceptionHandler().Invoke(ex);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
    }

    void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(typeof(ReactiveFrameProvider).FullName);
        }
    }
}
