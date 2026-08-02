using System.Runtime.InteropServices;

namespace AvaloniaShapeEngineExample.Win32;

/// <summary>
/// Minimal Win32 interop for reparenting the raylib/GLFW window as a child of an
/// Avalonia-owned native handle. Windows-only.
/// </summary>
internal static class NativeMethods
{
    public const int GWL_STYLE = -16;

    public const long WS_CHILD = 0x40000000L;
    public const long WS_POPUP = 0x80000000L;
    public const long WS_CAPTION = 0x00C00000L;
    public const long WS_THICKFRAME = 0x00040000L;
    public const long WS_SYSMENU = 0x00080000L;
    public const long WS_MINIMIZEBOX = 0x00020000L;
    public const long WS_MAXIMIZEBOX = 0x00010000L;

    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;

    /// <summary>
    /// The minimum interval Windows allows for <see cref="SetTimer"/>; smaller values are silently
    /// clamped up to this by the OS.
    /// </summary>
    public const uint USER_TIMER_MINIMUM = 0x0000000A;

    public delegate void TimerProc(IntPtr hWnd, uint uMsg, UIntPtr idEvent, uint dwTime);

    /// <summary>
    /// Creates a timer that is not associated with a window. When <paramref name="hWnd"/> is
    /// <see cref="IntPtr.Zero"/>, <paramref name="lpTimerFunc"/> is invoked directly by whichever
    /// thread's message loop processes the resulting WM_TIMER message — bypassing any window
    /// procedure (and, for an Avalonia app, its Dispatcher) entirely.
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern UIntPtr SetTimer(IntPtr hWnd, UIntPtr nIDEvent, uint uElapse, TimerProc? lpTimerFunc);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool KillTimer(IntPtr hWnd, UIntPtr uIDEvent);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    public static long GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
        Environment.Is64BitProcess ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);

    public static long SetWindowLongPtr(IntPtr hWnd, int nIndex, long dwNewLong) =>
        Environment.Is64BitProcess ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, (int)dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern long GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern long SetWindowLongPtr64(IntPtr hWnd, int nIndex, long dwNewLong);
}
