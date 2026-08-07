using System.Numerics;
using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Raylib_cs;
using RlMouseButton = Raylib_cs.MouseButton;

namespace ShapeEngine.Avalonia.Input;

/// <summary>Feeds raylib's input state into an Avalonia top level once per frame.</summary>
/// <remarks>
/// Input is read straight from raylib rather than from ShapeEngine's <c>InputSystem</c>. That matters
/// for typed characters: <c>Raylib.GetCharPressed</c> drains a queue, and ShapeEngine's
/// <c>KeyboardDevice</c> drains it too - but only while unlocked. <see cref="AvaloniaSurface"/> locks
/// the ShapeEngine devices whenever the UI has capture, which leaves the queue intact for us and
/// suppresses game input at the same time.
/// </remarks>
internal sealed class AvaloniaInputPump
{
    private readonly ShapeEngineTopLevelImpl impl;

    private Point lastPointerPosition = new(Double.NaN, Double.NaN);
    private bool pointerWasInside;

    public AvaloniaInputPump(ShapeEngineTopLevelImpl impl) => this.impl = impl;

    /// <summary>Translates this frame's raylib input into Avalonia raw input events.</summary>
    /// <param name="pointerPosition">
    /// The cursor in Avalonia's client coordinate space. The caller maps it, because a placed surface
    /// has its own coordinate space that only the placement texture knows about.
    /// </param>
    /// <param name="pointerEnabled">
    /// Whether pointer events should reach Avalonia at all. False while the game has grabbed the mouse.
    /// </param>
    /// <param name="keyboardEnabled">Whether key and text events should reach Avalonia.</param>
    public void Pump(Point pointerPosition, bool pointerEnabled, bool keyboardEnabled)
    {
        var timestamp = (ulong)Environment.TickCount64;
        var modifiers = KeyMap.GetModifiers();

        if (pointerEnabled) PumpPointer(pointerPosition, timestamp, modifiers);
        else if (pointerWasInside)
        {
            pointerWasInside = false;

            // Forget the cached position too, so re-entering the UI sends a fresh move event even if
            // the cursor happens to be exactly where it left.
            lastPointerPosition = new Point(Double.NaN, Double.NaN);
            impl.OnPointerLeft(timestamp);
        }

        if (keyboardEnabled) PumpKeyboard(timestamp, modifiers);
    }

    private void PumpPointer(Point point, ulong timestamp, RawInputModifiers modifiers)
    {
        // Compared in client space rather than window space so a surface that moves under a stationary
        // cursor still reports the move.
        if (point != lastPointerPosition)
        {
            lastPointerPosition = point;
            pointerWasInside = true;
            impl.OnPointerMoved(point, modifiers, timestamp);
        }

        PumpButton(RlMouseButton.Left, RawPointerEventType.LeftButtonDown, RawPointerEventType.LeftButtonUp, point, modifiers, timestamp);
        PumpButton(RlMouseButton.Right, RawPointerEventType.RightButtonDown, RawPointerEventType.RightButtonUp, point, modifiers, timestamp);
        PumpButton(RlMouseButton.Middle, RawPointerEventType.MiddleButtonDown, RawPointerEventType.MiddleButtonUp, point, modifiers, timestamp);
        PumpButton(RlMouseButton.Side, RawPointerEventType.XButton1Down, RawPointerEventType.XButton1Up, point, modifiers, timestamp);
        PumpButton(RlMouseButton.Extra, RawPointerEventType.XButton2Down, RawPointerEventType.XButton2Up, point, modifiers, timestamp);

        var wheel = Raylib.GetMouseWheelMoveV();
        if (wheel != Vector2.Zero)
        {
            impl.OnPointerWheel(point, new global::Avalonia.Vector(wheel.X, wheel.Y), modifiers, timestamp);
        }
    }

    private void PumpButton(
        RlMouseButton button,
        RawPointerEventType downType,
        RawPointerEventType upType,
        Point point,
        RawInputModifiers modifiers,
        ulong timestamp)
    {
        if (Raylib.IsMouseButtonPressed(button)) impl.OnPointerButton(downType, point, modifiers, timestamp);
        if (Raylib.IsMouseButtonReleased(button)) impl.OnPointerButton(upType, point, modifiers, timestamp);
    }

    private void PumpKeyboard(ulong timestamp, RawInputModifiers modifiers)
    {
        foreach (var (raylibKey, key, physicalKey) in KeyMap.Keys)
        {
            // IsKeyPressedRepeat covers held-down auto-repeat, which text editing and list navigation
            // both rely on. It never fires for the initial press, so the two are complementary.
            if (Raylib.IsKeyPressed(raylibKey) || Raylib.IsKeyPressedRepeat(raylibKey))
            {
                impl.OnKey(RawKeyEventType.KeyDown, key, physicalKey, modifiers, null, timestamp);
            }

            if (Raylib.IsKeyReleased(raylibKey))
            {
                impl.OnKey(RawKeyEventType.KeyUp, key, physicalKey, modifiers, null, timestamp);
            }
        }

        var unicode = Raylib.GetCharPressed();
        while (unicode > 0)
        {
            impl.OnTextInput(Char.ConvertFromUtf32(unicode), timestamp);
            unicode = Raylib.GetCharPressed();
        }
    }

}
