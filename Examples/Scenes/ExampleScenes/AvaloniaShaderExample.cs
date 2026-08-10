using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;
using ShapeEngine.Screen;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// Three fragment shaders, each post-processing its own Avalonia surface.
/// </summary>
/// <remarks>
/// Every <see cref="AvaloniaSurface"/> renders through a <see cref="ScreenTexture"/> that supports
/// shaders, so applying one is just adding a <see cref="ShapeShader"/> to
/// <see cref="AvaloniaSurface.PlacementTexture"/>. Each shader runs over finished Avalonia output - text,
/// borders, control chrome and all - rather than anything Avalonia has to cooperate with, and each
/// surface's shader, toggle and strength slider are entirely independent of its neighbours'.
/// <para>
/// Only these three surfaces are affected; the game behind them renders untouched, which is the
/// difference between this and a shader on the game's own screen texture.
/// </para>
/// </remarks>
public class AvaloniaShaderExample : AvaloniaExampleSceneBase
{
    private static readonly (
        string Title,
        string Description,
        AvaloniaSurfaceAnchor Anchor,
        Func<ShapeShader?> Load,
        Action<ShapeShader, float, float, int, int> Update)[] Shaders =
    [
        (
            "Hologram",
            "A travelling wobble, chromatic split and scanlines.",
            AvaloniaExampleLayout.Region(AvaloniaExampleLayout.Inset, AvaloniaExampleLayout.PaddedTop, 0.30f, AvaloniaExampleLayout.PaddedHeight),
            AvaloniaHologramShader.Load,
            AvaloniaHologramShader.Update
        ),
        (
            "CRT",
            "Barrel distortion, a vignette and phosphor scanlines - the corners fall outside the curved glass.",
            AvaloniaExampleLayout.Region(0.35f, AvaloniaExampleLayout.PaddedTop, 0.30f, AvaloniaExampleLayout.PaddedHeight),
            AvaloniaCrtShader.Load,
            AvaloniaCrtShader.Update
        ),
        (
            "Pixelate",
            "Snaps the surface onto a blocky grid - a static effect, so strength is the only thing that moves.",
            AvaloniaExampleLayout.Region(0.67f, AvaloniaExampleLayout.PaddedTop, 0.30f, AvaloniaExampleLayout.PaddedHeight),
            AvaloniaPixelateShader.Load,
            AvaloniaPixelateShader.Update
        )
    ];

    private readonly List<(AvaloniaSurface Surface, AvaloniaShaderPanel Panel, ShapeShader? Shader, Action<ShapeShader, float, float, int, int> Update)> views = [];

    private float elapsed;

    public AvaloniaShaderExample()
    {
        Title = "Avalonia - Shader";
        Description = "Three fragment shaders, each post-processing its own Avalonia surface";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        views.Clear();
        elapsed = 0f;

        var surfaces = new List<AvaloniaSurface>(Shaders.Length);

        foreach (var (title, description, anchor, load, update) in Shaders)
        {
            var panel = new AvaloniaShaderPanel(title, description);
            var surface = new AvaloniaSurface(panel, anchor, true);
            var shader = load();

            // The surface always creates its texture with shader support, so there is nothing to configure.
            if (shader is not null) surface.PlacementTexture.Shaders?.Add(shader);

            views.Add((surface, panel, shader, update));
            surfaces.Add(surface);
        }

        return surfaces;
    }

    protected override void OnDeactivate()
    {
        // Each surface owns its texture, but the shader is this scene's resource to unload.
        foreach (var (_, _, shader, _) in views) shader?.Unload();
        views.Clear();

        base.OnDeactivate();
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        elapsed += time.Delta;

        foreach (var (surface, panel, shader, update) in views)
        {
            if (shader is not null)
            {
                shader.Enabled = panel.ShaderEnabled;
                update(shader, elapsed, panel.Strength, surface.PlacementTexture.Width, surface.PlacementTexture.Height);
            }

            var rect = surface.DestinationRect;
            var state = shader switch
            {
                null => "shader failed to compile",
                { Enabled: false } => "shader off",
                _ => $"shader on at {panel.Strength:0.00}"
            };

            panel.SetStatus(
                $"""
                 {state}
                 Surface {rect.Width:0}x{rect.Height:0}
                 WantsPointer: {surface.WantsPointer}   WantsKeyboard: {surface.WantsKeyboard}
                 """);
        }
    }
}
