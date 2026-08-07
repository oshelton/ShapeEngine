namespace ShapeEngine.Avalonia;

/// <summary>
/// How an <see cref="AvaloniaSurface"/> rasterizes its UI when its placement texture is drawn to the
/// screen at a different size than the texture itself.
/// </summary>
/// <remarks>
/// Only meaningful when the surface has a placement <c>ScreenTexture</c>. Without one the surface
/// always covers the window at native resolution and both values behave identically.
/// </remarks>
public enum AvaloniaSurfaceScaling
{
    /// <summary>
    /// Render at the placement texture's resolution and let the texture scale, exactly like the game
    /// texture. Pixel-consistent with the game and letterboxes identically; text softens when
    /// magnified. This is the only mode whose output passes through the texture's shaders.
    /// </summary>
    MatchTexture = 0,

    /// <summary>
    /// Render at the on-screen pixel size and scale Avalonia's layout instead, so text stays crisp at
    /// any window size. Layout is unchanged - the UI still lays out in the texture's coordinate space.
    /// </summary>
    /// <remarks>
    /// The result is drawn straight to the screen rather than through the texture, which has two
    /// consequences: the texture's shaders do not apply, and the UI composites after the game's
    /// <c>DrawUI</c> instead of before it, so it ends up on top.
    /// </remarks>
    NativeDensity = 1
}
