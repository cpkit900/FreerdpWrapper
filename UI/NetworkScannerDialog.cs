using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class NetworkScannerDialog : Form
    {
        private TextBox txtSubnet = null!;
        private CheckBox chkRdp = null!;
        private CheckBox chkSsh = null!;
        private CheckBox chkVnc = null!;
        private CheckBox chkWeb = null!;
        private CheckBox chkOpenPortsOnly = null!;
        private NumericUpDown numTimeout = null!;

        private Button btnStart = null!;
        private Button btnStop = null!;
        private ProgressBar progressBar = null!;
        private Label lblStatus = null!;
        private ListView lvResults = null!;

        private ComboBox cmbGroup = null!;
        private TextBox txtUsername = null!;
        private ComboBox cmbProtocol = null!;
        private Button btnAddSelected = null!;

        private CancellationTokenSource? _cts;
        private readonly List<DiscoveredDevice> _discoveredDevices = new();
        private readonly List<string> _existingGroups;

        public event Action<List<SavedConnection>>? OnConnectionsAdded;
        public event Action<SavedConnection>? OnQuickConnectRequested;

        public NetworkScannerDialog(List<string> existingGroups)
        {
            _existingGroups = existingGroups ?? new List<string>();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "Network Scanner — Discover Remote Hosts & Services";
            this.Size = new Size(860, 680);
            this.MinimumSize = new Size(740, 560);
            this.StartPosition = FormStartPosition.CenterParent;

            bool isMatrix = ThemeSettings.CurrentTheme == AppTheme.HighContrast;
            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;

            Color bg = isMatrix ? Color.FromArgb(2, 6, 2) : (isDark ? Color.FromArgb(28, 30, 34) : Color.FromArgb(248, 249, 250));
            Color cardBg = isMatrix ? Color.FromArgb(6, 14, 6) : (isDark ? Color.FromArgb(36, 38, 44) : Color.White);
            Color fg = isMatrix ? Color.FromArgb(0, 255, 102) : (isDark ? Color.FromArgb(230, 235, 242) : Color.FromArgb(30, 30, 30));
            Color subFg = isMatrix ? Color.FromArgb(0, 190, 75) : (isDark ? Color.FromArgb(160, 165, 175) : Color.FromArgb(90, 95, 105));
            Color border = isMatrix ? Color.FromArgb(0, 160, 50) : (isDark ? Color.FromArgb(55, 60, 70) : Color.FromArgb(215, 220, 225));
            Color accent = isMatrix ? Color.FromArgb(0, 255, 102) : Color.FromArgb(0, 120, 215);

            this.BackColor = bg;
            this.ForeColor = fg;

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));  // Header
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));  // Scan Options
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));  // Progress Row
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Results Grid
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));  // Add / Import Bar

            // 1. Header
            Panel pnlHeader = new Panel { Dock = DockStyle.Fill };
            Label lblTitle = new Label
            {
                Text = "🌐 Network Device & Protocol Scanner",
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = fg,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            Label lblSub = new Label
            {
                Text = "Scan your local subnet or CIDR range for active machines, discover open RDP and SSH ports, and add them directly to your connections.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = subFg,
                AutoSize = true,
                Location = new Point(2, 28)
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);

            // 2. Scan Options Panel
            Panel pnlOptions = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardBg,
                Padding = new Padding(10, 8, 10, 8)
            };
            pnlOptions.Paint += (s, e) => {
                using var p = new Pen(border, 1f);
                e.Graphics.DrawRectangle(p, 0, 0, pnlOptions.Width - 1, pnlOptions.Height - 1);
            };

            Label lblSubnet = new Label { Text = "Subnet / IP Range:", Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            txtSubnet = new TextBox
            {
                Text = NetworkScannerService.DetectLocalSubnet(),
                Location = new Point(140, 7),
                Width = 170,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };

            Button btnDetect = new Button
            {
                Text = "📍 Auto-Detect",
                Location = new Point(318, 6),
                Width = 100,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = isMatrix ? Color.FromArgb(10, 30, 12) : (isDark ? Color.FromArgb(45, 48, 55) : Color.WhiteSmoke),
                ForeColor = fg
            };
            btnDetect.FlatAppearance.BorderColor = border;
            btnDetect.Click += (s, e) => txtSubnet.Text = NetworkScannerService.DetectLocalSubnet();

            Label lblTimeout = new Label { Text = "Timeout (ms):", Location = new Point(430, 10), AutoSize = true };
            numTimeout = new NumericUpDown
            {
                Location = new Point(520, 8),
                Width = 65,
                Minimum = 150,
                Maximum = 5000,
                Value = 500,
                Increment = 50,
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };

            btnStart = new Button
            {
                Text = "▶  Start Scan",
                Location = new Point(605, 5),
                Width = 110,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = isMatrix ? Color.FromArgb(0, 100, 35) : Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            btnStart.FlatAppearance.BorderColor = accent;
            btnStart.Click += async (s, e) => await StartScanAsync();

            btnStop = new Button
            {
                Text = "⏹  Stop",
                Location = new Point(722, 5),
                Width = 85,
                Height = 30,
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                BackColor = isMatrix ? Color.FromArgb(40, 10, 10) : Color.IndianRed,
                ForeColor = Color.White
            };
            btnStop.FlatAppearance.BorderColor = Color.DarkRed;
            btnStop.Click += (s, e) => StopScan();

            // Row 2 of options: Port checkboxes
            Label lblPorts = new Label { Text = "Target Ports:", Location = new Point(10, 48), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            chkRdp = new CheckBox { Text = "RDP (3389)", Checked = true, Location = new Point(140, 46), AutoSize = true };
            chkSsh = new CheckBox { Text = "SSH (22)", Checked = true, Location = new Point(255, 46), AutoSize = true };
            chkVnc = new CheckBox { Text = "VNC (5900)", Checked = false, Location = new Point(345, 46), AutoSize = true };
            chkWeb = new CheckBox { Text = "Web (80/443)", Checked = false, Location = new Point(450, 46), AutoSize = true };
            chkOpenPortsOnly = new CheckBox { Text = "Only show responsive hosts", Checked = true, Location = new Point(570, 46), AutoSize = true, ForeColor = subFg };

            pnlOptions.Controls.AddRange(new Control[] {
                lblSubnet, txtSubnet, btnDetect, lblTimeout, numTimeout, btnStart, btnStop,
                lblPorts, chkRdp, chkSsh, chkVnc, chkWeb, chkOpenPortsOnly
            });

            // 3. Progress Row
            Panel pnlProgress = new Panel { Dock = DockStyle.Fill };
            progressBar = new ProgressBar
            {
                Location = new Point(0, 4),
                Width = 460,
                Height = 16,
                Style = ProgressBarStyle.Continuous
            };
            lblStatus = new Label
            {
                Text = "Ready to scan. Click 'Start Scan' to begin discovery.",
                Location = new Point(475, 4),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = subFg
            };
            pnlProgress.Controls.Add(progressBar);
            pnlProgress.Controls.Add(lblStatus);

            // 4. Results Grid & Selection Controls
            Panel pnlResultsContainer = new Panel { Dock = DockStyle.Fill };
            
            Panel pnlSelectBar = new Panel { Dock = DockStyle.Top, Height = 28 };
            Button btnSelectAll = new Button { Text = "☑ Select All", Width = 85, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat };
            btnSelectAll.FlatAppearance.BorderSize = 0;
            btnSelectAll.Click += (s, e) => SetAllChecked(true);

            Button btnDeselectAll = new Button { Text = "☐ Deselect All", Width = 95, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat };
            btnDeselectAll.FlatAppearance.BorderSize = 0;
            btnDeselectAll.Click += (s, e) => SetAllChecked(false);

            Button btnSelectRdp = new Button { Text = "Select RDP Only", Width = 110, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat };
            btnSelectRdp.FlatAppearance.BorderSize = 0;
            btnSelectRdp.Click += (s, e) => SelectByProtocol(ConnectionProtocol.RDP);

            Button btnSelectSsh = new Button { Text = "Select SSH Only", Width = 110, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat };
            btnSelectSsh.FlatAppearance.BorderSize = 0;
            btnSelectSsh.Click += (s, e) => SelectByProtocol(ConnectionProtocol.SSH);

            pnlSelectBar.Controls.AddRange(new Control[] { btnSelectSsh, btnSelectRdp, btnDeselectAll, btnSelectAll });

            lvResults = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = isMatrix ? Color.FromArgb(2, 6, 2) : (isDark ? Color.FromArgb(20, 22, 26) : Color.White),
                ForeColor = fg
            };
            lvResults.Columns.Add("Select", 55);
            lvResults.Columns.Add("IP Address", 130);
            lvResults.Columns.Add("Hostname / Name", 200);
            lvResults.Columns.Add("Primary Protocol", 115);
            lvResults.Columns.Add("Discovered Services", 160);
            lvResults.Columns.Add("Ping (ms)", 85);
            lvResults.Columns.Add("Status", 80);

            lvResults.DoubleClick += LvResults_DoubleClick;

            pnlResultsContainer.Controls.Add(lvResults);
            pnlResultsContainer.Controls.Add(pnlSelectBar);

            // 5. Add / Import Bar
            Panel pnlImport = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardBg,
                Padding = new Padding(8)
            };
            pnlImport.Paint += (s, e) => {
                using var p = new Pen(border, 1f);
                e.Graphics.DrawRectangle(p, 0, 0, pnlImport.Width - 1, pnlImport.Height - 1);
            };

            Label lblGroup = new Label { Text = "Group:", Location = new Point(10, 16), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            cmbGroup = new ComboBox
            {
                Location = new Point(60, 12),
                Width = 140,
                Font = new Font("Segoe UI", 9f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };
            foreach (var g in _existingGroups.Distinct())
            {
                if (!string.IsNullOrEmpty(g)) cmbGroup.Items.Add(g);
            }
            if (!cmbGroup.Items.Contains("Scanned Network"))
                cmbGroup.Items.Insert(0, "Scanned Network");
            cmbGroup.SelectedIndex = 0;

            Label lblUser = new Label { Text = "Username:", Location = new Point(210, 16), AutoSize = true };
            txtUsername = new TextBox
            {
                Text = "Administrator",
                Location = new Point(280, 12),
                Width = 110,
                Font = new Font("Segoe UI", 9f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };

            Label lblProto = new Label { Text = "Protocol:", Location = new Point(400, 16), AutoSize = true };
            cmbProtocol = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(460, 12),
                Width = 140,
                Font = new Font("Segoe UI", 9f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };
            cmbProtocol.Items.AddRange(new object[] { "Auto (From Port)", "Force RDP (3389)", "Force SSH (22)" });
            cmbProtocol.SelectedIndex = 0;

            btnAddSelected = new Button
            {
                Text = "➕  Add Selected to Workspace",
                Location = new Point(610, 10),
                Width = 210,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = isMatrix ? Color.FromArgb(0, 100, 35) : Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            btnAddSelected.FlatAppearance.BorderColor = accent;
            btnAddSelected.Click += BtnAddSelected_Click;

            pnlImport.Controls.AddRange(new Control[] {
                lblGroup, cmbGroup, lblUser, txtUsername, lblProto, cmbProtocol, btnAddSelected
            });

            mainLayout.Controls.Add(pnlHeader, 0, 0);
            mainLayout.Controls.Add(pnlOptions, 0, 1);
            mainLayout.Controls.Add(pnlProgress, 0, 2);
            mainLayout.Controls.Add(pnlResultsContainer, 0, 3);
            mainLayout.Controls.Add(pnlImport, 0, 4);

            this.Controls.Add(mainLayout);
        }

        private async Task StartScanAsync()
        {
            string subnetInput = txtSubnet.Text.Trim();
            var ips = NetworkScannerService.ParseIpRange(subnetInput);
            if (ips.Count == 0)
            {
                MessageBox.Show("Please enter a valid CIDR subnet (e.g. 192.168.1.0/24) or IP range (e.g. 192.168.1.1-254).", "Invalid Subnet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ports = new List<int>();
            if (chkRdp.Checked) ports.Add(3389);
            if (chkSsh.Checked) ports.Add(22);
            if (chkVnc.Checked) ports.Add(5900);
            if (chkWeb.Checked) { ports.Add(80); ports.Add(443); }

            if (ports.Count == 0)
            {
                MessageBox.Show("Please select at least one port/protocol to scan.", "No Ports Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            _discoveredDevices.Clear();
            lvResults.Items.Clear();

            progressBar.Value = 0;
            progressBar.Maximum = ips.Count;
            lblStatus.Text = $"Starting scan across {ips.Count} hosts...";

            _cts = new CancellationTokenSource();
            int timeout = (int)numTimeout.Value;
            bool onlyOpen = chkOpenPortsOnly.Checked;

            var progress = new Progress<ScanProgressReport>(report => {
                progressBar.Value = Math.Min(report.ScannedCount, progressBar.Maximum);
                lblStatus.Text = $"Scanned {report.ScannedCount}/{report.TotalCount} hosts • Found {_discoveredDevices.Count} responsive devices";

                if (report.DiscoveredDevice != null)
                {
                    if (onlyOpen && report.DiscoveredDevice.OpenPorts.Count == 0)
                        return;

                    _discoveredDevices.Add(report.DiscoveredDevice);
                    AddDeviceToListView(report.DiscoveredDevice);
                }
            });

            try
            {
                await NetworkScannerService.ScanNetworkAsync(ips, ports, timeout, progress, _cts.Token);
                lblStatus.Text = $"Scan complete! Discovered {_discoveredDevices.Count} responsive devices out of {ips.Count} hosts.";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = $"Scan stopped by user. Found {_discoveredDevices.Count} devices.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Scan encountered an error: {ex.Message}";
            }
            finally
            {
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void StopScan()
        {
            _cts?.Cancel();
            btnStop.Enabled = false;
        }

        private void AddDeviceToListView(DiscoveredDevice dev)
        {
            var item = new ListViewItem("");
            item.Checked = dev.IsSelected;
            item.SubItems.Add(dev.IpAddress);
            item.SubItems.Add(dev.HostName);
            item.SubItems.Add(dev.DetectedProtocol == ConnectionProtocol.SSH ? "SSH" : "RDP");
            item.SubItems.Add(dev.ProtocolSummary);
            item.SubItems.Add($"{dev.LatencyMs} ms");
            item.SubItems.Add(dev.OpenPorts.Count > 0 ? "Online" : "Ping Only");
            item.Tag = dev;

            lvResults.Items.Add(item);
        }

        private void SetAllChecked(bool check)
        {
            foreach (ListViewItem item in lvResults.Items)
            {
                item.Checked = check;
            }
        }

        private void SelectByProtocol(ConnectionProtocol proto)
        {
            foreach (ListViewItem item in lvResults.Items)
            {
                if (item.Tag is DiscoveredDevice dev)
                {
                    item.Checked = (dev.DetectedProtocol == proto);
                }
            }
        }

        private void BtnAddSelected_Click(object? sender, EventArgs e)
        {
            var selectedDevices = new List<DiscoveredDevice>();
            foreach (ListViewItem item in lvResults.Items)
            {
                if (item.Checked && item.Tag is DiscoveredDevice dev)
                {
                    selectedDevices.Add(dev);
                }
            }

            if (selectedDevices.Count == 0)
            {
                MessageBox.Show("Please select at least one discovered host from the list above.", "No Hosts Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string group = cmbGroup.Text.Trim();
            if (string.IsNullOrEmpty(group)) group = "Scanned Network";
            string user = txtUsername.Text.Trim();

            var newConnections = new List<SavedConnection>();
            foreach (var dev in selectedDevices)
            {
                ConnectionProtocol proto = dev.DetectedProtocol;
                if (cmbProtocol.SelectedIndex == 1) proto = ConnectionProtocol.RDP;
                else if (cmbProtocol.SelectedIndex == 2) proto = ConnectionProtocol.SSH;

                string name = (dev.HostName != dev.IpAddress && !string.IsNullOrWhiteSpace(dev.HostName))
                    ? dev.HostName
                    : $"Host-{dev.IpAddress}";

                int targetPort = proto == ConnectionProtocol.SSH ? 22 : 3389;
                string hostStr = (targetPort != 3389 && targetPort != 22 && targetPort > 0)
                    ? $"{dev.IpAddress}:{targetPort}"
                    : dev.IpAddress;

                var conn = new SavedConnection
                {
                    Name = name,
                    Host = hostStr,
                    Group = group,
                    User = user,
                    Protocol = proto,
                    DynamicResolution = true,
                    GamingMode = true
                };

                newConnections.Add(conn);
            }

            OnConnectionsAdded?.Invoke(newConnections);

            MessageBox.Show($"Successfully added {newConnections.Count} connection(s) to group '{group}'!", "Connections Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void LvResults_DoubleClick(object? sender, EventArgs e)
        {
            if (lvResults.SelectedItems.Count > 0 && lvResults.SelectedItems[0].Tag is DiscoveredDevice dev)
            {
                int targetPort = dev.DetectedProtocol == ConnectionProtocol.SSH ? 22 : 3389;
                string hostStr = (targetPort != 3389 && targetPort != 22 && targetPort > 0)
                    ? $"{dev.IpAddress}:{targetPort}"
                    : dev.IpAddress;

                var conn = new SavedConnection
                {
                    Name = dev.HostName,
                    Host = hostStr,
                    Protocol = dev.DetectedProtocol,
                    User = txtUsername.Text.Trim()
                };

                OnQuickConnectRequested?.Invoke(conn);
                this.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopScan();
            base.OnFormClosing(e);
        }
    }
}
