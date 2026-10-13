using WhaleDualInput.Core;

namespace WhaleDualInput.Probe;

internal static class ProtectionTests
{
    internal static int Run()
    {
        int count = 0; long now = 0;
        void Check(bool value, string name) { if (!value) throw new InvalidOperationException("FAIL protection: " + name); count++; }
        void Reject(Action action, string code)
        {
            try { action(); throw new InvalidOperationException("Expected " + code); }
            catch (InvalidOperationException error) when (error.Message == code) { count++; }
        }
        ProtectionStateManager Make() => new(() => now, 100, 200);
        var yes = new ProtectionEvidence(true, IsolationEvidence.VERIFIED, true, true, true, true, 1);
        var off = yes with { IsolationAlive = false, Evidence = IsolationEvidence.NONE };
        var manager = Make();
        Check(manager.Snapshot().State == ProtectionState.OFF, "fresh owner boots OFF");
        var start = manager.Begin("start", ProtectionCommand.START);
        Check(manager.Snapshot().State == ProtectionState.DISCOVERING && !manager.Snapshot().StatusKnown, "start is pending not active");
        Reject(() => manager.Begin("start", ProtectionCommand.START), "DUPLICATE_REQUEST");
        Reject(() => manager.Begin("other", ProtectionCommand.START), "COMMAND_PENDING");
        Check(manager.MappingReady(start) && manager.Snapshot().State == ProtectionState.PREPARING, "mapping checkpoint");
        Check(manager.Confirm(start, yes with { Evidence = IsolationEvidence.EXPERIMENTAL }) && manager.Snapshot().State == ProtectionState.UNSUPPORTED, "experiment is not certification");
        var retry = manager.Begin("retry", ProtectionCommand.START);
        Check(!manager.Confirm(start, yes), "late start cannot replace newer operation");
        Check(manager.Confirm(retry, yes with { HardwareVerified = false }) && manager.Snapshot().State == ProtectionState.UNSUPPORTED, "hardware proof required");
        retry = manager.Begin("ready", ProtectionCommand.START);
        Check(manager.Confirm(retry, yes) && manager.Snapshot().State == ProtectionState.ACTIVE, "all proofs permit active");
        Check(manager.Observe(retry.Epoch, yes with { AdapterReady = false }) && manager.Snapshot().State == ProtectionState.SAFE_HOLD, "delivery failure with live isolation");
        Check(!manager.Observe(retry.Epoch, yes with { IsolationEpoch = 2 }), "old isolation cannot impersonate new registration");
        Check(manager.Observe(retry.Epoch, off) && manager.Snapshot().State == ProtectionState.LOST_PROTECTION, "actual loss distinguished from hold");
        Check(!manager.Observe(retry.Epoch, yes), "lost cannot jump back to active");
        retry = manager.Begin("recover", ProtectionCommand.START); manager.Confirm(retry, yes);
        now = 100;
        Check(!manager.Snapshot().StatusKnown && manager.Snapshot().State != ProtectionState.ACTIVE && manager.Snapshot().ReasonCode == "EVIDENCE_EXPIRED", "expired evidence hides old active");
        Reject(() => manager.Begin("guess", ProtectionCommand.START), "RECHECK_OR_STOP_REQUIRED");
        var recheck = manager.Begin("recheck", ProtectionCommand.RECHECK); manager.Confirm(recheck, yes);
        manager.ConnectionUnknown();
        Check(!manager.Snapshot().StatusKnown && manager.Snapshot().State != ProtectionState.SAFE_HOLD, "disconnect does not imply isolation alive");
        Check(!manager.Observe(recheck.Epoch, yes), "old connection epoch rejected");
        recheck = manager.Begin("reconnect", ProtectionCommand.RECHECK); manager.Confirm(recheck, yes);
        var pause = manager.Begin("pause", ProtectionCommand.PAUSE);
        Check(manager.Snapshot().ReleasePending && !manager.Snapshot().StatusKnown, "pause waits for release");
        Check(!manager.Confirm(pause, yes), "live backend cannot ACK release");
        Check(manager.Confirm(pause, off) && manager.Snapshot().State == ProtectionState.PAUSED_BY_TEACHER, "pause after real release");
        var resume = manager.Begin("resume", ProtectionCommand.RESUME);
        Check(manager.Snapshot().State == ProtectionState.DISCOVERING, "resume rechecks mapping");
        manager.Confirm(resume, yes);
        var stop = manager.Begin("stop", ProtectionCommand.STOP);
        now += 200;
        Check(manager.Snapshot().ReleasePending && manager.Snapshot().ReasonCode == "RELEASE_TIMEOUT", "stop timeout remains unconfirmed");
        Check(!manager.Confirm(stop, off), "late release ACK rejected");
        recheck = manager.Begin("release-recheck", ProtectionCommand.RECHECK);
        Check(!manager.Confirm(recheck, yes), "recheck cannot reenable pending release");
        Check(manager.Confirm(recheck, off) && manager.Snapshot().State == ProtectionState.OFF && !manager.Snapshot().ReleasePending, "recheck confirms actual release");
        manager = Make(); start = manager.Begin("start", ProtectionCommand.START);
        stop = manager.Begin("stop", ProtectionCommand.STOP);
        Check(!manager.Confirm(start, yes) && manager.Confirm(stop, off), "stop supersedes starting");
        Check(!manager.MappingReady(start), "stale mapping checkpoint rejected");
        Check(manager.Snapshot().InstanceId != Make().Snapshot().InstanceId, "process-owner identity unique");
        manager = Make(); start = manager.Begin("clock", ProtectionCommand.START); manager.Confirm(start, yes);
        now--;
        Check(!manager.Snapshot().StatusKnown, "clock rollback fails closed for status");
        Reject(() => Make().Begin("", ProtectionCommand.START), "INVALID_REQUEST_ID");
        Console.WriteLine($"Protection state fault tests OK: {count}; test evidence is synthetic, G1/G2 NOT TESTED");
        return count;
    }
}
