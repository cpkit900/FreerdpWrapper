using System;
using System.Drawing;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class AiAssistantPanel : UserControl
    {
        private TextBox txtInput = null!;
        private TextBox txtOutput = null!;
        private Button btnAsk = null!;
        private Button btnDiagnoseLogs = null!;
        private Button btnScriptWin = null!;
        private Button btnScriptLinux = null!;
        private Button btnSettings = null!;
        private Button btnCopy = null!;
        private Label lblStatus = null!;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Func<string>? GetActiveLogsFunc { get; set; }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public AiSettings Settings { get; set; } = new AiSettings();

        public AiAssistantPanel()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;
            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;
            this.BackColor = isDark ? Color.FromArgb(32, 33, 36) : Color.WhiteSmoke;
            this.ForeColor = isDark ? Color.FromArgb(232, 234, 237) : Color.Black;

            // Top Header & Action Buttons
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 75, Padding = new Padding(6) };
            
            Label lblTitle = new Label 
            { 
                Text = "🤖 FreeRDP AI Remote Ops Copilot", 
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(8, 6),
                AutoSize = true
            };

            lblStatus = new Label
            {
                Text = "Engine: Built-in Knowledgebase (Offline)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray,
                Location = new Point(10, 28),
                AutoSize = true
            };

            btnSettings = new Button 
            { 
                Text = "⚙️ AI Config", 
                Location = new Point(340, 8), 
                Size = new Size(100, 26),
                FlatStyle = FlatStyle.Flat 
            };
            btnSettings.Click += BtnSettings_Click;

            FlowLayoutPanel flowQuick = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight
            };

            btnDiagnoseLogs = new Button { Text = "🔍 Diagnose Last Error", AutoSize = true, FlatStyle = FlatStyle.Flat };
            btnDiagnoseLogs.Click += async (s, e) => {
                string logs = GetActiveLogsFunc?.Invoke() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(logs))
                {
                    txtOutput.Text = "No log output available to diagnose yet. Try connecting or paste your error below.";
                    return;
                }
                await RunDiagnosis(logs);
            };

            btnScriptWin = new Button { Text = "🪟 Win: Enable RDP Script", AutoSize = true, FlatStyle = FlatStyle.Flat };
            btnScriptWin.Click += async (s, e) => {
                await GenerateScript("Enable Remote Desktop and open firewall rule", false);
            };

            btnScriptLinux = new Button { Text = "🐧 Linux: Setup XRDP", AutoSize = true, FlatStyle = FlatStyle.Flat };
            btnScriptLinux.Click += async (s, e) => {
                await GenerateScript("Install XRDP and XFCE desktop environment", true);
            };

            btnCopy = new Button { Text = "📋 Copy Response", AutoSize = true, FlatStyle = FlatStyle.Flat };
            btnCopy.Click += (s, e) => {
                if (!string.IsNullOrEmpty(txtOutput.Text))
                {
                    Clipboard.SetText(txtOutput.Text);
                    MessageBox.Show("Response copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            flowQuick.Controls.Add(btnDiagnoseLogs);
            flowQuick.Controls.Add(btnScriptWin);
            flowQuick.Controls.Add(btnScriptLinux);
            flowQuick.Controls.Add(btnCopy);

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblStatus);
            topPanel.Controls.Add(btnSettings);
            topPanel.Controls.Add(flowQuick);

            // Output Console
            txtOutput = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9.5f),
                BackColor = isDark ? Color.FromArgb(20, 20, 22) : Color.White,
                ForeColor = isDark ? Color.FromArgb(220, 220, 220) : Color.Black,
                BorderStyle = BorderStyle.None,
                Text = "👋 Welcome to FreeRDP AI Copilot!\r\n\r\n• Click '🔍 Diagnose Last Error' to analyze connection logs.\r\n• Ask any question below to generate scripts or troubleshoot remote servers."
            };

            // Bottom Prompt Input Bar
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(6) };
            txtInput = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                Font = new Font("Segoe UI", 10f),
                PlaceholderText = "Ask AI: e.g. 'Why did my connection fail?' or 'How to reset RDP port?'"
            };
            txtInput.KeyDown += async (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    await SubmitPrompt();
                }
            };

            btnAsk = new Button 
            { 
                Text = "⚡ Ask AI", 
                Dock = DockStyle.Right, 
                Width = 90, 
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White
            };
            btnAsk.FlatAppearance.BorderSize = 0;
            btnAsk.Click += async (s, e) => await SubmitPrompt();

            bottomPanel.Controls.Add(txtInput);
            bottomPanel.Controls.Add(btnAsk);

            this.Controls.Add(txtOutput);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);
        }

        private async System.Threading.Tasks.Task SubmitPrompt()
        {
            string prompt = txtInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt)) return;

            txtOutput.AppendText($"\r\n\r\n👤 User: {prompt}\r\n🤖 AI: Analyzing...\r\n");
            txtInput.Clear();

            try
            {
                string response = await AiAssistantService.DiagnoseErrorAsync(prompt, Settings);
                txtOutput.AppendText($"{response}\r\n");
                txtOutput.SelectionStart = txtOutput.TextLength;
                txtOutput.ScrollToCaret();
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"Error: {ex.Message}\r\n");
            }
        }

        private async System.Threading.Tasks.Task RunDiagnosis(string logs)
        {
            txtOutput.AppendText($"\r\n\r\n🔍 Diagnosing FreeRDP Session Logs...\r\n");
            string response = await AiAssistantService.DiagnoseErrorAsync(logs, Settings);
            txtOutput.AppendText($"{response}\r\n");
            txtOutput.SelectionStart = txtOutput.TextLength;
            txtOutput.ScrollToCaret();
        }

        private async System.Threading.Tasks.Task GenerateScript(string task, bool isLinux)
        {
            txtOutput.AppendText($"\r\n\r\n🛠️ Generating script for: {task} ({(isLinux ? "Linux" : "Windows")})...\r\n");
            string script = await AiAssistantService.GenerateScriptAsync(task, isLinux, Settings);
            txtOutput.AppendText($"{script}\r\n");
            txtOutput.SelectionStart = txtOutput.TextLength;
            txtOutput.ScrollToCaret();
        }

        private void BtnSettings_Click(object? sender, EventArgs e)
        {
            using var dlg = new Form
            {
                Text = "AI Copilot Configuration",
                Size = new Size(420, 280),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lblProvider = new Label { Text = "Provider:", Location = new Point(20, 20), Width = 100 };
            ComboBox cmbProvider = new ComboBox { Location = new Point(130, 18), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProvider.Items.AddRange(new object[] { "Built-in Offline Knowledgebase", "Google Gemini API", "OpenAI / Local Ollama" });
            cmbProvider.SelectedIndex = (int)Settings.Provider;

            Label lblKey = new Label { Text = "API Key:", Location = new Point(20, 60), Width = 100 };
            TextBox txtKey = new TextBox { Location = new Point(130, 58), Width = 240, PasswordChar = '*', Text = Settings.ApiKey };

            Label lblEndpoint = new Label { Text = "Endpoint URL:", Location = new Point(20, 100), Width = 100 };
            TextBox txtEndpoint = new TextBox { Location = new Point(130, 98), Width = 240, Text = Settings.EndpointUrl };

            Label lblModel = new Label { Text = "Model Name:", Location = new Point(20, 140), Width = 100 };
            TextBox txtModel = new TextBox { Location = new Point(130, 138), Width = 240, Text = Settings.ModelName };

            Button btnSave = new Button { Text = "Save", Location = new Point(200, 190), Width = 80 };
            btnSave.Click += (s2, e2) => {
                Settings.Provider = (AiProviderType)cmbProvider.SelectedIndex;
                Settings.ApiKey = txtKey.Text.Trim();
                Settings.EndpointUrl = txtEndpoint.Text.Trim();
                Settings.ModelName = txtModel.Text.Trim();
                lblStatus.Text = $"Engine: {cmbProvider.SelectedItem}";
                dlg.DialogResult = DialogResult.OK;
                dlg.Close();
            };

            Button btnCancel = new Button { Text = "Cancel", Location = new Point(290, 190), Width = 80 };
            btnCancel.Click += (s2, e2) => dlg.Close();

            dlg.Controls.AddRange(new Control[] { lblProvider, cmbProvider, lblKey, txtKey, lblEndpoint, txtEndpoint, lblModel, txtModel, btnSave, btnCancel });
            dlg.ShowDialog(this);
        }
    }
}
