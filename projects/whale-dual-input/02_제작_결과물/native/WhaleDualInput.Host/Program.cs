using System.Buffers.Binary;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Win32;

namespace WhaleDualInput;

internal static class Program
{
    internal const string HostName = "org.luckybridge.whale_dual_input";
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var nativeMode = args.Any(arg => arg.StartsWith("chrome-extension://", StringComparison.Ordinal));
        if (!nativeMode) { Application.Run(new Installer()); return; }
        var origin = args.First(arg => arg.StartsWith("chrome-extension://", StringComparison.Ordinal));
        var id = origin["chrome-extension://".Length..].TrimEnd('/');
        if (id.Length != 32 || id.Any(c => c < 'a' || c > 'p')) return;
        using var mutex = new Mutex(true, "Local\\WhaleDualInput-" + id, out var created);
        if (!created) return;
        var queue = Channel.CreateBounded<object>(new BoundedChannelOptions(256) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
        void Send(object value) { if (!queue.Writer.TryWrite(value)) Environment.Exit(4); }
        using var form = new ControlWindow(Send);
        _ = form.Handle;
        _ = Task.Run(async () =>
        {
            try
            {
                await using var output = Console.OpenStandardOutput();
                await foreach (var value in queue.Reader.ReadAllAsync())
                {
                    var body = JsonSerializer.SerializeToUtf8Bytes(value);
                    var header = new byte[4];
                    BinaryPrimitives.WriteInt32LittleEndian(header, body.Length);
                    await output.WriteAsync(header); await output.WriteAsync(body); await output.FlushAsync();
                }
            }
            catch { Environment.Exit(5); }
        });
        _ = Task.Run(async () =>
        {
            try
            {
                await using var input = Console.OpenStandardInput();
                while (true)
                {
                    var header = new byte[4]; await input.ReadExactlyAsync(header);
                    var length = BinaryPrimitives.ReadInt32LittleEndian(header);
                    if (length <= 0 || length > 65536) throw new InvalidDataException();
                    var body = new byte[length]; await input.ReadExactlyAsync(body);
                    using var json = JsonDocument.Parse(body);
                    var message = json.RootElement.Clone();
                    form.BeginInvoke(() => form.Receive(message));
                }
            }
            catch { if (!form.IsDisposed) try { form.BeginInvoke(() => form.Close()); } catch { Environment.Exit(6); } }
        });
        // A frozen UI or broken pipe must not leave an input-receiving overlay alive.
        _ = Task.Run(async () =>
        {
            while (!form.IsDisposed)
            {
                await Task.Delay(250);
                if (form.Active && (Environment.TickCount64 - form.Heartbeat > 2500 ||
                    Environment.TickCount64 - form.UiBeat > 2500)) Environment.Exit(7);
            }
        });
        form.Shown += (_, _) => Send(new { type = "hello", version = "0.2.0" });
        Application.Run(form);
        queue.Writer.TryComplete();
    }
}

internal sealed class Installer : Form
{
    private readonly TextBox extension = new() { Width = 360 };
    private readonly TextBox registry = new() { Width = 480, Text = @"Software\Naver\Naver Whale\NativeMessagingHosts" };
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(480, 0) };
    internal Installer()
    {
        Text = "웨일 듀얼 인풋 · 연결 설정"; Width = 560; Height = 360;
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), WrapContents = false };
        layout.Controls.Add(new Label { AutoSize = true, Text = "웨일 확장앱 관리자의 확장앱 ID" }); layout.Controls.Add(extension);
        layout.Controls.Add(new Label { AutoSize = true, Text = "Native 호스트 등록 경로 (웨일 설치 환경에 맞게 변경)" }); layout.Controls.Add(registry);
        var install = new Button { Text = "현재 사용자에 연결 등록", AutoSize = true, Height = 44 };
        var uninstall = new Button { Text = "연결 등록 해제", AutoSize = true, Height = 44 };
        install.Click += (_, _) => Register(false); uninstall.Click += (_, _) => Register(true);
        layout.Controls.Add(install); layout.Controls.Add(uninstall); layout.Controls.Add(status); Controls.Add(layout);
    }
    private void Register(bool remove)
    {
        try
        {
            var id = extension.Text.Trim();
            if (id.Length != 32 || id.Any(c => c < 'a' || c > 'p')) throw new InvalidOperationException("확장앱 ID 32자를 확인하세요.");
            var baseKey = registry.Text.Trim().TrimEnd('\\');
            if (!baseKey.StartsWith(@"Software\Naver\", StringComparison.OrdinalIgnoreCase) ||
                !baseKey.EndsWith(@"\NativeMessagingHosts", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Naver 아래의 NativeMessagingHosts 경로만 등록할 수 있습니다.");
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WhaleDualInput");
            var manifest = Path.Combine(dir, "native-host.json");
            var keyName = baseKey + "\\" + Program.HostName;
            if (remove)
            {
                using var key = Registry.CurrentUser.OpenSubKey(keyName);
                if (key?.GetValue("") as string != manifest) throw new InvalidOperationException("이 프로그램이 등록한 연결이 아닙니다.");
                Registry.CurrentUser.DeleteSubKey(keyName, false);
                status.Text = "연결 등록을 해제했습니다. 프로그램 폴더는 직접 삭제할 수 있습니다."; return;
            }
            Directory.CreateDirectory(dir);
            var value = new { name = Program.HostName, description = "Whale Dual Input", path = Environment.ProcessPath,
                type = "stdio", allowed_origins = new[] { "chrome-extension://" + id + "/" } };
            File.WriteAllText(manifest, JsonSerializer.Serialize(value));
            using var registration = Registry.CurrentUser.CreateSubKey(keyName);
            registration.SetValue("", manifest);
            status.Text = "등록했습니다. 웨일 사이드바에서 Windows 연결을 누르세요. 연결 실패 시 웨일의 등록 경로를 확인하세요.";
        }
        catch (Exception error) { status.Text = error.Message; }
    }
}
