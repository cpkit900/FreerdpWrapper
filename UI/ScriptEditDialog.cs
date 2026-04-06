using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class ScriptEditDialog : Form
    {
        public SavedScript Script { get; private set; }
        
        private TextBox txtName = null!;
        private TextBox txtContent = null!;

        public ScriptEditDialog(SavedScript? existingScript = null)
        {
            Script = existingScript ?? new SavedScript { Name = "New Script" };
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Edit SSH Script";
            this.Size = new Size(500, 400);
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.CenterParent;

            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;
            this.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.WhiteSmoke;
            this.ForeColor = isDark ? Color.FromArgb(220, 220, 220) : Color.Black;

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 40 };
            pnlTop.Controls.Add(new Label { Text = "Script Name:", Location = new Point(10, 10), Width = 80 });
            txtName = new TextBox { Location = new Point(90, 7), Width = 380, Text = Script.Name };
            pnlTop.Controls.Add(txtName);

            txtContent = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                Multiline = true, 
                ScrollBars = ScrollBars.Vertical, 
                Font = new Font("Consolas", 10f),
                Text = Script.Content,
                Margin = new Padding(10)
            };

            Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            Button btnSave = new Button { Text = "Save", Location = new Point(this.Width - 100, 10), Width = 80, Anchor = AnchorStyles.Right };
            btnSave.Click += BtnSave_Click;
            pnlBottom.Controls.Add(btnSave);

            this.Controls.Add(txtContent);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlBottom);

            if (isDark)
            {
                txtContent.BackColor = Color.FromArgb(30, 30, 30);
                txtContent.ForeColor = Color.LightGray;
                txtName.BackColor = Color.FromArgb(30, 30, 30);
                txtName.ForeColor = Color.White;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter a script name.");
                return;
            }
            Script.Name = txtName.Text;
            Script.Content = txtContent.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
