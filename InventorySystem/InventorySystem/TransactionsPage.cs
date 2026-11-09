using System;
using System.Data;
using System.Windows.Forms;

namespace InventorySystem
{
    /// <summary>Stock history. Rows are never edited or deleted (it is an audit trail); you only add new ones.</summary>
    public class TransactionsPage : ListPage
    {
        private readonly ComboBox cboType;

        private const string FromWhere =
            " FROM transactions t JOIN products p ON p.product_id = t.product_id " +
            "WHERE (p.name LIKE @l OR t.remarks LIKE @l OR t.performed_by LIKE @l) AND (@t = 'All' OR t.type = @t) ";

        public TransactionsPage() : base("Transactions", "transactions")
        {
            Filters.Controls.Add(new Label { Text = "Type:", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, 9, 4, 0) });
            cboType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 110,
                Font = Theme.UiFont(10F),
                Margin = new Padding(0, 4, 14, 0)
            };
            cboType.Items.AddRange(new object[] { "All", "Stock In", "Stock Out" });
            cboType.SelectedIndex = 0;
            Filters.Controls.Add(cboType);
            cboType.SelectedIndexChanged += (s, e) => ReloadFromStart();

            ConfigureButtons("+ New Transaction", null, null);
        }

        private string TypeCode()
        {
            switch (Convert.ToString(cboType.SelectedItem))
            {
                case "Stock In": return "IN";
                case "Stock Out": return "OUT";
                default: return "All";
            }
        }

        protected override DataTable LoadRows(string search, int offset, int limit, out int total)
        {
            string like = DbHelper.Like(search);
            string type = TypeCode();

            total = Convert.ToInt32(DbHelper.Scalar("SELECT COUNT(*)" + FromWhere, "@l", like, "@t", type));

            return DbHelper.Query(
                "SELECT t.transaction_id AS id, t.created_at AS `Date`, p.name AS `Product`, " +
                "CASE t.type WHEN 'IN' THEN 'Stock In' ELSE 'Stock Out' END AS `Type`, " +
                "t.quantity AS `Quantity`, IFNULL(t.remarks, '') AS `Remarks`, t.performed_by AS `By`" + FromWhere +
                "ORDER BY t.created_at DESC, t.transaction_id DESC LIMIT @o, @n",
                "@l", like, "@t", type, "@o", offset, "@n", limit);
        }

        protected override void AddClicked()
        {
            using (var f = new TransactionForm())
                if (f.ShowDialog(FindForm()) == DialogResult.OK) RefreshData();
        }
    }
}
