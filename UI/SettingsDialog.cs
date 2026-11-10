using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class SettingsDialog : Form
    {
        private ComboBox cmbLogLevel = null!;
        private CheckBox chkAutoShowLogs = null!;
        private ComboBox cmbTheme = null!;
        private Button btnMasterPassword = null!;
        private Label lblMasterPassStatus = null!;
        private CheckBox chkRememberToken = null!;
        private Button btnOpenTokenDir = null!;

        // Navigation header details
        private CheckBox chkNavShowHost = null!;
        private CheckBox chkNavShowUser = null!;
        private CheckBox chkNavShowPing = null!;

        // Window & Layout
        private CheckBox chkSaveWindowPosition = null!;
        private CheckBox chkSaveLayoutAndTabs = null!;
        private AppSettings _settings;

        public event Action? SettingsChanged;

        public SettingsDialog()
        {
            _settings = SettingsStore.LoadSettings();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Settings - AeroRemote Desktop";
            this.Size = new Size(520, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            bool isMatrix = ThemeSettings.CurrentTheme == AppTheme.HighContrast;
            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;

            Color bg = isMatrix ? Color.FromArgb(2, 6, 2) : (isDark ? Color.FromArgb(32, 34, 40) : Color.WhiteSmoke);
            Color fg = isMatrix ? Color.FromArgb(0, 255, 102) : (isDark ? Color.FromArgb(230, 235, 245) : Color.Black);
            Color inputBg = isMatrix ? Color.FromArgb(6, 14, 6) : (isDark ? Color.FromArgb(22, 24, 28) : Color.White);

            this.BackColor = bg;
            this.ForeColor = fg;

            int y = 16;

            // Section 1: Logging Configuration
            Label lblLoggingHeader = new Label
            {
                Text = "📋 Logging & Diagnostics",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = fg
            };
            this.Controls.Add(lblLoggingHeader);
            y += 26;

            Label lblLevel = new Label { Text = "Logging Level:", Location = new Point(25, y + 3), Width = 130, ForeColor = fg };
            cmbLogLevel = new ComboBox
            {
                Location = new Point(160, y),
                Width = 310,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = inputBg,
                ForeColor = fg
            };
            cmbLogLevel.Items.AddRange(new object[] {
                "Off (Silent - Recommended)",
                "Error (Only connection errors)",
                "Info (Standard milestones)",
                "Trace / Verbose (Full FreeRDP debug output)"
            });
            cmbLogLevel.SelectedIndex = (int)_settings.LogLevel;
            this.Controls.Add(lblLevel);
            this.Controls.Add(cmbLogLevel);
            y += 32;

            chkAutoShowLogs = new CheckBox
            {
                Text = "Automatically display logs drawer when a session fails or disconnects",
                Location = new Point(25, y),
                Width = 450,
                Checked = _settings.AutoShowLogsOnError,
                ForeColor = fg
            };
            this.Controls.Add(chkAutoShowLogs);
            y += 36;

            // Section 2: Security & Master Password
            Label lblSecHeader = new Label
            {
                Text = "🔒 Vault Security & Master Password",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = fg
            };
            this.Controls.Add(lblSecHeader);
            y += 26;

            bool hasMaster = !string.IsNullOrEmpty(_settings.MasterPasswordHash);
            lblMasterPassStatus = new Label
            {
                Text = hasMaster ? "Status: Master Password Active" : "Status: Protected by Windows DPAPI (Local User)",
                Location = new Point(25, y + 4),
                Width = 310,
                ForeColor = hasMaster ? Color.DodgerBlue : Color.Gray
            };
            btnMasterPassword = new Button
            {
                Text = hasMaster ? "Change / Remove" : "Set Password",
                Location = new Point(345, y),
                Width = 125,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = inputBg,
                ForeColor = fg
            };
            btnMasterPassword.Click += BtnMasterPassword_Click;
            this.Controls.Add(lblMasterPassStatus);
            this.Controls.Add(btnMasterPassword);
            y += 30;

            chkRememberToken = new CheckBox
            {
                Text = "Remember password on this PC (stores master.bin token)",
                Location = new Point(25, y),
                Width = 340,
                Checked = MasterPasswordTokenManager.IsTokenSaved(),
                Enabled = hasMaster,
                ForeColor = fg
            };
            btnOpenTokenDir = new Button
            {
                Text = "📂 Open Folder",
                Location = new Point(370, y - 2),
                Width = 100,
                Height = 24,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f),
                BackColor = inputBg,
                ForeColor = fg
            };
            btnOpenTokenDir.Click += (s, e) => {
                string dir = Path.GetDirectoryName(MasterPasswordTokenManager.TokenFilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                Process.Start("explorer.exe", dir);
            };
            this.Controls.Add(chkRememberToken);
            this.Controls.Add(btnOpenTokenDir);
            y += 36;

            // Section 3: Navigation Display & Headers
            Label lblNavHeader = new Label
            {
                Text = "🗂️ Navigation Item Headers & Details",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = fg
            };
            this.Controls.Add(lblNavHeader);
            y += 24;

            chkNavShowHost = new CheckBox
            {
                Text = "Show Host / IP address in item titles (unchecked = clean Name only)",
                Location = new Point(25, y),
                Width = 450,
                Checked = _settings.ShowHostInNavTree,
                ForeColor = fg
            };
            this.Controls.Add(chkNavShowHost);
            y += 24;

            chkNavShowUser = new CheckBox
            {
                Text = "Show User / Account in item titles",
                Location = new Point(25, y),
                Width = 450,
                Checked = _settings.ShowUserInNavTree,
                ForeColor = fg
            };
            this.Controls.Add(chkNavShowUser);
            y += 24;

            chkNavShowPing = new CheckBox
            {
                Text = "Show Reachability / Ping status indicators (🟢 Online / 🔴 Offline)",
                Location = new Point(25, y),
                Width = 450,
                Checked = _settings.ShowPingStatusInNavTree,
                ForeColor = fg
            };
            this.Controls.Add(chkNavShowPing);
            y += 34;

            // Section 4: Interface Theme
            Label lblThemeHeader = new Label
            {
                Text = "🎨 Visual Appearance",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = fg
            };
            this.Controls.Add(lblThemeHeader);
            y += 26;

            Label lblTheme = new Label { Text = "Interface Theme:", Location = new Point(25, y + 3), Width = 130, ForeColor = fg };
            cmbTheme = new ComboBox
            {
                Location = new Point(160, y),
                Width = 310,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = inputBg,
                ForeColor = fg
            };
            foreach (var t in Enum.GetNames(typeof(AppTheme)))
            {
                cmbTheme.Items.Add(t);
            }
            cmbTheme.SelectedItem = _settings.Theme.ToString();
            this.Controls.Add(lblTheme);
            this.Controls.Add(cmbTheme);
            y += 38;

            // Section 5: Window & Workspace Layout
            Label lblLayoutHeader = new Label
            {
                Text = "📐 Window & Workspace Layout",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true,
                ForeColor = fg
            };
            this.Controls.Add(lblLayoutHeader);
            y += 24;

            chkSaveWindowPosition = new CheckBox
            {
                Text = "Save window position and size on exit (retained on startup)",
                Location = new Point(25, y),
                Width = 450,
                Checked = _settings.SaveWindowPosition,
                ForeColor = fg
            };
            this.Controls.Add(chkSaveWindowPosition);
            y += 24;

            chkSaveLayoutAndTabs = new CheckBox
            {
                Text = "Save workspace layout and reopen active connection tabs on restart",
                Location = new Point(25, y),
                Width = 450,
                Checked = _settings.SaveLayoutAndTabs,
                ForeColor = fg
            };
            this.Controls.Add(chkSaveLayoutAndTabs);
            y += 42;

            // Buttons
            Button btnSave = new Button
            {
                Text = "Save",
                Location = new Point(285, y),
                Width = 95,
                Height = 32,
                BackColor = isMatrix ? Color.FromArgb(0, 100, 35) : Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(390, y),
                Width = 85,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = inputBg,
                ForeColor = fg
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void BtnMasterPassword_Click(object? sender, EventArgs e)
        {
            bool hasMaster = !string.IsNullOrEmpty(_settings.MasterPasswordHash);
            using var dlg = new MasterPasswordDialog(isSetupMode: !hasMaster, targetHash: _settings.MasterPasswordHash);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _settings = SettingsStore.LoadSettings();
                hasMaster = !string.IsNullOrEmpty(_settings.MasterPasswordHash);
                lblMasterPassStatus.Text = hasMaster ? "Status: Master Password Active" : "Status: Protected by Windows DPAPI";
                lblMasterPassStatus.ForeColor = hasMaster ? Color.DodgerBlue : Color.Gray;
                btnMasterPassword.Text = hasMaster ? "Change / Remove" : "Set Password";
                chkRememberToken.Enabled = hasMaster;
                chkRememberToken.Checked = MasterPasswordTokenManager.IsTokenSaved();
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            _settings.LogLevel = (LogLevelSetting)cmbLogLevel.SelectedIndex;
            _settings.AutoShowLogsOnError = chkAutoShowLogs.Checked;
            _settings.SaveWindowPosition = chkSaveWindowPosition.Checked;
            _settings.SaveLayoutAndTabs = chkSaveLayoutAndTabs.Checked;

            _settings.ShowHostInNavTree = chkNavShowHost.Checked;
            _settings.ShowUserInNavTree = chkNavShowUser.Checked;
            _settings.ShowPingStatusInNavTree = chkNavShowPing.Checked;

            // Handle Remember token on this PC
            if (!string.IsNullOrEmpty(_settings.MasterPasswordHash))
            {
                if (chkRememberToken.Checked)
                {
                    MasterPasswordTokenManager.SaveToken(_settings.MasterPasswordHash);
                }
                else
                {
                    MasterPasswordTokenManager.ClearToken();
                }
            }

            if (Enum.TryParse<AppTheme>(cmbTheme.SelectedItem?.ToString(), out var selectedTheme))
            {
                _settings.Theme = selectedTheme;
                ThemeSettings.CurrentTheme = selectedTheme;
            }

            SettingsStore.SaveSettings(_settings);

            SettingsChanged?.Invoke();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
