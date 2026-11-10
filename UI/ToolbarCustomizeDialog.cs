using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class ToolbarCustomizeDialog : Form
    {
        private CheckedListBox chkList = null!;
        private Button btnMoveUp = null!;
        private Button btnMoveDown = null!;
        private Button btnReset = null!;
        private Button btnClose = null!;
        private AppSettings _settings;
        public event Action? ToolbarsChanged;

        private class ToolbarItem
        {
            public string Key { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public bool IsMovable { get; set; } = true;

            public override string ToString() => DisplayName;
        }

        private List<ToolbarItem> _items = new();

        public ToolbarCustomizeDialog()
        {
            _settings = SettingsStore.LoadSettings();
            InitializeUI();
            LoadToolbarItems();
        }

        private void InitializeUI()
        {
            this.Text = "Customize Toolbars & Command Bars";
            this.Size = new Size(580, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            bool isMatrix = ThemeSettings.CurrentTheme == AppTheme.HighContrast;
            bool isDark = isMatrix || ThemeSettings.CurrentTheme != AppTheme.Light;

            this.BackColor = isMatrix ? Color.FromArgb(5, 10, 5) : (isDark ? Color.FromArgb(30, 32, 38) : Color.WhiteSmoke);
            this.ForeColor = isMatrix ? Color.FromArgb(0, 255, 102) : (isDark ? Color.FromArgb(230, 235, 245) : Color.Black);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f) };
            TabPage tabToolbars = new TabPage("Toolbars & Order") { BackColor = this.BackColor };

            Label lblHeading = new Label
            {
                Text = "Toolbars Visibility & Order:",
                Location = new Point(18, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = this.ForeColor
            };
            tabToolbars.Controls.Add(lblHeading);

            Label lblHint = new Label
            {
                Text = "💡 Select a toolbar and use ▲/▼ to change its horizontal order, or drag the ⠿ handle in the app.",
                Location = new Point(18, 38),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.25f),
                ForeColor = isMatrix ? Color.FromArgb(0, 190, 75) : Color.Gray
            };
            tabToolbars.Controls.Add(lblHint);

            chkList = new CheckedListBox
            {
                Location = new Point(20, 62),
                Size = new Size(390, 260),
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = isMatrix ? Color.Black : (isDark ? Color.FromArgb(20, 22, 26) : Color.White),
                ForeColor = this.ForeColor,
                CheckOnClick = true
            };
            chkList.ItemCheck += ChkList_ItemCheck;
            chkList.SelectedIndexChanged += (s, e) => UpdateButtonStates();
            tabToolbars.Controls.Add(chkList);

            // Action Buttons
            btnMoveUp = new Button
            {
                Text = "▲ Move Up",
                Location = new Point(425, 62),
                Width = 120,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                BackColor = isMatrix ? Color.FromArgb(0, 60, 20) : (isDark ? Color.FromArgb(45, 48, 55) : Color.FromArgb(230, 230, 230)),
                ForeColor = this.ForeColor,
                Enabled = false
            };
            btnMoveUp.Click += BtnMoveUp_Click;

            btnMoveDown = new Button
            {
                Text = "▼ Move Down",
                Location = new Point(425, 102),
                Width = 120,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                BackColor = isMatrix ? Color.FromArgb(0, 60, 20) : (isDark ? Color.FromArgb(45, 48, 55) : Color.FromArgb(230, 230, 230)),
                ForeColor = this.ForeColor,
                Enabled = false
            };
            btnMoveDown.Click += BtnMoveDown_Click;

            btnReset = new Button
            {
                Text = "Reset Order",
                Location = new Point(425, 155),
                Width = 120,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = this.ForeColor
            };
            btnReset.Click += BtnReset_Click;

            btnClose = new Button
            {
                Text = "Close",
                Location = new Point(425, 290),
                Width = 120,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = isMatrix ? Color.FromArgb(0, 100, 35) : Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            tabToolbars.Controls.Add(btnMoveUp);
            tabToolbars.Controls.Add(btnMoveDown);
            tabToolbars.Controls.Add(btnReset);
            tabToolbars.Controls.Add(btnClose);

            tabs.TabPages.Add(tabToolbars);
            this.Controls.Add(tabs);
            this.CancelButton = btnClose;
        }

        private bool _isLoading = false;

        private void LoadToolbarItems()
        {
            _isLoading = true;
            chkList.Items.Clear();
            _items.Clear();

            var order = _settings.ToolbarOrder ?? new List<string> { "Standard", "Session", "Tools" };

            // Add movable toolbars in current order
            foreach (var key in order)
            {
                if (key == "Standard")
                {
                    var item = new ToolbarItem { Key = "Standard", DisplayName = "Standard & Vault Toolbar (New, Vault, Scripts)", IsMovable = true };
                    _items.Add(item);
                    chkList.Items.Add(item, _settings.ShowStandardToolbar);
                }
                else if (key == "Session")
                {
                    var item = new ToolbarItem { Key = "Session", DisplayName = "Session Controls (Reconnect, Screenshot, SAS)", IsMovable = true };
                    _items.Add(item);
                    chkList.Items.Add(item, _settings.ShowSessionToolbar);
                }
                else if (key == "Tools")
                {
                    var item = new ToolbarItem { Key = "Tools", DisplayName = "Diagnostic & AI Tools Toolbar (Scanner, AI, Settings)", IsMovable = true };
                    _items.Add(item);
                    chkList.Items.Add(item, _settings.ShowToolsToolbar);
                }
            }

            // Fixed bars at bottom
            var qcItem = new ToolbarItem { Key = "QuickConnect", DisplayName = "Quick Connect Address Bar", IsMovable = false };
            _items.Add(qcItem);
            chkList.Items.Add(qcItem, _settings.ShowQuickConnectBar);

            var sbItem = new ToolbarItem { Key = "StatusBar", DisplayName = "Bottom Status Bar", IsMovable = false };
            _items.Add(sbItem);
            chkList.Items.Add(sbItem, _settings.ShowStatusBar);

            _isLoading = false;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            int idx = chkList.SelectedIndex;
            if (idx >= 0 && idx < _items.Count && _items[idx].IsMovable)
            {
                // Can move up if not first movable item
                btnMoveUp.Enabled = (idx > 0 && _items[idx - 1].IsMovable);
                // Can move down if next item is also movable
                btnMoveDown.Enabled = (idx < _items.Count - 1 && _items[idx + 1].IsMovable);
            }
            else
            {
                btnMoveUp.Enabled = false;
                btnMoveDown.Enabled = false;
            }
        }

        private void BtnMoveUp_Click(object? sender, EventArgs e)
        {
            int idx = chkList.SelectedIndex;
            if (idx > 0 && _items[idx].IsMovable && _items[idx - 1].IsMovable)
            {
                SwapItems(idx, idx - 1);
            }
        }

        private void BtnMoveDown_Click(object? sender, EventArgs e)
        {
            int idx = chkList.SelectedIndex;
            if (idx >= 0 && idx < _items.Count - 1 && _items[idx].IsMovable && _items[idx + 1].IsMovable)
            {
                SwapItems(idx, idx + 1);
            }
        }

        private void SwapItems(int i, int j)
        {
            var itemA = _items[i];
            bool checkedA = chkList.GetItemChecked(i);

            var itemB = _items[j];
            bool checkedB = chkList.GetItemChecked(j);

            _items[i] = itemB;
            _items[j] = itemA;

            // Update ToolbarOrder in settings
            _settings.ToolbarOrder = _items
                .Where(x => x.IsMovable)
                .Select(x => x.Key)
                .ToList();

            SettingsStore.SaveSettings(_settings);

            _isLoading = true;
            chkList.Items[i] = itemB;
            chkList.SetItemChecked(i, checkedB);
            chkList.Items[j] = itemA;
            chkList.SetItemChecked(j, checkedA);
            chkList.SelectedIndex = j;
            _isLoading = false;

            UpdateButtonStates();
            ToolbarsChanged?.Invoke();
        }

        private void ChkList_ItemCheck(object? sender, ItemCheckEventArgs e)
        {
            if (_isLoading) return;

            this.BeginInvoke((MethodInvoker)delegate {
                for (int i = 0; i < _items.Count; i++)
                {
                    bool isChecked = chkList.GetItemChecked(i);
                    switch (_items[i].Key)
                    {
                        case "Standard": _settings.ShowStandardToolbar = isChecked; break;
                        case "Session": _settings.ShowSessionToolbar = isChecked; break;
                        case "Tools": _settings.ShowToolsToolbar = isChecked; break;
                        case "QuickConnect": _settings.ShowQuickConnectBar = isChecked; break;
                        case "StatusBar": _settings.ShowStatusBar = isChecked; break;
                    }
                }
                SettingsStore.SaveSettings(_settings);
                ToolbarsChanged?.Invoke();
            });
        }

        private void BtnReset_Click(object? sender, EventArgs e)
        {
            _settings.ShowStandardToolbar = true;
            _settings.ShowSessionToolbar = true;
            _settings.ShowToolsToolbar = true;
            _settings.ShowQuickConnectBar = true;
            _settings.ShowStatusBar = true;
            _settings.ToolbarOrder = new List<string> { "Standard", "Session", "Tools" };
            SettingsStore.SaveSettings(_settings);

            LoadToolbarItems();
            ToolbarsChanged?.Invoke();
        }
    }
}
