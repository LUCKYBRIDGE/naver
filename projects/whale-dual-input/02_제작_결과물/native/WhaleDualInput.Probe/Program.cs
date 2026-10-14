using System.Runtime.InteropServices;

namespace WhaleDualInput.Probe;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        if (args.SequenceEqual(["--self-test"])) return SelfTest.Run();
        if (args.SequenceEqual(["--snapshot"]))
        {
            var log = new ObservationLog(); Console.WriteLine(Report.Serialize(DisplayDiscovery.Read(log), log)); return 0;
        }
        if (args.SequenceEqual(["--isolation-capabilities"]))
        {
            var log = new ObservationLog(); var inventory = DisplayDiscovery.Read(log);
            var uiAccess = PointerIsolationNative.HasUiAccess();
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { uiAccess,
                eligibility = DisplayDiscovery.IsolationEligibility(inventory, uiAccess), inventoryErrors = inventory.Errors,
                hardwareVerified = false, registrationAttempted = false, isolationEvidence = "NONE" }));
            return 0;
        }
        if (args.SequenceEqual(["--smoke-observe"])) return SmokeObserve();
        if (args.Length != 0) { Console.Error.WriteLine("Usage: [--snapshot | --self-test | --smoke-observe | --isolation-capabilities]"); return 2; }
        using var mutex = new Mutex(true, "Local\\WhaleDualInput-Probe", out var created);
        if (!created) { MessageBox.Show("입력 연구 도구가 이미 실행 중입니다."); return 1; }
        Application.Run(new ProbeWindow()); return 0;
    }
    private static int SmokeObserve()
    {
        // Read-only lifecycle smoke: no ClipCursor, blocking hook, surface or generated input.
        var log = new ObservationLog(); var inventory = DisplayDiscovery.Read(log);
        using var window = new Form(); _ = window.Handle;
        using var observer = new InputObserver(window.Handle, log, inventory);
        observer.Start();
        using var mouse = new MouseGuardExperiment(log, inventory.Teacher?.Bounds ?? Rectangle.Empty, SystemInformation.VirtualScreen, false);
        mouse.Start();
        using var timer = new System.Windows.Forms.Timer { Interval = 1200 };
        timer.Tick += (_, _) => { mouse.Stop(); observer.Dispose(); Application.ExitThread(); };
        timer.Start(); Application.Run();
        Console.WriteLine("Read-only observation lifecycle OK; G1/G2 NOT TESTED"); return 0;
    }
}

