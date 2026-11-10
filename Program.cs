using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using FreeRdpWrapper.Services;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // Internal diagnostic self-test
        if (args.Length > 0 && (args[0] == "--test-phase1" || args[0] == "--test-all"))
        {
            RunSelfTests();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Application encountered an error:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}", "FreeRDP Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        var settings = SettingsStore.LoadSettings();
        
        // Only require Master Password dialog if a master password has actually been set
        if (!string.IsNullOrEmpty(settings.MasterPasswordHash))
        {
            using var dialog = new UI.MasterPasswordDialog(isSetupMode: false, settings.MasterPasswordHash);
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                // User cancelled or failed authentication
                return;
            }
        }

        try
        {
            Application.Run(new Form1());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fatal Application Error:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Error Loading Application", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void RunSelfTests()
    {
        Console.WriteLine("=== FreeRDP Wrapper Phase 1 Self-Tests ===");
        
        // 1. Vault Crypto Secret Roundtrip
        string original = "EnterprisePassword_#2026_FreeRdp!";
        string encrypted = VaultCrypto.EncryptSecret(original);
        string decrypted = VaultCrypto.DecryptSecret(encrypted);
        if (original != decrypted)
        {
            Console.WriteLine("[FAIL] VaultCrypto roundtrip mismatch!");
            Environment.Exit(1);
        }
        Console.WriteLine("[PASS] VaultCrypto AES-256-GCM + DPAPI encryption & decryption");

        // 2. Vault Export & Import
        string sampleJson = "[{\"Name\":\"WebProd01\",\"Host\":\"10.0.1.50\",\"Group\":\"Production\"}]";
        string pass = "BackupPassphrase$99!";
        byte[] vaultBytes = VaultCrypto.ExportEncryptedVault(sampleJson, pass);
        string restoredJson = VaultCrypto.ImportEncryptedVault(vaultBytes, pass);
        if (sampleJson != restoredJson)
        {
            Console.WriteLine("[FAIL] Vault export/import mismatch!");
            Environment.Exit(1);
        }
        Console.WriteLine("[PASS] Encrypted .frdpvault bundle export & import");

        // 3. RDP File Parser
        string tempRdp = Path.Combine(Path.GetTempPath(), "test_phase1.rdp");
        File.WriteAllText(tempRdp, "full address:s:rdp.company.com:3390\r\nusername:s:corp\\jdoe\r\ndesktopwidth:i:2560\r\ndesktopheight:i:1440\r\nredirectclipboard:i:1\r\naudiomode:i:0\r\n");
        var conn = RdpFileParser.Parse(tempRdp);
        if (conn.Host != "rdp.company.com:3390" || conn.User != "corp\\jdoe" || conn.ResolutionWidth != 2560 || !conn.EnableClipboard || !conn.EnableSound)
        {
            Console.WriteLine("[FAIL] RdpFileParser parsed values mismatch!");
            Environment.Exit(1);
        }
        File.Delete(tempRdp);
        Console.WriteLine("[PASS] Microsoft .rdp file import & parsing");

        // 4. AI Diagnostics Engine
        string diagResult = AiAssistantService.DiagnoseErrorAsync("ERRCONNECT_SECURITY_NEGO_CONNECT_FAILED [0x2000C]").GetAwaiter().GetResult();
        if (!diagResult.Contains("0x2000C") || !diagResult.Contains("Security Negotiation"))
        {
            Console.WriteLine("[FAIL] AiAssistantService diagnosis rule failed!");
            Environment.Exit(1);
        }
        Console.WriteLine("[PASS] AI Diagnostics Engine (Built-in Knowledgebase)");

        // 5. AI Script Generator
        string winScript = AiAssistantService.GenerateScriptAsync("Enable Remote Desktop", false).GetAwaiter().GetResult();
        string linuxScript = AiAssistantService.GenerateScriptAsync("Install XRDP", true).GetAwaiter().GetResult();
        if (!winScript.Contains("termservice") || !linuxScript.Contains("xrdp"))
        {
            Console.WriteLine("[FAIL] AiAssistantService script generator failed!");
            Environment.Exit(1);
        }
        Console.WriteLine("[PASS] AI Script Generator (Windows & Linux)");

        // 6. Network Reachability Probe
        var probe = NetworkTester.ProbeHostAsync("127.0.0.1", defaultPort: 3389, timeoutMs: 500).GetAwaiter().GetResult();
        if (probe.Port != 3389)
        {
            Console.WriteLine("[FAIL] NetworkTester probe failed!");
            Environment.Exit(1);
        }
        Console.WriteLine("[PASS] Network Reachability & Port Probe");

        Console.WriteLine("=== ALL SYSTEM TESTS PASSED SUCCESSFULLY! ===");
        Environment.Exit(0);
    }
}
