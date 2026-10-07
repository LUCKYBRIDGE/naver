using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WhaleDualInput;

internal sealed class ControlWindow : Form
{
    private readonly Action<object> send;
    private readonly ComboBox windows = new() { Width = 480, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(480, 0) };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 250 };
    private TouchOverlay? overlay;
    private Rectangle viewport, originalWindow;
    private nint target;
    private string session = "";
    private int cssWidth, cssHeight;
    private bool hotkey;
    private long seq;
    internal volatile bool Active;
    internal long LastHeartbeat = Environment.TickCount64, LastUiBeat = Environment.TickCount64;
    internal long Heartbeat => Interlocked.Read(ref LastHeartbeat);
    internal long UiBeat => Interlocked.Read(ref LastUiBeat);
    protected override bool ShowWithoutActivation => true;
    internal ControlWindow(Action<object> send)
    {
        this.send = send;
        Text = "웨일 듀얼 인풋 · Windows 제어"; Width = 560; Height = 330;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20) };
        layout.Controls.Add(new Label { AutoSize = true, Text = "학생이 사용할 웨일 창" }); layout.Controls.Add(windows);
        var refresh = new Button { Text = "웨일 창 목록 갱신", AutoSize = true, Height = 44 };
        var calibrate = new Button { Text = "웹페이지 영역 지정", AutoSize = true, Height = 44 };
        var stop = new Button { Text = "OFF · 즉시 해제 (F9)", AutoSize = true, Height = 44 };
        refresh.Click += (_, _) => RefreshWindows(); calibrate.Click += (_, _) => Calibrate(); stop.Click += (_, _) => Stop("사용자가 해제했습니다.");
        windows.SelectedIndexChanged += (_, _) => { Stop("대상 창이 변경되었습니다."); viewport = Rectangle.Empty; };
        layout.Controls.Add(refresh); layout.Controls.Add(calibrate); layout.Controls.Add(stop); layout.Controls.Add(status); Controls.Add(layout);
        timer.Tick += (_, _) => Watch(); timer.Start(); RefreshWindows();
    }
    private sealed record WindowChoice(nint Handle, string Label) { public override string ToString() => Label; }
    private void RefreshWindows()
    {
        Stop("OFF"); windows.Items.Clear();
        Native.EnumWindows((handle, _) =>
        {
            try
            {
                if (!Native.IsWindowVisible(handle)) return true;
                Native.GetWindowThreadProcessId(handle, out var pid);
                using var process = Process.GetProcessById((int)pid);
                if (!process.ProcessName.Equals("whale", StringComparison.OrdinalIgnoreCase)) return true;
                var title = new StringBuilder(256); Native.GetWindowText(handle, title, title.Capacity);
                windows.Items.Add(new WindowChoice(handle, title.Length == 0 ? "웨일 창 " + handle : title.ToString()));
            }
            catch { /* Windows may close while enumerating. */ }
            return true;
        }, 0);
        if (windows.Items.Count > 0) windows.SelectedIndex = 0;
    }
    private void Calibrate()
    {
        Stop("영역 지정 중");
        if (session.Length == 0 || windows.SelectedItem is not WindowChoice choice) { status.Text = "사이드바에서 학생 탭을 선택하고 웨일 창을 지정하세요."; return; }
        using var selection = new ScreenSelection();
        if (selection.ShowDialog() != DialogResult.OK) return;
        if (!Native.GetWindowRect(choice.Handle, out var rect) || !rect.Bounds.Contains(selection.Selected) || selection.Selected.Width < 100 || selection.Selected.Height < 100)
        { status.Text = "선택한 웨일 창의 웹페이지 내부 영역을 지정하세요."; return; }
        target = choice.Handle; originalWindow = rect.Bounds; viewport = selection.Selected;
        status.Text = $"준비됨: {viewport.Width}×{viewport.Height}. 사이드바에서 ON을 누르세요.";
        send(new { type = "ready", session });
    }
    internal void Receive(JsonElement message)
    {
        try
        {
            var type = message.GetProperty("type").GetString();
            if (type == "configure")
            {
                Stop("대상 설정 변경");
                session = message.GetProperty("session").GetString() ?? "";
                if (session.Length < 16 || session.Length > 100) throw new InvalidDataException("잘못된 세션");
                cssWidth = message.GetProperty("width").GetInt32(); cssHeight = message.GetProperty("height").GetInt32();
                if (cssWidth < 100 || cssWidth > 10000 || cssHeight < 100 || cssHeight > 10000) throw new InvalidDataException("잘못된 페이지 크기");
                viewport = Rectangle.Empty; seq = 0; status.Text = "웹페이지 영역을 지정하세요."; return;
            }
            if (message.GetProperty("session").GetString() != session) return;
            switch (type)
            {
                case "tick": Interlocked.Exchange(ref LastHeartbeat, Environment.TickCount64); break;
                case "stop": Stop("OFF"); break;
                case "mode": if (overlay != null) overlay.ScrollMode = message.GetProperty("scroll").GetBoolean(); break;
                case "start": Start(); break;
            }
        }
        catch (Exception error) { Stop(error.Message); }
    }
    private void Start()
    {
        if (Active) return;
        if (viewport.IsEmpty || !Native.IsWindowVisible(target) || Native.IsIconic(target) ||
            !Native.GetWindowRect(target, out var rect) || rect.Bounds != originalWindow)
        { Stop("창 위치가 변경되었습니다. 영역을 다시 지정하세요."); return; }
        if (!Native.RegisterHotKey(Handle, 9, 0x4000, 0x78)) { Stop("F9를 등록할 수 없습니다. 다른 프로그램의 단축키를 확인하세요."); return; }
        hotkey = true;
        overlay = new TouchOverlay(viewport, cssWidth, cssHeight, EmitPointer, () => Stop("입력 장치 또는 영역이 변경되었습니다."));
        overlay.Show(); Active = true;
        Interlocked.Exchange(ref LastHeartbeat, Environment.TickCount64);
        status.Text = "ON · F9로 즉시 해제"; send(new { type = "active", session });
    }
    private void EmitPointer(string phase, uint pointer, double x, double y, double delta)
    {
        if (!Active) return;
        if (Native.IsIconic(target) || !Native.IsWindowVisible(target) || !Native.GetWindowRect(target, out var rect) || rect.Bounds != originalWindow || Covered())
        { Stop("학생 창이 이동하거나 가려졌습니다."); return; }
        send(new { type = "pointer", session, seq = ++seq, pointer, phase, x, y, delta });
    }
    internal void Stop(string reason)
    {
        Active = false;
        overlay?.Close(); overlay?.Dispose(); overlay = null;
        if (hotkey) Native.UnregisterHotKey(Handle, 9);
        hotkey = false;
        status.Text = reason;
        if (session.Length > 0) send(new { type = "off", session, reason });
    }
    private void Watch()
    {
        Interlocked.Exchange(ref LastUiBeat, Environment.TickCount64);
        if (!Active) return;
        if (Environment.TickCount64 - Interlocked.Read(ref LastHeartbeat) > 2500 || !Native.IsWindowVisible(target) || Native.IsIconic(target) ||
            !Native.GetWindowRect(target, out var rect) || rect.Bounds != originalWindow || Covered()) { Stop("연결 또는 대상 창의 표시 상태가 변경되었습니다."); return; }
        var cursor = new Native.CursorInfo { Size = (uint)Marshal.SizeOf<Native.CursorInfo>() };
        if (!Native.GetCursorInfo(ref cursor) || (cursor.Flags & 2) != 0) Stop("Windows가 교사 커서를 억제했습니다. 독립 입력을 해제했습니다.");
    }
    private bool Covered()
    {
        var covered = false;
        Native.EnumWindows((window, _) =>
        {
            if (window == target) return false;
            if (window == Handle || window == overlay?.Handle || !Native.IsWindowVisible(window) || Native.IsIconic(window)) return true;
            if (Native.GetWindowRect(window, out var rectangle) && rectangle.Bounds.IntersectsWith(viewport)) { covered = true; return false; }
            return true;
        }, 0);
        return covered;
    }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x312 && message.WParam.ToInt32() == 9) { Stop("F9 해제"); return; }
        base.WndProc(ref message);
    }
    protected override void OnFormClosing(FormClosingEventArgs e) { Stop("Windows 프로그램 종료"); timer.Dispose(); base.OnFormClosing(e); }
}

