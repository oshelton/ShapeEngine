using System.Numerics;
using ImGuiNET;
using R3;
using ShapeEngine.Color;
using ShapeEngine.Core;
using ShapeEngine.Core.Structs;
using ShapeEngine.Geometry.CircleDef;
using ShapeEngine.Input;
using ShapeEngine.Timing.R3;

namespace Examples.Scenes.ExampleScenes;

public class R3DemoScene : ExampleScene
{
    private readonly Circle circle;
    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private GameTime initialTime;
    private GameTime currentGameTime;
    private ColorRgba currentColor = ColorRgba.White;

    public R3DemoScene() : base()
    {
        Title = "R3 Demo Window Scene";
        Description = "A simple example of using R3 with ShapeEngine";

        float radius = 450 / 2;
        var center = new Vector2(0, -6);
        circle = new(center, radius);
    }

    protected override void OnActivate(Scene oldScene)
    {
        base.OnActivate(oldScene);

        initialTime = GameloopExamples.Instance.Time;
        currentGameTime = initialTime;

        disposables.Add(Observable.EveryUpdate()
            .GameTime()
            .Subscribe(x => currentGameTime = x));

        disposables.Add(Observable.Interval(TimeSpan.FromSeconds(5))
            .Subscribe(_ =>
            {
                var newComponents = new Byte[3];
                Random.Shared.NextBytes(newComponents);
                currentColor = new ColorRgba(newComponents[0], newComponents[1], newComponents[2]);
            }));
    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();

        disposables.Clear();
    }

    protected override void OnDrawGameExample(ScreenInfo game)
    {
        circle.Draw(currentColor);
    }

    protected override void OnDrawUIExample(ScreenInfo gameUi)
    {
        var topCenter = GameloopExamples.Instance.UIRects.GetRect("center").ApplyMargins(0,0,0.01f,0.94f);
        textFont.ColorRgba = Colors.Light;
        var anchorPoint = new AnchorPoint(0.5f, 0f);
        textFont.DrawTextWrapNone($"Total runtime in seconds: {(currentGameTime.TotalSeconds - initialTime.TotalSeconds).ToString("###.000")}", topCenter, anchorPoint);
    }
}
