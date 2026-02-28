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
        private CheckBox chkIgnoreCert = null!;
        private CheckBox chkDynamicRes = null!;
        private CheckBox chkEnableDpiScale = null!;
        private TextBox txtRemoteApp = null!;
        private CheckBox chkCredGuard = null!;
        private TextBox txtGwHost = null!;
        private TextBox txtGwUser = null!;
        private TextBox txtGwPass = null!;
        private CheckBox chkClipboard = null!;
        private CheckBox chkSound = null!;
        private CheckBox chkMicrophone = null!;
        private CheckBox chkDrive = null!;
        private CheckBox chkPrinter = null!;
        private CheckBox chkSmartcard = null!;

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

            TabControl tabs = new TabControl { Dock = DockStyle.Top, Height = 400 };

            // General Tab
            TabPage tabGeneral = new TabPage("General");
            int y = 20, lblW = 100, txtW = 250;
            
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
            tabGeneral.Controls.Add(chkCredGuard);

            // Display Tab
            TabPage tabDisplay = new TabPage("Display");
            y = 20;
            chkDynamicRes = new CheckBox { Text = "Dynamic Resolution (Resize window)", Location = new Point(20, y), Width = 300 };
            tabDisplay.Controls.Add(chkDynamicRes); y += 30;
            
            chkEnableDpiScale = new CheckBox { Text = "Enable High DPI Scaling (+dpiscale)", Location = new Point(20, y), Width = 300 };
            tabDisplay.Controls.Add(chkEnableDpiScale); y += 30;

            chkUseSdl = new CheckBox { Text = "Use SDL3 Engine (Instead of wfreerdp)", Location = new Point(20, y), Width = 300 };
            tabDisplay.Controls.Add(chkUseSdl); y += 30;

            chkIgnoreCert = new CheckBox { Text = "Ignore Certificate Warnings", Location = new Point(20, y), Width = 300 };
            tabDisplay.Controls.Add(chkIgnoreCert);

            // Local Resources Tab
            TabPage tabResources = new TabPage("Local Resources");
            y = 20;
            chkClipboard = new CheckBox { Text = "Clipboard (Copy/Paste)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkClipboard); y += 30;

            chkSound = new CheckBox { Text = "Audio Playback (/sound)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkSound); y += 30;

            chkMicrophone = new CheckBox { Text = "Microphone (/microphone)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkMicrophone); y += 30;

            chkDrive = new CheckBox { Text = "Map Local C:\\ Drive (/drive)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkDrive); y += 30;

            chkPrinter = new CheckBox { Text = "Map Printers (/printers)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkPrinter); y += 30;

            chkSmartcard = new CheckBox { Text = "Smartcards (/smartcard)", Location = new Point(20, y), Width = 200 };
            tabResources.Controls.Add(chkSmartcard);

            // Advanced Tab
            TabPage tabAdvanced = new TabPage("Advanced");
            y = 20;
            tabAdvanced.Controls.Add(new Label { Text = "RD Gateway Server:", Location = new Point(20, y), Width = 120 });
            txtGwHost = new TextBox { Location = new Point(150, y), Width = 200 };
            tabAdvanced.Controls.Add(txtGwHost); y += 30;

            tabAdvanced.Controls.Add(new Label { Text = "RD Gateway User:", Location = new Point(20, y), Width = 120 });
            txtGwUser = new TextBox { Location = new Point(150, y), Width = 200 };
            tabAdvanced.Controls.Add(txtGwUser); y += 30;

            tabAdvanced.Controls.Add(new Label { Text = "RD Gateway Pass:", Location = new Point(20, y), Width = 120 });
            txtGwPass = new TextBox { Location = new Point(150, y), Width = 200, PasswordChar = '*' };
            tabAdvanced.Controls.Add(txtGwPass); y += 40;

            tabAdvanced.Controls.Add(new Label { Text = "RemoteApp Alias\n(e.g., ||word):", Location = new Point(20, y), Width = 120, Height = 40 });
            txtRemoteApp = new TextBox { Location = new Point(150, y+10), Width = 200 };
            tabAdvanced.Controls.Add(txtRemoteApp);

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
            txtName.Text = Connection.Name;
            txtGroup.Text = Connection.Group;
            txtHost.Text = Connection.Host;
            txtUser.Text = Connection.User;
            txtPass.Text = Connection.Pass;
            txtDomain.Text = Connection.Domain;
            chkCredGuard.Checked = Connection.EnableCredGuard;

            chkDynamicRes.Checked = Connection.DynamicResolution;
            chkEnableDpiScale.Checked = Connection.EnableDpiScale;
            chkUseSdl.Checked = Connection.UseSdl;
            chkIgnoreCert.Checked = Connection.IgnoreCert;

            chkClipboard.Checked = Connection.EnableClipboard;
            chkSound.Checked = Connection.EnableSound;
            chkMicrophone.Checked = Connection.EnableMicrophone;
            chkDrive.Checked = Connection.MapDrive;
            chkPrinter.Checked = Connection.MapPrinter;
            chkSmartcard.Checked = Connection.MapSmartcard;

            txtGwHost.Text = Connection.GatewayHost;
            txtGwUser.Text = Connection.GatewayUser;
            txtGwPass.Text = Connection.GatewayPass;
            txtRemoteApp.Text = Connection.RemoteApp;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtHost.Text))
            {
                MessageBox.Show("Name and Host are required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Connection.Name = txtName.Text;
            Connection.Group = string.IsNullOrWhiteSpace(txtGroup.Text) ? "Default" : txtGroup.Text;
            Connection.Host = txtHost.Text;
            Connection.User = txtUser.Text;
            Connection.Pass = txtPass.Text;
            Connection.Domain = txtDomain.Text;
            Connection.EnableCredGuard = chkCredGuard.Checked;

            Connection.DynamicResolution = chkDynamicRes.Checked;
            Connection.EnableDpiScale = chkEnableDpiScale.Checked;
            Connection.UseSdl = chkUseSdl.Checked;
            Connection.IgnoreCert = chkIgnoreCert.Checked;

            Connection.EnableClipboard = chkClipboard.Checked;
            Connection.EnableSound = chkSound.Checked;
            Connection.EnableMicrophone = chkMicrophone.Checked;
            Connection.MapDrive = chkDrive.Checked;
            Connection.MapPrinter = chkPrinter.Checked;
            Connection.MapSmartcard = chkSmartcard.Checked;

            Connection.GatewayHost = txtGwHost.Text;
            Connection.GatewayUser = txtGwUser.Text;
            Connection.GatewayPass = txtGwPass.Text;
            Connection.RemoteApp = txtRemoteApp.Text;

            Connection.AdditionalFlags = "/gfx:avc444"; // Fixed for now

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
