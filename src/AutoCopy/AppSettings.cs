using System.Text.Json;
namespace AutoCopy;
public sealed class AppSettings
{
    public bool Tip { get; set; } = true;
    public bool Sound { get; set; }
    public List<string> Exclusions { get; set; } = ["explorer.exe"];
    public bool IsExcluded(string path) => Exclusions.Any(item => string.Equals(item, Path.GetFileName(path), StringComparison.OrdinalIgnoreCase) || string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
    public static AppSettings Load(string path) => JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? throw new InvalidDataException("Пустой файл настроек.");
    public void Save(string path)
    {
        var temp = path + ".tmp";
        try { File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
