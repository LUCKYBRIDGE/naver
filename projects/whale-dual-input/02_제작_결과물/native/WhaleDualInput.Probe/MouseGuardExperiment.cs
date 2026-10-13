using System.Runtime.InteropServices;

namespace WhaleDualInput.Probe;

/// <summary>Bounded user-mode hypothesis, never an isolation certification.</summary>
internal sealed class MouseGuardExperiment : IDisposable
{
    private readonly ObservationLog log;
    private readonly Rectangle teacher, desktop;
    private readonly Thread thread;
    private readonly bool block;
    private readonly ManualResetEventSlim ready = new();
    private uint threadId;
    private long uiBeat = Environment.TickCount64, blocked;
    private volatile bool stopRequested;
    private string state = "STARTING";
    internal string State => Volatile.Read(ref state);
    internal long Blocked => Interlocked.Read(ref blocked);
    internal bool Running => thread.IsAlive;
    internal MouseGuardExperiment(ObservationLog log, Rectangle teacher, Rectangle desktop, bool block)
    {
        this.log = log; this.teacher = teacher; this.desktop = desktop; this.block = block;
        thread = new Thread(Run) { IsBackground = true, Name = "WhaleDualInput.MouseExperiment" };
    }
    internal void Beat() => Interlocked.Exchange(ref uiBeat, Environment.TickCount64);
    internal void Start()
    {
        thread.Start();
        if (!ready.Wait(3000)) { Stop(); throw new InvalidOperationException("MOUSE_THREAD_START_TIMEOUT"); }
        if (State.StartsWith("ERROR_", StringComparison.Ordinal)) throw new InvalidOperationException(State);
        if (State is not ("EXPERIMENTAL_NOT_VERIFIED" or "OBSERVING_NOT_PROTECTED")) throw new InvalidOperationException("MOUSE_THREAD_STOPPED_DURING_START");
    }
    internal void Stop()
    {
        stopRequested = true;
        if (threadId != 0) Native.PostThreadMessage(threadId, 0x12, 0, 0);
        if (thread.IsAlive && !thread.Join(3000)) throw new InvalidOperationException("MOUSE_THREAD_STOP_UNCONFIRMED");
    }
    private void Run()
    {
        nint hook = 0; bool hotkey = false, clipped = false;
        var previous = new Native.Rect(); var ours = Native.Rect.From(teacher);
        var buttons = new MouseButtonLedger();
        Native.MouseCallback callback = (code, message, data) =>
        {
            if (code < 0) return Native.CallNextHookEx(hook, code, message, data);
            var info = Marshal.PtrToStructure<Native.MouseHookInfo>(data);
            var suppress = block && !stopRequested && buttons.ShouldBlock(InputPolicy.Block(teacher, info.Point.Value, info.Extra), (uint)message, info.Data);
            log.Add(suppress ? "MOUSE_BLOCKED_CANDIDATE" : "MOUSE_HOOK_OBSERVED", x: info.Point.X, y: info.Point.Y, detail: (uint)message);
            if (InputPolicy.Promoted(info.Extra)) log.Add("PROMOTED_POINTER_MOUSE", x: info.Point.X, y: info.Point.Y);
            if (suppress) { Interlocked.Increment(ref blocked); return 1; }
            return Native.CallNextHookEx(hook, code, message, data);
        };
        System.Threading.Timer? watchdog = null;
        try
        {
            threadId = Native.GetCurrentThreadId();
            Native.PeekMessage(out _, 0, 0, 0, 0); // Create queue before cross-thread stop/timeout.
            if (stopRequested) return;
            if (block)
            {
                if (!Native.GetCursorPos(out var cursor) || !teacher.Contains(cursor.Value)) throw new InvalidOperationException("CURSOR_MUST_START_ON_TEACHER_DISPLAY");
                if (new[] { 1, 2, 4, 5, 6 }.Any(key => (Native.GetAsyncKeyState(key) & 0x8000) != 0)) throw new InvalidOperationException("RELEASE_MOUSE_BUTTONS_BEFORE_START");
                if (!Native.GetClipCursor(out previous) || previous.Value != desktop) throw new InvalidOperationException("EXISTING_CURSOR_CONFINEMENT");
                hotkey = Native.RegisterHotKey(0, 9, 0x4000, 0x78);
                if (!hotkey) throw new InvalidOperationException("F9_UNAVAILABLE");
            }
            hook = Native.SetWindowsHookEx(14, callback, Native.GetModuleHandle(null), 0);
            if (hook == 0) throw new InvalidOperationException("MOUSE_HOOK_FAILED");
            if (block)
            {
                if (!Native.ClipCursor(ref ours)) throw new InvalidOperationException("CURSOR_CONFINEMENT_FAILED");
                clipped = true;
            }
            var expires = Environment.TickCount64 + (block ? 30000 : 120000);
            watchdog = new System.Threading.Timer(_ =>
            {
                if (Environment.TickCount64 >= expires || Environment.TickCount64 - Interlocked.Read(ref uiBeat) > 2000)
                { stopRequested = true; Native.PostThreadMessage(threadId, 0x12, 0, 0); }
            }, null, 100, 100);
            Volatile.Write(ref state, block ? "EXPERIMENTAL_NOT_VERIFIED" : "OBSERVING_NOT_PROTECTED");
            log.Add(State); ready.Set();
            while (Native.GetMessage(out var message, 0, 0, 0) > 0)
            {
                if (message.Id == 0x312 && message.WParam == 9) { log.Add("F9_STOP"); break; }
                if (stopRequested) break;
            }
        }
        catch (Exception error)
        {
            Volatile.Write(ref state, "ERROR_" + error.Message); log.Add(State);
        }
        finally
        {
            watchdog?.Dispose();
            var cleanupOk = true;
            if (hook != 0 && !Native.UnhookWindowsHookEx(hook)) { log.Add("HOOK_RELEASE_FAILED"); cleanupOk = false; }
            if (clipped)
            {
                // Do not undo a different application's newer confinement.
                if (!Native.GetClipCursor(out var current)) { log.Add("CLIP_READ_FAILED"); cleanupOk = false; }
                else if (current.Value == teacher)
                {
                    if (!Native.ClipCursor(ref previous)) { log.Add("CLIP_RELEASE_FAILED"); cleanupOk = false; }
                }
                else log.Add("CLIP_CHANGED_EXTERNALLY");
            }
            if (hotkey) Native.UnregisterHotKey(0, 9);
            if (!cleanupOk) Volatile.Write(ref state, "ERROR_RELEASE_UNCONFIRMED");
            else if (!State.StartsWith("ERROR_", StringComparison.Ordinal)) Volatile.Write(ref state, "STOPPED");
            log.Add(State); ready.Set(); GC.KeepAlive(callback);
        }
    }
    public void Dispose() => Stop();
}
