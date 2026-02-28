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

        const int GWL_STYLE = -16;
        const int WS_VISIBLE = 0x10000000;
        const int WS_CHILD = 0x40000000;

        public event Action<string>? OnLogMessage;
        public event Action? OnDisconnected;

        public FreeRdpContainer()
        {
            this.Resize += FreeRdpContainer_Resize;
        }

        public void LaunchSession(RdpConfig config, string exePath)
        {
            if (!this.IsHandleCreated)
            {
                var h = this.Handle;
            }

            string uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
            
            // Build arguments
            string dynRes = config.DynamicResolution ? "+dynamic-resolution" : "";
            string certArg = config.IgnoreCert ? "/cert:ignore" : "";
            string baseArgs = $"/v:{config.Host} /u:{config.User} /p:{config.Pass} /parent-window:{this.Handle} {dynRes} {certArg}";
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
                while (_rdpProcess != null && !_rdpProcess.HasExited && retries < 50)
                {
                    _rdpProcess.Refresh();
                    if (_rdpProcess.MainWindowHandle != IntPtr.Zero)
                    {
                        _rdpHandle = _rdpProcess.MainWindowHandle;
                        if (this.IsHandleCreated && !this.IsDisposed)
                        {
                            this.Invoke((MethodInvoker)delegate {
                                // Swallow the window
                                SetParent(_rdpHandle, this.Handle);
                                
                                // Remove borders
                                SetWindowLong(_rdpHandle, GWL_STYLE, WS_VISIBLE | WS_CHILD);

                                // Resize it
                                MoveWindow(_rdpHandle, 0, 0, this.Width, this.Height, true);
                            });
                        }
                        break;
                    }
                    Thread.Sleep(100);
                    retries++;
                }

                if (_rdpHandle == IntPtr.Zero && _rdpProcess != null && !_rdpProcess.HasExited)
                {
                    OnLogMessage?.Invoke("Failed to capture FreeRDP window handle within timeout.");
                }
            });
        }

        private void FreeRdpContainer_Resize(object? sender, EventArgs e)
        {
            if (_rdpHandle != IntPtr.Zero)
            {
                MoveWindow(_rdpHandle, 0, 0, this.Width, this.Height, true);
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
