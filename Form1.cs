using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.UI;
using FreeRdpWrapper.Services;
using System.Collections.Generic;

namespace FreeRdpWrapper
{
    public partial class Form1 : Form
    {
        private ConnectionStore _store = default!;
        private List<SavedConnection> _connections = new();
        private ScriptStore _scriptStore = default!;
        private List<SavedScript> _scripts = new();

        // UI Elements
        private SplitContainer mainSplit = default!; // Left: Tree, Right: Content
        private SplitContainer leftSplit = default!; // Top: Connections, Bottom: Scripts
        private TreeView treeConnections = default!;
        private TreeView treeScripts = default!;
        private ContextMenuStrip treeContextMenu = default!;
        private ContextMenuStrip scriptContextMenu = default!;
        
        // Right side
        private SplitContainer rightSplit = default!; // Top: Tabs, Bottom: Logs
        private TabControl tabControl = default!;
        private TextBox txtLogs = default!;

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
            this.Text = "FreeRDP Manager (.NET 8 WinForms)";
            this.Width = 1200;
            this.Height = 800;

            // Main Split
            mainSplit = new SplitContainer 
            { 
                Dock = DockStyle.Fill, 
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel1,
                SplitterDistance = 200 // Width of left panel
            };

            // Setup Tree
            treeConnections = new TreeView 
            { 
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                HideSelection = false,
                ShowLines = false,
                ItemHeight = 24,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.None,
                CheckBoxes = true
            };
            treeConnections.AfterCheck += (s, e) => {
                if (e.Action != TreeViewAction.Unknown)
                {
                    foreach (TreeNode child in e.Node.Nodes)
                        child.Checked = e.Node.Checked;
                }
            };
            treeConnections.NodeMouseDoubleClick += TreeConnections_NodeMouseDoubleClick;
            treeConnections.MouseUp += TreeConnections_MouseUp;

            // Context Menu for Tree
            treeContextMenu = new ContextMenuStrip();
            treeContextMenu.Items.Add("Add Connection", null, (s, e) => AddConnection());
            treeContextMenu.Items.Add("Edit Connection", null, (s, e) => EditConnection());
            treeContextMenu.Items.Add("Delete Connection", null, (s, e) => DeleteConnection());
            treeContextMenu.Items.Add("-");
            treeContextMenu.Items.Add("Run Script on Selected...", null, (s, e) => RunScriptOnSelected());
            
            // Setup Scripts Tree
            treeScripts = new TreeView 
            { 
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                HideSelection = false,
                ShowLines = false,
                ItemHeight = 24,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.None
            };
            treeScripts.NodeMouseDoubleClick += TreeScripts_NodeMouseDoubleClick;
            treeScripts.MouseUp += TreeScripts_MouseUp;

            // Context Menu for Scripts
            scriptContextMenu = new ContextMenuStrip();
            scriptContextMenu.Items.Add("Add Script", null, (s, e) => AddScript());
            scriptContextMenu.Items.Add("Edit Script", null, (s, e) => EditScript());
            scriptContextMenu.Items.Add("Delete Script", null, (s, e) => DeleteScript());

            leftSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = this.Height / 2,
                SplitterWidth = 5
            };

