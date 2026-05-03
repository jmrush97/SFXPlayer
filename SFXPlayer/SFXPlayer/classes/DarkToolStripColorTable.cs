using System.Drawing;
using System.Windows.Forms;

namespace SFXPlayer.classes
{
    /// <summary>
    /// A ProfessionalColorTable that renders MenuStrip and ToolStrip controls
    /// using the same dark palette as the web app (#0f0f1e background, #c8c8f0 text).
    /// Assign via:
    ///   strip.Renderer = new ToolStripProfessionalRenderer(new DarkToolStripColorTable());
    /// </summary>
    internal sealed class DarkToolStripColorTable : ProfessionalColorTable
    {
        // ── palette ──────────────────────────────────────────────────────────
        private static readonly Color BgDark      = Color.FromArgb(15, 15, 30);   // #0f0f1e
        private static readonly Color BgMid       = Color.FromArgb(26, 26, 46);   // #1a1a2e
        private static readonly Color BgHover     = Color.FromArgb(30, 48, 80);   // #1e3050
        private static readonly Color BgPressed   = Color.FromArgb(15, 52, 96);   // #0f3460
        private static readonly Color Border      = Color.FromArgb(68, 68, 68);   // #444444
        private static readonly Color Separator   = Color.FromArgb(60, 60, 80);

        // ── menu bar ─────────────────────────────────────────────────────────
        public override Color MenuStripGradientBegin  => BgDark;
        public override Color MenuStripGradientEnd    => BgDark;

        public override Color MenuItemSelected        => BgHover;
        public override Color MenuItemSelectedGradientBegin => BgHover;
        public override Color MenuItemSelectedGradientEnd   => BgHover;

        public override Color MenuItemPressedGradientBegin  => BgPressed;
        public override Color MenuItemPressedGradientEnd    => BgPressed;
        public override Color MenuItemPressedGradientMiddle => BgPressed;

        public override Color MenuItemBorder          => Border;

        // ── drop-down panel ──────────────────────────────────────────────────
        public override Color MenuBorder              => Border;
        public override Color ToolStripDropDownBackground => BgDark;
        public override Color ImageMarginGradientBegin  => BgMid;
        public override Color ImageMarginGradientMiddle => BgMid;
        public override Color ImageMarginGradientEnd    => BgMid;

        // ── toolbar ──────────────────────────────────────────────────────────
        public override Color ToolStripGradientBegin  => BgDark;
        public override Color ToolStripGradientMiddle => BgDark;
        public override Color ToolStripGradientEnd    => BgDark;

        public override Color ToolStripBorder         => Border;

        public override Color ButtonSelectedHighlight           => BgHover;
        public override Color ButtonSelectedHighlightBorder     => Border;
        public override Color ButtonSelectedGradientBegin       => BgHover;
        public override Color ButtonSelectedGradientMiddle      => BgHover;
        public override Color ButtonSelectedGradientEnd         => BgHover;

        public override Color ButtonPressedHighlight            => BgPressed;
        public override Color ButtonPressedHighlightBorder      => Border;
        public override Color ButtonPressedGradientBegin        => BgPressed;
        public override Color ButtonPressedGradientMiddle       => BgPressed;
        public override Color ButtonPressedGradientEnd          => BgPressed;

        public override Color ButtonCheckedHighlight            => BgPressed;
        public override Color ButtonCheckedHighlightBorder      => Border;
        public override Color ButtonCheckedGradientBegin        => BgPressed;
        public override Color ButtonCheckedGradientMiddle       => BgPressed;
        public override Color ButtonCheckedGradientEnd          => BgPressed;

        // ── separator ────────────────────────────────────────────────────────
        public override Color SeparatorLight  => Separator;
        public override Color SeparatorDark   => Separator;

        // ── status strip ─────────────────────────────────────────────────────
        public override Color StatusStripGradientBegin => BgDark;
        public override Color StatusStripGradientEnd   => BgDark;

        // ── grip / overflow ──────────────────────────────────────────────────
        public override Color GripLight  => Border;
        public override Color GripDark   => Border;
        public override Color OverflowButtonGradientBegin  => BgMid;
        public override Color OverflowButtonGradientMiddle => BgMid;
        public override Color OverflowButtonGradientEnd    => BgMid;
    }
}
