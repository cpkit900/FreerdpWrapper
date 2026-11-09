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
        if (args.Length > 0 && args[0] == "--test-phase1")
        {
            RunSelfTests();
            return;
        }

        ApplicationConfiguration.Initialize();

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

        Application.Run(new Form1());
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

        Console.WriteLine("=== ALL PHASE 1 TESTS PASSED SUCCESSFULLY! ===");
        Environment.Exit(0);
    }
}
