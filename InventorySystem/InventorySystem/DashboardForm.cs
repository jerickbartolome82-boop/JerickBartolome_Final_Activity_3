using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace InventorySystem
{
    /// <summary>
    /// Main window after sign in: navy sidebar on the left, the selected screen on the right.
    /// Keep the constructor DashboardForm(string username, string role) and the LoggedOut property.
    /// </summary>
    public class DashboardForm : Form
    {
        private readonly Dictionary<Button, Control> screens = new Dictionary<Button, Control>();
        private readonly Panel content;

        /// <summary>True when the user clicked Log Out (Program then shows Sign In again).</summary>
        public bool LoggedOut { get; private set; }

        public DashboardForm(string username, string role)
        {
            Session.Username = username;
            Session.Role = role;

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.UiFont(9F);
            Text = "Inventory Management System";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 560);
            ClientSize = new Size(1100, 680);
            BackColor = Theme.PageBg;

            // Fill first, then the edge-docked parts (Left, Bottom).
            content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.PageBg };
            Controls.Add(content);

            // ----- sidebar -----
            var side = new TableLayoutPanel
            {
                Dock = DockStyle.Left, Width = 210, BackColor = Theme.Navy,
                ColumnCount = 1, RowCount = 3, Padding = new Padding(10, 14, 10, 14)
            };
            side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            side.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            side.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown,
                WrapContents = false, Margin = Padding.Empty
            };
            menu.Controls.Add(new Label
            {
                Text = "Inventory\r\nManagement\r\nSystem", AutoSize = false, Width = 186, Height = 84,
                ForeColor = Color.White, Font = Theme.UiFont(13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(0, 0, 0, 14)
            });

            AddScreen(menu, "Dashboard", new DashboardPage(username));
            AddScreen(menu, "Products", new ProductsPage(false));
            AddScreen(menu, "Suppliers", new SuppliersPage());
            AddScreen(menu, "Transactions", new TransactionsPage());
            AddScreen(menu, "Reports", new ReportsPage());
            AddScreen(menu, "Users", new UsersPage());

            var logout = MakeNavButton("Log Out");
            logout.Margin = Padding.Empty;
            logout.Dock = DockStyle.Fill;
            logout.Click += (s, e) =>
            {
                if (MessageBox.Show("Sign out now?", "Log Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    LoggedOut = true;
                    Close();
                }
            };

            side.Controls.Add(menu, 0, 0);
            side.Controls.Add(logout, 0, 2);
            Controls.Add(side);

            // ----- status bar -----
            var status = new StatusStrip();
            status.Items.Add(new ToolStripStatusLabel("User: " + username + "  |  Role: " + role));
            Controls.Add(status);

            // open on the dashboard
            foreach (KeyValuePair<Button, Control> pair in screens)
            {
                Navigate(pair.Key);
                break;
            }
        }

        private Button MakeNavButton(string text)
        {
            var b = new Button
            {
                Text = text, Width = 186, Height = 42, FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Navy, ForeColor = Color.White, Font = Theme.UiFont(10.5F),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(14, 0, 0, 0),
                Cursor = Cursors.Hand, UseVisualStyleBackColor = false, Margin = new Padding(0, 0, 0, 4)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.PrimaryDark;
            b.FlatAppearance.MouseDownBackColor = Theme.PrimaryDark;
            return b;
        }

        private void AddScreen(FlowLayoutPanel menu, string text, Control screen)
        {
            Button b = MakeNavButton(text);
            b.Click += (s, e) => Navigate(b);
            menu.Controls.Add(b);

            screen.Dock = DockStyle.Fill;
            screen.Visible = false;
            content.Controls.Add(screen);
            screens[b] = screen;
        }

        private void Navigate(Button target)
        {
            foreach (KeyValuePair<Button, Control> pair in screens)
            {
                bool active = pair.Key == target;
                pair.Key.BackColor = active ? Theme.Primary : Theme.Navy;
                if (!active) pair.Value.Visible = false;
            }
            screens[target].Visible = true;      // each screen reloads its data when it becomes visible
        }
    }
}
