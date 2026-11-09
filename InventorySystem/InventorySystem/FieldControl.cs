using System;
using System.Drawing;
using System.Windows.Forms;

namespace InventorySystem
{
    /// <summary>Anything a dialog can validate (text fields and combo fields).</summary>
    public interface IValidatable
    {
        bool Check();
        Control FocusTarget { get; }
    }

    /// <summary>
    /// One form field: caption, bordered text box, and a reserved line underneath
    /// for the inline error (reserved so the layout never jumps).
    /// Password fields get a Show/Hide toggle and a Caps Lock warning.
    /// </summary>
    public class FieldControl : TableLayoutPanel, IValidatable
    {
        private readonly Label caption;
        private readonly Label hint;
        private readonly Panel border;
        private readonly Panel inner;
        private readonly Button toggle;
        private readonly bool isPassword;
        private bool touched, hasError, focused;

        public TextBox Input { get; private set; }
        public Func<string, string> Validator { get; set; }
        public event EventHandler ValueChanged;
        public string Value { get { return Input.Text; } }
        public Control FocusTarget { get { return Input; } }

        public FieldControl(string captionText, bool password, int maxLength)
        {
            isPassword = password;

            ColumnCount = 1;
            RowCount = 3;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;

            caption = new Label
            {
                Text = captionText,
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.UiFont(9F),
                Margin = new Padding(0, 0, 0, 4)
            };

            border = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 38,
                MinimumSize = new Size(0, 38),
                Padding = new Padding(1),
                BackColor = Theme.Border,
                Margin = Padding.Empty
            };
            inner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10, 0, 4, 0) };
            var textHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 9, 0, 0) };

            Input = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = Theme.UiFont(10.5F),
                ForeColor = Theme.Text,
                MaxLength = maxLength,
                UseSystemPasswordChar = password
            };
            textHost.Controls.Add(Input);
            inner.Controls.Add(textHost);   // Fill first, then the Right-docked button

            if (password)
            {
                toggle = new Button
                {
                    Text = "Show",
                    Dock = DockStyle.Right,
                    Width = 52,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = Theme.Focus,
                    Font = Theme.UiFont(8.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    TabStop = false,
                    UseVisualStyleBackColor = false
                };
                toggle.FlatAppearance.BorderSize = 0;
                toggle.Click += (s, e) =>
                {
                    bool nowHidden = !Input.UseSystemPasswordChar;
                    Input.UseSystemPasswordChar = nowHidden;
                    toggle.Text = nowHidden ? "Show" : "Hide";
                    Input.Focus();
                };
                inner.Controls.Add(toggle);
            }

            border.Controls.Add(inner);

            hint = new Label
            {
                Dock = DockStyle.Fill,
                Font = Theme.UiFont(8.5F),
                ForeColor = Theme.Error,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = Padding.Empty,
                Padding = new Padding(1, 2, 0, 0)
            };

            Controls.Add(caption, 0, 0);
            Controls.Add(border, 0, 1);
            Controls.Add(hint, 0, 2);

            Input.Enter += (s, e) => { focused = true; UpdateBorder(); UpdateHint(); };
            Input.Leave += (s, e) => { focused = false; touched = true; Check(); UpdateHint(); };
            Input.KeyUp += (s, e) => UpdateHint();
            Input.TextChanged += (s, e) =>
            {
                if (touched || hasError) Check();      // live feedback once the user has been in the field
                var h = ValueChanged;
                if (h != null) h(this, EventArgs.Empty);
            };
        }

        /// <summary>Runs the validator, shows/clears the message, returns true when valid.</summary>
        public bool Check()
        {
            string msg = Validator == null ? null : Validator(Input.Text);
            SetError(msg);
            return msg == null;
        }

        public void SetError(string message)
        {
            hasError = !string.IsNullOrEmpty(message);
            hint.ForeColor = Theme.Error;
            hint.Text = hasError ? message : "";
            UpdateBorder();
            if (!hasError) UpdateHint();
        }

        public void Reset()
        {
            touched = false;
            SetError(null);
        }

        private void UpdateBorder()
        {
            border.BackColor = hasError ? Theme.Error : (focused ? Theme.Focus : Theme.Border);
        }

        private void UpdateHint()
        {
            if (hasError) return;
            bool caps = isPassword && focused && Control.IsKeyLocked(Keys.CapsLock);
            hint.ForeColor = Theme.Warn;
            hint.Text = caps ? "Caps Lock is on" : "";
        }
    }
}
