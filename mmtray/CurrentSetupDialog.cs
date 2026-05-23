using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace mmtray;

internal sealed class CurrentSetupDialog : Form
{
    public CurrentSetupDialog(List<MonitorConfiguration.MonitorInfo> monitors)
    {
        Text = "Current Monitor Setup";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(580, 380);
        BackColor = Palette.Navy;
        ForeColor = Palette.Beige;
        Padding = new Padding(10);
        Font = new Font("Segoe UI", 9f);

        var border = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Palette.Orange,
            Padding = new Padding(1),
        };
        var text = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Palette.Beige,
            ForeColor = Palette.Navy,
            BorderStyle = BorderStyle.None,
            Font = new Font(FontFamily.GenericMonospace, 9.5f),
            Text = Format(monitors),
        };
        border.Controls.Add(text);
        Controls.Add(border);
    }

    private static string Format(List<MonitorConfiguration.MonitorInfo> monitors)
    {
        if (monitors.Count == 0)
            return "No active monitors detected.";

        var sb = new StringBuilder();
        foreach (var m in monitors)
        {
            sb.AppendLine($"Device:       {m.DeviceName}");
            sb.AppendLine($"Adapter:      {m.DeviceString}");
            sb.AppendLine($"Monitor:      {(string.IsNullOrEmpty(m.MonitorFriendlyName) ? "(EDID unavailable)" : m.MonitorFriendlyName)}");
            if (!string.IsNullOrEmpty(m.MonitorDevicePath))
            {
                var mfg = DisplayConfig.DecodeManufacturerId(m.ManufacturerId);
                sb.AppendLine($"EDID:         {mfg} product 0x{m.ProductCodeId:X4}");
            }
            sb.AppendLine($"Resolution:   {m.Width}x{m.Height}");
            sb.AppendLine($"Position:     ({m.PositionX}, {m.PositionY})");
            sb.AppendLine($"Frequency:    {m.Frequency}Hz");
            sb.AppendLine($"Bits/Pixel:   {m.BitsPerPixel}");
            sb.AppendLine($"Primary:      {(m.IsPrimary ? "Yes" : "No")}");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
