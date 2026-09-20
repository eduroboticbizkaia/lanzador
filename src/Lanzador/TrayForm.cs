using System.Diagnostics;
using System.Drawing.Drawing2D;
using Lanzador.Models;
using Velopack;

namespace Lanzador;

public sealed class TrayForm : Form
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ConfigManager _configManager = new();
    private readonly UpdateService _updateService = new();
    private readonly Icon _linkIcon;

    private FileSystemWatcher? _watcher;
    private System.Threading.Timer? _reloadDebounceTimer;
    private System.Threading.Timer? _updateTimer;
    private UpdateInfo? _pendingUpdate;
    private bool _isCheckingUpdate;

    public TrayForm()
    {
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-2000, -2000);
        Size = new Size(1, 1);

        _linkIcon = LoadEmbeddedLinkIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = ResolveTrayIcon(),
            Text = "Lanzador",
            Visible = true,
        };

        _updateService.StatusChanged += status => Debug.WriteLine($"[Update] {status}");

        RebuildMenu();
        SetupConfigWatcher();

        _updateTimer = new System.Threading.Timer(
            async _ => await CheckForUpdatesAsync(),
            null,
            TimeSpan.FromSeconds(15),
            AppSettings.UpdateCheckInterval);
    }

    protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);

    // ---------- Menú ----------

    private void RebuildMenu()
    {
        var config = _configManager.Load();
        var menu = new ContextMenuStrip();

        foreach (var entry in config.Items)
        {
            var item = BuildMenuItem(entry);
            if (item != null)
            {
                menu.Items.Add(item);
            }
        }

        if (config.Items.Count > 0)
        {
            menu.Items.Add(new ToolStripSeparator());
        }

        if (_pendingUpdate != null)
        {
            var updateItem = new ToolStripMenuItem("Reiniciar y actualizar ahora");
            updateItem.Click += (_, _) => _updateService.ApplyUpdateAndRestart(_pendingUpdate!);
            menu.Items.Add(updateItem);
            menu.Items.Add(new ToolStripSeparator());
        }

        var editItem = new ToolStripMenuItem("Editar configuración...");
        editItem.Click += (_, _) => OpenConfigForEditing();
        menu.Items.Add(editItem);

        var reloadItem = new ToolStripMenuItem("Recargar menú");
        reloadItem.Click += (_, _) => RebuildMenu();
        menu.Items.Add(reloadItem);

        var checkUpdateItem = new ToolStripMenuItem("Buscar actualizaciones ahora");
        checkUpdateItem.Click += async (_, _) => await CheckForUpdatesAsync();
        menu.Items.Add(checkUpdateItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Salir");
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        var old = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = menu;
        old?.Dispose();
    }

    private ToolStripItem? BuildMenuItem(MenuEntry entry)
    {
        switch (entry.Type)
        {
            case MenuEntryType.Separator:
                return new ToolStripSeparator();

            case MenuEntryType.Folder:
                var folder = new ToolStripMenuItem(entry.Name);
                foreach (var child in entry.Items ?? new List<MenuEntry>())
                {
                    var childItem = BuildMenuItem(child);
                    if (childItem != null)
                    {
                        folder.DropDownItems.Add(childItem);
                    }
                }
                return folder;

            case MenuEntryType.App:
            case MenuEntryType.Url:
                var item = new ToolStripMenuItem(entry.Name) { Image = GetIconImage(entry) };
                item.Click += (_, _) => Launch(entry);
                return item;

            default:
                return null;
        }
    }

    private void Launch(MenuEntry entry)
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
            _notifyIcon.ShowBalloonTip(4000, $"No se pudo abrir \"{entry.Name}\"", ex.Message, ToolTipIcon.Error);
        }
    }

    private void OpenConfigForEditing()
    {
        _configManager.EnsureConfigExists();
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _configManager.ConfigPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(4000, "No se pudo abrir la configuración", ex.Message, ToolTipIcon.Error);
        }
    }

    // ---------- Iconos ----------

    private static Icon ResolveTrayIcon()
    {
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
    }

    private static Icon LoadEmbeddedLinkIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "link.ico");
        return File.Exists(path) ? new Icon(path, 16, 16) : SystemIcons.Application;
    }

    private Image? GetIconImage(MenuEntry entry)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(entry.Icon) && File.Exists(entry.Icon))
            {
                using var icon = new Icon(entry.Icon, 16, 16);
                return icon.ToBitmap();
            }

            if (entry.Type == MenuEntryType.App && !string.IsNullOrWhiteSpace(entry.Target) && File.Exists(entry.Target))
            {
                using var icon = Icon.ExtractAssociatedIcon(entry.Target);
                return icon is null ? null : ResizeToMenuIcon(icon);
            }

            if (entry.Type == MenuEntryType.Url)
            {
                return ResizeToMenuIcon(_linkIcon);
            }
        }
        catch
        {
            // Icono opcional: si falla la carga, el elemento se muestra sin icono.
        }

        return null;
    }

    private static Bitmap ResizeToMenuIcon(Icon icon, int size = 16)
    {
        using var bmp = icon.ToBitmap();
        var resized = new Bitmap(size, size);
        using var g = Graphics.FromImage(resized);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(bmp, 0, 0, size, size);
        return resized;
    }

    // ---------- Config en caliente ----------

    private void SetupConfigWatcher()
    {
        _configManager.EnsureConfigExists();
        _watcher = new FileSystemWatcher(_configManager.ConfigDirectory, "config.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += (_, _) => DebounceReload();
        _watcher.Renamed += (_, _) => DebounceReload();
    }

    private void DebounceReload()
    {
        _reloadDebounceTimer?.Dispose();
        _reloadDebounceTimer = new System.Threading.Timer(_ => BeginInvoke(RebuildMenu), null, 400, System.Threading.Timeout.Infinite);
    }

    // ---------- Actualizaciones ----------

    private async Task CheckForUpdatesAsync()
    {
        if (_isCheckingUpdate)
        {
            return;
        }

        _isCheckingUpdate = true;
        try
        {
            var info = await _updateService.CheckAndDownloadAsync();
            if (info != null)
            {
                _pendingUpdate = info;
                BeginInvoke(() =>
                {
                    RebuildMenu();
                    _notifyIcon.ShowBalloonTip(
                        5000,
                        "Actualización disponible",
                        "Se ha descargado una nueva versión de Lanzador. Usa \"Reiniciar y actualizar ahora\" en el menú cuando quieras.",
                        ToolTipIcon.Info);
                });
            }
        }
        finally
        {
            _isCheckingUpdate = false;
        }
    }

    // ---------- Salida ----------

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _watcher?.Dispose();
            _reloadDebounceTimer?.Dispose();
            _updateTimer?.Dispose();
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
            _linkIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
