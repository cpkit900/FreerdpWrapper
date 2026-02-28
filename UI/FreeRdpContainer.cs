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

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        const int GWL_STYLE = -16;
        const int WS_VISIBLE = 0x10000000;
        const int WS_CHILD = 0x40000000;
        const int WS_CAPTION = 0x00C00000;
        const int WS_THICKFRAME = 0x00040000;
        const int WS_POPUP = unchecked((int)0x80000000);

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
            string certArg = config.IgnoreCert ? "/cert:ignore" : "";
            
            // Core Authentication
            string baseArgs = $"/v:\"{config.Host}\" /u:\"{config.User}\" /p:\"{config.Pass}\"";
            if (!string.IsNullOrEmpty(config.Domain)) baseArgs += $" /d:\"{config.Domain}\"";
            if (config.EnableCredGuard) baseArgs += " +remote-credential-guard";

            // Gateway Setup
            if (!string.IsNullOrEmpty(config.GatewayHost))
            {
                baseArgs += $" /g:\"{config.GatewayHost}\"";
                if (!string.IsNullOrEmpty(config.GatewayUser)) baseArgs += $" /gu:\"{config.GatewayUser}\"";
                if (!string.IsNullOrEmpty(config.GatewayPass)) baseArgs += $" /gp:\"{config.GatewayPass}\"";
            }

            // Display & Embed
            baseArgs += $" /parent-window:{this.Handle} {dynRes} {certArg}";
            if (config.EnableDpiScale) baseArgs += " +dpiscale";
            if (!string.IsNullOrEmpty(config.RemoteApp)) baseArgs += $" /app:\"||{config.RemoteApp}\"";

            // Hardware Redirection
            if (config.EnableClipboard) baseArgs += " +clipboard";
            else baseArgs += " -clipboard";

            if (config.EnableSound) baseArgs += " /sound";
            if (config.EnableMicrophone) baseArgs += " /microphone";
            if (config.MapDrive) baseArgs += " /drive:host,C:\\";
            if (config.MapPrinter) baseArgs += " /printers";
            if (config.MapSmartcard) baseArgs += " /smartcard";

            if (!string.IsNullOrEmpty(config.AdditionalFlags))
            {
                baseArgs += $" {config.AdditionalFlags}";
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
            Task.Run(() => {
                int retries = 0;
                while (_rdpProcess != null && !_rdpProcess.HasExited && retries < 100)
                {
                    _rdpProcess.Refresh();
                    
                    IntPtr foundHandle = IntPtr.Zero;
                    if (_rdpProcess.Id > 0)
                    {
                        EnumWindows((hWnd, lParam) => {
                            GetWindowThreadProcessId(hWnd, out uint processId);
                            if (processId == _rdpProcess.Id && IsWindowVisible(hWnd))
                            {
                                foundHandle = hWnd;
                                return false; // Stop enumerating when found
                            }
                            return true; // Continue
                        }, IntPtr.Zero);
                    }

                    if (foundHandle != IntPtr.Zero)
                    {
                        _rdpHandle = foundHandle;
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)delegate {
                                // Swallow the window
                                SetParent(_rdpHandle, this.Handle);
                                
                                // Aggressively remove all borders, captions, and popup styles
                                int style = GetWindowLong(_rdpHandle, GWL_STYLE);
                                style &= ~WS_POPUP;
                                style &= ~WS_CAPTION;
                                style &= ~WS_THICKFRAME;
                                style |= WS_CHILD | WS_VISIBLE;
                                SetWindowLong(_rdpHandle, GWL_STYLE, style);

                                // Resize it to perfectly fill the client area
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
                    OnLogMessage?.Invoke("Failed to capture FreeRDP window handle within timeout.");
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


        public void Disconnect()
        {
            if (_rdpProcess != null && !_rdpProcess.HasExited)
            {
                try
                {
                    _rdpProcess.Kill();
                }
                catch { } // absorb if already killed
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
