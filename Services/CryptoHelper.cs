using System;
using System.Security.Cryptography;
using System.Text;

namespace FreeRdpWrapper.Services
{
    public static class CryptoHelper
    {
        // Entropy for added security. Can be any byte array, but must be the same for encryption and decryption.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("FreeRdpWrapper_Secure_Key_!@#");

        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption failed: {ex.Message}");
                // Return original string if encryption fails (fallback/graceful handling)
                return plainText; 
            }
        }

        public static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return encryptedText;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (FormatException)
            {
                 // Not base64, so it might be an older unencrypted password still in the system.
                 return encryptedText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption failed: {ex.Message}");
                return encryptedText;
            }
        }
    }
}
