using Microsoft.Win32;
namespace AutoCopy;
internal sealed class SettingsForm : Form
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunName = "AutoCopy";
    private readonly AppSettings settings;
    private readonly Action save;
    private readonly ListBox list = new() { Dock = DockStyle.Fill };
    internal SettingsForm(AppSettings settings, Action save)
    {
        this.settings = settings; this.save = save;
        Text = "Настройки AutoCopy"; Width = 520; Height = 400; StartPosition = FormStartPosition.CenterScreen;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var tip = new CheckBox { Text = "Показывать уведомление при копировании", AutoSize = true, Checked = settings.Tip };
        var sound = new CheckBox { Text = "Воспроизводить звук при копировании", AutoSize = true, Checked = settings.Sound };
        var startup = new CheckBox { Text = "Запускать вместе с Windows", AutoSize = true };
        try { using var key = Registry.CurrentUser.OpenSubKey(RunKey); var value = key?.GetValue(RunName) as string; startup.Checked = value == QuotedPath; if (!string.IsNullOrEmpty(value) && value != QuotedPath) MessageBox.Show("Автозапуск указывает на другой путь. Включите его заново после переноса папки.", "AutoCopy"); }
        catch { startup.Enabled = false; MessageBox.Show("Не удалось прочитать настройку автозапуска.", "AutoCopy"); }
        tip.CheckedChanged += (_, _) => { settings.Tip = tip.Checked; save(); };
        sound.CheckedChanged += (_, _) => { settings.Sound = sound.Checked; save(); };
        startup.CheckedChanged += (_, _) => { try { using var key = Registry.CurrentUser.CreateSubKey(RunKey); if (startup.Checked) key.SetValue(RunName, QuotedPath); else if ((key.GetValue(RunName) as string) == QuotedPath) key.DeleteValue(RunName, false); } catch { MessageBox.Show("Не удалось изменить автозапуск.", "AutoCopy"); startup.Enabled = false; } };
        layout.Controls.Add(tip); layout.Controls.Add(sound); layout.Controls.Add(startup); layout.Controls.Add(new Label { Text = "Исключённые приложения", AutoSize = true }); layout.Controls.Add(list);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        var add = new Button { Text = "Добавить…", AutoSize = true }; var remove = new Button { Text = "Удалить", AutoSize = true };
        add.Click += (_, _) => { using var picker = new System.Windows.Forms.OpenFileDialog { Filter = "Приложения (*.exe)|*.exe" }; if (picker.ShowDialog(this) == DialogResult.OK && !settings.IsExcluded(picker.FileName)) { settings.Exclusions.Add(Path.GetFileName(picker.FileName)); RefreshList(); save(); } };
        remove.Click += (_, _) => { if (list.SelectedItem is string selected) { settings.Exclusions.Remove(selected); RefreshList(); save(); } };
        buttons.Controls.Add(add); buttons.Controls.Add(remove); layout.Controls.Add(buttons);
        var close = new Button { Text = "Закрыть", AutoSize = true, Anchor = AnchorStyles.Right }; close.Click += (_, _) => Close(); layout.Controls.Add(close);
        Controls.Add(layout); RefreshList();
    }
    private static string QuotedPath => $"\"{Environment.ProcessPath}\"";
    private void RefreshList() { list.Items.Clear(); list.Items.AddRange(settings.Exclusions.ToArray()); }
}

