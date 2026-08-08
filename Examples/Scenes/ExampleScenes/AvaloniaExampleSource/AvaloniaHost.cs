using Avalonia;
using ShapeEngine.Avalonia;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>Shared one-time Avalonia setup for the example scenes.</summary>
/// <remarks>
/// Avalonia can only be configured once per process, and needs the OpenGL context to exist first, so
/// setup is deferred to the first scene that asks for it.
/// </remarks>
public static class AvaloniaHost
{
    private static bool initialized;

    public static void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        AppBuilder.Configure<AvaloniaExampleApp>()
            .UseShapeEngine()
            .WithInterFont()
            .SetupWithoutStarting();
    }
}
