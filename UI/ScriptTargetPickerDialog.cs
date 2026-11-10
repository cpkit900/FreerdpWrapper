using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class ScriptTargetPickerDialog : Form
    {
        private readonly List<SavedConnection> _allConnections;
        private readonly SavedScript _script;
        private readonly HashSet<string> _checkedConnectionIds = new(StringComparer.OrdinalIgnoreCase);

        private TextBox txtSearch = null!;
        private ComboBox cmbProtocol = null!;
        private ComboBox cmbGroup = null!;
        private ListView lvServers = null!;
        private Label lblSelectedCount = null!;
        private Button btnSelectAll = null!;
        private Button btnDeselectAll = null!;
        private Button btnExecute = null!;
        private Button btnCancel = null!;

        public List<SavedConnection> SelectedConnections { get; private set; } = new();

        public ScriptTargetPickerDialog(List<SavedConnection> connections, SavedScript script, SavedConnection? preselectedConnection = null)
        {
            _allConnections = connections ?? new List<SavedConnection>();
            _script = script;

            if (preselectedConnection != null)
            {
                _checkedConnectionIds.Add(preselectedConnection.Id);
            }

            SetupUI();
            PopulateGroupCombo();
            ApplyFilters();
        }

        private void SetupUI()
        {
            this.Text = $"Run Automation Script — {_script.Name}";
            this.Size = new Size(820, 580);
            this.MinimumSize = new Size(680, 440);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            bool isMatrix = ThemeSettings.CurrentTheme == AppTheme.HighContrast;
            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;

            Color bg = isMatrix ? Color.FromArgb(2, 6, 2) : (isDark ? Color.FromArgb(28, 30, 34) : Color.FromArgb(248, 249, 250));
            Color cardBg = isMatrix ? Color.FromArgb(4, 10, 4) : (isDark ? Color.FromArgb(34, 37, 42) : Color.White);
            Color inputBg = isMatrix ? Color.FromArgb(6, 14, 6) : (isDark ? Color.FromArgb(20, 22, 26) : Color.White);
            Color fg = isMatrix ? Color.FromArgb(0, 255, 102) : (isDark ? Color.FromArgb(230, 235, 242) : Color.FromArgb(30, 30, 30));
            Color subFg = isMatrix ? Color.FromArgb(0, 190, 75) : (isDark ? Color.FromArgb(160, 165, 175) : Color.FromArgb(100, 105, 115));
            Color border = isMatrix ? Color.FromArgb(0, 160, 50) : (isDark ? Color.FromArgb(55, 60, 70) : Color.FromArgb(215, 220, 225));
            Color primaryBtnBg = isMatrix ? Color.FromArgb(0, 100, 35) : Color.FromArgb(0, 120, 215);

            this.BackColor = bg;
            this.ForeColor = fg;

            // ════════════════════════════════════════════════════════════════
            // TOP HEADER PANEL
            // ════════════════════════════════════════════════════════════════
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(16, 12, 16, 8),
                BackColor = cardBg
            };

            Label lblTitle = new Label
            {
                Text = $"📜 Target Servers for '{_script.Name}'",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = fg,
                AutoSize = true,
                Location = new Point(14, 12)
            };

            Label lblSubtitle = new Label
            {
                Text = "Search and select one or more targets from your workspace. Windows targets run via PowerShell WinRM; SSH targets run via OpenSSH.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = subFg,
                AutoSize = true,
                Location = new Point(16, 38)
            };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // ════════════════════════════════════════════════════════════════
            // FILTER TOOLBAR
            // ════════════════════════════════════════════════════════════════
            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(16, 6, 16, 6),
                BackColor = bg
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "🔍 Search by name, host, IP, group, username...",
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(16, 8),
                Width = 280,
                BackColor = inputBg,
                ForeColor = fg
            };
            txtSearch.TextChanged += (s, e) => ApplyFilters();

            cmbProtocol = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(306, 8),
                Width = 150,
                BackColor = inputBg,
                ForeColor = fg
            };
            cmbProtocol.Items.AddRange(new object[] { "All Protocols", "SSH Only", "Windows / WinRM Only" });
            cmbProtocol.SelectedIndex = 0;
            cmbProtocol.SelectedIndexChanged += (s, e) => ApplyFilters();

            cmbGroup = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(466, 8),
                Width = 140,
                BackColor = inputBg,
                ForeColor = fg
            };
            cmbGroup.SelectedIndexChanged += (s, e) => ApplyFilters();

            btnSelectAll = new Button
            {
                Text = "Select All",
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(616, 7),
                Width = 80,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = cardBg,
                ForeColor = fg
            };
            btnSelectAll.Click += (s, e) => SelectAllFiltered(true);

            btnDeselectAll = new Button
            {
                Text = "Clear All",
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(704, 7),
                Width = 80,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = cardBg,
                ForeColor = fg
            };
            btnDeselectAll.Click += (s, e) => SelectAllFiltered(false);

            pnlFilter.Controls.Add(txtSearch);
            pnlFilter.Controls.Add(cmbProtocol);
            pnlFilter.Controls.Add(cmbGroup);
            pnlFilter.Controls.Add(btnSelectAll);
            pnlFilter.Controls.Add(btnDeselectAll);

            // ════════════════════════════════════════════════════════════════
            // SERVER LISTVIEW
            // ════════════════════════════════════════════════════════════════
            lvServers = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9f),
                BackColor = inputBg,
                ForeColor = fg
            };

            lvServers.Columns.Add("Server Name", 230);
            lvServers.Columns.Add("Protocol", 120);
            lvServers.Columns.Add("Host / IP", 170);
            lvServers.Columns.Add("Group", 120);
            lvServers.Columns.Add("User", 110);

            lvServers.ItemChecked += LvServers_ItemChecked;
            lvServers.DoubleClick += LvServers_DoubleClick;

            // ════════════════════════════════════════════════════════════════
            // BOTTOM ACTION BAR
            // ════════════════════════════════════════════════════════════════
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = cardBg
            };

            lblSelectedCount = new Label
            {
                Text = "0 targets selected",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = subFg,
                Location = new Point(16, 18),
                AutoSize = true
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 9f),
                Width = 90,
                Height = 32,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(570, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = cardBg,
                ForeColor = fg,
                DialogResult = DialogResult.Cancel
            };

            btnExecute = new Button
            {
                Text = "▶ Execute Script",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Width = 140,
                Height = 32,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(668, 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = primaryBtnBg,
                ForeColor = Color.White,
                Enabled = false
            };
            btnExecute.Click += BtnExecute_Click;

            pnlBottom.Controls.Add(lblSelectedCount);
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnExecute);

            this.Controls.Add(lvServers);
            this.Controls.Add(pnlFilter);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlBottom);

            this.CancelButton = btnCancel;
        }

        private void PopulateGroupCombo()
        {
            cmbGroup.Items.Clear();
            cmbGroup.Items.Add("All Groups");

            var groups = _allConnections
                .Select(c => string.IsNullOrWhiteSpace(c.Group) ? "Default" : c.Group.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g)
                .ToList();

            foreach (var g in groups)
            {
                cmbGroup.Items.Add(g);
            }

            cmbGroup.SelectedIndex = 0;
        }

        private bool _isUpdatingList = false;

        private void ApplyFilters()
        {
            _isUpdatingList = true;
            lvServers.BeginUpdate();
            lvServers.Items.Clear();

            string search = txtSearch.Text.Trim();
            string selectedProto = cmbProtocol.SelectedItem?.ToString() ?? "All Protocols";
            string selectedGroup = cmbGroup.SelectedItem?.ToString() ?? "All Groups";

            var filtered = _allConnections.Where(c =>
            {
                // Protocol filter
                if (selectedProto == "SSH Only" && !c.Protocol.IsSsh()) return false;
                if (selectedProto == "Windows / WinRM Only" && c.Protocol.IsSsh()) return false;

                // Group filter
                string groupName = string.IsNullOrWhiteSpace(c.Group) ? "Default" : c.Group.Trim();
                if (selectedGroup != "All Groups" && !string.Equals(groupName, selectedGroup, StringComparison.OrdinalIgnoreCase)) return false;

                // Text search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    bool matchName = c.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false;
                    bool matchHost = c.Host?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false;
                    bool matchGroup = c.Group?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false;
                    bool matchUser = c.User?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false;
                    if (!matchName && !matchHost && !matchGroup && !matchUser) return false;
                }

                return true;
            }).ToList();

            foreach (var conn in filtered)
            {
                string protoStr = conn.Protocol.IsSsh() ? (conn.Protocol == ConnectionProtocol.SSH_Putty ? "SSH (PuTTY)" : "SSH") : "Windows (WinRM)";
                var lvi = new ListViewItem(conn.Name)
                {
                    Tag = conn
                };
                lvi.SubItems.Add(protoStr);
                lvi.SubItems.Add(conn.Host);
                lvi.SubItems.Add(string.IsNullOrWhiteSpace(conn.Group) ? "Default" : conn.Group);
                lvi.SubItems.Add(conn.User ?? string.Empty);

                lvi.Checked = _checkedConnectionIds.Contains(conn.Id);
                lvServers.Items.Add(lvi);
            }

            lvServers.EndUpdate();
            _isUpdatingList = false;

            UpdateSelectionSummary();
        }

        private void LvServers_ItemChecked(object? sender, ItemCheckedEventArgs e)
        {
            if (_isUpdatingList) return;

            if (e.Item.Tag is SavedConnection conn)
            {
                if (e.Item.Checked)
                {
                    _checkedConnectionIds.Add(conn.Id);
                }
                else
                {
                    _checkedConnectionIds.Remove(conn.Id);
                }
            }

            UpdateSelectionSummary();
        }

        private void LvServers_DoubleClick(object? sender, EventArgs e)
        {
            if (lvServers.SelectedItems.Count > 0)
            {
                var item = lvServers.SelectedItems[0];
                item.Checked = !item.Checked;
            }
        }

        private void SelectAllFiltered(bool check)
        {
            _isUpdatingList = true;
            lvServers.BeginUpdate();
            foreach (ListViewItem item in lvServers.Items)
            {
                item.Checked = check;
                if (item.Tag is SavedConnection conn)
                {
                    if (check) _checkedConnectionIds.Add(conn.Id);
                    else _checkedConnectionIds.Remove(conn.Id);
                }
            }
            lvServers.EndUpdate();
            _isUpdatingList = false;

            UpdateSelectionSummary();
        }

        private void UpdateSelectionSummary()
        {
            int count = _checkedConnectionIds.Count;
            int total = _allConnections.Count;
            lblSelectedCount.Text = $"{count} targets selected (out of {total} total)";
            btnExecute.Enabled = count > 0;
            btnExecute.Text = count > 1 ? $"▶ Execute on ({count}) Targets" : "▶ Execute Script";
        }

        private void BtnExecute_Click(object? sender, EventArgs e)
        {
            SelectedConnections = _allConnections
                .Where(c => _checkedConnectionIds.Contains(c.Id))
                .ToList();

            if (SelectedConnections.Count == 0)
            {
                MessageBox.Show("Please select at least one target server.", "No Targets Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

