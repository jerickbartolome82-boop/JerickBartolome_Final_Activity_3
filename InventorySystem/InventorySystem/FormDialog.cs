using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace InventorySystem
{
    /// <summary>
    /// Base for the Add/Edit pop-ups: heading, fields, error banner, Save/Cancel.
    /// Derived classes add fields with AddField(...) and implement Save().
    /// </summary>
    public abstract class FormDialog : Form
    {
        private readonly TableLayoutPanel fieldsPanel;
        private readonly Label banner;
        private readonly List<IValidatable> fields = new List<IValidatable>();

        protected FormDialog(string heading, string saveText)
        {
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.UiFont(9F);
            Text = heading;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(26, 22, 26, 22);

            var layout = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));

            layout.Controls.Add(new Label
            {
                Text = heading,
                AutoSize = true,
                Font = Theme.UiFont(15F, FontStyle.Bold),
                ForeColor = Theme.Navy,
                Margin = new Padding(0, 0, 0, 14)
            });

            fieldsPanel = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            fieldsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(fieldsPanel);

            banner = new Label
            {
                Dock = DockStyle.Fill,
                Height = 40,
                MinimumSize = new Size(0, 40),
                Font = Theme.UiFont(9F),
                Padding = new Padding(10, 0, 8, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Theme.ErrorBg,
                ForeColor = Theme.Error,
                Margin = new Padding(0, 0, 0, 10),
                Visible = false
            };
            layout.Controls.Add(banner);

            var buttons = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var save = Theme.MakeButton(saveText, Theme.Green, Theme.GreenDark);
            save.Margin = new Padding(0, 4, 6, 0);
            save.Click += (s, e) => OnSaveClicked();
            var cancel = Theme.MakeButton("Cancel", Theme.Gray, Theme.GrayDark);
            cancel.Margin = new Padding(6, 4, 0, 0);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            buttons.Controls.Add(save, 0, 0);
            buttons.Controls.Add(cancel, 1, 0);
            layout.Controls.Add(buttons);

            Controls.Add(layout);
            AcceptButton = save;
            CancelButton = cancel;
        }

        protected void AddField<T>(T field) where T : Control, IValidatable
        {
            field.Dock = DockStyle.Fill;
            fieldsPanel.Controls.Add(field);
            fields.Add(field);
        }

        protected void AddNote(string text)
        {
            fieldsPanel.Controls.Add(new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Theme.Muted,
                Font = Theme.UiFont(9F, FontStyle.Italic),
                Margin = new Padding(0, 0, 0, 12)
            });
        }

        protected void ShowError(string message)
        {
            banner.Text = message;
            banner.Visible = true;
        }

        /// <summary>
        /// Write to the database. Return true when saved. For a problem with one field,
        /// call that field's SetError(...) and return false. Database exceptions are
        /// caught here and shown in the banner.
        /// </summary>
        protected abstract bool Save();

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (fields.Count > 0) fields[0].FocusTarget.Focus();
        }

        private void OnSaveClicked()
        {
            banner.Visible = false;

            IValidatable firstBad = null;
            foreach (IValidatable f in fields)
                if (!f.Check() && firstBad == null) firstBad = f;
            if (firstBad != null)
            {
                firstBad.FocusTarget.Focus();
                return;
            }

            try
            {
                if (Save())
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (MySqlException ex)
            {
                ShowError(DbHelper.Describe(ex));
            }
            catch (Exception)
            {
                ShowError("Something went wrong. Please try again.");
            }
        }
    }
}