internal static class SelfTest
{
    internal static int Run()
    {
        var checkedCount = 0;
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("FAIL " + name); checkedCount++; }
        var left = new Rectangle(-1920, 0, 1920, 1080); var right = new Rectangle(0, 0, 1920, 1080);
        Check(!InputPolicy.Block(left, new(-1, 500), 0), "teacher negative coordinates remain usable");
        Check(InputPolicy.Block(left, new(0, 500), 0), "right edge exclusive");
        Check(InputPolicy.Block(right, new(10, 10), (nuint)0xff515780), "touch promotion blocked even after confinement");
        Check(InputPolicy.Block(right, new(10, 10), (nuint)0xff515700), "pen promotion blocked");
        Check(!InputPolicy.Promoted(0xff515600), "unrelated extra data is not promoted pointer");
        var buttons = new MouseButtonLedger();
        Check(!buttons.ShouldBlock(false, 0x201, 0) && !buttons.ShouldBlock(true, 0x202, 0), "forwarded teacher down always gets release");
        Check(buttons.ShouldBlock(true, 0x201, 0) && buttons.ShouldBlock(true, 0x202, 0), "blocked click remains blocked");
        Check(!buttons.ShouldBlock(false, 0x20b, 1u << 16) && !buttons.ShouldBlock(true, 0x20c, 1u << 16), "extra button release preserved");
        Check(InputPolicy.Absolute(0, 0, left) == new Point(-1920, 0), "absolute origin");
        Check(InputPolicy.Absolute(65535, 65535, left) == new Point(-1, 1079), "absolute endpoint inside display");
        Check(InputPolicy.Absolute(65536, 5, right) == null, "invalid absolute rejected");
        Display[] displays = [new("teacher", true, left, left, 96, 96, 1), new("student", false, right, right, 144, 144, 1)];
        Device touch = new("pointer-1", "TOUCH", "student", right, 10, 0, 0);
        Check(DisplayDiscovery.Decide(displays, [touch], []) == "POINTER_MAPPING_OBSERVED_ISOLATION_UNVERIFIED", "mapped does not imply isolation");
        Check(DisplayDiscovery.Decide(displays, [], []) == "NO_MAPPED_POINTER_MOUSE_PATH_UNVERIFIED", "mouse-only does not imply support");
        Check(DisplayDiscovery.Decide(displays, [touch with { DisplayId = "teacher" }], []) == "OTHER_OR_UNMAPPED_POINTER_DEVICE", "other teacher touch rejected");
        Check(DisplayDiscovery.Decide(displays, [touch with { DisplayId = null }], []) == "OTHER_OR_UNMAPPED_POINTER_DEVICE", "unknown mapping rejected");
        Check(DisplayDiscovery.Decide(displays, [], ["DISPLAY_CLONE_DETECTED"]) == "DISCOVERY_INCOMPLETE", "clone cannot start");
        Check(DisplayDiscovery.Decide([displays[0]], [], []) == "EXACTLY_TWO_EXTENDED_DISPLAYS_REQUIRED", "single display rejected");
        Check(DisplayDiscovery.Decide([displays[0], displays[1] with { Bounds = left }], [], []) == "DISPLAY_OVERLAP", "overlap rejected");
        Check(!InputPolicy.CanBlock(new(displays, [], ["DISPLAY_CLONE_DETECTED"], "DISCOVERY_INCOMPLETE")), "clone disables blocking only");
        Check(!InputPolicy.CanBlock(new(displays, [], [], "OTHER_OR_UNMAPPED_POINTER_DEVICE")), "other touch disables blocking");
        Check(!InputPolicy.StopForTopology(false, false), "passive observation survives topology notifications");
        Check(!InputPolicy.StopForTopology(true, true), "baseline Raw Input arrivals do not stop own guard");
        Check(InputPolicy.StopForTopology(true, false), "real topology change stops blocking trial");
        var log = new ObservationLog();
        Check(log.Alias(1, "raw") == log.Alias(1, "raw") && log.Alias(1, "raw") != log.Alias(2, "raw"), "source identity");
        for (var i = 0; i < ObservationLog.Capacity + 5; i++) log.Add("EVENT", detail: (uint)i);
        var snapshot = log.Snapshot();
        Check(snapshot.Events.Length == ObservationLog.Capacity && snapshot.Dropped == 5 && snapshot.Counts["EVENT"] == ObservationLog.Capacity + 5, "bounded observations with explicit loss");
        Check(!Report.Serialize(new(displays, [], [], "NONE"), log).Contains("ACTIVE"), "report never claims ACTIVE");
        if (IntPtr.Size == 8)
        {
            Check(Marshal.SizeOf<Native.RawHeader>() == 24, "RAWINPUTHEADER x64 ABI");
            Check(Marshal.SizeOf<Native.PointerDevice>() == 1080, "POINTER_DEVICE_INFO x64 ABI");
            Check(Marshal.SizeOf<Native.GuiInfo>() == 72, "GUITHREADINFO x64 ABI");
        }
        Check(Marshal.SizeOf<Native.DisplayPath>() == 72, "DISPLAYCONFIG_PATH_INFO ABI");
        Check(Marshal.SizeOf<Native.SourceName>() == 84, "DISPLAYCONFIG_SOURCE_DEVICE_NAME ABI");
        Check(Marshal.SizeOf<Native.RawMouse>() == 24, "RAWMOUSE union ABI");
        var seat = new StudentSeat(right);
        Check(!seat.Accept("pointer-1", 1, "touch", "move", new(5, 5)), "move without down rejected");
        Check(seat.Accept("pointer-1", 1, "touch", "down", new(5, 5)), "student contact begins");
        Check(!seat.Accept("pointer-1", 1, "touch", "down", new(5, 5)), "duplicate down rejected");
        Check(seat.Accept("pointer-2", 1, "pen", "down", new(10, 10)) && seat.ContactCount == 2, "contact identity includes device");
        seat.Drain();
        Check(!seat.Accept("pointer-1", 1, "touch", "move", new(-1, 5)) && seat.ContactCount == 1, "leaving student area cancels only own contact");
        Check(seat.Drain().Single().Phase == "cancel", "leaving area emits cancel");
        seat.CancelAll(true);
        var canceled = seat.Drain().Single();
        Check(seat.ContactCount == 0 && seat.GeometryEpoch == 2 && canceled.IsolationEvidence == "NONE" && canceled.SourceKind == "pen", "geometry reset preserves kind and never asserts isolation");
        seat.Accept("pointer-1", 2, "touch", "down", new(5, 5));
        for (var i = 0; i < StudentSeat.Capacity; i++) seat.Accept("pointer-1", 2, "touch", "move", new(5, 5));
        Check(seat.Overflows == 1 && seat.ContactCount == 0 && seat.Drain().All(e => e.Phase == "cancel"), "queue overload invalidates pending contacts");
        Console.WriteLine($"Native policy/ABI tests OK: {checkedCount}; hardware G1/G2 NOT TESTED");
        ProtectionTests.Run(); PointerIsolationTests.Run(); return 0;
    }
}
