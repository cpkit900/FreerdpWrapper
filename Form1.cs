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
        private ConnectionStore _store;
        private List<SavedConnection> _connections;

        // UI Elements
        private SplitContainer mainSplit; // Left: Tree, Right: Content
        private TreeView treeConnections;
        private ContextMenuStrip treeContextMenu;
        
        // Right side
        private SplitContainer rightSplit; // Top: Tabs, Bottom: Logs
        private TabControl tabControl;
        private TextBox txtLogs;

        public Form1()
        {
            InitializeComponent();
            _store = new ConnectionStore();
            _connections = _store.LoadConnections();
            
            SetupUI();
            RefreshTree();
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
                SplitterDistance = 250 // Width of left panel
            };

            // Setup Tree
            treeConnections = new TreeView 
            { 
                Dock = DockStyle.Fill,
                FullRowSelect = true,
                HideSelection = false
            };
            treeConnections.NodeMouseDoubleClick += TreeConnections_NodeMouseDoubleClick;
            treeConnections.MouseUp += TreeConnections_MouseUp;

            // Context Menu for Tree
            treeContextMenu = new ContextMenuStrip();
            treeContextMenu.Items.Add("Add Connection", null, (s, e) => AddConnection());
            treeContextMenu.Items.Add("Edit Connection", null, (s, e) => EditConnection());
            treeContextMenu.Items.Add("Delete Connection", null, (s, e) => DeleteConnection());
            
            Panel leftPanel = new Panel { Dock = DockStyle.Fill };
            Label lblServers = new Label { Text = "Saved Connections", Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.MiddleLeft, Font = new Font(this.Font, FontStyle.Bold) };
            Button btnAdd = new Button { Text = "+ Add", Dock = DockStyle.Bottom, Height = 30 };
            btnAdd.Click += (s, e) => AddConnection();

            leftPanel.Controls.Add(treeConnections);
            leftPanel.Controls.Add(lblServers);
            leftPanel.Controls.Add(btnAdd);

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

            txtLogs = new TextBox 
            { 
                Dock = DockStyle.Fill, 
                Multiline = true, 
                ScrollBars = ScrollBars.Vertical, 
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 9f)
            };

            rightSplit.Panel1.Controls.Add(tabControl);
            rightSplit.Panel2.Controls.Add(txtLogs);

            mainSplit.Panel1.Controls.Add(leftPanel);
            mainSplit.Panel2.Controls.Add(rightSplit);

            this.Controls.Add(mainSplit);
        }

        private void RefreshTree()
        {
            treeConnections.Nodes.Clear();
            var dict = new Dictionary<string, TreeNode>();

            foreach (var c in _connections)
            {
                if (!dict.ContainsKey(c.Group))
                {
                    var groupNode = new TreeNode(c.Group);
                    groupNode.ImageIndex = 0; // if you had images
                    dict[c.Group] = groupNode;
                    treeConnections.Nodes.Add(groupNode);
                }

                var node = new TreeNode(c.Name) { Tag = c };
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
            Brush backBrush = isSelected ? SystemBrushes.Window : SystemBrushes.ControlLight;
            e.Graphics.FillRectangle(backBrush, tabRect);

            if (isSelected)
            {
                e.Graphics.DrawLine(SystemPens.ControlDark, tabRect.Left, tabRect.Top, tabRect.Right, tabRect.Top);
            }

            string title = tabPage.Text;
            Font font = e.Font ?? tabControl.Font;
            
            // Draw Text
            e.Graphics.DrawString(title, font, SystemBrushes.ControlText, new PointF(tabRect.X + 5, tabRect.Y + 4));

            // Draw 'X'
            var closeRect = new Rectangle(tabRect.Right - 15, tabRect.Y + 6, 10, 10);
            e.Graphics.DrawString("X", new Font("Arial", 8, FontStyle.Bold), Brushes.Red, closeRect);
            
            e.DrawFocusRectangle();
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
            string host = config.Host;
            string exeName = config.UseSdl ? "sdl3-freerdp.exe" : "wfreerdp.exe";
            string relativeBuildPath = Path.Combine("Compiled", "bin", exeName);
            
            string possiblePath = exeName;
            string? currentDir = AppDomain.CurrentDomain.BaseDirectory;
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

            var tabPage = new TabPage(config.Name);
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
