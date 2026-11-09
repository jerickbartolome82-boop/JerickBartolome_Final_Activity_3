using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using MySql.Data.MySqlClient;

namespace InventorySystem
{
    /// <summary>Add or edit a product. Stock is entered once when adding; after that it only changes through Transactions.</summary>
    public class ProductForm : FormDialog
    {
        private readonly int productId;                 // 0 = adding
        private readonly FieldControl fName, fPrice, fStock, fReorder;
        private readonly ComboField cCategory, cSupplier;

        public ProductForm(int productId)
            : base(productId == 0 ? "Add Product" : "Edit Product", productId == 0 ? "Add Product" : "Save Changes")
        {
            this.productId = productId;

            fName = new FieldControl("Product Name", false, 100);
            fName.Validator = s => Validators.Text(s, "Product name", 2, 100, true);

            cCategory = new ComboField("Category (pick one or type a new one)", true);
            foreach (string c in LoadCategories()) cCategory.Combo.Items.Add(c);
            cCategory.Validator = () => Validators.Text(cCategory.Combo.Text, "Category", 2, 50, true);

            cSupplier = new ComboField("Supplier (optional)", false);
            cSupplier.Combo.Items.Add(new ListItem(0, "(None)"));
            foreach (DataRow r in DbHelper.Query("SELECT supplier_id, name FROM suppliers ORDER BY name").Rows)
                cSupplier.Combo.Items.Add(new ListItem(Convert.ToInt32(r["supplier_id"]), Convert.ToString(r["name"])));
            cSupplier.Combo.SelectedIndex = 0;

            fPrice = new FieldControl("Unit Price (PHP)", false, 12);
            fPrice.Validator = Validators.Price;

            fStock = new FieldControl("Initial Stock (quantity on hand)", false, 7);
            fStock.Validator = s => Validators.IntRange(s, "Initial stock", 0, 1000000);

            fReorder = new FieldControl("Reorder Level (warn when stock is at or below this)", false, 6);
            fReorder.Validator = s => Validators.IntRange(s, "Reorder level", 0, 100000);

            AddField(fName);
            AddField(cCategory);
            AddField(cSupplier);
            AddField(fPrice);

            if (productId == 0)
            {
                AddField(fStock);
                fStock.Input.Text = "0";
                fReorder.Input.Text = "5";
            }
            AddField(fReorder);

            if (productId != 0) LoadExisting();
        }

        private static List<string> LoadCategories()
        {
            var list = new List<string> { "Electronics", "Accessories", "Furniture", "Office Supplies", "Other" };
            foreach (DataRow r in DbHelper.Query("SELECT DISTINCT category FROM products ORDER BY category").Rows)
            {
                string c = Convert.ToString(r["category"]);
                if (!list.Exists(x => string.Equals(x, c, StringComparison.OrdinalIgnoreCase))) list.Add(c);
            }
            return list;
        }

        private void LoadExisting()
        {
            DataTable dt = DbHelper.Query(
                "SELECT name, category, supplier_id, quantity, reorder_level, unit_price FROM products WHERE product_id = @id",
                "@id", productId);
            if (dt.Rows.Count == 0) throw new InvalidOperationException("Product not found.");

            DataRow r = dt.Rows[0];
            fName.Input.Text = Convert.ToString(r["name"]);
            cCategory.Combo.Text = Convert.ToString(r["category"]);
            fPrice.Input.Text = Convert.ToDecimal(r["unit_price"]).ToString("0.00", CultureInfo.InvariantCulture);
            fReorder.Input.Text = Convert.ToString(r["reorder_level"]);

            if (r["supplier_id"] != DBNull.Value)
            {
                int sid = Convert.ToInt32(r["supplier_id"]);
                foreach (object o in cSupplier.Combo.Items)
                    if (((ListItem)o).Id == sid) cSupplier.Combo.SelectedItem = o;
            }
            AddNote("Current stock: " + Convert.ToString(r["quantity"]) + ". Change it using the Transactions screen.");
        }

        protected override bool Save()
        {
            string name = fName.Value.Trim();
            string category = cCategory.Combo.Text.Trim();
            decimal price = Validators.ParsePrice(fPrice.Value);
            int reorder = Validators.ParseInt(fReorder.Value);
            var supplier = (ListItem)cSupplier.Combo.SelectedItem;
            object supplierId = supplier == null || supplier.Id == 0 ? (object)DBNull.Value : supplier.Id;

            try
            {
                if (productId == 0)
                {
                    int stock = Validators.ParseInt(fStock.Value);
                    using (var conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        using (var tx = conn.BeginTransaction())
                        {
                            long newId;
                            using (var cmd = new MySqlCommand(
                                "INSERT INTO products (name, category, supplier_id, quantity, reorder_level, unit_price) " +
                                "VALUES (@n, @c, @s, @q, @r, @p)", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@n", name);
                                cmd.Parameters.AddWithValue("@c", category);
                                cmd.Parameters.AddWithValue("@s", supplierId);
                                cmd.Parameters.AddWithValue("@q", stock);
                                cmd.Parameters.AddWithValue("@r", reorder);
                                cmd.Parameters.AddWithValue("@p", price);
                                cmd.ExecuteNonQuery();
                                newId = cmd.LastInsertedId;
                            }
                            if (stock > 0)
                            {
                                using (var log = new MySqlCommand(
                                    "INSERT INTO transactions (product_id, type, quantity, remarks, performed_by) " +
                                    "VALUES (@id, 'IN', @q, 'Initial stock', @u)", conn, tx))
                                {
                                    log.Parameters.AddWithValue("@id", newId);
                                    log.Parameters.AddWithValue("@q", stock);
                                    log.Parameters.AddWithValue("@u", Session.Username);
                                    log.ExecuteNonQuery();
                                }
                            }
                            tx.Commit();
                        }
                    }
                }
                else
                {
                    DbHelper.Exec(
                        "UPDATE products SET name = @n, category = @c, supplier_id = @s, reorder_level = @r, unit_price = @p " +
                        "WHERE product_id = @id",
                        "@n", name, "@c", category, "@s", supplierId, "@r", reorder, "@p", price, "@id", productId);
                }
                return true;
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    fName.SetError("A product with this name already exists");
                    fName.Input.Focus();
                    return false;
                }
                throw;
            }
        }
    }
}
