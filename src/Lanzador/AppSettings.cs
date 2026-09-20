namespace Lanzador;

public static class AppSettings
{
    /// <summary>
    /// URL del repositorio de GitHub donde se publican las releases (vpk upload github).
    /// Cámbiala por tu repositorio antes de compilar/publicar.
    /// </summary>
    public const string UpdateRepoUrl = "https://github.com/eduroboticbizkaia/lanzador";

    /// <summary>
    /// Token de acceso a GitHub, solo necesario si el repositorio es privado.
    /// Déjalo en null para repos públicos.
    /// </summary>
    public const string? UpdateRepoToken = null;

    /// <summary>Incluir prereleases de GitHub como actualizaciones válidas.</summary>
    public const bool AllowPrerelease = false;

    /// <summary>Cada cuánto se comprueba si hay una versión nueva.</summary>
    public static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);
}
