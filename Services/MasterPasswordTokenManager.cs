using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FreeRdpWrapper.Services
{
    public static class MasterPasswordTokenManager
    {
        public static string TokenFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FreeRdpWrapper",
            "master.bin"
        );

        private static readonly byte[] TokenEntropy = Encoding.UTF8.GetBytes("FreeRdpWrapper_MasterToken_Entropy_v1");

        public static bool IsTokenSaved()
        {
            return File.Exists(TokenFilePath);
        }

        public static bool TryAutoUnlock(string targetHash)
        {
            if (string.IsNullOrEmpty(targetHash) || !File.Exists(TokenFilePath))
                return false;

            try
            {
                byte[] protectedBytes = File.ReadAllBytes(TokenFilePath);
                byte[] rawBytes = ProtectedData.Unprotect(protectedBytes, TokenEntropy, DataProtectionScope.CurrentUser);
                string savedHash = Encoding.UTF8.GetString(rawBytes);

                return string.Equals(savedHash, targetHash, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        public static void SaveToken(string passwordHash)
        {
            try
            {
                string dir = Path.GetDirectoryName(TokenFilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                byte[] rawBytes = Encoding.UTF8.GetBytes(passwordHash);
                byte[] protectedBytes = ProtectedData.Protect(rawBytes, TokenEntropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(TokenFilePath, protectedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving master token: {ex.Message}");
            }
        }

        public static void ClearToken()
        {
            try
            {
                if (File.Exists(TokenFilePath))
                {
                    File.Delete(TokenFilePath);
                }
            }
            catch { }
        }
    }
}

