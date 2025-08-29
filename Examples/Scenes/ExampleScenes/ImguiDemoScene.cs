using System.Numerics;
using ImGuiNET;
using R3;
using ShapeEngine.Core;
using ShapeEngine.Core.Structs;
using ShapeEngine.Geometry.CircleDef;
using ShapeEngine.Input;

namespace Examples.Scenes.ExampleScenes;

public class ImguiDemoScene : ExampleScene
{
    private readonly InputAction increaseFontScaleAction;
    private readonly InputAction decreaseFontScaleAction;
    private readonly Circle circle;

    private const float minFontScale = 0.5f;
    private const float maxFontScale = 3.0f;
    private float currentFontScale = 1.0f;
    private readonly string instructions = "";
    private readonly CompositeDisposable subscriptions = new CompositeDisposable();

    public ImguiDemoScene() : base()
    {
        Title = "ImGui Demo Window Scene";
        Description = "A window presenting the ImGui demo window";

        var increaseScaleKb = new InputTypeKeyboardButton(ShapeKeyboardButton.PERIOD);
        increaseFontScaleAction = new(InputSystem.NextAccessTag, new InputActionSettings(), increaseScaleKb);
        increaseFontScaleAction.Title = "Increase Font Scale";

        var decreaseScaleKb = new InputTypeKeyboardButton(ShapeKeyboardButton.COMMA);
        decreaseFontScaleAction = new(InputSystem.NextAccessTag, new InputActionSettings(), decreaseScaleKb);
        decreaseFontScaleAction.Title = "Decrease Font Scale";

        var increaseFontScaleText = increaseFontScaleAction.GetInputTypeDescription(InputDeviceType.Keyboard, false, 1, false);
        var decreaseFontScaleText = decreaseFontScaleAction.GetInputTypeDescription(InputDeviceType.Keyboard, false, 1, false);

        instructions = $"{decreaseFontScaleText} - {increaseFontScaleText} to adjust ImGui font scale";

        float radius = 450 / 2;
        var center = new Vector2(0, -6);
        circle = new(center, radius);
    }

    protected override void OnActivate(Scene oldScene)
    {
        base.OnActivate(oldScene);

        increaseFontScaleAction.WhenPressed()
            .Where(_ => currentFontScale < maxFontScale)
            .Subscribe(_ => currentFontScale += 0.1f)
            .AddTo(subscriptions);

        decreaseFontScaleAction.WhenPressed()
            .Where(_ => currentFontScale > minFontScale)
            .Subscribe(_ => currentFontScale -= 0.1f)
            .AddTo(subscriptions);
    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();

        subscriptions.Clear();
    }

    protected override void OnHandleInputExample(float dt, Vector2 mousePosGame, Vector2 mousePosGameUi, Vector2 mousePosUI)
    {
        increaseFontScaleAction.Update(dt, out _);
        decreaseFontScaleAction.Update(dt, out _);
    }

    protected override void OnDrawGameExample(ScreenInfo game)
    {
        circle.Draw(Colors.PcDark.ColorRgba);
    }

    protected override void OnDrawUIExample(ScreenInfo gameUi)
    {
        // ImGui may only be accessed and used from within OnDrawUI.
        var imguiIo = ImGui.GetIO();
        imguiIo.FontGlobalScale = currentFontScale;

        ImGui.ShowDemoWindow();

         var topCenter = GameloopExamples.Instance.UIRects.GetRect("center").ApplyMargins(0,0,0.01f,0.94f);
        textFont.ColorRgba = Colors.Light;
        var anchorPoint = new AnchorPoint(0.5f, 0f);
        textFont.DrawTextWrapNone(instructions, topCenter, anchorPoint);
    }
}
