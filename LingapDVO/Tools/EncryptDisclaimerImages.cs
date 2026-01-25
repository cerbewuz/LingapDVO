// =====================================================
// DISCLAIMER IMAGE ENCRYPTION TOOL
// This file is DEPRECATED - Use the DashboardController endpoint instead
// =====================================================
//
// HOW TO USE:
// Use the admin endpoint: POST /Dashboard/EncryptDisclaimerImages
// This ensures images are encrypted using the system's AesEncryptionService
// which is properly configured and consistent across the application.
//
// The GetDisclaimerImage endpoint in DashboardController decrypts them on-the-fly.
// =====================================================

using System;
using System.IO;
using LingapDVO.Services;

namespace LingapDVO.Tools
{
    /// <summary>
    /// DEPRECATED: Use DashboardController.EncryptDisclaimerImages() endpoint instead.
    /// This wrapper delegates to the system's AesEncryptionService.
    /// </summary>
    public class DisclaimerImageEncryptor
    {
        private readonly IAesEncryptionService _aesService;

        public DisclaimerImageEncryptor(IAesEncryptionService aesService)
        {
            _aesService = aesService ?? throw new ArgumentNullException(nameof(aesService));
        }

        public byte[] Encrypt(byte[] plainBytes)
        {
            using var inputStream = new MemoryStream(plainBytes);
            return _aesService.EncryptStream(inputStream);
        }

        public byte[] Decrypt(byte[] encryptedBytes)
        {
            return _aesService.DecryptFile(encryptedBytes);
        }

        public int EncryptFolder(string sourceFolder, string encryptedFolder)
        {
            if (!Directory.Exists(encryptedFolder))
            {
                Directory.CreateDirectory(encryptedFolder);
            }

            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            int count = 0;

            foreach (var file in Directory.GetFiles(sourceFolder))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (Array.IndexOf(imageExtensions, ext) >= 0)
                {
                    string fileName = Path.GetFileName(file);
                    string encryptedPath = Path.Combine(encryptedFolder, fileName + ".enc");

                    byte[] originalBytes = File.ReadAllBytes(file);
                    byte[] encryptedBytes = Encrypt(originalBytes);
                    File.WriteAllBytes(encryptedPath, encryptedBytes);

                    Console.WriteLine($"Encrypted: {fileName} -> {fileName}.enc ({encryptedBytes.Length} bytes)");
                    count++;
                }
            }

            return count;
        }
    }
}
