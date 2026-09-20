using System.Drawing.Drawing2D;
using Lanzador.Models;

namespace Lanzador;

public static class IconResolver
{
    public static Icon GetTrayIcon()
        => Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;

    public static Icon LoadLinkIcon(int size = 32)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "link.ico");
        return File.Exists(path) ? new Icon(path, size, size) : SystemIcons.Application;
    }

    /// <summary>Icono a mostrar en el botón de un acceso: icon propio &gt; icono del .exe &gt; icono genérico de enlace.</summary>
    public static Image? GetEntryImage(MenuEntry entry, Icon linkIcon, int size = 24)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(entry.Icon))
            {
                var iconPath = Path.IsPathRooted(entry.Icon)
                    ? entry.Icon
                    : Path.Combine(AppContext.BaseDirectory, entry.Icon);

                if (File.Exists(iconPath))
                {
                    using var custom = new Icon(iconPath, size, size);
                    return custom.ToBitmap();
                }
            }

            if (entry.Type == MenuEntryType.App && !string.IsNullOrWhiteSpace(entry.Target) && File.Exists(entry.Target))
            {
                using var icon = Icon.ExtractAssociatedIcon(entry.Target);
                return icon is null ? null : Resize(icon, size);
            }

            if (entry.Type == MenuEntryType.Url)
            {
                return Resize(linkIcon, size);
            }
        }
        catch
        {
            // Icono opcional: si falla la carga, el botón se muestra sin icono.
        }

        return null;
    }

    public static Bitmap Resize(Icon icon, int size)
    {
        using var bmp = icon.ToBitmap();
        var resized = new Bitmap(size, size);
        using var g = Graphics.FromImage(resized);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(bmp, 0, 0, size, size);
        return resized;
    }
}
