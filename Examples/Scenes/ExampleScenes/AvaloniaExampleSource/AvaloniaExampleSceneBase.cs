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
/// Handles Avalonia setup, the moving background the surfaces are composited over, and registering and
/// tearing down surfaces. Subclasses only describe which surfaces they want.
/// <para>
/// The bouncing circles are not decoration: they show that raylib keeps rendering correctly after Skia
/// has had the OpenGL context, and they show through the panels' translucent backgrounds.
/// </para>
/// </remarks>
public abstract class AvaloniaExampleSceneBase : ExampleScene
{
    private readonly List<(Vector2 Position, Vector2 Velocity, float Radius, ColorRgba Color)> circles = [];
    private readonly List<AvaloniaSurface> surfaces = [];
    private readonly List<ScreenTexture> placements = [];

    /// <summary>
    /// Creates the scene's surfaces. The base class registers them and their placement textures, and
    /// tears them down again on deactivation.
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
            surfaces.Add(surface);
            Game.Instance.AddCustomEvent(surface);

            if (surface.PlacementTexture is not { } placement) continue;

            placements.Add(placement);
            Game.Instance.AddScreenTexture(placement);
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

        foreach (var placement in placements)
        {
            Game.Instance.RemoveScreenTexture(placement);
            placement.Unload();
        }

        surfaces.Clear();
        placements.Clear();
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
            new Circle(position, radius).Draw(color, 0.8f);
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
