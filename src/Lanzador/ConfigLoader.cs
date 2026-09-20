using System.Text.Json;
using Lanzador.Models;

namespace Lanzador;

/// <summary>
/// Carga el listado de accesos desde config.json, que va compilado como recurso embebido
/// dentro del propio ejecutable (ver Lanzador.csproj). No es editable por quien usa la app:
/// para cambiar los enlaces hay que editar el archivo fuente y publicar una nueva versión,
/// que llegará sola a los equipos ya instalados mediante la auto-actualización.
/// </summary>
public static class ConfigLoader
{
    private const string ResourceName = "Lanzador.config.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static AppConfig Load()
    {
        try
        {
            var assembly = typeof(ConfigLoader).Assembly;
            using var stream = assembly.GetManifestResourceStream(ResourceName);
            if (stream is null)
            {
                return new AppConfig();
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (Exception)
        {
            // Config inválido: mejor un panel vacío que un crash.
            return new AppConfig();
        }
    }
}
