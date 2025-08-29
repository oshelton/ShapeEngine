using System;
using R3;

namespace ShapeEngine.Input;

/// <summary>
/// Extension methods for interacting with InputActions.
/// </summary>
public static class InputActionObservableExtensions
{
    /// <summary>
    /// Creates an Observable for when the action input is pressed.
    /// </summary>
    public static Observable<InputAction> WhenPressed(this Observable<InputAction> input)
        => input.Where(x => x.State.Pressed);

    /// <summary>
    /// Creates an Observable for when the action input is pressed.
    /// </summary>
    public static Observable<InputAction> WhenPressed(this InputAction action)
        => action.WhenInputStateChanges.WhenPressed();

    /// <summary>
    /// Creates an Observable for when the action input is pressed.
    /// </summary>
    public static Observable<InputAction> WhenReleased(this Observable<InputAction> input)
        => input.Where(x => x.State.Released);

    /// <summary>
    /// Creates an Observable for when the action input is pressed.
    /// </summary>
    public static Observable<InputAction> WhenReleased(this InputAction action)
        => action.WhenInputStateChanges.WhenReleased();

    /// <summary>
    /// Creates an Observable for when the action input is down.
    /// </summary>
    public static Observable<InputAction> WhenDown(this Observable<InputAction> input)
        => input.Where(x => x.State.Down);

    /// <summary>
    /// Creates an Observable for when the action input is down.
    /// </summary>
    public static Observable<InputAction> WhenDown(this InputAction action)
        => action.WhenInputStateChanges.WhenDown();

    /// <summary>
    /// Creates an Observable for when the action input is up.
    /// </summary>
    public static Observable<InputAction> WhenUp(this Observable<InputAction> input)
        => input.Where(x => x.State.Up);

    /// <summary>
    /// Creates an Observable for when the action input is up.
    /// </summary>
    public static Observable<InputAction> WhenUp(this InputAction action)
        => action.WhenInputStateChanges.WhenUp();
}
