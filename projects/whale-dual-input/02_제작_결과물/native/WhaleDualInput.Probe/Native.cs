using System.Runtime.InteropServices;

namespace WhaleDualInput.Probe;

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; public readonly System.Drawing.Point Value => new(X, Y); }
    [StructLayout(LayoutKind.Sequential)] internal struct Rect
    {
        public int Left, Top, Right, Bottom;
        public readonly Rectangle Value => Rectangle.FromLTRB(Left, Top, Right, Bottom);
        public static Rect From(Rectangle r) => new() { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom };
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Luid { public uint Low; public int High; }
    [StructLayout(LayoutKind.Sequential)] internal struct Source { public Luid Adapter; public uint Id, Mode, Flags; }
    [StructLayout(LayoutKind.Sequential)] internal struct Target
    {
        public Luid Adapter; public uint Id, Mode, Technology, Rotation, Scaling, RefreshNumerator, RefreshDenominator, Scan;
        public int Available; public uint Flags;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct DisplayPath { public Source Source; public Target Target; public uint Flags; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] internal struct SourceName
    {
        public uint Type, Size; public Luid Adapter; public uint Id;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] internal struct PointerDevice
    {
        public uint Orientation; public nint Device; public uint Type; public nint Monitor;
        public uint Start; public ushort Contacts;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 520)] public string Product;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] internal struct MonitorInfo
    {
        public uint Size; public Rect Monitor, Work; public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string Name;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct RawDevice { public nint Handle; public uint Type; }
    [StructLayout(LayoutKind.Sequential)] internal struct RawRegistration { public ushort Page, Usage; public uint Flags; public nint Target; }
    [StructLayout(LayoutKind.Sequential)] internal struct RawHeader { public uint Type, Size; public nint Device, Param; }
    [StructLayout(LayoutKind.Explicit, Size = 24)] internal struct RawMouse
    {
        [FieldOffset(0)] public ushort Flags;
        [FieldOffset(4)] public ushort Buttons;
        [FieldOffset(6)] public ushort ButtonData;
        [FieldOffset(12)] public int X;
        [FieldOffset(16)] public int Y;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct PointerInfo
    {
        public uint Type, Id, Frame, Flags; public nint Device, Target;
        public Point Pixel, Himetric, PixelRaw, HimetricRaw; public uint Time, History;
        public int Data; public uint Keys; public ulong Performance; public uint ButtonChange;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct GuiInfo
    {
        public uint Size, Flags; public nint Active, Focus, Capture, MenuOwner, MoveSize, Caret; public Rect CaretRect;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseHookInfo
    { public Point Point; public uint Data, Flags, Time; public nuint Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct Message
    { public nint Window; public uint Id; public nuint WParam; public nint LParam; public uint Time; public Point Point; public uint Private; }
    internal delegate bool MonitorCallback(nint monitor, nint dc, nint rect, nint data);
    internal delegate void WinEventCallback(nint hook, uint kind, nint window, int objectId, int child, uint thread, uint time);
    internal delegate nint MouseCallback(int code, nuint message, nint data);
    [DllImport("user32.dll")] internal static extern int GetDisplayConfigBufferSizes(uint flags, out uint paths, out uint modes);
    [DllImport("user32.dll")] internal static extern int QueryDisplayConfig(uint flags, ref uint paths, [Out] DisplayPath[] data, ref uint modes, nint modeData, nint topology);
    [DllImport("user32.dll")] internal static extern int DisplayConfigGetDeviceInfo(ref SourceName name);
    [DllImport("user32.dll")] internal static extern bool EnumDisplayMonitors(nint dc, nint clip, MonitorCallback callback, nint data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("shcore.dll")] internal static extern int GetDpiForMonitor(nint monitor, int type, out uint x, out uint y);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool GetPointerDevices(ref uint count, [Out] PointerDevice[]? devices);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool GetPointerDeviceRects(nint device, out Rect deviceRect, out Rect displayRect);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint GetRawInputDeviceList([Out] RawDevice[]? list, ref uint count, uint size);
    [DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", SetLastError = true)] internal static extern uint GetRawInputDeviceInfo(nint device, uint command, nint data, ref uint size);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterRawInputDevices(RawRegistration[] list, uint count, uint size);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint GetRawInputData(nint input, uint command, nint data, ref uint size, uint headerSize);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool GetPointerInfo(uint id, out PointerInfo info);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] internal static extern bool GetGUIThreadInfo(uint thread, ref GuiInfo info);
    [DllImport("user32.dll")] internal static extern nint SetWinEventHook(uint min, uint max, nint module, WinEventCallback callback, uint process, uint thread, uint flags);
    [DllImport("user32.dll")] internal static extern bool UnhookWinEvent(nint hook);
    [DllImport("user32.dll", SetLastError = true)] internal static extern nint SetWindowsHookEx(int kind, MouseCallback callback, nint module, uint thread);
    [DllImport("user32.dll")] internal static extern nint CallNextHookEx(nint hook, int code, nuint message, nint data);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern nint GetModuleHandle(string? name);
    [DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(nint window, int id);
    [DllImport("user32.dll")] internal static extern int GetMessage(out Message message, nint window, uint min, uint max);
    [DllImport("user32.dll")] internal static extern bool PeekMessage(out Message message, nint window, uint min, uint max, uint flags);
    [DllImport("user32.dll")] internal static extern bool PostThreadMessage(uint thread, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll")] internal static extern bool GetClipCursor(out Rect rect);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool ClipCursor(ref Rect rect);
    [DllImport("user32.dll", EntryPoint = "ClipCursor")] internal static extern bool ReleaseClip(nint rect);
}
