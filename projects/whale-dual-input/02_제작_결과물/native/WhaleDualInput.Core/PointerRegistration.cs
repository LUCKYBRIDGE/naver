namespace WhaleDualInput.Core;

internal interface IPointerRegistration
{
    bool Register(uint type);
    bool Unregister(uint type);
    int LastError { get; }
}

/// <summary>Transactional type registrations; only touch/pen, never mouse or generic pointer.</summary>
internal sealed class PointerRegistration
{
    private readonly IPointerRegistration api;
    private readonly List<uint> registered = new();
    internal string Reason { get; private set; } = "OFF";
    internal bool ReleaseConfirmed => registered.Count == 0;
    internal PointerRegistration(IPointerRegistration api) => this.api = api;
    internal bool Start(bool uiAccess, bool mappingVerified, uint[] types)
    {
        if (registered.Count != 0) { Reason = "ALREADY_REGISTERED"; return false; }
        if (!uiAccess) { Reason = "UIACCESS_REQUIRED"; return false; }
        if (!mappingVerified) { Reason = "MAPPING_UNVERIFIED"; return false; }
        if (types.Length == 0 || types.Distinct().Count() != types.Length || types.Any(t => t is not (2 or 3)))
        { Reason = "UNSUPPORTED_POINTER_TYPE"; return false; }
        foreach (var type in types)
        {
            if (!api.Register(type))
            {
                var error = api.LastError;
                Release();
                Reason = ReleaseConfirmed ? $"REGISTER_FAILED_{error}" : "RELEASE_UNCONFIRMED";
                return false;
            }
            registered.Add(type);
        }
        Reason = "REGISTERED_EXPERIMENTAL"; return true;
    }
    internal bool Release()
    {
        for (var i = registered.Count - 1; i >= 0; i--)
            if (api.Unregister(registered[i])) registered.RemoveAt(i);
        Reason = ReleaseConfirmed ? "RELEASE_CONFIRMED" : "RELEASE_UNCONFIRMED";
        return ReleaseConfirmed;
    }
}
