using System;
using System.Data;
using System.Windows.Forms;

namespace InventorySystem
{
    public class ProductsPage : ListPage
    {
        private readonly ComboBox cboStatus;

        private const string StatusExpr =
            "CASE WHEN p.quantity = 0 THEN 'Out of Stock' WHEN p.quantity <= p.reorder_level THEN 'Low Stock' ELSE 'In Stock' END";

        private const string FromWhere =
            " FROM products p LEFT JOIN suppliers s ON s.supplier_id = p.supplier_id " +
            "WHERE (p.name LIKE @l OR p.category LIKE @l OR IFNULL(s.name, '') LIKE @l) " +
            "AND (@st = 'All' OR " + StatusExpr + " = @st) ";

        /// <param name="readOnly">true = the table shown on the Dashboard (no Add/Edit/Delete).</param>
        public ProductsPage(bool readOnly)
            : base(readOnly ? "Product Inventory" : "Products", "products")
        {
            Filters.Controls.Add(new Label { Text = "Status:", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, 9, 4, 0) });
            cboStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120,
                Font = Theme.UiFont(10F),
                Margin = new Padding(0, 4, 14, 0)
            };
            cboStatus.Items.AddRange(new object[] { "All", "In Stock", "Low Stock", "Out of Stock" });
            cboStatus.SelectedIndex = 0;
            Filters.Controls.Add(cboStatus);
            cboStatus.SelectedIndexChanged += (s, e) => ReloadFromStart();

            if (readOnly) ConfigureButtons(null, null, null);
            else ConfigureButtons("+ Add Product", "Edit", "Delete");
        }

        protected override DataTable LoadRows(string search, int offset, int limit, out int total)
        {
            string like = DbHelper.Like(search);
            string status = Convert.ToString(cboStatus.SelectedItem);

            total = Convert.ToInt32(DbHelper.Scalar("SELECT COUNT(*)" + FromWhere, "@l", like, "@st", status));

            return DbHelper.Query(
                "SELECT p.product_id AS id, p.name AS `Product Name`, p.category AS `Category`, " +
                "IFNULL(s.name, '-') AS `Supplier`, p.quantity AS `Stock`, p.reorder_level AS `Reorder Level`, " +
                "p.unit_price AS `Unit Price`, " + StatusExpr + " AS `Status`" + FromWhere +
                "ORDER BY p.product_id LIMIT @o, @n",
                "@l", like, "@st", status, "@o", offset, "@n", limit);
        }

        protected override void AddClicked()
        {
            using (var f = new ProductForm(0))
                if (f.ShowDialog(FindForm()) == DialogResult.OK) RefreshData();
        }

        protected override void EditClicked(int id)
        {
            using (var f = new ProductForm(id))
                if (f.ShowDialog(FindForm()) == DialogResult.OK) RefreshData();
        }

        protected override void DeleteClicked(int id)
        {
            string name = CellText("Product Name");
            DialogResult answer = MessageBox.Show(
                "Delete product \"" + name + "\"?\r\n\r\nIts stock history will be deleted too. This cannot be undone.",
                "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;

            DbHelper.Exec("DELETE FROM products WHERE product_id = @id", "@id", id);
            RefreshData();
        }
    }
}
