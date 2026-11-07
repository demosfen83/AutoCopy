using System.Diagnostics;
using System.Runtime.InteropServices;
namespace AutoCopy;
internal static class Native
{
    internal const int WH_MOUSE_LL = 14, WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202;
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseData { public Point Point; public uint MouseDataValue, Flags, Time; public nuint ExtraInfo; }
    internal delegate nint HookProc(int code, nint message, nint data);
    [DllImport("user32.dll", SetLastError=true)] internal static extern nint SetWindowsHookEx(int id, HookProc proc, nint module, uint threadId);
    [DllImport("user32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook, int code, nint message, nint data);
    [DllImport("user32.dll")] internal static extern nint WindowFromPoint(Point point);
    [DllImport("user32.dll")] internal static extern nint GetAncestor(nint hwnd, uint flags);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    internal static nint RootAt(Point p) => GetAncestor(WindowFromPoint(p), 2);
    internal static string? ProcessPath(nint hwnd)
    {
        try { GetWindowThreadProcessId(hwnd, out var id); using var process = Process.GetProcessById((int)id); return process.MainModule?.FileName; }
        catch { return null; }
    }
}
