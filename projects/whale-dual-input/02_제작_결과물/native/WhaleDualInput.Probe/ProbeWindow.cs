using System.Text.Json;

namespace WhaleDualInput.Probe;

internal sealed class ProbeWindow : Form
{
    private readonly ObservationLog log = new();
    private Inventory inventory;
    private InputObserver? observer;
    private MouseGuardExperiment? mouse;
    private StudentSurface? surface;
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(600, 0), Text = "OFF · 관찰 시작 전. 입력 보호 기능은 아직 검증되지 않았습니다." };
    private readonly TextBox details = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Button observe = Button("입력 관찰 시작 (2분)");
    private readonly Button experiment = Button("차단 후보 ON (30초 시험)");
    private readonly Button stop = Button("OFF · 시험 중지 (F9)");
    private readonly System.Windows.Forms.Timer refresh = new() { Interval = 200 };
    private bool ending;
    private bool sessionRegistered;
    internal ProbeWindow()
    {
        Text = "웨일 듀얼 인풋 · 입력 분리 연구 도구";
        Font = new Font("Malgun Gothic", 10);
        AutoScaleMode = AutoScaleMode.Dpi; MinimumSize = new(500, 480); Size = new(680, 650);
        StartPosition = FormStartPosition.Manual;
        inventory = DisplayDiscovery.Read(log);
        if (inventory.Teacher is Display teacher) Location = new(teacher.Work.Left + 24, teacher.Work.Top + 24);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(16) };
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(600, 0), Text = "교사: Windows 주 화면 / 학생: 유일한 보조 화면\n관찰은 터치를 차단하지 않습니다. 교사 커서가 움직일 수 있습니다.\n팝업 안: 직접 터치·학생 채널 / 팝업 밖: Raw Input·마우스 관측만" });
        layout.Controls.Add(status);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        var diagnose = Button("화면·장치 다시 확인"); var export = Button("익명 진단 저장"); var mark = Button("학생 터치 시험 구간 표시");
        buttons.Controls.AddRange([observe, experiment, stop, diagnose, mark]); layout.Controls.Add(buttons);
        layout.Controls.Add(details); layout.Controls.Add(export); Controls.Add(layout);
        observe.Click += (_, _) => Start(false);
        experiment.Click += (_, _) =>
        {
            if (MessageBox.Show(this, "30초 동안 교사 커서를 주 화면에 제한하고 마우스 차단 후보를 시험합니다.\n\n직접 터치 경로가 새어 교사 커서·포커스를 바꿀 수 있습니다. 시험 문서만 사용하세요. F9 또는 OFF로 중지합니다. Whale에는 입력을 보내지 않습니다.", "연구 시험", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.OK) Start(true);
        };
        stop.Click += (_, _) => End("사용자 OFF");
        diagnose.Click += (_, _) => { End("장치 재검사"); inventory = DisplayDiscovery.Read(log); Render(); };
        mark.Click += (_, _) => { if (observer?.Running == true) log.Add("STUDENT_TRIAL_MARKER"); };
        export.Click += (_, _) => Export();
        refresh.Tick += (_, _) =>
        {
            mouse?.Beat();
            if (mouse != null && !mouse.Running) End(mouse.State);
            if (surface != null && observer?.LastStudentPoint is Point point) surface.UpdateCandidate(point, observer.LastStudentSource);
            if (observer != null)
            {
                foreach (var input in observer.Seat.Drain()) log.Add("SEAT_" + input.Phase.ToUpperInvariant(), input.SourceDeviceId, input.XPhysicalPx, input.YPhysicalPx, input.ContactId);
            }
            Render();
        };
        LocationChanged += (_, _) => { if (mouse?.Running == true && inventory.Teacher?.Bounds.Contains(Bounds) != true) End("교사 제어창이 주 화면을 벗어남"); };
        refresh.Start(); Render();
    }
    private static Button Button(string text) => new() { Text = text, AutoSize = true, MinimumSize = new(150, 44), Margin = new Padding(4) };
    private void Start(bool block)
    {
        End("새 관찰 세션");
        try
        {
            inventory = DisplayDiscovery.Read(log);
            if (block && !InputPolicy.CanBlock(inventory))
                throw new InvalidOperationException("확장된 화면 2개와 주 화면을 확실히 식별해야 합니다. " + inventory.Decision);
            if (block && inventory.Decision == "OTHER_OR_UNMAPPED_POINTER_DEVICE") throw new InvalidOperationException("다른 화면의 터치 또는 미매핑 장치가 있어 차단 시험을 거부합니다.");
            if (block && !sessionRegistered) throw new InvalidOperationException("세션 변경 감시를 등록하지 못해 차단 시험을 거부합니다.");
            if (block && inventory.Teacher?.Bounds.Contains(Bounds) != true) throw new InvalidOperationException("제어창을 주 화면 안에 두고 시작하세요.");
            observer = new InputObserver(Handle, log, inventory); observer.Start();
            if (inventory.Student is Display student)
            {
                surface = new StudentSurface(observer, student);
                surface.FormClosed += (_, _) => End("학생 관찰 창 종료");
                surface.Show();
            }
            mouse = new(log, inventory.Teacher?.Bounds ?? Rectangle.Empty, SystemInformation.VirtualScreen, block); mouse.Start();
            status.Text = block ? "시험 ON · 보호 미검증 · 최대 30초 / F9·OFF로 중지" : "관찰 중 · 터치 차단 없음 · 커서 납치 가능 · 최대 2분";
        }
        catch (Exception error) { End("시작 거부: " + error.Message); }
        Render();
    }
    private void End(string reason)
    {
        if (ending) return;
        ending = true;
        try
        {
            string? failure = null;
            if (mouse != null)
            {
                try { mouse.Dispose(); if (mouse.State.StartsWith("ERROR_", StringComparison.Ordinal)) failure = mouse.State; }
                catch (Exception error) { failure = error.Message; }
                if (!mouse.Running) mouse = null;
            }
            if (observer != null)
            {
                observer.Dispose();
                foreach (var input in observer.Seat.Drain()) log.Add("SEAT_" + input.Phase.ToUpperInvariant(), input.SourceDeviceId, input.XPhysicalPx, input.YPhysicalPx, input.ContactId);
                observer = null;
            }
            surface?.Close(); surface?.Dispose(); surface = null;
            status.Text = failure == null ? "OFF · " + reason + " · G1/G2 미검증" : "해제 확인 필요 · " + failure + " · F9/프로그램 종료 후 입력 상태 확인";
        }
        finally { ending = false; }
    }
    private void Render()
    {
        var snapshot = log.Snapshot();
        observe.Enabled = mouse == null;
        experiment.Enabled = mouse == null && InputPolicy.CanBlock(inventory);
        stop.Enabled = mouse != null || observer != null;
        details.Text = $"자동 판정: {inventory.Decision}\r\n조회 사유: {string.Join(", ", inventory.Errors)}\r\n격리 증거: NONE / G1: NOT TESTED / G2: NOT TESTED\r\n" +
            (inventory.Errors.Length != 0 || inventory.Decision == "OTHER_OR_UNMAPPED_POINTER_DEVICE" ? "현재 구성: 관찰 가능 / 차단 시험 불가\r\n" : "") +
            string.Join("\r\n", inventory.Displays.Select(d => $"{d.Id} {(d.Primary ? "교사 주 화면" : "학생 보조 화면")} {d.Bounds} DPI {d.DpiX}/{d.DpiY} 회전 {d.Rotation} 출력경로={d.PhysicalTargetPaths}")) + "\r\n\r\n" +
            string.Join("\r\n", inventory.Devices.Select(d => $"{d.Id} {d.Kind} 화면={d.DisplayId ?? "미확인"} 최대접촉={d.Contacts} usage={d.UsagePage}/{d.Usage}")) + "\r\n\r\n" +
            string.Join("\r\n", snapshot.Counts.Select(c => $"{c.Key}: {c.Value}")) +
            $"\r\n최근 사건 최대 {ObservationLog.Capacity}개 / 덮어쓴 사건: {snapshot.Dropped}\r\n" +
            "커서/포커스 변화는 관측값입니다. 학생 원인 자동 판정·G1 합격 판정이 아닙니다.\r\n직접 포인터는 학생 시험 창에 도달한 것만 관측합니다.";
    }
    private void Export()
    {
        // End first so export dialogs cannot stall a blocking input hook or skew a trial.
        End("진단 저장 전 중지");
        using var save = new SaveFileDialog { Filter = "진단 JSON|*.json", FileName = "whale-input-probe.json" };
        if (save.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllText(save.FileName, Report.Serialize(inventory, log)); status.Text = "OFF · 익명 진단 저장 완료. 입력 원문·URL·창 제목은 포함하지 않습니다."; }
        catch (Exception) { status.Text = "저장하지 못했습니다. 쓰기 가능한 폴더를 선택하세요."; }
    }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0xff) observer?.ReadRaw(message.LParam);
        if (message.Msg is 0xfe or 0x7e or 0x219 or 0x2b1)
        {
            // RegisterRawInputDevices itself announces already-present devices.
            // A read-only observer must not stop itself on those initial arrivals.
            var arrivalId = message.Msg == 0xfe ? log.Alias(message.LParam, "raw") : "none";
            var baselineArrival = message.Msg == 0xfe && message.WParam == 1 &&
                inventory.Devices.Any(d => d.Id == arrivalId);
            if (observer?.Running == true) log.Add("TOPOLOGY_NOTIFICATION", detail: (uint)message.Msg);
            if (InputPolicy.StopForTopology(mouse?.State == "EXPERIMENTAL_NOT_VERIFIED", baselineArrival)) End("장치·화면·세션 변경");
        }
        base.WndProc(ref message);
    }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); sessionRegistered = SessionNotifications.Register(Handle); }
    protected override void OnFormClosing(FormClosingEventArgs e)
    { refresh.Stop(); End("프로그램 종료"); refresh.Dispose(); SessionNotifications.Unregister(Handle); base.OnFormClosing(e); }
}

