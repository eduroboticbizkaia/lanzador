using Velopack;
using Velopack.Windows;

namespace Lanzador;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Debe ejecutarse lo antes posible: gestiona instalación/actualización/desinstalación.
        // Velopack crea/borra automáticamente los accesos de Escritorio y Menú Inicio; el de
        // "Inicio de Windows" (Startup) hay que gestionarlo a mano con la API (marcada obsoleta
        // solo porque ya no hace falta para Desktop/StartMenuRoot, pero sigue siendo la forma
        // soportada de gestionar ShortcutLocation.Startup).
#pragma warning disable CS0618
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ =>
            {
                try
                {
                    new Shortcuts().CreateShortcutForThisExe(ShortcutLocation.Startup);
                }
                catch
                {
                    // No crítico: si falla, el usuario puede crear el acceso directo a mano
                    // en shell:startup apuntando al ejecutable instalado.
                }
            })
            .OnBeforeUninstallFastCallback(_ =>
            {
                try
                {
                    new Shortcuts().RemoveShortcutForThisExe(ShortcutLocation.Startup);
                }
                catch
                {
                    // No crítico.
                }
            })
            .Run();
#pragma warning restore CS0618

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayForm());
    }
}
