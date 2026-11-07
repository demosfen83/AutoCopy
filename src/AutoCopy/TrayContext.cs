using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Media;
namespace AutoCopy;
internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon icon;
    private MediaPlayer? soundPlayer;
    private readonly Icon activeIcon = LoadIcon("active");
    private readonly Icon pausedIcon = LoadIcon("paused");
    private readonly System.Windows.Forms.Timer timer = new();
    private readonly System.Windows.Forms.Timer foregroundTimer = new() { Interval = 400 };
    private readonly Native.HookProc callback;
    private readonly string settingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");
    private AppSettings settings = new();
    private nint hook, downWindow, targetWindow;
    private Native.Point downPoint, targetPoint, previousPoint;
    private long previousClick;
    private int clicks, generation;
    private bool paused, dragged;
    private volatile bool busy;
    private string? lastApplication;
    private readonly ToolStripMenuItem excludeLast = new("Исключить последнее приложение");
    internal TrayContext()
    {
        if (File.Exists(settingsPath))
            try { settings = AppSettings.Load(settingsPath); if (settings.Exclusions == null) throw new InvalidDataException(); }
            catch { settings = new(); MessageBox.Show("Не удалось прочитать настройки. Используются значения по умолчанию; проверьте файл settings.json.", "AutoCopy"); }
        var menu = new ContextMenuStrip();
        menu.Opening += (_, _) => { excludeLast.Enabled = lastApplication != null; excludeLast.Text = lastApplication == null ? "Исключить последнее приложение" : $"Исключить {Path.GetFileName(lastApplication)}"; };
        excludeLast.Click += (_, _) => { if (lastApplication != null && !settings.IsExcluded(lastApplication)) { settings.Exclusions.Add(Path.GetFileName(lastApplication)); Save(); } };
        menu.Items.Add(excludeLast);
        menu.Items.Add("Настройки", null, (_, _) => OpenSettings());
        menu.Items.Add("Выход", null, (_, _) => ExitThread());
        icon = new NotifyIcon { Icon = activeIcon, Text = "AutoCopy: работает", ContextMenuStrip = menu, Visible = true };
        icon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) { paused = !paused; generation++; timer.Stop(); icon.Icon = paused ? pausedIcon : activeIcon; icon.Text = paused ? "AutoCopy: пауза" : "AutoCopy: работает"; } };
        foregroundTimer.Tick += (_, _) => RememberForeground(); foregroundTimer.Start();
        timer.Interval = SystemInformation.DoubleClickTime;
        timer.Tick += (_, _) => { timer.Stop(); StartRead(); };
        callback = OnMouse;
        hook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, callback, nint.Zero, 0);
        if (hook == 0) { icon.Visible = false; throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Не удалось отслеживать мышь."); }
    }
    private static Icon LoadIcon(string name)
    {
        using var stream = typeof(TrayContext).Assembly.GetManifestResourceStream($"AutoCopy.Assets.{name}.ico")
            ?? throw new InvalidOperationException("Не найден значок AutoCopy.");
        return new Icon(stream);
    }
    private nint OnMouse(int code, nint message, nint data)
    {
        if (code >= 0 && (message == Native.WM_LBUTTONDOWN || message == Native.WM_LBUTTONUP))
        {
            var mouse = Marshal.PtrToStructure<Native.MouseData>(data);
            if ((mouse.Flags & 1) == 0)
            {
                if (message == Native.WM_LBUTTONDOWN)
                {
                    generation++; timer.Stop(); downPoint = mouse.Point; downWindow = Native.RootAt(mouse.Point);
                    var now = Environment.TickCount64;
                    if (downWindow == targetWindow && now - previousClick <= SystemInformation.DoubleClickTime && Math.Abs(mouse.Point.X - previousPoint.X) <= SystemInformation.DoubleClickSize.Width && Math.Abs(mouse.Point.Y - previousPoint.Y) <= SystemInformation.DoubleClickSize.Height) clicks++;
                    else clicks = 1;
                    previousClick = now; previousPoint = mouse.Point;
                }
                else
                {
                    targetPoint = mouse.Point; targetWindow = Native.RootAt(mouse.Point);
                    dragged = Math.Abs(targetPoint.X - downPoint.X) > SystemInformation.DragSize.Width / 2 || Math.Abs(targetPoint.Y - downPoint.Y) > SystemInformation.DragSize.Height / 2;
                    if (!paused && GestureRules.ShouldRead(clicks, dragged, downWindow != 0 && downWindow == targetWindow)) { timer.Stop(); timer.Start(); }
                }
            }
        }
        return Native.CallNextHookEx(hook, code, message, data);
    }
    private void RememberForeground()
    {
        var path = Native.ProcessPath(Native.GetForegroundWindow());
        if (path != null && !path.Equals(Environment.ProcessPath, StringComparison.OrdinalIgnoreCase)) lastApplication = path;
    }
    private async void StartRead()
    {
        var window = targetWindow; var point = targetPoint; var ticket = generation;
        if (busy || paused || window == 0 || Native.GetForegroundWindow() != window || !Native.IsWindow(window)) return;
        var path = Native.ProcessPath(window);
        if (path == null || path.Equals(Environment.ProcessPath, StringComparison.OrdinalIgnoreCase)) return;
        lastApplication = path;
        if (settings.IsExcluded(path)) return;
        busy = true;
        var timedOut = false;
        try
        {
            var read = Task.Run(() => SelectionReader.Read(point, window));
            if (await Task.WhenAny(read, Task.Delay(2000)) != read) { timedOut = true; _ = read.ContinueWith(_ => { busy = false; }); return; }
            var result = await read;
            if (ticket != generation || paused || Native.GetForegroundWindow() != window || !Native.IsWindow(window)) return;
            bool copied;
            try { copied = CopyRules.Copy(result, text => Clipboard.SetText(text, TextDataFormat.UnicodeText)); }
            catch { icon.ShowBalloonTip(2000, "AutoCopy", "Не удалось записать в буфер обмена.", ToolTipIcon.Warning); return; }
            if (!copied) return;
            if (settings.Tip) icon.ShowBalloonTip(800, "AutoCopy", "Скопировано", ToolTipIcon.None);
            if (settings.Sound) PlayClick();
        }
        finally { if (!timedOut) busy = false; }
    }
    private void PlayClick()
    {
        try
        {
            if (soundPlayer == null)
            {
                soundPlayer = new MediaPlayer();
                soundPlayer.MediaFailed += (_, _) => icon.ShowBalloonTip(2000, "AutoCopy", "Не удалось воспроизвести звук.", ToolTipIcon.Warning);
                soundPlayer.Open(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "mouse-click.mp3")));
            }
            soundPlayer.Stop();
            soundPlayer.Play();
        }
        catch { icon.ShowBalloonTip(2000, "AutoCopy", "Не удалось воспроизвести звук.", ToolTipIcon.Warning); }
    }
    private void Save() { try { settings.Save(settingsPath); } catch { MessageBox.Show("Не удалось сохранить настройки рядом с программой. Изменения действуют до выхода.", "AutoCopy"); } }
    private void OpenSettings()
    {
        using var dialog = new SettingsForm(settings, Save);
        dialog.ShowDialog();
    }
    protected override void ExitThreadCore()
    {
        generation++; timer.Stop(); if (hook != 0) Native.UnhookWindowsHookEx(hook); hook = 0;
        soundPlayer?.Close(); icon.Visible = false; icon.Dispose(); activeIcon.Dispose(); pausedIcon.Dispose(); timer.Dispose(); foregroundTimer.Dispose(); base.ExitThreadCore();
    }
}




