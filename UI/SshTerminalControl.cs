using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace FreeRdpWrapper.UI
{
    public class SshTerminalControl : UserControl
    {
        private SavedConnection? _connection;
        private SshClient? _client;
        private ShellStream? _stream;
        private CancellationTokenSource? _cts;

        private Panel _topBar = null!;
        private Label _lblTitle = null!;
        private Label _lblStatus = null!;
        private Button _btnCopy = null!;
        private Button _btnPaste = null!;
        private Button _btnClear = null!;
        private Button _btnReconnect = null!;
        private Button _btnExternal = null!;
        private Button _btnDisconnect = null!;

        private RichTextBox _txtTerminal = null!;

        public event Action<string>? OnLogMessage;
        public event Action? OnDisconnected;

        public bool IsConnected => _client != null && _client.IsConnected;

        public SshTerminalControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(12, 12, 12);
            this.Font = new Font("Segoe UI", 9f);

            // ── Top Bar ──────────────────────────────────────────────────────────
            _topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(28, 28, 30),
                Padding = new Padding(8, 3, 8, 3)
            };

            _lblTitle = new Label
            {
                Text = "🐧 SSH Terminal",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(8, 7)
            };
            _topBar.Controls.Add(_lblTitle);

            _lblStatus = new Label
            {
                Text = "🟡 Initializing",
                AutoSize = true,
                ForeColor = Color.FromArgb(240, 200, 80),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Location = new Point(220, 8)
            };
            _topBar.Controls.Add(_lblStatus);

            var pnlButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            _btnCopy = CreateTopButton("📋 Copy", (s, e) => CopySelection());
            _btnPaste = CreateTopButton("📋 Paste", (s, e) => PasteClipboard());
            _btnClear = CreateTopButton("⚡ Clear", (s, e) => ClearTerminal());
            _btnReconnect = CreateTopButton("🔄 Reconnect", (s, e) => Reconnect());
            _btnExternal = CreateTopButton("↗️ External", (s, e) => LaunchExternal());
            _btnDisconnect = CreateTopButton("❌ Disconnect", (s, e) => Disconnect());

            pnlButtons.Controls.AddRange(new Control[] { _btnCopy, _btnPaste, _btnClear, _btnReconnect, _btnExternal, _btnDisconnect });
            _topBar.Controls.Add(pnlButtons);

            // ── Terminal Output Control ──────────────────────────────────────────
            _txtTerminal = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 12),
                ForeColor = Color.FromArgb(212, 212, 212),
                Font = GetMonospaceFont(),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ShortcutsEnabled = false,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };

            _txtTerminal.KeyDown += TxtTerminal_KeyDown;
            _txtTerminal.KeyPress += TxtTerminal_KeyPress;

            // Context menu
            var cm = new ContextMenuStrip();
            cm.Items.Add("📋 Copy (Ctrl+C)", null, (s, e) => CopySelection());
            cm.Items.Add("📋 Paste (Ctrl+V)", null, (s, e) => PasteClipboard());
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add("⚡ Clear Screen (Ctrl+L)", null, (s, e) => ClearTerminal());
            cm.Items.Add("🛑 Send SIGINT (Ctrl+C)", null, (s, e) => SendToShell("\x03"));
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add("🔄 Reconnect", null, (s, e) => Reconnect());
            cm.Items.Add("↗️ Open in External Console", null, (s, e) => LaunchExternal());
            _txtTerminal.ContextMenuStrip = cm;

            this.Controls.Add(_txtTerminal);
            this.Controls.Add(_topBar);

            this.Resize += (s, e) => UpdatePtySize();

            this.ResumeLayout(false);
        }

        private static Font GetMonospaceFont()
        {
            string[] preferred = { "Cascadia Mono", "Cascadia Code", "Consolas", "Courier New" };
            foreach (var name in preferred)
            {
                using var test = new Font(name, 10f);
                if (test.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return new Font(name, 10f, FontStyle.Regular);
            }
            return new Font(FontFamily.GenericMonospace, 10f, FontStyle.Regular);
        }

        private static Button CreateTopButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(45, 45, 48),
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 1, 3, 1)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(62, 62, 66);
            btn.Click += onClick;
            return btn;
        }

        public void FocusTerminal()
        {
            if (_txtTerminal.IsHandleCreated && !_txtTerminal.IsDisposed)
            {
                _txtTerminal.Focus();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // CONNECTION LIFECYCLE
        // ════════════════════════════════════════════════════════════════════

        public void Connect(SavedConnection connection)
        {
            _connection = connection;
            _lblTitle.Text = $"🐧 {connection.Name} ({connection.User}@{connection.Host})";
            _lblStatus.Text = "🟡 Connecting...";
            _lblStatus.ForeColor = Color.FromArgb(240, 200, 80);

            ClearTerminal();
            AppendTerminalLine($"[AeroRemote SSH] Connecting to {connection.User}@{connection.Host} ...", Color.FromArgb(100, 180, 255));

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            Task.Run(() => ConnectBackground(_cts.Token));
        }

        private void ConnectBackground(CancellationToken ct)
        {
            if (_connection == null) return;

            try
            {
                ConnectionInfo connInfo = SshLauncher.BuildConnectionInfo(_connection);

                _client = new SshClient(connInfo);
                _client.KeepAliveInterval = TimeSpan.FromSeconds(30);
                _client.Connect();

                if (!_client.IsConnected)
                {
                    throw new Exception("SSH handshake completed but client is not connected.");
                }

                // Determine terminal dimensions
                uint cols = 120;
                uint rows = 35;
                if (this.IsHandleCreated)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        cols = (uint)Math.Max(40, _txtTerminal.ClientSize.Width / 8);
                        rows = (uint)Math.Max(10, _txtTerminal.ClientSize.Height / 18);
                    });
                }

                var terminalModes = new Dictionary<TerminalModes, uint>
                {
                    { TerminalModes.ECHO, 1 }
                };

                _stream = _client.CreateShellStream("xterm-256color", cols, rows, (uint)_txtTerminal.ClientSize.Width, (uint)_txtTerminal.ClientSize.Height, 4096, terminalModes);

                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        _lblStatus.Text = "🟢 Connected";
                        _lblStatus.ForeColor = Color.FromArgb(80, 220, 100);
                        AppendTerminalLine($"[Connected to {_connection.Host}]", Color.FromArgb(80, 220, 100));
                        FocusTerminal();
                    });
                }

                OnLogMessage?.Invoke($"SSH session established to {_connection.Host}:{connInfo.Port}");

                // Start background read loop
                ReadStreamLoop(ct);
            }
            catch (Exception ex)
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        _lblStatus.Text = "🔴 Failed";
                        _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
                        AppendTerminalLine($"\r\n[SSH Connection Error]: {ex.Message}", Color.FromArgb(255, 90, 90));
                        AppendTerminalLine("[Tip: Check Host, Port, Username, and Password in Connection Properties]", Color.FromArgb(200, 200, 200));
                    });
                }
                OnLogMessage?.Invoke($"SSH connection failed: {ex.Message}");
            }
        }

        private void ReadStreamLoop(CancellationToken ct)
        {
            if (_stream == null) return;

            var buffer = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested && _stream.CanRead && _client != null && _client.IsConnected)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0) break;

                    string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            AppendAnsiText(text);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                {
                    OnLogMessage?.Invoke($"SSH Stream error: {ex.Message}");
                }
            }
            finally
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        _lblStatus.Text = "🔴 Disconnected";
                        _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
                        AppendTerminalLine("\r\n[SSH Session Disconnected]", Color.FromArgb(255, 120, 120));
                        OnDisconnected?.Invoke();
                    });
                }
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();

            try
            {
                _stream?.Dispose();
                _stream = null;
            }
            catch { }

            try
            {
                if (_client != null && _client.IsConnected)
                {
                    _client.Disconnect();
                }
                _client?.Dispose();
                _client = null;
            }
            catch { }

            if (this.IsHandleCreated && !this.IsDisposed)
            {
                _lblStatus.Text = "🔴 Disconnected";
                _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
            }
        }

        public void Reconnect()
        {
            Disconnect();
            if (_connection != null)
            {
                Connect(_connection);
            }
        }

        private void LaunchExternal()
        {
            if (_connection != null)
            {
                SshLauncher.LaunchInteractiveSession(_connection);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // TERMINAL RENDERING & ANSI ESCAPE HANDLING
        // ════════════════════════════════════════════════════════════════════

        private Color _currentForeColor = Color.FromArgb(212, 212, 212);
        private static readonly Regex AnsiRegex = new Regex(
            @"\x1B\[[\?><=]?[0-9;]*[@-~]|\x1B\][^\x07\x1B]*(?:\x07|\x1B\\)|\x1B[=>NOPn-o]|\x1B[()][A-Z0-9]",
            RegexOptions.Compiled);

        private void AppendAnsiText(string raw)
        {
            // Truncate terminal buffer if it gets too large (> 50,000 lines) to maintain fast rendering
            if (_txtTerminal.TextLength > 200_000)
            {
                _txtTerminal.Select(0, 50_000);
                _txtTerminal.SelectedText = "";
            }

            int lastIndex = 0;
            var matches = AnsiRegex.Matches(raw);

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    string segment = raw.Substring(lastIndex, match.Index - lastIndex);
                    AppendPlainText(segment);
                }

                // Process ANSI escape command
                ProcessAnsiCommand(match.Value);
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < raw.Length)
            {
                string remaining = raw.Substring(lastIndex);
                AppendPlainText(remaining);
            }

            _txtTerminal.SelectionStart = _txtTerminal.TextLength;
            _txtTerminal.ScrollToCaret();
        }

        private void AppendPlainText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            // Handle backspace \b in streaming output
            if (text.Contains("\b"))
            {
                foreach (char c in text)
                {
                    if (c == '\b')
                    {
                        if (_txtTerminal.TextLength > 0)
                        {
                            _txtTerminal.Select(_txtTerminal.TextLength - 1, 1);
                            _txtTerminal.SelectedText = "";
                        }
                    }
                    else
                    {
                        _txtTerminal.SelectionStart = _txtTerminal.TextLength;
                        _txtTerminal.SelectionColor = _currentForeColor;
                        _txtTerminal.AppendText(c.ToString());
                    }
                }
                return;
            }

            _txtTerminal.SelectionStart = _txtTerminal.TextLength;
            _txtTerminal.SelectionColor = _currentForeColor;
            _txtTerminal.AppendText(text);
        }

        private void ProcessAnsiCommand(string code)
        {
            // Clear screen \x1b[2J or \x1b[H\x1b[2J
            if (code.Equals("\x1B[2J", StringComparison.OrdinalIgnoreCase) || code.Equals("\x1B[H", StringComparison.OrdinalIgnoreCase))
            {
                _txtTerminal.Clear();
                return;
            }

            // SGR Color codes \x1b[...m
            if (code.EndsWith("m", StringComparison.OrdinalIgnoreCase) && code.StartsWith("\x1B["))
            {
                string sgr = code.Substring(2, code.Length - 3);
                if (string.IsNullOrEmpty(sgr) || sgr == "0")
                {
                    _currentForeColor = Color.FromArgb(212, 212, 212);
                    return;
                }

                var parts = sgr.Split(';');
                foreach (var part in parts)
                {
                    if (int.TryParse(part, out int n))
                    {
                        switch (n)
                        {
                            case 0: _currentForeColor = Color.FromArgb(212, 212, 212); break;
                            case 1: break; // Bold
                            case 30: _currentForeColor = Color.FromArgb(90, 90, 90); break;
                            case 31: _currentForeColor = Color.FromArgb(235, 87, 87); break;
                            case 32: _currentForeColor = Color.FromArgb(39, 201, 63); break;
                            case 33: _currentForeColor = Color.FromArgb(240, 200, 80); break;
                            case 34: _currentForeColor = Color.FromArgb(80, 160, 255); break;
                            case 35: _currentForeColor = Color.FromArgb(215, 90, 255); break;
                            case 36: _currentForeColor = Color.FromArgb(80, 220, 240); break;
                            case 37: _currentForeColor = Color.FromArgb(240, 240, 240); break;
                            case 90: _currentForeColor = Color.FromArgb(120, 120, 120); break;
                            case 91: _currentForeColor = Color.FromArgb(255, 110, 110); break;
                            case 92: _currentForeColor = Color.FromArgb(80, 240, 120); break;
                            case 93: _currentForeColor = Color.FromArgb(255, 230, 100); break;
                            case 94: _currentForeColor = Color.FromArgb(120, 190, 255); break;
                            case 95: _currentForeColor = Color.FromArgb(235, 130, 255); break;
                            case 96: _currentForeColor = Color.FromArgb(110, 245, 255); break;
                            case 97: _currentForeColor = Color.White; break;
                        }
                    }
                }
            }
        }

        private void AppendTerminalLine(string line, Color color)
        {
            _txtTerminal.SelectionStart = _txtTerminal.TextLength;
            _txtTerminal.SelectionColor = color;
            _txtTerminal.AppendText(line + "\r\n");
            _txtTerminal.SelectionStart = _txtTerminal.TextLength;
            _txtTerminal.ScrollToCaret();
        }

        public void ClearTerminal()
        {
            _txtTerminal.Clear();
            _currentForeColor = Color.FromArgb(212, 212, 212);
        }

        // ════════════════════════════════════════════════════════════════════
        // USER INPUT HANDLING
        // ════════════════════════════════════════════════════════════════════

        private void TxtTerminal_KeyDown(object? sender, KeyEventArgs e)
        {
            if (_stream == null || !_stream.CanWrite) return;

            // Handle standard shortcuts
            if (e.Control && e.KeyCode == Keys.C)
            {
                if (_txtTerminal.SelectionLength > 0)
                {
                    CopySelection();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
                else
                {
                    SendToShell("\x03"); // SIGINT
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteClipboard();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.L)
            {
                ClearTerminal();
                SendToShell("\x0C"); // Form feed / clear
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.KeyCode == Keys.D) { SendToShell("\x04"); e.Handled = true; e.SuppressKeyPress = true; return; }
            if (e.Control && e.KeyCode == Keys.Z) { SendToShell("\x1A"); e.Handled = true; e.SuppressKeyPress = true; return; }
            if (e.Control && e.KeyCode == Keys.A) { SendToShell("\x01"); e.Handled = true; e.SuppressKeyPress = true; return; }
            if (e.Control && e.KeyCode == Keys.E) { SendToShell("\x05"); e.Handled = true; e.SuppressKeyPress = true; return; }
            if (e.Control && e.KeyCode == Keys.U) { SendToShell("\x15"); e.Handled = true; e.SuppressKeyPress = true; return; }
            if (e.Control && e.KeyCode == Keys.K) { SendToShell("\x0B"); e.Handled = true; e.SuppressKeyPress = true; return; }
            if (e.Control && e.KeyCode == Keys.W) { SendToShell("\x17"); e.Handled = true; e.SuppressKeyPress = true; return; }

            // Special keys
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    SendToShell("\r");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Back:
                    SendToShell("\x7F");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Tab:
                    SendToShell("\t");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Escape:
                    SendToShell("\x1B");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Up:
                    SendToShell("\x1B[A");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Down:
                    SendToShell("\x1B[B");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Right:
                    SendToShell("\x1B[C");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Left:
                    SendToShell("\x1B[D");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Home:
                    SendToShell("\x1B[H");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.End:
                    SendToShell("\x1B[F");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.PageUp:
                    SendToShell("\x1B[5~");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.PageDown:
                    SendToShell("\x1B[6~");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Insert:
                    SendToShell("\x1B[2~");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Delete:
                    SendToShell("\x1B[3~");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void TxtTerminal_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (_stream == null || !_stream.CanWrite) return;

            // Send printable ASCII / UTF-8 chars
            if (e.KeyChar >= 32 && e.KeyChar != 127)
            {
                SendToShell(e.KeyChar.ToString());
                e.Handled = true;
            }
        }

        private void SendToShell(string input)
        {
            if (_stream != null && _stream.CanWrite)
            {
                try
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(input);
                    _stream.Write(bytes, 0, bytes.Length);
                    _stream.Flush();
                }
                catch (Exception ex)
                {
                    OnLogMessage?.Invoke($"Write to shell failed: {ex.Message}");
                }
            }
        }

        private void CopySelection()
        {
            if (!string.IsNullOrEmpty(_txtTerminal.SelectedText))
            {
                Clipboard.SetText(_txtTerminal.SelectedText);
            }
        }

        private void PasteClipboard()
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();
                // Replace CRLF with CR for SSH PTY
                text = text.Replace("\r\n", "\r").Replace("\n", "\r");
                SendToShell(text);
            }
        }

        private void UpdatePtySize()
        {
            // PTY window resize hook
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }
            base.Dispose(disposing);
        }
    }
}
