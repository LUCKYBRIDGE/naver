using System.Runtime.InteropServices;
using System.Threading.Channels;
using WhaleDualInput.Core;

namespace WhaleDualInput.Probe;

internal sealed record IsolatedPointer(nint Device, uint Id, uint Type, uint Message, uint Flags, int X, int Y);

/// <summary>
/// Research-only C2 backend. Consumes redirected messages without OS reinjection or web delivery.
/// Successful Windows registration does not certify physical cursor/focus protection.
/// </summary>
internal sealed class PointerIsolationExperiment : IDisposable
{
    private readonly Dictionary<nint, uint> devices;
    private readonly Rectangle student;
    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private readonly Channel<IsolatedPointer> events = Channel.CreateBounded<IsolatedPointer>(new BoundedChannelOptions(256)
        { SingleReader = true, SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
    private long uiBeat = Environment.TickCount64, received;
    private volatile bool stopRequested;
    private nint window;
    private string status = "OFF";
    private readonly ProtectionStateManager protection = new(() => Environment.TickCount64, 2000, 3000);
    internal ProtectionSnapshot Protection => protection.Snapshot();
    internal string Status => Volatile.Read(ref status);
    internal bool Running => thread.IsAlive;
    internal long Received => Interlocked.Read(ref received);
    internal PointerIsolationExperiment(Dictionary<nint, uint> devices, Rectangle student)
    {
        this.devices = new(devices); this.student = student;
        thread = new Thread(Run) { IsBackground = true, Name = "WhaleDualInput.PointerIsolation" };
        thread.SetApartmentState(ApartmentState.STA);
    }
    internal void Start()
    {
        thread.Start();
        if (!ready.Wait(3000)) { Stop(); throw new InvalidOperationException("ISOLATION_START_TIMEOUT"); }
        if (Status != "REGISTERED_EXPERIMENTAL") throw new InvalidOperationException(Status);
    }
    internal void Beat() => Interlocked.Exchange(ref uiBeat, Environment.TickCount64);
    internal IsolatedPointer[] Drain()
    {
        var result = new List<IsolatedPointer>();
        while (result.Count < 256 && events.Reader.TryRead(out var item)) result.Add(item);
        return result.ToArray();
    }
    internal void Stop()
    {
        stopRequested = true;
        var handle = Interlocked.CompareExchange(ref window, 0, 0);
        if (handle != 0) PointerIsolationNative.PostMessage(handle, 0x8001, 0, 0);
        if (thread.IsAlive && !thread.Join(3000)) throw new InvalidOperationException("ISOLATION_STOP_UNCONFIRMED");
    }
    private void Run()
    {
        using var context = new ApplicationContext();
        using var receiver = new Receiver(this, context);
        var hotkey = false;
        PointerRegistration? registration = null;
        var operation = protection.Begin(Guid.NewGuid().ToString(), ProtectionCommand.START);
        try
        {
            receiver.CreateHandle(new CreateParams { Caption = "WhaleDualInput isolation receiver", Parent = new nint(-3), ExStyle = 0x08000000 });
            Interlocked.Exchange(ref window, receiver.Handle);
            registration = new(new RegistrationApi(receiver.Handle));
            if (stopRequested) throw new InvalidOperationException("START_CANCELED");
            hotkey = Native.RegisterHotKey(receiver.Handle, 10, 0x4000, 0x78);
            if (!hotkey) throw new InvalidOperationException("F9_UNAVAILABLE");
            if (!registration.Start(PointerIsolationNative.HasUiAccess(), devices.Count > 0,
                devices.Values.Distinct().Order().ToArray())) throw new InvalidOperationException(registration.Reason);
            protection.MappingReady(operation);
            // An OS registration ACK is not a physical G1 verification or an adapter ACK.
            protection.Confirm(operation, new(true, IsolationEvidence.EXPERIMENTAL, false, true, false, false, operation.Epoch));
            var expires = Environment.TickCount64 + 30000;
            using var timer = new System.Windows.Forms.Timer { Interval = 100 };
            timer.Tick += (_, _) =>
            {
                if (stopRequested || Environment.TickCount64 >= expires || Environment.TickCount64 - Interlocked.Read(ref uiBeat) > 2000)
                    context.ExitThread();
            };
            timer.Start(); Volatile.Write(ref status, "REGISTERED_EXPERIMENTAL"); ready.Set();
            Application.Run(context);
        }
        catch (Exception error) { Volatile.Write(ref status, error.Message); }
        finally
        {
            var stopOperation = protection.Begin(Guid.NewGuid().ToString(), ProtectionCommand.STOP);
            var releaseOk = registration?.Release() ?? true;
            if (hotkey) Native.UnregisterHotKey(receiver.Handle, 10);
            // Destroy the owning window on its own thread; Windows releases its registrations on destruction.
            receiver.DestroyHandle(); Interlocked.Exchange(ref window, 0);
            if (releaseOk) protection.Confirm(stopOperation, new(false, IsolationEvidence.NONE, false, true, false, false, operation.Epoch));
            else protection.ConnectionUnknown();
            if (!releaseOk) Volatile.Write(ref status, "UNREGISTER_FAILED_WINDOW_DESTROYED_RECHECK_REQUIRED");
            else if (Status == "REGISTERED_EXPERIMENTAL") Volatile.Write(ref status, "RELEASE_CONFIRMED");
            events.Writer.TryComplete(); ready.Set();
        }
    }
    private void Fail(string reason, ApplicationContext context)
    {
        stopRequested = true; Volatile.Write(ref status, reason); context.ExitThread();
    }
    private sealed class Receiver : NativeWindow, IDisposable
    {
        private readonly PointerIsolationExperiment owner;
        private readonly ApplicationContext context;
        internal Receiver(PointerIsolationExperiment owner, ApplicationContext context) { this.owner = owner; this.context = context; }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x8001 || message.Msg == 0x312 || message.Msg is 0x7e or 0x219)
            { owner.stopRequested = true; context.ExitThread(); message.Result = 0; return; }
            if (message.Msg is 0x21 or 0x24b) { message.Result = 3; return; }
            if (message.Msg is 0x245 or 0x246 or 0x247 or 0x24c)
            {
                // Read immediately on the owner thread while the pointer message data is still valid.
                var id = (uint)(message.WParam.ToInt64() & 0xffff);
                if (!Native.GetPointerInfo(id, out var info)) owner.Fail("POINTER_READ_FAILED", context);
                else if (!owner.devices.TryGetValue(info.Device, out var type) || type != info.Type)
                    owner.Fail("UNEXPECTED_POINTER_SOURCE", context);
                else if (!owner.student.Contains(info.Pixel.Value)) owner.Fail("POINTER_MAPPING_CHANGED", context);
                else
                {
                    Interlocked.Increment(ref owner.received);
                    if (!owner.events.Writer.TryWrite(new(info.Device, info.Id, info.Type, (uint)message.Msg, info.Flags, info.Pixel.X, info.Pixel.Y)))
                        owner.Fail("ISOLATION_QUEUE_OVERFLOW", context);
                }
                message.Result = 0; return; // Consume; never DefWindowProc, SendInput or touch reinjection.
            }
            if (message.Msg is >= 0x200 and <= 0x20e) { message.Result = 0; return; }
            base.WndProc(ref message);
        }
        public void Dispose() { if (Handle != 0) DestroyHandle(); }
    }
    private sealed class RegistrationApi(nint handle) : IPointerRegistration
    {
        public int LastError { get; private set; }
        public bool Register(uint type)
        { var ok = PointerIsolationNative.RegisterPointerInputTarget(handle, type); LastError = ok ? 0 : Marshal.GetLastWin32Error(); return ok; }
        public bool Unregister(uint type)
        { var ok = PointerIsolationNative.UnregisterPointerInputTarget(handle, type); LastError = ok ? 0 : Marshal.GetLastWin32Error(); return ok; }
    }
    public void Dispose() => Stop();
}
