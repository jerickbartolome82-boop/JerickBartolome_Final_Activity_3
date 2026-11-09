using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace InventorySystem
{
    /// <summary>Summary cards (real numbers from the database) + the product table underneath.</summary>
    public class DashboardPage : UserControl
    {
        private readonly Label vTotal, vIn, vLow, vOut, vValue, lblError;

        public DashboardPage(string username)
        {
            Dock = DockStyle.Fill;
            BackColor = Theme.PageBg;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var head = new FlowLayoutPanel
            {
                AutoSize = true, Dock = DockStyle.Fill, WrapContents = false,
                Padding = new Padding(20, 16, 20, 0), Margin = Padding.Empty
            };
            head.Controls.Add(new Label
            {
                Text = "Dashboard", AutoSize = true, Font = Theme.UiFont(18F, FontStyle.Bold),
                ForeColor = Theme.Navy, Margin = Padding.Empty
            });
            head.Controls.Add(new Label
            {
                Text = "Welcome, " + username + "!", AutoSize = true, Font = Theme.UiFont(10.5F),
                ForeColor = Theme.Muted, Margin = new Padding(16, 12, 0, 0)
            });

            lblError = new Label
            {
                AutoSize = true, ForeColor = Theme.Error, Font = Theme.UiFont(10F),
                Margin = new Padding(20, 6, 20, 0), Visible = false
            };

            var cards = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 5, RowCount = 1,
                Padding = new Padding(20, 12, 8, 4), Margin = Padding.Empty
            };
            for (int i = 0; i < 5; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            vTotal = AddCard(cards, 0, "Total Products", Color.FromArgb(23, 71, 214));
            vIn    = AddCard(cards, 1, "In Stock", Color.FromArgb(40, 160, 70));
            vLow   = AddCard(cards, 2, "Low Stock", Color.FromArgb(214, 140, 0));
            vOut   = AddCard(cards, 3, "Out of Stock", Color.FromArgb(176, 25, 43));
            vValue = AddCard(cards, 4, "Stock Value (PHP)", Theme.Navy);

            root.Controls.Add(head, 0, 0);
            root.Controls.Add(lblError, 0, 1);
            root.Controls.Add(cards, 0, 2);
            root.Controls.Add(new ProductsPage(true), 0, 3);
            Controls.Add(root);
        }

        private Label AddCard(TableLayoutPanel parent, int column, string caption, Color color)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill, BackColor = color, Padding = new Padding(14, 10, 10, 10),
                Margin = new Padding(0, 0, 12, 0), Height = 88, MinimumSize = new Size(0, 88)
            };
            var value = new Label
            {
                Dock = DockStyle.Fill, Text = "-", ForeColor = Color.White, AutoEllipsis = true,
                Font = Theme.UiFont(20F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft
            };
            var title = new Label
            {
                Dock = DockStyle.Top, Text = caption, ForeColor = Color.White, Height = 22,
                Font = Theme.UiFont(10F, FontStyle.Bold)
            };
            card.Controls.Add(value);     // Fill first, then the Top caption
            card.Controls.Add(title);
            parent.Controls.Add(card, column, 0);
            return value;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && !DesignMode) LoadStats();
        }

        private void LoadStats()
        {
            try
            {
                DataTable dt = DbHelper.Query(
                    "SELECT COUNT(*) AS total, " +
                    "CAST(IFNULL(SUM(quantity > 0 AND quantity > reorder_level), 0) AS SIGNED) AS ins, " +
                    "CAST(IFNULL(SUM(quantity > 0 AND quantity <= reorder_level), 0) AS SIGNED) AS low, " +
                    "CAST(IFNULL(SUM(quantity = 0), 0) AS SIGNED) AS outs, " +
                    "IFNULL(SUM(quantity * unit_price), 0) AS value FROM products");
                DataRow r = dt.Rows[0];
                vTotal.Text = Convert.ToInt32(r["total"]).ToString("N0", CultureInfo.InvariantCulture);
                vIn.Text = Convert.ToInt32(r["ins"]).ToString("N0", CultureInfo.InvariantCulture);
                vLow.Text = Convert.ToInt32(r["low"]).ToString("N0", CultureInfo.InvariantCulture);
                vOut.Text = Convert.ToInt32(r["outs"]).ToString("N0", CultureInfo.InvariantCulture);
                vValue.Text = Convert.ToDecimal(r["value"]).ToString("N2", CultureInfo.InvariantCulture);
                lblError.Visible = false;
            }
            catch (MySqlException ex)
            {
                lblError.Text = DbHelper.Describe(ex);
                lblError.Visible = true;
            }
            catch (Exception)
            {
                lblError.Text = "Could not load the summary.";
                lblError.Visible = true;
            }
        }
    }
}
