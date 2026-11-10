using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.Services;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace FreeRdpWrapper.UI
{
    public static class SshLauncher
    {
        public static ConnectionInfo BuildConnectionInfo(SavedConnection connection)
        {
            string host = connection.Host.Trim();
            int port = 22;

            if (host.Contains(":"))
            {
                var parts = host.Split(':');
                host = parts[0];
                if (int.TryParse(parts[1], out int p)) port = p;
            }

            string user = string.IsNullOrWhiteSpace(connection.User) ? "root" : connection.User.Trim();

            // Decrypt password if encrypted
            string plainPass = !string.IsNullOrEmpty(connection.Pass) 
                ? VaultCrypto.DecryptSecret(connection.Pass) 
                : string.Empty;
            if (string.IsNullOrEmpty(plainPass) && !string.IsNullOrEmpty(connection.Pass))
                plainPass = connection.Pass;

            var authMethods = new List<AuthenticationMethod>();

            // 1. Password and Keyboard-Interactive Auth
            if (!string.IsNullOrEmpty(plainPass))
            {
                authMethods.Add(new PasswordAuthenticationMethod(user, plainPass));

                var kbi = new KeyboardInteractiveAuthenticationMethod(user);
                kbi.AuthenticationPrompt += (s, e) =>
                {
                    foreach (var prompt in e.Prompts)
                    {
                        if (prompt.Request.ToLower().Contains("password") ||
                            prompt.Request.ToLower().Contains("verification") ||
                            prompt.Request.Trim().EndsWith(":"))
                        {
                            prompt.Response = plainPass;
                        }
                    }
                };
                authMethods.Add(kbi);
            }

            // 2. Private Key Auth
            if (!string.IsNullOrEmpty(connection.SshKeyPath) && File.Exists(connection.SshKeyPath))
            {
                try
                {
                    PrivateKeyFile keyFile = string.IsNullOrEmpty(plainPass)
                        ? new PrivateKeyFile(connection.SshKeyPath)
                        : new PrivateKeyFile(connection.SshKeyPath, plainPass);
                    authMethods.Add(new PrivateKeyAuthenticationMethod(user, keyFile));
                }
                catch { }
            }

            if (authMethods.Count == 0)
            {
                authMethods.Add(new PasswordAuthenticationMethod(user, ""));
            }

            return new ConnectionInfo(host, port, user, authMethods.ToArray())
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public static void LaunchInteractiveSession(SavedConnection connection)
        {
            try
            {
                string host = connection.Host.Trim();
                string portArg = "";
                
                if (host.Contains(":"))
                {
                    var parts = host.Split(':');
                    host = parts[0];
                    portArg = $"-p {parts[1]} ";
                }

                string user = string.IsNullOrWhiteSpace(connection.User) ? "root" : connection.User.Trim();
                string userHost = $"{user}@{host}";

                string args = "";
                if (!string.IsNullOrEmpty(connection.SshKeyPath) && File.Exists(connection.SshKeyPath))
                {
                    args += $"-i \"{connection.SshKeyPath}\" ";
                }

                string plainPass = !string.IsNullOrEmpty(connection.Pass) 
                    ? VaultCrypto.DecryptSecret(connection.Pass) 
                    : string.Empty;
                if (string.IsNullOrEmpty(plainPass) && !string.IsNullOrEmpty(connection.Pass))
                    plainPass = connection.Pass;

                string askpassScript = "";
                if (!string.IsNullOrEmpty(plainPass))
                {
                    askpassScript = Path.Combine(Path.GetTempPath(), $"ssh_askpass_{Guid.NewGuid():N}.cmd");
                    File.WriteAllText(askpassScript, $"@echo off\r\necho {plainPass.Replace("&", "^&").Replace("%", "%%")}\r\n");
                }

                string finalCommand = $"ssh {args}{portArg}{userHost}";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c title SSH: {connection.Name} && {finalCommand} && pause",
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                if (!string.IsNullOrEmpty(askpassScript))
                {
                    psi.EnvironmentVariables["SSH_ASKPASS"] = askpassScript;
                    psi.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
                    psi.EnvironmentVariables["DISPLAY"] = "dummy:0";
                }

                var p = Process.Start(psi);
                if (p != null && !string.IsNullOrEmpty(askpassScript))
                {
                    p.EnableRaisingEvents = true;
                    p.Exited += (s, e) =>
                    {
                        try { if (File.Exists(askpassScript)) File.Delete(askpassScript); } catch { }
                    };
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to launch SSH session: {ex.Message}", "SSH Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        public static async Task<string> ExecuteBackgroundCommandAsync(SavedConnection connection, string script)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var connInfo = BuildConnectionInfo(connection);
                    using var client = new SshClient(connInfo);
                    client.Connect();

                    if (!client.IsConnected)
                    {
                        return "Error: Could not establish SSH connection to host.";
                    }

                    using var cmd = client.CreateCommand(script);
                    string output = cmd.Execute();
                    string error = cmd.Error;

                    if (!string.IsNullOrEmpty(error))
                    {
                        output += (!string.IsNullOrEmpty(output) ? "\n" : "") + "Error Output:\n" + error;
                    }

                    return string.IsNullOrWhiteSpace(output) ? "Command executed successfully with no output." : output;
                }
                catch (Exception ex)
                {
                    return $"SSH Execution Error: {ex.Message}";
                }
            });
        }
    }
}
