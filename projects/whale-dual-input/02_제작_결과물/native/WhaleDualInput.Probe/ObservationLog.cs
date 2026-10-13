using System.Diagnostics;

namespace WhaleDualInput.Probe;

internal sealed record Observation(double Ms, string Kind, string Source, int X = 0, int Y = 0, uint Detail = 0);
internal sealed class ObservationLog
{
    private readonly object gate = new();
    private readonly Dictionary<(nint, string), string> aliases = new();
    private readonly Queue<Observation> events = new();
    private readonly Dictionary<string, long> counts = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private long dropped;
    internal const int Capacity = 4096;
    internal string Alias(nint handle, string kind)
    {
        if (handle == 0) return "none";
        lock (gate)
        {
            var key = (handle, kind);
            if (aliases.TryGetValue(key, out var id)) return id;
            if (aliases.Count >= 2048) return "alias-limit";
            id = $"{kind}-{aliases.Count + 1}"; aliases.Add(key, id); return id;
        }
    }
    internal void Add(string kind, string source = "none", int x = 0, int y = 0, uint detail = 0)
    {
        lock (gate)
        {
            counts[kind] = counts.GetValueOrDefault(kind) + 1;
            if (events.Count == Capacity) { events.Dequeue(); dropped++; }
            events.Enqueue(new(Math.Round(clock.Elapsed.TotalMilliseconds, 3), kind, source, x, y, detail));
        }
    }
    internal (Observation[] Events, Dictionary<string, long> Counts, long Dropped) Snapshot()
    { lock (gate) return (events.ToArray(), new(counts), dropped); }
}

internal static class InputPolicy
{
    internal static bool CanBlock(Inventory inventory) => inventory.Errors.Length == 0 && inventory.Teacher != null && inventory.Student != null &&
        inventory.Decision is "POINTER_MAPPING_OBSERVED_ISOLATION_UNVERIFIED" or "NO_MAPPED_POINTER_MOUSE_PATH_UNVERIFIED";
    internal static bool StopForTopology(bool blocking, bool baselineArrival) => blocking && !baselineArrival;
    internal static bool Promoted(nuint extra) => ((ulong)extra & 0xffffff00UL) == 0xff515700UL;
    internal static bool Block(Rectangle teacher, Point point, nuint extra) => Promoted(extra) || !teacher.Contains(point);
    internal static Point? Absolute(int x, int y, Rectangle desktop)
    {
        if (x is < 0 or > 65535 || y is < 0 or > 65535 || desktop.Width <= 0 || desktop.Height <= 0) return null;
        return new(desktop.Left + (int)((long)x * desktop.Width / 65536), desktop.Top + (int)((long)y * desktop.Height / 65536));
    }
}

internal sealed class MouseButtonLedger
{
    private uint forwarded;
    internal bool ShouldBlock(bool candidate, uint message, uint data)
    {
        var mask = message switch
        {
            0x201 or 0x202 => 1u, 0x204 or 0x205 => 2u, 0x207 or 0x208 => 4u,
            0x20b or 0x20c => (data >> 16) == 1 ? 8u : 16u, _ => 0u
        };
        var up = message is 0x202 or 0x205 or 0x208 or 0x20c;
        if (up && (forwarded & mask) != 0)
        {
            forwarded &= ~mask; return false; // Do not strand a teacher drag by filtering its release.
        }
        if (!candidate && message is 0x201 or 0x204 or 0x207 or 0x20b) forwarded |= mask;
        return candidate;
    }
}
