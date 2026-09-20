using System.Text.Json.Serialization;

namespace Lanzador.Models;

public enum MenuEntryType
{
    App,
    Url,
    Folder,
    Separator
}

public sealed class MenuEntry
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MenuEntryType Type { get; set; } = MenuEntryType.Url;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Ruta al ejecutable (type=app) o URL (type=url). No se usa en folder/separator.</summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    /// <summary>Argumentos opcionales para lanzar el ejecutable (solo type=app).</summary>
    [JsonPropertyName("args")]
    public string? Args { get; set; }

    /// <summary>Ruta opcional a un icono .ico/.exe propio para este elemento.</summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>Elementos hijos, solo para type=folder.</summary>
    [JsonPropertyName("items")]
    public List<MenuEntry>? Items { get; set; }
}

public sealed class AppConfig
{
    [JsonPropertyName("items")]
    public List<MenuEntry> Items { get; set; } = new();
}
