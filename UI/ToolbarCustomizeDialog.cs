using System;
using System.Drawing;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class ToolbarCustomizeDialog : Form
    {
        private CheckedListBox chkList = null!;
        private AppSettings _settings;
        public event Action? ToolbarsChanged;

        public ToolbarCustomizeDialog()
        {
            _settings = SettingsStore.LoadSettings();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Customization";
            this.Size = new Size(540, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            bool isMatrix = ThemeSettings.CurrentTheme == AppTheme.HighContrast;
            bool isDark = isMatrix || ThemeSettings.CurrentTheme != AppTheme.Light;

            this.BackColor = isMatrix ? Color.FromArgb(5, 10, 5) : (isDark ? Color.FromArgb(30, 32, 38) : Color.WhiteSmoke);
            this.ForeColor = isMatrix ? Color.FromArgb(0, 255, 102) : (isDark ? Color.FromArgb(230, 235, 245) : Color.Black);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f) };
            TabPage tabToolbars = new TabPage("Toolbars") { BackColor = this.BackColor };

            Label lblHeading = new Label
            {
                Text = "Toolbars & Command Bars:",
                Location = new Point(18, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            tabToolbars.Controls.Add(lblHeading);

            chkList = new CheckedListBox
            {
                Location = new Point(20, 42),
                Size = new Size(360, 220),
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = isMatrix ? Color.Black : (isDark ? Color.FromArgb(20, 22, 26) : Color.White),
                ForeColor = this.ForeColor,
                CheckOnClick = true
            };

            chkList.Items.Add("Standard & Vault Toolbar (New, Import, Export)", _settings.ShowStandardToolbar);
            chkList.Items.Add("Session Controls (Reconnect, Screenshot, SAS)", _settings.ShowSessionToolbar);
            chkList.Items.Add("Diagnostic & AI Tools Toolbar", _settings.ShowToolsToolbar);
            chkList.Items.Add("Quick Connect Address Bar", _settings.ShowQuickConnectBar);
            chkList.Items.Add("Bottom Status Bar", _settings.ShowStatusBar);

            chkList.ItemCheck += (s, e) => {
                this.BeginInvoke((MethodInvoker)delegate {
                    _settings.ShowStandardToolbar = chkList.GetItemChecked(0);
                    _settings.ShowSessionToolbar = chkList.GetItemChecked(1);
                    _settings.ShowToolsToolbar = chkList.GetItemChecked(2);
                    _settings.ShowQuickConnectBar = chkList.GetItemChecked(3);
                    _settings.ShowStatusBar = chkList.GetItemChecked(4);
                    SettingsStore.SaveSettings(_settings);
                    ToolbarsChanged?.Invoke();
                });
            };

            tabToolbars.Controls.Add(chkList);

            // Right Action Buttons (DevExpress/RRD Style)
            Button btnReset = new Button
            {
                Text = "Reset",
                Location = new Point(400, 42),
                Width = 90,
                Height = 30,
                FlatStyle = FlatStyle.Flat
            };
            btnReset.Click += (s, e) => {
                _settings.ShowStandardToolbar = true;
                _settings.ShowSessionToolbar = true;
                _settings.ShowToolsToolbar = true;
                _settings.ShowQuickConnectBar = true;
                _settings.ShowStatusBar = true;
                SettingsStore.SaveSettings(_settings);

                for (int i = 0; i < chkList.Items.Count; i++)
                    chkList.SetItemChecked(i, true);

                ToolbarsChanged?.Invoke();
            };

            Button btnClose = new Button
            {
                Text = "Close",
                Location = new Point(400, 80),
                Width = 90,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            tabToolbars.Controls.Add(btnReset);
            tabToolbars.Controls.Add(btnClose);

            tabs.TabPages.Add(tabToolbars);
            this.Controls.Add(tabs);
        }
    }
}
