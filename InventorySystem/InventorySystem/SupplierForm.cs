using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace InventorySystem
{
    public class SupplierForm : FormDialog
    {
        private readonly int supplierId;                // 0 = adding
        private readonly FieldControl fName, fContact, fPhone, fEmail, fAddress;

        public SupplierForm(int supplierId)
            : base(supplierId == 0 ? "Add Supplier" : "Edit Supplier", supplierId == 0 ? "Add Supplier" : "Save Changes")
        {
            this.supplierId = supplierId;

            fName = new FieldControl("Supplier Name", false, 100);
            fName.Validator = s => Validators.Text(s, "Supplier name", 2, 100, true);

            fContact = new FieldControl("Contact Person (optional)", false, 100);
            fContact.Validator = Validators.OptionalPerson;

            fPhone = new FieldControl("Phone (optional)", false, 20);
            fPhone.Validator = Validators.Phone;

            fEmail = new FieldControl("Email (optional)", false, 100);
            fEmail.Validator = Validators.OptionalEmail;

            fAddress = new FieldControl("Address (optional)", false, 200);
            fAddress.Validator = s => Validators.Text(s, "Address", 0, 200, false);

            AddField(fName);
            AddField(fContact);
            AddField(fPhone);
            AddField(fEmail);
            AddField(fAddress);

            if (supplierId != 0)
            {
                DataTable dt = DbHelper.Query(
                    "SELECT name, contact_person, phone, email, address FROM suppliers WHERE supplier_id = @id", "@id", supplierId);
                if (dt.Rows.Count == 0) throw new InvalidOperationException("Supplier not found.");
                DataRow r = dt.Rows[0];
                fName.Input.Text = Convert.ToString(r["name"]);
                fContact.Input.Text = Convert.ToString(r["contact_person"]);
                fPhone.Input.Text = Convert.ToString(r["phone"]);
                fEmail.Input.Text = Convert.ToString(r["email"]);
                fAddress.Input.Text = Convert.ToString(r["address"]);
            }
        }

        protected override bool Save()
        {
            object contact = DbHelper.NullIfEmpty(fContact.Value);
            object phone = DbHelper.NullIfEmpty(fPhone.Value);
            object email = string.IsNullOrWhiteSpace(fEmail.Value) ? (object)DBNull.Value : fEmail.Value.Trim().ToLowerInvariant();
            object address = DbHelper.NullIfEmpty(fAddress.Value);

            try
            {
                if (supplierId == 0)
                    DbHelper.Exec(
                        "INSERT INTO suppliers (name, contact_person, phone, email, address) VALUES (@n, @c, @p, @e, @a)",
                        "@n", fName.Value.Trim(), "@c", contact, "@p", phone, "@e", email, "@a", address);
                else
                    DbHelper.Exec(
                        "UPDATE suppliers SET name = @n, contact_person = @c, phone = @p, email = @e, address = @a WHERE supplier_id = @id",
                        "@n", fName.Value.Trim(), "@c", contact, "@p", phone, "@e", email, "@a", address, "@id", supplierId);
                return true;
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    fName.SetError("A supplier with this name already exists");
                    fName.Input.Focus();
                    return false;
                }
                throw;
            }
        }
    }
}
