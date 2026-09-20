using Lanzador.Models;

namespace Lanzador;

/// <summary>
/// Panel flotante (estilo flyout) con un botón por cada acceso configurado.
/// Se abre junto al icono de la bandeja y se cierra solo al perder el foco.
/// </summary>
public sealed class LauncherWindow : Form
{
    private const int PanelWidth = 300;

    private readonly Icon _linkIcon;
    private readonly FlowLayoutPanel _content;

    public LauncherWindow(AppConfig config, Icon linkIcon)
    {
        _linkIcon = linkIcon;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.FromArgb(210, 210, 210); // se ve como borde de 1px alrededor del contenido
        Padding = new Padding(1);
        KeyPreview = true;

        _content = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(PanelWidth, 0),
            MaximumSize = new Size(PanelWidth, Screen.PrimaryScreen!.WorkingArea.Height - 60),
            BackColor = Color.White,
            Padding = new Padding(8),
            Location = new Point(1, 1),
        };

        var title = new Label
        {
            Text = "Accesos rápidos",
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 60, 60),
            AutoSize = true,
            Margin = new Padding(4, 2, 4, 10),
        };
        _content.Controls.Add(title);

        BuildEntries(config.Items, 0);

        Controls.Add(_content);

        Deactivate += (_, _) => Hide();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Hide();
            }
        };
    }

    private void BuildEntries(List<MenuEntry> entries, int indent)
    {
        foreach (var entry in entries)
        {
            switch (entry.Type)
            {
                case MenuEntryType.Separator:
                    _content.Controls.Add(new Panel
                    {
                        Height = 1,
                        Width = PanelWidth - 16,
                        BackColor = Color.FromArgb(230, 230, 230),
                        Margin = new Padding(4, 8, 4, 8),
                    });
                    break;

                case MenuEntryType.Folder:
                    _content.Controls.Add(new Label
                    {
                        Text = entry.Name,
                        Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
                        ForeColor = Color.Gray,
                        AutoSize = true,
                        Margin = new Padding(4 + indent, 10, 4, 4),
                    });
                    BuildEntries(entry.Items ?? new List<MenuEntry>(), indent + 14);
                    break;

                case MenuEntryType.App:
                case MenuEntryType.Url:
                    _content.Controls.Add(BuildButton(entry, indent));
                    break;
            }
        }
    }

    private Button BuildButton(MenuEntry entry, int indent)
    {
        var button = new Button
        {
            Text = "  " + entry.Name,
            AutoSize = false,
            Width = PanelWidth - 16 - indent,
            Height = 42,
            TextAlign = ContentAlignment.MiddleLeft,
            ImageAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Padding = new Padding(10, 0, 6, 0),
            Margin = new Padding(indent, 2, 4, 2),
            FlatStyle = FlatStyle.Flat,
            Image = IconResolver.GetEntryImage(entry, _linkIcon),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            BackColor = Color.White,
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(225, 225, 225);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(237, 243, 255);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 232, 255);

        button.Click += (_, _) =>
        {
            Hide();
            Launcher.Open(entry, (name, error) =>
                MessageBox.Show(this, error, $"No se pudo abrir \"{name}\"", MessageBoxButtons.OK, MessageBoxIcon.Warning));
        };

        return button;
    }

    /// <summary>Muestra el panel anclado cerca del punto indicado (normalmente la posición del cursor sobre la bandeja).</summary>
    public void ShowNear(Point anchor)
    {
        Size = PreferredSize;

        var workArea = Screen.FromPoint(anchor).WorkingArea;
        var x = Math.Min(anchor.X, workArea.Right - Width - 8);
        var y = Math.Min(anchor.Y - Height - 8, workArea.Bottom - Height - 8);
        x = Math.Max(x, workArea.Left + 8);
        y = Math.Max(y, workArea.Top + 8);

        Location = new Point(x, y);
        Show();
        Activate();
    }
}
