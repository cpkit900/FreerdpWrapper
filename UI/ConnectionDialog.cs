using System;
using System.Drawing;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class ConnectionDialog : Form
    {
        public SavedConnection Connection { get; private set; }

        private TextBox txtName = null!;
        private TextBox txtGroup = null!;
        private TextBox txtHost = null!;
        private TextBox txtUser = null!;
        private TextBox txtPass = null!;
        private TextBox txtDomain = null!;
        private ComboBox cmbRdpClient = null!;
        private ComboBox cmbCertSecurity = null!;
        private ComboBox cmbResolution = null!;
        private TextBox txtResWidth = null!;
        private TextBox txtResHeight = null!;
        private CheckBox chkDynamicRes = null!;
        private CheckBox chkEnableDpiScale = null!;
        private TextBox txtRemoteApp = null!;
        private CheckBox chkCredGuard = null!;
        private TextBox txtGwHost = null!;
        private TextBox txtGwDomain = null!;
        private TextBox txtGwUser = null!;
        private TextBox txtGwPass = null!;
        private CheckBox chkGateway = null!;
        private CheckBox chkClipboard = null!;
        private CheckBox chkSound = null!;
        private CheckBox chkMicrophone = null!;
        private CheckBox chkCamera = null!;
        private CheckBox chkDrive = null!;
        private CheckBox chkPrinter = null!;
        private CheckBox chkSmartcard = null!;
        private CheckBox chkEnableUsbRedirection = null!;

        private CheckBox chkMultiMonitor = null!;
        private CheckBox chkAutoReconnect = null!;
        private CheckBox chkAdminSession = null!;
        private CheckBox chkAutoNetworkProfile = null!;
        private CheckBox chkGamingMode = null!;
        private CheckBox chkDisableUdp = null!;

        private ComboBox cmbProtocol = null!;
        private Label lblSshKey = null!;
        private TextBox txtSshKey = null!;
        private Button btnBrowseSshKey = null!;
        private Label lblSshEngine = null!;
        private ComboBox cmbSshEngine = null!;
        private TabControl tabs = null!;
        private TabPage tabGeneral = null!;
        private TabPage tabDisplay = null!;
        private TabPage tabResources = null!;
        private TabPage tabAdvanced = null!;

        public ConnectionDialog(SavedConnection? existing = null)
        {
            Connection = existing ?? new SavedConnection();
            SetupUI();
            LoadData();
        }

        private void SetupUI()
        {
            this.Text = "Connection Properties";
            this.Size = new Size(450, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;
            this.BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.WhiteSmoke;
            this.ForeColor = isDark ? Color.FromArgb(220, 220, 220) : Color.Black;

            tabs = new TabControl { Dock = DockStyle.Top, Height = 400 };

            // General Tab
            tabGeneral = new TabPage("⚙️ General");
            int y = 20, lblW = 100, txtW = 250;
            
            tabGeneral.Controls.Add(new Label { Text = "Protocol:", Location = new Point(20, y), Width = lblW });
            cmbProtocol = new ComboBox { Location = new Point(120, y), Width = txtW, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProtocol.Items.AddRange(new string[] { "RDP", "SSH", "SSH (PuTTY)" });
            tabGeneral.Controls.Add(cmbProtocol); y += 30;

            tabGeneral.Controls.Add(new Label { Text = "Profile Name:", Location = new Point(20, y), Width = lblW });
            txtName = new TextBox { Location = new Point(120, y), Width = txtW };
            tabGeneral.Controls.Add(txtName); y += 30;

            tabGeneral.Controls.Add(new Label { Text = "Group:", Location = new Point(20, y), Width = lblW });
            txtGroup = new TextBox { Location = new Point(120, y), Width = txtW };
            tabGeneral.Controls.Add(txtGroup); y += 30;

            tabGeneral.Controls.Add(new Label { Text = "Host / IP:", Location = new Point(20, y), Width = lblW });
            txtHost = new TextBox { Location = new Point(120, y), Width = txtW };
            tabGeneral.Controls.Add(txtHost); y += 30;

            tabGeneral.Controls.Add(new Label { Text = "Username:", Location = new Point(20, y), Width = lblW });
            txtUser = new TextBox { Location = new Point(120, y), Width = txtW - 38 };
            tabGeneral.Controls.Add(txtUser);

            var btnPickCred = new Button { Text = "🔑", Location = new Point(120 + txtW - 34, y - 1), Width = 34, Height = 24, FlatStyle = FlatStyle.Flat };
            var tip = new ToolTip();
            tip.SetToolTip(btnPickCred, "Fill from Stored Credentials in Vault");
            btnPickCred.Click += (s, e) => {
                var credStore = new CredentialStore();
                var creds = credStore.LoadCredentials();
                if (creds.Count == 0)
                {
                    if (MessageBox.Show("No stored credentials found in vault. Would you like to create one now?", "No Credentials", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        using var dlg = new CredentialEditDialog();
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            creds.Add(dlg.Credential);
                            credStore.SaveCredentials(creds);
                            txtUser.Text = dlg.Credential.Username;
                            txtPass.Text = dlg.Credential.Password;
                            if (!string.IsNullOrEmpty(dlg.Credential.Domain)) txtDomain.Text = dlg.Credential.Domain;
                        }
                    }
                    return;
                }

                var menu = new ContextMenuStrip();
                foreach (var cr in creds)
                {
                    string label = string.IsNullOrEmpty(cr.Domain) ? $"{cr.Title} ({cr.Username})" : $"{cr.Title} ({cr.Domain}\\{cr.Username})";
                    menu.Items.Add($"🔑 {label}", null, (s2, e2) => {
                        txtUser.Text = cr.Username;
                        txtPass.Text = cr.Password;
                        if (!string.IsNullOrEmpty(cr.Domain)) txtDomain.Text = cr.Domain;
                    });
                }
                menu.Show(btnPickCred, new Point(0, btnPickCred.Height));
            };
            tabGeneral.Controls.Add(btnPickCred);
            y += 30;

            tabGeneral.Controls.Add(new Label { Text = "Password:", Location = new Point(20, y), Width = lblW });
            txtPass = new TextBox { Location = new Point(120, y), Width = txtW, PasswordChar = '*' };
            tabGeneral.Controls.Add(txtPass); y += 30;

            tabGeneral.Controls.Add(new Label { Text = "Domain:", Location = new Point(20, y), Width = lblW });
            txtDomain = new TextBox { Location = new Point(120, y), Width = txtW };
            tabGeneral.Controls.Add(txtDomain); y += 40;

            chkCredGuard = new CheckBox { Text = "Enable Remote Credential Guard", Location = new Point(120, y), Width = 250 };
            tabGeneral.Controls.Add(chkCredGuard); y += 30;

            chkAdminSession = new CheckBox { Text = "Connect to Console/Admin Session (+admin)", Location = new Point(120, y), Width = 250 };
            tabGeneral.Controls.Add(chkAdminSession); y += 30;

            lblSshKey = new Label { Text = "SSH Key:", Location = new Point(20, y), Width = lblW };
            tabGeneral.Controls.Add(lblSshKey);
            txtSshKey = new TextBox { Location = new Point(120, y), Width = 170 };
            tabGeneral.Controls.Add(txtSshKey);
            btnBrowseSshKey = new Button { Text = "Browse", Location = new Point(295, y - 1), Width = 75 };
            btnBrowseSshKey.Click += (s, e) => {
                using var ofd = new OpenFileDialog { Filter = "Key Files (*.pem;*.ppk)|*.pem;*.ppk|All Files (*.*)|*.*" };
                if (ofd.ShowDialog() == DialogResult.OK) txtSshKey.Text = ofd.FileName;
            };
            tabGeneral.Controls.Add(btnBrowseSshKey); y += 30;

            lblSshEngine = new Label { Text = "SSH Engine:", Location = new Point(20, y), Width = lblW };
            tabGeneral.Controls.Add(lblSshEngine);
            cmbSshEngine = new ComboBox { Location = new Point(120, y), Width = txtW, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbSshEngine.Items.AddRange(new object[] { "Integrated Tab (SSH.NET)", "PuTTY (Interactive GUI)", "Windows OpenSSH Console" });
            cmbSshEngine.SelectedIndex = 0;
            tabGeneral.Controls.Add(cmbSshEngine);

            cmbProtocol.SelectedIndexChanged += (s, e) => {
                bool isSsh = cmbProtocol.SelectedIndex >= 1;
                txtDomain.Enabled = !isSsh;
                chkCredGuard.Enabled = !isSsh;
                chkAdminSession.Enabled = !isSsh;

                lblSshKey.Enabled = isSsh;
                txtSshKey.Enabled = isSsh;
                btnBrowseSshKey.Enabled = isSsh;
                lblSshEngine.Enabled = isSsh;
                cmbSshEngine.Enabled = isSsh;

                if (cmbProtocol.SelectedIndex == 2)
                {
                    cmbSshEngine.SelectedIndex = 1; // PuTTY
                }
                else if (cmbProtocol.SelectedIndex == 1 && cmbSshEngine.SelectedIndex == 1)
                {
                    cmbSshEngine.SelectedIndex = 0; // Integrated Tab
                }

                if (isSsh) {
                    if (tabs.TabPages.Contains(tabDisplay)) tabs.TabPages.Remove(tabDisplay);
                    if (tabs.TabPages.Contains(tabResources)) tabs.TabPages.Remove(tabResources);
                    if (tabs.TabPages.Contains(tabAdvanced)) tabs.TabPages.Remove(tabAdvanced);
                } else {
                    if (!tabs.TabPages.Contains(tabDisplay)) tabs.TabPages.Add(tabDisplay);
                    if (!tabs.TabPages.Contains(tabResources)) tabs.TabPages.Add(tabResources);
                    if (!tabs.TabPages.Contains(tabAdvanced)) tabs.TabPages.Add(tabAdvanced);
                }
            };

            // Display Tab
            tabDisplay = new TabPage("🖥️ Display");
            y = 20;
            chkDynamicRes = new CheckBox { Text = "Dynamic Resolution (Resize window)", Location = new Point(20, y), Width = 300 };
            tabDisplay.Controls.Add(chkDynamicRes); y += 30;

            tabDisplay.Controls.Add(new Label { Text = "Resolution:", Location = new Point(20, y), Width = 80 });
            cmbResolution = new ComboBox { Location = new Point(100, y), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbResolution.Items.AddRange(new string[] { "Default", "1920x1080", "2560x1440", "Custom" });
            tabDisplay.Controls.Add(cmbResolution);
            
            txtResWidth = new TextBox { Location = new Point(260, y), Width = 40, Enabled = false };
            tabDisplay.Controls.Add(txtResWidth);
            txtResHeight = new TextBox { Location = new Point(310, y), Width = 40, Enabled = false };
            tabDisplay.Controls.Add(txtResHeight); y += 30;

            cmbResolution.SelectedIndexChanged += (s, e) => {
                bool isCustom = cmbResolution.SelectedItem?.ToString() == "Custom";
                txtResWidth.Enabled = isCustom;
                txtResHeight.Enabled = isCustom;
            };
            
            chkEnableDpiScale = new CheckBox { Text = "Enable High DPI Scaling (+dpiscale)", Location = new Point(20, y), Width = 300 };
            tabDisplay.Controls.Add(chkEnableDpiScale); y += 30;

            chkMultiMonitor = new CheckBox { Text = "Use Multiple Monitors (/multimon)\nNote: Will launch detached", Location = new Point(20, y), Width = 300, Height = 35 };
            tabDisplay.Controls.Add(chkMultiMonitor); y += 40;

            tabDisplay.Controls.Add(new Label { Text = "RDP Client:", Location = new Point(20, y + 3), Width = 80 });
            cmbRdpClient = new ComboBox { Location = new Point(100, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRdpClient.Items.AddRange(new string[] { "wfreerdp (Windows Native)", "sdl3-freerdp (SDL3 Engine)", "xfreerdp (X11 / Linux)" });
            tabDisplay.Controls.Add(cmbRdpClient); y += 35;

            tabDisplay.Controls.Add(new Label { Text = "Cert Security:", Location = new Point(20, y), Width = 80 });
            cmbCertSecurity = new ComboBox { Location = new Point(100, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCertSecurity.Items.AddRange(new string[] { "Ignore", "Trust on First Use (TOFU)", "Deny" });
            tabDisplay.Controls.Add(cmbCertSecurity); y += 30;

            chkAutoNetworkProfile = new CheckBox { Text = "Auto Network Profile (/network:auto)", Location = new Point(20, y), Width = 300 };
            tabDisplay.Controls.Add(chkAutoNetworkProfile);

            // Local Resources Tab
            tabResources = new TabPage("🖧 Local Resources");
            y = 20;
            chkClipboard = new CheckBox { Text = "Clipboard (Copy/Paste)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkClipboard); y += 30;

            chkSound = new CheckBox { Text = "Audio Playback (/sound)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkSound); y += 30;

            chkMicrophone = new CheckBox { Text = "Microphone (/microphone)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkMicrophone); y += 30;

            chkCamera = new CheckBox { Text = "Webcam (/camera)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkCamera); y += 30;

            chkDrive = new CheckBox { Text = "Map Local C:\\ Drive (/drive)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkDrive); y += 30;

            chkPrinter = new CheckBox { Text = "Map Printers (/printers)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkPrinter); y += 30;

            chkSmartcard = new CheckBox { Text = "Smartcards (/smartcard)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkSmartcard); y += 30;

            chkEnableUsbRedirection = new CheckBox { Text = "USB Device Redirection (/usb:auto)", Location = new Point(20, y), Width = 250 };
            tabResources.Controls.Add(chkEnableUsbRedirection);

            // Advanced Tab
            tabAdvanced = new TabPage("🛠️ Advanced");
            y = 20;
            
            chkGamingMode = new CheckBox { Text = "Enable Gaming Mode (Low Latency / AVC444)", Location = new Point(20, y), Width = 300, Font = new Font(this.Font, FontStyle.Bold), ForeColor = Color.DarkOrange };
            tabAdvanced.Controls.Add(chkGamingMode); y += 35;

            chkGateway = new CheckBox { Text = "Enable RD Gateway", Location = new Point(20, y), Width = 300, Font = new Font(this.Font, FontStyle.Bold) };
            tabAdvanced.Controls.Add(chkGateway); y += 35;

            tabAdvanced.Controls.Add(new Label { Text = "RD Gateway Server:", Location = new Point(20, y), Width = 120 });
            txtGwHost = new TextBox { Location = new Point(150, y), Width = 200 };
            tabAdvanced.Controls.Add(txtGwHost); y += 30;

            tabAdvanced.Controls.Add(new Label { Text = "RD Gateway Domain:", Location = new Point(20, y), Width = 120 });
            txtGwDomain = new TextBox { Location = new Point(150, y), Width = 200 };
            tabAdvanced.Controls.Add(txtGwDomain); y += 30;

            tabAdvanced.Controls.Add(new Label { Text = "RD Gateway User:", Location = new Point(20, y), Width = 120 });
            txtGwUser = new TextBox { Location = new Point(150, y), Width = 200 };
            tabAdvanced.Controls.Add(txtGwUser); y += 30;

            tabAdvanced.Controls.Add(new Label { Text = "RD Gateway Pass:", Location = new Point(20, y), Width = 120 });
            txtGwPass = new TextBox { Location = new Point(150, y), Width = 200, PasswordChar = '*' };
            tabAdvanced.Controls.Add(txtGwPass); y += 40;

            tabAdvanced.Controls.Add(new Label { Text = "RemoteApp Alias\n(e.g., ||word):", Location = new Point(20, y), Width = 120, Height = 40 });
            txtRemoteApp = new TextBox { Location = new Point(150, y+10), Width = 200 };
            tabAdvanced.Controls.Add(txtRemoteApp); y += 50;

            chkAutoReconnect = new CheckBox { Text = "Auto-Reconnect on network drop", Location = new Point(20, y), Width = 300 };
            tabAdvanced.Controls.Add(chkAutoReconnect); y += 30;

            chkDisableUdp = new CheckBox { Text = "Disable UDP Multitransport (/multitransport:tcp-only)", Location = new Point(20, y), Width = 350 };
            tabAdvanced.Controls.Add(chkDisableUdp);

            // Add Tabs
            tabs.TabPages.Add(tabGeneral);
            tabs.TabPages.Add(tabDisplay);
            tabs.TabPages.Add(tabResources);
            tabs.TabPages.Add(tabAdvanced);
            this.Controls.Add(tabs);

            // Buttons
            Button btnSave = new Button { Text = "Save", Location = new Point(250, 420), Width = 80 };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            Button btnCancel = new Button { Text = "Cancel", Location = new Point(340, 420), Width = 80 };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            this.Controls.Add(btnCancel);
        }

        private void LoadData()
        {
            cmbProtocol.SelectedIndex = Connection.Protocol switch
            {
                ConnectionProtocol.SSH_Putty => 2,
                ConnectionProtocol.SSH => 1,
                _ => 0
            };
            cmbSshEngine.SelectedIndex = Connection.SshEngine switch
            {
                SshClientEngine.PuTTY => 1,
                SshClientEngine.OpenSshConsole => 2,
                _ => 0
            };
            txtName.Text = Connection.Name;
            txtGroup.Text = Connection.Group;
            txtHost.Text = Connection.Host;
            txtUser.Text = Connection.User;
            txtPass.Text = Connection.Pass;
            txtSshKey.Text = Connection.SshKeyPath;
            txtDomain.Text = Connection.Domain;
            chkCredGuard.Checked = Connection.EnableCredGuard;
            chkAdminSession.Checked = Connection.AdminSession;

            chkDynamicRes.Checked = Connection.DynamicResolution;
            
            if (!Connection.UseCustomResolution) cmbResolution.SelectedIndex = 0;
            else if (Connection.ResolutionWidth == 1920 && Connection.ResolutionHeight == 1080) cmbResolution.SelectedIndex = 1;
            else if (Connection.ResolutionWidth == 2560 && Connection.ResolutionHeight == 1440) cmbResolution.SelectedIndex = 2;
            else cmbResolution.SelectedIndex = 3;

            txtResWidth.Text = Connection.ResolutionWidth.ToString();
            txtResHeight.Text = Connection.ResolutionHeight.ToString();

            chkEnableDpiScale.Checked = Connection.EnableDpiScale;
            chkMultiMonitor.Checked = Connection.MultiMonitor;
                        cmbRdpClient.SelectedIndex = Connection.ClientType switch
            {
                RdpClientType.Sdl3     => 1,
                RdpClientType.XFreeRdp => 2,
                _                      => 0
            };

            cmbCertSecurity.SelectedIndex = Connection.CertConfig switch
            {
                CertSecurity.Ignore => 0,
                CertSecurity.Tofu => 1,
                CertSecurity.Deny => 2,
                _ => 0
            };

            chkAutoNetworkProfile.Checked = Connection.AutoNetworkProfile;
            chkGamingMode.Checked = Connection.GamingMode;

            chkClipboard.Checked = Connection.EnableClipboard;
            chkSound.Checked = Connection.EnableSound;
            chkMicrophone.Checked = Connection.EnableMicrophone;
            chkCamera.Checked = Connection.EnableCamera;
            chkDrive.Checked = Connection.MapDrive;
            chkPrinter.Checked = Connection.MapPrinter;
            chkSmartcard.Checked = Connection.MapSmartcard;
            chkEnableUsbRedirection.Checked = Connection.EnableUsbRedirection;

            txtGwHost.Text = Connection.GatewayHost;
            txtGwDomain.Text = Connection.GatewayDomain;
            txtGwUser.Text = Connection.GatewayUser;
            txtGwPass.Text = Connection.GatewayPass;
            chkGateway.Checked = Connection.EnableGateway;
            txtRemoteApp.Text = Connection.RemoteApp;
            chkAutoReconnect.Checked = Connection.AutoReconnect;
            chkDisableUdp.Checked = Connection.DisableUdp;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtHost.Text))
            {
                MessageBox.Show("Name and Host are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Connection.Protocol = cmbProtocol.SelectedIndex switch
            {
                2 => ConnectionProtocol.SSH_Putty,
                1 => ConnectionProtocol.SSH,
                _ => ConnectionProtocol.RDP
            };
            Connection.SshEngine = cmbSshEngine.SelectedIndex switch
            {
                1 => SshClientEngine.PuTTY,
                2 => SshClientEngine.OpenSshConsole,
                _ => SshClientEngine.IntegratedTab
            };
            Connection.Name = txtName.Text;
            Connection.Group = string.IsNullOrWhiteSpace(txtGroup.Text) ? "Default" : txtGroup.Text;
            Connection.Host = txtHost.Text;
            Connection.User = txtUser.Text;
            Connection.Pass = txtPass.Text;
            Connection.SshKeyPath = txtSshKey.Text;
            Connection.Domain = txtDomain.Text;
            Connection.EnableCredGuard = chkCredGuard.Checked;
            Connection.AdminSession = chkAdminSession.Checked;

            Connection.DynamicResolution = chkDynamicRes.Checked;

            int resIndex = cmbResolution.SelectedIndex;
            if (resIndex == 0) Connection.UseCustomResolution = false;
            else
            {
                Connection.UseCustomResolution = true;
                if (resIndex == 1) { Connection.ResolutionWidth = 1920; Connection.ResolutionHeight = 1080; }
                else if (resIndex == 2) { Connection.ResolutionWidth = 2560; Connection.ResolutionHeight = 1440; }
                else
                {
                    int.TryParse(txtResWidth.Text, out int w);
                    int.TryParse(txtResHeight.Text, out int h);
                    Connection.ResolutionWidth = w > 0 ? w : 1920;
                    Connection.ResolutionHeight = h > 0 ? h : 1080;
                }
            }

            Connection.EnableDpiScale = chkEnableDpiScale.Checked;
            Connection.MultiMonitor = chkMultiMonitor.Checked;
                        Connection.ClientType = cmbRdpClient.SelectedIndex switch
            {
                1 => RdpClientType.Sdl3,
                2 => RdpClientType.XFreeRdp,
                _ => RdpClientType.WFreeRdp
            };

            Connection.CertConfig = cmbCertSecurity.SelectedIndex switch
            {
                0 => CertSecurity.Ignore,
                1 => CertSecurity.Tofu,
                2 => CertSecurity.Deny,
                _ => CertSecurity.Ignore
            };

            Connection.AutoNetworkProfile = chkAutoNetworkProfile.Checked;
            Connection.GamingMode = chkGamingMode.Checked;

            Connection.EnableClipboard = chkClipboard.Checked;
            Connection.EnableSound = chkSound.Checked;
            Connection.EnableMicrophone = chkMicrophone.Checked;
            Connection.EnableCamera = chkCamera.Checked;
            Connection.MapDrive = chkDrive.Checked;
            Connection.MapPrinter = chkPrinter.Checked;
            Connection.MapSmartcard = chkSmartcard.Checked;
            Connection.EnableUsbRedirection = chkEnableUsbRedirection.Checked;

            Connection.GatewayHost = txtGwHost.Text;
            Connection.GatewayDomain = txtGwDomain.Text;
            Connection.GatewayUser = txtGwUser.Text;
            Connection.GatewayPass = txtGwPass.Text;
            Connection.EnableGateway = chkGateway.Checked;
            Connection.RemoteApp = txtRemoteApp.Text;
            Connection.AutoReconnect = chkAutoReconnect.Checked;
            Connection.DisableUdp = chkDisableUdp.Checked;

            Connection.AdditionalFlags = "/gfx"; // Auto-negotiate best graphics

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
