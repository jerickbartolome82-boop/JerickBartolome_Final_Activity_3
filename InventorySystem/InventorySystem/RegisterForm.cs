using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InventorySystem
{
    public class RegisterForm : AuthFormBase
    {
        private readonly FieldControl fName, fUser, fEmail, fPass, fConfirm;
        private readonly StrengthMeter meter;
        private readonly ComboBox cboRole;
        private readonly Button btnRegister, btnBack;

        /// <summary>Set after a successful registration so Sign In can pre-fill it.</summary>
        public string RegisteredUsername { get; private set; }

        public RegisterForm()
            : base("Inventory Management System - Register", "Create Account", "Fill in the details below to register.")
        {
            ClientSize = new Size(500, 760);
            StartPosition = FormStartPosition.CenterParent;

            fName = new FieldControl("Full Name", false, Validators.NameMax);
            fName.Validator = Validators.FullName;

            fUser = new FieldControl("Username (letters, numbers, _)", false, Validators.UserMax);
            fUser.Validator = Validators.Username;

            fEmail = new FieldControl("Email", false, Validators.EmailMax);
            fEmail.Validator = Validators.Email;

            fPass = new FieldControl("Password (8+ chars, upper, lower, number)", true, Validators.PassMax);
            fPass.Validator = s => Validators.Password(s, fUser.Value);

            meter = new StrengthMeter();

            fConfirm = new FieldControl("Confirm Password", true, Validators.PassMax);
            fConfirm.Validator = s => Validators.Confirm(s, fPass.Value);

            // Keep dependent fields in sync while typing.
            fPass.ValueChanged += (s, e) =>
            {
                meter.SetPassword(fPass.Value);
                if (fConfirm.Value.Length > 0) fConfirm.Check();
            };
            fUser.ValueChanged += (s, e) => { if (fPass.Value.Length > 0 && fPass.Input.Modified) fPass.Check(); };

            var roleBox = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 14)
            };
            roleBox.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            roleBox.Controls.Add(new Label
            {
                Text = "Role",
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.UiFont(9F),
                Margin = new Padding(0, 0, 0, 4)
            }, 0, 0);
            cboRole = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.UiFont(10.5F),
                Margin = Padding.Empty
            };
            cboRole.Items.AddRange(new object[] { "Staff", "Administrator" });
            cboRole.SelectedIndex = 0;
            roleBox.Controls.Add(cboRole, 0, 1);

            btnRegister = Theme.MakeButton("Register", Theme.Green, Theme.GreenDark);
            btnRegister.Click += BtnRegister_Click;

            btnBack = Theme.MakeButton("Back to Sign In", Theme.Gray, Theme.GrayDark);
            btnBack.Font = Theme.UiFont(9.5F, FontStyle.Regular);
            btnBack.Height = 36;
            btnBack.MinimumSize = new Size(0, 36);
            btnBack.Margin = new Padding(0, 8, 0, 0);
            btnBack.Click += (s, e) => { if (!Busy) Close(); };

            AddRow(fName);
            AddRow(fUser);
            AddRow(fEmail);
            AddRow(fPass);
            AddRow(meter);
            AddRow(fConfirm);
            AddRow(roleBox);
            AddRow(btnRegister);
            AddRow(btnBack);

            AcceptButton = btnRegister;   // Enter key submits
            CancelButton = btnBack;       // Esc goes back
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            fName.Input.Focus();
        }

        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            HideBanner();

            // Validate everything so every bad field lights up, then focus the first one.
            FieldControl firstBad = null;
            foreach (FieldControl f in new[] { fName, fUser, fEmail, fPass, fConfirm })
                if (!f.Check() && firstBad == null) firstBad = f;

            if (firstBad != null)
            {
                firstBad.Input.Focus();
                ShowBanner("Please fix the highlighted fields.", BannerKind.Error);
                return;
            }

            string name = fName.Value, user = fUser.Value.Trim(), email = fEmail.Value;
            string pass = fPass.Value, role = Convert.ToString(cboRole.SelectedItem);

            SetBusy(true, btnRegister, "Creating account...", "Register");
            RegisterResult result = await Task.Run(() => AuthService.Register(name, user, email, pass, role));
            if (IsDisposed) return;
            SetBusy(false, btnRegister, "Creating account...", "Register");

            switch (result.Status)
            {
                case RegisterStatus.Success:
                    RegisteredUsername = user;
                    DialogResult = DialogResult.OK;
                    Close();
                    break;

                case RegisterStatus.UsernameTaken:
                    fUser.SetError("That username is already taken");
                    fUser.Input.Focus();
                    break;

                case RegisterStatus.EmailTaken:
                    fEmail.SetError("That email is already registered");
                    fEmail.Input.Focus();
                    break;

                default:
                    ShowBanner(result.Message, BannerKind.Error);
                    break;
            }
        }
    }
}
