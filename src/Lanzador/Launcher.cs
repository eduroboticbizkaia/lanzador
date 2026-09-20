using System.Diagnostics;
using Lanzador.Models;

namespace Lanzador;

public static class Launcher
{
    public static void Open(MenuEntry entry, Action<string, string>? onError = null)
    {
        if (string.IsNullOrWhiteSpace(entry.Target))
        {
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = entry.Target,
                UseShellExecute = true,
            };

            if (entry.Type == MenuEntryType.App && !string.IsNullOrWhiteSpace(entry.Args))
            {
                psi.Arguments = entry.Args;
            }

            Process.Start(psi);
        }
        catch (Exception ex)
        {
            onError?.Invoke(entry.Name, ex.Message);
        }
    }
}
