using System.Numerics;
using ShapeEngine.Avalonia;
using ShapeEngine.Color;
using ShapeEngine.Core;
using ShapeEngine.Core.GameDef;
using ShapeEngine.Core.Structs;
using ShapeEngine.Geometry.CircleDef;
using ShapeEngine.Screen;

namespace Examples.Scenes.ExampleScenes.AvaloniaExampleSource;

/// <summary>
/// Shared plumbing for the Avalonia example scenes.
/// </summary>
/// <remarks>
/// Handles Avalonia setup, surface registration and teardown, and the moving background; subclasses only
/// describe which surfaces they want. The bouncing circles are not decoration - they show that raylib
/// keeps rendering correctly after Skia has had the OpenGL context.
/// </remarks>
public abstract class AvaloniaExampleSceneBase : ExampleScene
{
    private readonly List<(Vector2 Position, Vector2 Velocity, float Radius, ColorRgba Color)> circles = [];
    private readonly List<AvaloniaSurface> surfaces = [];

    /// <summary>
    /// Creates the scene's surfaces. The base class registers them and disposes them on deactivation;
    /// each surface manages its own screen texture.
    /// </summary>
    protected abstract IReadOnlyList<AvaloniaSurface> CreateSurfaces();

    /// <summary>Called once per frame while the scene is active, after the surfaces have updated.</summary>
    protected virtual void OnSurfacesUpdated(GameTime time) { }

    protected override void OnActivate(Scene oldScene)
    {
        base.OnActivate(oldScene);

        AvaloniaHost.EnsureInitialized();

        foreach (var surface in CreateSurfaces())
        {
            // Escape backs out to the main scene everywhere else in the app; without this it would stop
            // working the moment a surface locks the keyboard to a text box. Registering the app's own
            // cancel action - rather than duplicating its binding - is enough: ExampleScene's existing
            // HandleInput() already consumes it every frame, and needs no changes to see it working here.
            surface.OverrideActions.Add(GameloopExamples.Instance.InputActionUICancel);

            surfaces.Add(surface);
            Game.Instance.AddCustomEvent(surface);
        }

        SpawnCircles();
    }

    protected override void OnDeactivate()
    {
        foreach (var surface in surfaces)
        {
            Game.Instance.RemoveCustomEvent(surface);
            surface.Dispose();
        }

        surfaces.Clear();
        circles.Clear();

        base.OnDeactivate();
    }

    protected override void OnUpdateExample(GameTime time, ScreenInfo game, ScreenInfo gameUi, ScreenInfo ui)
    {
        var bounds = game.Area;

        for (var i = 0; i < circles.Count; i++)
        {
            var (position, velocity, radius, color) = circles[i];

            position += velocity * time.Delta;

            if (position.X - radius < bounds.Left || position.X + radius > bounds.Right) velocity.X = -velocity.X;
            if (position.Y - radius < bounds.Top || position.Y + radius > bounds.Bottom) velocity.Y = -velocity.Y;

            position = new Vector2(
                Math.Clamp(position.X, bounds.Left + radius, bounds.Right - radius),
                Math.Clamp(position.Y, bounds.Top + radius, bounds.Bottom - radius));

            circles[i] = (position, velocity, radius, color);
        }

        OnSurfacesUpdated(time);
    }

    protected override void OnDrawGameExample(ScreenInfo game)
    {
        foreach (var (position, _, radius, color) in circles)
        {
            new Circle(position, radius).Draw(color, 0.9f);
        }
    }

    private void SpawnCircles()
    {
        circles.Clear();

        var random = new Random(42);
        var area = Game.Instance.GameScreenInfo.Area;

        for (var i = 0; i < 24; i++)
        {
            var radius = random.Next(14, 46);
            circles.Add((
                new Vector2(
                    area.Left + radius + random.NextSingle() * Math.Max(area.Width - radius * 2f, 1f),
                    area.Top + radius + random.NextSingle() * Math.Max(area.Height - radius * 2f, 1f)),
                new Vector2(random.NextSingle() - 0.5f, random.NextSingle() - 0.5f) * 400f,
                radius,
                new ColorRgba(random.Next(60, 220), random.Next(60, 220), random.Next(120, 255), 200)));
        }
    }
}