internal sealed class StudentSurface : Form
{
    private readonly InputObserver observer;
    private readonly Display display;
    private Point? point;
    private string source = "직접 터치 또는 절대좌표 입력 대기";
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams { get { var p = base.CreateParams; p.ExStyle |= 0x08000000; return p; } }
    internal StudentSurface(InputObserver observer, Display display)
    {
        this.observer = observer; this.display = display;
        Text = "학생 입력 관찰 창 · 보호 미검증 · Whale 전송 없음";
        StartPosition = FormStartPosition.Manual;
        Bounds = new(display.Work.Left + 20, display.Work.Top + 20, Math.Min(900, display.Work.Width - 40), Math.Min(650, display.Work.Height - 40));
        BackColor = Color.White; DoubleBuffered = true;
    }
    internal void UpdateCandidate(Point screen, string id)
    { point = PointToClient(screen); source = id; Invalidate(); }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg is 0x21 or 0x24b) { message.Result = 3; return; }
        if (message.Msg is 0x245 or 0x246 or 0x247 or 0x24c)
            observer.Pointer((uint)(message.WParam.ToInt64() & 0xffff), (uint)message.Msg);
        base.WndProc(ref message);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        TextRenderer.DrawText(e.Graphics, "학생 입력 위치 관찰\n표시는 입력 격리 성공의 증거가 아닙니다.\n" + source + "\n화면: " + display.Id,
            Font, new Rectangle(20, 20, Math.Max(1, ClientSize.Width - 40), 120), Color.DarkSlateGray);
        if (point is Point p) { using var pen = new Pen(Color.Teal, 4); e.Graphics.DrawEllipse(pen, p.X - 12, p.Y - 12, 24, 24); }
    }
}

