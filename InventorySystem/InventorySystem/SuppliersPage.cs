using System;
using System.Data;
using System.Windows.Forms;

namespace InventorySystem
{
    public class SuppliersPage : ListPage
    {
        private const string Where =
            " FROM suppliers s WHERE s.name LIKE @l OR s.contact_person LIKE @l OR s.email LIKE @l OR s.phone LIKE @l ";

        public SuppliersPage() : base("Suppliers", "suppliers")
        {
            ConfigureButtons("+ Add Supplier", "Edit", "Delete");
        }

        protected override DataTable LoadRows(string search, int offset, int limit, out int total)
        {
            string like = DbHelper.Like(search);
            total = Convert.ToInt32(DbHelper.Scalar("SELECT COUNT(*)" + Where, "@l", like));

            return DbHelper.Query(
                "SELECT s.supplier_id AS id, s.name AS `Supplier`, IFNULL(s.contact_person, '') AS `Contact Person`, " +
                "IFNULL(s.phone, '') AS `Phone`, IFNULL(s.email, '') AS `Email`, IFNULL(s.address, '') AS `Address`, " +
                "(SELECT COUNT(*) FROM products p WHERE p.supplier_id = s.supplier_id) AS `Products`" + Where +
                "ORDER BY s.name LIMIT @o, @n",
                "@l", like, "@o", offset, "@n", limit);
        }

        protected override void AddClicked()
        {
            using (var f = new SupplierForm(0))
                if (f.ShowDialog(FindForm()) == DialogResult.OK) RefreshData();
        }

        protected override void EditClicked(int id)
        {
            using (var f = new SupplierForm(id))
                if (f.ShowDialog(FindForm()) == DialogResult.OK) RefreshData();
        }

        protected override void DeleteClicked(int id)
        {
            string name = CellText("Supplier");
            DialogResult answer = MessageBox.Show(
                "Delete supplier \"" + name + "\"?\r\n\r\nIts products are kept, but will show no supplier.",
                "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes) return;

            DbHelper.Exec("DELETE FROM suppliers WHERE supplier_id = @id", "@id", id);
            RefreshData();
        }
    }
}
