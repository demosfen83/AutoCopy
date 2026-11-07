namespace AutoCopy;
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, "Local\\AutoCopy.MVP", out var first);
        if (!first) { MessageBox.Show("AutoCopy уже запущен.", "AutoCopy"); return; }
        ApplicationConfiguration.Initialize();
        try { Application.Run(new TrayContext()); }
        catch (Exception e) { MessageBox.Show($"Не удалось запустить AutoCopy: {e.Message}", "AutoCopy"); }
    }
}
