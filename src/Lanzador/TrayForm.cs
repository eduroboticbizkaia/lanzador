using System.Diagnostics;
using Velopack;

namespace Lanzador;

public sealed class TrayForm : Form
{
    private readonly NotifyIcon _notifyIcon;
    private readonly UpdateService _updateService = new();
    private readonly Icon _linkIcon;
    private readonly LauncherWindow _launcherWindow;

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

        _linkIcon = IconResolver.LoadLinkIcon();
        _launcherWindow = new LauncherWindow(ConfigLoader.Load(), _linkIcon);

        _notifyIcon = new NotifyIcon
        {
            Icon = IconResolver.GetTrayIcon(),
            Text = "Lanzador",
            Visible = true,
        };
        _notifyIcon.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ToggleLauncher();
            }
        };

        _updateService.StatusChanged += status => Debug.WriteLine($"[Update] {status}");

        RebuildAdminMenu();

        _updateTimer = new System.Threading.Timer(
            async _ => await CheckForUpdatesAsync(),
            null,
            TimeSpan.FromSeconds(15),
            AppSettings.UpdateCheckInterval);
    }

    protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);

    // ---------- Panel de accesos ----------

    private void ToggleLauncher()
    {
        if (_launcherWindow.Visible)
        {
            _launcherWindow.Hide();
        }
        else
        {
            _launcherWindow.ShowNear(Cursor.Position);
        }
    }

    // ---------- Menú de administración (clic derecho) ----------

    private void RebuildAdminMenu()
    {
        var menu = new ContextMenuStrip();

        if (_pendingUpdate != null)
        {
            var updateItem = new ToolStripMenuItem("Reiniciar y actualizar ahora");
            updateItem.Click += (_, _) => _updateService.ApplyUpdateAndRestart(_pendingUpdate!);
            menu.Items.Add(updateItem);
            menu.Items.Add(new ToolStripSeparator());
        }

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
                    RebuildAdminMenu();
                    _notifyIcon.ShowBalloonTip(
                        5000,
                        "Actualización disponible",
                        "Se ha descargado una nueva versión de Lanzador. Haz clic derecho en el icono y elige \"Reiniciar y actualizar ahora\" cuando quieras, o se aplicará sola al reiniciar.",
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
            _updateTimer?.Dispose();
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
            _launcherWindow.Dispose();
            _linkIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
