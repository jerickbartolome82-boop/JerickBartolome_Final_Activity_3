using System;
using System.Drawing;
using System.Windows.Forms;

namespace InventorySystem
{
    public enum BannerKind { Error, Success, Info }

    /// <summary>
    /// Shared responsive shell for Sign In and Register: navy background, a white
    /// card that stays centered, shrinks on small windows, and scrolls when the
    /// window is shorter than the card. All sizes are 96-DPI units and scale with
    /// the display (AutoScaleMode.Dpi).
    /// </summary>
    public abstract class AuthFormBase : Form
    {
        private readonly Panel scroller;
        private readonly TableLayoutPanel center;
        private readonly Label banner;
        private bool loaded;

        /// <summary>The white card. Add rows with AddRow.</summary>
        protected TableLayoutPanel Body { get; private set; }
        protected bool Busy { get; private set; }

        protected AuthFormBase(string windowTitle, string heading, string subtitle)
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.UiFont(9F);
            Text = windowTitle;
            BackColor = Theme.Navy;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(340, 420);
            ClientSize = new Size(520, 640);

            scroller = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                AutoScrollMargin = new Size(16, 16),
                BackColor = Theme.Navy
            };

            Body = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Theme.Card,
                Padding = new Padding(28, 26, 28, 24),
                Margin = new Padding(16),
                Anchor = AnchorStyles.None
            };
            Body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360F));

            var title = new Label
            {
                Text = heading,
                AutoSize = true,
                Font = Theme.UiFont(20F, FontStyle.Bold),
                ForeColor = Theme.Navy,
                Margin = new Padding(0, 0, 0, 2)
            };
            var sub = new Label
            {
                Text = subtitle,
                AutoSize = true,
                Font = Theme.UiFont(9.5F),
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 0, 0, 16)
            };
            banner = new Label
            {
                Dock = DockStyle.Fill,
                Height = 44,
                MinimumSize = new Size(0, 44),
                Font = Theme.UiFont(9F),
                Padding = new Padding(10, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 0, 14),
                Visible = false
            };

            Body.Controls.Add(title);
            Body.Controls.Add(sub);
            Body.Controls.Add(banner);

            // 3x3 grid with the card in the middle cell: stays centered at any window size.
            center = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                BackColor = Theme.Navy,
                Margin = Padding.Empty
            };
            center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            center.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            center.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            center.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            center.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            center.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            center.Controls.Add(Body, 1, 1);

            scroller.Controls.Add(center);
            Controls.Add(scroller);

            scroller.SizeChanged += (s, e) => LayoutCard();
            Body.SizeChanged += (s, e) => LayoutCard();
        }

        protected void AddRow(Control c)
        {
            c.Dock = DockStyle.Fill;
            Body.Controls.Add(c);
        }

        protected int Dpi(int logicalPixels)
        {
            return (int)Math.Round(logicalPixels * CurrentAutoScaleDimensions.Width / 96F);
        }

        // ---------- banner (inline status message, replaces pop-up boxes) ----------

        protected void ShowBanner(string message, BannerKind kind)
        {
            banner.Text = message;
            banner.BackColor = kind == BannerKind.Error ? Theme.ErrorBg
                             : kind == BannerKind.Success ? Theme.SuccessBg : Theme.InfoBg;
            banner.ForeColor = kind == BannerKind.Error ? Theme.Error
                             : kind == BannerKind.Success ? Theme.Success : Theme.PrimaryDark;
            banner.Visible = true;
        }

        protected void HideBanner()
        {
            banner.Visible = false;
        }

        // ---------- busy state while a database call is running ----------

        protected void SetBusy(bool busy, Button primary, string busyText, string normalText)
        {
            Busy = busy;
            primary.Text = busy ? busyText : normalText;
            Body.Enabled = !busy;
            UseWaitCursor = busy;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (Busy) e.Cancel = true;     // don't close in the middle of a database call
            base.OnFormClosing(e);
        }

        // ---------- responsive layout ----------

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Never open taller/wider than the screen (the card scrolls instead).
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            if (Height > wa.Height) Height = wa.Height;
            if (Width > wa.Width) Width = wa.Width;
            CenterToScreen();

            loaded = true;
            LayoutCard();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LayoutCard();
        }

        private void LayoutCard()
        {
            if (!loaded) return;

            int cardW = Math.Max(Dpi(300), Math.Min(Dpi(440), scroller.ClientSize.Width - Dpi(32) - SystemInformation.VerticalScrollBarWidth));
            float inner = cardW - Body.Padding.Horizontal;
            if (Math.Abs(Body.ColumnStyles[0].Width - inner) > 0.5F)
                Body.ColumnStyles[0].Width = inner;

            // When the window is smaller than the card, the scroll bars appear instead of clipping.
            int cardH = Body.GetPreferredSize(Size.Empty).Height;
            var min = new Size(cardW + Body.Margin.Horizontal, cardH + Body.Margin.Vertical);
            if (scroller.AutoScrollMinSize != min) scroller.AutoScrollMinSize = min;
        }
    }
}
