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
        private bool _isInputAttached = false;

        private Panel _topBar = null!;
        private Label _lblTitle = null!;
        private Label _lblStatus = null!;
        private Button _btnReconnect = null!;
        private Button _btnDetach = null!;
        private Button _btnDisconnect = null!;
        private PuttyHostPanel _puttyHostPanel = null!;

        public event Action<string>? OnLogMessage;
        public event Action? OnDisconnected;

        public bool IsSessionActive => _puttyProcess != null && !_puttyProcess.HasExited && _puttyHwnd != IntPtr.Zero;

        // ── Win32 API Imports ────────────────────────────────────────────────
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", CharSet = CharSet.Unicode)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr GetWindowLongAuto(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);
        }

        private static IntPtr SetWindowLongAuto(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, dwNewLong);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        private const int SW_SHOW = 5;
        private const uint WM_ACTIVATE = 0x0006;
        private const uint WM_SETFOCUS = 0x0007;
        private const int WA_ACTIVE = 1;

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;

        private const long WS_VISIBLE = 0x10000000L;
        private const long WS_CHILD = 0x40000000L;
        private const long WS_POPUP = 0x80000000L;
        private const long WS_CAPTION = 0x00C00000L;
        private const long WS_BORDER = 0x00800000L;
        private const long WS_DLGFRAME = 0x00400000L;
        private const long WS_SYSMENU = 0x00080000L;
        private const long WS_THICKFRAME = 0x00040000L;
        private const long WS_MINIMIZEBOX = 0x00020000L;
        private const long WS_MAXIMIZEBOX = 0x00010000L;

        private const long WS_EX_DLGMODALFRAME = 0x00000001L;
        private const long WS_EX_WINDOWEDGE = 0x00000100L;
        private const long WS_EX_CLIENTEDGE = 0x00000200L;
        private const long WS_EX_STATICEDGE = 0x00020000L;
        private const long WS_EX_APPWINDOW = 0x00040000L;

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
            _puttyHostPanel = new PuttyHostPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 12)
            };
            _puttyHostPanel.Resize += (s, e) => ResizePuttyWindow();

            this.Controls.Add(_puttyHostPanel);
            this.Controls.Add(_topBar);

            this.ResumeLayout(false);
        }

        internal class PuttyHostPanel : Panel
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.Style |= 0x02000000; // WS_CLIPCHILDREN
                    return cp;
                }
            }
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

        private void SafeInvoke(Action action)
        {
            try
            {
                if (this.IsDisposed || this.Disposing) return;
                if (!this.IsHandleCreated) return;

                this.BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        if (!this.IsDisposed && !this.Disposing)
                        {
                            action();
                        }
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                });
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private void OnPuttyProcessExited(object? sender, EventArgs e)
        {
            _puttyHwnd = IntPtr.Zero;
            SafeInvoke(() =>
            {
                _lblStatus.Text = "🔴 Disconnected";
                _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
                OnDisconnected?.Invoke();
            });
        }

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
                _puttyProcess.Exited += OnPuttyProcessExited;

                // Wait for PuTTY's main terminal window to appear
                IntPtr hwnd = IntPtr.Zero;
                int elapsed = 0;
                const int timeoutMs = 15000;

                while (hwnd == IntPtr.Zero && elapsed < timeoutMs && !_puttyProcess.HasExited)
                {
                    if (this.IsDisposed || this.Disposing)
                    {
                        Disconnect();
                        return;
                    }

                    System.Threading.Thread.Sleep(100);
                    elapsed += 100;
                    _puttyProcess.Refresh();

                    // Prefer PuTTY's main terminal window by class name "PuTTY"
                    IntPtr found = IntPtr.Zero;
                    EnumWindows((h, _) =>
                    {
                        GetWindowThreadProcessId(h, out uint pid);
                        if (pid == _puttyProcess.Id && IsWindowVisible(h))
                        {
                            var sbClass = new StringBuilder(256);
                            GetClassName(h, sbClass, 256);
                            if (sbClass.ToString() == "PuTTY")
                            {
                                found = h;
                                return false;
                            }
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (found != IntPtr.Zero)
                    {
                        hwnd = found;
                    }
                    else if (_puttyProcess.MainWindowHandle != IntPtr.Zero)
                    {
                        hwnd = _puttyProcess.MainWindowHandle;
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

                if (this.IsDisposed || this.Disposing)
                {
                    Disconnect();
                    return;
                }

                _puttyHwnd = hwnd;

                // Embed the PuTTY window into _puttyHostPanel
                SafeInvoke(() =>
                {
                    EmbedWindow(_puttyHwnd);
                });
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"PuTTY embedding failed: {ex.Message}");
                SafeInvoke(() =>
                {
                    _lblStatus.Text = "🔴 Failed";
                    _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
                    MessageBox.Show($"Failed to embed PuTTY session: {ex.Message}", "PuTTY Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        private void EmbedWindow(IntPtr hwnd)
        {
            try
            {
                if (this.IsDisposed || this.Disposing) return;

                // Ensure parent handle is created
                if (!_puttyHostPanel.IsHandleCreated) _ = _puttyHostPanel.Handle;

                // Reparent to our panel
                SetParent(hwnd, _puttyHostPanel.Handle);

                // Strip all styles, caption and borders
                StripWindowBorders(hwnd);

                // Resize and position to fill panel, force DWM / Windows to recalculate non-client area
                ResizePuttyWindow();

                _lblStatus.Text = "🟢 Connected (Embedded)";
                _lblStatus.ForeColor = Color.FromArgb(80, 220, 100);

                FocusPutty();
                OnLogMessage?.Invoke("PuTTY session embedded successfully.");

                // Follow-up pass to guarantee PuTTY's message loop doesn't re-assert frame styles on activation
                Task.Delay(150).ContinueWith(_ =>
                {
                    SafeInvoke(() =>
                    {
                        if (_puttyHwnd != IntPtr.Zero)
                        {
                            StripWindowBorders(_puttyHwnd);
                            ResizePuttyWindow();
                            FocusPutty();
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error while setting parent for PuTTY: {ex.Message}");
            }
        }

        private void StripWindowBorders(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return;

            try
            {
                // Strip all top-level borders, caption, system menu, dialog frame, convert to WS_CHILD
                long style = GetWindowLongAuto(hwnd, GWL_STYLE).ToInt64();
                style &= ~WS_POPUP;
                style &= ~WS_CAPTION;
                style &= ~WS_BORDER;
                style &= ~WS_DLGFRAME;
                style &= ~WS_SYSMENU;
                style &= ~WS_THICKFRAME;
                style &= ~WS_MINIMIZEBOX;
                style &= ~WS_MAXIMIZEBOX;
                style |= (WS_CHILD | WS_VISIBLE);
                SetWindowLongAuto(hwnd, GWL_STYLE, new IntPtr(style));

                // Strip extended window borders and sunken/raised frames
                long exStyle = GetWindowLongAuto(hwnd, GWL_EXSTYLE).ToInt64();
                exStyle &= ~WS_EX_DLGMODALFRAME;
                exStyle &= ~WS_EX_WINDOWEDGE;
                exStyle &= ~WS_EX_CLIENTEDGE;
                exStyle &= ~WS_EX_STATICEDGE;
                exStyle &= ~WS_EX_APPWINDOW;
                SetWindowLongAuto(hwnd, GWL_EXSTYLE, new IntPtr(exStyle));
            }
            catch { }
        }

        public void AttachInput()
        {
            if (_puttyHwnd == IntPtr.Zero) return;
            try
            {
                uint targetThreadId = GetWindowThreadProcessId(_puttyHwnd, out _);
                uint myThreadId = GetCurrentThreadId();

                if (targetThreadId != 0 && targetThreadId != myThreadId && !_isInputAttached)
                {
                    if (AttachThreadInput(myThreadId, targetThreadId, true))
                    {
                        _isInputAttached = true;
                    }
                }
            }
            catch { }
        }

        public void DetachInput()
        {
            if (_puttyHwnd == IntPtr.Zero) return;
            try
            {
                uint targetThreadId = GetWindowThreadProcessId(_puttyHwnd, out _);
                uint myThreadId = GetCurrentThreadId();

                if (targetThreadId != 0 && targetThreadId != myThreadId && _isInputAttached)
                {
                    AttachThreadInput(myThreadId, targetThreadId, false);
                    _isInputAttached = false;
                }
            }
            catch { }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                if (_puttyHwnd != IntPtr.Zero)
                {
                    try
                    {
                        ShowWindow(_puttyHwnd, SW_SHOW);
                        ResizePuttyWindow();
                        FocusPutty();
                    }
                    catch { }
                }
            }
            else
            {
                DetachInput();
            }
        }

        public void FocusPutty()
        {
            if (_puttyHwnd == IntPtr.Zero) return;

            try
            {
                AttachInput();
                BringWindowToTop(_puttyHwnd);
                SetFocus(_puttyHwnd);
            }
            catch { }
        }

        private void ResizePuttyWindow()
        {
            if (_puttyHwnd != IntPtr.Zero && _puttyHostPanel.Width > 0 && _puttyHostPanel.Height > 0)
            {
                try
                {
                    // Ensure window is parented to _puttyHostPanel
                    if (GetAncestor(_puttyHwnd, 1) != _puttyHostPanel.Handle)
                    {
                        SetParent(_puttyHwnd, _puttyHostPanel.Handle);
                    }

                    // 1. Re-assert basic child styles
                    StripWindowBorders(_puttyHwnd);

                    // 2. Measure actual non-client border offsets
                    GetWindowRect(_puttyHwnd, out RECT wr);
                    GetClientRect(_puttyHwnd, out RECT cr);
                    POINT pt = new POINT { X = 0, Y = 0 };
                    ClientToScreen(_puttyHwnd, ref pt);

                    int borderLeft = Math.Max(0, pt.X - wr.Left);
                    int borderTop = Math.Max(0, pt.Y - wr.Top);
                    int borderRight = Math.Max(0, wr.Right - (pt.X + cr.Right));
                    int borderBottom = Math.Max(0, wr.Bottom - (pt.Y + cr.Bottom));

                    // Safe fallbacks for Windows standard frame metrics if uninitialized
                    if (borderTop < 20) borderTop = 32;
                    if (borderLeft < 4) borderLeft = 8;
                    if (borderRight < 4) borderRight = 8;
                    if (borderBottom < 4) borderBottom = 8;

                    int targetX = -borderLeft;
                    int targetY = -borderTop;
                    int targetW = _puttyHostPanel.Width + borderLeft + borderRight;
                    int targetH = _puttyHostPanel.Height + borderTop + borderBottom;

                    // 3. Move and size PuTTY window so caption and borders lie outside the clipping panel
                    SetWindowPos(_puttyHwnd, IntPtr.Zero, targetX, targetY, targetW, targetH,
                        SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                }
                catch { }
            }
        }

        public void DetachPutty()
        {
            DetachInput();
            if (_puttyHwnd != IntPtr.Zero)
            {
                try
                {
                    // Restore window styles for standalone window
                    long style = GetWindowLongAuto(_puttyHwnd, GWL_STYLE).ToInt64();
                    style &= ~WS_CHILD;
                    style |= (WS_POPUP | WS_CAPTION | WS_THICKFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_VISIBLE);
                    SetWindowLongAuto(_puttyHwnd, GWL_STYLE, new IntPtr(style));

                    long exStyle = GetWindowLongAuto(_puttyHwnd, GWL_EXSTYLE).ToInt64();
                    exStyle |= WS_EX_WINDOWEDGE | WS_EX_APPWINDOW;
                    SetWindowLongAuto(_puttyHwnd, GWL_EXSTYLE, new IntPtr(exStyle));

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
            DetachInput();
            try
            {
                if (_puttyProcess != null)
                {
                    try { _puttyProcess.EnableRaisingEvents = false; } catch { }
                    try { _puttyProcess.Exited -= OnPuttyProcessExited; } catch { }
                    if (!_puttyProcess.HasExited)
                    {
                        _puttyProcess.Kill();
                    }
                    _puttyProcess.Dispose();
                }
            }
            catch { }

            _puttyHwnd = IntPtr.Zero;
            _puttyProcess = null;

            SafeInvoke(() =>
            {
                _lblStatus.Text = "🔴 Disconnected";
                _lblStatus.ForeColor = Color.FromArgb(255, 90, 90);
            });
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

