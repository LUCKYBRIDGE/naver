namespace WhaleDualInput.Core;

internal enum ProtectionState { OFF, DISCOVERING, PREPARING, ACTIVE, SAFE_HOLD, LOST_PROTECTION, PAUSED_BY_TEACHER, UNSUPPORTED }
internal enum ProtectionCommand { START, STOP, PAUSE, RESUME, RECHECK }
internal enum IsolationEvidence { NONE, OBSERVATION_ONLY, EXPERIMENTAL, VERIFIED }
internal sealed record Operation(string RequestId, long Epoch, ProtectionCommand Command);
internal sealed record ProtectionEvidence(bool IsolationAlive, IsolationEvidence Evidence, bool HardwareVerified,
    bool MappingVerified, bool AdapterReady, bool ChannelReady, long IsolationEpoch);
internal sealed record ProtectionSnapshot(int SchemaVersion, Guid InstanceId, long Revision, long Epoch,
    ProtectionState State, bool StatusKnown, string ReasonCode, bool ReleasePending,
    string? RequestId, IsolationEvidence IsolationEvidence, long? IsolationEpoch);

/// <summary>
/// Owns state only, not input interception. Evidence comes from a trusted isolation backend,
/// never from a UI, stored enum, Raw Input observer, or a successful CDP call.
/// Callers serialize backend operations. Every public method is thread-safe.
/// </summary>
internal sealed class ProtectionStateManager
{
    private readonly object gate = new();
    private readonly Func<long> clock;
    private readonly long evidenceTtlMs, operationTimeoutMs;
    private readonly HashSet<string> seen = new(StringComparer.Ordinal);
    private readonly Queue<string> recent = new();
    private readonly Guid instanceId = Guid.NewGuid();
    private ProtectionState state = ProtectionState.OFF;
    private string reason = "BOOT_OFF";
    private long revision, epoch, evidenceAt, deadline;
    private bool known = true;
    private Operation? pending;
    private ProtectionEvidence? evidence;
    // STOP/PAUSE timeout cannot be superseded by an old START acknowledgement.
    private bool releaseUnconfirmed;

