using System;
using System.Drawing;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class MasterPasswordDialog : Form
    {
        private bool _isSetupMode;
        private string _targetHash;
        
        private TextBox txtPassword = null!;
        private TextBox txtConfirm = null!;
        private CheckBox chkRemember = null!;

        public bool IsAuthenticated { get; private set; } = false;

        public MasterPasswordDialog(bool isSetupMode, string targetHash = "")
        {
            _isSetupMode = isSetupMode;
            _targetHash = targetHash;
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = _isSetupMode ? "Setup Master Password" : "Login Required";
            this.Size = new Size(360, _isSetupMode ? 320 : 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            bool isMatrix = ThemeSettings.CurrentTheme == AppTheme.HighContrast;
            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;
            this.BackColor = isMatrix ? Color.FromArgb(5, 10, 5) : (isDark ? Color.FromArgb(30, 30, 30) : Color.WhiteSmoke);
            this.ForeColor = isMatrix ? Color.FromArgb(0, 255, 102) : (isDark ? Color.FromArgb(220, 220, 220) : Color.Black);

            int y = 18;

            Label lblDesc = new Label
            {
                Text = _isSetupMode 
                    ? "Welcome! Please protect your saved connections by creating a Master Password." 
                    : "Enter your Master Password to access your saved connections.",
                Location = new Point(20, y),
                Width = 300,
                Height = 36,
                TextAlign = ContentAlignment.TopCenter,
                ForeColor = this.ForeColor
            };
            this.Controls.Add(lblDesc);
            y += 44;

            Label lblPass = new Label { Text = "Password:", Location = new Point(20, y), Width = 90, ForeColor = this.ForeColor };
            this.Controls.Add(lblPass);
            
            txtPassword = new TextBox { Location = new Point(115, y), Width = 205, PasswordChar = '*' };
            this.Controls.Add(txtPassword);
            y += 36;

            if (_isSetupMode)
            {
                Label lblConfirm = new Label { Text = "Confirm:", Location = new Point(20, y), Width = 90, ForeColor = this.ForeColor };
                this.Controls.Add(lblConfirm);
                
                txtConfirm = new TextBox { Location = new Point(115, y), Width = 205, PasswordChar = '*' };
                this.Controls.Add(txtConfirm);
                y += 36;
            }

            chkRemember = new CheckBox
            {
                Text = "Remember password on this machine",
                Location = new Point(25, y),
                Width = 300,
                Checked = MasterPasswordTokenManager.IsTokenSaved(),
                ForeColor = this.ForeColor
            };
            this.Controls.Add(chkRemember);
            y += 36;

            Button btnSubmit = new Button 
            { 
                Text = _isSetupMode ? "Save" : "Unlock", 
                Location = new Point(115, y), 
                Width = 95,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = isMatrix ? Color.FromArgb(0, 100, 35) : Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += BtnSubmit_Click;
            this.Controls.Add(btnSubmit);

            Button btnCancel = new Button 
            { 
                Text = "Cancel", 
                Location = new Point(225, y), 
                Width = 95,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                ForeColor = this.ForeColor
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSubmit;
            this.CancelButton = btnCancel;
        }

        private void BtnSubmit_Click(object? sender, EventArgs e)
        {
            string pass = txtPassword.Text;

            if (string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Password cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isSetupMode)
            {
                if (pass != txtConfirm.Text)
                {
                    MessageBox.Show("Passwords do not match.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Save new password hash
                string hash = SettingsStore.ComputeSha256Hash(pass);
                var settings = SettingsStore.LoadSettings();
                settings.MasterPasswordHash = hash;
                SettingsStore.SaveSettings(settings);

                if (chkRemember.Checked)
                {
                    MasterPasswordTokenManager.SaveToken(hash);
                }
                else
                {
                    MasterPasswordTokenManager.ClearToken();
                }

                IsAuthenticated = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                // Verify password
                string inputHash = SettingsStore.ComputeSha256Hash(pass);
                if (inputHash == _targetHash)
                {
                    if (chkRemember.Checked)
                    {
                        MasterPasswordTokenManager.SaveToken(inputHash);
                    }
                    else
                    {
                        MasterPasswordTokenManager.ClearToken();
                    }

                    IsAuthenticated = true;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Incorrect Master Password.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
        }
    }
}
