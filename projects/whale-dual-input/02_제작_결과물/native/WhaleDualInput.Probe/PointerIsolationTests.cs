using WhaleDualInput.Core;

namespace WhaleDualInput.Probe;

internal static class PointerIsolationTests
{
    private sealed class FakeRegistration : IPointerRegistration
    {
        internal readonly List<uint> Registered = new(), Released = new();
        internal uint FailType;
        internal bool FailRelease;
        public int LastError => 5;
        public bool Register(uint type) { Registered.Add(type); return type != FailType; }
        public bool Unregister(uint type) { Released.Add(type); return !FailRelease; }
    }
    internal static void Run()
    {
        int count = 0;
        void Check(bool value, string name) { if (!value) throw new InvalidOperationException("FAIL isolation: " + name); count++; }
        var api = new FakeRegistration(); var registration = new PointerRegistration(api);
        Check(!registration.Start(false, true, [2]) && api.Registered.Count == 0 && registration.Reason == "UIACCESS_REQUIRED", "no privilege means no registration attempt");
        Check(!registration.Start(true, false, [2]) && api.Registered.Count == 0, "no mapping means no call");
        Check(!registration.Start(true, true, [4]) && api.Registered.Count == 0, "mouse type cannot redirect");
        Check(!registration.Start(true, true, [1]), "generic pointer cannot redirect");
        Check(!registration.Start(true, true, [2, 2]), "duplicate types rejected");
        Check(!registration.Start(true, true, []), "no type rejected");
        Check(registration.Start(true, true, [2, 3]) && registration.Reason == "REGISTERED_EXPERIMENTAL", "touch and pen are experimental only");
        Check(!registration.Start(true, true, [2]) && api.Registered.Count == 2, "no duplicate registration");
        Check(registration.Release() && api.Released.SequenceEqual([3u, 2u]), "release on reverse order");
        Check(registration.Release() && api.Released.Count == 2, "release idempotent");
        api = new() { FailType = 3 }; registration = new(api);
        Check(!registration.Start(true, true, [2, 3]) && api.Released.SequenceEqual([2u]) && registration.ReleaseConfirmed, "partial registration rollback");
        Check(registration.Reason == "REGISTER_FAILED_5", "access denied is not falsely called ownership conflict");
        api = new() { FailType = 3, FailRelease = true }; registration = new(api);
        Check(!registration.Start(true, true, [2, 3]) && !registration.ReleaseConfirmed && registration.Reason == "RELEASE_UNCONFIRMED", "rollback failure remains unconfirmed");
        api.FailRelease = false;
        Check(registration.Release() && registration.ReleaseConfirmed, "release retry");
        var displays = new[] { new Display("teacher", true, new(0, 0, 1920, 1080), new(0, 0, 1920, 1080), 96, 96, 1),
            new Display("student", false, new(1920, 0, 1920, 1080), new(1920, 0, 1920, 1080), 96, 96, 1) };
        var device = new Device("pointer-1", "TOUCH", "student", displays[1].Bounds, 40, 0, 0);
        var inventory = new Inventory(displays, [device], [], "POINTER_MAPPING_OBSERVED_ISOLATION_UNVERIFIED");
        Check(DisplayDiscovery.IsolationEligibility(inventory, false) == "UIACCESS_REQUIRED", "UI credential required");
        Check(DisplayDiscovery.IsolationEligibility(inventory, true) == "RESEARCH_ELIGIBLE", "research eligible never hardware supported");
        Check(DisplayDiscovery.IsolationEligibility(inventory with { Errors = ["DISPLAY_CLONE_DETECTED"] }, true) == "ISOLATION_MAPPING_UNSUPPORTED", "clone rejects isolation");
        Check(DisplayDiscovery.IsolationEligibility(inventory with { Decision = "OTHER_OR_UNMAPPED_POINTER_DEVICE" }, true) == "ISOLATION_MAPPING_UNSUPPORTED", "other touchscreen rejects isolation");
        Check(DisplayDiscovery.IsolationEligibility(inventory with { Devices = [], Decision = "NO_MAPPED_POINTER_MOUSE_PATH_UNVERIFIED" }, true) == "PT_MOUSE_REDIRECTION_UNSUPPORTED", "mouse-only not misrepresented");
        Console.WriteLine($"Pointer registration lifecycle tests OK: {count}; fake API only, physical G1 NOT TESTED");
    }
}