    internal ProtectionStateManager(Func<long> clock, long evidenceTtlMs, long operationTimeoutMs)
    {
        if (evidenceTtlMs <= 0 || operationTimeoutMs <= 0) throw new ArgumentOutOfRangeException(nameof(evidenceTtlMs));
        this.clock = clock; this.evidenceTtlMs = evidenceTtlMs; this.operationTimeoutMs = operationTimeoutMs;
    }
    internal Operation Begin(string requestId, ProtectionCommand command)
    {
        lock (gate)
        {
            Tick();
            if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 100) throw new InvalidOperationException("INVALID_REQUEST_ID");
            if (seen.Contains(requestId)) throw new InvalidOperationException("DUPLICATE_REQUEST");
            if (pending != null && command != ProtectionCommand.STOP) throw new InvalidOperationException("COMMAND_PENDING");
            if (command == ProtectionCommand.PAUSE && state != ProtectionState.ACTIVE) throw new InvalidOperationException("NOT_ACTIVE");
            if (command == ProtectionCommand.RESUME && state != ProtectionState.PAUSED_BY_TEACHER) throw new InvalidOperationException("NOT_PAUSED");
            if (command is ProtectionCommand.START or ProtectionCommand.RESUME &&
                (releaseUnconfirmed || !known || state is ProtectionState.ACTIVE or ProtectionState.SAFE_HOLD))
                throw new InvalidOperationException("RECHECK_OR_STOP_REQUIRED");
            seen.Add(requestId); recent.Enqueue(requestId);
            if (recent.Count > 256) seen.Remove(recent.Dequeue());
            pending = new(requestId, ++epoch, command);
            deadline = checked(clock() + operationTimeoutMs);
            if (command is ProtectionCommand.STOP or ProtectionCommand.PAUSE)
            {
                releaseUnconfirmed = true;
                known = false; reason = "RELEASE_PENDING";
            }
            else
            {
                state = ProtectionState.DISCOVERING; known = false; reason = "CHECK_PENDING";
            }
            revision++; return pending;
        }
    }
    internal bool MappingReady(Operation operation)
    {
        lock (gate)
        {
            Tick();
            if (!Matches(operation) || operation.Command is ProtectionCommand.STOP or ProtectionCommand.PAUSE) return false;
            state = ProtectionState.PREPARING; reason = "ISOLATION_CHECK_PENDING"; revision++; return true;
        }
    }
    internal bool Confirm(Operation operation, ProtectionEvidence observed)
    {
        lock (gate)
        {
            Tick();
            if (!Matches(operation)) return false;
            if (observed.IsolationEpoch < 0) return false;
            if (operation.Command is not (ProtectionCommand.STOP or ProtectionCommand.PAUSE) && state == ProtectionState.DISCOVERING &&
                observed.IsolationAlive && !observed.MappingVerified)
            {
                // A live but unmapped isolation may exist; do not describe this as an ordinary unsupported OFF.
                if (!Verified(observed)) { known = false; reason = "ISOLATION_STATUS_UNVERIFIED"; revision++; return false; }
            }
            if (operation.Command is ProtectionCommand.STOP or ProtectionCommand.PAUSE)
            {
                if (observed.IsolationAlive) { reason = "RELEASE_NOT_CONFIRMED"; revision++; return false; }
                state = operation.Command == ProtectionCommand.PAUSE ? ProtectionState.PAUSED_BY_TEACHER : ProtectionState.OFF;
                releaseUnconfirmed = false;
            }
            else
            {
                if (releaseUnconfirmed && observed.IsolationAlive) { reason = "RELEASE_NOT_CONFIRMED"; revision++; return false; }
                if (!observed.IsolationAlive && operation.Command == ProtectionCommand.RECHECK)
                { state = ProtectionState.OFF; releaseUnconfirmed = false; }
                else if (!Verified(observed)) state = ProtectionState.UNSUPPORTED;
                else if (!observed.MappingVerified || !observed.AdapterReady || !observed.ChannelReady) state = ProtectionState.SAFE_HOLD;
                else state = ProtectionState.ACTIVE;
            }
            evidence = observed; evidenceAt = clock(); known = true;
            reason = state switch
            {
                ProtectionState.ACTIVE => "PROTECTION_CONFIRMED",
                ProtectionState.SAFE_HOLD => "DELIVERY_NOT_READY",
                ProtectionState.UNSUPPORTED => "ISOLATION_UNVERIFIED",
                ProtectionState.PAUSED_BY_TEACHER => "TEACHER_RELEASE_CONFIRMED",
                _ => "RELEASE_CONFIRMED"
            };
            pending = null; revision++; return true;
        }
    }
    internal bool Observe(long operationEpoch, ProtectionEvidence observed)
    {
        lock (gate)
        {
            Tick();
            if (operationEpoch != epoch || pending != null || evidence == null || releaseUnconfirmed ||
                evidence.IsolationEpoch != observed.IsolationEpoch || state is ProtectionState.OFF or ProtectionState.PAUSED_BY_TEACHER or ProtectionState.LOST_PROTECTION)
                return false;
            evidence = observed; evidenceAt = clock(); known = true;
            if (!observed.IsolationAlive) { state = ProtectionState.LOST_PROTECTION; reason = "PROTECTION_LOST"; }
            else if (!Verified(observed)) { state = ProtectionState.UNSUPPORTED; reason = "ISOLATION_UNVERIFIED"; }
            else if (!observed.MappingVerified || !observed.AdapterReady || !observed.ChannelReady) { state = ProtectionState.SAFE_HOLD; reason = "DELIVERY_NOT_READY"; }
            else { state = ProtectionState.ACTIVE; reason = "PROTECTION_CONFIRMED"; }
            revision++; return true;
        }
    }
    internal void ConnectionUnknown()
    {
        lock (gate)
        {
            known = false; reason = "BRIDGE_DISCONNECTED";
            pending = null; epoch++; revision++;
        }
    }
    internal ProtectionSnapshot Snapshot()
    {
        lock (gate)
        {
            Tick();
            // Unknown never exposes an old ACTIVE/SAFE_HOLD as a present confirmed state.
            var shown = known ? state : state is ProtectionState.ACTIVE or ProtectionState.SAFE_HOLD ? ProtectionState.PREPARING : state;
            return new(1, instanceId, revision, epoch, shown, known, reason, releaseUnconfirmed,
                pending?.RequestId, known ? evidence?.Evidence ?? IsolationEvidence.NONE : IsolationEvidence.NONE,
                evidence?.IsolationEpoch);
        }
    }
    private static bool Verified(ProtectionEvidence value) => value.IsolationAlive && value.Evidence == IsolationEvidence.VERIFIED && value.HardwareVerified;
    private bool Matches(Operation operation) => pending == operation;
    private void Tick()
    {
        var now = clock();
        if (pending != null && now >= deadline)
        {
            pending = null; known = false; reason = releaseUnconfirmed ? "RELEASE_TIMEOUT" : "COMMAND_TIMEOUT";
            epoch++; revision++;
        }
        if (known && state is ProtectionState.ACTIVE or ProtectionState.SAFE_HOLD &&
            (now < evidenceAt || now - evidenceAt >= evidenceTtlMs))
        { known = false; reason = "EVIDENCE_EXPIRED"; revision++; }
    }
}
