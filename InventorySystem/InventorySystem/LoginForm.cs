using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InventorySystem
{
    public class LoginForm : AuthFormBase
    {
        private readonly FieldControl fUser, fPass;
        private readonly Button btnLogin;
        private readonly Label lnkRegister;

        public string Username { get; private set; }
        public string Role { get; private set; }

        public LoginForm()
            : base("Inventory Management System - Sign In", "Sign In", "Welcome back. Enter your account details.")
        {
            ClientSize = new System.Drawing.Size(500, 560);

            fUser = new FieldControl("Username", false, Validators.UserMax);
            fUser.Validator = s => Validators.Required(s, "Username");

            fPass = new FieldControl("Password", true, Validators.PassMax);
            fPass.Validator = s => string.IsNullOrEmpty(s) ? "Password is required" : null;

            btnLogin = Theme.MakeButton("Sign In", Theme.Primary, Theme.PrimaryDark);
            btnLogin.Click += BtnLogin_Click;

            lnkRegister = new Label
            {
                Text = "No account? Register here",
                AutoSize = false,
                Height = 34,
                MinimumSize = new System.Drawing.Size(0, 34),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ForeColor = Theme.Focus,
                Font = Theme.UiFont(9.5F, System.Drawing.FontStyle.Underline),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 10, 0, 0)
            };
            lnkRegister.Click += (s, e) => OpenRegister();

            AddRow(fUser);
            AddRow(fPass);
            AddRow(btnLogin);
            AddRow(lnkRegister);

            AcceptButton = btnLogin;     // Enter key signs in
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            fUser.Input.Focus();
        }

        private void OpenRegister()
        {
            if (Busy) return;
            using (var f = new RegisterForm())
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                {
                    fUser.Input.Text = f.RegisteredUsername;
                    fUser.Reset();
                    fPass.Input.Clear();
                    fPass.Reset();
                    ShowBanner("Account created. Sign in with your new password.", BannerKind.Success);
                    fPass.Input.Focus();
                }
            }
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            HideBanner();

            bool userOk = fUser.Check();
            bool passOk = fPass.Check();
            if (!userOk) { fUser.Input.Focus(); return; }
            if (!passOk) { fPass.Input.Focus(); return; }

            string user = fUser.Value.Trim();
            string pass = fPass.Value;

            SetBusy(true, btnLogin, "Signing in...", "Sign In");
            LoginResult result = await Task.Run(() => AuthService.Login(user, pass));
            if (IsDisposed) return;
            SetBusy(false, btnLogin, "Signing in...", "Sign In");

            switch (result.Status)
            {
                case LoginStatus.Success:
                    Username = result.Username;
                    Role = result.Role;
                    DialogResult = DialogResult.OK;
                    Close();
                    return;

                case LoginStatus.Locked:
                    int mins = (int)Math.Ceiling(result.LockSecondsRemaining / 60.0);
                    ShowBanner("Too many failed attempts. Try again in " + mins + (mins == 1 ? " minute." : " minutes."),
                        BannerKind.Error);
                    fPass.Input.Clear();
                    break;

                case LoginStatus.Disabled:
                    ShowBanner("This account is disabled. Contact an administrator.", BannerKind.Error);
                    fPass.Input.Clear();
                    break;

                case LoginStatus.Error:
                    ShowBanner(result.Message, BannerKind.Error);
                    break;

                default:   // InvalidCredentials: deliberately vague (doesn't say which part was wrong)
                    ShowBanner("Invalid username or password.", BannerKind.Error);
                    fPass.Input.Clear();
                    fPass.Reset();
                    fPass.Input.Focus();
                    break;
            }
        }
    }
}
