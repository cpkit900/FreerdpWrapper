using System;
using System.Drawing;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class ScriptEditDialog : Form
    {
        public SavedScript Script { get; private set; }
        private readonly bool _isNew;

        private TextBox txtName = null!;
        private TextBox txtContent = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        public ScriptEditDialog(SavedScript? existingScript = null)
        {
            _isNew = (existingScript == null);
            Script = existingScript ?? new SavedScript { Name = "New Script", Content = "#!/bin/bash\n# Automation script\n\nuname -a\nuptime\n" };
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = _isNew ? "➕ Add New Automation Script" : $"✏️ Edit Script — {Script.Name}";
            this.Size = new Size(580, 460);
            this.MinimumSize = new Size(460, 340);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;

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

            // ════════════════════════════════════════════════════════════════
            // TOP PANEL (Name & Description)
            // ════════════════════════════════════════════════════════════════
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                Padding = new Padding(12, 10, 12, 6)
            };

            Label lblName = new Label
            {
                Text = "Script Name:",
                Location = new Point(12, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = fg
            };

            txtName = new TextBox
            {
                Location = new Point(105, 9),
                Width = 445,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9.5f),
                Text = Script.Name,
                PlaceholderText = "e.g., Check Disk Usage, Update Packages, Restart NGINX",
                BackColor = inputBg,
                ForeColor = fg
            };

            Label lblSub = new Label
            {
                Text = "Scripts can be executed on any SSH server or batch-executed on checked connections.",
                Location = new Point(12, 38),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                ForeColor = subFg
            };

            pnlTop.Controls.Add(lblName);
            pnlTop.Controls.Add(txtName);
            pnlTop.Controls.Add(lblSub);

            // ════════════════════════════════════════════════════════════════
            // BOTTOM ACTION BAR (Save & Cancel buttons — Always visible!)
            // ════════════════════════════════════════════════════════════════
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

            Label lblHint = new Label
            {
                Text = "Tip: Supports multi-line Bash, PowerShell, or Batch commands.",
                Location = new Point(12, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 8f),
                ForeColor = subFg
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
                Text = "💾  Save Script",
                Location = new Point(pnlBottom.Width - 235, 10),
                Width = 125,
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = saveBg,
                ForeColor = Color.White
            };
            btnSave.FlatAppearance.BorderColor = isMatrix ? Color.FromArgb(0, 255, 102) : border;
            btnSave.Click += BtnSave_Click;

            pnlBottom.Controls.Add(lblHint);
            pnlBottom.Controls.Add(btnSave);
            pnlBottom.Controls.Add(btnCancel);

            // ════════════════════════════════════════════════════════════════
            // CENTER SCRIPT CONTENT EDITOR (Fill)
            // ════════════════════════════════════════════════════════════════
            Panel pnlEditorWrapper = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 4, 12, 4)
            };

            txtContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 10.5f),
                Text = Script.Content,
                BackColor = inputBg,
                ForeColor = fg
            };

            pnlEditorWrapper.Controls.Add(txtContent);

            // IMPORTANT WinForms Docking Order:
            // Add DockStyle.Fill LAST so Top and Bottom reserve their space properly!
            this.Controls.Add(pnlEditorWrapper);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBottom);

            pnlTop.BringToFront();
            pnlBottom.BringToFront();
            pnlEditorWrapper.BringToFront();

            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a name for the script.", "Script Name Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            Script.Name = name;
            Script.Content = txtContent.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
