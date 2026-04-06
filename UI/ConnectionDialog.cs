using System;
using System.Drawing;
using System.Windows.Forms;
using FreeRdpWrapper.Models;

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
        private CheckBox chkUseSdl = null!;
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

        private ComboBox cmbProtocol = null!;
        private TextBox txtSshKey = null!;
        private Button btnBrowseSshKey = null!;
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
            cmbProtocol.Items.AddRange(new string[] { "RDP", "SSH" });
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
            txtUser = new TextBox { Location = new Point(120, y), Width = txtW };
            tabGeneral.Controls.Add(txtUser); y += 30;

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

            tabGeneral.Controls.Add(new Label { Text = "SSH Key:", Location = new Point(20, y), Width = lblW });
            txtSshKey = new TextBox { Location = new Point(120, y), Width = 170 };
            tabGeneral.Controls.Add(txtSshKey);
            btnBrowseSshKey = new Button { Text = "Browse", Location = new Point(295, y - 1), Width = 75 };
            btnBrowseSshKey.Click += (s, e) => {
                using var ofd = new OpenFileDialog { Filter = "Key Files (*.pem;*.ppk)|*.pem;*.ppk|All Files (*.*)|*.*" };
                if (ofd.ShowDialog() == DialogResult.OK) txtSshKey.Text = ofd.FileName;
            };
            tabGeneral.Controls.Add(btnBrowseSshKey);

            cmbProtocol.SelectedIndexChanged += (s, e) => {
                bool isSsh = cmbProtocol.SelectedIndex == 1;
                txtDomain.Enabled = !isSsh;
                chkCredGuard.Enabled = !isSsh;
                chkAdminSession.Enabled = !isSsh;
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

            chkUseSdl = new CheckBox { Text = "Use SDL3 Engine (Instead of wfreerdp)", Location = new Point(20, y), Width = 300 };
            tabDisplay.Controls.Add(chkUseSdl); y += 30;

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
            tabAdvanced.Controls.Add(chkAutoReconnect);

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
            cmbProtocol.SelectedIndex = Connection.Protocol == ConnectionProtocol.SSH ? 1 : 0;
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
            chkUseSdl.Checked = Connection.UseSdl;

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
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtHost.Text))
            {
                MessageBox.Show("Name and Host are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Connection.Protocol = cmbProtocol.SelectedIndex == 1 ? ConnectionProtocol.SSH : ConnectionProtocol.RDP;
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
            Connection.UseSdl = chkUseSdl.Checked;

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

            Connection.AdditionalFlags = "/gfx:avc444"; // Fixed for now

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
