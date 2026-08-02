using System.Runtime.Versioning;
using Avalonia;
using R3;

// ShapeEngineView embeds raylib's window via Win32 HWND reparenting, so the whole app is
// Windows-only for now (see ShapeEngineView.cs).
[assembly: SupportedOSPlatform("windows")]

namespace AvaloniaShapeEngineExample;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseR3();
}