internal sealed class ScreenSelection : Form
{
    private Point start, end;
    private bool dragging;
    internal Rectangle Selected { get; private set; }
    internal ScreenSelection()
    {
        FormBorderStyle = FormBorderStyle.None; Bounds = SystemInformation.VirtualScreen; TopMost = true;
        BackColor = Color.DarkSlateGray; Opacity = 0.35; Cursor = Cursors.Cross; DoubleBuffered = true; KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
        MouseDown += (_, e) => { start = e.Location; end = start; dragging = true; Capture = true; };
        MouseMove += (_, e) => { if (dragging) { end = e.Location; Invalidate(); } };
        MouseUp += (_, e) =>
        {
            end = e.Location; dragging = false;
            Selected = Rectangle.FromLTRB(Math.Min(start.X, end.X) + Left, Math.Min(start.Y, end.Y) + Top,
                Math.Max(start.X, end.X) + Left, Math.Max(start.Y, end.Y) + Top);
            DialogResult = DialogResult.OK; Close();
        };
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); using var pen = new Pen(Color.Lime, 3);
        e.Graphics.DrawRectangle(pen, Rectangle.FromLTRB(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Max(start.X, end.X), Math.Max(start.Y, end.Y)));
    }
}

internal sealed class TouchOverlay : Form
{
    private readonly Action<string, uint, double, double, double> emit;
    private readonly Action fail;
    private readonly int cssWidth, cssHeight;
    private uint? pointer;
    private nint device;
    private double lastY;
    private double downX, downY;
    private bool scrollTravel;
    private double lastX;
    internal bool ScrollMode;
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams { get { var p = base.CreateParams; p.ExStyle |= 0x08000000 | 0x00000080; return p; } }
    internal TouchOverlay(Rectangle bounds, int width, int height, Action<string, uint, double, double, double> emit, Action fail)
    {
        this.emit = emit; this.fail = fail; cssWidth = width; cssHeight = height;
        FormBorderStyle = FormBorderStyle.None; Bounds = bounds; TopMost = true; ShowInTaskbar = false;
        BackColor = Color.Black; Opacity = 0.01;
    }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x21) { message.Result = 3; return; } // MA_NOACTIVATE
        if (message.Msg == 0x24B) { message.Result = 3; return; } // PA_NOACTIVATE
        // Candidate only: HTTRANSPARENT is documented for same-thread windows.
        // Passing physical mouse input to the external Whale process is NOT established.
        if (message.Msg == 0x84 && !Native.IsTouchMouse) { message.Result = -1; return; }
        if (message.Msg is 0x246 or 0x245 or 0x247)
        {
            var id = (uint)(message.WParam.ToInt64() & 0xffff);
            if (!Native.GetPointerInfo(id, out var info) || info.Type != 2) { message.Result = 0; return; }
            if (device != 0 && device != info.Device) { fail(); return; }
            if (info.Device == 0) { fail(); return; }
            device = info.Device;
            if (message.Msg == 0x246 && pointer == null) pointer = id;
            if (pointer != id) { message.Result = 0; return; }
            var x = Math.Clamp((info.Pixel.X - Left) * (double)cssWidth / Width, 0, cssWidth - 1);
            var y = Math.Clamp((info.Pixel.Y - Top) * (double)cssHeight / Height, 0, cssHeight - 1);
            var canceled = (info.Flags & 0x8000) != 0;
            var phase = canceled ? "cancel" : message.Msg == 0x246 ? "down" : message.Msg == 0x247 ? "up" : "move";
            if (phase == "down") { downX = x; downY = y; scrollTravel = false; }
            if (ScrollMode)
            {
                if (phase == "move" && Math.Abs(y - downY) > 5) scrollTravel = true;
                if (phase == "move" && scrollTravel) emit("scroll", id, x, y, lastY - y);
                if (phase == "up" && !scrollTravel) { emit("down", id, downX, downY, 0); emit("up", id, x, y, 0); }
            }
            else emit(phase, id, x, y, 0);
            lastX = x; lastY = y;
            if (phase is "up" or "cancel") pointer = null;
            message.Result = 0; return;
        }
        if (message.Msg == 0x24C && pointer is uint captured)
        {
            emit("cancel", captured, lastX, lastY, 0); pointer = null; message.Result = 0; return;
        }
        // Touch-promoted mouse messages must not be duplicated as page mouse input.
        if (message.Msg >= 0x200 && message.Msg <= 0x20E && Native.IsTouchMouse) { message.Result = 0; return; }
        base.WndProc(ref message);
    }
}
