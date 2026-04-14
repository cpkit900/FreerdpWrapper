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

        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        const uint RDW_INVALIDATE  = 0x0001;
        const uint RDW_ERASE       = 0x0004;
        const uint RDW_ALLCHILDREN = 0x0080;
        const uint RDW_UPDATENOW   = 0x0100;
        const uint WM_PAINT        = 0x000F;

        // ── Events ───────────────────────────────────────────────────────────

        public event Action<string>? OnLogMessage;
        public event Action? OnDisconnected;

        public FreeRdpContainer() { }

        // ── Session launch ───────────────────────────────────────────────────

        public void LaunchSession(RdpConfig config, string exePath)
        {
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

            string plainPass = FreeRdpWrapper.Services.CryptoHelper.DecryptString(config.Pass);
            if (!string.IsNullOrEmpty(plainPass)) baseArgs += $" /p:\"{plainPass}\"";

            if (!string.IsNullOrEmpty(config.Domain)) baseArgs += $" /d:\"{config.Domain}\"";
            if (config.EnableCredGuard) baseArgs += " +remote-credential-guard";

            // Gateway
            if (config.EnableGateway && !string.IsNullOrEmpty(config.GatewayHost))
            {
                string gwArgs = $"g:{config.GatewayHost}";
                if (!string.IsNullOrEmpty(config.GatewayDomain)) gwArgs += $",d:{config.GatewayDomain}";
                if (!string.IsNullOrEmpty(config.GatewayUser))   gwArgs += $",u:{config.GatewayUser}";

                string plainGwPass = FreeRdpWrapper.Services.CryptoHelper.DecryptString(config.GatewayPass);
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
            const int maxWaitMs   = 20_000;
            const int intervalMs  = 100;
            int elapsed = 0;

            // Give the process a moment to create its message loop before we poll
            try { _rdpProcess?.WaitForInputIdle(3000); } catch { }

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
                            MoveWindow(_rdpHandle, 0, 0, this.ClientSize.Width, this.ClientSize.Height, true)));
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

        /// <summary>Called by Form1 when a tab is selected to route focus into the RDP session.</summary>
        public void FocusRdpWindow()
        {
            RepaintRdpWindow();
            this.Focus();
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

        // ── Disconnect / cleanup ─────────────────────────────────────────────

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
