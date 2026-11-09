using System;
using System.Data;
using System.Globalization;
using System.Windows.Forms;

namespace InventorySystem
{
    /// <summary>Three read-only reports in one screen. Each has search, paging and CSV export.</summary>
    public class ReportsPage : ListPage
    {
        private readonly ComboBox cboReport;

        private const string StatusExpr =
            "CASE WHEN p.quantity = 0 THEN 'Out of Stock' WHEN p.quantity <= p.reorder_level THEN 'Low Stock' ELSE 'In Stock' END";

        public ReportsPage() : base("Reports", "report")
        {
            ConfigureButtons(null, null, null);

            Filters.Controls.Add(new Label { Text = "Report:", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, 9, 4, 0) });
            cboReport = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 260,
                Font = Theme.UiFont(10F),
                Margin = new Padding(0, 4, 14, 0)
            };
            cboReport.Items.AddRange(new object[] { "Reorder List (low & out of stock)", "Stock Valuation", "Stock Movement Summary" });
            cboReport.SelectedIndex = 0;
            Filters.Controls.Add(cboReport);
            cboReport.SelectedIndexChanged += (s, e) => ReloadFromStart();
        }

        private string ReportSql()
        {
            switch (cboReport.SelectedIndex)
            {
                case 1:
                    return "SELECT p.name AS `Product`, p.category AS `Category`, p.quantity AS `Stock`, " +
                           "p.unit_price AS `Unit Price`, p.quantity * p.unit_price AS `Total Value` FROM products p";
                case 2:
                    return "SELECT p.name AS `Product`, " +
                           "CAST(IFNULL(SUM(CASE WHEN t.type = 'IN' THEN t.quantity END), 0) AS SIGNED) AS `Total In`, " +
                           "CAST(IFNULL(SUM(CASE WHEN t.type = 'OUT' THEN t.quantity END), 0) AS SIGNED) AS `Total Out`, " +
                           "p.quantity AS `Current Stock` " +
                           "FROM products p LEFT JOIN transactions t ON t.product_id = p.product_id " +
                           "GROUP BY p.product_id, p.name, p.quantity";
                default:
                    return "SELECT p.name AS `Product`, p.category AS `Category`, IFNULL(s.name, '-') AS `Supplier`, " +
                           "IFNULL(s.phone, '-') AS `Supplier Phone`, p.quantity AS `Stock`, p.reorder_level AS `Reorder Level`, " +
                           StatusExpr + " AS `Status` " +
                           "FROM products p LEFT JOIN suppliers s ON s.supplier_id = p.supplier_id " +
                           "WHERE p.quantity <= p.reorder_level";
            }
        }

        protected override DataTable LoadRows(string search, int offset, int limit, out int total)
        {
            string like = DbHelper.Like(search);
            string inner = ReportSql();

            total = Convert.ToInt32(DbHelper.Scalar(
                "SELECT COUNT(*) FROM (" + inner + ") r WHERE r.`Product` LIKE @l", "@l", like));

            if (cboReport.SelectedIndex == 1)
            {
                object sum = DbHelper.Scalar("SELECT IFNULL(SUM(quantity * unit_price), 0) FROM products");
                SetSummary("Total inventory value: PHP " + Convert.ToDecimal(sum).ToString("N2", CultureInfo.InvariantCulture));
            }
            else if (cboReport.SelectedIndex == 2)
                SetSummary("Quantities received and issued per product");
            else
                SetSummary("Products at or below their reorder level");

            return DbHelper.Query(
                "SELECT * FROM (" + inner + ") r WHERE r.`Product` LIKE @l ORDER BY r.`Product` LIMIT @o, @n",
                "@l", like, "@o", offset, "@n", limit);
        }
    }
}
