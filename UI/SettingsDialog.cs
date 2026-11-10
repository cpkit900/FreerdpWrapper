using System;
using System.Drawing;
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
            this.Size = new Size(460, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;
            this.BackColor = isDark ? Color.FromArgb(32, 34, 40) : Color.WhiteSmoke;
            this.ForeColor = isDark ? Color.FromArgb(230, 235, 245) : Color.Black;

            int y = 20;

            // Section 1: Logging Configuration
            Label lblLoggingHeader = new Label
            {
                Text = "📋 Logging & Diagnostics",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true
            };
            this.Controls.Add(lblLoggingHeader);
            y += 30;

            Label lblLevel = new Label { Text = "Logging Level:", Location = new Point(25, y + 3), Width = 130 };
            cmbLogLevel = new ComboBox
            {
                Location = new Point(160, y),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList
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
            y += 35;

            chkAutoShowLogs = new CheckBox
            {
                Text = "Automatically display logs when a session fails or disconnects",
                Location = new Point(25, y),
                Width = 380,
                Checked = _settings.AutoShowLogsOnError
            };
            this.Controls.Add(chkAutoShowLogs);
            y += 45;

            // Section 2: Security & Master Password
            Label lblSecHeader = new Label
            {
                Text = "🔒 Vault Security",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true
            };
            this.Controls.Add(lblSecHeader);
            y += 30;

            bool hasMaster = !string.IsNullOrEmpty(_settings.MasterPasswordHash);
            lblMasterPassStatus = new Label
            {
                Text = hasMaster ? "Status: Master Password Active" : "Status: Protected by Windows DPAPI (Local User)",
                Location = new Point(25, y + 4),
                Width = 260,
                ForeColor = hasMaster ? Color.DodgerBlue : Color.Gray
            };
            btnMasterPassword = new Button
            {
                Text = hasMaster ? "Change / Remove" : "Set Password",
                Location = new Point(290, y),
                Width = 110,
                Height = 28,
                FlatStyle = FlatStyle.Flat
            };
            btnMasterPassword.Click += BtnMasterPassword_Click;
            this.Controls.Add(lblMasterPassStatus);
            this.Controls.Add(btnMasterPassword);
            y += 45;

            // Section 3: Interface Theme
            Label lblThemeHeader = new Label
            {
                Text = "🎨 Visual Appearance",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, y),
                AutoSize = true
            };
            this.Controls.Add(lblThemeHeader);
            y += 30;

            Label lblTheme = new Label { Text = "Interface Theme:", Location = new Point(25, y + 3), Width = 130 };
            cmbTheme = new ComboBox
            {
                Location = new Point(160, y),
                Width = 240,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var t in Enum.GetNames(typeof(AppTheme)))
            {
                cmbTheme.Items.Add(t);
            }
            cmbTheme.SelectedItem = ThemeSettings.CurrentTheme.ToString();
            this.Controls.Add(lblTheme);
            this.Controls.Add(cmbTheme);
            y += 50;

            // Buttons
            Button btnSave = new Button
            {
                Text = "Save",
                Location = new Point(230, y),
                Width = 90,
                Height = 32,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(330, y),
                Width = 80,
                Height = 32,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
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
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            _settings.LogLevel = (LogLevelSetting)cmbLogLevel.SelectedIndex;
            _settings.AutoShowLogsOnError = chkAutoShowLogs.Checked;
            SettingsStore.SaveSettings(_settings);

            if (Enum.TryParse<AppTheme>(cmbTheme.SelectedItem?.ToString(), out var selectedTheme))
            {
                ThemeSettings.CurrentTheme = selectedTheme;
            }

            SettingsChanged?.Invoke();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

