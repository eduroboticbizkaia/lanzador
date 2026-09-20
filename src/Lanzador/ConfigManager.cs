using System.Text.Json;
using Lanzador.Models;

namespace Lanzador;

public sealed class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string ConfigDirectory { get; }
    public string ConfigPath { get; }

    public ConfigManager()
    {
        ConfigDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Lanzador");
        ConfigPath = Path.Combine(ConfigDirectory, "config.json");
    }

    public void EnsureConfigExists()
    {
        if (File.Exists(ConfigPath))
        {
            return;
        }

        Directory.CreateDirectory(ConfigDirectory);

        var sampleSourcePath = Path.Combine(AppContext.BaseDirectory, "config.sample.json");
        if (File.Exists(sampleSourcePath))
        {
            File.Copy(sampleSourcePath, ConfigPath);
        }
        else
        {
            var empty = new AppConfig();
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(empty, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public AppConfig Load()
    {
        EnsureConfigExists();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            return config ?? new AppConfig();
        }
        catch (Exception)
        {
            // Config corrupto o con JSON inválido: mejor un menú vacío que un crash.
            return new AppConfig();
        }
    }
}
