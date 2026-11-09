using System;
using System.Windows.Forms;

namespace InventorySystem
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Sign In -> Dashboard -> (Logout) -> Sign In again, until the user closes a window.
            while (true)
            {
                string username, role;
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                    username = login.Username;
                    role = login.Role;
                }

                using (var dashboard = new DashboardForm(username, role))
                {
                    dashboard.ShowDialog();
                    if (!dashboard.LoggedOut) return;
                }
            }
        }
    }
}
