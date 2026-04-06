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
            this.Size = new Size(350, _isSetupMode ? 280 : 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;
            this.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.WhiteSmoke;
            this.ForeColor = isDark ? Color.FromArgb(220, 220, 220) : Color.Black;

            int y = 20;

            Label lblDesc = new Label
            {
                Text = _isSetupMode 
                    ? "Welcome! Please protect your saved connections by creating a Master Password." 
                    : "Enter your Master Password to access your saved connections.",
                Location = new Point(20, y),
                Width = 290,
                Height = 40,
                TextAlign = ContentAlignment.TopCenter
            };
            this.Controls.Add(lblDesc);
            y += 50;

            Label lblPass = new Label { Text = "Password:", Location = new Point(20, y), Width = 100 };
            this.Controls.Add(lblPass);
            
            txtPassword = new TextBox { Location = new Point(120, y), Width = 190, PasswordChar = '*' };
            this.Controls.Add(txtPassword);
            y += 40;

            if (_isSetupMode)
            {
                Label lblConfirm = new Label { Text = "Confirm:", Location = new Point(20, y), Width = 100 };
                this.Controls.Add(lblConfirm);
                
                txtConfirm = new TextBox { Location = new Point(120, y), Width = 190, PasswordChar = '*' };
                this.Controls.Add(txtConfirm);
                y += 40;
            }

            Button btnSubmit = new Button 
            { 
                Text = _isSetupMode ? "Save" : "Unlock", 
                Location = new Point(120, y), 
                Width = 90 
            };
            btnSubmit.Click += BtnSubmit_Click;
            this.Controls.Add(btnSubmit);

            Button btnCancel = new Button { Text = "Cancel", Location = new Point(220, y), Width = 90 };
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
