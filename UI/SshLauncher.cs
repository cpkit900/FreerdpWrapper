using System;
using System.Diagnostics;
using System.IO;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.UI
{
    public static class SshLauncher
    {
        public static void LaunchInteractiveSession(SavedConnection connection)
        {
            try
            {
                string args = "";
                if (!string.IsNullOrEmpty(connection.SshKeyPath) && File.Exists(connection.SshKeyPath))
                {
                    args += $"-i \"{connection.SshKeyPath}\" ";
                }
                
                string host = connection.Host;
                string portArg = "";
                
                // Handle custom ports like 192.168.1.10:2222
                if (host.Contains(":"))
                {
                    var parts = host.Split(':');
                    host = parts[0];
                    portArg = $"-p {parts[1]} ";
                }

                string userHost = string.IsNullOrEmpty(connection.User) ? host : $"{connection.User}@{host}";
                
                string finalCommand = $"ssh {args}{portArg}{userHost}";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c title SSH: {connection.Name} && {finalCommand} && pause",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to launch SSH session: {ex.Message}", "SSH Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        public static async System.Threading.Tasks.Task<string> ExecuteBackgroundCommandAsync(SavedConnection connection, string script)
        {
            try
            {
                string args = "";
                if (!string.IsNullOrEmpty(connection.SshKeyPath) && File.Exists(connection.SshKeyPath))
                {
                    args += $"-i \"{connection.SshKeyPath}\" ";
                }
                
                string host = connection.Host;
                string portArg = "";
                
                if (host.Contains(":"))
                {
                    var parts = host.Split(':');
                    host = parts[0];
                    portArg = $"-p {parts[1]} ";
                }

                string userHost = string.IsNullOrEmpty(connection.User) ? host : $"{connection.User}@{host}";
                
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ssh",
                    Arguments = $"{args}{portArg}{userHost}",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                await process.StandardInput.WriteLineAsync(script);
                process.StandardInput.Close();

                string output = await process.StandardOutput.ReadToEndAsync();
                string err = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(err))
                    output += "\nError Output:\n" + err;

                return string.IsNullOrWhiteSpace(output) ? "Command executed successfully with no output." : output;
            }
            catch (Exception ex)
            {
                return $"Exception during execution: {ex.Message}";
            }
        }
    }
}
