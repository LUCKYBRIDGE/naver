using System.Runtime.InteropServices;

namespace WhaleDualInput.Probe;

internal sealed record Display(string Id, bool Primary, Rectangle Bounds, Rectangle Work, uint DpiX, uint DpiY, uint Rotation, uint PhysicalTargetPaths = 1);
internal sealed record Device(string Id, string Kind, string? DisplayId, Rectangle? DisplayBounds, ushort Contacts, uint UsagePage, uint Usage);
internal sealed record Inventory(Display[] Displays, Device[] Devices, string[] Errors, string Decision)
{
    internal Display? Teacher => Displays.SingleOrDefault(d => d.Primary);
    internal Display? Student => Displays.Length == 2 ? Displays.SingleOrDefault(d => !d.Primary) : null;
}

internal static class DisplayDiscovery
{
    internal static Inventory Read(ObservationLog log)
    {
        var errors = new List<string>();
        var monitors = new List<(nint Handle, Native.MonitorInfo Info, uint X, uint Y)>();
        if (!Native.EnumDisplayMonitors(0, 0, (handle, _, _, _) =>
        {
            var info = new Native.MonitorInfo { Size = (uint)Marshal.SizeOf<Native.MonitorInfo>(), Name = "" };
            if (!Native.GetMonitorInfo(handle, ref info)) { errors.Add("MONITOR_INFO_FAILED"); return true; }
            if (Native.GetDpiForMonitor(handle, 0, out var x, out var y) != 0) errors.Add("MONITOR_DPI_FAILED");
            monitors.Add((handle, info, x, y)); return true;
        }, 0)) errors.Add("MONITOR_ENUM_FAILED");
        var paths = ReadPaths(errors);
        var displays = monitors.Select((m, i) => new Display($"display-{i + 1}", (m.Info.Flags & 1) != 0,
            m.Info.Monitor.Value, m.Info.Work.Value, m.X, m.Y, paths.GetValueOrDefault(m.Info.Name).Rotation,
            paths.GetValueOrDefault(m.Info.Name).Targets)).ToArray();
        var devices = new List<Device>();
        uint count = 0;
        if (!Native.GetPointerDevices(ref count, null)) errors.Add("POINTER_ENUM_FAILED");
        else if (count > 256) errors.Add("POINTER_LIMIT");
        else if (count > 0)
        {
            var pointers = new Native.PointerDevice[count];
            if (!Native.GetPointerDevices(ref count, pointers)) errors.Add("POINTER_ENUM_CHANGED");
            else foreach (var pointer in pointers.Take((int)count))
            {
                var index = monitors.FindIndex(m => m.Handle == pointer.Monitor);
                var mapped = Native.GetPointerDeviceRects(pointer.Device, out _, out var rect);
                if (!mapped) errors.Add("POINTER_MAPPING_FAILED");
                // Match both HMONITOR and the reported display rectangle; never guess a mapping.
                var display = index >= 0 && mapped && displays[index].Bounds == rect.Value ? displays[index] : null;
                devices.Add(new(log.Alias(pointer.Device, "pointer"), pointer.Type switch { 1 => "PEN_INTEGRATED", 2 => "PEN_EXTERNAL", 3 => "TOUCH", 4 => "TOUCHPAD", _ => "UNKNOWN_POINTER" },
                    display?.Id, mapped ? rect.Value : null, pointer.Contacts, 0, 0));
            }
        }
        count = 0;
        var listSize = (uint)Marshal.SizeOf<Native.RawDevice>();
        if (Native.GetRawInputDeviceList(null, ref count, listSize) == uint.MaxValue || count > 1024) errors.Add("RAW_ENUM_FAILED");
        else if (count > 0)
        {
            var raws = new Native.RawDevice[count];
            var read = Native.GetRawInputDeviceList(raws, ref count, listSize);
            if (read == uint.MaxValue) errors.Add("RAW_ENUM_CHANGED");
            else foreach (var raw in raws.Take((int)read))
            {
                // RID_DEVICE_INFO is 32 bytes; do not request RIDI_DEVICENAME or a serial number.
                uint size = 32; var buffer = Marshal.AllocHGlobal(32);
                try
                {
                    Marshal.WriteInt32(buffer, 32);
                    uint page = 0, usage = 0;
                    if (Native.GetRawInputDeviceInfo(raw.Handle, 0x2000000b, buffer, ref size) == uint.MaxValue) errors.Add("RAW_INFO_FAILED");
                    else if (raw.Type == 2) { page = (ushort)Marshal.ReadInt16(buffer, 20); usage = (ushort)Marshal.ReadInt16(buffer, 22); }
                    devices.Add(new(log.Alias(raw.Handle, "raw"), raw.Type switch { 0 => "RAW_MOUSE", 1 => "RAW_KEYBOARD_NOT_OBSERVED", 2 => "RAW_HID", _ => "UNKNOWN_RAW" }, null, null, 0, page, usage));
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
        }
        var decision = Decide(displays, devices.ToArray(), errors.ToArray());
        return new(displays, devices.ToArray(), errors.Distinct().ToArray(), decision);
    }

    internal static string Decide(Display[] displays, Device[] devices, string[] errors)
    {
        if (errors.Length > 0) return "DISCOVERY_INCOMPLETE";
        if (displays.Length != 2 || displays.Count(d => d.Primary) != 1) return "EXACTLY_TWO_EXTENDED_DISPLAYS_REQUIRED";
        if (displays[0].Bounds.IntersectsWith(displays[1].Bounds)) return "DISPLAY_OVERLAP";
        var student = displays.Single(d => !d.Primary);
        var pointers = devices.Where(d => d.Kind is "TOUCH" or "PEN_INTEGRATED" or "PEN_EXTERNAL").ToArray();
        if (pointers.Any(p => p.DisplayId != student.Id)) return "OTHER_OR_UNMAPPED_POINTER_DEVICE";
        if (pointers.Length > 0) return "POINTER_MAPPING_OBSERVED_ISOLATION_UNVERIFIED";
        return "NO_MAPPED_POINTER_MOUSE_PATH_UNVERIFIED";
    }

    private static Dictionary<string, (uint Rotation, uint Targets)> ReadPaths(List<string> errors)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (Native.GetDisplayConfigBufferSizes(2, out var count, out var modes) != 0 || count > 128 || modes > 512)
            { errors.Add("DISPLAY_CONFIG_SIZE_FAILED"); break; }
            var data = new Native.DisplayPath[count];
            var buffer = Marshal.AllocHGlobal(checked((int)Math.Max(1, modes) * 64));
            try
            {
                var result = Native.QueryDisplayConfig(2, ref count, data, ref modes, buffer, 0);
                if (result == 122) continue;
                if (result != 0) { errors.Add("DISPLAY_CONFIG_FAILED"); break; }
                var names = new Dictionary<string, (uint Rotation, uint Targets)>(StringComparer.OrdinalIgnoreCase);
                foreach (var path in data.Take((int)count))
                {
                    var name = new Native.SourceName { Type = 1, Size = (uint)Marshal.SizeOf<Native.SourceName>(), Adapter = path.Source.Adapter, Id = path.Source.Id, Name = "" };
                    if (Native.DisplayConfigGetDeviceInfo(ref name) != 0) errors.Add("DISPLAY_SOURCE_FAILED");
                    else if (names.TryGetValue(name.Name, out var existing))
                    {
                        names[name.Name] = (existing.Rotation, existing.Targets + 1);
                        errors.Add("DISPLAY_CLONE_DETECTED");
                    }
                    else names.Add(name.Name, (path.Target.Rotation, 1));
                }
                if (names.Count != 2) errors.Add("TWO_ACTIVE_DISPLAY_PATHS_REQUIRED");
                return names;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        errors.Add("DISPLAY_TOPOLOGY_UNVERIFIED");
        return new(StringComparer.OrdinalIgnoreCase);
    }
}
