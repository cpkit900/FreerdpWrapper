using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.UI
{
    public partial class FreeRdpContainer : UserControl
    {
        private Process? _rdpProcess;
        private IntPtr _rdpHandle = IntPtr.Zero;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
        // RDW flags: INVALIDATE=0x1, ALLCHILDREN=0x80, UPDATENOW=0x100, ERASE=0x4
        const uint RDW_INVALIDATE   = 0x0001;
        const uint RDW_ERASE        = 0x0004;
        const uint RDW_ALLCHILDREN  = 0x0080;
        const uint RDW_UPDATENOW    = 0x0100;

        const uint WM_ACTIVATE = 0x0006;
        const uint WM_SETFOCUS = 0x0007;
        const uint WM_PAINT    = 0x000F;

        const uint WM_LBUTTONDOWN = 0x0201;
        const uint WM_LBUTTONUP   = 0x0202;

        public event Action<string>? OnLogMessage;
        public event Action? OnDisconnected;

        public FreeRdpContainer()
        {
        }

        public void LaunchSession(RdpConfig config, string exePath)
        {
            if (!this.IsHandleCreated)
            {
                var h = this.Handle;
            }

            string uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
            
            string dynRes = config.DynamicResolution ? "+dynamic-resolution" : "";
            if (config.UseCustomResolution)
            {
                dynRes = $"/w:{config.ResolutionWidth} /h:{config.ResolutionHeight}";
            }

            string certArg = config.CertConfig switch
            {
                CertSecurity.Tofu => "/cert:tofu",
                CertSecurity.Deny => "/cert:deny",
                _ => "/cert:ignore"
            };
            
            // Core Authentication
            string baseArgs = $"/v:\"{config.Host}\" /u:\"{config.User}\"";
            
            string plainPass = FreeRdpWrapper.Services.CryptoHelper.DecryptString(config.Pass);
            if (!string.IsNullOrEmpty(plainPass)) baseArgs += $" /p:\"{plainPass}\"";
            
            if (!string.IsNullOrEmpty(config.Domain)) baseArgs += $" /d:\"{config.Domain}\"";
            if (config.EnableCredGuard) baseArgs += " +remote-credential-guard";

            // Gateway Setup
            if (config.EnableGateway && !string.IsNullOrEmpty(config.GatewayHost))
            {
                string gwArgs = $"g:{config.GatewayHost}";
                if (!string.IsNullOrEmpty(config.GatewayDomain)) gwArgs += $",d:{config.GatewayDomain}";
                if (!string.IsNullOrEmpty(config.GatewayUser)) gwArgs += $",u:{config.GatewayUser}";
                
                string plainGwPass = FreeRdpWrapper.Services.CryptoHelper.DecryptString(config.GatewayPass);
                if (!string.IsNullOrEmpty(plainGwPass))
                {
                    // If password has commas or quotes, we need to be careful, but generally removing internal quotes is better.
                    gwArgs += $",p:{plainGwPass}";
                }
                baseArgs += $" \"/gateway:{gwArgs}\"";
            }

            if (config.MultiMonitor) 
            {
                baseArgs += " /multimon";
            }
            else 
            {
                // Native embedding: both wfreerdp (now patched for IsChild focus check) and
                // sdl3-freerdp (now patched with SDL_PROP_WINDOW_CREATE_WIN32_HWND_POINTER)
                // correctly support /parent-window without breaking keyboard input.
                baseArgs += $" /parent-window:{this.Handle}";
            }
            
            baseArgs += $" {dynRes} {certArg}";
            if (!string.IsNullOrEmpty(config.RemoteApp)) 
            {
                // Simplified syntax: /app:"||Alias" instead of /app:program:"||Alias"
                // This ensures the SDL parser correctly identifies the application name.
                baseArgs += $" /app:\"||{config.RemoteApp}\"";
            }

            // Hardware Redirection
            if (config.EnableClipboard) baseArgs += " +clipboard";
            else baseArgs += " -clipboard";

            if (config.EnableSound) baseArgs += " /sound";
            if (config.EnableMicrophone) baseArgs += " /microphone";
            if (config.MapDrive) baseArgs += " +drives"; // Replaces specific C:\ mapping with universal mapping
            if (config.MapSmartcard) baseArgs += " /smartcard";
            if (config.EnableUsbRedirection) baseArgs += " /usb:auto";

            // Security & Admin Features
            if (config.AdminSession) baseArgs += " +admin";
            
            // RAIL (RemoteApp) conflicts with modern GFX and Auto Network profiles in some FreeRDP builds.
            // We disable them here if a RemoteApp is active to ensure a stable handshake.
            if (string.IsNullOrEmpty(config.RemoteApp))
            {
                if (config.AutoNetworkProfile && !config.GamingMode) baseArgs += " /network:auto";

                // Gaming Optimizations
                if (config.GamingMode)
                {
                    baseArgs += " /gfx +async-update +async-channels /network:broadband-high";
                }
            }
            else
            {
                // For RemoteApp, we use a more conservative network profile if none specified
                if (config.AutoNetworkProfile) baseArgs += " /network:lan";
            }

            if (!string.IsNullOrEmpty(config.AdditionalFlags))
            {
                // Sanitize legacy gfx arguments from older saved connections
                string safeFlags = config.AdditionalFlags
                    .Replace("/gfx:avc444,mask:1", "/gfx")
                    .Replace("/gfx:avc444", "/gfx")
                    .Replace("/gfx:AVC444", "/gfx")
                    .Replace("/gfx:AVC444:on", "/gfx");
                    
                baseArgs += $" {safeFlags}";
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = baseArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            string vcpkgBin = @"C:\Users\Junnu\vcpkg\installed\x64-windows\bin";
            string exeDir = System.IO.Path.GetDirectoryName(exePath) ?? "";
            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            startInfo.EnvironmentVariables["PATH"] = $"{vcpkgBin};{exeDir};{currentPath}";

            OnLogMessage?.Invoke($"Launching process: {exePath} {baseArgs}");

            _rdpProcess = new Process { StartInfo = startInfo };
            
            _rdpProcess.OutputDataReceived += (s, e) => {
                if(e.Data != null) OnLogMessage?.Invoke(e.Data);
            };
            _rdpProcess.ErrorDataReceived += (s, e) => {
                if(e.Data != null) OnLogMessage?.Invoke("ERROR: " + e.Data);
            };

            _rdpProcess.EnableRaisingEvents = true;
            _rdpProcess.Exited += (s, e) => {
                int exitCode = -1;
                try { exitCode = _rdpProcess.ExitCode; } catch { }
                OnLogMessage?.Invoke($"Process exited with code {exitCode}");

                _rdpHandle = IntPtr.Zero;
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke((MethodInvoker)delegate { OnDisconnected?.Invoke(); });
                }
            };

            _rdpProcess.Start();
            
            _rdpProcess.BeginOutputReadLine();
            _rdpProcess.BeginErrorReadLine();

            // Wait for window handle creation
            if (config.MultiMonitor) return; // Do not swallow window if using multiple monitors

            Task.Run(() => {
                int retries = 0;
                while (_rdpProcess != null && !_rdpProcess.HasExited && retries < 400) // 20 seconds timeout for SDL3
                {
                    _rdpProcess.Refresh();
                    
                    IntPtr foundHandle = IntPtr.Zero;
                    if (_rdpProcess.Id > 0)
                    {
                        // Native child window search — works for both patched wfreerdp and sdl3
                        EnumChildWindows(this.Handle, (hWnd, lParam) => {
                            GetWindowThreadProcessId(hWnd, out uint processId);
                            if (processId == _rdpProcess.Id && IsWindowVisible(hWnd))
                            {
                                foundHandle = hWnd;
                                return false;
                            }
                            return true;
                        }, IntPtr.Zero);
                    }

                    if (foundHandle != IntPtr.Zero)
                    {
                        OnLogMessage?.Invoke($"[WindowCapture] Successfully found handle: {foundHandle}");
                        _rdpHandle = foundHandle;
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)delegate {
                                // Simple initial resize to fill our container
                                MoveWindow(_rdpHandle, 0, 0, this.ClientSize.Width, this.ClientSize.Height, true);
                            });
                        }
                        break;
                    }
                    Thread.Sleep(50);
                    retries++;
                }

                if (_rdpHandle == IntPtr.Zero && _rdpProcess != null && !_rdpProcess.HasExited)
                {
                    OnLogMessage?.Invoke("[WindowCapture] Failed to capture FreeRDP window handle within timeout. Did it spawn as a totally hidden background process?");
                }
            });
        }


        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_rdpHandle != IntPtr.Zero)
            {
                MoveWindow(_rdpHandle, 0, 0, this.ClientSize.Width, this.ClientSize.Height, true);
            }
        }

        public void FocusRdpWindow()
        {
            if (_rdpHandle != IntPtr.Zero)
            {
                this.Focus();
                
                uint targetThreadId = GetWindowThreadProcessId(_rdpHandle, out _);
                uint myThreadId = GetCurrentThreadId();

                if (targetThreadId != 0 && targetThreadId != myThreadId)
                {
                    AttachThreadInput(myThreadId, targetThreadId, true);
                    SetFocus(_rdpHandle);
                    Task.Delay(100).ContinueWith(_ => {
                        AttachThreadInput(myThreadId, targetThreadId, false);
                    });
                }
                else
                {
                    SetFocus(_rdpHandle);
                }
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                RepaintRdpWindow();
                FocusRdpWindow();
            }
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            RepaintRdpWindow();
            FocusRdpWindow();
        }

        /// <summary>
        /// Forces the embedded SDL3 child window to immediately repaint itself.
        /// SDL3's Direct3D11 renderer does not respond to WM_PAINT for child windows
        /// — it only renders when FreeRDP pushes frame updates. Calling RedrawWindow
        /// with RDW_UPDATENOW forces the compositor to blit the last rendered D3D11
        /// frame to screen immediately, eliminating the blank/stale tab-switch delay.
        /// </summary>
        private void RepaintRdpWindow()
        {
            if (_rdpHandle == IntPtr.Zero) return;

            // Invalidate the entire child window tree and force an immediate update.
            RedrawWindow(_rdpHandle, IntPtr.Zero, IntPtr.Zero,
                RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_UPDATENOW);
            
            // Also send WM_PAINT directly in case SDL3 processes paint messages
            SendMessage(_rdpHandle, WM_PAINT, IntPtr.Zero, IntPtr.Zero);
        }


        public void Disconnect()
        {
            if (_rdpProcess != null && !_rdpProcess.HasExited)
            {
                try
                {
                    _rdpProcess.Kill();
                }
                catch { }
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
