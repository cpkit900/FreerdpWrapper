using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.UI
{
    public partial class FreeRdpContainer : UserControl
    {
        private Process? _rdpProcess;
        private IntPtr _rdpHandle = IntPtr.Zero;
        private RdpConfig? _lastConfig;
        private string _lastExePath = string.Empty;

        // ── Win32 imports needed at runtime ──────────────────────────────────

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        const uint RDW_INVALIDATE  = 0x0001;
        const uint RDW_ERASE       = 0x0004;
        const uint RDW_ALLCHILDREN = 0x0080;
        const uint RDW_UPDATENOW   = 0x0100;
        const uint WM_PAINT        = 0x000F;
        const uint WM_ACTIVATE     = 0x0006;

        // ── Events ───────────────────────────────────────────────────────────

        public event Action<string>? OnLogMessage;
        public event Action? OnDisconnected;

        public FreeRdpContainer() { }

        // ── Session launch ───────────────────────────────────────────────────

        public void LaunchSession(RdpConfig config, string exePath)
        {
            _lastConfig = config;
            _lastExePath = exePath;

            // Ensure the Win32 handle is created before we pass it to FreeRDP
            if (!this.IsHandleCreated)
                _ = this.Handle;

            // ── Build arguments ───────────────────────────────────────────────

            string dynRes = config.DynamicResolution ? "+dynamic-resolution" : "";
            if (config.UseCustomResolution)
                dynRes = $"/w:{config.ResolutionWidth} /h:{config.ResolutionHeight}";

            string certArg = config.CertConfig switch
            {
                CertSecurity.Tofu => "/cert:tofu",
                CertSecurity.Deny => "/cert:deny",
                _                 => "/cert:ignore"
            };

            string baseArgs = $"/v:\"{config.Host}\" /u:\"{config.User}\"";

            string plainPass = !string.IsNullOrEmpty(config.Pass) ? FreeRdpWrapper.Services.VaultCrypto.DecryptSecret(config.Pass) : string.Empty;
            if (string.IsNullOrEmpty(plainPass) && !string.IsNullOrEmpty(config.Pass)) plainPass = config.Pass;
            if (!string.IsNullOrEmpty(plainPass)) baseArgs += $" /p:\"{plainPass}\"";

            if (!string.IsNullOrEmpty(config.Domain)) baseArgs += $" /d:\"{config.Domain}\"";
            if (config.EnableCredGuard) baseArgs += " +remote-credential-guard";

            // Gateway
            if (config.EnableGateway && !string.IsNullOrEmpty(config.GatewayHost))
            {
                string gwArgs = $"g:{config.GatewayHost}";
                if (!string.IsNullOrEmpty(config.GatewayDomain)) gwArgs += $",d:{config.GatewayDomain}";
                if (!string.IsNullOrEmpty(config.GatewayUser))   gwArgs += $",u:{config.GatewayUser}";

                string plainGwPass = !string.IsNullOrEmpty(config.GatewayPass) ? FreeRdpWrapper.Services.VaultCrypto.DecryptSecret(config.GatewayPass) : string.Empty;
                if (string.IsNullOrEmpty(plainGwPass) && !string.IsNullOrEmpty(config.GatewayPass)) plainGwPass = config.GatewayPass;
                if (!string.IsNullOrEmpty(plainGwPass)) gwArgs += $",p:{plainGwPass}";

                baseArgs += $" \"/gateway:{gwArgs}\"";
            }

            if (config.MultiMonitor)
            {
                baseArgs += " /multimon";
            }
            else
            {
                // sdl3-freerdp and wfreerdp are both patched to correctly handle
                // /parent-window — SDL3 calls Win32 SetParent() internally, and
                // wfreerdp's keyboard hook was updated to use IsChild() instead of
                // a strict hwndFocus == hWndParent equality check.
                baseArgs += $" /parent-window:{this.Handle}";
            }

            baseArgs += $" {dynRes} {certArg}";

            if (!string.IsNullOrEmpty(config.RemoteApp))
                baseArgs += $" /app:\"||{config.RemoteApp}\"";

            // Device redirection
            if (config.EnableClipboard) baseArgs += " +clipboard";
            else                        baseArgs += " -clipboard";

            if (config.EnableSound)          baseArgs += " /sound";
            if (config.EnableMicrophone)     baseArgs += " /microphone";
            if (config.MapDrive)             baseArgs += " +drives";
            if (config.MapSmartcard)         baseArgs += " /smartcard";
            if (config.EnableUsbRedirection) baseArgs += " /usb:auto";

            if (config.AdminSession) baseArgs += " +admin";
            if (config.DisableUdp)   baseArgs += " /multitransport:tcp-only";

            // Network / GFX (skip for RemoteApp — conflicts with RAIL)
            if (string.IsNullOrEmpty(config.RemoteApp))
            {
                if (config.AutoNetworkProfile && !config.GamingMode)
                    baseArgs += " /network:auto";

                if (config.GamingMode)
                    baseArgs += " /gfx +async-update +async-channels /network:broadband-high";
            }
            else
            {
                if (config.AutoNetworkProfile) baseArgs += " /network:lan";
            }

            if (!string.IsNullOrEmpty(config.AdditionalFlags))
            {
                string safeFlags = config.AdditionalFlags
                    .Replace("/gfx:avc444,mask:1", "/gfx")
                    .Replace("/gfx:avc444",        "/gfx")
                    .Replace("/gfx:AVC444",        "/gfx")
                    .Replace("/gfx:AVC444:on",     "/gfx");
                baseArgs += $" {safeFlags}";
            }

            // ── Start process ─────────────────────────────────────────────────

            var startInfo = new ProcessStartInfo
            {
                FileName              = exePath,
                Arguments             = baseArgs,
                UseShellExecute       = false,
                CreateNoWindow        = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            };

            // Ensure vcpkg and exe directory are on PATH so FreeRDP finds its DLLs
            string vcpkgBin    = @"C:\Users\Junnu\vcpkg\installed\x64-windows\bin";
            string exeDir      = System.IO.Path.GetDirectoryName(exePath) ?? "";
            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            startInfo.EnvironmentVariables["PATH"] = $"{vcpkgBin};{exeDir};{currentPath}";

            OnLogMessage?.Invoke($"Launching process: {exePath} {baseArgs}");

            _rdpProcess = new Process { StartInfo = startInfo };

            _rdpProcess.OutputDataReceived += (s, e) => { if (e.Data != null) OnLogMessage?.Invoke(e.Data); };
            _rdpProcess.ErrorDataReceived  += (s, e) => { if (e.Data != null) OnLogMessage?.Invoke("ERROR: " + e.Data); };

            _rdpProcess.EnableRaisingEvents = true;
            _rdpProcess.Exited += (s, e) =>
            {
                int exitCode = -1;
                try { exitCode = _rdpProcess!.ExitCode; } catch { }
                OnLogMessage?.Invoke($"Process exited with code {exitCode}");

                _rdpHandle = IntPtr.Zero;
                if (this.IsHandleCreated && !this.IsDisposed)
                    this.Invoke((MethodInvoker)(() => OnDisconnected?.Invoke()));
            };

            _rdpProcess.Start();
            _rdpProcess.BeginOutputReadLine();
            _rdpProcess.BeginErrorReadLine();

            // Multi-monitor mode: FreeRDP manages its own windows
            if (config.MultiMonitor) return;

            // ── Capture the child HWND ────────────────────────────────────────
            // /parent-window means sdl3-freerdp calls SetParent(sdlHwnd, this.Handle)
            // during its own initialization. We still need the child HWND so we can
            // call MoveWindow on resize and RepaintRdpWindow on tab switch.
            //
            // Strategy: wait for the process to finish initializing (WaitForInputIdle),
            // then do a single EnumChildWindows pass. Retry with backoff until the
            // SDL3 window is visible (connection established) or process exits.
            Task.Run(() => CaptureChildHandle(exePath));
        }

        private void CaptureChildHandle(string exePath)
        {
            const int maxWaitMs  = 20_000;
            const int intervalMs = 100;
            int elapsed = 0;

            // Poll every 100ms — do NOT use WaitForInputIdle here.
            // WaitForInputIdle returns as soon as SDL3's message loop starts, which is
            // well before the RDP connection is established and the child HWND is visible.
            // If we capture too early we get a stale/wrong handle. We need the window
            // to be visible (IsWindowVisible) which only happens after GDI init + first frame.
            while (_rdpProcess != null && !_rdpProcess.HasExited && elapsed < maxWaitMs)
            {
                IntPtr found = IntPtr.Zero;
                EnumChildWindows(this.Handle, (hWnd, _) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint pid);
                    if (pid == _rdpProcess?.Id && IsWindowVisible(hWnd))
                    {
                        found = hWnd;
                        return false; // stop enumeration
                    }
                    return true;
                }, IntPtr.Zero);

                if (found != IntPtr.Zero)
                {
                    OnLogMessage?.Invoke($"[WindowCapture] Successfully found handle: {found}");
                    _rdpHandle = found;

                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            MoveWindow(_rdpHandle, 0, 0, this.ClientSize.Width, this.ClientSize.Height, true);
                            FocusRdpWindow(); // route keyboard focus now that handle is ready
                        }));
                    }
                    return;
                }

                System.Threading.Thread.Sleep(intervalMs);
                elapsed += intervalMs;
            }

            if (_rdpHandle == IntPtr.Zero && _rdpProcess != null && !_rdpProcess.HasExited)
                OnLogMessage?.Invoke("[WindowCapture] Failed to capture FreeRDP window handle within timeout.");
        }

        // ── Resize ───────────────────────────────────────────────────────────

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_rdpHandle != IntPtr.Zero)
                MoveWindow(_rdpHandle, 0, 0, this.ClientSize.Width, this.ClientSize.Height, true);
        }

        // ── Tab visibility / focus ────────────────────────────────────────────

        /// <summary>
        /// Routes keyboard focus into the embedded SDL3 session.
        /// SDL3 runs in a separate Win32 process — this.Focus() alone only focuses our
        /// WinForms container. The SDL3 HWND in the other process never receives
        /// WM_SETFOCUS and won't process keyboard events until the user clicks.
        /// AttachThreadInput + SetFocus bridges the cross-process focus gap.
        /// WM_ACTIVATE is also sent so SDL3's internal focus tracking updates correctly.
        /// </summary>
        public void FocusRdpWindow()
        {
            RepaintRdpWindow();
            this.Focus(); // focus our container first

            if (_rdpHandle == IntPtr.Zero) return;

            uint targetThreadId = GetWindowThreadProcessId(_rdpHandle, out _);
            uint myThreadId     = GetCurrentThreadId();

            if (targetThreadId != 0 && targetThreadId != myThreadId)
            {
                AttachThreadInput(myThreadId, targetThreadId, true);
                BringWindowToTop(_rdpHandle);
                SetFocus(_rdpHandle);
                // WM_ACTIVATE(WA_ACTIVE=1) triggers SDL3's SDL_EVENT_WINDOW_FOCUS_GAINED
                // so its event loop recognises the focus and starts routing key events
                SendMessage(_rdpHandle, WM_ACTIVATE, (IntPtr)1, IntPtr.Zero);
                Task.Delay(150).ContinueWith(_ =>
                    AttachThreadInput(myThreadId, targetThreadId, false));
            }
            else
            {
                SetFocus(_rdpHandle);
                SendMessage(_rdpHandle, WM_ACTIVATE, (IntPtr)1, IntPtr.Zero);
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
                FocusRdpWindow();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            FocusRdpWindow();
        }

        /// <summary>
        /// Asks the OS to immediately blit whatever the SDL3 D3D11 renderer last
        /// drew. SDL3 does not handle WM_PAINT for child windows on its own —
        /// without this there is a visible stale/blank frame on every tab switch.
        /// The SDL3 source fix (SDL_EVENT_WINDOW_EXPOSED handler) is the primary
        /// solution; this is the belt-and-suspenders fallback from the C# side.
        /// </summary>
        private void RepaintRdpWindow()
        {
            if (_rdpHandle == IntPtr.Zero) return;
            RedrawWindow(_rdpHandle, IntPtr.Zero, IntPtr.Zero,
                RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_UPDATENOW);
            SendMessage(_rdpHandle, WM_PAINT, IntPtr.Zero, IntPtr.Zero);
        }

        public void Reconnect()
        {
            if (_lastConfig != null && !string.IsNullOrEmpty(_lastExePath))
            {
                Disconnect();
                System.Threading.Thread.Sleep(300);
                LaunchSession(_lastConfig, _lastExePath);
            }
        }

        public System.Drawing.Bitmap? CaptureScreenshot()
        {
            if (this.Width <= 0 || this.Height <= 0) return null;
            try
            {
                var bmp = new System.Drawing.Bitmap(this.Width, this.Height);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    System.Drawing.Point screenPt = this.PointToScreen(System.Drawing.Point.Empty);
                    g.CopyFromScreen(screenPt.X, screenPt.Y, 0, 0, this.Size);
                }
                return bmp;
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"[Screenshot] Failed: {ex.Message}");
                return null;
            }
        }

        public void SendCtrlAltDel()
        {
            FocusRdpWindow();
            // Send Ctrl + Alt + End (standard Windows SAS key for Remote Desktop)
            const byte VK_CONTROL = 0x11;
            const byte VK_MENU = 0x12; // Alt
            const byte VK_END = 0x23;
            const uint KEYEVENTF_KEYUP = 0x0002;

            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
            keybd_event(VK_END, 0, 0, UIntPtr.Zero);
            System.Threading.Thread.Sleep(50);
            keybd_event(VK_END, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            OnLogMessage?.Invoke("[Input] Injected Remote Ctrl+Alt+Del (Ctrl+Alt+End) sequence.");
        }

        // ── Disconnect / cleanup ─────────────────────────────────────────────

        public bool IsSessionActive => _rdpProcess != null && !_rdpProcess.HasExited;

        public void Disconnect()
        {
            if (_rdpProcess != null && !_rdpProcess.HasExited)
            {
                try { _rdpProcess.Kill(); } catch { }
                _rdpProcess.Dispose();
                _rdpProcess = null;
            }
            _rdpHandle = IntPtr.Zero;
        }

        protected override void Dispose(bool disposing)
        {
            Disconnect();
            base.Dispose(disposing);
        }
    }
}
