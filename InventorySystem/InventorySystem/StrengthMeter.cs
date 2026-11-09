using System;
using System.Drawing;
using System.Windows.Forms;

namespace InventorySystem
{
    /// <summary>Four-segment password strength bar with a text label.</summary>
    public class StrengthMeter : Control
    {
        private int score;
        private string label = "";

        public StrengthMeter()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Dock = DockStyle.Fill;
            Height = 22;
            MinimumSize = new Size(0, 22);
            Margin = new Padding(0, 0, 0, 6);
            Font = Theme.UiFont(8.5F);
            BackColor = Theme.Card;
            TabStop = false;
        }

        public void SetPassword(string password)
        {
            string l;
            score = Validators.PasswordScore(password, out l);
            label = l;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);

            Color fill = score <= 1 ? Theme.Error
                       : score == 2 ? Color.FromArgb(230, 140, 0)
                       : score == 3 ? Color.FromArgb(150, 170, 20)
                       : Theme.Green;

            float scale = g.DpiX / 96F;
            int gap = (int)(4 * scale);
            int barH = Math.Max(4, (int)(6 * scale));
            int labelW = (int)(64 * scale);
            int totalW = Math.Max(40, Width - labelW);
            int segW = (totalW - gap * 3) / 4;
            int y = (Height - barH) / 2;

            for (int i = 0; i < 4; i++)
            {
                using (var b = new SolidBrush(i < score ? fill : Theme.Border))
                    g.FillRectangle(b, i * (segW + gap), y, segW, barH);
            }

            if (label.Length > 0)
            {
                var r = new Rectangle(totalW, 0, Width - totalW, Height);
                TextRenderer.DrawText(g, label, Font, r, fill,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }
}
