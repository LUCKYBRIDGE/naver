using System.Runtime.InteropServices;

namespace WhaleDualInput.Probe;

internal static class PointerIsolationNative
{
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterPointerInputTarget(nint window, uint type);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool UnregisterPointerInputTarget(nint window, uint type);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool PostMessage(nint window, uint message, nuint wparam, nint lparam);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(nint process, uint access, out nint token);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool GetTokenInformation(nint token, int type, out uint value, uint size, out uint needed);
    [DllImport("kernel32.dll")] private static extern nint GetCurrentProcess();
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    internal static bool HasUiAccess()
    {
        if (!OpenProcessToken(GetCurrentProcess(), 8, out var token)) return false;
        try { return GetTokenInformation(token, 26, out var value, 4, out var needed) && needed == 4 && value != 0; }
        finally { CloseHandle(token); }
    }
}
