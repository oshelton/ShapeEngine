using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using R3;
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
        KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.Escape), Command = new ReactiveCommand(_ => Close()) });
        KeyBindings.Add(new KeyBinding { Gesture = new KeyGesture(Key.Enter, KeyModifiers.Alt), Command = new ReactiveCommand(_ => ToggleFullscreen()) });

        this.Title("Avalonia Shape Engine Integration")
            .Width(1280)
            .Height(720)
            .Content(new ShapeEngineView
            {
                GameFactory = CreateGame,
                SceneFactory = () => new CircleScene(),
                Content =  new DockPanel()
                    .Children(
                        new ScrollViewer()
                            .HorizontalAlignment(HorizontalAlignment.Left)
                            .Content(
                            new StackPanel()
                                .DockPanel_Dock(Dock.Left)
                                .HorizontalAlignment(HorizontalAlignment.Left)
                                .VerticalAlignment(VerticalAlignment.Stretch)
                                .Spacing(12)
                                .Margin(new Thickness(16))
                                .MinWidth(300)
                                .MaxWidth(300)
                                .Children(
                                    new TextBlock()
                                        .Text("Press ESC to exit\nPress Alt + Enter to make fullscreen"),
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
                                        .Height(700)
                                        .With(x =>
                                        {
                                            for (int i = 0; i < 100; i++)
                                            {
                                                x.Items.Add($"Item {i}");
                                            }
                                        }),
                                    new TextBox()
                                )
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

    private PixelPoint _restorePosition;
    private double _restoreWidth;
    private double _restoreHeight;
    private bool _isBorderlessFullscreen;

    // Not WindowState.FullScreen: Avalonia sizes that to the monitor's exact pixel bounds with no
    // way to adjust it. A window (and the raylib child surface inside it) whose size exactly
    // matches the monitor can be granted a direct GPU flip/scanout path that bypasses DWM
    // composition for that whole screen region, hiding every other window there regardless of
    // correct Win32 z-order — sizing manually lets us fall 1px short below to avoid that.
    private void ToggleFullscreen()
    {
        if (_isBorderlessFullscreen)
        {
            WindowDecorations = WindowDecorations.Full;
            Width = _restoreWidth;
            Height = _restoreHeight;
            Position = _restorePosition;
            _isBorderlessFullscreen = false;
        }
        else
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen is null) return;

            _restorePosition = Position;
            _restoreWidth = Width;
            _restoreHeight = Height;

            WindowDecorations = WindowDecorations.None;
            Position = screen.Bounds.Position;
            Width = screen.Bounds.Width / screen.Scaling;
            Height = screen.Bounds.Height / screen.Scaling - 1; // 1px short avoids the direct-flip path noted above
            _isBorderlessFullscreen = true;
        }
    }
}