internal static class SessionNotifications
{
    [System.Runtime.InteropServices.DllImport("wtsapi32.dll", EntryPoint = "WTSRegisterSessionNotification")]
    private static extern bool RegisterNative(nint window, uint flags);
    [System.Runtime.InteropServices.DllImport("wtsapi32.dll", EntryPoint = "WTSUnRegisterSessionNotification")]
    private static extern bool UnregisterNative(nint window);
    internal static bool Register(nint window) => RegisterNative(window, 0);
    internal static void Unregister(nint window) { UnregisterNative(window); }
}

internal static class Report
{
    internal static string Serialize(Inventory inventory, ObservationLog log)
    {
        var snapshot = log.Snapshot();
        return JsonSerializer.Serialize(new { schemaVersion = 1, tool = "WhaleDualInput.Probe", mode = "RESEARCH",
            g1 = "NOT_TESTED", g2 = "NOT_TESTED", isolationEvidence = "NONE", cursorSampleTargetMs = 4,
            inventory, counts = snapshot.Counts, overwritten = snapshot.Dropped, events = snapshot.Events,
            limitations = new[] { "No automatic attribution or pass verdict", "Only local surface sees direct pointer messages", "Raw HID payload not decoded", "Relative mouse cannot be mapped to student automatically", "Pointer and Raw Input aliases are not proven physical-device matches", "No Whale input transport" } }, new JsonSerializerOptions { WriteIndented = true });
    }
}
