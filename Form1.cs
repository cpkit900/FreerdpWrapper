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

        // UI Zones (Rocket Remote Desktop Inspired)
        private Panel ribbonPanel = default!;       // Top Zone A: Command Bar & Quick Connect
        private SplitContainer mainSplit = default!; // Left: Nav, Right: Workspace
        private SplitContainer leftSplit = default!; // Top: Connections, Bottom: Scripts
        private SplitContainer rightSplit = default!; // Top: Tabs, Bottom: Logs
        
        // Navigation Zone B
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
        private int _leftSavedWidth = 240;
        private Button btnPinLeft = default!;
        private Button btnCollapseLeft = default!;

        private bool _isBottomPinned = true;
        private bool _isBottomCollapsed = false;
        private int _bottomSavedHeight = 180;
        private Button btnPinBottom = default!;
        private Button btnToggleBottom = default!;
        private Label lblBottomStatus = default!;

        private bool _isRibbonCompact = false;
        private Panel ribbonActionRow = default!;
        private Panel quickConnectRow = default!;

        // Center Workspace Zone C
        private TabControl tabControl = default!;
        private TextBox txtLogs = default!;
        private TabPage? _aiTabPage = null;

        // Clipboard clear timer
        private System.Windows.Forms.Timer? _clipboardTimer;

        public Form1()
        {
            InitializeComponent();
            _store = new ConnectionStore();
            _connections = _store.LoadConnections();
            _scriptStore = new ScriptStore();
            _scripts = _scriptStore.LoadScripts();
            
            SetupUI();
            RefreshTree();
            RefreshScriptsTree();
        }

        private void SetupUI()
        {
            this.Text = "FreeRDP Wrapper - Modern Remote Desktop Manager (.NET 10)";
            this.Width = 1280;
            this.Height = 840;
            this.StartPosition = FormStartPosition.CenterScreen;

            // ════════════════════════════════════════════════════════════════════
            // ZONE A: TOP COMMAND RIBBON & QUICK CONNECT
            // ════════════════════════════════════════════════════════════════════
            ribbonPanel = new Panel { Dock = DockStyle.Top, Height = 84, AutoSize = false };

            // Row 1: Action Ribbon Buttons
            ribbonActionRow = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(4) };
            
            Button btnAdd = CreateToolButton("➕ New Server", (s, e) => AddConnection());
            Button btnImportRdp = CreateToolButton("📥 Import .rdp", (s, e) => ImportRdpFile());
            Button btnExportVault = CreateToolButton("📤 Export Vault", (s, e) => ExportVaultArchive());
            Button btnImportVault = CreateToolButton("📥 Import Vault", (s, e) => ImportVaultArchive());

            Button btnReconnect = CreateToolButton("🔁 Reconnect", (s, e) => ReconnectActiveSession());
            Button btnScreenshot = CreateToolButton("📸 Screenshot", (s, e) => CaptureActiveScreenshot());
            Button btnCtrlAltDel = CreateToolButton("⌨️ Ctrl+Alt+Del", (s, e) => SendActiveCtrlAltDel());
            Button btnAiCopilot = CreateToolButton("🤖 AI Copilot", (s, e) => ToggleAiCopilotTab());
            Button btnFullscreen = CreateToolButton("⛶ Fullscreen (F11)", (s, e) => ToggleFullscreen());
            Button btnTheme = CreateToolButton("🌗 Theme", (s, e) => CycleTheme());
            Button btnCompactRibbon = CreateToolButton("▲ Compact", (s, e) => ToggleRibbonCompact());

            FlowLayoutPanel ribbonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true
            };
            ribbonFlow.Controls.AddRange(new Control[] {
                btnAdd, btnImportRdp, btnExportVault, btnImportVault,
                CreateSeparator(),
                btnReconnect, btnScreenshot, btnCtrlAltDel,
                CreateSeparator(),
                btnAiCopilot, btnFullscreen, btnTheme, btnCompactRibbon
            });
            ribbonActionRow.Controls.Add(ribbonFlow);

            // Row 2: Quick Connect Address Bar
            quickConnectRow = new Panel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(6) };
            
            Label lblQc = new Label { Text = "⚡ Quick Connect:", Location = new Point(8, 12), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
            TextBox txtQHost = new TextBox { Location = new Point(130, 9), Width = 170, PlaceholderText = "Host or IP[:Port]", Font = new Font("Segoe UI", 9.5f) };
            TextBox txtQUser = new TextBox { Location = new Point(310, 9), Width = 130, PlaceholderText = "Username", Font = new Font("Segoe UI", 9.5f) };
            TextBox txtQPass = new TextBox { Location = new Point(450, 9), Width = 130, PlaceholderText = "Password", PasswordChar = '*', Font = new Font("Segoe UI", 9.5f) };
            
            ComboBox cmbQClient = new ComboBox { Location = new Point(590, 8), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cmbQClient.Items.AddRange(new object[] { "wfreerdp (Windows)", "sdl3 (Modern D3D11)", "xfreerdp (Linux/WSL)" });
            cmbQClient.SelectedIndex = 0;

            Button btnQConnect = new Button 
            { 
                Text = "🚀 Connect", 
                Location = new Point(750, 7), 
                Width = 100, 
                Height = 28, 
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

                    LaunchSession(new SavedConnection {
                        Name = "Quick: " + txtQHost.Text,
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

            ribbonPanel.Controls.Add(quickConnectRow);
            ribbonPanel.Controls.Add(ribbonActionRow);

            // ════════════════════════════════════════════════════════════════════
            // ZONE B: LEFT NAVIGATION & LIVE SEARCH DOCK (RETRACTABLE & PINNABLE)
            // ════════════════════════════════════════════════════════════════════
            mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                SplitterDistance = _leftSavedWidth,
                SplitterWidth = 5
            };

            // Search Header
            leftNavHeader = new Panel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(4) };
            
            Panel headerTop = new Panel { Dock = DockStyle.Top, Height = 28 };
            Label lblNavTitle = new Label { Text = "🌍 CONNECTIONS", Dock = DockStyle.Left, Width = 140, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            
            btnPinLeft = new Button { Text = "📌", Dock = DockStyle.Right, Width = 30, FlatStyle = FlatStyle.Flat };
            btnPinLeft.FlatAppearance.BorderSize = 0;
            btnPinLeft.Click += (s, e) => TogglePinLeft();

            btnCollapseLeft = new Button { Text = "◀", Dock = DockStyle.Right, Width = 30, FlatStyle = FlatStyle.Flat };
            btnCollapseLeft.FlatAppearance.BorderSize = 0;
            btnCollapseLeft.Click += (s, e) => ToggleCollapseLeft();

            headerTop.Controls.Add(lblNavTitle);
            headerTop.Controls.Add(btnPinLeft);
            headerTop.Controls.Add(btnCollapseLeft);

            Panel searchBar = new Panel { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(2) };
            txtSearch = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "🔍 Search servers (Host, Name)...", Font = new Font("Segoe UI", 9f) };
            txtSearch.TextChanged += (s, e) => FilterTree(txtSearch.Text);

            btnClearSearch = new Button { Text = "✖", Dock = DockStyle.Right, Width = 26, FlatStyle = FlatStyle.Flat };
            btnClearSearch.FlatAppearance.BorderSize = 0;
            btnClearSearch.Click += (s, e) => { txtSearch.Clear(); };

            lblSearchStats = new Label { Dock = DockStyle.Bottom, Height = 14, Font = new Font("Segoe UI", 7.5f), ForeColor = Color.Gray, Text = "" };

            searchBar.Controls.Add(txtSearch);
            searchBar.Controls.Add(btnClearSearch);

            leftNavHeader.Controls.Add(headerTop);
            leftNavHeader.Controls.Add(searchBar);
            leftNavHeader.Controls.Add(lblSearchStats);

            // Trees
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

            // Rich Context Menu for Connections
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
                SplitterDistance = 420,
                SplitterWidth = 4
            };

            Panel pnlConn = new Panel { Dock = DockStyle.Fill };
            pnlConn.Controls.Add(treeConnections);
            pnlConn.Controls.Add(leftNavHeader);

            Panel pnlScripts = new Panel { Dock = DockStyle.Fill };
            Panel scriptsHeader = new Panel { Dock = DockStyle.Top, Height = 26 };
            Label lblScriptsTitle = new Label { Text = "📜 SAVED SCRIPTS", Dock = DockStyle.Left, Width = 150, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            Button btnAddSc = new Button { Text = "➕", Dock = DockStyle.Right, Width = 30, FlatStyle = FlatStyle.Flat };
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
            // ZONE C & D: WORKSPACE & RETRACTABLE BOTTOM STATUS/LOGS DOCK
            // ════════════════════════════════════════════════════════════════════
            rightSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = this.Height - _bottomSavedHeight - 120,
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

            // Zone D: Bottom Status & Diagnostics Dock
            Panel bottomDock = new Panel { Dock = DockStyle.Fill };
            Panel bottomHeader = new Panel { Dock = DockStyle.Top, Height = 28 };
            
            Label lblLogsTitle = new Label { Text = "  📋 LOGS & DIAGNOSTICS", Dock = DockStyle.Left, Width = 180, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            
            lblBottomStatus = new Label 
            { 
                Text = "⚡ Ready | 🔒 Vault: AES-256-GCM Active | 🐧 FreeRDP 3.33.0 (.NET 10)", 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleLeft, 
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.CornflowerBlue
            };

            btnPinBottom = new Button { Text = "📌", Dock = DockStyle.Right, Width = 30, FlatStyle = FlatStyle.Flat };
            btnPinBottom.FlatAppearance.BorderSize = 0;
            btnPinBottom.Click += (s, e) => TogglePinBottom();

            btnToggleBottom = new Button { Text = "▼ Retract", Dock = DockStyle.Right, Width = 80, FlatStyle = FlatStyle.Flat };
            btnToggleBottom.FlatAppearance.BorderSize = 0;
            btnToggleBottom.Click += (s, e) => ToggleCollapseBottom();

            Button btnClearLogs = new Button { Text = "🗑️ Clear", Dock = DockStyle.Right, Width = 70, FlatStyle = FlatStyle.Flat };
            btnClearLogs.FlatAppearance.BorderSize = 0;
            btnClearLogs.Click += (s, e) => txtLogs.Clear();

            Button btnAiDiagLogs = new Button { Text = "🤖 Diagnose with AI", Dock = DockStyle.Right, Width = 140, FlatStyle = FlatStyle.Flat };
            btnAiDiagLogs.FlatAppearance.BorderSize = 0;
            btnAiDiagLogs.Click += (s, e) => {
                ToggleAiCopilotTab();
                if (_aiTabPage?.Controls.Count > 0 && _aiTabPage.Controls[0] is AiAssistantPanel ai)
                {
                    ai.GetActiveLogsFunc = () => txtLogs.Text;
                }
            };

            bottomHeader.Controls.Add(lblBottomStatus);
            bottomHeader.Controls.Add(lblLogsTitle);
            bottomHeader.Controls.Add(btnAiDiagLogs);
            bottomHeader.Controls.Add(btnClearLogs);
            bottomHeader.Controls.Add(btnPinBottom);
            bottomHeader.Controls.Add(btnToggleBottom);

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

            bottomDock.Controls.Add(txtLogs);
            bottomDock.Controls.Add(bottomHeader);

            rightSplit.Panel1.Controls.Add(tabControl);
            rightSplit.Panel2.Controls.Add(bottomDock);

            mainSplit.Panel2.Controls.Add(rightSplit);

            this.Controls.Add(mainSplit);
            this.Controls.Add(ribbonPanel);

            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.F11) ToggleFullscreen();
            };

            ApplyTheme(this);
        }

        private Button CreateToolButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(2)
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
                Padding = new Padding(2, 6, 2, 0)
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // RETRACTABLE & PINNABLE DOCK HANDLERS
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
                mainSplit.SplitterDistance = 32; // Collapsed narrow rail
                btnCollapseLeft.Text = "▶";
            }
            else
            {
                mainSplit.SplitterDistance = _leftSavedWidth > 50 ? _leftSavedWidth : 240;
                btnCollapseLeft.Text = "◀";
            }
        }

        private void TogglePinBottom()
        {
            _isBottomPinned = !_isBottomPinned;
            btnPinBottom.Text = _isBottomPinned ? "📌" : "📍";
        }

        private void ToggleCollapseBottom()
        {
            _isBottomCollapsed = !_isBottomCollapsed;
            if (_isBottomCollapsed)
            {
                _bottomSavedHeight = rightSplit.Height - rightSplit.SplitterDistance;
                rightSplit.SplitterDistance = rightSplit.Height - 34; // Retracted status strip
                btnToggleBottom.Text = "▲ Expand";
            }
            else
            {
                int dist = rightSplit.Height - (_bottomSavedHeight > 60 ? _bottomSavedHeight : 180);
                if (dist > 50) rightSplit.SplitterDistance = dist;
                btnToggleBottom.Text = "▼ Retract";
            }
        }

        private void ToggleRibbonCompact()
        {
            _isRibbonCompact = !_isRibbonCompact;
            quickConnectRow.Visible = !_isRibbonCompact;
            ribbonPanel.Height = _isRibbonCompact ? 42 : 84;
        }

        // ════════════════════════════════════════════════════════════════════
        // LIVE INSTANT SEARCH FILTER
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
            lblSearchStats.Text = $"  Found {matchCount} matching server(s)";
        }

        // ════════════════════════════════════════════════════════════════════
        // ACTIVE SESSION ACTIONS (SCREENSHOT, CTRL+ALT+DEL, RECONNECT)
        // ════════════════════════════════════════════════════════════════════
        private void ReconnectActiveSession()
        {
            if (tabControl.SelectedTab?.Controls.Count > 0 && tabControl.SelectedTab.Controls[0] is FreeRdpContainer container)
            {
                container.Reconnect();
                LogSystem("Reconnected active session.");
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
                    
                    // Save to pictures directory as well
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FreeRDP_Screenshots");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string file = Path.Combine(dir, $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    bmp.Save(file, System.Drawing.Imaging.ImageFormat.Png);

                    LogSystem($"Screenshot captured and saved to: {file} (and copied to clipboard)");
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
        // REACHABILITY PROBE & QUICK ACTIONS
        // ════════════════════════════════════════════════════════════════════
        private async Task ProbeSelectedServer()
        {
            if (treeConnections.SelectedNode?.Tag is SavedConnection c)
            {
                lblBottomStatus.Text = $"⚡ Probing {c.Host} (Port 3389 & Ping)...";
                var result = await NetworkTester.ProbeHostAsync(c.Host);
                
                lblBottomStatus.Text = $"⚡ [{c.Name}] {result.SummaryText}";
                LogSystem($"[Probe] {c.Name} ({c.Host}): {result.SummaryText}");

                MessageBox.Show($"Reachability check for '{c.Name}' ({c.Host}):\r\n\r\n" +
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
                    LogSystem($"Password for {c.Name} copied to clipboard (auto-wipes in 30s).");

                    // Start a 30s timer to clear clipboard for security
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
                    LogSystem($"Duplicated connection: {clone.Name}");
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
                    LogSystem($"Imported RDP file: {ofd.FileName} -> {conn.Name}");
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
                        LogSystem($"Exported {c.Name} to {sfd.FileName}");
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
                    FileName = $"FreeRdp_Backup_{DateTime.Now:yyyyMMdd}.frdpvault"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _store.ExportVault(sfd.FileName, txtPass.Text, _connections);
                        LogSystem($"Exported encrypted vault backup to {sfd.FileName}");
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

        private void ImportVaultArchive()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "FreeRDP Vault (*.frdpvault)|*.frdpvault|All Files (*.*)|*.*",
                Title = "Open Encrypted Vault Backup"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                using var dlg = new Form
                {
                    Text = "Unlock Vault Backup",
                    Size = new Size(360, 180),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                Label lbl = new Label { Text = "Enter the password used when exporting this vault:", Location = new Point(20, 20), Width = 300, Height = 30 };
                TextBox txtPass = new TextBox { Location = new Point(20, 55), Width = 300, PasswordChar = '*' };
                Button btnImport = new Button { Text = "Unlock & Merge", Location = new Point(120, 100), Width = 110 };
                Button btnCancel = new Button { Text = "Cancel", Location = new Point(240, 100), Width = 80 };

                btnImport.Click += (s, e) => {
                    try
                    {
                        var imported = _store.ImportVault(ofd.FileName, txtPass.Text);
                        int count = 0;
                        foreach (var item in imported)
                        {
                            if (!_connections.Any(x => x.Id == item.Id))
                            {
                                _connections.Add(item);
                                count++;
                            }
                        }
                        _store.SaveConnections(_connections);
                        RefreshTree();
                        LogSystem($"Imported {count} new connections from vault archive.");
                        MessageBox.Show($"Successfully merged {count} connections from vault!", "Vault Imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        dlg.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Decryption failed: {ex.Message}\r\nPlease check your password.", "Decryption Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                btnCancel.Click += (s, e) => dlg.Close();
                dlg.Controls.AddRange(new Control[] { lbl, txtPass, btnImport, btnCancel });
                dlg.ShowDialog(this);
            }
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
                rightSplit.Panel2Collapsed = true;
                ribbonPanel.Visible = false;
                
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                tabControl.Padding = new Point(0, 0);
            }
            else
            {
                mainSplit.Panel1Collapsed = false;
                rightSplit.Panel2Collapsed = false;
                ribbonPanel.Visible = true;

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
                    bg = Color.WhiteSmoke;
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
                    bg = Color.FromArgb(30, 30, 30);
                    fg = Color.FromArgb(220, 220, 220);
                    panelBg = Color.FromArgb(45, 45, 48);
                    treeBg = Color.FromArgb(37, 37, 38);
                    activeTabBg = Color.FromArgb(45, 45, 48);
                    inactiveTabBg = Color.FromArgb(30, 30, 30);
                    logBg = Color.FromArgb(18, 18, 20);
                    break;
            }

            this.BackColor = bg;
            this.ForeColor = fg;
            ribbonPanel.BackColor = panelBg;
            
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
                if (node != null)
                {
                    treeConnections.SelectedNode = node;
                }
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
                LogSystem($"\n--- Starting Bulk Execution: {selectedScript.Name} ---");
                foreach (var conn in selectedConnections)
                {
                    LogSystem($"[{conn.Name}] Executing script...");
                    string result = await SshLauncher.ExecuteBackgroundCommandAsync(conn, selectedScript.Content);
                    LogSystem($"[{conn.Name}] Result:\n{result}");
                }
                LogSystem("--- Bulk Execution Complete ---\n");
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
                    activeBg = Color.FromArgb(45, 45, 48);
                    inactiveBg = Color.FromArgb(30, 30, 30);
                    textFg = Color.FromArgb(220, 220, 220);
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
            
            // Draw Text
            e.Graphics.DrawString(title, font, new SolidBrush(textFg), new PointF(tabRect.X + 6, tabRect.Y + 5));

            // Draw Close 'X'
            var closeRect = new Rectangle(tabRect.Right - 16, tabRect.Y + 6, 12, 12);
            e.Graphics.DrawString("×", new Font("Arial", 9, FontStyle.Bold), Brushes.IndianRed, closeRect);
            
            backBrush.Dispose();
        }

        private void TabControl_MouseDown(object? sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
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

        private void LogSystem(string msg)
        {
            if (txtLogs.IsHandleCreated && !txtLogs.Disposing)
            {
                txtLogs.BeginInvoke((MethodInvoker)delegate {
                    if (txtLogs.TextLength > 60000) txtLogs.Clear();
                    txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
                    txtLogs.SelectionStart = txtLogs.TextLength;
                    txtLogs.ScrollToCaret();
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // SESSION LAUNCHING & BINARY RESOLUTION
        // ════════════════════════════════════════════════════════════════════
        private void LaunchSession(SavedConnection config)
        {
            if (config.Protocol == ConnectionProtocol.SSH)
            {
                LogSystem($"[{config.Name}] Launching external SSH terminal...");
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
            
            Action<string> appendLog = (msg) => {
                if (txtLogs.IsHandleCreated && !txtLogs.Disposing)
                {
                    txtLogs.BeginInvoke((MethodInvoker)delegate {
                        if (txtLogs.TextLength > 60000) txtLogs.Clear();
                        txtLogs.AppendText($"[{config.Name}] {msg}\r\n");
                        txtLogs.SelectionStart = txtLogs.TextLength;
                        txtLogs.ScrollToCaret();
                    });
                }
            };

            container.OnLogMessage += appendLog;

            container.OnDisconnected += () => {
                if (tabControl.IsHandleCreated && !tabControl.Disposing)
                {
                    tabControl.Invoke((MethodInvoker)delegate {
                        appendLog("Session Disconnected.");
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
                appendLog($"Attempting to launch {possiblePath}");
                container.LaunchSession(config, possiblePath);
            }
            catch (Exception ex)
            {
                appendLog($"Failed to launch {possiblePath}: {ex.Message}");
            }
        }
    }
}
