using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FreeRdpWrapper.Models;
using FreeRdpWrapper.UI;

namespace FreeRdpWrapper.Services
{
    public static class RemoteScriptRunner
    {
        public static async Task<string> ExecuteScriptAsync(SavedConnection connection, string scriptContent)
        {
            if (connection.Protocol == ConnectionProtocol.SSH)
            {
                return await SshLauncher.ExecuteBackgroundCommandAsync(connection, scriptContent);
            }
            else
            {
                return await ExecuteWinRmScriptAsync(connection, scriptContent);
            }
        }

        public static async Task<string> ExecuteWinRmScriptAsync(SavedConnection connection, string scriptContent)
        {
            try
            {
                string host = connection.Host.Trim();
                if (host.Contains(":"))
                {
                    host = host.Split(':')[0].Trim();
                }

                if (string.IsNullOrWhiteSpace(host))
                {
                    return "Error: Host address is empty.";
                }

                string user = connection.User ?? string.Empty;
                string pass = connection.Pass ?? string.Empty;

                // Handle domain\user or user@domain if domain is set separately
                if (!string.IsNullOrEmpty(connection.Domain) && !user.Contains("\\") && !user.Contains("@"))
                {
                    user = $"{connection.Domain}\\{user}";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command -",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                var sbCommands = new StringBuilder();
                sbCommands.AppendLine("$ErrorActionPreference = 'Stop'");
                sbCommands.AppendLine("$ProgressPreference = 'SilentlyContinue'");
                sbCommands.AppendLine("try {");

                // Prepare script text safely as a here-string
                // Escape any '@ or @' in the scriptContent
                string safeScript = scriptContent.Replace("'@", "' @").Replace("@'", "@ '");
                sbCommands.AppendLine($"$scriptContent = @'\n{safeScript}\n'@");
                sbCommands.AppendLine("$scriptBlock = [ScriptBlock]::Create($scriptContent)");

                bool isLocal = string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(host, Environment.MachineName, StringComparison.OrdinalIgnoreCase);

                if (isLocal)
                {
                    sbCommands.AppendLine("& $scriptBlock");
                }
                else
                {
                    sbCommands.AppendLine("$sessionOpt = New-PSSessionOption -SkipCACheck -SkipCNCheck -SkipRevocationCheck -OperationTimeout 120000");

                    if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
                    {
                        // Escape single quotes in user and pass for PowerShell literal
                        string escapedUser = user.Replace("'", "''");
                        string escapedPass = pass.Replace("'", "''");

                        sbCommands.AppendLine($"$secPass = ConvertTo-SecureString '{escapedPass}' -AsPlainText -Force");
                        sbCommands.AppendLine($"$cred = New-Object System.Management.Automation.PSCredential ('{escapedUser}', $secPass)");
                        sbCommands.AppendLine($"Invoke-Command -ComputerName '{host}' -Credential $cred -SessionOption $sessionOpt -ScriptBlock $scriptBlock");
                    }
                    else
                    {
                        // Negotiate with current Windows session / Kerberos
                        sbCommands.AppendLine($"Invoke-Command -ComputerName '{host}' -SessionOption $sessionOpt -ScriptBlock $scriptBlock");
                    }
                }

                sbCommands.AppendLine("} catch {");
                sbCommands.AppendLine("    Write-Error $_.Exception.Message");
                sbCommands.AppendLine("}");

                await process.StandardInput.WriteAsync(sbCommands.ToString());
                process.StandardInput.Close();

                string output = await process.StandardOutput.ReadToEndAsync();
                string err = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(err))
                {
                    var sbResult = new StringBuilder();
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        sbResult.AppendLine(output);
                    }
                    sbResult.AppendLine("⚠️ PowerShell Remoting / WinRM Error:");
                    sbResult.AppendLine(err.Trim());

                    // Helpful diagnostic hints for common WinRM errors
                    if (err.Contains("WinRM cannot process the request") || err.Contains("WS-Management service") || err.Contains("10061") || err.Contains("WinRM"))
                    {
                        sbResult.AppendLine("\n💡 Troubleshooting Tip for Windows / RDP targets:");
                        sbResult.AppendLine("• Ensure WinRM is enabled on the target Windows server (Run 'winrm quickconfig' or 'Enable-PSRemoting -Force').");
                        sbResult.AppendLine("• Ensure port 5985 (HTTP) or 5986 (HTTPS) is allowed in Windows Firewall.");
                        sbResult.AppendLine("• If using workgroup/local accounts, add target to TrustedHosts: Set-Item WSMan:\\localhost\\Client\\TrustedHosts -Value '*' -Force");
                    }
                    return sbResult.ToString();
                }

                return string.IsNullOrWhiteSpace(output) ? "Command executed successfully with no output." : output;
            }
            catch (Exception ex)
            {
                return $"Execution failure: {ex.Message}";
            }
        }
    }
}
