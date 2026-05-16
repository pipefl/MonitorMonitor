using System.Drawing;
using System.Windows.Forms;

namespace mmtray;

internal sealed class NavyMenuRenderer : ToolStripProfessionalRenderer
{
    public NavyMenuRenderer() : base(new NavyColorTable()) { }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? Palette.Beige : Color.FromArgb(0x80, 0x90, 0xA8);
        base.OnRenderItemText(e);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var rect = new Rectangle(Point.Empty, e.Item.Size);
        using var bg = new SolidBrush(e.Item.Selected ? Palette.Orange : Palette.Navy);
        e.Graphics.FillRectangle(bg, rect);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var bg = new SolidBrush(Palette.Navy);
        e.Graphics.FillRectangle(bg, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Palette.Orange);
        var r = e.AffectedBounds;
        e.Graphics.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var r = e.Item.Bounds;
        int y = r.Height / 2;
        using var pen = new Pen(Palette.Sep);
        e.Graphics.DrawLine(pen, 8, y, r.Width - 8, y);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item is { Selected: true } ? Palette.Beige : Palette.Orange;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        using var bg = new SolidBrush(Palette.Navy);
        e.Graphics.FillRectangle(bg, e.AffectedBounds);
    }

    private sealed class NavyColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Palette.Navy;
        public override Color MenuStripGradientBegin => Palette.Navy;
        public override Color MenuStripGradientEnd => Palette.Navy;
        public override Color MenuItemSelected => Palette.Orange;
        public override Color MenuItemSelectedGradientBegin => Palette.Orange;
        public override Color MenuItemSelectedGradientEnd => Palette.Orange;
        public override Color MenuItemPressedGradientBegin => Palette.Orange;
        public override Color MenuItemPressedGradientEnd => Palette.Orange;
        public override Color MenuItemPressedGradientMiddle => Palette.Orange;
        public override Color MenuItemBorder => Palette.Orange;
        public override Color MenuBorder => Palette.Orange;
        public override Color ImageMarginGradientBegin => Palette.Navy;
        public override Color ImageMarginGradientMiddle => Palette.Navy;
        public override Color ImageMarginGradientEnd => Palette.Navy;
        public override Color SeparatorDark => Palette.Sep;
        public override Color SeparatorLight => Palette.Sep;
    }
}
