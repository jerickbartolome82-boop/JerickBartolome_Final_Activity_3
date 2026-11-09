using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace InventorySystem
{
    /// <summary>Record Stock In (received) or Stock Out (issued/sold). Updates the product's stock atomically.</summary>
    public class TransactionForm : FormDialog
    {
        private readonly ComboField cProduct, cType;
        private readonly FieldControl fQty, fRemarks;

        public TransactionForm() : base("New Transaction", "Save Transaction")
        {
            cProduct = new ComboField("Product", false);
            foreach (DataRow r in DbHelper.Query("SELECT product_id, name, quantity FROM products ORDER BY name").Rows)
            {
                int stock = Convert.ToInt32(r["quantity"]);
                cProduct.Combo.Items.Add(new ListItem(Convert.ToInt32(r["product_id"]),
                    Convert.ToString(r["name"]) + "  (stock: " + stock + ")", stock));
            }
            cProduct.Validator = () => cProduct.Combo.SelectedItem == null
                ? (cProduct.Combo.Items.Count == 0 ? "Add a product first" : "Select a product") : null;

            cType = new ComboField("Type", false);
            cType.Combo.Items.AddRange(new object[] { "Stock In", "Stock Out" });
            cType.Combo.SelectedIndex = 0;

            fQty = new FieldControl("Quantity", false, 6);
            fQty.Validator = s =>
            {
                string msg = Validators.IntRange(s, "Quantity", 1, 100000);
                if (msg != null) return msg;

                var item = cProduct.Combo.SelectedItem as ListItem;
                if (IsStockOut && item != null && Validators.ParseInt(s) > item.Stock)
                    return "Only " + item.Stock + " in stock";
                return null;
            };

            fRemarks = new FieldControl("Remarks (optional)", false, 200);
            fRemarks.Validator = s => Validators.Text(s, "Remarks", 0, 200, false);

            // the quantity rule depends on product and type, so re-check it when they change
            cProduct.ValueChanged += (s, e) => { if (fQty.Value.Length > 0) fQty.Check(); };
            cType.ValueChanged += (s, e) => { if (fQty.Value.Length > 0) fQty.Check(); };

            AddField(cProduct);
            AddField(cType);
            AddField(fQty);
            AddField(fRemarks);
        }

        private bool IsStockOut
        {
            get { return Convert.ToString(cType.Combo.SelectedItem) == "Stock Out"; }
        }

        protected override bool Save()
        {
            var item = (ListItem)cProduct.Combo.SelectedItem;
            int qty = Validators.ParseInt(fQty.Value);
            bool isOut = IsStockOut;

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    // The WHERE clause makes the stock check and the update one atomic step,
                    // so two people can't both take the last items.
                    string sql = isOut
                        ? "UPDATE products SET quantity = quantity - @q WHERE product_id = @id AND quantity >= @q"
                        : "UPDATE products SET quantity = quantity + @q WHERE product_id = @id AND quantity <= 1000000000 - @q";

                    int changed;
                    using (var upd = new MySqlCommand(sql, conn, tx))
                    {
                        upd.Parameters.AddWithValue("@q", qty);
                        upd.Parameters.AddWithValue("@id", item.Id);
                        changed = upd.ExecuteNonQuery();
                    }
                    if (changed != 1)
                    {
                        tx.Rollback();
                        fQty.SetError(isOut ? "Not enough stock (it may have just changed)" : "Could not add stock to this product");
                        fQty.Input.Focus();
                        return false;
                    }

                    using (var log = new MySqlCommand(
                        "INSERT INTO transactions (product_id, type, quantity, remarks, performed_by) VALUES (@id, @t, @q, @r, @u)", conn, tx))
                    {
                        log.Parameters.AddWithValue("@id", item.Id);
                        log.Parameters.AddWithValue("@t", isOut ? "OUT" : "IN");
                        log.Parameters.AddWithValue("@q", qty);
                        log.Parameters.AddWithValue("@r", DbHelper.NullIfEmpty(fRemarks.Value));
                        log.Parameters.AddWithValue("@u", Session.Username);
                        log.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
            return true;
        }
    }
}
