using System.Security.Cryptography;
using System.Text;

namespace LingapDVO.Services
{
    public interface IAesEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string encryptedText);
        byte[] EncryptStream(Stream inputStream);
        byte[] DecryptFile(byte[] encryptedData);
        string EncryptFilename(string originalFileName);
        string DecryptFilename(string encryptedFileName);
        string EncryptTimestamp(string timestamp);
    }

    public class AesEncryptionService : IAesEncryptionService
    {
        private readonly byte[] _aesKey;

        public AesEncryptionService(IConfiguration configuration)
        {
            string keyHex = configuration["Security:AesEncryption:Key"]
                ?? throw new InvalidOperationException("AES encryption key not found in configuration");

            // Clean the key - remove any whitespace or special characters
            keyHex = keyHex.Trim().Replace(" ", "").Replace("-", "").Replace(":", "");

            if (string.IsNullOrWhiteSpace(keyHex))
                throw new InvalidOperationException("AES encryption key is empty");

            // Convert with automatic padding
            _aesKey = SafeConvertHexStringToByteArray(keyHex);

            if (_aesKey.Length != 32)
                throw new InvalidOperationException($"AES key must be 32 bytes (256 bits). Current: {_aesKey.Length} bytes");
        }

        private static byte[] SafeConvertHexStringToByteArray(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Hex string cannot be null or empty");

            // Clean the hex string
            hex = hex.Trim().Replace(" ", "").Replace("-", "").Replace(":", "");

            // Ensure even length by padding with leading zero if needed
            if (hex.Length % 2 != 0)
            {
                hex = "0" + hex;
            }

            // Validate hex format
            if (!System.Text.RegularExpressions.Regex.IsMatch(hex, @"^[0-9A-Fa-f]+$"))
            {
                throw new ArgumentException("Hex string contains invalid characters");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Encrypts plain text using AES-256 encryption
        /// </summary>
        /// <param name="plainText">Plain text to encrypt</param>
        /// <returns>Base64-encoded encrypted string</returns>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var memoryStream = new MemoryStream();

            memoryStream.Write(aes.IV, 0, aes.IV.Length);

            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cryptoStream))
            {
                writer.Write(plainText);
            }

            byte[] encryptedData = memoryStream.ToArray();
            return Convert.ToBase64String(encryptedData);
        }

        /// <summary>
        /// Decrypts encrypted text using AES-256 decryption
        /// </summary>
        /// <param name="encryptedText">Base64-encoded encrypted string</param>
        /// <returns>Decrypted plain text</returns>
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            // Check if the text looks like it could be encrypted (valid Base64)
            // Encrypted text should be at least 17 bytes (16 IV + at least 1 byte data) = ~24 chars in Base64
            if (!IsLikelyEncrypted(encryptedText))
            {
                // Return original text if it doesn't look encrypted (legacy plain text data)
                return encryptedText;
            }

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                // Encrypted data must be at least 17 bytes (16 IV + minimum encrypted block)
                if (encryptedBytes.Length < 17)
                {
                    // Too short to be encrypted, return original
                    return encryptedText;
                }

                using var aes = Aes.Create();
                aes.Key = _aesKey;

                byte[] iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.IV = iv;

                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(encryptedBytes, 16, encryptedBytes.Length - 16);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);

                return reader.ReadToEnd();
            }
            catch (FormatException)
            {
                // Not valid Base64, return original text (legacy plain text data)
                return encryptedText;
            }
            catch (CryptographicException)
            {
                // Decryption failed (wrong key or corrupted), return original text
                return encryptedText;
            }
        }

        /// <summary>
        /// Checks if a string looks like it could be AES encrypted data (valid Base64 with sufficient length)
        /// </summary>
        private bool IsLikelyEncrypted(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // AES encrypted text should be at least ~24 chars in Base64 (17 bytes minimum)
            if (text.Length < 24)
                return false;

            // Check if it's valid Base64 format
            // Base64 uses A-Z, a-z, 0-9, +, /, and = for padding
            foreach (char c in text)
            {
                if (!((c >= 'A' && c <= 'Z') || 
                      (c >= 'a' && c <= 'z') || 
                      (c >= '0' && c <= '9') || 
                      c == '+' || c == '/' || c == '='))
                {
                    return false;
                }
            }

            // Check length is valid for Base64 (multiple of 4 or with proper padding)
            int paddingCount = 0;
            if (text.EndsWith("==")) paddingCount = 2;
            else if (text.EndsWith("=")) paddingCount = 1;

            return (text.Length % 4 == 0) || (text.Length - paddingCount) % 4 == 0;
        }

        /// <summary>
        /// Encrypts a stream (file content) using AES-256 encryption
        /// </summary>
        /// <param name="inputStream">Stream to encrypt</param>
        /// <returns>Byte array of encrypted data with IV prepended</returns>
        public byte[] EncryptStream(Stream inputStream)
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var memoryStream = new MemoryStream();
            memoryStream.Write(aes.IV, 0, aes.IV.Length);

            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                inputStream.CopyTo(cryptoStream);
            }

            return memoryStream.ToArray();
        }

        /// <summary>
        /// Decrypts encrypted file data
        /// </summary>
        /// <param name="encryptedData">Encrypted byte array with IV prepended</param>
        /// <returns>Decrypted byte array</returns>
        public byte[] DecryptFile(byte[] encryptedData)
        {
            using var memoryStream = new MemoryStream(encryptedData);
            using var aes = Aes.Create();
            aes.Key = _aesKey;

            byte[] iv = new byte[16];
            memoryStream.Read(iv, 0, 16);
            aes.IV = iv;

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var outputStream = new MemoryStream();

            cryptoStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }

        /// <summary>
        /// Encrypts timestamp string using AES-256
        /// </summary>
        /// <param name="timestamp">Timestamp string to encrypt</param>
        /// <returns>Base64-encoded encrypted timestamp</returns>
        public string EncryptTimestamp(string timestamp)
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            byte[] inputBytes = Encoding.UTF8.GetBytes(timestamp);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Encrypts the original filename (including extension) using AES-256 encryption
        /// Returns a filesystem-safe encrypted filename suitable for storage
        /// </summary>
        /// <param name="originalFileName">Original filename with extension (e.g., "document.pdf")</param>
        /// <returns>Encrypted filename in Base64 URL-safe format</returns>
        public string EncryptFilename(string originalFileName)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
                throw new ArgumentException("Filename cannot be null or empty");

            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var memoryStream = new MemoryStream();

            // Write IV first
            memoryStream.Write(aes.IV, 0, aes.IV.Length);

            // Encrypt the filename
            byte[] inputBytes = Encoding.UTF8.GetBytes(originalFileName);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            memoryStream.Write(encryptedBytes, 0, encryptedBytes.Length);

            // Convert to Base64 and make it filesystem-safe
            string base64 = Convert.ToBase64String(memoryStream.ToArray());
            // Replace characters that are not filesystem-safe
            string safeFilename = base64.Replace("+", "-").Replace("/", "_").Replace("=", "");

            return safeFilename;
        }

        /// <summary>
        /// Decrypts an encrypted filename back to its original form
        /// </summary>
        /// <param name="encryptedFileName">Encrypted filename (without .enc extension)</param>
        /// <returns>Original filename with extension</returns>
        public string DecryptFilename(string encryptedFileName)
        {
            if (string.IsNullOrWhiteSpace(encryptedFileName))
                throw new ArgumentException("Encrypted filename cannot be null or empty");

            // Restore Base64 characters
            string base64 = encryptedFileName.Replace("-", "+").Replace("_", "/");
            // Add padding if needed
            int padding = (4 - (base64.Length % 4)) % 4;
            base64 += new string('=', padding);

            byte[] encryptedData = Convert.FromBase64String(base64);

            using var aes = Aes.Create();
            aes.Key = _aesKey;

            // Extract IV (first 16 bytes)
            byte[] iv = new byte[16];
            Array.Copy(encryptedData, 0, iv, 0, 16);
            aes.IV = iv;

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            byte[] cipherText = new byte[encryptedData.Length - 16];
            Array.Copy(encryptedData, 16, cipherText, 0, cipherText.Length);

            byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
