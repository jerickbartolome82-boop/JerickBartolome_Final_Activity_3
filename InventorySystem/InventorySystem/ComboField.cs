using System;
using System.Drawing;
using System.Windows.Forms;

namespace InventorySystem
{
    public class ListItem
    {
        public int Id;
        public string Text;
        public int Stock;
        public ListItem(int id, string text, int stock = 0) { Id = id; Text = text; Stock = stock; }
        public override string ToString() { return Text; }
    }

    public class ComboField : TableLayoutPanel, IValidatable
    {
        private readonly Label hint;
        private bool hasError;

        public ComboBox Combo { get; private set; }
        public Func<string> Validator { get; set; }
        public Control FocusTarget { get { return Combo; } }
        public event EventHandler ValueChanged;

        public ComboField(string captionText, bool editable)
        {
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

            var caption = new Label
            {
                Text = captionText,
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.UiFont(9F),
                Margin = new Padding(0, 0, 0, 4)
            };

            var host = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 38,
                MinimumSize = new Size(0, 38),
                Padding = new Padding(0, 4, 0, 0),
                Margin = Padding.Empty
            };
            Combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
                Font = Theme.UiFont(10.5F),
                MaxLength = editable ? 50 : 0
            };
            host.Controls.Add(Combo);

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
            Controls.Add(host, 0, 1);
            Controls.Add(hint, 0, 2);

            Combo.SelectedIndexChanged += (s, e) => Changed();
            Combo.TextChanged += (s, e) => Changed();
            Combo.Leave += (s, e) => Check();
        }

        private void Changed()
        {
            if (hasError) Check();
            var h = ValueChanged;
            if (h != null) h(this, EventArgs.Empty);
        }

        public bool Check()
        {
            string msg = Validator == null ? null : Validator();
            SetError(msg);
            return msg == null;
        }

        public void SetError(string message)
        {
            hasError = !string.IsNullOrEmpty(message);
            hint.Text = hasError ? message : "";
        }
    }
}
