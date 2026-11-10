using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;

namespace FreeRdpWrapper.UI
{
    public class PuttyContainer : UserControl
    {
        private SavedConnection? _connection;
        private Process? _puttyProcess;
        private IntPtr _puttyHwnd = IntPtr.Zero;

        private Panel _topBar = null!;
        private Label _lblTitle = null!;
        private Label _lblStatus = null!;
        private Button _btnReconnect = null!;
        private Button _btnDetach = null!;
        private Button _btnDisconnect = null!;
        private Panel _puttyHostPanel = null!;

        public event Action<string>? OnLogMessage;
        public event Action? OnDisconnected;

        public bool IsSessionActive => _puttyProcess != null && !_puttyProcess.HasExited && _puttyHwnd != IntPtr.Zero;

        // ── Win32 API Imports ────────────────────────────────────────────────
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        private const int GWL_STYLE = -16;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_CHILD = 0x40000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOACTIVATE = 0x0010;

        public PuttyContainer()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(12, 12, 12);
            this.Font = new Font("Segoe UI", 9f);

            // ── Top Action Bar ───────────────────────────────────────────────────
            _topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(28, 28, 30),
                Padding = new Padding(8, 3, 8, 3)
            };

            _lblTitle = new Label
            {
                Text = "🐱 PuTTY SSH Session",
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
                Location = new Point(250, 8)
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

            _btnReconnect = CreateTopButton("🔄 Reconnect", (s, e) => Reconnect());
            _btnDetach = CreateTopButton("↗️ Detach (Pop Out)", (s, e) => DetachPutty());
            _btnDisconnect = CreateTopButton("❌ Disconnect", (s, e) => Disconnect());

            pnlButtons.Controls.AddRange(new Control[] { _btnReconnect, _btnDetach, _btnDisconnect });
            _topBar.Controls.Add(pnlButtons);

            // ── PuTTY Host Panel ────────────────────────────────────────────────
            _puttyHostPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 12)
            };
            _puttyHostPanel.Resize += (s, e) => ResizePuttyWindow();

            this.Controls.Add(_puttyHostPanel);
            this.Controls.Add(_topBar);

            this.ResumeLayout(false);
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

        // ════════════════════════════════════════════════════════════════════
        // SESSION LAUNCH & WIN32 EMBEDDING
        // ════════════════════════════════════════════════════════════════════

        public void LaunchSession(SavedConnection connection)
        {
            _connection = connection;
            _lblTitle.Text = $"🐱 {connection.Name} (PuTTY • {connection.User}@{connection.Host})";
            _lblStatus.Text = "🟡 Launching PuTTY...";
            _lblStatus.ForeColor = Color.FromArgb(240, 200, 80);

            string? puttyPath = SshLauncher.FindPuttyExecutable();
            if (string.IsNullOrEmpty(puttyPath) || !File.Exists(puttyPath))
            {
                ShowPuttyNotFoundMessage();
                return;
            }

            Task.Run(() => StartAndEmbedPutty(puttyPath, connection));
        }

        private void StartAndEmbedPutty(string puttyPath, SavedConnection connection)
        {
            try
            {
                string host = connection.Host.Trim();
                int port = 22;
                if (host.Contains(":"))
                {
                    var parts = host.Split(':');
                    host = parts[0];
                    if (int.TryParse(parts[1], out int p)) port = p;
                }

                string user = connection.User.Trim();
                string plainPass = !string.IsNullOrEmpty(connection.Pass) 
                    ? VaultCrypto.DecryptSecret(connection.Pass) 
                    : string.Empty;
                if (string.IsNullOrEmpty(plainPass) && !string.IsNullOrEmpty(connection.Pass))
                    plainPass = connection.Pass;

                var args = new StringBuilder($"-ssh \"{host}\" -P {port}");
                if (!string.IsNullOrEmpty(user))
                    args.Append($" -l \"{user}\"");
                if (!string.IsNullOrEmpty(plainPass))
                    args.Append($" -pw \"{plainPass}\"");
                if (!string.IsNullOrEmpty(connection.SshKeyPath) && File.Exists(connection.SshKeyPath))
                    args.Append($" -i \"{connection.SshKeyPath}\"");

                var psi = new ProcessStartInfo
                {
                    FileName = puttyPath,
                    Arguments = args.ToString(),
                    UseShellExecute = true
                };

                OnLogMessage?.Invoke($"Starting PuTTY: {puttyPath} -ssh {host} -P {port} -l {user}");

                _puttyProcess = Process.Start(psi);
                if (_puttyProcess == null)
                {
                    throw new Exception("Failed to launch putty.exe process.");
                }

                _puttyProcess.EnableRaisingEvents = true;
                _puttyProcess.Exited += (s, e) =>
                {
                    _puttyHwnd = IntPtr.Zero;
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            _lblStatus.Text = "🔴 Disconnected";
                            _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
                            OnDisconnected?.Invoke();
                        });
                    }
                };

                // Wait for PuTTY's main terminal window to appear
                IntPtr hwnd = IntPtr.Zero;
                int elapsed = 0;
                const int timeoutMs = 15000;

                while (hwnd == IntPtr.Zero && elapsed < timeoutMs && !_puttyProcess.HasExited)
                {
                    System.Threading.Thread.Sleep(100);
                    elapsed += 100;
                    _puttyProcess.Refresh();

                    if (_puttyProcess.MainWindowHandle != IntPtr.Zero)
                    {
                        hwnd = _puttyProcess.MainWindowHandle;
                    }
                    else
                    {
                        // Fallback window search by process ID
                        IntPtr found = IntPtr.Zero;
                        EnumWindows((h, _) =>
                        {
                            GetWindowThreadProcessId(h, out uint pid);
                            if (pid == _puttyProcess.Id && IsWindowVisible(h))
                            {
                                found = h;
                                return false;
                            }
                            return true;
                        }, IntPtr.Zero);
                        hwnd = found;
                    }
                }

                if (hwnd == IntPtr.Zero)
                {
                    if (_puttyProcess.HasExited)
                    {
                        OnLogMessage?.Invoke("PuTTY process exited before window handle was captured.");
                        return;
                    }
                    throw new Exception("Timed out waiting for PuTTY terminal window.");
                }

                _puttyHwnd = hwnd;

                // Embed the PuTTY window into _puttyHostPanel
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        EmbedWindow(_puttyHwnd);
                    });
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"PuTTY embedding failed: {ex.Message}");
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        _lblStatus.Text = "🔴 Failed";
                        _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
                        MessageBox.Show($"Failed to embed PuTTY session: {ex.Message}", "PuTTY Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
            }
        }

        private void EmbedWindow(IntPtr hwnd)
        {
            try
            {
                // Ensure parent handle is created
                if (!_puttyHostPanel.IsHandleCreated) _ = _puttyHostPanel.Handle;

                // Reparent to our panel
                SetParent(hwnd, _puttyHostPanel.Handle);

                // Strip top-level borders and caption, convert to WS_CHILD
                int style = GetWindowLong(hwnd, GWL_STYLE);
                style &= ~WS_POPUP;
                style &= ~WS_CAPTION;
                style &= ~WS_THICKFRAME;
                style &= ~WS_MINIMIZEBOX;
                style &= ~WS_MAXIMIZEBOX;
                style |= WS_CHILD | WS_VISIBLE;
                SetWindowLong(hwnd, GWL_STYLE, style);

                // Resize and position to fill panel
                ResizePuttyWindow();

                _lblStatus.Text = "🟢 Connected (Embedded)";
                _lblStatus.ForeColor = Color.FromArgb(80, 220, 100);

                FocusPutty();
                OnLogMessage?.Invoke("PuTTY session embedded successfully.");
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error while setting parent for PuTTY: {ex.Message}");
            }
        }

        public void FocusPutty()
        {
            if (_puttyHwnd != IntPtr.Zero)
            {
                try
                {
                    SetFocus(_puttyHwnd);
                }
                catch { }
            }
        }

        private void ResizePuttyWindow()
        {
            if (_puttyHwnd != IntPtr.Zero && _puttyHostPanel.Width > 0 && _puttyHostPanel.Height > 0)
            {
                try
                {
                    SetWindowPos(_puttyHwnd, IntPtr.Zero, 0, 0, _puttyHostPanel.Width, _puttyHostPanel.Height,
                        SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                    MoveWindow(_puttyHwnd, 0, 0, _puttyHostPanel.Width, _puttyHostPanel.Height, true);
                }
                catch { }
            }
        }

        public void DetachPutty()
        {
            if (_puttyHwnd != IntPtr.Zero)
            {
                try
                {
                    // Restore window styles for standalone window
                    int style = GetWindowLong(_puttyHwnd, GWL_STYLE);
                    style &= ~WS_CHILD;
                    style |= WS_POPUP | WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
                    SetWindowLong(_puttyHwnd, GWL_STYLE, style);

                    // Reparent back to desktop root
                    SetParent(_puttyHwnd, IntPtr.Zero);
                    SetWindowPos(_puttyHwnd, IntPtr.Zero, 100, 100, 800, 600, SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);

                    _lblStatus.Text = "↗️ Detached Window";
                    _lblStatus.ForeColor = Color.FromArgb(120, 190, 255);
                    _puttyHwnd = IntPtr.Zero;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to detach PuTTY window: {ex.Message}", "Detach Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_puttyProcess != null && !_puttyProcess.HasExited)
                {
                    _puttyProcess.Kill();
                }
            }
            catch { }

            _puttyHwnd = IntPtr.Zero;
            _puttyProcess = null;

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
                LaunchSession(_connection);
            }
        }

        private void ShowPuttyNotFoundMessage()
        {
            _puttyHostPanel.Controls.Clear();

            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20)
            };

            var lbl = new Label
            {
                Text = "🐱 PuTTY (putty.exe) was not found on this computer.\r\n\r\n" +
                       "To use embedded PuTTY sessions:\r\n" +
                       "1. Download and install PuTTY from https://www.putty.org/\r\n" +
                       "2. Or place putty.exe in 'C:\\Program Files\\PuTTY\\' or in the 'Bin\\PuTTY' folder.",
                ForeColor = Color.FromArgb(220, 220, 220),
                Font = new Font("Segoe UI", 10.5f),
                AutoSize = false,
                Size = new Size(550, 120),
                Location = new Point(40, 40)
            };
            pnl.Controls.Add(lbl);

            var btnOpenUrl = new Button
            {
                Text = "🌐 Download PuTTY",
                Location = new Point(40, 170),
                Size = new Size(160, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnOpenUrl.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo("https://www.putty.org/") { UseShellExecute = true }); } catch { }
            };
            pnl.Controls.Add(btnOpenUrl);

            _puttyHostPanel.Controls.Add(pnl);
            _lblStatus.Text = "🔴 PuTTY Missing";
            _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
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
