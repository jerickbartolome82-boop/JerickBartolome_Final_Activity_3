using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace InventorySystem
{
    /// <summary>
    /// One reusable screen: title, search box, Add/Edit/Delete/Export buttons,
    /// a table, and a paging bar. Paging is done by MySQL (LIMIT/OFFSET), so it stays
    /// fast no matter how many rows there are.
    /// Derived pages only implement LoadRows(...) and the button actions.
    /// </summary>
    public abstract class ListPage : UserControl
    {
        private readonly string exportName;
        private readonly Label lblSummary, lblNotice, lblInfo, lblPage;
        private readonly Button btnFirst, btnPrev, btnNext, btnLast;
        private readonly ComboBox cboSize;
        private readonly System.Windows.Forms.Timer searchTimer;
        private readonly Font boldFont;
        private int page = 1, pageSize = 10, total, pages = 1;

        protected readonly TextBox SearchBox;
        protected readonly FlowLayoutPanel Filters;
        protected readonly Button BtnAdd, BtnEdit, BtnDelete, BtnExport;
        protected readonly DataGridView Grid;

        /// <summary>Return one page of rows (and the total row count for the current search).</summary>
        protected abstract DataTable LoadRows(string search, int offset, int limit, out int total);
        protected virtual void AddClicked() { }
        protected virtual void EditClicked(int id) { }
        protected virtual void DeleteClicked(int id) { }

        protected ListPage(string title, string exportName)
        {
            this.exportName = exportName;
            Dock = DockStyle.Fill;
            BackColor = Theme.PageBg;
            boldFont = Theme.UiFont(9.5F, FontStyle.Bold);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(20, 16, 20, 12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // --- title row ---
            var head = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
            head.Controls.Add(new Label
            {
                Text = title,
                AutoSize = true,
                Font = Theme.UiFont(18F, FontStyle.Bold),
                ForeColor = Theme.Navy,
                Margin = new Padding(0)
            });
            lblSummary = new Label
            {
                AutoSize = true,
                Font = Theme.UiFont(10F),
                ForeColor = Theme.Muted,
                Margin = new Padding(16, 14, 0, 0)
            };
            head.Controls.Add(lblSummary);

            // --- toolbar row: search | filters | buttons ---
            var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0, 6, 0, 6) };
            toolbar.Controls.Add(new Label { Text = "Search:", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, 9, 4, 0) });
            SearchBox = new TextBox { Width = 210, Font = Theme.UiFont(10F), Margin = new Padding(0, 5, 14, 0), MaxLength = 100 };
            toolbar.Controls.Add(SearchBox);

            Filters = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            toolbar.Controls.Add(Filters);

            BtnAdd = Theme.SmallButton("+ Add", Theme.Green, Theme.GreenDark);
            BtnEdit = Theme.SmallButton("Edit", Theme.Primary, Theme.PrimaryDark);
            BtnDelete = Theme.SmallButton("Delete", Theme.Error, Color.FromArgb(160, 25, 40));
            BtnExport = Theme.SmallButton("Export CSV", Theme.Gray, Theme.GrayDark);
            toolbar.Controls.Add(BtnAdd);
            toolbar.Controls.Add(BtnEdit);
            toolbar.Controls.Add(BtnDelete);
            toolbar.Controls.Add(BtnExport);

            // --- notice line (errors / "no records") ---
            lblNotice = new Label { AutoSize = true, Font = Theme.UiFont(10F), Margin = new Padding(0, 0, 0, 6), Visible = false };

            // --- table ---
            Grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Theme.Border,
                EnableHeadersVisualStyles = false,
                Font = Theme.UiFont(9.5F),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36,
                Margin = Padding.Empty
            };
            Grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Navy;
            Grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            Grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Navy;
            Grid.ColumnHeadersDefaultCellStyle.Font = boldFont;
            Grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            Grid.RowTemplate.Height = 30;
            Grid.DefaultCellStyle.SelectionBackColor = Theme.Primary;
            Grid.DefaultCellStyle.SelectionForeColor = Color.White;
            Grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 252);

            // --- paging bar ---
            var pager = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0, 8, 0, 0) };
            lblInfo = new Label { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, 7, 18, 0) };
            btnFirst = PagerButton("\u00AB");
            btnPrev = PagerButton("\u2039");
            lblPage = new Label { AutoSize = true, Margin = new Padding(8, 7, 8, 0), Font = boldFont, ForeColor = Theme.Text };
            btnNext = PagerButton("\u203A");
            btnLast = PagerButton("\u00BB");
            var lblRows = new Label { Text = "Rows per page:", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(18, 7, 4, 0) };
            cboSize = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 60, Font = Theme.UiFont(9.5F), Margin = new Padding(0, 3, 0, 0) };
            cboSize.Items.AddRange(new object[] { "5", "10", "25", "50" });
            cboSize.SelectedIndex = 1;
            pager.Controls.Add(lblInfo);
            pager.Controls.Add(btnFirst);
            pager.Controls.Add(btnPrev);
            pager.Controls.Add(lblPage);
            pager.Controls.Add(btnNext);
            pager.Controls.Add(btnLast);
            pager.Controls.Add(lblRows);
            pager.Controls.Add(cboSize);

            root.Controls.Add(head, 0, 0);
            root.Controls.Add(toolbar, 0, 1);
            root.Controls.Add(lblNotice, 0, 2);
            root.Controls.Add(Grid, 0, 3);
            root.Controls.Add(pager, 0, 4);
            Controls.Add(root);

            // --- behaviour ---
            searchTimer = new System.Windows.Forms.Timer { Interval = 350 };
            searchTimer.Tick += (s, e) => { searchTimer.Stop(); page = 1; LoadPage(); };
            SearchBox.TextChanged += (s, e) => { searchTimer.Stop(); searchTimer.Start(); };

            btnFirst.Click += (s, e) => { page = 1; LoadPage(); };
            btnPrev.Click += (s, e) => { page--; LoadPage(); };
            btnNext.Click += (s, e) => { page++; LoadPage(); };
            btnLast.Click += (s, e) => { page = pages; LoadPage(); };
            cboSize.SelectedIndexChanged += (s, e) =>
            {
                pageSize = int.Parse(Convert.ToString(cboSize.SelectedItem), CultureInfo.InvariantCulture);
                page = 1;
                LoadPage();
            };

            BtnAdd.Click += (s, e) => RunSafe(AddClicked);
            BtnEdit.Click += (s, e) => DoEdit();
            BtnDelete.Click += (s, e) => DoDelete();
            BtnExport.Click += (s, e) => Export();

            Grid.SelectionChanged += (s, e) => UpdateButtons();
            Grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && BtnEdit.Visible && BtnEdit.Enabled) DoEdit(); };
            Grid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Delete && BtnDelete.Visible && BtnDelete.Enabled) DoDelete(); };
            Grid.CellFormatting += Grid_CellFormatting;

            UpdateButtons();
        }

        private Button PagerButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Width = 36,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Theme.Text,
                Font = Theme.UiFont(11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Margin = new Padding(2, 1, 2, 0)
            };
            b.FlatAppearance.BorderColor = Theme.Border;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 238, 250);
            return b;
        }

        // ---------- things derived pages use ----------

        /// <summary>Pass null to hide a button.</summary>
        protected void ConfigureButtons(string add, string edit, string delete)
        {
            BtnAdd.Visible = add != null;
            if (add != null) BtnAdd.Text = add;
            BtnEdit.Visible = edit != null;
            if (edit != null) BtnEdit.Text = edit;
            BtnDelete.Visible = delete != null;
            if (delete != null) BtnDelete.Text = delete;
        }

        protected void SetSummary(string text)
        {
            lblSummary.Text = text ?? "";
        }

        /// <summary>Call when a filter changes: back to page 1 and reload.</summary>
        protected void ReloadFromStart()
        {
            page = 1;
            LoadPage();
        }

        public void RefreshData()
        {
            LoadPage();
        }

        protected int SelectedId
        {
            get
            {
                DataGridViewRow r = Grid.CurrentRow;
                if (r == null || !Grid.Columns.Contains("id")) return 0;
                return Convert.ToInt32(r.Cells["id"].Value);
            }
        }

        protected string CellText(string column)
        {
            DataGridViewRow r = Grid.CurrentRow;
            if (r == null || !Grid.Columns.Contains(column)) return "";
            return Convert.ToString(r.Cells[column].Value);
        }

        protected Form ParentDialogOwner { get { return FindForm(); } }

        // ---------- loading + paging ----------

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && !DesignMode) LoadPage();       // refresh every time the screen is shown
        }

        private void LoadPage()
        {
            try
            {
                string search = SearchBox.Text.Trim();
                int count;
                DataTable dt = LoadRows(search, (page - 1) * pageSize, pageSize, out count);

                pages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
                if (page > pages)                           // e.g. deleted the last row of the last page
                {
                    page = pages;
                    dt = LoadRows(search, (page - 1) * pageSize, pageSize, out count);
                }
                if (page < 1) page = 1;
                total = count;

                BindGrid(dt);
                ShowNotice(total == 0 ? (search.Length > 0 ? "No records match your search." : "No records yet.") : null, false);
                UpdatePager();
            }
            catch (MySqlException ex)
            {
                BindGrid(null);
                total = 0; pages = 1; page = 1;
                ShowNotice(DbHelper.Describe(ex), true);
                UpdatePager();
            }
            catch (Exception)
            {
                BindGrid(null);
                total = 0; pages = 1; page = 1;
                ShowNotice("Could not load the data. Please try again.", true);
                UpdatePager();
            }
        }

        private void BindGrid(DataTable dt)
        {
            Grid.DataSource = dt;
            foreach (DataGridViewColumn col in Grid.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;   // sorting one page of many would mislead
                if (col.Name == "id") { col.Visible = false; continue; }

                Type t = col.ValueType;
                if (t == typeof(decimal))
                {
                    col.DefaultCellStyle.Format = "N2";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (t == typeof(DateTime))
                {
                    col.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                }
                else if (t == typeof(int) || t == typeof(long))
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            UpdateButtons();
        }

        private void ShowNotice(string text, bool isError)
        {
            lblNotice.Visible = !string.IsNullOrEmpty(text);
            lblNotice.Text = text ?? "";
            lblNotice.ForeColor = isError ? Theme.Error : Theme.Muted;
        }

        private void UpdatePager()
        {
            int first = total == 0 ? 0 : (page - 1) * pageSize + 1;
            int last = Math.Min(page * pageSize, total);
            lblInfo.Text = total == 0 ? "0 records" : "Showing " + first + "\u2013" + last + " of " + total + " records";
            lblPage.Text = "Page " + page + " of " + pages;
            btnFirst.Enabled = btnPrev.Enabled = page > 1;
            btnNext.Enabled = btnLast.Enabled = page < pages;
        }

        private void UpdateButtons()
        {
            bool has = Grid.CurrentRow != null && Grid.Rows.Count > 0;
            BtnEdit.Enabled = has;
            BtnDelete.Enabled = has;
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string col = Grid.Columns[e.ColumnIndex].Name;
            if (col != "Status" && col != "Type") return;

            switch (Convert.ToString(e.Value))
            {
                case "In Stock": case "Active": case "Stock In":
                    e.CellStyle.ForeColor = Theme.Success; break;
                case "Low Stock":
                    e.CellStyle.ForeColor = Theme.Warn; break;
                case "Out of Stock": case "Disabled": case "Locked": case "Stock Out":
                    e.CellStyle.ForeColor = Theme.Error; break;
            }
            e.CellStyle.Font = boldFont;
        }

        // ---------- actions ----------

        private void DoEdit()
        {
            int id = SelectedId;
            if (id == 0) return;
            RunSafe(() => EditClicked(id));
        }

        private void DoDelete()
        {
            int id = SelectedId;
            if (id == 0) return;
            RunSafe(() => DeleteClicked(id));
        }

        private void RunSafe(Action action)
        {
            try
            {
                action();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(DbHelper.Describe(ex), "Database error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception)
            {
                MessageBox.Show("That could not be completed. The record may have been changed or deleted.",
                    "Inventory", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LoadPage();
            }
        }

        // ---------- CSV export (all rows that match the current search, not just this page) ----------

        private void Export()
        {
            try
            {
                int count;
                DataTable dt = LoadRows(SearchBox.Text.Trim(), 0, 1000000, out count);

                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "CSV file (*.csv)|*.csv";
                    dlg.FileName = exportName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".csv";
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;

                    var sb = new StringBuilder();
                    bool first = true;
                    foreach (DataColumn c in dt.Columns)
                    {
                        if (c.ColumnName == "id") continue;
                        if (!first) sb.Append(',');
                        sb.Append(Csv(c.ColumnName, false));
                        first = false;
                    }
                    sb.AppendLine();

                    foreach (DataRow row in dt.Rows)
                    {
                        first = true;
                        foreach (DataColumn c in dt.Columns)
                        {
                            if (c.ColumnName == "id") continue;
                            if (!first) sb.Append(',');
                            sb.Append(Csv(row[c], c.DataType == typeof(string)));
                            first = false;
                        }
                        sb.AppendLine();
                    }

                    File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));   // BOM so Excel reads it right
                    MessageBox.Show("Exported " + dt.Rows.Count + " rows.", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(DbHelper.Describe(ex), "Database error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (IOException)
            {
                MessageBox.Show("Could not write the file. Is it open in Excel?", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("You don't have permission to save there. Pick another folder.", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string Csv(object value, bool isText)
        {
            if (value == null || value == DBNull.Value) return "\"\"";
            string s;
            if (value is DateTime) s = ((DateTime)value).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            else if (value is decimal) s = ((decimal)value).ToString("0.00", CultureInfo.InvariantCulture);
            else s = Convert.ToString(value, CultureInfo.InvariantCulture);

            // Stop spreadsheet formulas: text starting with = + - @ would be run by Excel.
            if (isText && s.Length > 0 && "=+-@".IndexOf(s[0]) >= 0) s = "'" + s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
    }
}
