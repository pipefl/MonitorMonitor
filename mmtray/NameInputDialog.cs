using System.Drawing;
using System.Windows.Forms;

namespace mmtray;

internal sealed class NameInputDialog : Form
{
    private readonly TextBox _textBox;

    public string ProfileName => _textBox.Text.Trim();

    public NameInputDialog()
    {
        Text = "Save Profile As";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 130);
        BackColor = Palette.Navy;
        ForeColor = Palette.Beige;
        Font = new Font("Segoe UI", 9f);

        var label = new Label
        {
            Text = "Profile name:",
            Location = new Point(14, 14),
            AutoSize = true,
            ForeColor = Palette.Beige,
            BackColor = Color.Transparent,
        };
        Controls.Add(label);

        var border = new Panel
        {
            Location = new Point(14, 36),
            Size = new Size(332, 28),
            BackColor = Palette.Orange,
            Padding = new Padding(1),
        };
        _textBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Palette.Beige,
            ForeColor = Palette.Navy,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10f),
        };
        border.Controls.Add(_textBox);
        Controls.Add(border);

        var ok = MakeButton("OK", new Point(184, 84), DialogResult.OK);
        var cancel = MakeButton("Cancel", new Point(272, 84), DialogResult.Cancel);
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static Button MakeButton(string text, Point location, DialogResult result)
    {
        var b = new Button
        {
            Text = text,
            DialogResult = result,
            Location = location,
            Size = new Size(78, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Palette.Navy,
            ForeColor = Palette.Beige,
            UseVisualStyleBackColor = false,
        };
        b.FlatAppearance.BorderColor = Palette.Orange;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = Palette.Orange;
        b.FlatAppearance.MouseDownBackColor = Palette.Orange;
        return b;
    }
}