            Panel pnlConn = new Panel { Dock = DockStyle.Fill };
            Label lblServers = new Label { Text = "  🌍 Saved Connections", Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold) };
            Button btnAddConn = new Button { Text = "➕ Add Connection", Dock = DockStyle.Bottom, Height = 35, FlatStyle = FlatStyle.Flat };
            btnAddConn.FlatAppearance.BorderSize = 0;
            btnAddConn.Click += (s, e) => AddConnection();
            pnlConn.Controls.Add(treeConnections);
            pnlConn.Controls.Add(lblServers);
            pnlConn.Controls.Add(btnAddConn);

            Panel pnlScripts = new Panel { Dock = DockStyle.Fill };
            Label lblScriptsTitle = new Label { Text = "  📜 Saved Scripts", Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold) };
            Button btnAddScript = new Button { Text = "➕ Add Script", Dock = DockStyle.Bottom, Height = 35, FlatStyle = FlatStyle.Flat };
            btnAddScript.FlatAppearance.BorderSize = 0;
            btnAddScript.Click += (s, e) => AddScript();
            pnlScripts.Controls.Add(treeScripts);
            pnlScripts.Controls.Add(lblScriptsTitle);
            pnlScripts.Controls.Add(btnAddScript);

            leftSplit.Panel1.Controls.Add(pnlConn);
            leftSplit.Panel2.Controls.Add(pnlScripts);
            
            Panel leftPanel = new Panel { Dock = DockStyle.Fill };
            leftPanel.Controls.Add(leftSplit);

            // Right Split
            rightSplit = new SplitContainer 
            { 
                Dock = DockStyle.Fill, 
                Orientation = Orientation.Horizontal,
                SplitterDistance = this.Height - 200
            };

            tabControl = new TabControl 
            { 
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                Padding = new Point(15, 4) // extra space for X
            };
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.MouseDown += TabControl_MouseDown;

            Panel logPanel = new Panel { Dock = DockStyle.Fill };
            Panel logHeader = new Panel { Dock = DockStyle.Top, Height = 30 };
            Label lblLogs = new Label { Text = "  Session Logs", Dock = DockStyle.Left, Width = 150, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold) };
            Button btnToggleLogs = new Button { Text = "▼ Minimize", Dock = DockStyle.Right, Width = 80, FlatStyle = FlatStyle.Flat };
            btnToggleLogs.FlatAppearance.BorderSize = 0;
            btnToggleLogs.Click += (s, e) => {
                if (btnToggleLogs.Text.Contains("▼"))
                {
                    rightSplit.SplitterDistance = rightSplit.Height - logHeader.Height - 5;
                    btnToggleLogs.Text = "▲ Maximize";
                }
                else
                {
                    rightSplit.SplitterDistance = rightSplit.Height - 200;
                    btnToggleLogs.Text = "▼ Minimize";
                }
            };

            Button btnClearLogs = new Button { Text = "🗑️ Clear Logs", Dock = DockStyle.Right, Width = 110, FlatStyle = FlatStyle.Flat };
            btnClearLogs.FlatAppearance.BorderSize = 0;
            btnClearLogs.Click += (s, e) => txtLogs.Clear();

            logHeader.Controls.Add(btnClearLogs);
            logHeader.Controls.Add(btnToggleLogs);
            logHeader.Controls.Add(lblLogs);
            
            txtLogs = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                Multiline = true, 
                ScrollBars = ScrollBars.Vertical, 
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.None
            };
            
            logPanel.Controls.Add(txtLogs);
            logPanel.Controls.Add(logHeader);

            rightSplit.Panel1.Controls.Add(tabControl);
            rightSplit.Panel2.Controls.Add(logPanel);

            mainSplit.Panel1.Controls.Add(leftPanel);
            mainSplit.Panel2.Controls.Add(rightSplit);

            // Quick Connect Panel
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
            
            TextBox txtQHost = new TextBox { Location = new Point(10, 10), Width = 150, PlaceholderText = "Host / IP", BorderStyle = BorderStyle.FixedSingle };
            TextBox txtQUser = new TextBox { Location = new Point(170, 10), Width = 120, PlaceholderText = "Username", BorderStyle = BorderStyle.FixedSingle };
            TextBox txtQPass = new TextBox { Location = new Point(300, 10), Width = 120, PlaceholderText = "Password", PasswordChar = '*', BorderStyle = BorderStyle.FixedSingle };
            Button btnQConnect = new Button { Text = "⚡ Quick Connect", Location = new Point(430, 8), Width = 130, FlatStyle = FlatStyle.Flat, BackColor = Color.DodgerBlue, ForeColor = Color.White };
            btnQConnect.FlatAppearance.BorderSize = 0;
            btnQConnect.Click += (s, e) => {
                if (!string.IsNullOrWhiteSpace(txtQHost.Text))
                {
                    LaunchSession(new SavedConnection { 
                        Name = "Quick: " + txtQHost.Text, 
                        Host = txtQHost.Text, 
                        User = txtQUser.Text, 
                        Pass = txtQPass.Text 
                    });
                }
            };
            
            Button btnToggleTheme = new Button { Text = "🌗 Theme", Location = new Point(570, 8), Width = 90, FlatStyle = FlatStyle.Flat, BackColor = Color.Gray, ForeColor = Color.White };
            btnToggleTheme.FlatAppearance.BorderSize = 0;
            btnToggleTheme.Click += (s, e) => {
                var themes = (AppTheme[])Enum.GetValues(typeof(AppTheme));
                int nextIndex = ((int)ThemeSettings.CurrentTheme + 1) % themes.Length;
                ThemeSettings.CurrentTheme = themes[nextIndex];
                ApplyTheme(this);
            };

            Button btnFullscreen = new Button { Text = "⛶ Fullscreen (F11)", Location = new Point(670, 8), Width = 140, FlatStyle = FlatStyle.Flat, BackColor = Color.DarkSlateGray, ForeColor = Color.White };
            btnFullscreen.FlatAppearance.BorderSize = 0;
            btnFullscreen.Click += (s, e) => ToggleFullscreen();

            topPanel.Controls.Add(txtQHost);
            topPanel.Controls.Add(txtQUser);
            topPanel.Controls.Add(txtQPass);
            topPanel.Controls.Add(btnQConnect);
            topPanel.Controls.Add(btnToggleTheme);
            topPanel.Controls.Add(btnFullscreen);

            this.Controls.Add(mainSplit);
            this.Controls.Add(topPanel);

            this.KeyPreview = true;
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.F11) ToggleFullscreen();
            };

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
                mainSplit.Panel1Collapsed = true; // Hide Tree
                rightSplit.Panel2Collapsed = true; // Hide Logs
                foreach (Control c in this.Controls)
                {
                    if (c is Panel p && p.Dock == DockStyle.Top) p.Visible = false; // Hide Top Panel
                }
                
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                tabControl.Padding = new Point(0, 0); // Minimize tab overhead
            }
            else
            {
                mainSplit.Panel1Collapsed = false;
                rightSplit.Panel2Collapsed = false;
                foreach (Control c in this.Controls)
                {
                    if (c is Panel p && p.Dock == DockStyle.Top) p.Visible = true;
                }

                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                tabControl.Padding = new Point(15, 4);
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
                    panelBg = Color.LightGray;
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
                    fg = Color.Lime; // Classic terminal look
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
                    logBg = Color.Black;
                    break;
            }

            this.BackColor = bg;
            this.ForeColor = fg;
            
            treeConnections.BackColor = treeBg;
            treeConnections.ForeColor = fg;

            treeScripts.BackColor = treeBg;
            treeScripts.ForeColor = fg;
            
            txtLogs.BackColor = logBg;
            txtLogs.ForeColor = fg;

            foreach (Control c in this.Controls)
            {
                if (c is Panel p && p.Dock == DockStyle.Top)
                {
                    p.BackColor = panelBg;
                    foreach (Control child in p.Controls)
                    {
                        if (child is TextBox tb)
                        {
                            tb.BackColor = logBg;
                            tb.ForeColor = fg;
                        }
                    }
                }
            }
            
            // Paint Log Header
            if (rightSplit.Panel2.Controls.Count > 0 && rightSplit.Panel2.Controls[0] is Panel lpnl)
            {
                foreach (Control c in lpnl.Controls)
                {
                    if (c is Panel headerPanel)
                    {
                        headerPanel.BackColor = panelBg;
                        headerPanel.ForeColor = fg;
                    }
                }
            }

            // Update Add buttons in Tree panel
            if (mainSplit.Panel1.Controls.Count > 0 && mainSplit.Panel1.Controls[0] is Panel leftPnl)
            {
                if (leftPnl.Controls.Count > 0 && leftPnl.Controls[0] is SplitContainer lspl)
                {
                    foreach (var panel in new[] { lspl.Panel1, lspl.Panel2 })
                    {
                        if (panel.Controls.Count > 0 && panel.Controls[0] is Panel containerPanel)
                        {
                            foreach (Control c in containerPanel.Controls)
                            {
                                if (c is Button b && b.Text.Contains("Add"))
                                {
                                    b.BackColor = panelBg;
                                }
                            }
                        }
                    }
                }
            }

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

                var node = new TreeNode($"🖥️ {c.Name}") { Tag = c };
                dict[c.Group].Nodes.Add(node);
            }

            treeConnections.ExpandAll();
        }

        private void TreeConnections_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TreeNode node = treeConnections.GetNodeAt(e.X, e.Y);
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
                if (MessageBox.Show($"Delete {c.Name}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
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
                TreeNode node = treeScripts.GetNodeAt(e.X, e.Y);
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
                if (MessageBox.Show($"Delete script '{sc.Name}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
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

            var store = new ScriptStore();
            var scripts = store.LoadScripts();
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
                Action<string> appendLog = (msg) => {
                    if (txtLogs.IsHandleCreated && !txtLogs.Disposing)
                    {
                        txtLogs.BeginInvoke((MethodInvoker)delegate {
                            if (txtLogs.TextLength > 50000) txtLogs.Clear();
                            txtLogs.AppendText($"{msg}\r\n");
                            txtLogs.SelectionStart = txtLogs.TextLength;
                            txtLogs.ScrollToCaret();
                        });
                    }
                };

                appendLog($"\n--- Starting Bulk Execution: {selectedScript.Name} ---");
                foreach (var conn in selectedConnections)
                {
                    appendLog($"[{conn.Name}] Executing script...");
                    string result = await SshLauncher.ExecuteBackgroundCommandAsync(conn, selectedScript.Content);
                    appendLog($"[{conn.Name}] Result:\n{result}");
                }
                appendLog("--- Bulk Execution Complete ---\n");
            }
        }

        private void TreeConnections_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is SavedConnection c)
            {
                LaunchSession(c);
            }
        }

        // --- Custom Tab Drawing for 'X' button ---
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
            e.Graphics.DrawString(title, font, new SolidBrush(textFg), new PointF(tabRect.X + 5, tabRect.Y + 4));

            // Draw 'X'
            var closeRect = new Rectangle(tabRect.Right - 15, tabRect.Y + 6, 10, 10);
            e.Graphics.DrawString("X", new Font("Arial", 8, FontStyle.Bold), Brushes.IndianRed, closeRect);
            
            backBrush.Dispose();
        }

        private void TabControl_MouseDown(object? sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                Rectangle r = tabControl.GetTabRect(i);
                Rectangle closeRect = new Rectangle(r.Right - 15, r.Y + 4, 10, 10);
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
                // Disconnecting kills the process
                container.Disconnect();
                container.Dispose();
            }
            tabControl.TabPages.Remove(tabPage);
            tabPage.Dispose();
        }
        
        // --- Session Launching ---
        private void LaunchSession(SavedConnection config)
        {
            if (config.Protocol == ConnectionProtocol.SSH)
            {
                if (txtLogs.IsHandleCreated && !txtLogs.Disposing)
                {
                    txtLogs.BeginInvoke((MethodInvoker)delegate {
                        if (txtLogs.TextLength > 50000) txtLogs.Clear();
                        txtLogs.AppendText($"[{config.Name}] Launching external SSH terminal...\r\n");
                        txtLogs.SelectionStart = txtLogs.TextLength;
                        txtLogs.ScrollToCaret();
                    });
                }
                SshLauncher.LaunchInteractiveSession(config);
                return;
            }

            string host = config.Host;
            string exeName = config.UseSdl ? "sdl3-freerdp.exe" : "wfreerdp.exe";
            string relativeBuildPath = Path.Combine("Compiled", "bin", exeName);
            
            string possiblePath = exeName;
            string? currentDir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName) ?? AppDomain.CurrentDomain.BaseDirectory;
            
            // Fast-path: Check if the binary is exactly next to the executable (which it is in the Zipped Release)
            if (File.Exists(Path.Combine(currentDir, "Bin", "FreeRDP", exeName)))
            {
                possiblePath = Path.Combine(currentDir, "Bin", "FreeRDP", exeName);
            }
            else
            {
                // Recursive search outward for development runs
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
            
            // Log hook
            Action<string> appendLog = (msg) => {
                if (txtLogs.IsHandleCreated && !txtLogs.Disposing)
                {
                    txtLogs.BeginInvoke((MethodInvoker)delegate {
                        if (txtLogs.TextLength > 50000) txtLogs.Clear();
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
