using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace mmtray;

internal sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ProfileManager _profiles = new();
    private readonly Icon _icon;
    private IntPtr _iconHandle;

    public TrayApp()
    {
        _icon = CreateMonitorIcon(out _iconHandle);
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "MonitorMonitor",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
        _notifyIcon.MouseUp += OnTrayMouseUp;
    }

    private void OnTrayMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            _notifyIcon.ContextMenuStrip = BuildMenu();
        }
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new NavyMenuRenderer(),
            BackColor = Palette.Navy,
            ForeColor = Palette.Beige,
            ShowImageMargin = false,
            Font = new Font("Segoe UI", 9f),
        };

        var profiles = _profiles.ListProfiles()
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var loadItem = new ToolStripMenuItem("Load Profile");
        if (profiles.Count == 0)
        {
            loadItem.DropDownItems.Add(new ToolStripMenuItem("(no profiles)") { Enabled = false });
        }
        else
        {
            foreach (var profile in profiles)
            {
                var name = profile;
                var item = new ToolStripMenuItem(name);
                item.Click += (_, _) => OnLoad(name);
                loadItem.DropDownItems.Add(item);
            }
        }
        menu.Items.Add(loadItem);

        var saveItem = new ToolStripMenuItem("Save Profile");
        var saveAs = new ToolStripMenuItem("Save as new...");
        saveAs.Click += (_, _) => OnSaveAs();
        saveItem.DropDownItems.Add(saveAs);
        if (profiles.Count > 0)
        {
            saveItem.DropDownItems.Add(new ToolStripSeparator());
            foreach (var profile in profiles)
            {
                var name = profile;
                var item = new ToolStripMenuItem($"Overwrite \"{name}\"");
                item.Click += (_, _) => OnSave(name, confirmOverwrite: false);
                saveItem.DropDownItems.Add(item);
            }
        }
        menu.Items.Add(saveItem);

        var deleteItem = new ToolStripMenuItem("Delete Profile");
        if (profiles.Count == 0)
        {
            deleteItem.DropDownItems.Add(new ToolStripMenuItem("(no profiles)") { Enabled = false });
        }
        else
        {
            foreach (var profile in profiles)
            {
                var name = profile;
                var item = new ToolStripMenuItem(name);
                item.Click += (_, _) => OnDelete(name);
                deleteItem.DropDownItems.Add(item);
            }
        }
        menu.Items.Add(deleteItem);

        menu.Items.Add(new ToolStripSeparator());

        var showItem = new ToolStripMenuItem("Show Current Setup");
        showItem.Click += (_, _) => OnShowCurrent();
        menu.Items.Add(showItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        StyleSubmenus(menu);
        return menu;
    }

    private static void StyleSubmenus(ToolStrip strip)
    {
        foreach (ToolStripItem item in strip.Items)
        {
            if (item is ToolStripMenuItem mi && mi.HasDropDownItems)
            {
                mi.DropDown.BackColor = Palette.Navy;
                mi.DropDown.ForeColor = Palette.Beige;
                if (mi.DropDown is ToolStripDropDownMenu menu)
                {
                    menu.ShowImageMargin = false;
                }
                StyleSubmenus(mi.DropDown);
            }
        }
    }

    private void OnLoad(string name)
    {
        var monitors = _profiles.LoadProfile(name, out var error);
        if (monitors == null)
        {
            Notify("Load failed", error ?? "Unknown error", ToolTipIcon.Error);
            return;
        }

        bool ok = MonitorConfiguration.ApplyConfiguration(monitors);
        if (ok)
        {
            Notify("Profile loaded", $"Applied '{name}'.");
        }
        else
        {
            Notify("Load completed with warnings", $"Some settings in '{name}' may not have been applied.", ToolTipIcon.Warning);
        }
    }

    private void OnSaveAs()
    {
        using var dialog = new NameInputDialog();
        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        var name = dialog.ProfileName;
        if (string.IsNullOrWhiteSpace(name))
        {
            Notify("Save cancelled", "Profile name was empty.", ToolTipIcon.Warning);
            return;
        }

        if (!IsValidProfileName(name))
        {
            MessageBox.Show("Profile name contains invalid characters.", "Invalid name",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        OnSave(name, confirmOverwrite: true);
    }

    private void OnSave(string name, bool confirmOverwrite)
    {
        if (confirmOverwrite && _profiles.ProfileExists(name))
        {
            var result = MessageBox.Show(
                $"Profile '{name}' already exists. Overwrite it?",
                "Confirm Overwrite",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;
        }

        var monitors = MonitorConfiguration.GetCurrentConfiguration();
        if (monitors.Count == 0)
        {
            Notify("Save failed", "No active monitors detected.", ToolTipIcon.Error);
            return;
        }

        try
        {
            _profiles.SaveProfile(name, monitors);
            Notify("Profile saved", $"Saved '{name}' ({monitors.Count} monitor(s)).");
        }
        catch (Exception ex)
        {
            Notify("Save failed", ex.Message, ToolTipIcon.Error);
        }
    }

    private void OnDelete(string name)
    {
        var result = MessageBox.Show(
            $"Delete profile '{name}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
            return;

        if (_profiles.DeleteProfile(name, out var error))
        {
            Notify("Profile deleted", $"Removed '{name}'.");
        }
        else
        {
            Notify("Delete failed", error ?? "Unknown error", ToolTipIcon.Error);
        }
    }

    private void OnShowCurrent()
    {
        var monitors = MonitorConfiguration.GetCurrentConfiguration();
        using var dialog = new CurrentSetupDialog(monitors);
        dialog.ShowDialog();
    }

    private void ExitApp()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        Application.Exit();
    }

    private void Notify(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.ShowBalloonTip(3000);
    }

    private static bool IsValidProfileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return !name.Any(c => Array.IndexOf(invalid, c) >= 0);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _icon.Dispose();
            if (_iconHandle != IntPtr.Zero)
            {
                DestroyIcon(_iconHandle);
                _iconHandle = IntPtr.Zero;
            }
        }
        base.Dispose(disposing);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);

    private static Icon CreateMonitorIcon(out IntPtr handle)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.None;
            g.Clear(Color.Transparent);

            var body = new Rectangle(2, 4, size - 4, 20);
            using (var brush = new SolidBrush(Palette.Navy))
                g.FillRectangle(brush, body);
            using (var brush = new SolidBrush(Palette.Beige))
                g.FillRectangle(brush, Rectangle.Inflate(body, -2, -2));
            using (var pen = new Pen(Palette.Orange, 1f))
                g.DrawRectangle(pen, body);

            var neck = new Rectangle((size - 10) / 2, body.Bottom, 10, 3);
            using (var brush = new SolidBrush(Palette.Navy))
                g.FillRectangle(brush, neck);

            var baseRect = new Rectangle((size - 18) / 2, neck.Bottom, 18, 3);
            using (var brush = new SolidBrush(Palette.Navy))
                g.FillRectangle(brush, baseRect);
            using (var pen = new Pen(Palette.Orange, 1f))
                g.DrawRectangle(pen, baseRect);
        }

        handle = bmp.GetHicon();
        var fromHandle = Icon.FromHandle(handle);
        return (Icon)fromHandle.Clone();
    }
}
