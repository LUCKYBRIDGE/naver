using System.Runtime.InteropServices;

namespace WhaleDualInput.Probe;

internal sealed class InputObserver : IDisposable
{
    private readonly ObservationLog log;
    private readonly Native.WinEventCallback callback;
    private readonly List<nint> hooks = new();
    private readonly nint window;
    private readonly CancellationTokenSource cancellation = new();
    private Task? sampler;
    internal Point? LastStudentPoint { get; private set; }
    internal string LastStudentSource { get; private set; } = "none";
    internal bool Running { get; private set; }
    internal Inventory Inventory { get; }
    internal StudentSeat Seat { get; }
    internal InputObserver(nint window, ObservationLog log, Inventory inventory)
    {
        this.window = window; this.log = log; Inventory = inventory;
        Seat = new(inventory.Student?.Bounds ?? Rectangle.Empty);
        callback = (_, kind, hwnd, _, _, _, _) => log.Add(kind == 3 ? "FOREGROUND_EVENT" : "FOCUS_EVENT", log.Alias(hwnd, "window"));
    }
    internal void Start()
    {
        try
        {
            var registrations = Registrations(0x2100, window); // INPUTSINK + DEVNOTIFY, no NOLEGACY, no keyboard.
            if (!Native.RegisterRawInputDevices(registrations, (uint)registrations.Length, (uint)Marshal.SizeOf<Native.RawRegistration>()))
                throw new InvalidOperationException("RAW_REGISTRATION_FAILED");
            Running = true;
            foreach (var kind in new uint[] { 3, 0x8005 })
            {
                var hook = Native.SetWinEventHook(kind, kind, 0, callback, 0, 0, 0);
                if (hook == 0) throw new InvalidOperationException("FOCUS_OBSERVER_FAILED");
                hooks.Add(hook);
            }
            sampler = Task.Run(async () =>
            {
                Point? last = null; nint foreground = 0, focus = 0;
                using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(4));
                try
                {
                    while (await timer.WaitForNextTickAsync(cancellation.Token))
                    {
                        if (Native.GetCursorPos(out var cursor))
                        {
                            if (last != cursor.Value) { log.Add("CURSOR", x: cursor.X, y: cursor.Y); last = cursor.Value; }
                        }
                        else log.Add("CURSOR_READ_FAILED");
                        var current = Native.GetForegroundWindow();
                        if (current != foreground) { foreground = current; log.Add("FOREGROUND_SAMPLE", log.Alias(current, "window")); }
                        var thread = Native.GetWindowThreadProcessId(current, out _);
                        var info = new Native.GuiInfo { Size = (uint)Marshal.SizeOf<Native.GuiInfo>() };
                        if (thread != 0 && Native.GetGUIThreadInfo(thread, ref info))
                        {
                            if (focus != info.Focus) { focus = info.Focus; log.Add("FOCUS_SAMPLE", log.Alias(focus, "window")); }
                        }
                        else log.Add("FOCUS_READ_FAILED");
                    }
                }
                catch (OperationCanceledException) { }
            });
            log.Add("OBSERVATION_STARTED");
        }
        catch { Dispose(); throw; }
    }
    private static Native.RawRegistration[] Registrations(uint flags, nint target) =>
    [new() { Page = 1, Usage = 2, Flags = flags, Target = target },
     new() { Page = 13, Usage = 2, Flags = flags, Target = target },
     new() { Page = 13, Usage = 4, Flags = flags, Target = target },
     new() { Page = 13, Usage = 5, Flags = flags, Target = target }];

    internal void ReadRaw(nint input)
    {
        if (!Running) return;
        uint size = 0; var headerSize = (uint)Marshal.SizeOf<Native.RawHeader>();
        if (Native.GetRawInputData(input, 0x10000003, 0, ref size, headerSize) == uint.MaxValue || size < headerSize || size > 65536)
        { log.Add("RAW_SIZE_FAILED"); return; }
        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            var read = Native.GetRawInputData(input, 0x10000003, buffer, ref size, headerSize);
            if (read == uint.MaxValue || read < headerSize) { log.Add("RAW_READ_FAILED"); return; }
            var header = Marshal.PtrToStructure<Native.RawHeader>(buffer);
            if (header.Type == 1) return; // Never parse keyboard data.
            var source = log.Alias(header.Device, "raw");
            if (header.Type != 0) { log.Add("RAW_HID_PACKET", source); return; } // Never export HID payload bytes.
            if (read < headerSize + 24) { log.Add("RAW_TRUNCATED"); return; }
            var mouse = Marshal.PtrToStructure<Native.RawMouse>(buffer + (int)headerSize);
            var absolute = (mouse.Flags & 1) != 0;
            log.Add(absolute ? "RAW_MOUSE_ABSOLUTE" : "RAW_MOUSE_RELATIVE", source, mouse.X, mouse.Y, mouse.Buttons);
            if (absolute)
            {
                var bounds = (mouse.Flags & 2) != 0 ? SystemInformation.VirtualScreen : Inventory.Teacher?.Bounds ?? Rectangle.Empty;
                var point = InputPolicy.Absolute(mouse.X, mouse.Y, bounds);
                if (point is Point p && Inventory.Student?.Bounds.Contains(p) == true)
                {
                    LastStudentPoint = p; LastStudentSource = source;
                    log.Add("STUDENT_CANDIDATE_ABSOLUTE", source, p.X, p.Y, mouse.Buttons);
                }
            }
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }
    internal void Pointer(uint id, uint message)
    {
        if (!Running) return;
        if (!Native.GetPointerInfo(id, out var info)) { log.Add("POINTER_READ_FAILED"); return; }
        var source = log.Alias(info.Device, "pointer");
        log.Add(info.Type switch { 2 => "PT_TOUCH", 3 => "PT_PEN", 4 => "PT_MOUSE", _ => "PT_OTHER" }, source, info.Pixel.X, info.Pixel.Y, message);
        if (Inventory.Student?.Bounds.Contains(info.Pixel.Value) == true)
        { LastStudentPoint = info.Pixel.Value; LastStudentSource = source; }
        // Only the standard mapped pointer is eligible for this research channel.
        // An absolute mouse position alone is not sufficient proof of device ownership.
        if (Inventory.Devices.Any(d => d.Id == source && d.DisplayId == Inventory.Student?.Id) && info.Type is 2 or 3)
        {
            var phase = (info.Flags & 0x8000) != 0 || message == 0x24c ? "cancel" : message == 0x246 ? "down" : message == 0x247 ? "up" : "move";
            if (!Seat.Accept(source, id, info.Type == 2 ? "touch" : "pen", phase, info.Pixel.Value)) log.Add("SEAT_EVENT_REJECTED", source);
        }
    }
    public void Dispose()
    {
        cancellation.Cancel();
        Seat.CancelAll();
        foreach (var hook in hooks) Native.UnhookWinEvent(hook);
        hooks.Clear();
        if (Running)
        {
            var remove = Registrations(1, 0);
            if (!Native.RegisterRawInputDevices(remove, (uint)remove.Length, (uint)Marshal.SizeOf<Native.RawRegistration>())) log.Add("RAW_UNREGISTER_FAILED");
            Running = false; log.Add("OBSERVATION_STOPPED");
        }
        if (sampler != null) { sampler.GetAwaiter().GetResult(); sampler = null; }
    }
}
