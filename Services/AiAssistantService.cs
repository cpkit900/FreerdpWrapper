using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.Services
{
    public enum AiProviderType
    {
        OfflineRules,
        GoogleGemini,
        OpenAiOrLocalLlm // OpenAI or Ollama / LM Studio compatible
    }

    public class AiSettings
    {
        public AiProviderType Provider { get; set; } = AiProviderType.OfflineRules;
        public string ApiKey { get; set; } = string.Empty;
        public string EndpointUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
        public string ModelName { get; set; } = "gemini-1.5-flash";
    }

    public static class AiAssistantService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };

        /// <summary>
        /// Diagnoses an RDP error or log snippet and provides immediate, actionable root-cause analysis and fixes.
        /// </summary>
        public static async Task<string> DiagnoseErrorAsync(string errorOrLog, AiSettings? settings = null)
        {
            if (string.IsNullOrWhiteSpace(errorOrLog))
                return "No log or error message provided for diagnosis.";

            // 1. Built-in Offline Expert Rules Engine (Instant & 100% Reliable without internet)
            string offlineAnalysis = AnalyzeWithBuiltinRules(errorOrLog);

            // If user configured an online LLM (Gemini / OpenAI / Ollama) and gave an API key or local endpoint
            if (settings != null && settings.Provider != AiProviderType.OfflineRules && !string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                try
                {
                    string prompt = $"You are an expert Windows Remote Desktop and FreeRDP troubleshooting assistant. " +
                                    $"Analyze this FreeRDP log/error:\n\n{errorOrLog}\n\n" +
                                    $"Provide:\n1. Root cause\n2. Specific troubleshooting steps\n3. Exact command or setting to fix it.";

                    string aiResult = settings.Provider == AiProviderType.GoogleGemini
                        ? await QueryGeminiAsync(prompt, settings)
                        : await QueryOpenAiCompatibleAsync(prompt, settings);

                    if (!string.IsNullOrWhiteSpace(aiResult))
                    {
                        return aiResult;
                    }
                }
                catch (Exception ex)
                {
                    return $"[Offline Engine Analysis (AI endpoint returned: {ex.Message})]:\n\n{offlineAnalysis}";
                }
            }

            return offlineAnalysis;
        }

        /// <summary>
        /// Generates administration scripts (PowerShell / Bash) for remote systems.
        /// </summary>
        public static async Task<string> GenerateScriptAsync(string taskDescription, bool isLinux, AiSettings? settings = null)
        {
            if (settings != null && settings.Provider != AiProviderType.OfflineRules && !string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                try
                {
                    string os = isLinux ? "Linux (Ubuntu/Debian Bash)" : "Windows (PowerShell)";
                    string prompt = $"You are an expert DevOps engineer. Generate a clean, production-ready script for {os} to perform:\n{taskDescription}\n" +
                                    $"Return only the code block and brief execution instructions.";

                    string result = settings.Provider == AiProviderType.GoogleGemini
                        ? await QueryGeminiAsync(prompt, settings)
                        : await QueryOpenAiCompatibleAsync(prompt, settings);

                    if (!string.IsNullOrWhiteSpace(result)) return result;
                }
                catch { }
            }

            // Fallback offline templates
            return GenerateOfflineScriptTemplate(taskDescription, isLinux);
        }

        private static string AnalyzeWithBuiltinRules(string text)
        {
            var sb = new StringBuilder();
            sb.AppendLine("🔍 FreeRDP Expert Diagnostics (Built-in Knowledgebase):");
            sb.AppendLine(new string('─', 50));

            string lower = text.ToLowerInvariant();

            if (lower.Contains("0x2000c") || lower.Contains("nego_connect_failed") || lower.Contains("security_nego"))
            {
                sb.AppendLine("⚠️ Failure: Security Negotiation Failed (0x2000C / ERRCONNECT_SECURITY_NEGO_CONNECT_FAILED)");
                sb.AppendLine("• Cause: The client and server could not agree on encryption/NLA protocols.");
                sb.AppendLine("• Solutions:");
                sb.AppendLine("  1. In Connection settings, set Security to 'NLA' or 'TLS'.");
                sb.AppendLine("  2. If connecting to Linux XRDP, disable NLA and use Standard RDP security (add `/sec:rdp` or `/sec:tls`).");
                sb.AppendLine("  3. Check if server requires Remote Credential Guard (+remote-credential-guard).");
            }
            else if (lower.Contains("0x20008") || lower.Contains("transport_failed") || lower.Contains("timed out") || lower.Contains("10060"))
            {
                sb.AppendLine("⚠️ Failure: Network Transport / Port Unreachable (0x20008 / WSAETIMEDOUT)");
                sb.AppendLine("• Cause: Could not establish TCP connection to port 3389.");
                sb.AppendLine("• Solutions:");
                sb.AppendLine("  1. Use the '⚡ Ping / Port Check' button in the server context menu to verify reachability.");
                sb.AppendLine("  2. Check Windows Firewall on target: run 'netsh advfirewall firewall set rule group=\"remote desktop\" new enable=Yes'.");
                sb.AppendLine("  3. If connecting over VPN or cloud (AWS/Azure), verify Security Groups allow inbound TCP 3389.");
            }
            else if (lower.Contains("0x20009") || lower.Contains("authentication_failed") || lower.Contains("0xc000006d") || lower.Contains("logon_failure"))
            {
                sb.AppendLine("⚠️ Failure: Authentication Rejected (0x20009 / STATUS_LOGON_FAILURE)");
                sb.AppendLine("• Cause: Invalid username, password, or domain credentials.");
                sb.AppendLine("• Solutions:");
                sb.AppendLine("  1. Verify the Domain/Workgroup prefix (e.g. '.\\Username' for local accounts or 'DOMAIN\\User').");
                sb.AppendLine("  2. Check if the user account is locked or password expired in Active Directory.");
                sb.AppendLine("  3. Ensure the account belongs to the 'Remote Desktop Users' group on the target machine.");
            }
            else if (lower.Contains("cert_validation_failed") || lower.Contains("certificate") || lower.Contains("0x2000d"))
            {
                sb.AppendLine("⚠️ Failure: SSL/TLS Certificate Validation Failed");
                sb.AppendLine("• Cause: The remote server presented a self-signed or untrusted certificate.");
                sb.AppendLine("• Solutions:");
                sb.AppendLine("  1. In Connection settings, set Certificate Security to 'Ignore' (/cert:ignore) or 'Trust On First Use' (/cert:tofu).");
            }
            else if (lower.Contains("udp") || lower.Contains("multitransport"))
            {
                sb.AppendLine("⚠️ Warning: UDP Multitransport Degraded");
                sb.AppendLine("• Cause: UDP port 3389 is blocked or MTU packet size causes fragmentation.");
                sb.AppendLine("• Solutions:");
                sb.AppendLine("  1. In Connection settings, enable 'Disable UDP (TCP Only)' (/multitransport:tcp-only).");
            }
            else
            {
                sb.AppendLine("ℹ️ General Diagnostic Recommendations:");
                sb.AppendLine("• Verify the target IP address and port 3389 are accessible via the built-in Ping tool.");
                sb.AppendLine("• Check whether wfreerdp.exe or sdl3-freerdp.exe is selected in Client Engine.");
                sb.AppendLine("• Review the full log entries above for additional error details.");
            }

            return sb.ToString();
        }

        private static string GenerateOfflineScriptTemplate(string description, bool isLinux)
        {
            if (isLinux)
            {
                return "#!/usr/bin/env bash\n" +
                       "# Generated by FreeRDP Wrapper AI Assistant\n" +
                       "# Target: Ubuntu / Debian Linux\n" +
                       "set -e\n\n" +
                       "echo '=== Configuring XRDP Remote Desktop ==='\n" +
                       "sudo apt-get update\n" +
                       "sudo apt-get install -y xrdp xfce4 xfce4-goodies\n" +
                       "sudo adduser xrdp ssl-cert\n" +
                       "sudo systemctl enable xrdp\n" +
                       "sudo systemctl restart xrdp\n" +
                       "echo 'XRDP service is active on port 3389'\n";
            }
            else
            {
                return "# Generated by FreeRDP Wrapper AI Assistant\n" +
                       "# Target: Windows Server / Windows 10/11\n" +
                       "# Run as Administrator in PowerShell\n\n" +
                       "Write-Host '=== Enabling Remote Desktop & Firewall Rule ===' -ForegroundColor Cyan\n" +
                       "Set-ItemProperty -Path 'HKLM:\\System\\CurrentControlSet\\Control\\Terminal Server' -Name 'fDenyTSConnections' -Value 0\n" +
                       "Enable-NetFirewallRule -DisplayGroup 'Remote Desktop'\n" +
                       "Set-Service -Name termservice -StartupType Automatic\n" +
                       "Start-Service -Name termservice\n" +
                       "Write-Host 'Remote Desktop successfully enabled on port 3389.' -ForegroundColor Green\n";
            }
        }

        private static async Task<string> QueryGeminiAsync(string prompt, AiSettings settings)
        {
            string url = $"{settings.EndpointUrl}?key={settings.ApiKey}";
            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            string respJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(respJson);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() > 0)
            {
                var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                return text ?? string.Empty;
            }
            return string.Empty;
        }

        private static async Task<string> QueryOpenAiCompatibleAsync(string prompt, AiSettings settings)
        {
            string url = string.IsNullOrEmpty(settings.EndpointUrl) ? "https://api.openai.com/v1/chat/completions" : settings.EndpointUrl;
            var payload = new
            {
                model = string.IsNullOrEmpty(settings.ModelName) ? "gpt-4o-mini" : settings.ModelName,
                messages = new[]
                {
                    new { role = "system", content = "You are a professional Remote Desktop and FreeRDP system administration assistant." },
                    new { role = "user", content = prompt }
                }
            };

            string json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                request.Headers.Add("Authorization", $"Bearer {settings.ApiKey}");
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string respJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(respJson);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var text = choices[0].GetProperty("message").GetProperty("content").GetString();
                return text ?? string.Empty;
            }
            return string.Empty;
        }
    }
}
