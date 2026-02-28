using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.UI;

namespace FreeRdpWrapper
{
    public partial class Form1 : Form
    {
        private Panel topPanel;
        private TextBox txtHost;
        private TextBox txtUser;
        private TextBox txtPass;
        private CheckBox chkUseSdl;
        private CheckBox chkIgnoreCert;
        private Button btnConnect;

        private SplitContainer splitContainer;
        private TabControl tabControl;
        private TextBox txtLogs;

        public Form1()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "FreeRDP Wrapper (.NET 8 WinForms)";
            this.Width = 1024;
            this.Height = 768;

            topPanel = new Panel { Dock = DockStyle.Top, Height = 50 };
            
            txtHost = new TextBox { Location = new Point(10, 15), Width = 150, Text = "192.168.1.10" };
            txtUser = new TextBox { Location = new Point(170, 15), Width = 100, Text = "Administrator" };
            txtPass = new TextBox { Location = new Point(280, 15), Width = 100, PasswordChar = '*' };
            chkUseSdl = new CheckBox { Location = new Point(390, 15), Width = 120, Text = "Use sdlfreerdp" };
            chkIgnoreCert = new CheckBox { Location = new Point(520, 15), Width = 100, Text = "Ignore Cert", Checked = true };
            btnConnect = new Button { Location = new Point(630, 13), Width = 100, Text = "Connect" };
            btnConnect.Click += BtnConnect_Click;

            topPanel.Controls.Add(new Label { Text = "Host:", Location = new Point(10, 0), AutoSize = true });
            topPanel.Controls.Add(txtHost);
            topPanel.Controls.Add(new Label { Text = "User:", Location = new Point(170, 0), AutoSize = true });
            topPanel.Controls.Add(txtUser);
            topPanel.Controls.Add(new Label { Text = "Pass:", Location = new Point(280, 0), AutoSize = true });
            topPanel.Controls.Add(txtPass);
            topPanel.Controls.Add(chkUseSdl);
            topPanel.Controls.Add(chkIgnoreCert);
            topPanel.Controls.Add(btnConnect);

            splitContainer = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            splitContainer.SplitterDistance = this.Height - 200;

            tabControl = new TabControl { Dock = DockStyle.Fill };
            txtLogs = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true };

            splitContainer.Panel1.Controls.Add(tabControl);
            splitContainer.Panel2.Controls.Add(txtLogs);

            this.Controls.Add(splitContainer);
            this.Controls.Add(topPanel);
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            string host = txtHost.Text;
            if (string.IsNullOrWhiteSpace(host)) return;

            var config = new RdpConfig
            {
                Host = host,
                User = txtUser.Text,
                Pass = txtPass.Text,
                UseSdl = chkUseSdl.Checked,
                IgnoreCert = chkIgnoreCert.Checked,
                DynamicResolution = true,
                AdditionalFlags = "/gfx:avc444"
            }; // Removing /f because it might force full screen independently of our embedding.

            string exeName = config.UseSdl ? "sdl3-freerdp.exe" : "wfreerdp.exe";
            string relativeBuildPath = Path.Combine("Compiled", "bin", exeName);
            
            string possiblePath = exeName;
            string? currentDir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(currentDir))
            {
                // Unified path where cmake --install put both exe and DLLs
                string testPath = Path.GetFullPath(Path.Combine(currentDir, "Bin", "FreeRDP", relativeBuildPath));
                if (File.Exists(testPath))
                {
                    possiblePath = testPath;
                    break;
                }
                
                string fallbackPath = Path.GetFullPath(Path.Combine(currentDir, "Bin", "FreeRDP", exeName));
                if (File.Exists(fallbackPath))
                {
                    possiblePath = fallbackPath;
                    break;
                }

                string? parentDir = Path.GetDirectoryName(currentDir);
                if (parentDir == currentDir) break;
                currentDir = parentDir;
            }

            var tabPage = new TabPage(host);
            var container = new FreeRdpContainer { Dock = DockStyle.Fill };
            
            container.OnLogMessage += (msg) => {
                if (txtLogs.IsHandleCreated && !txtLogs.Disposing)
                {
                    txtLogs.Invoke((MethodInvoker)delegate {
                        txtLogs.AppendText($"[{host}] {msg}\r\n");
                    });
                }
            };

            container.OnDisconnected += () => {
                if (tabControl.IsHandleCreated && !tabControl.Disposing)
                {
                    tabControl.Invoke((MethodInvoker)delegate {
                        txtLogs.AppendText($"[{host}] Session Disconnected.\r\n");
                        tabControl.TabPages.Remove(tabPage);
                        container.Dispose();
                    });
                }
            };

            tabPage.Controls.Add(container);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;

            try
            {
                container.LaunchSession(config, possiblePath);
            }
            catch (Exception ex)
            {
                txtLogs.AppendText($"[{host}] Failed to launch {possiblePath}: {ex.Message}\r\n");
            }
        }
    }
}
