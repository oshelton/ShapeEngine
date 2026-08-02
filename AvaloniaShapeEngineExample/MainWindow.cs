using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Raylib_cs;
using ShapeEngine.Core;
using ShapeEngine.Core.GameDef;
using ShapeEngine.Core.Structs;
using ShapeEngine.Input;
using ShapeEngine.Screen;

namespace AvaloniaShapeEngineExample;

public class MainWindow : Window
{
    public MainWindow()
    {
        this.Title("Avalonia Shape Engine Integration")
            .Width(1280)
            .Height(720)
            .Content(new ShapeEngineView
            {
                GameFactory = CreateGame,
                SceneFactory = () => new CircleScene(),
                Content =  new DockPanel()
                    .Children(
                        new StackPanel()
                            .DockPanel_Dock(Dock.Left)
                            .HorizontalAlignment(HorizontalAlignment.Left)
                            .VerticalAlignment(VerticalAlignment.Stretch)
                            .Spacing(12)
                            .Margin(new Thickness(16))
                            .MinWidth(300)
                            .MaxWidth(300)
                            .Children(
                                new Button()
                                    .Content("Button 1")
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .HorizontalContentAlignment(HorizontalAlignment.Center),
                                new ComboBox()
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .HorizontalContentAlignment(HorizontalAlignment.Center)
                                    .With(x =>
                                    {
                                        x.Items.Add("Item 1");
                                        x.Items.Add("Item 2");
                                        x.Items.Add("Item 3");
                                    })
                                    .SelectedIndex(0),
                                new ListBox()
                                    .HorizontalAlignment(HorizontalAlignment.Stretch)
                                    .Height(500)
                                    .With(x =>
                                    {
                                        for (int i = 0; i < 100; i++)
                                        {
                                            x.Items.Add($"Item {i}");
                                        }
                                    })
                            )
                    )
            });
    }

    private static Game CreateGame()
    {
        // Fixed dimensions keep the game's virtual resolution (ScreenInfo.Area used by scenes for
        // drawing) constant regardless of the actual window size — the rendered game texture is
        // scaled/letterboxed to fit the window instead of the game area itself changing.
        var gameSettings = new GameSettings(
            new Dimensions(1280, 720),
            TextureFilter.Bilinear,
            ShaderSupportType.Multi,
            nearestScaling: false,
            applicationName: "Avalonia Shape Engine Integration");

        // Undecorated removes raylib's own window chrome. Combined with ShapeEngineView creating
        // the window via ConfigFlags.HiddenWindow, it's never visible before being reparented.
        var windowSettings = WindowSettings.Default with
        {
            Title = "Avalonia Shape Engine Integration",
            FullscreenAutoRestoring = false,
            WindowBorder = WindowBorder.Undecorated,
            WindowMinSize = new(100, 100),
            WindowSize = new(1280, 720)
        };

        var framerateSettings = FramerateSettings.Default;

        // maxGamepadCount: 0 skips creating any GamepadDevice instances, and disabling the
        // embedded mappings load skips work for gamepads that will never be used (see
        // game.InputEnabled below).
        var inputSettings = new InputSettings
        (
            new InputSettings.MouseSettings(25, 3, 2, 0.5f, 1f, 0.25f),
            new InputSettings.KeyboardSettings(2, 0.5f, 1f, 2f),
            new InputSettings.GamepadSettings(),
            maxGamepadCount: 0,
            loadEmbeddedGamepadMappings: false
        );

        var game = new Game(gameSettings, windowSettings, framerateSettings, inputSettings);

        // The embedded window doesn't receive normal OS focus/input messages, so raylib's
        // keyboard/mouse polling doesn't reflect real input here (verified via
        // IsWindowFocused/IsMouseButtonDown/GetMousePosition). This also skips gamepad polling —
        // InputSystem.Update() interleaves all three device types with no way to disable just one.
        game.InputEnabled = false;

        return game;
    }
}
