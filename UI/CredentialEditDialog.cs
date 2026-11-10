using System;
using System.Drawing;
using System.Windows.Forms;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.UI
{
    public class CredentialEditDialog : Form
    {
        public SavedCredential Credential { get; private set; }
        private readonly bool _isNew;

        private TextBox txtTitle = null!;
        private TextBox txtUser = null!;
        private TextBox txtPass = null!;
        private TextBox txtDomain = null!;
        private TextBox txtDesc = null!;
        private Button btnTogglePass = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public CredentialEditDialog(SavedCredential? existingCredential = null)
        {
            _isNew = (existingCredential == null);
            Credential = existingCredential ?? new SavedCredential { Title = "New Credential" };
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = _isNew ? "➕ Add New Credential" : $"✏️ Edit Credential — {Credential.Title}";
            this.Size = new Size(520, 390);
            this.MinimumSize = new Size(460, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            bool isMatrix = ThemeSettings.CurrentTheme == AppTheme.HighContrast;
            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;

            Color bg = isMatrix ? Color.FromArgb(2, 6, 2) : (isDark ? Color.FromArgb(28, 30, 34) : Color.FromArgb(248, 249, 250));
            Color inputBg = isMatrix ? Color.FromArgb(6, 14, 6) : (isDark ? Color.FromArgb(20, 22, 26) : Color.White);
            Color fg = isMatrix ? Color.FromArgb(0, 255, 102) : (isDark ? Color.FromArgb(230, 235, 242) : Color.FromArgb(30, 30, 30));
            Color subFg = isMatrix ? Color.FromArgb(0, 190, 75) : (isDark ? Color.FromArgb(160, 165, 175) : Color.FromArgb(100, 105, 115));
            Color border = isMatrix ? Color.FromArgb(0, 160, 50) : (isDark ? Color.FromArgb(55, 60, 70) : Color.FromArgb(215, 220, 225));
            Color saveBg = isMatrix ? Color.FromArgb(0, 100, 35) : Color.FromArgb(0, 120, 215);

            this.BackColor = bg;
            this.ForeColor = fg;

            int y = 16;
            int lblW = 105;
            int inputW = 360;
            int rowH = 34;

            // Header hint
            Label lblHeader = new Label
            {
                Text = "Stored credentials are authenticated and encrypted with AES-256-GCM vault security.",
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.25f),
                ForeColor = subFg
            };
            this.Controls.Add(lblHeader);
            y += 28;

            // Title / Friendly Name
            Label lblTitle = new Label { Text = "Friendly Name:", Location = new Point(20, y + 4), Width = lblW, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtTitle = new TextBox { Text = Credential.Title, Location = new Point(130, y), Width = inputW, Font = new Font("Segoe UI", 9.5f), BackColor = inputBg, ForeColor = fg, PlaceholderText = "e.g., Domain Admin, Ubuntu Root" };
            this.Controls.Add(lblTitle);
            this.Controls.Add(txtTitle);
            y += rowH;

            // Username
            Label lblUser = new Label { Text = "Username:", Location = new Point(20, y + 4), Width = lblW, Font = new Font("Segoe UI", 9f) };
            txtUser = new TextBox { Text = Credential.Username, Location = new Point(130, y), Width = inputW, Font = new Font("Segoe UI", 9.5f), BackColor = inputBg, ForeColor = fg, PlaceholderText = "e.g., administrator or root" };
            this.Controls.Add(lblUser);
            this.Controls.Add(txtUser);
            y += rowH;

            // Password with eye toggle
            Label lblPass = new Label { Text = "Password:", Location = new Point(20, y + 4), Width = lblW, Font = new Font("Segoe UI", 9f) };
            txtPass = new TextBox { Text = Credential.Password, Location = new Point(130, y), Width = inputW - 38, PasswordChar = '●', Font = new Font("Segoe UI", 9.5f), BackColor = inputBg, ForeColor = fg };
            btnTogglePass = new Button
            {
                Text = "👁️",
                Location = new Point(130 + inputW - 34, y - 1),
                Width = 34,
                Height = txtPass.Height + 2,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = isDark ? Color.FromArgb(40, 44, 52) : Color.WhiteSmoke,
                ForeColor = fg
            };
            btnTogglePass.FlatAppearance.BorderColor = border;
            btnTogglePass.Click += (s, e) => {
                if (txtPass.PasswordChar == '●')
                {
                    txtPass.PasswordChar = '\0';
                }
                else
                {
                    txtPass.PasswordChar = '●';
                }
            };
            this.Controls.Add(lblPass);
            this.Controls.Add(txtPass);
            this.Controls.Add(btnTogglePass);
            y += rowH;

            // Domain
            Label lblDomain = new Label { Text = "Domain / Realm:", Location = new Point(20, y + 4), Width = lblW, Font = new Font("Segoe UI", 9f) };
            txtDomain = new TextBox { Text = Credential.Domain, Location = new Point(130, y), Width = inputW, Font = new Font("Segoe UI", 9.5f), BackColor = inputBg, ForeColor = fg, PlaceholderText = "Optional (e.g. WORKGROUP or CORP)" };
            this.Controls.Add(lblDomain);
            this.Controls.Add(txtDomain);
            y += rowH;

            // Description / Notes
            Label lblDesc = new Label { Text = "Notes:", Location = new Point(20, y + 4), Width = lblW, Font = new Font("Segoe UI", 9f) };
            txtDesc = new TextBox { Text = Credential.Description, Location = new Point(130, y), Width = inputW, Height = 48, Multiline = true, Font = new Font("Segoe UI", 9f), BackColor = inputBg, ForeColor = fg, PlaceholderText = "Optional remarks or tags" };
            this.Controls.Add(lblDesc);
            this.Controls.Add(txtDesc);
            y += 56;

            // Bottom action panel
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                Padding = new Padding(12, 8, 12, 8)
            };
            pnlBottom.Paint += (s, e) => {
                using var p = new Pen(border, 1f);
                e.Graphics.DrawLine(p, 0, 0, pnlBottom.Width, 0);
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(pnlBottom.Width - 100, 10),
                Width = 85,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                BackColor = isMatrix ? Color.FromArgb(25, 10, 10) : (isDark ? Color.FromArgb(40, 44, 52) : Color.WhiteSmoke),
                ForeColor = isMatrix ? Color.FromArgb(255, 100, 100) : fg
            };
            btnCancel.FlatAppearance.BorderColor = isMatrix ? Color.FromArgb(160, 40, 40) : border;
            btnCancel.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            btnSave = new Button
            {
                Text = "💾  Save Credential",
                Location = new Point(pnlBottom.Width - 250, 10),
                Width = 140,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = saveBg,
                ForeColor = Color.White
            };
            btnSave.FlatAppearance.BorderColor = isMatrix ? Color.FromArgb(0, 255, 102) : border;
            btnSave.Click += BtnSave_Click;

            pnlBottom.Controls.Add(btnSave);
            pnlBottom.Controls.Add(btnCancel);
            this.Controls.Add(pnlBottom);

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string title = txtTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Please enter a friendly name for this credential.", "Title Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTitle.Focus();
                return;
            }

            Credential.Title = title;
            Credential.Username = txtUser.Text.Trim();
            Credential.Password = txtPass.Text;
            Credential.Domain = txtDomain.Text.Trim();
            Credential.Description = txtDesc.Text.Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}

