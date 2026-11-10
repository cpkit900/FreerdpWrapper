using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.UI;
using FreeRdpWrapper.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeRdpWrapper
{
    public partial class Form1 : Form
    {
        private ConnectionStore _store = default!;
        private List<SavedConnection> _connections = new();
        private ScriptStore _scriptStore = default!;
        private List<SavedScript> _scripts = new();
        private AppSettings _settings = default!;

        // UI Zones (Devolutions RDM + Rocket Remote Desktop Inspired)
        private Panel topNavPanel = default!;       // Top Zone: Modern Command Bar & Search
        private Panel quickConnectRow = default!;   // Quick Connect Bar
        private SplitContainer mainSplit = default!; // Left: Nav, Right: Workspace & Logs
        private SplitContainer leftSplit = default!; // Left Top: Connections, Left Bottom: Scripts
        private SplitContainer rightSplit = default!; // Center: TabControl, Bottom: Hidden Logs Drawer

        // Navigation Zone
        private Panel leftNavHeader = default!;
        private TextBox txtSearch = default!;
        private Button btnClearSearch = default!;
        private Label lblSearchStats = default!;
        private TreeView treeConnections = default!;
        private TreeView treeScripts = default!;
        private ContextMenuStrip treeContextMenu = default!;
        private ContextMenuStrip scriptContextMenu = default!;

        // Pinning & Retracting states
        private bool _isLeftPinned = true;
        private bool _isLeftCollapsed = false;
        private int _leftSavedWidth = 250;
        private Button btnPinLeft = default!;
        private Button btnCollapseLeft = default!;

        // Logs Drawer (Hidden by default, Devolutions style)
        private bool _isLogsVisible = false;
        private Panel logsDrawer = default!;
        private TextBox txtLogs = default!;
        private Button btnLogsToggle = default!;
        private Label lblStatusText = default!;

        // Workspace Zone
        private TabControl tabControl = default!;
        private TabPage? _quickStartTab = null;
        private TabPage? _aiTabPage = null;

        // Clipboard timer
        private System.Windows.Forms.Timer? _clipboardTimer;

        public Form1()
        {
            InitializeComponent();
            _settings = SettingsStore.LoadSettings();
            _store = new ConnectionStore();
            _connections = _store.LoadConnections();
            _scriptStore = new ScriptStore();
            _scripts = _scriptStore.LoadScripts();

            // Set application icon
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }

            SetupUI();
            RefreshTree();
            RefreshScriptsTree();
        }

        private void SetupUI()
        {
            this.Text = "AeroRemote Desktop - FreeRDP 3 Workspace (.NET 10)";
            this.Width = 1300;
            this.Height = 860;
            this.StartPosition = FormStartPosition.CenterScreen;

            // ════════════════════════════════════════════════════════════════════
            // TOP COMMAND BAR (DEVOLUTIONS RDM STYLE)
            // ════════════════════════════════════════════════════════════════════
            topNavPanel = new Panel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6, 4, 6, 4) };

            Button btnAdd = CreateNavButton("➕ Add Entry", (s, e) => AddConnection(), isPrimary: true);
            Button btnEdit = CreateNavButton("✏️ Edit", (s, e) => EditConnection());
            Button btnDelete = CreateNavButton("🗑️ Delete", (s, e) => DeleteConnection());
            Button btnImport = CreateNavButton("📥 Import .rdp", (s, e) => ImportRdpFile());
            Button btnVault = CreateNavButton("🔒 Vault Backup", (s, e) => ExportVaultArchive());

            Button btnReconnect = CreateNavButton("🔁 Reconnect", (s, e) => ReconnectActiveSession());
            Button btnScreenshot = CreateNavButton("📸 Screenshot", (s, e) => CaptureActiveScreenshot());
            Button btnCtrlAltDel = CreateNavButton("⌨️ Ctrl+Alt+Del", (s, e) => SendActiveCtrlAltDel());
            Button btnFullscreen = CreateNavButton("⛶ Fullscreen (F11)", (s, e) => ToggleFullscreen());

            Button btnAiCopilot = CreateNavButton("🤖 AI Copilot", (s, e) => ToggleAiCopilotTab());
            Button btnSettings = CreateNavButton("⚙️ Settings", (s, e) => OpenSettings());
            Button btnTheme = CreateNavButton("🌗", (s, e) => CycleTheme());

            // Global Search Bar in Top Bar
            TextBox txtGlobalSearch = new TextBox
            {
                Width = 220,
                Height = 26,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "🔍 Search everywhere..."
            };
            txtGlobalSearch.TextChanged += (s, e) => {
                txtSearch.Text = txtGlobalSearch.Text;
            };

            FlowLayoutPanel leftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            leftFlow.Controls.AddRange(new Control[] {
                btnAdd, btnEdit, btnDelete,
                CreateSeparator(),
                btnImport, btnVault,
                CreateSeparator(),
                btnReconnect, btnScreenshot, btnCtrlAltDel, btnFullscreen
            });

            FlowLayoutPanel rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            rightFlow.Controls.AddRange(new Control[] {
                txtGlobalSearch,
                btnAiCopilot, btnSettings, btnTheme
            });

            topNavPanel.Controls.Add(leftFlow);
            topNavPanel.Controls.Add(rightFlow);

            // ════════════════════════════════════════════════════════════════════
            // QUICK CONNECT ADDRESS BAR (ROCKET REMOTE DESKTOP STYLE)
            // ════════════════════════════════════════════════════════════════════
            quickConnectRow = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(8, 6, 8, 4) };

            Label lblQc = new Label { Text = "⚡ Quick Connect:", Location = new Point(8, 10), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
            TextBox txtQHost = new TextBox { Location = new Point(135, 7), Width = 190, PlaceholderText = "Host or IP[:Port]", Font = new Font("Segoe UI", 9.5f) };
            TextBox txtQUser = new TextBox { Location = new Point(335, 7), Width = 140, PlaceholderText = "Username", Font = new Font("Segoe UI", 9.5f) };
            TextBox txtQPass = new TextBox { Location = new Point(485, 7), Width = 140, PlaceholderText = "Password", PasswordChar = '*', Font = new Font("Segoe UI", 9.5f) };

            ComboBox cmbQClient = new ComboBox { Location = new Point(635, 6), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cmbQClient.Items.AddRange(new object[] { "wfreerdp (Windows)", "sdl3 (Modern D3D11)", "xfreerdp (Linux/WSL)" });
            cmbQClient.SelectedIndex = 0;

            Button btnQConnect = new Button
            {
                Text = "🚀 Connect",
                Location = new Point(805, 5),
                Width = 100,
                Height = 29,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnQConnect.FlatAppearance.BorderSize = 0;
            btnQConnect.Click += (s, e) => {
                if (!string.IsNullOrWhiteSpace(txtQHost.Text))
                {
                    var clientType = cmbQClient.SelectedIndex switch
                    {
                        1 => RdpClientType.Sdl3,
                        2 => RdpClientType.XFreeRdp,
                        _ => RdpClientType.WFreeRdp
                    };

                    LaunchSession(new SavedConnection
                    {
                        Name = "Quick: " + txtQHost.Text.Trim(),
                        Host = txtQHost.Text.Trim(),
                        User = txtQUser.Text.Trim(),
                        Pass = txtQPass.Text,
                        ClientType = clientType
                    });
                }
                else
                {
                    MessageBox.Show("Please enter a destination Host or IP address.", "Quick Connect", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            quickConnectRow.Controls.AddRange(new Control[] { lblQc, txtQHost, txtQUser, txtQPass, cmbQClient, btnQConnect });

            // ════════════════════════════════════════════════════════════════════
            // LEFT NAVIGATION SIDEBAR (RETRACTABLE & PINNABLE)
            // ════════════════════════════════════════════════════════════════════
            mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                SplitterDistance = _leftSavedWidth,
                SplitterWidth = 5
            };

            leftNavHeader = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(6, 4, 6, 2) };

            Panel navTopRow = new Panel { Dock = DockStyle.Top, Height = 26 };
            Label lblNavTitle = new Label { Text = "🌍 NAVIGATION", Dock = DockStyle.Left, Width = 140, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            btnPinLeft = new Button { Text = "📌", Dock = DockStyle.Right, Width = 28, FlatStyle = FlatStyle.Flat };
            btnPinLeft.FlatAppearance.BorderSize = 0;
            btnPinLeft.Click += (s, e) => TogglePinLeft();

            btnCollapseLeft = new Button { Text = "◀", Dock = DockStyle.Right, Width = 28, FlatStyle = FlatStyle.Flat };
            btnCollapseLeft.FlatAppearance.BorderSize = 0;
            btnCollapseLeft.Click += (s, e) => ToggleCollapseLeft();

            navTopRow.Controls.Add(lblNavTitle);
            navTopRow.Controls.Add(btnPinLeft);
            navTopRow.Controls.Add(btnCollapseLeft);

            Panel searchPanel = new Panel { Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(1) };
            txtSearch = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "🔍 Filter entries...", Font = new Font("Segoe UI", 9f) };
            txtSearch.TextChanged += (s, e) => FilterTree(txtSearch.Text);

            btnClearSearch = new Button { Text = "✖", Dock = DockStyle.Right, Width = 24, FlatStyle = FlatStyle.Flat };
            btnClearSearch.FlatAppearance.BorderSize = 0;
            btnClearSearch.Click += (s, e) => txtSearch.Clear();

            searchPanel.Controls.Add(txtSearch);
            searchPanel.Controls.Add(btnClearSearch);

            lblSearchStats = new Label { Dock = DockStyle.Bottom, Height = 13, Font = new Font("Segoe UI", 7.5f), ForeColor = Color.Gray, Text = "" };

            leftNavHeader.Controls.Add(navTopRow);
            leftNavHeader.Controls.Add(searchPanel);
            leftNavHeader.Controls.Add(lblSearchStats);

            treeConnections = new TreeView
            {
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                HideSelection = false,
                ShowLines = false,
                ItemHeight = 24,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None,
                CheckBoxes = true
            };
            treeConnections.AfterCheck += (s, e) => {
                if (e.Action != TreeViewAction.Unknown && e.Node != null)
                {
                    foreach (TreeNode child in e.Node.Nodes)
                        child.Checked = e.Node.Checked;
                }
            };
            treeConnections.NodeMouseDoubleClick += TreeConnections_NodeMouseDoubleClick;
            treeConnections.MouseUp += TreeConnections_MouseUp;

            treeContextMenu = new ContextMenuStrip();
            treeContextMenu.Items.Add("🚀 Connect", null, (s, e) => {
                if (treeConnections.SelectedNode?.Tag is SavedConnection c) LaunchSession(c);
            });
            treeContextMenu.Items.Add("⚡ Ping & Probe Port 3389", null, async (s, e) => await ProbeSelectedServer());
            treeContextMenu.Items.Add("📋 Copy Decrypted Password", null, (s, e) => CopySelectedPassword());
            treeContextMenu.Items.Add("📑 Duplicate Connection", null, (s, e) => DuplicateConnection());
            treeContextMenu.Items.Add("-");
            treeContextMenu.Items.Add("✏️ Edit Connection", null, (s, e) => EditConnection());
            treeContextMenu.Items.Add("🗑️ Delete Connection", null, (s, e) => DeleteConnection());
            treeContextMenu.Items.Add("📤 Export to .rdp...", null, (s, e) => ExportSelectedToRdp());
            treeContextMenu.Items.Add("-");
            treeContextMenu.Items.Add("▶ Run Script on Selected...", null, (s, e) => RunScriptOnSelected());

            treeScripts = new TreeView
            {
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                HideSelection = false,
                ShowLines = false,
                ItemHeight = 22,
                Font = new Font("Segoe UI", 9f),
                BorderStyle = BorderStyle.None
            };
            treeScripts.NodeMouseDoubleClick += TreeScripts_NodeMouseDoubleClick;
            treeScripts.MouseUp += TreeScripts_MouseUp;

            scriptContextMenu = new ContextMenuStrip();
            scriptContextMenu.Items.Add("➕ Add Script", null, (s, e) => AddScript());
            scriptContextMenu.Items.Add("✏️ Edit Script", null, (s, e) => EditScript());
            scriptContextMenu.Items.Add("🗑️ Delete Script", null, (s, e) => DeleteScript());

            leftSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 460,
                SplitterWidth = 4
            };

            Panel pnlConn = new Panel { Dock = DockStyle.Fill };
            pnlConn.Controls.Add(treeConnections);
            pnlConn.Controls.Add(leftNavHeader);

            Panel pnlScripts = new Panel { Dock = DockStyle.Fill };
            Panel scriptsHeader = new Panel { Dock = DockStyle.Top, Height = 26 };
            Label lblScriptsTitle = new Label { Text = "📜 SAVED SCRIPTS", Dock = DockStyle.Left, Width = 150, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            Button btnAddSc = new Button { Text = "➕", Dock = DockStyle.Right, Width = 28, FlatStyle = FlatStyle.Flat };
            btnAddSc.FlatAppearance.BorderSize = 0;
            btnAddSc.Click += (s, e) => AddScript();
            scriptsHeader.Controls.Add(lblScriptsTitle);
            scriptsHeader.Controls.Add(btnAddSc);

            pnlScripts.Controls.Add(treeScripts);
            pnlScripts.Controls.Add(scriptsHeader);

            leftSplit.Panel1.Controls.Add(pnlConn);
            leftSplit.Panel2.Controls.Add(pnlScripts);

            mainSplit.Panel1.Controls.Add(leftSplit);

            // ════════════════════════════════════════════════════════════════════
            // CENTER WORKSPACE & CONFIGURABLE LOGS DRAWER (DEVOLUTIONS STYLE)
            // ════════════════════════════════════════════════════════════════════
            rightSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                Panel2Collapsed = true, // LOGS REMOVED FROM VIEW BY DEFAULT!
                SplitterDistance = this.Height - 220,
                SplitterWidth = 5
            };

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                Padding = new Point(16, 5)
            };
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.MouseDown += TabControl_MouseDown;
            tabControl.SelectedIndexChanged += async (s, e) => {
                if (tabControl.SelectedTab?.Controls.Count > 0 && tabControl.SelectedTab.Controls[0] is FreeRdpContainer container)
                {
                    await Task.Delay(50);
                    container.FocusRdpWindow();
                }
            };

            // Add Quick Start Tab by Default
            ShowQuickStartTab();

            // Logs Drawer (Panel 2, collapsed by default)
            logsDrawer = new Panel { Dock = DockStyle.Fill };
            Panel logsHeader = new Panel { Dock = DockStyle.Top, Height = 28 };
            Label lblLogsTitle = new Label { Text = "  📋 SESSION LOGS & DIAGNOSTICS", Dock = DockStyle.Left, Width = 240, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };

            Button btnCloseLogs = new Button { Text = "✕ Close Logs", Dock = DockStyle.Right, Width = 90, FlatStyle = FlatStyle.Flat };
            btnCloseLogs.FlatAppearance.BorderSize = 0;
            btnCloseLogs.Click += (s, e) => HideLogsDrawer();

            Button btnClearLogs = new Button { Text = "🗑️ Clear", Dock = DockStyle.Right, Width = 65, FlatStyle = FlatStyle.Flat };
            btnClearLogs.FlatAppearance.BorderSize = 0;
            btnClearLogs.Click += (s, e) => txtLogs.Clear();

            Button btnAiDiag = new Button { Text = "🤖 Diagnose with AI", Dock = DockStyle.Right, Width = 140, FlatStyle = FlatStyle.Flat };
            btnAiDiag.FlatAppearance.BorderSize = 0;
            btnAiDiag.Click += (s, e) => {
                ToggleAiCopilotTab();
                if (_aiTabPage?.Controls.Count > 0 && _aiTabPage.Controls[0] is AiAssistantPanel ai)
                {
                    ai.GetActiveLogsFunc = () => txtLogs.Text;
                }
            };

            logsHeader.Controls.Add(lblLogsTitle);
            logsHeader.Controls.Add(btnAiDiag);
            logsHeader.Controls.Add(btnClearLogs);
            logsHeader.Controls.Add(btnCloseLogs);

            txtLogs = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(18, 18, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None
            };

            logsDrawer.Controls.Add(txtLogs);
            logsDrawer.Controls.Add(logsHeader);

            rightSplit.Panel1.Controls.Add(tabControl);
            rightSplit.Panel2.Controls.Add(logsDrawer);

            mainSplit.Panel2.Controls.Add(rightSplit);

            // ════════════════════════════════════════════════════════════════════
            // BOTTOM STATUS BAR (CLEAN & MINIMALIST)
            // ════════════════════════════════════════════════════════════════════
            Panel statusBar = new Panel { Dock = DockStyle.Bottom, Height = 26, Padding = new Padding(6, 2, 6, 2) };
            
            lblStatusText = new Label
            {
                Text = $"⚡ Ready • {Environment.UserName} • 🔒 Vault: AES-256-GCM • FreeRDP 3.33.0 (.NET 10)",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.CornflowerBlue
            };

            btnLogsToggle = new Button
            {
                Text = "📋 Logs",
                Dock = DockStyle.Right,
                Width = 70,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f)
            };
            btnLogsToggle.FlatAppearance.BorderSize = 0;
            btnLogsToggle.Click += (s, e) => ToggleLogsDrawer();

            statusBar.Controls.Add(lblStatusText);
            statusBar.Controls.Add(btnLogsToggle);

            this.Controls.Add(mainSplit);
            this.Controls.Add(statusBar);
            this.Controls.Add(quickConnectRow);
            this.Controls.Add(topNavPanel);

            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.F11) ToggleFullscreen();
            };

            ApplyTheme(this);
        }

        private Button CreateNavButton(string text, EventHandler onClick, bool isPrimary = false)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, isPrimary ? FontStyle.Bold : FontStyle.Regular),
                Margin = new Padding(2),
                BackColor = isPrimary ? Color.FromArgb(0, 120, 215) : Color.Transparent,
                ForeColor = isPrimary ? Color.White : Color.Empty
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private Label CreateSeparator()
        {
            return new Label
            {
                Text = "|",
                AutoSize = true,
                ForeColor = Color.Gray,
                Padding = new Padding(3, 5, 3, 0)
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // QUICK START DASHBOARD TAB (DEVOLUTIONS STYLE)
        // ════════════════════════════════════════════════════════════════════
        private void ShowQuickStartTab()
        {
            if (_quickStartTab != null && tabControl.TabPages.Contains(_quickStartTab))
            {
                tabControl.SelectedTab = _quickStartTab;
                return;
            }

            _quickStartTab = new TabPage("🚀 Quick start");
            var dashboard = new QuickStartDashboard();
            dashboard.QuickConnectClicked += (s, e) => {
                quickConnectRow.Visible = true;
                foreach (Control c in quickConnectRow.Controls)
                {
                    if (c is TextBox tb && tb.PlaceholderText.Contains("Host"))
                    {
                        tb.Focus();
                        break;
                    }
                }
            };
            dashboard.AddEntryClicked += (s, e) => AddConnection();
            dashboard.ImportClicked += (s, e) => ImportRdpFile();
            dashboard.ManageVaultClicked += (s, e) => OpenSettings();
            dashboard.ProbeNetworkClicked += (s, e) => {
                if (treeConnections.SelectedNode?.Tag is SavedConnection)
                    _ = ProbeSelectedServer();
                else
                    MessageBox.Show("Select a server from the connection tree on the left to probe its reachability.", "Probe Network", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            _quickStartTab.Controls.Add(dashboard);
            tabControl.TabPages.Insert(0, _quickStartTab);
            tabControl.SelectedTab = _quickStartTab;
        }

        // ════════════════════════════════════════════════════════════════════
        // LOG DRAWER & CONFIGURABLE LOGGING
        // ════════════════════════════════════════════════════════════════════
        private void ToggleLogsDrawer()
        {
            _isLogsVisible = !_isLogsVisible;
            rightSplit.Panel2Collapsed = !_isLogsVisible;
            if (_isLogsVisible)
            {
                rightSplit.SplitterDistance = Math.Max(150, rightSplit.Height - 200);
                btnLogsToggle.Text = "📋 Logs ▲";
            }
            else
            {
                btnLogsToggle.Text = "📋 Logs";
            }
        }

        private void HideLogsDrawer()
        {
            _isLogsVisible = false;
            rightSplit.Panel2Collapsed = true;
            btnLogsToggle.Text = "📋 Logs";
        }

        private void LogMessage(string msg, LogLevelSetting level = LogLevelSetting.Info)
        {
            if (_settings.LogLevel == LogLevelSetting.Off) return;
            if (_settings.LogLevel == LogLevelSetting.Error && level != LogLevelSetting.Error) return;
            if (_settings.LogLevel == LogLevelSetting.Info && level == LogLevelSetting.Trace) return;

            if (txtLogs.IsHandleCreated && !txtLogs.Disposing)
            {
                txtLogs.BeginInvoke((MethodInvoker)delegate {
                    if (txtLogs.TextLength > 60000) txtLogs.Clear();
                    txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpperInvariant()}] {msg}\r\n");
                    txtLogs.SelectionStart = txtLogs.TextLength;
                    txtLogs.ScrollToCaret();
                });
            }

            // Auto show logs on error if configured
            if (level == LogLevelSetting.Error && _settings.AutoShowLogsOnError && !_isLogsVisible)
            {
                if (this.IsHandleCreated && !this.Disposing)
                {
                    this.BeginInvoke((MethodInvoker)delegate {
                        ToggleLogsDrawer();
                    });
                }
            }
        }

        private void OpenSettings()
        {
            using var dlg = new SettingsDialog();
            dlg.SettingsChanged += () => {
                _settings = SettingsStore.LoadSettings();
                ApplyTheme(this);
            };
            dlg.ShowDialog(this);
        }

        // ════════════════════════════════════════════════════════════════════
        // RETRACTABLE & PINNABLE SIDEBAR
        // ════════════════════════════════════════════════════════════════════
        private void TogglePinLeft()
        {
            _isLeftPinned = !_isLeftPinned;
            btnPinLeft.Text = _isLeftPinned ? "📌" : "📍";
        }

        private void ToggleCollapseLeft()
        {
            _isLeftCollapsed = !_isLeftCollapsed;
            if (_isLeftCollapsed)
            {
                _leftSavedWidth = mainSplit.SplitterDistance;
                mainSplit.SplitterDistance = 32;
                btnCollapseLeft.Text = "▶";
            }
            else
            {
                mainSplit.SplitterDistance = _leftSavedWidth > 50 ? _leftSavedWidth : 250;
                btnCollapseLeft.Text = "◀";
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // LIVE SEARCH FILTER
        // ════════════════════════════════════════════════════════════════════
        private void FilterTree(string query)
        {
            query = query.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(query))
            {
                RefreshTree();
                lblSearchStats.Text = "";
                return;
            }

            treeConnections.Nodes.Clear();
            var dict = new Dictionary<string, TreeNode>();
            int matchCount = 0;

            foreach (var c in _connections)
            {
                bool matches = c.Name.ToLowerInvariant().Contains(query) ||
                               c.Host.ToLowerInvariant().Contains(query) ||
                               c.Group.ToLowerInvariant().Contains(query) ||
                               c.User.ToLowerInvariant().Contains(query);

                if (matches)
                {
                    matchCount++;
                    if (!dict.ContainsKey(c.Group))
                    {
                        var groupNode = new TreeNode($"📁 {c.Group}");
                        dict[c.Group] = groupNode;
                        treeConnections.Nodes.Add(groupNode);
                    }

                    var node = new TreeNode($"🖥️ {c.Name} ({c.Host})") { Tag = c };
                    dict[c.Group].Nodes.Add(node);
                }
            }

            treeConnections.ExpandAll();
            lblSearchStats.Text = $"  Found {matchCount} match(es)";
        }

        // ════════════════════════════════════════════════════════════════════
        // ACTIVE SESSION ACTIONS
        // ════════════════════════════════════════════════════════════════════
        private void ReconnectActiveSession()
        {
            if (tabControl.SelectedTab?.Controls.Count > 0 && tabControl.SelectedTab.Controls[0] is FreeRdpContainer container)
            {
                container.Reconnect();
                LogMessage("Reconnected active session.", LogLevelSetting.Info);
            }
            else
            {
                MessageBox.Show("No active RDP session selected.", "Reconnect", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void CaptureActiveScreenshot()
        {
            if (tabControl.SelectedTab?.Controls.Count > 0 && tabControl.SelectedTab.Controls[0] is FreeRdpContainer container)
            {
                var bmp = container.CaptureScreenshot();
                if (bmp != null)
                {
                    Clipboard.SetImage(bmp);

                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "AeroRemote_Screenshots");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string file = Path.Combine(dir, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);

                    LogMessage($"Screenshot saved to: {file}", LogLevelSetting.Info);
                    MessageBox.Show($"Screenshot copied to clipboard and saved to:\r\n{file}", "Screenshot Captured", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("No active RDP session selected to screenshot.", "Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SendActiveCtrlAltDel()
        {
            if (tabControl.SelectedTab?.Controls.Count > 0 && tabControl.SelectedTab.Controls[0] is FreeRdpContainer container)
            {
                container.SendCtrlAltDel();
            }
            else
            {
                MessageBox.Show("No active RDP session selected.", "Send Keys", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ToggleAiCopilotTab()
        {
            if (_aiTabPage != null && tabControl.TabPages.Contains(_aiTabPage))
            {
                tabControl.SelectedTab = _aiTabPage;
                return;
            }

            _aiTabPage = new TabPage("🤖 AI Remote Ops Copilot");
            var aiPanel = new AiAssistantPanel
            {
                Dock = DockStyle.Fill,
                GetActiveLogsFunc = () => txtLogs.Text
            };
            _aiTabPage.Controls.Add(aiPanel);
            tabControl.TabPages.Add(_aiTabPage);
            tabControl.SelectedTab = _aiTabPage;
        }

        // ════════════════════════════════════════════════════════════════════
        // REACHABILITY PROBE
        // ════════════════════════════════════════════════════════════════════
        private async Task ProbeSelectedServer()
        {
            if (treeConnections.SelectedNode?.Tag is SavedConnection c)
            {
                lblStatusText.Text = $"⚡ Probing {c.Host}...";
                var result = await NetworkTester.ProbeHostAsync(c.Host);

                lblStatusText.Text = $"⚡ [{c.Name}] {result.SummaryText}";
                LogMessage($"[Probe] {c.Name} ({c.Host}): {result.SummaryText}", LogLevelSetting.Info);

                MessageBox.Show($"Reachability probe for '{c.Name}' ({c.Host}):\r\n\r\n" +
                                $"• TCP Port {result.Port}: {(result.IsPortOpen ? $"OPEN ({result.PortHandshakeMs} ms)" : "CLOSED / FIREWALLED")}\r\n" +
                                $"• ICMP Ping: {(result.IsPingSuccessful ? $"REACHABLE ({result.PingRoundtripMs} ms)" : "UNREACHABLE / BLOCKED")}\r\n" +
                                (string.IsNullOrEmpty(result.ErrorMessage) ? "" : $"• Note: {result.ErrorMessage}"),
                                "Server Reachability Probe",
                                MessageBoxButtons.OK,
                                result.IsPortOpen ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
        }

        private void CopySelectedPassword()
        {
            if (treeConnections.SelectedNode?.Tag is SavedConnection c)
            {
                string plain = VaultCrypto.DecryptSecret(c.Pass);
                if (string.IsNullOrEmpty(plain)) plain = c.Pass;

                if (!string.IsNullOrEmpty(plain))
                {
                    Clipboard.SetText(plain);
                    LogMessage($"Password for {c.Name} copied to clipboard (wipes in 30s).", LogLevelSetting.Info);

                    _clipboardTimer?.Stop();
                    _clipboardTimer?.Dispose();
                    _clipboardTimer = new System.Windows.Forms.Timer { Interval = 30000 };
                    _clipboardTimer.Tick += (s, e) => {
                        try {
                            if (Clipboard.GetText() == plain) Clipboard.Clear();
                        } catch { }
                        _clipboardTimer.Stop();
                        _clipboardTimer.Dispose();
                    };
                    _clipboardTimer.Start();

                    MessageBox.Show("Password copied to clipboard.\r\nIt will be automatically wiped in 30 seconds for security.", "Password Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No password stored for this connection.", "Password", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void DuplicateConnection()
        {
            if (treeConnections.SelectedNode?.Tag is SavedConnection c)
            {
                string json = System.Text.Json.JsonSerializer.Serialize(c);
                var clone = System.Text.Json.JsonSerializer.Deserialize<SavedConnection>(json);
                if (clone != null)
                {
                    clone.Id = Guid.NewGuid().ToString();
                    clone.Name += " (Copy)";
                    _connections.Add(clone);
                    _store.SaveConnections(_connections);
                    RefreshTree();
                    LogMessage($"Duplicated connection: {clone.Name}", LogLevelSetting.Info);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // IMPORT / EXPORT VAULT & RDP FILES
        // ════════════════════════════════════════════════════════════════════
        private void ImportRdpFile()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Remote Desktop Files (*.rdp)|*.rdp|All Files (*.*)|*.*",
                Title = "Import Microsoft RDP Connection File"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var conn = _store.ImportRdp(ofd.FileName);
                    _connections.Add(conn);
                    _store.SaveConnections(_connections);
                    RefreshTree();
                    LogMessage($"Imported RDP file: {ofd.FileName} -> {conn.Name}", LogLevelSetting.Info);
                    MessageBox.Show($"Successfully imported '{conn.Name}' ({conn.Host})!", "Import RDP", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to parse RDP file: {ex.Message}", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportSelectedToRdp()
        {
            if (treeConnections.SelectedNode?.Tag is SavedConnection c)
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "Remote Desktop Files (*.rdp)|*.rdp",
                    FileName = $"{c.Name}.rdp",
                    Title = "Export Connection to .rdp"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _store.ExportRdp(c, sfd.FileName);
                        LogMessage($"Exported {c.Name} to {sfd.FileName}", LogLevelSetting.Info);
                        MessageBox.Show($"Exported '{c.Name}' to {sfd.FileName}", "Export RDP", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportVaultArchive()
        {
            using var dlg = new Form
            {
                Text = "Export Encrypted Vault Backup",
                Size = new Size(380, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label lbl = new Label { Text = "Choose a strong password to encrypt your vault backup:", Location = new Point(20, 20), Width = 320, Height = 35 };
            TextBox txtPass = new TextBox { Location = new Point(20, 60), Width = 320, PasswordChar = '*' };
            Button btnExport = new Button { Text = "Save Backup...", Location = new Point(140, 110), Width = 110 };
            Button btnCancel = new Button { Text = "Cancel", Location = new Point(260, 110), Width = 80 };

            btnExport.Click += (s, e) => {
                if (string.IsNullOrEmpty(txtPass.Text))
                {
                    MessageBox.Show("Please enter an encryption password.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "FreeRDP Vault (*.frdpvault)|*.frdpvault",
                    FileName = $"AeroRemote_Vault_{DateTime.Now:yyyyMMdd}.frdpvault"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _store.ExportVault(sfd.FileName, txtPass.Text, _connections);
                        LogMessage($"Exported encrypted vault backup to {sfd.FileName}", LogLevelSetting.Info);
                        MessageBox.Show("Encrypted vault backup successfully created!", "Vault Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            btnCancel.Click += (s, e) => dlg.Close();
            dlg.Controls.AddRange(new Control[] { lbl, txtPass, btnExport, btnCancel });
            dlg.ShowDialog(this);
        }

        // ════════════════════════════════════════════════════════════════════
        // THEME & FULLSCREEN
        // ════════════════════════════════════════════════════════════════════
        private void CycleTheme()
        {
            var themes = (AppTheme[])Enum.GetValues(typeof(AppTheme));
            int nextIndex = ((int)ThemeSettings.CurrentTheme + 1) % themes.Length;
            ThemeSettings.CurrentTheme = themes[nextIndex];
            ApplyTheme(this);
        }

        private bool _isFullscreen = false;
        private void ToggleFullscreen()
        {
            if (tabControl.TabCount == 0)
            {
                MessageBox.Show("Please connect to a session first before entering fullscreen.", "Fullscreen", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isFullscreen = !_isFullscreen;
            if (_isFullscreen)
            {
                mainSplit.Panel1Collapsed = true;
                topNavPanel.Visible = false;
                quickConnectRow.Visible = false;

                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                tabControl.Padding = new Point(0, 0);
            }
            else
            {
                mainSplit.Panel1Collapsed = false;
                topNavPanel.Visible = true;
                quickConnectRow.Visible = true;

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                tabControl.Padding = new Point(16, 5);
            }
        }

        private void ApplyTheme(Control parent)
        {
            Color bg, fg, panelBg, treeBg, activeTabBg, inactiveTabBg, logBg;

            switch (ThemeSettings.CurrentTheme)
            {
                case AppTheme.Light:
                    bg = Color.FromArgb(248, 249, 250);
                    fg = Color.Black;
                    panelBg = Color.FromArgb(235, 237, 240);
                    treeBg = Color.White;
                    activeTabBg = SystemColors.Window;
                    inactiveTabBg = SystemColors.ControlLight;
                    logBg = Color.White;
                    break;
                case AppTheme.SolarizedDark:
                    bg = Color.FromArgb(0, 43, 54);
                    fg = Color.FromArgb(131, 148, 150);
                    panelBg = Color.FromArgb(7, 54, 66);
                    treeBg = Color.FromArgb(0, 43, 54);
                    activeTabBg = Color.FromArgb(7, 54, 66);
                    inactiveTabBg = Color.FromArgb(0, 43, 54);
                    logBg = Color.FromArgb(0, 43, 54);
                    break;
                case AppTheme.Dracula:
                    bg = Color.FromArgb(40, 42, 54);
                    fg = Color.FromArgb(248, 248, 242);
                    panelBg = Color.FromArgb(68, 71, 90);
                    treeBg = Color.FromArgb(40, 42, 54);
                    activeTabBg = Color.FromArgb(68, 71, 90);
                    inactiveTabBg = Color.FromArgb(40, 42, 54);
                    logBg = Color.FromArgb(40, 42, 54);
                    break;
                case AppTheme.HighContrast:
                    bg = Color.Black;
                    fg = Color.Lime;
                    panelBg = Color.FromArgb(20, 20, 20);
                    treeBg = Color.Black;
                    activeTabBg = Color.FromArgb(20, 20, 20);
                    inactiveTabBg = Color.Black;
                    logBg = Color.Black;
                    break;
                case AppTheme.Dark:
                default:
                    bg = Color.FromArgb(28, 30, 34);
                    fg = Color.FromArgb(220, 225, 232);
                    panelBg = Color.FromArgb(36, 38, 44);
                    treeBg = Color.FromArgb(24, 25, 28);
                    activeTabBg = Color.FromArgb(36, 38, 44);
                    inactiveTabBg = Color.FromArgb(24, 25, 28);
                    logBg = Color.FromArgb(18, 18, 20);
                    break;
            }

            this.BackColor = bg;
            this.ForeColor = fg;
            topNavPanel.BackColor = panelBg;
            quickConnectRow.BackColor = panelBg;

            treeConnections.BackColor = treeBg;
            treeConnections.ForeColor = fg;

            treeScripts.BackColor = treeBg;
            treeScripts.ForeColor = fg;

            txtLogs.BackColor = logBg;
            txtLogs.ForeColor = fg;

            leftNavHeader.BackColor = panelBg;
            txtSearch.BackColor = logBg;
            txtSearch.ForeColor = fg;

            tabControl.Invalidate();
        }

        private void RefreshTree()
        {
            treeConnections.Nodes.Clear();
            var dict = new Dictionary<string, TreeNode>();

            foreach (var c in _connections)
            {
                if (!dict.ContainsKey(c.Group))
                {
                    var groupNode = new TreeNode($"📁 {c.Group}");
                    dict[c.Group] = groupNode;
                    treeConnections.Nodes.Add(groupNode);
                }

                var node = new TreeNode($"🖥️ {c.Name} ({c.Host})") { Tag = c };
                dict[c.Group].Nodes.Add(node);
            }

            treeConnections.ExpandAll();
        }

        private void TreeConnections_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeNode? node = treeConnections.GetNodeAt(e.X, e.Y);
                if (node != null) treeConnections.SelectedNode = node;
                treeContextMenu.Show(treeConnections, e.Location);
            }
        }

        private void AddConnection()
        {
            using var dlg = new ConnectionDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _connections.Add(dlg.Connection);
                _store.SaveConnections(_connections);
                RefreshTree();
            }
        }

        private void EditConnection()
        {
            if (treeConnections.SelectedNode?.Tag is SavedConnection c)
            {
                using var dlg = new ConnectionDialog(c);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _store.SaveConnections(_connections);
                    RefreshTree();
                }
            }
        }

        private void DeleteConnection()
        {
            if (treeConnections.SelectedNode?.Tag is SavedConnection c)
            {
                if (MessageBox.Show($"Delete connection '{c.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _connections.Remove(c);
                    _store.SaveConnections(_connections);
                    RefreshTree();
                }
            }
        }

        private void RefreshScriptsTree()
        {
            treeScripts.Nodes.Clear();
            foreach (var sc in _scripts)
            {
                treeScripts.Nodes.Add(new TreeNode($"📜 {sc.Name}") { Tag = sc });
            }
        }

        private void TreeScripts_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeNode? node = treeScripts.GetNodeAt(e.X, e.Y);
                if (node != null) treeScripts.SelectedNode = node;
                scriptContextMenu.Show(treeScripts, e.Location);
            }
        }

        private void TreeScripts_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            EditScript();
        }

        private void AddScript()
        {
            using var dlg = new ScriptEditDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _scripts.Add(dlg.Script);
                _scriptStore.SaveScripts(_scripts);
                RefreshScriptsTree();
            }
        }

        private void EditScript()
        {
            if (treeScripts.SelectedNode?.Tag is SavedScript sc)
            {
                using var dlg = new ScriptEditDialog(sc);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _scriptStore.SaveScripts(_scripts);
                    RefreshScriptsTree();
                }
            }
        }

        private void DeleteScript()
        {
            if (treeScripts.SelectedNode?.Tag is SavedScript sc)
            {
                if (MessageBox.Show($"Delete script '{sc.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _scripts.Remove(sc);
                    _scriptStore.SaveScripts(_scripts);
                    RefreshScriptsTree();
                }
            }
        }

        private async void RunScriptOnSelected()
        {
            var selectedConnections = new List<SavedConnection>();
            foreach (TreeNode groupNode in treeConnections.Nodes)
            {
                foreach (TreeNode node in groupNode.Nodes)
                {
                    if (node.Checked && node.Tag is SavedConnection c && c.Protocol == ConnectionProtocol.SSH)
                    {
                        selectedConnections.Add(c);
                    }
                }
            }

            if (selectedConnections.Count == 0)
            {
                MessageBox.Show("Please check the boxes next to the SSH connections you want to run a script against.", "No SSH Connections Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var scripts = _scriptStore.LoadScripts();
            if (scripts.Count == 0)
            {
                MessageBox.Show("No scripts found. Please create one using 'Manage SSH Scripts'.", "No Scripts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var picker = new Form { Text = "Select Script to Run", Size = new Size(300, 150), FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MaximizeBox = false, MinimizeBox = false };
            var cmb = new ComboBox { Location = new Point(20, 20), Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", DataSource = scripts };
            var btnRun = new Button { Text = "Run", Location = new Point(180, 60), Width = 80 };
            btnRun.Click += (s, e) => { picker.DialogResult = DialogResult.OK; picker.Close(); };
            picker.Controls.Add(cmb);
            picker.Controls.Add(btnRun);

            if (picker.ShowDialog() == DialogResult.OK && cmb.SelectedItem is SavedScript selectedScript)
            {
                LogMessage($"\n--- Starting Bulk Execution: {selectedScript.Name} ---", LogLevelSetting.Info);
                foreach (var conn in selectedConnections)
                {
                    LogMessage($"[{conn.Name}] Executing script...", LogLevelSetting.Info);
                    string result = await SshLauncher.ExecuteBackgroundCommandAsync(conn, selectedScript.Content);
                    LogMessage($"[{conn.Name}] Result:\n{result}", LogLevelSetting.Info);
                }
                LogMessage("--- Bulk Execution Complete ---\n", LogLevelSetting.Info);
            }
        }

        private void TreeConnections_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is SavedConnection c)
            {
                LaunchSession(c);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // TAB DRAWING & MANAGEMENT
        // ════════════════════════════════════════════════════════════════════
        private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            var tabPage = tabControl.TabPages[e.Index];
            var tabRect = tabControl.GetTabRect(e.Index);

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color activeBg, inactiveBg, textFg;

            switch (ThemeSettings.CurrentTheme)
            {
                case AppTheme.Light:
                    activeBg = SystemColors.Window;
                    inactiveBg = SystemColors.ControlLight;
                    textFg = SystemColors.ControlText;
                    break;
                case AppTheme.SolarizedDark:
                    activeBg = Color.FromArgb(7, 54, 66);
                    inactiveBg = Color.FromArgb(0, 43, 54);
                    textFg = Color.FromArgb(131, 148, 150);
                    break;
                case AppTheme.Dracula:
                    activeBg = Color.FromArgb(68, 71, 90);
                    inactiveBg = Color.FromArgb(40, 42, 54);
                    textFg = Color.FromArgb(248, 248, 242);
                    break;
                case AppTheme.HighContrast:
                    activeBg = Color.FromArgb(20, 20, 20);
                    inactiveBg = Color.Black;
                    textFg = Color.Lime;
                    break;
                case AppTheme.Dark:
                default:
                    activeBg = Color.FromArgb(36, 38, 44);
                    inactiveBg = Color.FromArgb(24, 25, 28);
                    textFg = Color.FromArgb(220, 225, 232);
                    break;
            }

            Brush backBrush = new SolidBrush(isSelected ? activeBg : inactiveBg);
            e.Graphics.FillRectangle(backBrush, tabRect);

            if (isSelected)
            {
                e.Graphics.DrawLine(Pens.DodgerBlue, tabRect.Left, tabRect.Bottom - 1, tabRect.Right, tabRect.Bottom - 1);
            }

            string title = tabPage.Text;
            Font font = e.Font ?? tabControl.Font;

            e.Graphics.DrawString(title, font, new SolidBrush(textFg), new PointF(tabRect.X + 6, tabRect.Y + 5));

            // Close button (only for non-dashboard tabs)
            if (tabPage != _quickStartTab)
            {
                var closeRect = new Rectangle(tabRect.Right - 16, tabRect.Y + 6, 12, 12);
                e.Graphics.DrawString("×", new Font("Arial", 9, FontStyle.Bold), Brushes.IndianRed, closeRect);
            }

            backBrush.Dispose();
        }

        private void TabControl_MouseDown(object? sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                if (tabControl.TabPages[i] == _quickStartTab) continue;

                Rectangle r = tabControl.GetTabRect(i);
                Rectangle closeRect = new Rectangle(r.Right - 18, r.Y + 4, 14, 14);
                if (closeRect.Contains(e.Location))
                {
                    CloseTab(tabControl.TabPages[i]);
                    break;
                }
            }
        }

        private void CloseTab(TabPage tabPage)
        {
            if (tabPage.Controls.Count > 0 && tabPage.Controls[0] is FreeRdpContainer container)
            {
                container.Disconnect();
                container.Dispose();
            }
            tabControl.TabPages.Remove(tabPage);
            tabPage.Dispose();
        }

        // ════════════════════════════════════════════════════════════════════
        // SESSION LAUNCHING & ENGINE RESOLUTION
        // ════════════════════════════════════════════════════════════════════
        private void LaunchSession(SavedConnection config)
        {
            if (config.Protocol == ConnectionProtocol.SSH)
            {
                LogMessage($"[{config.Name}] Launching external SSH terminal...", LogLevelSetting.Info);
                SshLauncher.LaunchInteractiveSession(config);
                return;
            }

            string exeName = config.ClientType switch
            {
                RdpClientType.Sdl3     => "sdl3-freerdp.exe",
                RdpClientType.XFreeRdp => "xfreerdp.exe",
                _                      => "wfreerdp.exe"
            };
            string relativeBuildPath = Path.Combine("Compiled", "bin", exeName);

            string possiblePath = exeName;
            string? currentDir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName) ?? AppDomain.CurrentDomain.BaseDirectory;

            if (File.Exists(Path.Combine(currentDir, "Bin", "FreeRDP", exeName)))
            {
                possiblePath = Path.Combine(currentDir, "Bin", "FreeRDP", exeName);
            }
            else
            {
                while (!string.IsNullOrEmpty(currentDir))
                {
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
            }

            var tabPage = new TabPage($"🟢 {config.Name}");
            var container = new FreeRdpContainer { Dock = DockStyle.Fill };

            container.OnLogMessage += (msg) => {
                var level = msg.Contains("Failed", StringComparison.OrdinalIgnoreCase) || msg.Contains("Error", StringComparison.OrdinalIgnoreCase)
                    ? LogLevelSetting.Error
                    : LogLevelSetting.Trace;
                LogMessage($"[{config.Name}] {msg}", level);
            };

            container.OnDisconnected += () => {
                if (tabControl.IsHandleCreated && !tabControl.Disposing)
                {
                    tabControl.Invoke((MethodInvoker)delegate {
                        LogMessage($"[{config.Name}] Session Disconnected.", LogLevelSetting.Info);
                        if (tabControl.TabPages.Contains(tabPage))
                        {
                            tabControl.TabPages.Remove(tabPage);
                            container.Dispose();
                        }
                    });
                }
            };

            tabPage.Controls.Add(container);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;

            try
            {
                LogMessage($"Attempting to launch {possiblePath}", LogLevelSetting.Trace);
                container.LaunchSession(config, possiblePath);
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to launch {possiblePath}: {ex.Message}", LogLevelSetting.Error);
            }
        }
    }
}
