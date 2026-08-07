using Avalonia.Input;

namespace ShapeEngine.Avalonia.Input;

/// <summary>The Avalonia input devices every raylib-hosted top level reports events against.</summary>
/// <remarks>
/// raylib exposes a single system keyboard and a single system mouse, so one instance of each is
/// shared across all top levels.
/// </remarks>
internal static class ShapeEngineDevices
{
    public static readonly KeyboardDevice Keyboard = new();

    public static readonly MouseDevice Mouse = new(new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true));
}
