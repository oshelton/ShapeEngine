using ShapeEngine.Core;
using ShapeEngine.Core.Structs;
using ShapeEngine.Input;

namespace Examples;
public static class Program
{
    // STAThread is required if you deploy using NativeAOT on Windows - See https://github.com/raylib-cs/raylib-cs/issues/301
    [STAThread]
    public static void Main(string[] args)
    {
        
        var gameSettings = GameSettings.StretchMode("Shape Engine Examples");
        
        var windowSettings = new WindowSettings
        {
            Title = "Shape Engine Examples",
            Topmost = false,
            FullscreenAutoRestoring = true,
            WindowBorder = WindowBorder.Resizabled,
            WindowMinSize = new(1280, 720),
            WindowSize = new(1280, 720),
            Monitor = 0,
            Vsync = VsyncMode.Disabled,
            WindowOpacity = 1f,
            MouseEnabled = true,
            MouseVisible = false,
            Msaa4x = true,
            HighDPI = false,
            FramebufferTransparent = false
        };

        var framerateSettings = FramerateSettings.Default;

        
        var inputSettings = new InputSettings
        (
            new InputSettings.MouseSettings(25, 3, 2, 0.5f, 1f, 0.25f),
            new InputSettings.KeyboardSettings(2, 0.5f, 1f, 2f),
            new InputSettings.GamepadSettings()
        );
        
        GameloopExamples gameloop = new(gameSettings, windowSettings, framerateSettings, inputSettings);
        
        gameloop.Run(args);
    }
}