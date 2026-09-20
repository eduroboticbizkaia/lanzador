using System.Threading;
using Velopack;
using Velopack.Sources;

namespace Lanzador;

public sealed class UpdateService
{
    public event Action<string>? StatusChanged;

    private readonly UpdateManager? _manager;

    public UpdateService()
    {
        try
        {
            var source = new GithubSource(AppSettings.UpdateRepoUrl, AppSettings.UpdateRepoToken, AppSettings.AllowPrerelease);
            _manager = new UpdateManager(source);
        }
        catch (Exception ex)
        {
            // P.ej. si la app no se instaló con Velopack (modo desarrollo/debug): no hay updates.
            StatusChanged?.Invoke($"Auto-actualización no disponible: {ex.Message}");
            _manager = null;
        }
    }

    public bool IsInstalledViaVelopack => _manager?.IsInstalled ?? false;

    /// <summary>Comprueba, descarga y deja lista la actualización. No reinicia la app.</summary>
    public async Task<UpdateInfo?> CheckAndDownloadAsync(CancellationToken ct = default)
    {
        if (_manager is null || !_manager.IsInstalled)
        {
            return null;
        }

        try
        {
            var newVersion = await _manager.CheckForUpdatesAsync().WaitAsync(ct);
            if (newVersion is null)
            {
                StatusChanged?.Invoke("No hay actualizaciones nuevas.");
                return null;
            }

            StatusChanged?.Invoke($"Descargando actualización {newVersion.TargetFullRelease.Version}...");
            await _manager.DownloadUpdatesAsync(newVersion).WaitAsync(ct);
            StatusChanged?.Invoke("Actualización descargada. Se aplicará al reiniciar.");
            return newVersion;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Error comprobando actualizaciones: {ex.Message}");
            return null;
        }
    }

    public void ApplyUpdateAndRestart(UpdateInfo updateInfo)
    {
        _manager?.ApplyUpdatesAndRestart(updateInfo);
    }
}
