using Avalonia;

namespace ShapeEngine.Avalonia;

/// <summary>Extension methods on <see cref="AppBuilder"/> for hosting Avalonia inside ShapeEngine.</summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Configures Avalonia to render through ShapeEngine's raylib window instead of creating its own.
    /// </summary>
    /// <remarks>
    /// Follow this with <see cref="AppBuilder.SetupWithoutStarting"/>, since the game loop drives
    /// Avalonia and Avalonia must not take over the thread. Both go after the ShapeEngine <c>Game</c>
    /// has created its window - the OpenGL context has to exist first.
    /// </remarks>
    /// <example>
    /// <code>
    /// AppBuilder.Configure&lt;MyApp&gt;()
    ///     .UseShapeEngine()
    ///     .SetupWithoutStarting();
    /// </code>
    /// </example>
    public static AppBuilder UseShapeEngine(this AppBuilder builder)
        => builder
            .UseStandardRuntimePlatformSubsystem()
            .UseSkia()
            .UseHarfBuzz()
            .UseWindowingSubsystem(ShapeEnginePlatform.Initialize, "ShapeEngine");
}
