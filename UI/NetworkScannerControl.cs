using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class NetworkScannerControl : UserControl
    {
        // Top row controls
        private ComboBox cmbScanFrom = null!;
        private ComboBox cmbInterface = null!;
        private TextBox txtStartIp = null!;
        private TextBox txtEndIp = null!;
        private TextBox txtNetmask = null!;
        private Button btnOptions = null!;
        private Button btnStart = null!;

        // Second row controls
        private Button btnFilters = null!;
        private ContextMenuStrip menuFilters = null!;
        private TextBox txtSearch = null!;
        private Button btnClearSearch = null!;
        private Label lblResultsCount = null!;
        private Button btnConnect = null!;
        private Button btnImport = null!;

        // Progress & Grid
        private ProgressBar progressBar = null!;
        private ListView lvResults = null!;

        // State
        private CancellationTokenSource? _cts;
        private List<NetworkAdapterInfo> _adapters = new();
        private List<DiscoveredDevice> _allDiscovered = new();
        private ScanOptions _scanOptions = new();
        private readonly List<string> _existingGroups;

        // Filter flags
        private bool _filterRdp = true;
        private bool _filterSsh = true;
        private bool _filterVnc = false;
        private bool _filterWeb = false;
        private bool _filterTelnet = false;
        private bool _filterResponsiveOnly = true;

        public event Action<List<SavedConnection>>? OnConnectionsAdded;
        public event Action<SavedConnection>? OnQuickConnectRequested;

        public NetworkScannerControl(List<string> existingGroups)
        {
            _existingGroups = existingGroups ?? new List<string>();
            InitializeUI();
            LoadAdapters();
        }

        private void InitializeUI()
        {
            this.Dock = DockStyle.Fill;

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
                RowCount = 4,
                Padding = new Padding(10, 8, 10, 8)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // Top Bar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); // Second Filter Bar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));  // Progress bar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Results ListView

            // ════════════════════════════════════════════════════════════════════
            // ROW 1: TOP BAR (Devolutions Style)
            // ════════════════════════════════════════════════════════════════════
            Panel pnlTopBar = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };

            int x = 2;
            int y = 8;

            Label lblScanFrom = new Label { Text = "Scan from", Location = new Point(x, y + 4), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = subFg };
            x += 65;

            cmbScanFrom = new ComboBox
            {
                Location = new Point(x, y),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };
            cmbScanFrom.Items.Add("Local Computer");
            cmbScanFrom.SelectedIndex = 0;
            x += 128;

            Label lblInterface = new Label { Text = "on network interface", Location = new Point(x, y + 4), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = subFg };
            x += 125;

            cmbInterface = new ComboBox
            {
                Location = new Point(x, y),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };
            cmbInterface.SelectedIndexChanged += CmbInterface_SelectedIndexChanged;
            x += 228;

            Label lblRange = new Label { Text = "IP range", Location = new Point(x, y + 4), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = subFg };
            x += 54;

            txtStartIp = new TextBox
            {
                Location = new Point(x, y),
                Width = 110,
                Font = new Font("Segoe UI", 9f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };
            x += 114;

            Label lblTo = new Label { Text = "To", Location = new Point(x, y + 4), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = subFg };
            x += 24;

            txtEndIp = new TextBox
            {
                Location = new Point(x, y),
                Width = 110,
                Font = new Font("Segoe UI", 9f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };
            x += 114;

            Label lblMask = new Label { Text = "Netmask", Location = new Point(x, y + 4), AutoSize = true, Font = new Font("Segoe UI", 8.5f), ForeColor = subFg };
            x += 56;

            txtNetmask = new TextBox
            {
                Location = new Point(x, y),
                Width = 42,
                Text = "/24",
                Font = new Font("Segoe UI", 9f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };
            x += 48;

            // Action buttons on the right
            btnStart = new Button
            {
                Text = "▶  Start",
                Dock = DockStyle.Right,
                Width = 90,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = isMatrix ? Color.FromArgb(0, 100, 35) : Color.FromArgb(0, 120, 215),
                ForeColor = Color.White
            };
            btnStart.FlatAppearance.BorderColor = accent;
            btnStart.Click += async (s, e) => await StartOrStopScanAsync();

            btnOptions = new Button
            {
                Text = "⚙️ Options",
                Dock = DockStyle.Right,
                Width = 90,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = isMatrix ? Color.FromArgb(8, 20, 8) : (isDark ? Color.FromArgb(40, 44, 52) : Color.WhiteSmoke),
                ForeColor = fg
            };
            btnOptions.FlatAppearance.BorderColor = border;
            btnOptions.Click += BtnOptions_Click;

            pnlTopBar.Controls.AddRange(new Control[] {
                lblScanFrom, cmbScanFrom, lblInterface, cmbInterface,
                lblRange, txtStartIp, lblTo, txtEndIp, lblMask, txtNetmask,
                btnOptions, btnStart
            });

            // ════════════════════════════════════════════════════════════════════
            // ROW 2: FILTERS & SEARCH BAR (Devolutions Style)
            // ════════════════════════════════════════════════════════════════════
            Panel pnlSecondBar = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };

            // 1. Filters Button with Dropdown
            btnFilters = new Button
            {
                Text = "⛚  Filters (2)  ⌄",
                Location = new Point(2, 6),
                Width = 115,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = isMatrix ? Color.FromArgb(12, 30, 14) : (isDark ? Color.FromArgb(45, 50, 60) : Color.FromArgb(235, 238, 242)),
                ForeColor = fg
            };
            btnFilters.FlatAppearance.BorderColor = border;
            BuildFilterMenu();
            btnFilters.Click += (s, e) => menuFilters.Show(btnFilters, new Point(0, btnFilters.Height));

            // 2. Search Box
            Panel pnlSearch = new Panel
            {
                Location = new Point(125, 6),
                Width = 240,
                Height = 28,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White
            };
            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                PlaceholderText = "🔍 Search...",
                Font = new Font("Segoe UI", 9f),
                BackColor = isDark ? Color.FromArgb(20, 22, 24) : Color.White,
                ForeColor = fg
            };
            txtSearch.TextChanged += (s, e) => ApplySearchFilter();

            btnClearSearch = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 22,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 7.5f)
            };
            btnClearSearch.FlatAppearance.BorderSize = 0;
            btnClearSearch.Click += (s, e) => txtSearch.Clear();

            pnlSearch.Controls.Add(txtSearch);
            pnlSearch.Controls.Add(btnClearSearch);

            // 3. Results Count
            lblResultsCount = new Label
            {
                Text = "0 Results",
                Location = new Point(375, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = subFg
            };

            // 4. Right side: Connect and Import Buttons
            btnImport = new Button
            {
                Text = "📥  Import (0)",
                Dock = DockStyle.Right,
                Width = 120,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = isMatrix ? Color.FromArgb(0, 100, 35) : Color.FromArgb(30, 140, 80),
                ForeColor = Color.White
            };
            btnImport.FlatAppearance.BorderColor = accent;
            btnImport.Click += BtnImport_Click;

            btnConnect = new Button
            {
                Text = "🚀  Connect",
                Dock = DockStyle.Right,
                Width = 95,
                Height = 30,
                Enabled = false,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = isMatrix ? Color.FromArgb(8, 20, 8) : (isDark ? Color.FromArgb(40, 44, 52) : Color.WhiteSmoke),
                ForeColor = fg
            };
            btnConnect.FlatAppearance.BorderColor = border;
            btnConnect.Click += BtnConnect_Click;

            pnlSecondBar.Controls.AddRange(new Control[] {
                btnFilters, pnlSearch, lblResultsCount, btnConnect, btnImport
            });

            // ════════════════════════════════════════════════════════════════════
            // ROW 3: PROGRESS BAR
            // ════════════════════════════════════════════════════════════════════
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Fill,
                Height = 4,
                Style = ProgressBarStyle.Continuous,
                Visible = false
            };

            // ════════════════════════════════════════════════════════════════════
            // ROW 4: RESULTS LISTVIEW (Devolutions Style)
            // ════════════════════════════════════════════════════════════════════
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
            lvResults.Columns.Add("Source / Interface / IP", 240);
            lvResults.Columns.Add("Hostname", 220);
            lvResults.Columns.Add("Service", 140);
            lvResults.Columns.Add("Port", 90);
            lvResults.Columns.Add("Response time", 110);

            lvResults.ItemChecked += (s, e) => {
                if (e.Item.Tag is DiscoveredDevice dev)
                {
                    dev.IsSelected = e.Item.Checked;
                }
                UpdateImportButtonCount();
            };
            lvResults.SelectedIndexChanged += (s, e) => {
                btnConnect.Enabled = lvResults.SelectedItems.Count > 0;
            };
            lvResults.DoubleClick += (s, e) => BtnConnect_Click(s, e);

            mainLayout.Controls.Add(pnlTopBar, 0, 0);
            mainLayout.Controls.Add(pnlSecondBar, 0, 1);
            mainLayout.Controls.Add(progressBar, 0, 2);
            mainLayout.Controls.Add(lvResults, 0, 3);

            this.Controls.Add(mainLayout);
        }

        private void BuildFilterMenu()
        {
            menuFilters = new ContextMenuStrip();
            
            var itmRdp = new ToolStripMenuItem("RDP (Port 3389)", null, (s, e) => ToggleFilter(ref _filterRdp, s)) { Checked = _filterRdp, CheckOnClick = true };
            var itmSsh = new ToolStripMenuItem("SSH (Port 22)", null, (s, e) => ToggleFilter(ref _filterSsh, s)) { Checked = _filterSsh, CheckOnClick = true };
            var itmVnc = new ToolStripMenuItem("VNC (Port 5900)", null, (s, e) => ToggleFilter(ref _filterVnc, s)) { Checked = _filterVnc, CheckOnClick = true };
            var itmWeb = new ToolStripMenuItem("WEB (Port 80/443)", null, (s, e) => ToggleFilter(ref _filterWeb, s)) { Checked = _filterWeb, CheckOnClick = true };
            var itmTelnet = new ToolStripMenuItem("TELNET (Port 23)", null, (s, e) => ToggleFilter(ref _filterTelnet, s)) { Checked = _filterTelnet, CheckOnClick = true };

            var sep = new ToolStripSeparator();
            var itmResp = new ToolStripMenuItem("Only show responsive hosts", null, (s, e) => {
                _filterResponsiveOnly = !_filterResponsiveOnly;
                ApplySearchFilter();
            }) { Checked = _filterResponsiveOnly, CheckOnClick = true };

            var itmArp = new ToolStripMenuItem("Read ARP cache (Instant discovery)", null, (s, e) => {
                _scanOptions.UseArpCache = !_scanOptions.UseArpCache;
            }) { Checked = _scanOptions.UseArpCache, CheckOnClick = true };

            var itmDns = new ToolStripMenuItem("Resolve hostnames (DNS)", null, (s, e) => {
                _scanOptions.ResolveHostnames = !_scanOptions.ResolveHostnames;
            }) { Checked = _scanOptions.ResolveHostnames, CheckOnClick = true };

            menuFilters.Items.AddRange(new ToolStripItem[] {
                itmRdp, itmSsh, itmVnc, itmWeb, itmTelnet,
                sep, itmResp, itmArp, itmDns
            });

            UpdateFilterButtonText();
        }

        private void ToggleFilter(ref bool flag, object? sender)
        {
            flag = !flag;
            UpdateFilterButtonText();
            UpdateTargetPorts();
            ApplySearchFilter();
        }

        private void UpdateFilterButtonText()
        {
            int count = 0;
            if (_filterRdp) count++;
            if (_filterSsh) count++;
            if (_filterVnc) count++;
            if (_filterWeb) count++;
            if (_filterTelnet) count++;

            btnFilters.Text = $"⛚  Filters ({count})  ⌄";
        }

        private void UpdateTargetPorts()
        {
            var ports = new List<int>();
            if (_filterRdp) ports.Add(3389);
            if (_filterSsh) ports.Add(22);
            if (_filterVnc) ports.Add(5900);
            if (_filterWeb) { ports.Add(80); ports.Add(443); }
            if (_filterTelnet) ports.Add(23);

            _scanOptions.TargetPorts = ports;
        }

        private void LoadAdapters()
        {
            _adapters = NetworkScannerService.GetActiveAdapters();
            cmbInterface.Items.Clear();

            foreach (var ad in _adapters)
            {
                cmbInterface.Items.Add(ad.DisplayText);
            }
            cmbInterface.Items.Add("All Interfaces");

            if (_adapters.Count > 0)
            {
                // Auto-select the first adapter (which is prioritized by default gateway)
                cmbInterface.SelectedIndex = 0;
            }
            else
            {
                txtStartIp.Text = "192.168.1.1";
                txtEndIp.Text = "192.168.1.254";
                txtNetmask.Text = "/24";
            }
        }

        private void CmbInterface_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbInterface.SelectedIndex >= 0 && cmbInterface.SelectedIndex < _adapters.Count)
            {
                var ad = _adapters[cmbInterface.SelectedIndex];
                txtStartIp.Text = ad.StartIp;
                txtEndIp.Text = ad.EndIp;
                txtNetmask.Text = $"/{ad.PrefixLength}";
            }
            else
            {
                // "All Interfaces" selected
                if (_adapters.Count > 0)
                {
                    txtStartIp.Text = _adapters[0].StartIp;
                    txtEndIp.Text = _adapters[0].EndIp;
                    txtNetmask.Text = $"/{_adapters[0].PrefixLength}";
                }
            }
        }

        private async Task StartOrStopScanAsync()
        {
            if (_cts != null)
            {
                // Stop running scan
                _cts.Cancel();
                btnStart.Text = "▶  Start";
                progressBar.Visible = false;
                return;
            }

            // Determine IP list to scan
            var ipList = new List<string>();
            string ifaceName = "Network";

            if (cmbInterface.SelectedIndex == _adapters.Count) // "All Interfaces"
            {
                ifaceName = "All Interfaces";
                foreach (var ad in _adapters)
                {
                    ipList.AddRange(NetworkScannerService.GenerateIpRange(ad.StartIp, ad.EndIp));
                }
            }
            else
            {
                string start = txtStartIp.Text.Trim();
                string end = txtEndIp.Text.Trim();
                ifaceName = cmbInterface.SelectedItem?.ToString() ?? "Network";
                ipList = NetworkScannerService.GenerateIpRange(start, end);
            }

            if (ipList.Count == 0)
            {
                MessageBox.Show("Please enter a valid Start and End IP address range.", "Invalid Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UpdateTargetPorts();
            _scanOptions.OnlyResponsive = _filterResponsiveOnly;

            btnStart.Text = "⏹  Stop";
            btnStart.BackColor = Color.IndianRed;
            progressBar.Value = 0;
            progressBar.Maximum = ipList.Count;
            progressBar.Visible = true;

            _allDiscovered.Clear();
            lvResults.Items.Clear();
            lblResultsCount.Text = "Scanning...";

            _cts = new CancellationTokenSource();

            var progress = new Progress<ScanProgressReport>(report => {
                progressBar.Value = Math.Min(report.ScannedCount, progressBar.Maximum);

                if (report.DiscoveredDevice != null)
                {
                    _allDiscovered.Add(report.DiscoveredDevice);
                    AddDeviceRow(report.DiscoveredDevice);
                }
                lblResultsCount.Text = $"{_allDiscovered.Count} Results ({report.ScannedCount}/{report.TotalCount})";
            });

            try
            {
                await NetworkScannerService.ScanNetworkAsync(ipList, _scanOptions, ifaceName, progress, _cts.Token);
                lblResultsCount.Text = $"{_allDiscovered.Count} Results";
            }
            catch (OperationCanceledException)
            {
                lblResultsCount.Text = $"{_allDiscovered.Count} Results (Stopped)";
            }
            catch (Exception ex)
            {
                lblResultsCount.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btnStart.Text = "▶  Start";
                bool isMatrix = ThemeSettings.CurrentTheme == AppTheme.HighContrast;
                btnStart.BackColor = isMatrix ? Color.FromArgb(0, 100, 35) : Color.FromArgb(0, 120, 215);
                progressBar.Visible = false;
                _cts?.Dispose();
                _cts = null;
                UpdateImportButtonCount();
            }
        }

        private void AddDeviceRow(DiscoveredDevice dev)
        {
            string search = txtSearch.Text.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(search))
            {
                bool matches = dev.IpAddress.ToLowerInvariant().Contains(search) ||
                               dev.HostName.ToLowerInvariant().Contains(search) ||
                               dev.ServiceSummary.ToLowerInvariant().Contains(search);
                if (!matches) return;
            }

            var item = new ListViewItem(dev.IpAddress);
            item.Checked = dev.IsSelected;
            item.SubItems.Add(dev.HostName);
            item.SubItems.Add(dev.ServiceSummary);
            item.SubItems.Add(dev.PortSummary);
            item.SubItems.Add($"{dev.LatencyMs} ms");
            item.Tag = dev;

            lvResults.Items.Add(item);
            UpdateImportButtonCount();
        }

        private void ApplySearchFilter()
        {
            lvResults.BeginUpdate();
            lvResults.Items.Clear();

            string search = txtSearch.Text.Trim().ToLowerInvariant();

            foreach (var dev in _allDiscovered)
            {
                if (_filterResponsiveOnly && dev.OpenPorts.Count == 0 && dev.LatencyMs == 0)
                    continue;

                if (!string.IsNullOrEmpty(search))
                {
                    bool matches = dev.IpAddress.ToLowerInvariant().Contains(search) ||
                                   dev.HostName.ToLowerInvariant().Contains(search) ||
                                   dev.ServiceSummary.ToLowerInvariant().Contains(search);
                    if (!matches) continue;
                }

                var item = new ListViewItem(dev.IpAddress);
                item.Checked = dev.IsSelected;
                item.SubItems.Add(dev.HostName);
                item.SubItems.Add(dev.ServiceSummary);
                item.SubItems.Add(dev.PortSummary);
                item.SubItems.Add($"{dev.LatencyMs} ms");
                item.Tag = dev;
                lvResults.Items.Add(item);
            }

            lvResults.EndUpdate();
            lblResultsCount.Text = $"{lvResults.Items.Count} Results";
            UpdateImportButtonCount();
        }

        private void UpdateImportButtonCount()
        {
            int selectedCount = lvResults.CheckedItems.Count;
            btnImport.Text = $"📥  Import ({selectedCount})";
            btnImport.Enabled = selectedCount > 0;
        }

        private void BtnOptions_Click(object? sender, EventArgs e)
        {
            using var dlg = new Form
            {
                Text = "Network Scan Options",
                Size = new Size(420, 320),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;
            dlg.BackColor = isDark ? Color.FromArgb(30, 32, 38) : Color.WhiteSmoke;
            dlg.ForeColor = isDark ? Color.FromArgb(230, 235, 240) : Color.Black;

            var chkArp = new CheckBox { Text = "Read ARP cache (instant discovery)", Checked = _scanOptions.UseArpCache, Location = new Point(20, 20), AutoSize = true };
            var chkDns = new CheckBox { Text = "Resolve hostnames via reverse DNS", Checked = _scanOptions.ResolveHostnames, Location = new Point(20, 50), AutoSize = true };

            var lblPingTimeout = new Label { Text = "Ping Timeout (ms):", Location = new Point(20, 95), AutoSize = true };
            var numPing = new NumericUpDown { Minimum = 100, Maximum = 5000, Value = _scanOptions.PingTimeoutMs, Increment = 50, Location = new Point(170, 92), Width = 80 };

            var lblServiceTimeout = new Label { Text = "Service Timeout (ms):", Location = new Point(20, 135), AutoSize = true };
            var numService = new NumericUpDown { Minimum = 100, Maximum = 5000, Value = _scanOptions.ServiceTimeoutMs, Increment = 50, Location = new Point(170, 132), Width = 80 };

            var btnOk = new Button { Text = "Apply", DialogResult = DialogResult.OK, Location = new Point(190, 220), Width = 90, Height = 28 };
            var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(290, 220), Width = 90, Height = 28 };

            dlg.Controls.AddRange(new Control[] { chkArp, chkDns, lblPingTimeout, numPing, lblServiceTimeout, numService, btnOk, btnCancel });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _scanOptions.UseArpCache = chkArp.Checked;
                _scanOptions.ResolveHostnames = chkDns.Checked;
                _scanOptions.PingTimeoutMs = (int)numPing.Value;
                _scanOptions.ServiceTimeoutMs = (int)numService.Value;
            }
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (lvResults.SelectedItems.Count > 0 && lvResults.SelectedItems[0].Tag is DiscoveredDevice dev)
            {
                int port = dev.DetectedProtocol == ConnectionProtocol.SSH ? 22 : 3389;
                string host = (port != 3389 && port != 22) ? $"{dev.IpAddress}:{port}" : dev.IpAddress;

                var conn = new SavedConnection
                {
                    Name = dev.HostName,
                    Host = host,
                    Protocol = dev.DetectedProtocol,
                    User = "Administrator"
                };

                OnQuickConnectRequested?.Invoke(conn);
            }
        }

        private void BtnImport_Click(object? sender, EventArgs e)
        {
            var selected = new List<DiscoveredDevice>();
            foreach (ListViewItem item in lvResults.CheckedItems)
            {
                if (item.Tag is DiscoveredDevice dev) selected.Add(dev);
            }

            if (selected.Count == 0) return;

            // Import configuration dialog
            using var dlg = new Form
            {
                Text = "Import Discovered Hosts",
                Size = new Size(380, 240),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            bool isDark = ThemeSettings.CurrentTheme != AppTheme.Light;
            dlg.BackColor = isDark ? Color.FromArgb(30, 32, 38) : Color.WhiteSmoke;
            dlg.ForeColor = isDark ? Color.FromArgb(230, 235, 240) : Color.Black;

            var lblGrp = new Label { Text = "Destination Group:", Location = new Point(20, 20), AutoSize = true };
            var cmbGrp = new ComboBox { Location = new Point(140, 16), Width = 190 };
            foreach (var g in _existingGroups.Distinct()) if (!string.IsNullOrEmpty(g)) cmbGrp.Items.Add(g);
            if (!cmbGrp.Items.Contains("Scanned Network")) cmbGrp.Items.Insert(0, "Scanned Network");
            cmbGrp.SelectedIndex = 0;

            var lblUser = new Label { Text = "Default User:", Location = new Point(20, 60), AutoSize = true };
            var txtUser = new TextBox { Text = "Administrator", Location = new Point(140, 56), Width = 190 };

            var lblProto = new Label { Text = "Protocol Mode:", Location = new Point(20, 100), AutoSize = true };
            var cmbProto = new ComboBox { Location = new Point(140, 96), Width = 190, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProto.Items.AddRange(new object[] { "Auto (From Service)", "Force RDP (3389)", "Force SSH (22)" });
            cmbProto.SelectedIndex = 0;

            var btnAdd = new Button { Text = $"Import {selected.Count} Entries", DialogResult = DialogResult.OK, Location = new Point(140, 150), Width = 120, Height = 30 };
            var btnCan = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(270, 150), Width = 70, Height = 30 };

            dlg.Controls.AddRange(new Control[] { lblGrp, cmbGrp, lblUser, txtUser, lblProto, cmbProto, btnAdd, btnCan });
            dlg.AcceptButton = btnAdd;
            dlg.CancelButton = btnCan;

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string group = cmbGrp.Text.Trim();
                if (string.IsNullOrEmpty(group)) group = "Scanned Network";
                string user = txtUser.Text.Trim();

                var newConns = new List<SavedConnection>();
                foreach (var dev in selected)
                {
                    ConnectionProtocol proto = dev.DetectedProtocol;
                    if (cmbProto.SelectedIndex == 1) proto = ConnectionProtocol.RDP;
                    else if (cmbProto.SelectedIndex == 2) proto = ConnectionProtocol.SSH;

                    string name = (dev.HostName != dev.IpAddress && !string.IsNullOrWhiteSpace(dev.HostName))
                        ? dev.HostName
                        : $"Host-{dev.IpAddress}";

                    int port = proto == ConnectionProtocol.SSH ? 22 : 3389;
                    string host = (port != 3389 && port != 22) ? $"{dev.IpAddress}:{port}" : dev.IpAddress;

                    newConns.Add(new SavedConnection
                    {
                        Name = name,
                        Host = host,
                        Group = group,
                        User = user,
                        Protocol = proto,
                        DynamicResolution = true,
                        GamingMode = true
                    });
                }

                OnConnectionsAdded?.Invoke(newConns);
                MessageBox.Show($"Successfully imported {newConns.Count} connection(s) to group '{group}'!", "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}

