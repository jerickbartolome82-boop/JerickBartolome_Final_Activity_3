using System;
using System.Data;
using System.Windows.Forms;

namespace InventorySystem
{
    /// <summary>Registered accounts (password hashes are never shown). Disable/enable/unlock an account.</summary>
    public class UsersPage : ListPage
    {
        private const string Where = " FROM users WHERE full_name LIKE @l OR username LIKE @l OR email LIKE @l ";

        public UsersPage() : base("Users", "users")
        {
            ConfigureButtons(null, "Enable / Disable", null);
        }

        protected override DataTable LoadRows(string search, int offset, int limit, out int total)
        {
            string like = DbHelper.Like(search);
            total = Convert.ToInt32(DbHelper.Scalar("SELECT COUNT(*)" + Where, "@l", like));

            return DbHelper.Query(
                "SELECT user_id AS id, full_name AS `Full Name`, username AS `Username`, email AS `Email`, role AS `Role`, " +
                "CASE WHEN is_active = 0 THEN 'Disabled' " +
                "WHEN locked_until IS NOT NULL AND locked_until > NOW() THEN 'Locked' ELSE 'Active' END AS `Status`, " +
                "last_login AS `Last Login`, created_at AS `Created`" + Where +
                "ORDER BY user_id LIMIT @o, @n",
                "@l", like, "@o", offset, "@n", limit);
        }

        protected override void EditClicked(int id)
        {
            string username = CellText("Username");
            string status = CellText("Status");

            if (string.Equals(username, Session.Username, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("You can't disable your own account.", "Users", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string question;
            if (status == "Disabled") question = "Enable account \"" + username + "\"?";
            else if (status == "Locked") question = "Unlock account \"" + username + "\"?";
            else question = "Disable account \"" + username + "\"? They won't be able to sign in.";

            if (MessageBox.Show(question, "Users", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            if (status == "Active")
                DbHelper.Exec("UPDATE users SET is_active = 0 WHERE user_id = @id", "@id", id);
            else
                DbHelper.Exec("UPDATE users SET is_active = 1, failed_attempts = 0, locked_until = NULL WHERE user_id = @id", "@id", id);

            RefreshData();
        }
    }
}
