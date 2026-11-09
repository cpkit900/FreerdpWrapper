using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FreeRdpWrapper.Services
{
    /// <summary>
    /// Enterprise-grade cryptographic vault using authenticated AES-256-GCM and PBKDF2 key derivation.
    /// Provides protection against eavesdropping, bit-flipping, and offline tampering.
    /// </summary>
    public static class VaultCrypto
    {
        private const int KeySizeBits = 256;
        private const int KeySizeBytes = KeySizeBits / 8; // 32 bytes
        private const int NonceSizeBytes = 12;            // 96 bits for GCM
        private const int TagSizeBytes = 16;              // 128 bits authentication tag
        private const int SaltSizeBytes = 16;             // 128 bits salt for PBKDF2
        private const int Pbkdf2Iterations = 100_000;      // OWASP recommended benchmark

        // Entropy for machine-level DPAPI key protection
        private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("FreeRdpWrapper_Vault_Entropy_v2");

        /// <summary>
        /// Encrypts plain text using AES-256-GCM with a specified key.
        /// Payload layout: [12-byte Nonce] + [16-byte Tag] + [Ciphertext]
        /// </summary>
        public static byte[] EncryptWithKey(byte[] plainBytes, byte[] key)
        {
            if (key == null || key.Length != KeySizeBytes)
                throw new ArgumentException($"Encryption key must be exactly {KeySizeBytes} bytes (256-bit).", nameof(key));

            byte[] nonce = new byte[NonceSizeBytes];
            RandomNumberGenerator.Fill(nonce);

            byte[] tag = new byte[TagSizeBytes];
            byte[] cipherBytes = new byte[plainBytes.Length];

            using (var aesGcm = new AesGcm(key, TagSizeBytes))
            {
                aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
            }

            // Pack into single buffer: [Nonce (12)] + [Tag (16)] + [Ciphertext (N)]
            byte[] result = new byte[NonceSizeBytes + TagSizeBytes + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
            Buffer.BlockCopy(tag, 0, result, NonceSizeBytes, TagSizeBytes);
            Buffer.BlockCopy(cipherBytes, 0, result, NonceSizeBytes + TagSizeBytes, cipherBytes.Length);

            return result;
        }

        /// <summary>
        /// Decrypts AES-256-GCM payload with authenticated integrity verification.
        /// </summary>
        public static byte[] DecryptWithKey(byte[] encryptedPayload, byte[] key)
        {
            if (key == null || key.Length != KeySizeBytes)
                throw new ArgumentException($"Decryption key must be exactly {KeySizeBytes} bytes (256-bit).", nameof(key));

            if (encryptedPayload == null || encryptedPayload.Length < (NonceSizeBytes + TagSizeBytes))
                throw new CryptographicException("Ciphertext payload is truncated or invalid.");

            byte[] nonce = new byte[NonceSizeBytes];
            byte[] tag = new byte[TagSizeBytes];
            int cipherLength = encryptedPayload.Length - NonceSizeBytes - TagSizeBytes;
            byte[] cipherBytes = new byte[cipherLength];

            Buffer.BlockCopy(encryptedPayload, 0, nonce, 0, NonceSizeBytes);
            Buffer.BlockCopy(encryptedPayload, NonceSizeBytes, tag, 0, TagSizeBytes);
            Buffer.BlockCopy(encryptedPayload, NonceSizeBytes + TagSizeBytes, cipherBytes, 0, cipherLength);

            byte[] plainBytes = new byte[cipherLength];
            using (var aesGcm = new AesGcm(key, TagSizeBytes))
            {
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }

            return plainBytes;
        }

        /// <summary>
        /// Derives a 256-bit AES key from a passphrase and salt using PBKDF2 with HMAC-SHA256.
        /// </summary>
        public static byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                KeySizeBytes);
        }

        /// <summary>
        /// Encrypts sensitive string using transparent local machine DPAPI key sealing.
        /// Output is URL/storage-safe Base64.
        /// </summary>
        public static string EncryptSecret(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                // Generate a fresh random data encryption key (DEK)
                byte[] dek = new byte[KeySizeBytes];
                RandomNumberGenerator.Fill(dek);

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedData = EncryptWithKey(plainBytes, dek);

                // Seal the DEK with Windows DPAPI (CurrentUser scope)
                byte[] protectedDek = ProtectedData.Protect(dek, DpapiEntropy, DataProtectionScope.CurrentUser);

                // Package format: [Version: 1 byte (0x02)] + [Length of protected DEK: 2 bytes] + [Protected DEK] + [Encrypted Data]
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms);
                writer.Write((byte)0x02); // Vault v2 marker
                writer.Write((ushort)protectedDek.Length);
                writer.Write(protectedDek);
                writer.Write(encryptedData);

                return "vault2:" + Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VaultCrypto] Secret encryption failed: {ex.Message}");
                return CryptoHelper.EncryptString(plainText); // Fallback to v1 DPAPI
            }
        }

        /// <summary>
        /// Decrypts a secret that was encrypted with either Vault v2 (AES-GCM + DPAPI) or legacy v1 DPAPI.
        /// </summary>
        public static string DecryptSecret(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            if (cipherText.StartsWith("vault2:", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    byte[] raw = Convert.FromBase64String(cipherText.Substring("vault2:".Length));
                    using var ms = new MemoryStream(raw);
                    using var reader = new BinaryReader(ms);

                    byte version = reader.ReadByte();
                    if (version != 0x02)
                        throw new CryptographicException("Unsupported vault version marker.");

                    ushort dekLen = reader.ReadUInt16();
                    byte[] protectedDek = reader.ReadBytes(dekLen);
                    byte[] encryptedData = reader.ReadBytes((int)(ms.Length - ms.Position));

                    // Unseal DEK
                    byte[] dek = ProtectedData.Unprotect(protectedDek, DpapiEntropy, DataProtectionScope.CurrentUser);
                    byte[] plainBytes = DecryptWithKey(encryptedData, dek);

                    return Encoding.UTF8.GetString(plainBytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VaultCrypto] Vault v2 decryption failed: {ex.Message}");
                    return string.Empty;
                }
            }

            // Legacy v1 fallback (standard DPAPI)
            return CryptoHelper.DecryptString(cipherText);
        }

        /// <summary>
        /// Exports data to an encrypted portable vault file (.frdpvault) protected by a user passphrase.
        /// Layout: [Magic "FRDPVAULT1" 10 bytes] + [Salt (16 bytes)] + [AES-256-GCM Payload]
        /// </summary>
        public static byte[] ExportEncryptedVault(string jsonContent, string passphrase)
        {
            if (string.IsNullOrEmpty(passphrase))
                throw new ArgumentException("Passphrase cannot be empty for vault export.", nameof(passphrase));

            byte[] salt = new byte[SaltSizeBytes];
            RandomNumberGenerator.Fill(salt);

            byte[] key = DeriveKeyFromPassword(passphrase, salt);
            byte[] plainBytes = Encoding.UTF8.GetBytes(jsonContent);
            byte[] encryptedPayload = EncryptWithKey(plainBytes, key);

            byte[] magic = Encoding.ASCII.GetBytes("FRDPVAULT1");
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write(magic);
            writer.Write(salt);
            writer.Write(encryptedPayload);

            return ms.ToArray();
        }

        /// <summary>
        /// Imports and decrypts a portable vault file (.frdpvault) using the given passphrase.
        /// </summary>
        public static string ImportEncryptedVault(byte[] vaultBytes, string passphrase)
        {
            if (vaultBytes == null || vaultBytes.Length < 38)
                throw new CryptographicException("Invalid or corrupted vault file.");

            using var ms = new MemoryStream(vaultBytes);
            using var reader = new BinaryReader(ms);

            byte[] magic = reader.ReadBytes(10);
            string magicStr = Encoding.ASCII.GetString(magic);
            if (magicStr != "FRDPVAULT1")
                throw new CryptographicException("Invalid file format. Expected FreeRDP Vault header.");

            byte[] salt = reader.ReadBytes(SaltSizeBytes);
            byte[] encryptedPayload = reader.ReadBytes((int)(ms.Length - ms.Position));

            byte[] key = DeriveKeyFromPassword(passphrase, salt);
            byte[] plainBytes = DecryptWithKey(encryptedPayload, key);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
