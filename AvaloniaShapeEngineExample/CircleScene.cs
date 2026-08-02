using System.Numerics;
using Raylib_cs;
using ShapeEngine.Color;
using ShapeEngine.Core;
using ShapeEngine.Core.GameDef;
using ShapeEngine.Core.Structs;
using ShapeEngine.Geometry.CircleDef;
using ShapeEngine.Geometry.RectDef;
using ShapeEngine.Text;

namespace AvaloniaShapeEngineExample;

/// <summary>
/// Demo scene for the embedded <see cref="ShapeEngineView"/>: five equal circles in a centered
/// vertical column oscillate horizontally on a sine curve, plus an FPS counter in the top-right.
/// Positions are computed once since the game's virtual resolution is fixed (see
/// <c>MainWindow.CreateGame</c>) and never changes at runtime.
/// </summary>
internal sealed class CircleScene : Scene
{
    private const int CircleCount = 5;
    private const float Radius = 50f;

    private const float MovementPeriodSeconds = 3f;

    // Peak-to-peak horizontal travel is 75% of the screen width, so the amplitude (max
    // deviation from the base position in one direction) is half of that.
    private const float MovementAmplitudeFraction = 0.75f / 2f;

    private const float FpsDisplayWidth = 160f;
    private const float FpsDisplayHeight = 40f;
    private const float FpsDisplayMargin = 10f;

    // Game.FramesPerSecond is unsmoothed (1/frameDelta each frame), and we skip the standalone
    // loop's pacing regulation that would otherwise smooth it out, so it's noticeably jumpy here.
    // Smoothed via exponential moving average for display. Lower = smoother, slower to react.
    private const float FpsSmoothing = 0.1f;

    private Vector2[]? basePositions;
    private double totalSeconds;
    private TextFont? fpsFont;
    private float smoothedFps;

    protected override void OnActivate(Scene oldScene) { }
    protected override void OnDeactivate() { }

    protected override void OnUpdate(GameTime time, ScreenInfo game, ScreenInfo gameUi, ScreenInfo ui)
    {
        totalSeconds = time.TotalSeconds;

        smoothedFps = smoothedFps <= 0f
            ? Game.Instance.FramesPerSecond
            : smoothedFps + (Game.Instance.FramesPerSecond - smoothedFps) * FpsSmoothing;
    }

    protected override void OnDrawGame(ScreenInfo game)
    {
        basePositions ??= ComputeBasePositions(game.Area);

        var amplitude = MovementAmplitudeFraction * game.Area.Width;
        var xOffset = MathF.Sin((float)(totalSeconds / MovementPeriodSeconds) * (MathF.PI * 2f)) * amplitude;

        foreach (var pos in basePositions)
        {
            new Circle(new Vector2(pos.X + xOffset, pos.Y), Radius).Draw(ColorRgba.Red, smoothness: 1f);
        }
    }

    protected override void OnDrawGameUI(ScreenInfo gameUi)
    {
        // Raylib's built-in font avoids needing to load a custom font asset just for this demo.
        fpsFont ??= new TextFont(Raylib.GetFontDefault(), fontSpacing: 1f, ColorRgba.White);

        var rect = new Rect(
            gameUi.Area.Right - FpsDisplayWidth - FpsDisplayMargin,
            gameUi.Area.Top + FpsDisplayMargin,
            FpsDisplayWidth,
            FpsDisplayHeight);

        fpsFont.DrawTextWrapNone($"{MathF.Round(smoothedFps)} FPS", rect, AnchorPoint.TopRight);
    }

    private static Vector2[] ComputeBasePositions(Rect area)
    {
        // CircleCount circles create CircleCount + 1 gaps: area-top -> circle 1, between each
        // adjacent pair, and circle N -> area-bottom.
        var gap = (area.Height - CircleCount * (2f * Radius)) / (CircleCount + 1);

        var positions = new Vector2[CircleCount];
        var y = area.Top + gap + Radius;
        for (var i = 0; i < CircleCount; i++)
        {
            positions[i] = new Vector2(area.Center.X, y);
            y += 2f * Radius + gap;
        }

        return positions;
    }
}
