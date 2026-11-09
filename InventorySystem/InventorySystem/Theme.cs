using System.Drawing;
using System.Windows.Forms;

namespace InventorySystem
{
    /// <summary>Shared colors, fonts and button style so every screen looks the same.</summary>
    internal static class Theme
    {
        public static readonly Color Navy        = Color.FromArgb(15, 15, 90);
        public static readonly Color Card        = Color.White;
        public static readonly Color Text        = Color.FromArgb(33, 37, 41);
        public static readonly Color Muted       = Color.FromArgb(95, 104, 115);
        public static readonly Color Border      = Color.FromArgb(206, 212, 218);
        public static readonly Color Focus       = Color.FromArgb(30, 130, 230);
        public static readonly Color Primary     = Color.FromArgb(30, 130, 230);
        public static readonly Color PrimaryDark = Color.FromArgb(20, 105, 190);
        public static readonly Color Green       = Color.FromArgb(40, 160, 70);
        public static readonly Color GreenDark   = Color.FromArgb(30, 130, 55);
        public static readonly Color Gray        = Color.FromArgb(108, 117, 125);
        public static readonly Color GrayDark    = Color.FromArgb(84, 91, 98);
        public static readonly Color Error       = Color.FromArgb(200, 35, 50);
        public static readonly Color ErrorBg     = Color.FromArgb(253, 236, 238);
        public static readonly Color Warn        = Color.FromArgb(190, 110, 0);
        public static readonly Color Success     = Color.FromArgb(25, 125, 60);
        public static readonly Color SuccessBg   = Color.FromArgb(230, 245, 235);
        public static readonly Color InfoBg      = Color.FromArgb(230, 241, 252);
        public static readonly Color PageBg      = Color.FromArgb(241, 243, 247);

        public static Font UiFont(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font("Segoe UI", size, style);
        }

        public static Button MakeButton(string text, Color back, Color hover)
        {
            var b = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Height = 42,
                MinimumSize = new Size(0, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = Color.White,
                Font = UiFont(10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 4, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = hover;
            return b;
        }

        public static Button SmallButton(string text, Color back, Color hover)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, 34),
                Padding = new Padding(10, 0, 10, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = Color.White,
                Font = UiFont(9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 2, 8, 2)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = hover;
            return b;
        }
    }
}
