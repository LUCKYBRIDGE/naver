namespace WhaleDualInput.Probe;

internal sealed record SeatEventV1(long Sequence, long MonotonicTimeMs, string SourceDeviceId, uint ContactId,
    string SourceKind, string Phase, int XPhysicalPx, int YPhysicalPx, long IsolationEpoch, long GeometryEpoch,
    string IsolationEvidence = "NONE");

/// <summary>Research channel: state belongs to the student, never changes the Windows cursor.</summary>
internal sealed class StudentSeat
{
    internal const int Capacity = 128, MaxContacts = 32;
    private readonly Queue<SeatEventV1> queue = new();
    private readonly Dictionary<(string, uint), (Point Point, string Kind)> contacts = new();
    private readonly Rectangle bounds;
    private long sequence, epoch = 1;
    internal long GeometryEpoch { get; private set; } = 1;
    internal long Overflows { get; private set; }
    internal int ContactCount => contacts.Count;
    internal StudentSeat(Rectangle bounds) => this.bounds = bounds;
    internal bool Accept(string source, uint contact, string kind, string phase, Point point)
    {
        if (kind is not ("touch" or "pen") || phase is not ("down" or "move" or "up" or "cancel") || source == "none") return false;
        var key = (source, contact);
        if (queue.Count >= Capacity)
        {
            Overflows++; CancelAll(); return false;
        }
        if (!bounds.Contains(point) && phase != "cancel")
        {
            if (contacts.TryGetValue(key, out var last)) { contacts.Remove(key); Emit(source, contact, last.Kind, "cancel", last.Point); }
            return false;
        }
        if (phase == "down")
        {
            if (contacts.ContainsKey(key) || contacts.Count >= MaxContacts) return false;
        }
        else if (!contacts.ContainsKey(key)) return false;
        if (phase is "up" or "cancel") contacts.Remove(key); else contacts[key] = (point, kind);
        Emit(source, contact, kind, phase, point); return true;
    }
    private void Emit(string source, uint contact, string kind, string phase, Point point) =>
        queue.Enqueue(new(++sequence, Environment.TickCount64, source, contact, kind, phase, point.X, point.Y, epoch, GeometryEpoch));
    internal SeatEventV1[] Drain() { var result = queue.ToArray(); queue.Clear(); return result; }
    internal void CancelAll(bool geometryChanged = false)
    {
        queue.Clear();
        foreach (var (key, contact) in contacts) Emit(key.Item1, key.Item2, contact.Kind, "cancel", contact.Point);
        contacts.Clear(); epoch++;
        if (geometryChanged) GeometryEpoch++;
    }
}
