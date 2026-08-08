using Examples.Scenes.ExampleScenes.AvaloniaExampleSource;
using ShapeEngine.Avalonia;
using ShapeEngine.Core.Structs;
using ShapeEngine.Screen;

namespace Examples.Scenes.ExampleScenes;

/// <summary>
/// A fragment shader post-processing an Avalonia surface.
/// </summary>
/// <remarks>
/// Every <see cref="AvaloniaSurface"/> renders through a <see cref="ScreenTexture"/> that supports
/// shaders, so applying one is just adding a <see cref="ShapeShader"/> to
/// <see cref="AvaloniaSurface.PlacementTexture"/>. The shader runs over finished Avalonia output - text,
/// borders, control chrome and all - rather than anything Avalonia has to cooperate with.
/// <para>
/// Only this surface is affected; the game behind it renders untouched, which is the difference between
/// this and a shader on the game's own screen texture.
/// </para>
/// </remarks>
public class AvaloniaShaderExample : AvaloniaExampleSceneBase
{
    private static readonly AvaloniaSurfaceAnchor Anchor = new(0.36f, 0.86f, 0.04f, 0.55f);

    private AvaloniaSurface? surface;
    private AvaloniaShaderPanel? panel;
    private ShapeShader? shader;
    private float elapsed;

    public AvaloniaShaderExample()
    {
        Title = "Avalonia - Shader";
        Description = "A fragment shader post-processing the Avalonia surface, driven by its own controls";
    }

    protected override IReadOnlyList<AvaloniaSurface> CreateSurfaces()
    {
        panel = new AvaloniaShaderPanel();
        surface = new AvaloniaSurface(panel, Anchor, true);

        elapsed = 0f;
        shader = AvaloniaHologramShader.Load();

        // The surface always creates its texture with shader support, so there is nothing to configure.
        if (shader is not null) surface.PlacementTexture.Shaders?.Add(shader);

        return [surface];
    }

    protected override void OnDeactivate()
    {
        // The surface owns its texture, but the shader is this scene's resource to unload.
        shader?.Unload();
        shader = null;

        base.OnDeactivate();
    }

    protected override void OnSurfacesUpdated(GameTime time)
    {
        if (surface is null || panel is null) return;

        elapsed += time.Delta;

        if (shader is not null)
        {
            shader.Enabled = panel.ShaderEnabled;
            AvaloniaHologramShader.Update(
                shader, elapsed, panel.Strength, surface.PlacementTexture.Width, surface.PlacementTexture.Height);
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
