using Avalonia;
using ShapeEngine.Avalonia;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>Shared one-time Avalonia setup for the example scenes.</summary>
/// <remarks>
/// Avalonia can only be configured once per process, and the game window - and therefore the OpenGL
/// context - has to exist first, so setup is deferred to the first scene that needs it.
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
