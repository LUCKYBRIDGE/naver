using System.Runtime.InteropServices;
using System.Text;

namespace WhaleDualInput;

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct Rect { public int Left, Top, Right, Bottom; public readonly Rectangle Bounds => Rectangle.FromLTRB(Left, Top, Right, Bottom); }
    [StructLayout(LayoutKind.Sequential)] internal struct PointerInfo
    {
        public uint Type, Id, Frame, Flags;
        public nint Device, Target;
        public Point Pixel, Himetric, PixelRaw, HimetricRaw;
        public uint Time, History;
        public int Data;
        public uint Keys;
        public ulong Performance;
        public uint ButtonChange;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct CursorInfo
    {
        public uint Size, Flags;
        public nint Cursor;
        public Point Position;
    }
    internal delegate bool EnumCallback(nint window, nint param);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumCallback callback, nint param);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] internal static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(nint window, out Rect rect);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetWindowText(nint window, StringBuilder text, int length);
    [DllImport("user32.dll")] internal static extern bool GetPointerInfo(uint id, out PointerInfo info);
    [DllImport("user32.dll")] internal static extern nint GetMessageExtraInfo();
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(nint window, int id);
    [DllImport("user32.dll")] internal static extern bool GetCursorInfo(ref CursorInfo info);
    [DllImport("user32.dll")] internal static extern nint WindowFromPoint(Point point);
    [DllImport("user32.dll")] internal static extern nint GetAncestor(nint window, uint flags);

    internal static bool IsTouchMouse => ((ulong)GetMessageExtraInfo().ToInt64() & 0xFFFFFF80) == 0xFF515780;
}
