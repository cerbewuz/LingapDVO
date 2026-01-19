using iText.Commons.Actions.Data;
using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace LingapDVO.Controllers
{
   
    public class Dashboard : Controller
    {
        public readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration _configuration;
        private readonly FormSubmissionSecurityService _securityService;
        private readonly ISessionConfigurationService _sessionConfig;
        private readonly IDateTimeService _dateTimeService;
        private readonly IMultiChannelNotificationService _notificationService;
        private readonly IAdminNotificationService _adminNotificationService;
        private readonly IAesEncryptionService _aesEncryptionService;

        public Dashboard(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            FormSubmissionSecurityService securityService,
            ISessionConfigurationService sessionConfig,
            IDateTimeService dateTimeService,
            IMultiChannelNotificationService notificationService,
            IAdminNotificationService adminNotificationService,
            IAesEncryptionService aesEncryptionService)
        {
            this.context = context;
            this.environment = environment;
            _configuration = configuration;
            _securityService = securityService;
            _sessionConfig = sessionConfig;
            _dateTimeService = dateTimeService;
            _notificationService = notificationService;
            _adminNotificationService = adminNotificationService;
            _aesEncryptionService = aesEncryptionService;
        }
    
        public IActionResult Index()
        {

            return View();
        }

        public IActionResult Landingpage()
        {
            return View();
        }

        public IActionResult Listofpartners()
        {
            return View();
        }

        public IActionResult Homepage()
        {
            // Prevent browser caching
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            var userIdString = HttpContext.Session.GetString("UserId");
            bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;

            if (string.IsNullOrEmpty(userIdString) && !isAuthenticated)
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            int userId = 0; // default
            if (!string.IsNullOrEmpty(userIdString))
            {
                // ? Convert session UserId (string) ? int
                int.TryParse(userIdString, out userId);
                ViewBag.Username = HttpContext.Session.GetString("Username");

                // Get profile picture from database
                var user = context.UserAccount.FirstOrDefault(u => u.Id == userId);
                ViewBag.Profilepicture = user?.Profilepicture ?? "";
            }
            else if (isAuthenticated)
            {
                string username = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value
                                   ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                                   ?? "User";
                ViewBag.Username = username;

                // Get profile picture from database if userId is available
                if (userId > 0)
                {
                    var user = context.UserAccount.FirstOrDefault(u => u.Id == userId);
                    ViewBag.Profilepicture = user?.Profilepicture ?? "";
                }
                else
                {
                    ViewBag.Profilepicture = "";
                }
            }

            // ? Check if user has completed verification
            var verification = context.VerifiedAccount.FirstOrDefault(v => v.UserId == userId);
            bool isVerified = verification != null;
            ViewBag.IsVerified = isVerified;

            // ? Now you can safely filter only the logged-in user's data
            var hospitalBills = context.HospitalAssistance
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.OtherAssistance
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var FuneralAssistance = context.FuneralAssistance
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Find the latest document overall
            var allDocs = new List<dynamic>();

            if (hospitalBills.Any())
                allDocs.Add(new { Type = "Hospital", Data = hospitalBills.First() });
            if (medicalLabForms.Any())
                allDocs.Add(new { Type = "Other", Data = medicalLabForms.First() });
            if (FuneralAssistance.Any())
                allDocs.Add(new { Type = "Funeral", Data = FuneralAssistance.First() });

            var latestDoc = allDocs
                .OrderByDescending(d => d.Data.CreatedAt)
                .FirstOrDefault();

            var viewModel = new CombinedFormsViewModel
            {
                HospitalAssistance = hospitalBills,
                OtherAssistance = medicalLabForms,
                FuneralAssistance = FuneralAssistance
            };

            ViewBag.LatestDoc = latestDoc;

            ViewBag.Firstname = HttpContext.Session.GetString("Firstname") ?? "";
       

            return View(viewModel);
        }


        public IActionResult Userprofile(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
             {
              return RedirectToAction("Landingpage", "Dashboard");
             }

             // Get UserId from session
             var userIdString = HttpContext.Session.GetString("UserId");
             if (int.TryParse(userIdString, out int userId))
             {
                 // Fetch data from VerifyAccount table
                 var verifyAccount = context.VerifiedAccount.FirstOrDefault(v => v.UserId == userId);

                 if (verifyAccount != null)
                 {
                     // Use data from VerifyAccount table (most up-to-date)
                     ViewBag.IDnumber = verifyAccount.IDnumber ?? "";
                     ViewBag.IDtype = verifyAccount.IDtype ?? "";
                     ViewBag.Firstname = verifyAccount.Firstname ?? "";
                     ViewBag.Middlename = verifyAccount.Middlename ?? "";
                     ViewBag.Lastname = verifyAccount.Lastname ?? "";
                     ViewBag.Suffix = verifyAccount.Suffix ?? "";
                     ViewBag.BlkLotStreet = verifyAccount.BlkLotStreet ?? "";
                     ViewBag.SubVill = verifyAccount.SubVill ?? "";
                     ViewBag.Barangay = verifyAccount.Barangay ?? "";
                     ViewBag.Dateofbirth = verifyAccount.Dateofbirth ?? "";
                     ViewBag.Gender = verifyAccount.Gender ?? "";
                     ViewBag.CivilStatus = verifyAccount.CivilStatus ?? "";
                     ViewBag.Phonenumber = verifyAccount.Phonenumber ?? "";
                 }
                 else
                 {
                     // Fallback to session if VerifyAccount doesn't exist
                     ViewBag.IDnumber = HttpContext.Session.GetString("IDnumber");
                     ViewBag.IDtype = HttpContext.Session.GetString("IDtype");
                     ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
                     ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
                     ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
                     ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
                     ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
                     ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
                     ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
                     ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");
                     ViewBag.Gender = HttpContext.Session.GetString("Gender");
                     ViewBag.CivilStatus = HttpContext.Session.GetString("CivilStatus");
                     ViewBag.Phonenumber = HttpContext.Session.GetString("Phonenumber");
                 }
             }

             ViewBag.Id = userIdString;
             ViewBag.Username = HttpContext.Session.GetString("Username");
             ViewBag.Email = HttpContext.Session.GetString("Email");
             ViewBag.SecurityQuestions = HttpContext.Session.GetString("SecurityQuestions");

             // Get profile picture from database
             if (int.TryParse(userIdString, out int userIdForPicture))
             {
                 var user = context.UserAccount.FirstOrDefault(u => u.Id == userIdForPicture);
                 ViewBag.Profilepicture = user?.Profilepicture ?? "";
             }
             else
             {
                 ViewBag.Profilepicture = HttpContext.Session.GetString("Profilepicture");
             }

             ViewBag.GenderList = new SelectList(new List<string> { "Male", "Female" }, ViewBag.Gender);
             ViewBag.SecurityQuestionslist = new SelectList(
                   new List<string> {
                   "What is your first pet's name?",
                  "What is your mother's maiden name?",
                   "What was your first school?"
                          },
                           ViewBag.SecurityQuestions
                       );

              ViewBag.Securityanswer = HttpContext.Session.GetString("Securityanswer");

                 return View();


        }

        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture(IFormFile profilePicture)
        {
            try
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                if (profilePicture == null || profilePicture.Length == 0)
                {
                    return Json(new { success = false, message = "No file uploaded" });
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Invalid file type. Only JPG, PNG, and GIF are allowed." });
                }

                // Validate file size (5MB max)
                if (profilePicture.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size must be less than 5MB" });
                }

                var userIdString = HttpContext.Session.GetString("UserId");
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Json(new { success = false, message = "Invalid user ID" });
                }

                // Create profile pictures directory if it doesn't exist
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProfilePictures");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                // Update database
                var user = context.UserAccount.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.Profilepicture))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Profilepicture.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Update with new profile picture path
                    user.Profilepicture = $"/ProfilePictures/{uniqueFileName}";
                    context.SaveChanges();

                    // Update session
                    HttpContext.Session.SetString("Profilepicture", user.Profilepicture);

                    Console.WriteLine($"? Profile picture updated for user {userId}: {user.Profilepicture}");

                    return Json(new { success = true, message = "Profile picture uploaded successfully", profilePictureUrl = user.Profilepicture });
                }
                else
                {
                    return Json(new { success = false, message = "User not found" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error uploading profile picture: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while uploading the profile picture" });
            }
        }

        [HttpPost]
        public IActionResult RemoveProfilePicture()
        {
            try
            {
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                {
                    return Json(new { success = false, message = "User not logged in" });
                }

                var userIdString = HttpContext.Session.GetString("UserId");
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Json(new { success = false, message = "Invalid user ID" });
                }

                // Get user from database
                var user = context.UserAccount.FirstOrDefault(u => u.Id == userId);
                if (user != null)
                {
                    // Delete old picture file if exists
                    if (!string.IsNullOrEmpty(user.Profilepicture))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Profilepicture.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                            Console.WriteLine($"? Deleted old profile picture file: {oldFilePath}");
                        }
                    }

                    // Remove profile picture from database
                    user.Profilepicture = null;
                    context.SaveChanges();

                    // Remove from session
                    HttpContext.Session.Remove("Profilepicture");

                    Console.WriteLine($"? Profile picture removed for user {userId}");

                    return Json(new { success = true, message = "Profile picture removed successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "User not found" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error removing profile picture: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while removing the profile picture" });
            }
        }

        public async Task<IActionResult> HospitalAssistance()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            // Get user ID
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            // ?? SECURITY: Generate form submission token
            var token = await _securityService.GenerateSubmissionTokenAsync(userId, "HospitalBill");
            ViewBag.SubmissionToken = token.Token;

            ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
            ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
            ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
            ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
            ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
            ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
            ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
            ViewBag.Gender = HttpContext.Session.GetString("Gender");
            ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");

            ViewBag.FrontID = HttpContext.Session.GetString("FrontID");
            ViewBag.BackID = HttpContext.Session.GetString("BackID");

            // Get phone number from Verifyaccount table
            var verifyAccount = context.VerifiedAccount.FirstOrDefault(v => v.UserId == userId);
            ViewBag.Phonenumber = verifyAccount?.Phonenumber ?? "";

            return View();
        }

        // ====================================
        // COMPLETE HOSPITAL BILL CONTROLLER - WITH EMBEDDED AES ENCRYPTION HELPER
        // ====================================

        // +---------------------------------------------------------------------------+
        // �                    AES-256 ENCRYPTION HELPER CLASS                        �
        // �         Secure AES-256 Implementation using Configuration                 �
        // +---------------------------------------------------------------------------+
        private class AesEncryptionHelper
        {
            private readonly byte[] _aesKey;

            public AesEncryptionHelper(IConfiguration configuration)
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

            public string Encrypt(string plainText)
            {
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

            public string Decrypt(string encryptedText)
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

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

            /// <summary>
            /// Encrypts byte array (used for images) and returns encrypted bytes
            /// </summary>
            public byte[] EncryptBytes(byte[] plainBytes)
            {
                using var aes = Aes.Create();
                aes.Key = _aesKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var memoryStream = new MemoryStream();

                // Write IV at the beginning
                memoryStream.Write(aes.IV, 0, aes.IV.Length);

                // Encrypt the data
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                    cryptoStream.FlushFinalBlock();
                }

                return memoryStream.ToArray();
            }

            /// <summary>
            /// Decrypts byte array (used for images) and returns decrypted bytes
            /// </summary>
            public byte[] DecryptBytes(byte[] encryptedBytes)
            {
                using var aes = Aes.Create();
                aes.Key = _aesKey;

                // Extract IV from the beginning
                byte[] iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.IV = iv;

                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(encryptedBytes, 16, encryptedBytes.Length - 16);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var resultStream = new MemoryStream();

                cryptoStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        // 1. DECRYPTION HELPER METHOD USING CONFIGURATION-BASED AES KEY
        private byte[] DecryptFile(string encryptedFilePath)
        {
            byte[] encryptedData = System.IO.File.ReadAllBytes(encryptedFilePath);
            using var memoryStream = new MemoryStream(encryptedData);

            // Read IV from the beginning of the file (first 16 bytes)
            byte[] iv = new byte[16];
            memoryStream.Read(iv, 0, iv.Length);

            // Use configuration-based AES helper
            var aesHelper = new AesEncryptionHelper(_configuration);

            // Get the key using reflection
            var keyField = typeof(AesEncryptionHelper).GetField("_aesKey",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (keyField == null)
                throw new InvalidOperationException("Cannot access AES key");

            byte[]? key = keyField.GetValue(aesHelper) as byte[];
            if (key == null)
                throw new InvalidOperationException("AES key is null");

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptedStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(decryptedStream);
            }

            return decryptedStream.ToArray();
        }

        // PDF DETECTION HELPER METHOD
        private bool IsPdfFile(byte[] data)
        {
            try
            {
                if (data == null || data.Length < 4)
                    return false;

                // Check PDF magic number (%PDF)
                if (data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46)
                    return true;

                // Also check for PDF in text (case insensitive)
                if (data.Length >= 1000)
                {
                    string beginning = Encoding.UTF8.GetString(data, 0, Math.Min(1000, data.Length));
                    if (beginning.Contains("%PDF") || beginning.Contains("%pdf"))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }



        [HttpPost]
        public async Task<IActionResult> HospitalAssistance(HospitalAssistanceDto HospitalAssistanceDto)
        {
            // Get the current user's ID from the session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                // If user is not logged in, redirect to login page
                return RedirectToAction("Login", "Login");
            }

            // Get the user's ID filenames from session
            string userFrontID = HttpContext.Session.GetString("FrontID") ?? "";
            string userBackID = HttpContext.Session.GetString("BackID") ?? "";

            // FIRST: Check for recently approved forms (cooldown period) - THIS SHOULD BE FIRST
            var oneMonthAgo = _dateTimeService.Now.AddMonths(-1);

            // Check for forms with Status = "Approve" within the last month
            var hasRecentApproval = context.HospitalAssistance
                .Any(f => f.UserId == userId && f.Status2 == "Approve" && f.CreatedAt >= oneMonthAgo);

            if (hasRecentApproval)
            {
                // Get the most recent approved form to show the exact date
                var recentApprovedForm = context.HospitalAssistance
                    .Where(f => f.UserId == userId && f.Status2 == "Approve")
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefault();

                string approvedDate = recentApprovedForm?.CreatedAt.ToString("MMMM dd, yyyy") ?? "recently";

                ModelState.AddModelError("", $"You cannot submit a new form because you already have an approved request dated {approvedDate}. Please wait one month from {approvedDate} before submitting another application.");
                return View(HospitalAssistanceDto);
            }

            // SECOND: Check for any pending or processing forms (user can only have one form at a time)
            var hasPendingForm = context.HospitalAssistance
                .Any(f => f.UserId == userId && (f.Status == "Pending" || f.Status == "Processing"));

            if (hasPendingForm)
            {
                ModelState.AddModelError("", "You already have a form that is currently pending or being processed. Please wait until it's approved before submitting a new one.");
                return View(HospitalAssistanceDto);
            }

            // If neither condition above is met, proceed with form submission

            // Optional field handling
            if (string.IsNullOrEmpty(HospitalAssistanceDto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // MODIFIED: Image validation - Remove ID image validation since we'll use the existing ones from user account
            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");

            if (!ModelState.IsValid)
            {
                return View(HospitalAssistanceDto);
            }

            try
            {
                // ===========================
                // ?? AES-256 FILE ENCRYPTION
                // ===========================
                // Use configuration-based AES helper
                var aesHelper = new AesEncryptionHelper(_configuration);

                // Generate encrypted filename for the document
                string? newFileNameDocument = null;

                string uploadsFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

                // Ensure directory exists
                Directory.CreateDirectory(uploadsFolder);

                // Encrypt and Save Hospital Assistance Document
                if (HospitalAssistanceDto.HospitalAssistanceDocument != null)
                {
                    // Encrypt the original filename
                    string originalFileName = HospitalAssistanceDto.HospitalAssistanceDocument.FileName;
                    newFileNameDocument = aesHelper.EncryptFilename(originalFileName) + ".enc";
                    string filePathDocument = Path.Combine(uploadsFolder, newFileNameDocument);
                    using (var fileStream = new FileStream(filePathDocument, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(HospitalAssistanceDto.HospitalAssistanceDocument.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Normalize suffix values - convert "None" to empty string
                if (HospitalAssistanceDto.Suffix == "None") HospitalAssistanceDto.Suffix = "";
                if (HospitalAssistanceDto.RSuffix == "None") HospitalAssistanceDto.RSuffix = "";

                // Map data to entity
                HospitalAssistance HospitalAssistance = new HospitalAssistance()
                {
                    UserId = userId,
                    // Patient Details
                    Lastname = HospitalAssistanceDto.Lastname,
                    Firstname = HospitalAssistanceDto.Firstname,
                    Middlename = HospitalAssistanceDto.Middlename,
                    Suffix = HospitalAssistanceDto.Suffix,
                    BlkLotStreet = HospitalAssistanceDto.BlkLotStreet,
                    SubVill = HospitalAssistanceDto.SubVill,
                    Brgy = HospitalAssistanceDto.Brgy,
                    Sex = HospitalAssistanceDto.Sex,
                    PhilHealth = HospitalAssistanceDto.PhilHealth,
                    PhilHealthNo = HospitalAssistanceDto.PhilHealthNo,
                    Dateofbirth = HospitalAssistanceDto.Dateofbirth,
                    Age = HospitalAssistanceDto.Age,

                    // Requestor Details
                    RLastname = HospitalAssistanceDto.RLastname,
                    RFirstname = HospitalAssistanceDto.RFirstname,
                    RMiddlename = HospitalAssistanceDto.RMiddlename,
                    RSuffix = HospitalAssistanceDto.RSuffix,
                    RBlkLotStreet = HospitalAssistanceDto.RBlkLotStreet,
                    RSubVill = HospitalAssistanceDto.RSubVill,
                    RBrgy = HospitalAssistanceDto.RBrgy,
                    RelationshipPatient = HospitalAssistanceDto.RelationshipPatient,
                    ContactNo = HospitalAssistanceDto.ContactNo,

                    // Assistance Type
                    Typeassistance = HospitalAssistanceDto.Typeassistance,
                    ForCMOPERSONNEL = HospitalAssistanceDto.ForCMOPERSONNEL,

                    // Additional Information - Encrypted Fields
                    HospitalFacilityName = _aesEncryptionService.Encrypt(HospitalAssistanceDto.HospitalFacilityName),
                    HospitalFacilityAddress = _aesEncryptionService.Encrypt(HospitalAssistanceDto.HospitalFacilityAddress),
                    DiagnosisMedicalCondition = _aesEncryptionService.Encrypt(HospitalAssistanceDto.DiagnosisMedicalCondition),
                    HospitalBillCost = _aesEncryptionService.Encrypt(HospitalAssistanceDto.HospitalBillCost),
                    AdmissionDate = _aesEncryptionService.Encrypt(HospitalAssistanceDto.AdmissionDate),
                    DischargeDate = _aesEncryptionService.Encrypt(HospitalAssistanceDto.DischargeDate),
                    WardRoomType = _aesEncryptionService.Encrypt(HospitalAssistanceDto.WardRoomType),

                    // MODIFIED: Use existing ID images from user account instead of new uploads
                    Validfrontimage = userFrontID,
                    ValidBackimage = userBackID,
                    DoctorPrescription = newFileNameDocument ?? string.Empty,
                    DeathCertificate = string.Empty,
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = _dateTimeService.Now
                };

                context.HospitalAssistance.Add(HospitalAssistance);
                context.SaveChanges();

                // Send user notification for successful submission
                try
                {
                    var userFullName = $"{HospitalAssistance.Firstname} {HospitalAssistance.Lastname}";
                    await _notificationService.SendStatusChangeNotificationAsync(
                        userId,
                        userFullName,
                        "HospitalBill",
                        "Pending",
                        HospitalAssistance.Id
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't fail the submission if notification fails
                    Console.WriteLine($"Failed to send user notification: {ex.Message}");
                }

                // Send admin notification for new submission
                try
                {
                    var userFullName = $"{HospitalAssistance.Firstname} {HospitalAssistance.Lastname}";
                    await _adminNotificationService.SendAdminNotificationAsync(
                        "application_submitted",
                        "HospitalAssistance",
                        HospitalAssistance.Id,
                        userId,
                        userFullName,
                        "New Hospital Assistance Application",
                        $"{userFullName} submitted a new Hospital Assistance application.",
                        $"/HospitalAssistancePendingStatus/{HospitalAssistance.Id}"
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't fail the submission if admin notification fails
                    Console.WriteLine($"Failed to send admin notification: {ex.Message}");
                }

                // ? SUCCESS: Set the success flag to trigger the modal
                ViewBag.Success = true;

                // Also set the session data again to repopulate the form
                ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
                ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
                ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
                ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
                ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
                ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
                ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
                ViewBag.Gender = HttpContext.Session.GetString("Gender");
                ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");
                ViewBag.FrontID = HttpContext.Session.GetString("FrontID");
                ViewBag.BackID = HttpContext.Session.GetString("BackID");

                return View(HospitalAssistanceDto);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    string message = sqlEx.Message.ToLower();

                    if (message.Contains("fullname"))
                        ModelState.AddModelError("Fullname", "This full name is already in use.");
                    else if (message.Contains("username"))
                        ModelState.AddModelError("Username", "This username is already taken.");
                    else if (message.Contains("email"))
                        ModelState.AddModelError("Email", "This email is already registered.");
                    else if (message.Contains("phonenumber"))
                        ModelState.AddModelError("Phonenumber", "This phone number is already in use.");
                    else
                        ModelState.AddModelError("", "A record with one of your inputs already exists.");

                    return View(HospitalAssistanceDto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(HospitalAssistanceDto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(HospitalAssistanceDto);
            }
        }
        public IActionResult HospitalAssistanceedit(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var HospitalAssistance = context.HospitalAssistance.Find(id);
            if (HospitalAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = HospitalAssistance.Status;
            ViewData["Id"] = HospitalAssistance.Id;
            ViewData["Lastname"] = HospitalAssistance.Lastname;
            ViewData["Firstname"] = HospitalAssistance.Firstname;
            ViewData["Middlename"] = HospitalAssistance.Middlename;
            ViewData["Suffix"] = HospitalAssistance.Suffix;
            ViewData["BlkLotStreet"] = HospitalAssistance.BlkLotStreet;
            ViewData["SubVill"] = HospitalAssistance.SubVill;
            ViewData["Brgy"] = HospitalAssistance.Brgy;
            ViewData["Sex"] = HospitalAssistance.Sex;
            ViewData["PhilHealth"] = HospitalAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = HospitalAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = HospitalAssistance.Dateofbirth;
            ViewData["Age"] = HospitalAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = HospitalAssistance.RLastname;
            ViewData["RFirstname"] = HospitalAssistance.RFirstname;
            ViewData["RMiddlename"] = HospitalAssistance.RMiddlename;
            ViewData["RSuffix"] = HospitalAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = HospitalAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = HospitalAssistance.RSubVill;
            ViewData["RBrgy"] = HospitalAssistance.RBrgy;
            ViewData["RelationshipPatient"] = HospitalAssistance.RelationshipPatient;
            ViewData["ContactNo"] = HospitalAssistance.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = HospitalAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = HospitalAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(HospitalAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, HospitalAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(HospitalAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, HospitalAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, HospitalAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {HospitalAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = HospitalAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? DEATH CERTIFICATE - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, HospitalAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {HospitalAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = HospitalAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = HospitalAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = HospitalAssistance.ValidBackimage;
            ViewData["Comments"] = HospitalAssistance.Comments;

            // ============================================
            // ADD DECRYPTION FOR THESE 7 FIELDS ONLY
            // ============================================

            // Use your existing DecryptFile logic to decrypt text fields
            ViewData["HospitalFacilityName"] = DecryptFieldText(HospitalAssistance.HospitalFacilityName);
            ViewData["HospitalFacilityAddress"] = DecryptFieldText(HospitalAssistance.HospitalFacilityAddress);
            ViewData["DiagnosisMedicalCondition"] = DecryptFieldText(HospitalAssistance.DiagnosisMedicalCondition);
            ViewData["HospitalBillCost"] = DecryptFieldText(HospitalAssistance.HospitalBillCost);
            ViewData["AdmissionDate"] = DecryptFieldText(HospitalAssistance.AdmissionDate);
            ViewData["DischargeDate"] = DecryptFieldText(HospitalAssistance.DischargeDate);
            ViewData["WardRoomType"] = DecryptFieldText(HospitalAssistance.WardRoomType);

            return View();

        }

        [HttpPost]
        public async Task<IActionResult> HospitalAssistanceEdit(int id, HospitalAssistanceDto dto)
        {
            // ? 1. Check user session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            // ? 2. Get existing record
            var existing = context.HospitalAssistance.Find(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Form not found.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ? 3. Security checks
            if (existing.UserId != userId)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this form.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Check if application is editable: Pending status OR Status2 is "Retake"
            bool isRetakeMode = existing.Status2 == "Retake";
            if (existing.Status != "Pending" && !isRetakeMode)
            {
                TempData["ErrorMessage"] = "You can only edit forms that are in 'Pending' or 'Retake' status.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ? 4. Remove validations for optional fields
            if (string.IsNullOrEmpty(dto.PhilHealthNo))
                ModelState.Remove("PhilHealthNo");

            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");
            ModelState.Remove("HospitalAssistanceDocument");

            if (!ModelState.IsValid)
            {
                // Repopulate ViewData for validation errors
                PopulateViewDataForEdit(existing);
                return View(dto);
            }

            try
            {
                var aesHelper = new AesEncryptionHelper(_configuration);

                // ? 5. Update Hospital Assistance Document (optional - only if provided)
                if (dto.HospitalAssistanceDocument != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old files if they exist
                    if (!string.IsNullOrEmpty(existing.DoctorPrescription))
                    {
                        string oldPath = Path.Combine(folder, existing.DoctorPrescription);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    if (!string.IsNullOrEmpty(existing.DeathCertificate))
                    {
                        string oldPath = Path.Combine(folder, existing.DeathCertificate);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    // Encrypt and save new file
                    string originalFileName = dto.HospitalAssistanceDocument.FileName;
                    string fileName = aesHelper.EncryptFilename(originalFileName) + ".enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(dto.HospitalAssistanceDocument.OpenReadStream());
                        await fs.WriteAsync(encrypted, 0, encrypted.Length);
                    }

                    // Store in DoctorPrescription field
                    existing.DoctorPrescription = fileName;
                    existing.DeathCertificate = ""; // Clear the other field
                }

                // ? 6. Update patient information with null checks
                existing.Lastname = dto.Lastname ?? existing.Lastname;
                existing.Firstname = dto.Firstname ?? existing.Firstname;
                existing.Middlename = dto.Middlename ?? existing.Middlename;
                existing.Suffix = (dto.Suffix == "None" ? "" : dto.Suffix) ?? existing.Suffix;
                existing.BlkLotStreet = dto.BlkLotStreet ?? existing.BlkLotStreet;
                existing.SubVill = dto.SubVill ?? existing.SubVill;
                existing.Brgy = dto.Brgy ?? existing.Brgy;
                existing.Sex = dto.Sex ?? existing.Sex;
                existing.PhilHealth = dto.PhilHealth ?? existing.PhilHealth;
                existing.PhilHealthNo = dto.PhilHealthNo ?? existing.PhilHealthNo;
                existing.Dateofbirth = dto.Dateofbirth ?? existing.Dateofbirth;
                existing.Age = dto.Age ?? existing.Age;

                // ? 7. Update requestor details
                existing.RLastname = dto.RLastname ?? existing.RLastname;
                existing.RFirstname = dto.RFirstname ?? existing.RFirstname;
                existing.RMiddlename = dto.RMiddlename ?? existing.RMiddlename;
                existing.RSuffix = (dto.RSuffix == "None" ? "" : dto.RSuffix) ?? existing.RSuffix;
                existing.RBlkLotStreet = dto.RBlkLotStreet ?? existing.RBlkLotStreet;
                existing.RSubVill = dto.RSubVill ?? existing.RSubVill;
                existing.RBrgy = dto.RBrgy ?? existing.RBrgy;
                existing.RelationshipPatient = dto.RelationshipPatient ?? existing.RelationshipPatient;
                existing.ContactNo = dto.ContactNo ?? existing.ContactNo;

                // ? 8. Update assistance information
                existing.Typeassistance = dto.Typeassistance ?? existing.Typeassistance;
                existing.ForCMOPERSONNEL = dto.ForCMOPERSONNEL ?? existing.ForCMOPERSONNEL;

                // ? 9. Encrypt and update hospital information fields
                if (!string.IsNullOrEmpty(dto.HospitalFacilityName))
                    existing.HospitalFacilityName = _aesEncryptionService.Encrypt(dto.HospitalFacilityName);

                if (!string.IsNullOrEmpty(dto.HospitalFacilityAddress))
                    existing.HospitalFacilityAddress = _aesEncryptionService.Encrypt(dto.HospitalFacilityAddress);

                if (!string.IsNullOrEmpty(dto.DiagnosisMedicalCondition))
                    existing.DiagnosisMedicalCondition = _aesEncryptionService.Encrypt(dto.DiagnosisMedicalCondition);

                if (!string.IsNullOrEmpty(dto.HospitalBillCost))
                    existing.HospitalBillCost = _aesEncryptionService.Encrypt(dto.HospitalBillCost);

                if (!string.IsNullOrEmpty(dto.AdmissionDate))
                    existing.AdmissionDate = _aesEncryptionService.Encrypt(dto.AdmissionDate);

                if (!string.IsNullOrEmpty(dto.DischargeDate))
                    existing.DischargeDate = _aesEncryptionService.Encrypt(dto.DischargeDate);

                if (!string.IsNullOrEmpty(dto.WardRoomType))
                    existing.WardRoomType = _aesEncryptionService.Encrypt(dto.WardRoomType);

                // ? 10. Update ID images (if session has updated IDs)
                string frontID = HttpContext.Session.GetString("FrontID") ?? "";
                string backID = HttpContext.Session.GetString("BackID") ?? "";
                if (!string.IsNullOrEmpty(frontID)) existing.Validfrontimage = frontID;
                if (!string.IsNullOrEmpty(backID)) existing.ValidBackimage = backID;

                // ? 11. Handle retake mode vs normal edit mode
                if (!isRetakeMode)
                {
                    // Normal edit - update timestamp
                    existing.CreatedAt = _dateTimeService.Now;
                }
                else
                {
                    // Retake mode - reset all processing fields
                    existing.Status = "Processing"; // Go directly to Processing for review
                    existing.Status2 = "Resubmitted"; // Mark as resubmitted
                    existing.Status3 = "";
                    existing.CreatedAt = _dateTimeService.Now; // Reset timestamp
                    existing.ProcessAt = _dateTimeService.Now;
                    existing.Processby = "";
                    existing.Result = new DateTime(1900, 1, 1);
                    existing.ClaimedAt = new DateTime(1900, 1, 1);
                    existing.IsRetakeApplication = false;
                    existing.RetakeReason = "";
                    existing.RetakeRequestedAt = null;
                    existing.Comments = "";
                }

                // ? 12. Save changes
                context.Entry(existing).State = EntityState.Modified;
                await context.SaveChangesAsync();

                // ? 13. Send notifications for retake mode
                if (isRetakeMode)
                {
                    try
                    {
                        var userFullName = $"{existing.Firstname} {existing.Lastname}";

                        // User notification
                        await _notificationService.SendStatusChangeNotificationAsync(
                            userId,
                            userFullName,
                            "HospitalBill",
                            "Processing",
                            existing.Id
                        );

                        // Admin notification
                        await _adminNotificationService.SendAdminNotificationAsync(
                            "application_submitted",
                            "HospitalAssistance",
                            existing.Id,
                            userId,
                            userFullName,
                            "[PRIORITY] Retake Resubmitted - Hospital Assistance",
                            $"{userFullName} resubmitted their retake application for Hospital Assistance. PRIORITY: Ready for immediate review.",
                            $"/HospitalAssistanceProcessingStatus/{existing.Id}"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                }

                // ? 14. Set success flag
                TempData["Success"] = true;
                TempData["RAF"] = id.ToString();

                // ? 15. Return with success - redirect to show updated data
                return RedirectToAction("HospitalAssistanceEdit", new { id = id, success = true });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating the form: {ex.Message}");
                PopulateViewDataForEdit(existing);
                return View(dto);
            }
        }

        // Helper method to populate ViewData
        private void PopulateViewDataForEdit(HospitalAssistance existing)
        {
            ViewData["Id"] = existing.Id;
            ViewData["Lastname"] = existing.Lastname;
            ViewData["Firstname"] = existing.Firstname;
            ViewData["Middlename"] = existing.Middlename;
            ViewData["Suffix"] = existing.Suffix;
            ViewData["BlkLotStreet"] = existing.BlkLotStreet;
            ViewData["SubVill"] = existing.SubVill;
            ViewData["Brgy"] = existing.Brgy;
            ViewData["Sex"] = existing.Sex;
            ViewData["PhilHealth"] = existing.PhilHealth;
            ViewData["PhilHealthNo"] = existing.PhilHealthNo;
            ViewData["Dateofbirth"] = existing.Dateofbirth;
            ViewData["Age"] = existing.Age;

            ViewData["RLastname"] = existing.RLastname;
            ViewData["RFirstname"] = existing.RFirstname;
            ViewData["RMiddlename"] = existing.RMiddlename;
            ViewData["RSuffix"] = existing.RSuffix;
            ViewData["RBlkLotStreet"] = existing.RBlkLotStreet;
            ViewData["RSubVill"] = existing.RSubVill;
            ViewData["RBrgy"] = existing.RBrgy;
            ViewData["RelationshipPatient"] = existing.RelationshipPatient;
            ViewData["ContactNo"] = existing.ContactNo;

            ViewData["Typeassistance"] = existing.Typeassistance;
            ViewData["ForCMOPERSONNEL"] = existing.ForCMOPERSONNEL;

            // Decrypt and show hospital information fields
            ViewData["HospitalFacilityName"] = DecryptFieldText(existing.HospitalFacilityName);
            ViewData["HospitalFacilityAddress"] = DecryptFieldText(existing.HospitalFacilityAddress);
            ViewData["DiagnosisMedicalCondition"] = DecryptFieldText(existing.DiagnosisMedicalCondition);
            ViewData["HospitalBillCost"] = DecryptFieldText(existing.HospitalBillCost);
            ViewData["AdmissionDate"] = DecryptFieldText(existing.AdmissionDate);
            ViewData["DischargeDate"] = DecryptFieldText(existing.DischargeDate);
            ViewData["WardRoomType"] = DecryptFieldText(existing.WardRoomType);

            ViewData["CurrentDoctorPrescription"] = existing.DoctorPrescription;
            ViewData["CurrentDeathCertificate"] = existing.DeathCertificate;
            ViewData["CurrentValidFront"] = existing.Validfrontimage;
            ViewData["CurrentValidBack"] = existing.ValidBackimage;
            ViewData["IsRetakeMode"] = existing.Status2 == "Retake";
        }

        public IActionResult HospitalAssistancedelete(int id)
        {
            var HospitalAssistance = context.HospitalAssistance.Find(id);
            if (HospitalAssistance == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Get user ID for notification cleanup
            var userId = HospitalAssistance.UserId;

            // Instead of deleting files and record, just update the status
            HospitalAssistance.Status = "Removed";
            context.HospitalAssistance.Update(HospitalAssistance);
            
            // Archive all notifications related to this application
            var relatedNotifications = context.Notifications
                .Where(n => n.UserId == userId && 
                           n.ApplicationType == "HospitalAssistance" && 
                           n.ApplicationId == id)
                .ToList();
            
            foreach (var notification in relatedNotifications)
            {
                notification.IsArchived = true;
            }
            
            context.SaveChanges();

            return RedirectToAction("Homepage", "Dashboard");
        }

        public IActionResult OtherAssistance()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            // Get user ID
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
            ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
            ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
            ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
            ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
            ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
            ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
            ViewBag.Gender = HttpContext.Session.GetString("Gender");
            ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");

            ViewBag.FrontID = HttpContext.Session.GetString("FrontID");
            ViewBag.BackID = HttpContext.Session.GetString("BackID");

            // Get phone number from Verifyaccount table
            var verifyAccount = context.VerifiedAccount.FirstOrDefault(v => v.UserId == userId);
            ViewBag.Phonenumber = verifyAccount?.Phonenumber ?? "";

            return View();
        }
        //1
        [HttpPost]
        public async Task<IActionResult> OtherAssistance(OtherAssistanceDto OtherAssistanceDto)
        {
            // Get the current user's ID from the session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                // If user is not logged in, redirect to login page
                return RedirectToAction("Login", "Login");
            }

            // Get the user's ID filenames from session
            string userFrontID = HttpContext.Session.GetString("FrontID") ?? "";
            string userBackID = HttpContext.Session.GetString("BackID") ?? "";

            // FIRST: Check for recently approved forms (cooldown period) - THIS SHOULD BE FIRST
            var oneMonthAgo = _dateTimeService.Now.AddMonths(-1);

            // Check for forms with Status = "Approve" within the last month
            var hasRecentApproval = context.OtherAssistance
                .Any(f => f.UserId == userId && f.Status2 == "Approve" && f.CreatedAt >= oneMonthAgo);

            if (hasRecentApproval)
            {
                // Get the most recent approved form to show the exact date
                var recentApprovedForm = context.OtherAssistance
                    .Where(f => f.UserId == userId && f.Status2 == "Approve")
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefault();

                string approvedDate = recentApprovedForm?.CreatedAt.ToString("MMMM dd, yyyy") ?? "recently";

                ModelState.AddModelError("", $"You cannot submit a new form because you already have an approved request dated {approvedDate}. Please wait one month from {approvedDate} before submitting another application.");
                return View(OtherAssistanceDto);
            }

            // SECOND: Check for any pending or processing forms (user can only have one form at a time)
            var hasPendingForm = context.OtherAssistance
                .Any(f => f.UserId == userId && (f.Status == "Pending" || f.Status == "Processing"));

            if (hasPendingForm)
            {
                ModelState.AddModelError("", "You already have a form that is currently pending or being processed. Please wait until it's approved before submitting a new one.");
                return View(OtherAssistanceDto);
            }

            // If neither condition above is met, proceed with form submission

            // Optional field handling
            if (string.IsNullOrEmpty(OtherAssistanceDto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // MODIFIED: Image validation - Remove ID image validation since we'll use existing ones from user account
            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");

            if (!ModelState.IsValid)
            {
                return View(OtherAssistanceDto);
            }

            try
            {
                // ===========================
                // ?? AES-256 FILE ENCRYPTION
                // ===========================
                // Use configuration-based AES helper
                var aesHelper = new AesEncryptionHelper(_configuration);

                // Generate encrypted filename for the document
                string? newFileNameDocument = null;

                string uploadsFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");

                // Ensure directory exists
                Directory.CreateDirectory(uploadsFolder);

                // Encrypt and Save Other Assistance Document
                if (OtherAssistanceDto.OtherAssistanceDocument != null)
                {
                    // Encrypt the original filename
                    string originalFileName = OtherAssistanceDto.OtherAssistanceDocument.FileName;
                    newFileNameDocument = aesHelper.EncryptFilename(originalFileName) + ".enc";
                    string filePathDocument = Path.Combine(uploadsFolder, newFileNameDocument);
                    using (var fileStream = new FileStream(filePathDocument, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(OtherAssistanceDto.OtherAssistanceDocument.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Normalize suffix values - convert "None" to empty string
                if (OtherAssistanceDto.Suffix == "None") OtherAssistanceDto.Suffix = "";
                if (OtherAssistanceDto.RSuffix == "None") OtherAssistanceDto.RSuffix = "";

                // Map data to entity
                OtherAssistance OtherAssistance = new OtherAssistance()
                {
                    UserId = userId,
                    // Patient Details
                    Lastname = OtherAssistanceDto.Lastname,
                    Firstname = OtherAssistanceDto.Firstname,
                    Middlename = OtherAssistanceDto.Middlename,
                    Suffix = OtherAssistanceDto.Suffix,
                    BlkLotStreet = OtherAssistanceDto.BlkLotStreet,
                    SubVill = OtherAssistanceDto.SubVill,
                    Brgy = OtherAssistanceDto.Brgy,
                    Sex = OtherAssistanceDto.Sex,
                    PhilHealth = OtherAssistanceDto.PhilHealth,
                    PhilHealthNo = OtherAssistanceDto.PhilHealthNo,
                    Dateofbirth = OtherAssistanceDto.Dateofbirth,
                    Age = OtherAssistanceDto.Age,

                    // Requestor Details
                    RLastname = OtherAssistanceDto.RLastname,
                    RFirstname = OtherAssistanceDto.RFirstname,
                    RMiddlename = OtherAssistanceDto.RMiddlename,
                    RSuffix = OtherAssistanceDto.RSuffix,
                    RBlkLotStreet = OtherAssistanceDto.RBlkLotStreet,
                    RSubVill = OtherAssistanceDto.RSubVill,
                    RBrgy = OtherAssistanceDto.RBrgy,
                    RelationshipPatient = OtherAssistanceDto.RelationshipPatient,
                    ContactNo = OtherAssistanceDto.ContactNo,

                    // Assistance Type
                    Typeassistance = OtherAssistanceDto.Typeassistance,
                    ForCMOPERSONNEL = OtherAssistanceDto.ForCMOPERSONNEL,

                    // MODIFIED: Use existing ID images from user account instead of new uploads
                    Validfrontimage = userFrontID,
                    ValidBackimage = userBackID,
                    DoctorPrescription = string.Empty,
                    DeathCertificate = string.Empty,
                    MedCertificate = newFileNameDocument ?? string.Empty,
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = _dateTimeService.Now
                };

                // Additional Information - Conditional Encryption based on Assistance Type
                switch (OtherAssistanceDto.Typeassistance)
                {
                    case "Medicine Assistance":
                        OtherAssistance.MedicineName = _aesEncryptionService.Encrypt(OtherAssistanceDto.MedicineName ?? "");
                        OtherAssistance.MedicineQuantity = _aesEncryptionService.Encrypt(OtherAssistanceDto.MedicineQuantity ?? "");
                        OtherAssistance.MedicineCost = _aesEncryptionService.Encrypt(OtherAssistanceDto.MedicineCost ?? "");
                        OtherAssistance.PrescribingDoctor = _aesEncryptionService.Encrypt(OtherAssistanceDto.PrescribingDoctor ?? "");
                        OtherAssistance.DoctorContactDetail = _aesEncryptionService.Encrypt(OtherAssistanceDto.DoctorContactDetail ?? "");
                        break;

                    case "Laboratory":
                        OtherAssistance.LaboratoryCenterName = _aesEncryptionService.Encrypt(OtherAssistanceDto.LaboratoryCenterName ?? "");
                        OtherAssistance.LaboratoryCenterAddress = _aesEncryptionService.Encrypt(OtherAssistanceDto.LaboratoryCenterAddress ?? "");
                        OtherAssistance.TestName = _aesEncryptionService.Encrypt(OtherAssistanceDto.TestName ?? "");
                        OtherAssistance.TestCost = _aesEncryptionService.Encrypt(OtherAssistanceDto.TestCost ?? "");
                        OtherAssistance.TestOtherInfo = _aesEncryptionService.Encrypt(OtherAssistanceDto.TestOtherInfo ?? "");
                        break;

                    case "Therapy":
                        OtherAssistance.TherapyFacilityName = _aesEncryptionService.Encrypt(OtherAssistanceDto.TherapyFacilityName ?? "");
                        OtherAssistance.TherapyFacilityAddress = _aesEncryptionService.Encrypt(OtherAssistanceDto.TherapyFacilityAddress ?? "");
                        OtherAssistance.TherapyFacilityContact = _aesEncryptionService.Encrypt(OtherAssistanceDto.TherapyFacilityContact ?? "");
                        OtherAssistance.TherapyType = _aesEncryptionService.Encrypt(OtherAssistanceDto.TherapyType ?? "");
                        break;

                    case "Medical Equipment/ Apparatus":
                        OtherAssistance.EquipmentName = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentName ?? "");
                        OtherAssistance.EquipmentBrand = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentBrand ?? "");
                        OtherAssistance.EquipmentCategory = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentCategory ?? "");
                        OtherAssistance.EquipmentQuantity = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentQuantity ?? "");
                        OtherAssistance.EquipmentCost = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentCost ?? "");
                        break;
                }

                context.OtherAssistance.Add(OtherAssistance);
                context.SaveChanges();

                // Send user notification for successful submission
                try
                {
                    var userFullName = $"{OtherAssistance.Firstname} {OtherAssistance.Lastname}";
                    await _notificationService.SendStatusChangeNotificationAsync(
                        userId,
                        userFullName,
                        "Other",
                        "Pending",
                        OtherAssistance.Id
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't fail the submission if notification fails
                    Console.WriteLine($"Failed to send user notification: {ex.Message}");
                }

                // Send admin notification for new submission
                try
                {
                    var userFullName = $"{OtherAssistance.Firstname} {OtherAssistance.Lastname}";
                    await _adminNotificationService.SendAdminNotificationAsync(
                        "application_submitted",
                        "OtherAssistance",
                        OtherAssistance.Id,
                        userId,
                        userFullName,
                        "New Other Assistance Application",
                        $"{userFullName} submitted a new Other Assistance application.",
                        $"/OtherAssistancePendingStatus/{OtherAssistance.Id}"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send admin notification: {ex.Message}");
                }

                // ? SUCCESS: Set the success flag to trigger the modal
                ViewBag.Success = true;

                // Also set the session data again to repopulate the form
                ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
                ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
                ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
                ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
                ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
                ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
                ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
                ViewBag.Gender = HttpContext.Session.GetString("Gender");
                ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");
                ViewBag.FrontID = HttpContext.Session.GetString("FrontID");
                ViewBag.BackID = HttpContext.Session.GetString("BackID");

                return View(OtherAssistanceDto);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    string message = sqlEx.Message.ToLower();

                    if (message.Contains("fullname"))
                        ModelState.AddModelError("Fullname", "This full name is already in use.");
                    else if (message.Contains("username"))
                        ModelState.AddModelError("Username", "This username is already taken.");
                    else if (message.Contains("email"))
                        ModelState.AddModelError("Email", "This email is already registered.");
                    else if (message.Contains("phonenumber"))
                        ModelState.AddModelError("Phonenumber", "This phone number is already in use.");
                    else
                        ModelState.AddModelError("", "A record with one of your inputs already exists.");

                    return View(OtherAssistanceDto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(OtherAssistanceDto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(OtherAssistanceDto);
            }
        }

        public IActionResult OtherAssistanceedit(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.OtherAssistance.Find(id);
            if (medicallabform == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = medicallabform.Status;
            ViewData["Id"] = medicallabform.Id;
            ViewData["Lastname"] = medicallabform.Lastname;
            ViewData["Firstname"] = medicallabform.Firstname;
            ViewData["Middlename"] = medicallabform.Middlename;
            ViewData["Suffix"] = medicallabform.Suffix;
            ViewData["BlkLotStreet"] = medicallabform.BlkLotStreet;
            ViewData["SubVill"] = medicallabform.SubVill;
            ViewData["Brgy"] = medicallabform.Brgy;
            ViewData["Sex"] = medicallabform.Sex;
            ViewData["PhilHealth"] = medicallabform.PhilHealth;
            ViewData["PhilHealthNo"] = medicallabform.PhilHealthNo;
            ViewData["Dateofbirth"] = medicallabform.Dateofbirth;
            ViewData["Age"] = medicallabform.Age;

            // Requestor details
            ViewData["RLastname"] = medicallabform.RLastname;
            ViewData["RFirstname"] = medicallabform.RFirstname;
            ViewData["RMiddlename"] = medicallabform.RMiddlename;
            ViewData["RSuffix"] = medicallabform.RSuffix;
            ViewData["RBlkLotStreet"] = medicallabform.RBlkLotStreet;
            ViewData["RSubVill"] = medicallabform.RSubVill;
            ViewData["RBrgy"] = medicallabform.RBrgy;
            ViewData["RelationshipPatient"] = medicallabform.RelationshipPatient;
            ViewData["ContactNo"] = medicallabform.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = medicallabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = medicallabform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(medicallabform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, medicallabform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✓ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(medicallabform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, medicallabform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✓ Back ID decrypted");
                    }
                }

                // ✓ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"📄 Full path: {prescPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = medicallabform.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"✓ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"📄 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Doctor Prescription in database");
                }

                // ✓ MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"📄 Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"📄 Full path: {medicalPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(medicalPath)}");

                    if (System.IO.File.Exists(medicalPath))
                    {
                        try
                        {
                            byte[] decryptedMedical = DecryptFile(medicalPath);
                            ViewData["MedicalCertificateBase64"] = Convert.ToBase64String(decryptedMedical);
                            ViewData["MedicalCertificate"] = medicallabform.MedCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedMedical);
                            ViewData["IsMedicalCertificatePdf"] = isPdf;

                            debugMessages.Add($"✓ Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"📄 IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Medical Certificate in database");
                }

                // ✓ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"📄 Full path: {deathPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = medicallabform.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"✓ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"📄 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"⚠️ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            // ============================================
            // ADD DECRYPTION FOR THESE 19 FIELDS ONLY
            // ============================================

            // Medicine Assistance Fields
            ViewData["MedicineName"] = DecryptFieldText(medicallabform.MedicineName);
            ViewData["MedicineQuantity"] = DecryptFieldText(medicallabform.MedicineQuantity);
            ViewData["MedicineCost"] = DecryptFieldText(medicallabform.MedicineCost);
            ViewData["PrescribingDoctor"] = DecryptFieldText(medicallabform.PrescribingDoctor);
            ViewData["DoctorContactDetail"] = DecryptFieldText(medicallabform.DoctorContactDetail);

            // Laboratory Assistance Fields
            ViewData["LaboratoryCenterName"] = DecryptFieldText(medicallabform.LaboratoryCenterName);
            ViewData["LaboratoryCenterAddress"] = DecryptFieldText(medicallabform.LaboratoryCenterAddress);
            ViewData["TestName"] = DecryptFieldText(medicallabform.TestName);
            ViewData["TestCost"] = DecryptFieldText(medicallabform.TestCost);
            ViewData["TestOtherInfo"] = DecryptFieldText(medicallabform.TestOtherInfo);

            // Therapy Assistance Fields
            ViewData["TherapyFacilityName"] = DecryptFieldText(medicallabform.TherapyFacilityName);
            ViewData["TherapyFacilityAddress"] = DecryptFieldText(medicallabform.TherapyFacilityAddress);
            ViewData["TherapyFacilityContact"] = DecryptFieldText(medicallabform.TherapyFacilityContact);
            ViewData["TherapyType"] = DecryptFieldText(medicallabform.TherapyType);

            // Equipment Assistance Fields
            ViewData["EquipmentName"] = DecryptFieldText(medicallabform.EquipmentName);
            ViewData["EquipmentBrand"] = DecryptFieldText(medicallabform.EquipmentBrand);
            ViewData["EquipmentCategory"] = DecryptFieldText(medicallabform.EquipmentCategory);
            ViewData["EquipmentQuantity"] = DecryptFieldText(medicallabform.EquipmentQuantity);
            ViewData["EquipmentCost"] = DecryptFieldText(medicallabform.EquipmentCost);

            return View();

        }

        [HttpPost]
        public async Task<IActionResult> OtherAssistanceEdit(int id, OtherAssistanceDto OtherAssistanceDto)
        {
            // ? 1. Check user session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            // ? 2. Get existing record
            var existing = context.OtherAssistance.Find(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Form not found.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ? 3. Security checks
            if (existing.UserId != userId)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this form.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Check if application is editable: Pending status OR Status2 is "Retake"
            bool isRetakeMode = existing.Status2 == "Retake";
            if (existing.Status != "Pending" && !isRetakeMode)
            {
                TempData["ErrorMessage"] = "You can only edit forms that are in 'Pending' or 'Retake' status.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ? 4. Remove validations for optional fields
            if (string.IsNullOrEmpty(OtherAssistanceDto.PhilHealthNo))
                ModelState.Remove("PhilHealthNo");

            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");
            ModelState.Remove("OtherAssistanceDocument");

            if (!ModelState.IsValid)
            {
                // Repopulate ViewData for validation errors
                PopulateViewDataForOtherAssistanceEdit(existing);
                return View(OtherAssistanceDto);
            }

            try
            {
                var aesHelper = new AesEncryptionHelper(_configuration);

                // ? 5. Update Other Assistance Document (optional - only if provided)
                if (OtherAssistanceDto.OtherAssistanceDocument != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old files if they exist
                    if (!string.IsNullOrEmpty(existing.DoctorPrescription))
                    {
                        string oldPath = Path.Combine(folder, existing.DoctorPrescription);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    if (!string.IsNullOrEmpty(existing.DeathCertificate))
                    {
                        string oldPath = Path.Combine(folder, existing.DeathCertificate);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    if (!string.IsNullOrEmpty(existing.MedCertificate))
                    {
                        string oldPath = Path.Combine(folder, existing.MedCertificate);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    // Encrypt and save new file
                    string originalFileName = OtherAssistanceDto.OtherAssistanceDocument.FileName;
                    string fileName = aesHelper.EncryptFilename(originalFileName) + ".enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(OtherAssistanceDto.OtherAssistanceDocument.OpenReadStream());
                        await fs.WriteAsync(encrypted, 0, encrypted.Length);
                    }

                    // Store in DoctorPrescription field (unified field)
                    existing.DoctorPrescription = fileName;
                    existing.DeathCertificate = ""; // Clear the other fields
                    existing.MedCertificate = "";
                }

                // ? 6. Update patient information with null checks
                existing.Lastname = OtherAssistanceDto.Lastname ?? existing.Lastname;
                existing.Firstname = OtherAssistanceDto.Firstname ?? existing.Firstname;
                existing.Middlename = OtherAssistanceDto.Middlename ?? existing.Middlename;
                existing.Suffix = (OtherAssistanceDto.Suffix == "None" ? "" : OtherAssistanceDto.Suffix) ?? existing.Suffix;
                existing.BlkLotStreet = OtherAssistanceDto.BlkLotStreet ?? existing.BlkLotStreet;
                existing.SubVill = OtherAssistanceDto.SubVill ?? existing.SubVill;
                existing.Brgy = OtherAssistanceDto.Brgy ?? existing.Brgy;
                existing.Sex = OtherAssistanceDto.Sex ?? existing.Sex;
                existing.PhilHealth = OtherAssistanceDto.PhilHealth ?? existing.PhilHealth;
                existing.PhilHealthNo = OtherAssistanceDto.PhilHealthNo ?? existing.PhilHealthNo;
                existing.Dateofbirth = OtherAssistanceDto.Dateofbirth ?? existing.Dateofbirth;
                existing.Age = OtherAssistanceDto.Age ?? existing.Age;

                // ? 7. Update requestor details
                existing.RLastname = OtherAssistanceDto.RLastname ?? existing.RLastname;
                existing.RFirstname = OtherAssistanceDto.RFirstname ?? existing.RFirstname;
                existing.RMiddlename = OtherAssistanceDto.RMiddlename ?? existing.RMiddlename;
                existing.RSuffix = (OtherAssistanceDto.RSuffix == "None" ? "" : OtherAssistanceDto.RSuffix) ?? existing.RSuffix;
                existing.RBlkLotStreet = OtherAssistanceDto.RBlkLotStreet ?? existing.RBlkLotStreet;
                existing.RSubVill = OtherAssistanceDto.RSubVill ?? existing.RSubVill;
                existing.RBrgy = OtherAssistanceDto.RBrgy ?? existing.RBrgy;
                existing.RelationshipPatient = OtherAssistanceDto.RelationshipPatient ?? existing.RelationshipPatient;
                existing.ContactNo = OtherAssistanceDto.ContactNo ?? existing.ContactNo;

                // ? 8. Update assistance information
                existing.Typeassistance = OtherAssistanceDto.Typeassistance ?? existing.Typeassistance;
                existing.ForCMOPERSONNEL = OtherAssistanceDto.ForCMOPERSONNEL ?? existing.ForCMOPERSONNEL;

                // ? 9. Encrypt and update conditional fields based on assistance type
                switch (OtherAssistanceDto.Typeassistance)
                {
                    case "Medicine Assistance":
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.MedicineName))
                            existing.MedicineName = _aesEncryptionService.Encrypt(OtherAssistanceDto.MedicineName);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.MedicineQuantity))
                            existing.MedicineQuantity = _aesEncryptionService.Encrypt(OtherAssistanceDto.MedicineQuantity);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.MedicineCost))
                            existing.MedicineCost = _aesEncryptionService.Encrypt(OtherAssistanceDto.MedicineCost);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.PrescribingDoctor))
                            existing.PrescribingDoctor = _aesEncryptionService.Encrypt(OtherAssistanceDto.PrescribingDoctor);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.DoctorContactDetail))
                            existing.DoctorContactDetail = _aesEncryptionService.Encrypt(OtherAssistanceDto.DoctorContactDetail);
                        break;

                    case "Laboratory":
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.LaboratoryCenterName))
                            existing.LaboratoryCenterName = _aesEncryptionService.Encrypt(OtherAssistanceDto.LaboratoryCenterName);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.LaboratoryCenterAddress))
                            existing.LaboratoryCenterAddress = _aesEncryptionService.Encrypt(OtherAssistanceDto.LaboratoryCenterAddress);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.TestName))
                            existing.TestName = _aesEncryptionService.Encrypt(OtherAssistanceDto.TestName);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.TestCost))
                            existing.TestCost = _aesEncryptionService.Encrypt(OtherAssistanceDto.TestCost);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.TestOtherInfo))
                            existing.TestOtherInfo = _aesEncryptionService.Encrypt(OtherAssistanceDto.TestOtherInfo);
                        break;

                    case "Therapy":
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.TherapyFacilityName))
                            existing.TherapyFacilityName = _aesEncryptionService.Encrypt(OtherAssistanceDto.TherapyFacilityName);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.TherapyFacilityAddress))
                            existing.TherapyFacilityAddress = _aesEncryptionService.Encrypt(OtherAssistanceDto.TherapyFacilityAddress);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.TherapyFacilityContact))
                            existing.TherapyFacilityContact = _aesEncryptionService.Encrypt(OtherAssistanceDto.TherapyFacilityContact);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.TherapyType))
                            existing.TherapyType = _aesEncryptionService.Encrypt(OtherAssistanceDto.TherapyType);
                        break;

                    case "Medical Equipment/ Apparatus":
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.EquipmentName))
                            existing.EquipmentName = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentName);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.EquipmentBrand))
                            existing.EquipmentBrand = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentBrand);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.EquipmentCategory))
                            existing.EquipmentCategory = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentCategory);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.EquipmentQuantity))
                            existing.EquipmentQuantity = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentQuantity);
                        if (!string.IsNullOrEmpty(OtherAssistanceDto.EquipmentCost))
                            existing.EquipmentCost = _aesEncryptionService.Encrypt(OtherAssistanceDto.EquipmentCost);
                        break;
                }

                // ? 10. Update ID images (if session has updated IDs)
                string frontID = HttpContext.Session.GetString("FrontID") ?? "";
                string backID = HttpContext.Session.GetString("BackID") ?? "";
                if (!string.IsNullOrEmpty(frontID)) existing.Validfrontimage = frontID;
                if (!string.IsNullOrEmpty(backID)) existing.ValidBackimage = backID;

                // ? 11. Handle retake mode vs normal edit mode
                if (!isRetakeMode)
                {
                    // Normal edit - update timestamp
                    existing.CreatedAt = _dateTimeService.Now;
                }
                else
                {
                    // Retake mode - reset all processing fields
                    existing.Status = "Processing"; // Go directly to Processing for review
                    existing.Status2 = "Resubmitted"; // Mark as resubmitted
                    existing.Status3 = "";
                    existing.CreatedAt = _dateTimeService.Now; // Reset timestamp
                    existing.ProcessAt = _dateTimeService.Now;
                    existing.Processby = "";
                    existing.Result = new DateTime(1900, 1, 1);
                    existing.ClaimedAt = new DateTime(1900, 1, 1);
                    existing.IsRetakeApplication = false;
                    existing.RetakeReason = "";
                    existing.RetakeRequestedAt = null;
                    existing.Comments = "";
                }

                // ? 12. Save changes
                context.Entry(existing).State = EntityState.Modified;
                await context.SaveChangesAsync();

                // ? 13. Send notifications for retake mode
                if (isRetakeMode)
                {
                    try
                    {
                        var userFullName = $"{existing.Firstname} {existing.Lastname}";

                        // User notification
                        await _notificationService.SendStatusChangeNotificationAsync(
                            userId,
                            userFullName,
                            "Other",
                            "Processing",
                            existing.Id
                        );

                        // Admin notification
                        await _adminNotificationService.SendAdminNotificationAsync(
                            "application_submitted",
                            "OtherAssistance",
                            existing.Id,
                            userId,
                            userFullName,
                            "[PRIORITY] Retake Resubmitted - Other Assistance",
                            $"{userFullName} resubmitted their retake application for Other Assistance. PRIORITY: Ready for immediate review.",
                            $"/OtherAssistanceProcessingStatus/{existing.Id}"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                }

                // ? 14. Set success flag - FIXED: Use ViewBag instead of TempData
                ViewBag.Success = true;
                TempData["RAF"] = id.ToString();

                // ? 15. Return with success - IMPORTANT: Populate ViewData before returning
                PopulateViewDataForOtherAssistanceEdit(existing);
                return View(OtherAssistanceDto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating the form: {ex.Message}");
                PopulateViewDataForOtherAssistanceEdit(existing);
                return View(OtherAssistanceDto);
            }
        }
        // Helper method to populate ViewData for Other Assistance
        private void PopulateViewDataForOtherAssistanceEdit(OtherAssistance existing)
        {
            // Basic patient information
            ViewData["Id"] = existing.Id;
            ViewData["Lastname"] = existing.Lastname;
            ViewData["Firstname"] = existing.Firstname;
            ViewData["Middlename"] = existing.Middlename;
            ViewData["Suffix"] = existing.Suffix;
            ViewData["BlkLotStreet"] = existing.BlkLotStreet;
            ViewData["SubVill"] = existing.SubVill;
            ViewData["Brgy"] = existing.Brgy;
            ViewData["Sex"] = existing.Sex;
            ViewData["PhilHealth"] = existing.PhilHealth;
            ViewData["PhilHealthNo"] = existing.PhilHealthNo;
            ViewData["Dateofbirth"] = existing.Dateofbirth;
            ViewData["Age"] = existing.Age;

            // Requestor information
            ViewData["RLastname"] = existing.RLastname;
            ViewData["RFirstname"] = existing.RFirstname;
            ViewData["RMiddlename"] = existing.RMiddlename;
            ViewData["RSuffix"] = existing.RSuffix;
            ViewData["RBlkLotStreet"] = existing.RBlkLotStreet;
            ViewData["RSubVill"] = existing.RSubVill;
            ViewData["RBrgy"] = existing.RBrgy;
            ViewData["RelationshipPatient"] = existing.RelationshipPatient;
            ViewData["ContactNo"] = existing.ContactNo;

            // Assistance information
            ViewData["Typeassistance"] = existing.Typeassistance;
            ViewData["ForCMOPERSONNEL"] = existing.ForCMOPERSONNEL;

            // Decrypt and show conditional fields based on assistance type
            if (!string.IsNullOrEmpty(existing.Typeassistance))
            {
                switch (existing.Typeassistance)
                {
                    case "Medicine Assistance":
                        ViewData["MedicineName"] = DecryptFieldText(existing.MedicineName);
                        ViewData["MedicineQuantity"] = DecryptFieldText(existing.MedicineQuantity);
                        ViewData["MedicineCost"] = DecryptFieldText(existing.MedicineCost);
                        ViewData["PrescribingDoctor"] = DecryptFieldText(existing.PrescribingDoctor);
                        ViewData["DoctorContactDetail"] = DecryptFieldText(existing.DoctorContactDetail);
                        break;

                    case "Laboratory":
                        ViewData["LaboratoryCenterName"] = DecryptFieldText(existing.LaboratoryCenterName);
                        ViewData["LaboratoryCenterAddress"] = DecryptFieldText(existing.LaboratoryCenterAddress);
                        ViewData["TestName"] = DecryptFieldText(existing.TestName);
                        ViewData["TestCost"] = DecryptFieldText(existing.TestCost);
                        ViewData["TestOtherInfo"] = DecryptFieldText(existing.TestOtherInfo);
                        break;

                    case "Therapy":
                        ViewData["TherapyFacilityName"] = DecryptFieldText(existing.TherapyFacilityName);
                        ViewData["TherapyFacilityAddress"] = DecryptFieldText(existing.TherapyFacilityAddress);
                        ViewData["TherapyFacilityContact"] = DecryptFieldText(existing.TherapyFacilityContact);
                        ViewData["TherapyType"] = DecryptFieldText(existing.TherapyType);
                        break;

                    case "Medical Equipment/ Apparatus":
                        ViewData["EquipmentName"] = DecryptFieldText(existing.EquipmentName);
                        ViewData["EquipmentBrand"] = DecryptFieldText(existing.EquipmentBrand);
                        ViewData["EquipmentCategory"] = DecryptFieldText(existing.EquipmentCategory);
                        ViewData["EquipmentQuantity"] = DecryptFieldText(existing.EquipmentQuantity);
                        ViewData["EquipmentCost"] = DecryptFieldText(existing.EquipmentCost);
                        break;
                }
            }

            // File information
            ViewData["Validfrontimage"] = existing.Validfrontimage;
            ViewData["ValidBackimage"] = existing.ValidBackimage;
            ViewData["DoctorPrescription"] = existing.DoctorPrescription;
            ViewData["DeathCertificate"] = existing.DeathCertificate;
            ViewData["MedCertificate"] = existing.MedCertificate;

            // Check for current document to display
            ViewData["OtherAssistanceDocument"] = existing.DoctorPrescription ?? existing.DeathCertificate ?? existing.MedCertificate;

            // Check if it's a retake mode
            ViewData["IsRetakeMode"] = existing.Status2 == "Retake";
        }

  
 

        public IActionResult OtherAssistanceedelete(int id)
        {
            var OtherAssistance = context.OtherAssistance.Find(id);
            if (OtherAssistance == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Get user ID for notification cleanup
            var userId = OtherAssistance.UserId;

            // Instead of deleting files and record, just update the status
            OtherAssistance.Status = "Removed";
            context.OtherAssistance.Update(OtherAssistance);
            
            // Archive all notifications related to this application
            var relatedNotifications = context.Notifications
                .Where(n => n.UserId == userId && 
                           n.ApplicationType == "OtherAssistance" && 
                           n.ApplicationId == id)
                .ToList();
            
            foreach (var notification in relatedNotifications)
            {
                notification.IsArchived = true;
            }
            
            context.SaveChanges();

            return RedirectToAction("Homepage", "Dashboard");
        }

        public IActionResult FuneralAssistance()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            // Get user ID
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
            ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
            ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
            ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
            ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
            ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
            ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
            ViewBag.Gender = HttpContext.Session.GetString("Gender");
            ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");

            ViewBag.FrontID = HttpContext.Session.GetString("FrontID");
            ViewBag.BackID = HttpContext.Session.GetString("BackID");

            // Get phone number from Verifyaccount table
            var verifyAccount = context.VerifiedAccount.FirstOrDefault(v => v.UserId == userId);
            ViewBag.Phonenumber = verifyAccount?.Phonenumber ?? "";

            return View();
        }



        [HttpPost]
        public async Task<IActionResult> FuneralAssistance(FuneralAssistanceDto FuneralAssistanceDto)
        {
            // Get the current user's ID from the session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                // If user is not logged in, redirect to login page
                return RedirectToAction("Login", "Login");
            }

            // Get the user's ID filenames from session
            string userFrontID = HttpContext.Session.GetString("FrontID") ?? "";
            string userBackID = HttpContext.Session.GetString("BackID") ?? "";

            // FIRST: Check for recently approved forms (cooldown period) - THIS SHOULD BE FIRST
            var oneMonthAgo = _dateTimeService.Now.AddMonths(-1);

            // Check for forms with Status = "Approve" within the last month
            var hasRecentApproval = context.FuneralAssistance
                .Any(f => f.UserId == userId && f.Status2 == "Approve" && f.CreatedAt >= oneMonthAgo);

            if (hasRecentApproval)
            {
                // Get the most recent approved form to show the exact date
                var recentApprovedForm = context.FuneralAssistance
                    .Where(f => f.UserId == userId && f.Status2 == "Approve")
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefault();

                string approvedDate = recentApprovedForm?.CreatedAt.ToString("MMMM dd, yyyy") ?? "recently";

                ModelState.AddModelError("", $"You cannot submit a new form because you already have an approved request dated {approvedDate}. Please wait one month from {approvedDate} before submitting another application.");
                return View(FuneralAssistanceDto);
            }

            // SECOND: Check for any pending or processing forms (user can only have one form at a time)
            var hasPendingForm = context.FuneralAssistance
                .Any(f => f.UserId == userId && (f.Status == "Pending" || f.Status == "Processing"));

            if (hasPendingForm)
            {
                ModelState.AddModelError("", "You already have a form that is currently pending or being processed. Please wait until it's approved before submitting a new one.");
                return View(FuneralAssistanceDto);
            }

            // If neither condition above is met, proceed with form submission

            // Optional field handling
            if (string.IsNullOrEmpty(FuneralAssistanceDto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // Remove Typeassistance validation since FuneralAssistance doesn't have Type of Assistance checkboxes
            ModelState.Remove("Typeassistance");

            // MODIFIED: Image validation - Remove ID image validation since we'll use existing ones from user account
            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");

            if (!ModelState.IsValid)
            {
                return View(FuneralAssistanceDto);
            }

            try
            {
                // ===========================
                // ?? AES-256 FILE ENCRYPTION
                // ===========================
                // Use configuration-based AES helper
                var aesHelper = new AesEncryptionHelper(_configuration);

                // Generate encrypted filename for the document
                string? newFileNameDocument = null;

                string uploadsFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");

                // Ensure directory exists
                Directory.CreateDirectory(uploadsFolder);

                // Encrypt and Save Funeral Assistance Document
                if (FuneralAssistanceDto.FuneralAssistanceDocument != null)
                {
                    // Encrypt the original filename
                    string originalFileName = FuneralAssistanceDto.FuneralAssistanceDocument.FileName;
                    newFileNameDocument = aesHelper.EncryptFilename(originalFileName) + ".enc";
                    string filePathDocument = Path.Combine(uploadsFolder, newFileNameDocument);
                    using (var fileStream = new FileStream(filePathDocument, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(FuneralAssistanceDto.FuneralAssistanceDocument.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Normalize suffix values - convert "None" to empty string
                if (FuneralAssistanceDto.Suffix == "None") FuneralAssistanceDto.Suffix = "";
                if (FuneralAssistanceDto.RSuffix == "None") FuneralAssistanceDto.RSuffix = "";

                // Map data to entity
                FuneralAssistance FuneralAssistance = new FuneralAssistance()
                {
                    UserId = userId,
                    // Patient Details
                    Lastname = FuneralAssistanceDto.Lastname,
                    Firstname = FuneralAssistanceDto.Firstname,
                    Middlename = FuneralAssistanceDto.Middlename,
                    Suffix = FuneralAssistanceDto.Suffix,
                    BlkLotStreet = FuneralAssistanceDto.BlkLotStreet,
                    SubVill = FuneralAssistanceDto.SubVill,
                    Brgy = FuneralAssistanceDto.Brgy,
                    Sex = FuneralAssistanceDto.Sex,
                    PhilHealth = FuneralAssistanceDto.PhilHealth,
                    PhilHealthNo = FuneralAssistanceDto.PhilHealthNo,
                    Dateofbirth = FuneralAssistanceDto.Dateofbirth,
                    Age = FuneralAssistanceDto.Age,

                    // Requestor Details
                    RLastname = FuneralAssistanceDto.RLastname,
                    RFirstname = FuneralAssistanceDto.RFirstname,
                    RMiddlename = FuneralAssistanceDto.RMiddlename,
                    RSuffix = FuneralAssistanceDto.RSuffix,
                    RBlkLotStreet = FuneralAssistanceDto.RBlkLotStreet,
                    RSubVill = FuneralAssistanceDto.RSubVill,
                    RBrgy = FuneralAssistanceDto.RBrgy,
                    ContactNo = FuneralAssistanceDto.ContactNo,

                    // Assistance Type - Set to default "Funeral Assistance" since no checkboxes
                    Typeassistance = "Funeral Assistance",
                    ForCMOPERSONNEL = FuneralAssistanceDto.ForCMOPERSONNEL,

                    // Additional Information - Encrypted Fields
                    DeceasedPersonName = _aesEncryptionService.Encrypt(FuneralAssistanceDto.DeceasedPersonName),
                    RelationshipToDeceased = _aesEncryptionService.Encrypt(FuneralAssistanceDto.RelationshipToDeceased),
                    DateOfDeath = _aesEncryptionService.Encrypt(FuneralAssistanceDto.DateOfDeath),
                    TimeOfDeath = _aesEncryptionService.Encrypt(FuneralAssistanceDto.TimeOfDeath),
                    CauseOfDeath = _aesEncryptionService.Encrypt(FuneralAssistanceDto.CauseOfDeath),
                    FuneralHomeName = _aesEncryptionService.Encrypt(FuneralAssistanceDto.FuneralHomeName),
                    FuneralHomeAddress = _aesEncryptionService.Encrypt(FuneralAssistanceDto.FuneralHomeAddress),
                    BurialCremationDate = _aesEncryptionService.Encrypt(FuneralAssistanceDto.BurialCremationDate),
                    BurialCremationTime = _aesEncryptionService.Encrypt(FuneralAssistanceDto.BurialCremationTime),
                    BurialCremationType = _aesEncryptionService.Encrypt(FuneralAssistanceDto.BurialCremationType),

                    // MODIFIED: Use existing ID images from user account instead of new uploads
                    Validfrontimage = userFrontID,
                    ValidBackimage = userBackID,
                    DoctorPrescription = newFileNameDocument ?? string.Empty,
                    DeathCertificate = string.Empty,
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = _dateTimeService.Now
                };

                context.FuneralAssistance.Add(FuneralAssistance);
                context.SaveChanges();

                // Send user notification for successful submission
                try
                {
                    var userFullName = $"{FuneralAssistance.Firstname} {FuneralAssistance.Lastname}";
                    await _notificationService.SendStatusChangeNotificationAsync(
                        userId,
                        userFullName,
                        "Funeral",
                        "Pending",
                        FuneralAssistance.Id
                    );
                }
                catch (Exception ex)
                {
                    // Log but don't fail the submission if notification fails
                    Console.WriteLine($"Failed to send user notification: {ex.Message}");
                }

                // Send admin notification for new submission
                try
                {
                    var userFullName = $"{FuneralAssistance.Firstname} {FuneralAssistance.Lastname}";
                    await _adminNotificationService.SendAdminNotificationAsync(
                        "application_submitted",
                        "FuneralAssistance",
                        FuneralAssistance.Id,
                        userId,
                        userFullName,
                        "New Funeral Assistance Application",
                        $"{userFullName} submitted a new Funeral Assistance application.",
                        $"/FuneralAssistancePendingStatus/{FuneralAssistance.Id}"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send admin notification: {ex.Message}");
                }

                // ? SUCCESS: Set the success flag to trigger the modal
                ViewBag.Success = true;

                // Also set the session data again to repopulate the form
                ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
                ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
                ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
                ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
                ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
                ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
                ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
                ViewBag.Gender = HttpContext.Session.GetString("Gender");
                ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");
                ViewBag.FrontID = HttpContext.Session.GetString("FrontID");
                ViewBag.BackID = HttpContext.Session.GetString("BackID");

                return View(FuneralAssistanceDto);
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                {
                    string message = sqlEx.Message.ToLower();

                    if (message.Contains("fullname"))
                        ModelState.AddModelError("Fullname", "This full name is already in use.");
                    else if (message.Contains("username"))
                        ModelState.AddModelError("Username", "This username is already taken.");
                    else if (message.Contains("email"))
                        ModelState.AddModelError("Email", "This email is already registered.");
                    else if (message.Contains("phonenumber"))
                        ModelState.AddModelError("Phonenumber", "This phone number is already in use.");
                    else
                        ModelState.AddModelError("", "A record with one of your inputs already exists.");

                    return View(FuneralAssistanceDto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(FuneralAssistanceDto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(FuneralAssistanceDto);
            }
        }

        public IActionResult FuneralAssistanceedit(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var FuneralAssistance = context.FuneralAssistance.Find(id);
            if (FuneralAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = FuneralAssistance.Status;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
            ViewData["Sex"] = FuneralAssistance.Sex;
            ViewData["PhilHealth"] = FuneralAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = FuneralAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = FuneralAssistance.Dateofbirth;
            ViewData["Age"] = FuneralAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = FuneralAssistance.RLastname;
            ViewData["RFirstname"] = FuneralAssistance.RFirstname;
            ViewData["RMiddlename"] = FuneralAssistance.RMiddlename;
            ViewData["RSuffix"] = FuneralAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = FuneralAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = FuneralAssistance.RSubVill;
            ViewData["RBrgy"] = FuneralAssistance.RBrgy;
                        ViewData["ContactNo"] = FuneralAssistance.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = FuneralAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = FuneralAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(FuneralAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, FuneralAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✓ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(FuneralAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, FuneralAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✓ Back ID decrypted");
                    }
                }

                // ✓ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, FuneralAssistance.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {FuneralAssistance.DoctorPrescription}");
                    debugMessages.Add($"📄 Full path: {prescPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = FuneralAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"✓ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"📄 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Doctor Prescription in database");
                }

                // ✓ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, FuneralAssistance.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {FuneralAssistance.DeathCertificate}");
                    debugMessages.Add($"📄 Full path: {deathPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = FuneralAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"✓ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"📄 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"⚠️ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            ViewData["Comments"] = FuneralAssistance.Comments;

            // ============================================
            // ADD DECRYPTION FOR THESE 10 FIELDS ONLY
            // ============================================

            // Funeral Assistance Fields
            ViewData["DeceasedPersonName"] = DecryptFieldText(FuneralAssistance.DeceasedPersonName);
            ViewData["RelationshipToDeceased"] = DecryptFieldText(FuneralAssistance.RelationshipToDeceased);
            ViewData["DateOfDeath"] = DecryptFieldText(FuneralAssistance.DateOfDeath);
            ViewData["TimeOfDeath"] = DecryptFieldText(FuneralAssistance.TimeOfDeath);
            ViewData["CauseOfDeath"] = DecryptFieldText(FuneralAssistance.CauseOfDeath);
            ViewData["FuneralHomeName"] = DecryptFieldText(FuneralAssistance.FuneralHomeName);
            ViewData["FuneralHomeAddress"] = DecryptFieldText(FuneralAssistance.FuneralHomeAddress);
            ViewData["BurialCremationDate"] = DecryptFieldText(FuneralAssistance.BurialCremationDate);
            ViewData["BurialCremationTime"] = DecryptFieldText(FuneralAssistance.BurialCremationTime);
            ViewData["BurialCremationType"] = DecryptFieldText(FuneralAssistance.BurialCremationType);

            return View();

        }

        [HttpPost]
        public async Task<IActionResult> FuneralAssistanceEdit(int id, FuneralAssistanceDto dto)
        {
            // ? 1. Check user session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            // ? 2. Get existing record
            var existing = context.FuneralAssistance.Find(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Form not found.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ? 3. Security checks
            if (existing.UserId != userId)
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this form.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Check if application is editable: Pending status OR Status2 is "Retake"
            bool isRetakeMode = existing.Status2 == "Retake";
            if (existing.Status != "Pending" && !isRetakeMode)
            {
                TempData["ErrorMessage"] = "You can only edit forms that are in 'Pending' or 'Retake' status.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ? 4. Remove validations for optional fields
            if (string.IsNullOrEmpty(dto.PhilHealthNo))
                ModelState.Remove("PhilHealthNo");

            // Remove validation for ID images - they use session data instead
            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");

            // Remove validation for document - it's optional in edit
            ModelState.Remove("FuneralAssistanceDocument");

            if (!ModelState.IsValid)
            {
                // Repopulate ViewData for validation errors
                PopulateViewDataForFuneralEdit(existing);
                return View(dto);
            }

            try
            {
                var aesHelper = new AesEncryptionHelper(_configuration);

                // ? 5. Update Funeral Assistance Document (optional - only if provided)
                if (dto.FuneralAssistanceDocument != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old files if they exist
                    if (!string.IsNullOrEmpty(existing.DoctorPrescription))
                    {
                        string oldPath = Path.Combine(folder, existing.DoctorPrescription);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    if (!string.IsNullOrEmpty(existing.DeathCertificate))
                    {
                        string oldPath = Path.Combine(folder, existing.DeathCertificate);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    // Encrypt and save new file
                    string originalFileName = dto.FuneralAssistanceDocument.FileName;
                    string fileName = aesHelper.EncryptFilename(originalFileName) + ".enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(dto.FuneralAssistanceDocument.OpenReadStream());
                        await fs.WriteAsync(encrypted, 0, encrypted.Length);
                    }

                    // Store in DoctorPrescription field
                    existing.DoctorPrescription = fileName;
                    existing.DeathCertificate = ""; // Clear the other field
                }

                // ? 6. Update patient information with null checks
                existing.Lastname = dto.Lastname ?? existing.Lastname;
                existing.Firstname = dto.Firstname ?? existing.Firstname;
                existing.Middlename = dto.Middlename ?? existing.Middlename;
                existing.Suffix = (dto.Suffix == "None" ? "" : dto.Suffix) ?? existing.Suffix;
                existing.BlkLotStreet = dto.BlkLotStreet ?? existing.BlkLotStreet;
                existing.SubVill = dto.SubVill ?? existing.SubVill;
                existing.Brgy = dto.Brgy ?? existing.Brgy;
                existing.Sex = dto.Sex ?? existing.Sex;
                existing.PhilHealth = dto.PhilHealth ?? existing.PhilHealth;
                existing.PhilHealthNo = dto.PhilHealthNo ?? existing.PhilHealthNo;
                existing.Dateofbirth = dto.Dateofbirth ?? existing.Dateofbirth;
                existing.Age = dto.Age ?? existing.Age;

                // ? 7. Update requestor details
                existing.RLastname = dto.RLastname ?? existing.RLastname;
                existing.RFirstname = dto.RFirstname ?? existing.RFirstname;
                existing.RMiddlename = dto.RMiddlename ?? existing.RMiddlename;
                existing.RSuffix = (dto.RSuffix == "None" ? "" : dto.RSuffix) ?? existing.RSuffix;
                existing.RBlkLotStreet = dto.RBlkLotStreet ?? existing.RBlkLotStreet;
                existing.RSubVill = dto.RSubVill ?? existing.RSubVill;
                existing.RBrgy = dto.RBrgy ?? existing.RBrgy;
                existing.ContactNo = dto.ContactNo ?? existing.ContactNo;

                // ? 8. Update assistance information
                existing.Typeassistance = dto.Typeassistance ?? existing.Typeassistance;
                existing.ForCMOPERSONNEL = dto.ForCMOPERSONNEL ?? existing.ForCMOPERSONNEL;

                // ? 9. Encrypt and update funeral information fields
                if (!string.IsNullOrEmpty(dto.DeceasedPersonName))
                    existing.DeceasedPersonName = _aesEncryptionService.Encrypt(dto.DeceasedPersonName);

                if (!string.IsNullOrEmpty(dto.RelationshipToDeceased))
                    existing.RelationshipToDeceased = _aesEncryptionService.Encrypt(dto.RelationshipToDeceased);

                if (!string.IsNullOrEmpty(dto.DateOfDeath))
                    existing.DateOfDeath = _aesEncryptionService.Encrypt(dto.DateOfDeath);

                if (!string.IsNullOrEmpty(dto.TimeOfDeath))
                    existing.TimeOfDeath = _aesEncryptionService.Encrypt(dto.TimeOfDeath);

                if (!string.IsNullOrEmpty(dto.CauseOfDeath))
                    existing.CauseOfDeath = _aesEncryptionService.Encrypt(dto.CauseOfDeath);

                if (!string.IsNullOrEmpty(dto.FuneralHomeName))
                    existing.FuneralHomeName = _aesEncryptionService.Encrypt(dto.FuneralHomeName);

                if (!string.IsNullOrEmpty(dto.FuneralHomeAddress))
                    existing.FuneralHomeAddress = _aesEncryptionService.Encrypt(dto.FuneralHomeAddress);

                if (!string.IsNullOrEmpty(dto.BurialCremationDate))
                    existing.BurialCremationDate = _aesEncryptionService.Encrypt(dto.BurialCremationDate);

                if (!string.IsNullOrEmpty(dto.BurialCremationTime))
                    existing.BurialCremationTime = _aesEncryptionService.Encrypt(dto.BurialCremationTime);

                if (!string.IsNullOrEmpty(dto.BurialCremationType))
                    existing.BurialCremationType = _aesEncryptionService.Encrypt(dto.BurialCremationType);

                // ? 10. Update ID images (if session has updated IDs)
                string frontID = HttpContext.Session.GetString("FrontID") ?? "";
                string backID = HttpContext.Session.GetString("BackID") ?? "";
                if (!string.IsNullOrEmpty(frontID)) existing.Validfrontimage = frontID;
                if (!string.IsNullOrEmpty(backID)) existing.ValidBackimage = backID;

                // ? 11. Handle retake mode vs normal edit mode
                if (!isRetakeMode)
                {
                    // Normal edit - update timestamp
                    existing.CreatedAt = _dateTimeService.Now;
                }
                else
                {
                    // Retake mode - reset all processing fields
                    existing.Status = "Processing"; // Go directly to Processing for review
                    existing.Status2 = "Resubmitted"; // Mark as resubmitted
                    existing.Status3 = "";
                    existing.CreatedAt = _dateTimeService.Now; // Reset timestamp
                    existing.ProcessAt = _dateTimeService.Now;
                    existing.Processby = "";
                    existing.Result = new DateTime(1900, 1, 1);
                    existing.ClaimedAt = new DateTime(1900, 1, 1);
                    existing.IsRetakeApplication = false;
                    existing.RetakeReason = "";
                    existing.RetakeRequestedAt = null;
                    existing.Comments = "";
                }

                // ? 12. Save changes
                context.Entry(existing).State = EntityState.Modified;
                await context.SaveChangesAsync();

                // ? 13. Send notifications for retake mode
                if (isRetakeMode)
                {
                    try
                    {
                        var userFullName = $"{existing.Firstname} {existing.Lastname}";

                        // User notification
                        await _notificationService.SendStatusChangeNotificationAsync(
                            userId,
                            userFullName,
                            "Funeral",
                            "Processing",
                            existing.Id
                        );

                        // Admin notification
                        await _adminNotificationService.SendAdminNotificationAsync(
                            "application_submitted",
                            "FuneralAssistance",
                            existing.Id,
                            userId,
                            userFullName,
                            "[PRIORITY] Retake Resubmitted - Funeral Assistance",
                            $"{userFullName} resubmitted their retake application for Funeral Assistance. PRIORITY: Ready for immediate review.",
                            $"/FuneralAssistanceProcessingStatus/{existing.Id}"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail
                        Console.WriteLine($"Notification error: {ex.Message}");
                    }
                }

                // ? 14. Set success flag
                TempData["Success"] = true;
                TempData["RAF"] = id.ToString();

                // ? 15. Return with success - redirect to show updated data
                return RedirectToAction("FuneralAssistanceEdit", new { id = id, success = true });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating the form: {ex.Message}");
                PopulateViewDataForFuneralEdit(existing);
                return View(dto);
            }
        }

        // Helper method to populate ViewData for Funeral Assistance edit
        private void PopulateViewDataForFuneralEdit(FuneralAssistance existing)
        {
            ViewData["Id"] = existing.Id;
            ViewData["Lastname"] = existing.Lastname;
            ViewData["Firstname"] = existing.Firstname;
            ViewData["Middlename"] = existing.Middlename;
            ViewData["Suffix"] = existing.Suffix;
            ViewData["BlkLotStreet"] = existing.BlkLotStreet;
            ViewData["SubVill"] = existing.SubVill;
            ViewData["Brgy"] = existing.Brgy;
            ViewData["Sex"] = existing.Sex;
            ViewData["PhilHealth"] = existing.PhilHealth;
            ViewData["PhilHealthNo"] = existing.PhilHealthNo;
            ViewData["Dateofbirth"] = existing.Dateofbirth;
            ViewData["Age"] = existing.Age;

            ViewData["RLastname"] = existing.RLastname;
            ViewData["RFirstname"] = existing.RFirstname;
            ViewData["RMiddlename"] = existing.RMiddlename;
            ViewData["RSuffix"] = existing.RSuffix;
            ViewData["RBlkLotStreet"] = existing.RBlkLotStreet;
            ViewData["RSubVill"] = existing.RSubVill;
            ViewData["RBrgy"] = existing.RBrgy;
            ViewData["ContactNo"] = existing.ContactNo;

            ViewData["Typeassistance"] = existing.Typeassistance;
            ViewData["ForCMOPERSONNEL"] = existing.ForCMOPERSONNEL;

            // Decrypt and show funeral information fields
            ViewData["DeceasedPersonName"] = DecryptFieldText(existing.DeceasedPersonName);
            ViewData["RelationshipToDeceased"] = DecryptFieldText(existing.RelationshipToDeceased);
            ViewData["DateOfDeath"] = DecryptFieldText(existing.DateOfDeath);
            ViewData["TimeOfDeath"] = DecryptFieldText(existing.TimeOfDeath);
            ViewData["CauseOfDeath"] = DecryptFieldText(existing.CauseOfDeath);
            ViewData["FuneralHomeName"] = DecryptFieldText(existing.FuneralHomeName);
            ViewData["FuneralHomeAddress"] = DecryptFieldText(existing.FuneralHomeAddress);
            ViewData["BurialCremationDate"] = DecryptFieldText(existing.BurialCremationDate);
            ViewData["BurialCremationTime"] = DecryptFieldText(existing.BurialCremationTime);
            ViewData["BurialCremationType"] = DecryptFieldText(existing.BurialCremationType);

            ViewData["CurrentDoctorPrescription"] = existing.DoctorPrescription;
            ViewData["CurrentDeathCertificate"] = existing.DeathCertificate;
            ViewData["CurrentValidFront"] = existing.Validfrontimage;
            ViewData["CurrentValidBack"] = existing.ValidBackimage;
            ViewData["IsRetakeMode"] = existing.Status2 == "Retake";
        }


        public IActionResult FuneralAssistanceedelete(int id)
        {
            var FuneralAssistance = context.FuneralAssistance.Find(id);
            if (FuneralAssistance == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Get user ID for notification cleanup
            var userId = FuneralAssistance.UserId;

            // Instead of deleting files and record, just update the status
            FuneralAssistance.Status = "Removed";
            context.FuneralAssistance.Update(FuneralAssistance);
            
            // Archive all notifications related to this application
            var relatedNotifications = context.Notifications
                .Where(n => n.UserId == userId && 
                           n.ApplicationType == "FuneralAssistance" && 
                           n.ApplicationId == id)
                .ToList();
            
            foreach (var notification in relatedNotifications)
            {
                notification.IsArchived = true;
            }
            
            context.SaveChanges();

            return RedirectToAction("Homepage", "Dashboard");
        }





        public IActionResult Applicationtracking()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
            {
                // If the UserId is not in session or invalid, redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Pass userId to ViewBag for SignalR/tracking
            ViewBag.UserId = userId;

            // Calculate cutoff date: 1 month ago from now (Philippine time)
            var oneMonthAgo = _dateTimeService.Now.AddMonths(-1);

            // ⚡ PERFORMANCE: Optimized to reduce database queries and in-memory filtering
            // Previous: 3 queries fetching ALL records, then filtering in memory multiple times
            // New: Targeted queries with database-level filtering

            // Auto-archive applications older than 1 month (only fetch records that need archiving)
            var hospitalToArchive = context.HospitalAssistance
                .Where(f => f.UserId == userId && f.CreatedAt < oneMonthAgo && !f.IsArchived)
                .ToList();

            var medicalToArchive = context.OtherAssistance
                .Where(f => f.UserId == userId && f.CreatedAt < oneMonthAgo && !f.IsArchived)
                .ToList();

            var funeralToArchive = context.FuneralAssistance
                .Where(f => f.UserId == userId && f.CreatedAt < oneMonthAgo && !f.IsArchived)
                .ToList();

            // Mark as archived
            foreach (var app in hospitalToArchive) app.IsArchived = true;
            foreach (var app in medicalToArchive) app.IsArchived = true;
            foreach (var app in funeralToArchive) app.IsArchived = true;

            // Save changes to database if any applications were archived
            if (hospitalToArchive.Any() || medicalToArchive.Any() || funeralToArchive.Any())
            {
                context.SaveChanges();
            }

            // Fetch active and archived separately with proper filtering at database level
            // AsNoTracking for 20-30% performance improvement on read-only queries
            var activeHospitalBills = context.HospitalAssistance
                .AsNoTracking()
                .Where(f => f.UserId == userId && !f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var archivedHospitalBills = context.HospitalAssistance
                .AsNoTracking()
                .Where(f => f.UserId == userId && f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var activeMedicalLabForms = context.OtherAssistance
                .AsNoTracking()
                .Where(f => f.UserId == userId && !f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var archivedMedicalLabForms = context.OtherAssistance
                .AsNoTracking()
                .Where(f => f.UserId == userId && f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var activeFuneralAssistance = context.FuneralAssistance
                .AsNoTracking()
                .Where(f => f.UserId == userId && !f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var archivedFuneralAssistance = context.FuneralAssistance
                .AsNoTracking()
                .Where(f => f.UserId == userId && f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                // Active applications (less than 1 month old)
                HospitalAssistance = activeHospitalBills,
                OtherAssistance = activeMedicalLabForms,
                FuneralAssistance = activeFuneralAssistance,

                // Archived applications (1 month or older)
                ArchivedHospitalAssistance = archivedHospitalBills,
                ArchivedOtherAssistance = archivedMedicalLabForms,
                ArchivedFuneralAssistance = archivedFuneralAssistance
            };

            // Pass the view model to the view
            return View(viewModel);

        }

        // API endpoint for real-time application status updates
        [HttpGet]
        public JsonResult GetApplicationUpdates()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "Invalid user" });
            }

            try
            {
                // Get updated data from database
                var hospitalBills = context.HospitalAssistance
                    .Where(f => f.UserId == userId)
                    .Select(f => new
                    {
                        f.Id,
                        f.Firstname,
                        f.Middlename,
                        f.Lastname,
                        f.Status,
                        f.Status2,
                        f.Status3,
                        f.CreatedAt,
                        f.ProcessAt,
                        f.Processby,
                        f.Result,
                        f.ClaimedAt,
                        f.Comments
                    })
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();

                var medicalLabForms = context.OtherAssistance
                    .Where(f => f.UserId == userId)
                    .Select(f => new
                    {
                        f.Id,
                        f.Firstname,
                        f.Middlename,
                        f.Lastname,
                        f.Status,
                        f.Status2,
                        f.Status3,
                        f.CreatedAt,
                        f.ProcessAt,
                        f.Processby,
                        f.Result,
                        f.ClaimedAt,
                        f.Comments
                    })
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();

                var funeralForms = context.FuneralAssistance
                    .Where(f => f.UserId == userId)
                    .Select(f => new
                    {
                        f.Id,
                        f.Firstname,
                        f.Middlename,
                        f.Lastname,
                        f.Status,
                        f.Status2,
                        f.Status3,
                        f.CreatedAt,
                        f.ProcessAt,
                        f.Processby,
                        f.Result,
                        f.ClaimedAt,
                        f.Comments
                    })
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        hospitalBills,
                        medicalLabForms,
                        funeralForms
                    },
                    timestamp = _dateTimeService.Now
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching updates", error = ex.Message });
            }
        }

        // Public page - no authentication required
        public IActionResult Nearbyoffices()
        {
            return View();
        }

        public IActionResult Eligibilitychecking()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
            {
                // If the UserId is not in session or invalid, redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Get data from database filtered by userId
            var hospitalBills = context.HospitalAssistance
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.OtherAssistance
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var FuneralAssistance = context.FuneralAssistance
             .Where(f => f.UserId == userId)
             .OrderByDescending(f => f.CreatedAt)
             .ToList();

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalAssistance = hospitalBills,
                OtherAssistance = medicalLabForms,
                FuneralAssistance = FuneralAssistance //
            };

            // Pass the view model to the view
            return View(viewModel);
        }

        public IActionResult HospitalAssistanceview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var HospitalAssistance = context.HospitalAssistance.Find(id);
            if (HospitalAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = HospitalAssistance.Status;
            ViewData["Id"] = HospitalAssistance.Id;
            ViewData["Lastname"] = HospitalAssistance.Lastname;
            ViewData["Firstname"] = HospitalAssistance.Firstname;
            ViewData["Middlename"] = HospitalAssistance.Middlename;
            ViewData["Suffix"] = HospitalAssistance.Suffix;
            ViewData["BlkLotStreet"] = HospitalAssistance.BlkLotStreet;
            ViewData["SubVill"] = HospitalAssistance.SubVill;
            ViewData["Brgy"] = HospitalAssistance.Brgy;
            ViewData["Sex"] = HospitalAssistance.Sex;
            ViewData["PhilHealth"] = HospitalAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = HospitalAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = HospitalAssistance.Dateofbirth;
            ViewData["Age"] = HospitalAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = HospitalAssistance.RLastname;
            ViewData["RFirstname"] = HospitalAssistance.RFirstname;
            ViewData["RMiddlename"] = HospitalAssistance.RMiddlename;
            ViewData["RSuffix"] = HospitalAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = HospitalAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = HospitalAssistance.RSubVill;
            ViewData["RBrgy"] = HospitalAssistance.RBrgy;
            ViewData["RelationshipPatient"] = HospitalAssistance.RelationshipPatient;
            ViewData["ContactNo"] = HospitalAssistance.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = HospitalAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = HospitalAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(HospitalAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, HospitalAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✓ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(HospitalAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, HospitalAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✓ Back ID decrypted");
                    }
                }

                // Doctor Prescription - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, HospitalAssistance.DoctorPrescription);
                    debugMessages.Add($"🔍 Doctor Prescription filename: {HospitalAssistance.DoctorPrescription}");
                    debugMessages.Add($"🔍 Full path: {prescPath}");
                    debugMessages.Add($"🔍 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = HospitalAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"✓ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"🔍 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"⚠ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("⚠ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("🔍 No Doctor Prescription in database");
                }

                // Death Certificate - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, HospitalAssistance.DeathCertificate);
                    debugMessages.Add($"🔍 Death Certificate filename: {HospitalAssistance.DeathCertificate}");
                    debugMessages.Add($"🔍 Full path: {deathPath}");
                    debugMessages.Add($"🔍 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = HospitalAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"✓ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"🔍 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"⚠ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("⚠ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("🔍 No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"❌ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = HospitalAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = HospitalAssistance.ValidBackimage;
            ViewData["Comments"] = HospitalAssistance.Comments;

            // ============================================
            // STORE BOTH ENCRYPTED AND DECRYPTED VALUES
            // ============================================
            // Store ENCRYPTED values (for initial display in grey/black boxes)
            ViewData["HospitalFacilityNameEncrypted"] = HospitalAssistance.HospitalFacilityName;
            ViewData["HospitalFacilityAddressEncrypted"] = HospitalAssistance.HospitalFacilityAddress;
            ViewData["DiagnosisMedicalConditionEncrypted"] = HospitalAssistance.DiagnosisMedicalCondition;
            ViewData["HospitalBillCostEncrypted"] = HospitalAssistance.HospitalBillCost;
            ViewData["AdmissionDateEncrypted"] = HospitalAssistance.AdmissionDate;
            ViewData["DischargeDateEncrypted"] = HospitalAssistance.DischargeDate;
            ViewData["WardRoomTypeEncrypted"] = HospitalAssistance.WardRoomType;

            // Store DECRYPTED values (for display after password verification)
            ViewData["HospitalFacilityName"] = DecryptFieldText(HospitalAssistance.HospitalFacilityName);
            ViewData["HospitalFacilityAddress"] = DecryptFieldText(HospitalAssistance.HospitalFacilityAddress);
            ViewData["DiagnosisMedicalCondition"] = DecryptFieldText(HospitalAssistance.DiagnosisMedicalCondition);
            ViewData["HospitalBillCost"] = DecryptFieldText(HospitalAssistance.HospitalBillCost);
            ViewData["AdmissionDate"] = DecryptFieldText(HospitalAssistance.AdmissionDate);
            ViewData["DischargeDate"] = DecryptFieldText(HospitalAssistance.DischargeDate);
            ViewData["WardRoomType"] = DecryptFieldText(HospitalAssistance.WardRoomType);

            // ============================================
            // ADD TRACKING DATA FOR TIMELINE
            // ============================================

            // Created/Submitted Date
            ViewData["CreatedAt"] = HospitalAssistance.CreatedAt;

            // Processing Date
            ViewData["ProcessAt"] = HospitalAssistance.ProcessAt;
            ViewData["Processby"] = HospitalAssistance.Processby;

            // Result Date (Approval/Disapproval/Retake)
            ViewData["Result"] = HospitalAssistance.Result;
            ViewData["Status2"] = HospitalAssistance.Status2;
            ViewData["RetakeReason"] = HospitalAssistance.RetakeReason;

            // Claimed Date
            ViewData["ClaimedAt"] = HospitalAssistance.ClaimedAt;
            ViewData["Status3"] = HospitalAssistance.Status3;

            // ============================================
            // ADDITIONAL VIEWDATA FOR TRACKING DISPLAY
            // ============================================

            // Determine the current status badge type
            string currentStatusBadge = "";
            if (HospitalAssistance.Status2 == "Approve")
            {
                if (HospitalAssistance.Status3 == "Claimed")
                {
                    currentStatusBadge = "status-claimed-badge";
                }
                else
                {
                    currentStatusBadge = "status-approved-badge";
                }
            }
            else if (HospitalAssistance.Status2 == "Disapprove" || HospitalAssistance.Status2 == "Unapproved")
            {
                currentStatusBadge = "status-disapproved-badge";
            }
            else if (HospitalAssistance.Status2 == "Retake")
            {
                currentStatusBadge = "status-retake-badge";
            }
            else
            {
                currentStatusBadge = "status-pending-badge";
            }

            ViewData["CurrentStatusBadge"] = currentStatusBadge;
            ViewData["CurrentStatusText"] = HospitalAssistance.Status2 == "Approve" && HospitalAssistance.Status3 == "Claimed"
                ? "CLAIMED"
                : !string.IsNullOrEmpty(HospitalAssistance.Status2)
                    ? HospitalAssistance.Status2.ToUpper()
                    : "PENDING";

            // For timeline display
            ViewData["HasProcessing"] = HospitalAssistance.ProcessAt > DateTime.MinValue &&
                                        HospitalAssistance.ProcessAt.Year > 1 &&
                                        !string.IsNullOrWhiteSpace(HospitalAssistance.Processby);

            ViewData["HasResult"] = HospitalAssistance.Result > DateTime.MinValue &&
                                    HospitalAssistance.Result.Year > 1;

            ViewData["HasClaimed"] = HospitalAssistance.ClaimedAt > DateTime.MinValue &&
                                     HospitalAssistance.ClaimedAt.Year > 1 &&
                                     HospitalAssistance.Status3 == "Claimed";

            ViewData["IsApproved"] = HospitalAssistance.Status2 == "Approve";
            ViewData["IsRetake"] = HospitalAssistance.Status2 == "Retake";
            ViewData["IsUnapproved"] = HospitalAssistance.Status2 == "Disapprove" ||
                                       HospitalAssistance.Status2 == "Unapproved";

            ViewData["IsReadyForClaim"] = HospitalAssistance.Status2 == "Approve" &&
                                          string.IsNullOrEmpty(HospitalAssistance.Status3);

            // ============================================
            // FOR ADMIN STATUS UPDATE
            // ============================================
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                ViewData["IsAdmin"] = true;
                ViewData["AdminFullname"] = HttpContext.Session.GetString("AdminFullname");
            }
            else
            {
                ViewData["IsAdmin"] = false;
            }

            return View();
        }

        public IActionResult FuneralAssistanceview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var FuneralAssistance = context.FuneralAssistance.Find(id);
            if (FuneralAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = FuneralAssistance.Status;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
            ViewData["Sex"] = FuneralAssistance.Sex;
            ViewData["PhilHealth"] = FuneralAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = FuneralAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = FuneralAssistance.Dateofbirth;
            ViewData["Age"] = FuneralAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = FuneralAssistance.RLastname;
            ViewData["RFirstname"] = FuneralAssistance.RFirstname;
            ViewData["RMiddlename"] = FuneralAssistance.RMiddlename;
            ViewData["RSuffix"] = FuneralAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = FuneralAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = FuneralAssistance.RSubVill;
            ViewData["RBrgy"] = FuneralAssistance.RBrgy;
            ViewData["ContactNo"] = FuneralAssistance.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = FuneralAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = FuneralAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(FuneralAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, FuneralAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✓ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(FuneralAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, FuneralAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✓ Back ID decrypted");
                    }
                }

                // Doctor Prescription - UPDATED
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, FuneralAssistance.DoctorPrescription);
                    debugMessages.Add($"🔍 Doctor Prescription filename: {FuneralAssistance.DoctorPrescription}");
                    debugMessages.Add($"🔍 Full path: {prescPath}");
                    debugMessages.Add($"🔍 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = FuneralAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"✓ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"🔍 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"⚠ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("⚠ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("🔍 No Doctor Prescription in database");
                }

                // Death Certificate - UPDATED
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, FuneralAssistance.DeathCertificate);
                    debugMessages.Add($"🔍 Death Certificate filename: {FuneralAssistance.DeathCertificate}");
                    debugMessages.Add($"🔍 Full path: {deathPath}");
                    debugMessages.Add($"🔍 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = FuneralAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"✓ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"🔍 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"⚠ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("⚠ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("🔍 No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"❌ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            ViewData["Comments"] = FuneralAssistance.Comments;

            // ============================================
            // STORE BOTH ENCRYPTED AND DECRYPTED VALUES FOR THE 10 FIELDS
            // ============================================

            // Store ENCRYPTED values (for initial display in grey/black boxes or for admin verification)
            ViewData["DeceasedPersonNameEncrypted"] = FuneralAssistance.DeceasedPersonName;
            ViewData["RelationshipToDeceasedEncrypted"] = FuneralAssistance.RelationshipToDeceased;
            ViewData["DateOfDeathEncrypted"] = FuneralAssistance.DateOfDeath;
            ViewData["TimeOfDeathEncrypted"] = FuneralAssistance.TimeOfDeath;
            ViewData["CauseOfDeathEncrypted"] = FuneralAssistance.CauseOfDeath;
            ViewData["FuneralHomeNameEncrypted"] = FuneralAssistance.FuneralHomeName;
            ViewData["FuneralHomeAddressEncrypted"] = FuneralAssistance.FuneralHomeAddress;
            ViewData["BurialCremationDateEncrypted"] = FuneralAssistance.BurialCremationDate;
            ViewData["BurialCremationTimeEncrypted"] = FuneralAssistance.BurialCremationTime;
            ViewData["BurialCremationTypeEncrypted"] = FuneralAssistance.BurialCremationType;

            // Store DECRYPTED values (for display after password verification)
            ViewData["DeceasedPersonName"] = DecryptFieldText(FuneralAssistance.DeceasedPersonName);
            ViewData["RelationshipToDeceased"] = DecryptFieldText(FuneralAssistance.RelationshipToDeceased);
            ViewData["DateOfDeath"] = DecryptFieldText(FuneralAssistance.DateOfDeath);
            ViewData["TimeOfDeath"] = DecryptFieldText(FuneralAssistance.TimeOfDeath);
            ViewData["CauseOfDeath"] = DecryptFieldText(FuneralAssistance.CauseOfDeath);
            ViewData["FuneralHomeName"] = DecryptFieldText(FuneralAssistance.FuneralHomeName);
            ViewData["FuneralHomeAddress"] = DecryptFieldText(FuneralAssistance.FuneralHomeAddress);
            ViewData["BurialCremationDate"] = DecryptFieldText(FuneralAssistance.BurialCremationDate);
            ViewData["BurialCremationTime"] = DecryptFieldText(FuneralAssistance.BurialCremationTime);
            ViewData["BurialCremationType"] = DecryptFieldText(FuneralAssistance.BurialCremationType);

            // ============================================
            // ADD TRACKING DATA FOR TIMELINE
            // ============================================

            // Created/Submitted Date
            ViewData["CreatedAt"] = FuneralAssistance.CreatedAt;

            // Processing Date
            ViewData["ProcessAt"] = FuneralAssistance.ProcessAt;
            ViewData["Processby"] = FuneralAssistance.Processby;

            // Result Date (Approval/Disapproval/Retake)
            ViewData["Result"] = FuneralAssistance.Result;
            ViewData["Status2"] = FuneralAssistance.Status2;
            ViewData["RetakeReason"] = FuneralAssistance.RetakeReason;

            // Claimed Date
            ViewData["ClaimedAt"] = FuneralAssistance.ClaimedAt;
            ViewData["Status3"] = FuneralAssistance.Status3;

            // ============================================
            // ADDITIONAL VIEWDATA FOR TRACKING DISPLAY
            // ============================================

            // Determine the current status badge type
            string currentStatusBadge = "";
            if (FuneralAssistance.Status2 == "Approve")
            {
                if (FuneralAssistance.Status3 == "Claimed")
                {
                    currentStatusBadge = "status-claimed-badge";
                }
                else
                {
                    currentStatusBadge = "status-approved-badge";
                }
            }
            else if (FuneralAssistance.Status2 == "Disapprove" || FuneralAssistance.Status2 == "Unapproved")
            {
                currentStatusBadge = "status-disapproved-badge";
            }
            else if (FuneralAssistance.Status2 == "Retake")
            {
                currentStatusBadge = "status-retake-badge";
            }
            else
            {
                currentStatusBadge = "status-pending-badge";
            }

            ViewData["CurrentStatusBadge"] = currentStatusBadge;
            ViewData["CurrentStatusText"] = FuneralAssistance.Status2 == "Approve" && FuneralAssistance.Status3 == "Claimed"
                ? "CLAIMED"
                : !string.IsNullOrEmpty(FuneralAssistance.Status2)
                    ? FuneralAssistance.Status2.ToUpper()
                    : "PENDING";

            // For timeline display
            ViewData["HasProcessing"] = FuneralAssistance.ProcessAt > DateTime.MinValue &&
                                      FuneralAssistance.ProcessAt.Year > 1 &&
                                      !string.IsNullOrWhiteSpace(FuneralAssistance.Processby);

            ViewData["HasResult"] = FuneralAssistance.Result > DateTime.MinValue &&
                                   FuneralAssistance.Result.Year > 1;

            ViewData["HasClaimed"] = FuneralAssistance.ClaimedAt > DateTime.MinValue &&
                                    FuneralAssistance.ClaimedAt.Year > 1 &&
                                    FuneralAssistance.Status3 == "Claimed";

            ViewData["IsApproved"] = FuneralAssistance.Status2 == "Approve";
            ViewData["IsRetake"] = FuneralAssistance.Status2 == "Retake";
            ViewData["IsUnapproved"] = FuneralAssistance.Status2 == "Disapprove" ||
                                       FuneralAssistance.Status2 == "Unapproved";

            ViewData["IsReadyForClaim"] = FuneralAssistance.Status2 == "Approve" &&
                                          string.IsNullOrEmpty(FuneralAssistance.Status3);

            // ============================================
            // FOR ADMIN STATUS UPDATE
            // ============================================
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                ViewData["IsAdmin"] = true;
                ViewData["AdminFullname"] = HttpContext.Session.GetString("AdminFullname");
            }
            else
            {
                ViewData["IsAdmin"] = false;
            }

            return View();
        }

        public IActionResult OtherAssistanceview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.OtherAssistance.Find(id);
            if (medicallabform == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = medicallabform.Status;
            ViewData["Id"] = medicallabform.Id;
            ViewData["Lastname"] = medicallabform.Lastname;
            ViewData["Firstname"] = medicallabform.Firstname;
            ViewData["Middlename"] = medicallabform.Middlename;
            ViewData["Suffix"] = medicallabform.Suffix;
            ViewData["BlkLotStreet"] = medicallabform.BlkLotStreet;
            ViewData["SubVill"] = medicallabform.SubVill;
            ViewData["Brgy"] = medicallabform.Brgy;
            ViewData["Sex"] = medicallabform.Sex;
            ViewData["PhilHealth"] = medicallabform.PhilHealth;
            ViewData["PhilHealthNo"] = medicallabform.PhilHealthNo;
            ViewData["Dateofbirth"] = medicallabform.Dateofbirth;
            ViewData["Age"] = medicallabform.Age;

            // Requestor details
            ViewData["RLastname"] = medicallabform.RLastname;
            ViewData["RFirstname"] = medicallabform.RFirstname;
            ViewData["RMiddlename"] = medicallabform.RMiddlename;
            ViewData["RSuffix"] = medicallabform.RSuffix;
            ViewData["RBlkLotStreet"] = medicallabform.RBlkLotStreet;
            ViewData["RSubVill"] = medicallabform.RSubVill;
            ViewData["RBrgy"] = medicallabform.RBrgy;
            ViewData["RelationshipPatient"] = medicallabform.RelationshipPatient;
            ViewData["ContactNo"] = medicallabform.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = medicallabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = medicallabform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(medicallabform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, medicallabform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✓ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(medicallabform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, medicallabform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✓ Back ID decrypted");
                    }
                }

                // Doctor Prescription - UPDATED
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"🔍 Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"🔍 Full path: {prescPath}");
                    debugMessages.Add($"🔍 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = medicallabform.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"✓ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"🔍 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"⚠ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("⚠ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("🔍 No Doctor Prescription in database");
                }

                // Medical Certificate - UPDATED
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"🔍 Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"🔍 Full path: {medicalPath}");
                    debugMessages.Add($"🔍 File exists: {System.IO.File.Exists(medicalPath)}");

                    if (System.IO.File.Exists(medicalPath))
                    {
                        try
                        {
                            byte[] decryptedMedical = DecryptFile(medicalPath);
                            ViewData["MedicalCertificateBase64"] = Convert.ToBase64String(decryptedMedical);
                            ViewData["MedicalCertificate"] = medicallabform.MedCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedMedical);
                            ViewData["IsMedicalCertificatePdf"] = isPdf;

                            debugMessages.Add($"✓ Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"🔍 IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"⚠ Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("⚠ Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("🔍 No Medical Certificate in database");
                }

                // Death Certificate - UPDATED
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"🔍 Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"🔍 Full path: {deathPath}");
                    debugMessages.Add($"🔍 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = medicallabform.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"✓ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"🔍 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"⚠ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("⚠ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("🔍 No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"❌ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            // ============================================
            // ADD DECRYPTION FOR THESE 19 FIELDS ONLY
            // ============================================

            // Use your existing DecryptFile logic to decrypt text fields
            // Medicine Assistance Fields
            ViewData["MedicineName"] = DecryptFieldText(medicallabform.MedicineName);
            ViewData["MedicineQuantity"] = DecryptFieldText(medicallabform.MedicineQuantity);
            ViewData["MedicineCost"] = DecryptFieldText(medicallabform.MedicineCost);
            ViewData["PrescribingDoctor"] = DecryptFieldText(medicallabform.PrescribingDoctor);
            ViewData["DoctorContactDetail"] = DecryptFieldText(medicallabform.DoctorContactDetail);

            // Laboratory Assistance Fields
            ViewData["LaboratoryCenterName"] = DecryptFieldText(medicallabform.LaboratoryCenterName);
            ViewData["LaboratoryCenterAddress"] = DecryptFieldText(medicallabform.LaboratoryCenterAddress);
            ViewData["TestName"] = DecryptFieldText(medicallabform.TestName);
            ViewData["TestCost"] = DecryptFieldText(medicallabform.TestCost);
            ViewData["TestOtherInfo"] = DecryptFieldText(medicallabform.TestOtherInfo);

            // Therapy Assistance Fields
            ViewData["TherapyFacilityName"] = DecryptFieldText(medicallabform.TherapyFacilityName);
            ViewData["TherapyFacilityAddress"] = DecryptFieldText(medicallabform.TherapyFacilityAddress);
            ViewData["TherapyFacilityContact"] = DecryptFieldText(medicallabform.TherapyFacilityContact);
            ViewData["TherapyType"] = DecryptFieldText(medicallabform.TherapyType);

            // Equipment Assistance Fields
            ViewData["EquipmentName"] = DecryptFieldText(medicallabform.EquipmentName);
            ViewData["EquipmentBrand"] = DecryptFieldText(medicallabform.EquipmentBrand);
            ViewData["EquipmentCategory"] = DecryptFieldText(medicallabform.EquipmentCategory);
            ViewData["EquipmentQuantity"] = DecryptFieldText(medicallabform.EquipmentQuantity);
            ViewData["EquipmentCost"] = DecryptFieldText(medicallabform.EquipmentCost);

            // ============================================
            // ADD TRACKING DATA FOR TIMELINE
            // ============================================

            // Created/Submitted Date
            ViewData["CreatedAt"] = medicallabform.CreatedAt;

            // Processing Date
            ViewData["ProcessAt"] = medicallabform.ProcessAt;
            ViewData["Processby"] = medicallabform.Processby;

            // Result Date (Approval/Disapproval/Retake)
            ViewData["Result"] = medicallabform.Result;
            ViewData["Status2"] = medicallabform.Status2;
            ViewData["RetakeReason"] = medicallabform.RetakeReason;

            // Claimed Date
            ViewData["ClaimedAt"] = medicallabform.ClaimedAt;
            ViewData["Status3"] = medicallabform.Status3;

            // ============================================
            // ADDITIONAL VIEWDATA FOR TRACKING DISPLAY
            // ============================================

            // Determine the current status badge type
            string currentStatusBadge = "";
            if (medicallabform.Status2 == "Approve")
            {
                if (medicallabform.Status3 == "Claimed")
                {
                    currentStatusBadge = "status-claimed-badge";
                }
                else
                {
                    currentStatusBadge = "status-approved-badge";
                }
            }
            else if (medicallabform.Status2 == "Disapprove" || medicallabform.Status2 == "Unapproved")
            {
                currentStatusBadge = "status-disapproved-badge";
            }
            else if (medicallabform.Status2 == "Retake")
            {
                currentStatusBadge = "status-retake-badge";
            }
            else
            {
                currentStatusBadge = "status-pending-badge";
            }

            ViewData["CurrentStatusBadge"] = currentStatusBadge;
            ViewData["CurrentStatusText"] = medicallabform.Status2 == "Approve" && medicallabform.Status3 == "Claimed"
                ? "CLAIMED"
                : !string.IsNullOrEmpty(medicallabform.Status2)
                    ? medicallabform.Status2.ToUpper()
                    : "PENDING";

            // For timeline display
            ViewData["HasProcessing"] = medicallabform.ProcessAt > DateTime.MinValue &&
                                      medicallabform.ProcessAt.Year > 1 &&
                                      !string.IsNullOrWhiteSpace(medicallabform.Processby);

            ViewData["HasResult"] = medicallabform.Result > DateTime.MinValue &&
                                   medicallabform.Result.Year > 1;

            ViewData["HasClaimed"] = medicallabform.ClaimedAt > DateTime.MinValue &&
                                    medicallabform.ClaimedAt.Year > 1 &&
                                    medicallabform.Status3 == "Claimed";

            ViewData["IsApproved"] = medicallabform.Status2 == "Approve";
            ViewData["IsRetake"] = medicallabform.Status2 == "Retake";
            ViewData["IsUnapproved"] = medicallabform.Status2 == "Disapprove" ||
                                       medicallabform.Status2 == "Unapproved";

            ViewData["IsReadyForClaim"] = medicallabform.Status2 == "Approve" &&
                                          string.IsNullOrEmpty(medicallabform.Status3);

            // ============================================
            // FOR ADMIN STATUS UPDATE
            // ============================================
            if (HttpContext.Session.GetString("UserRole") == "Admin")
            {
                ViewData["IsAdmin"] = true;
                ViewData["AdminFullname"] = HttpContext.Session.GetString("AdminFullname");
            }
            else
            {
                ViewData["IsAdmin"] = false;
            }

            return View();
        }


        // UPDATED ViewPDF METHOD
        [HttpGet]
        public IActionResult ViewPDF(string fileName, string fileType)
        {
            try
            {
                // Authentication check - allow both user and admin access
                var userId = HttpContext.Session.GetString("UserId");
                var adminId = HttpContext.Session.GetString("AdminFullname");

                if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized("Please log in to view documents");
                }

                Console.WriteLine($"?? ViewPDF called - FileName: {fileName}, FileType: {fileType}");

                // Validate inputs
                if (string.IsNullOrEmpty(fileName))
                {
                    Console.WriteLine("? FileName is null or empty");
                    return BadRequest("FileName is required");
                }

                if (string.IsNullOrEmpty(fileType))
                {
                    Console.WriteLine("? FileType is null or empty");
                    return BadRequest("FileType is required");
                }

                // Security: Prevent directory traversal
                string safeFileName = Path.GetFileName(fileName);

                // Define possible directories to search based on file type
                List<string> possibleFolders = new List<string>();

                // NEW: Search in form-specific folders first
                if (fileType.ToLower() == "doctorprescription" || fileType.ToLower() == "deathcertificate")
                {
                    // These file types could be in Hospital, Funeral, or Other assistance
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage"));
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage"));
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage"));
                }
                else if (fileType.ToLower() == "medicalcertificate")
                {
                    // Medical certificates are only in Other assistance
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage"));
                }
                else
                {
                    // Default for valid ID images
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "Validimg"));
                }

                Console.WriteLine($"?? Searching in {possibleFolders.Count} possible folders");

                // Search for the file in possible folders
                string? encryptedFilePath = null;
                string? folderPath = null;

                foreach (var folder in possibleFolders)
                {
                    string testPath = Path.Combine(folder, safeFileName);
                    if (System.IO.File.Exists(testPath))
                    {
                        encryptedFilePath = testPath;
                        folderPath = folder;
                        Console.WriteLine($"? File found in: {folderPath}");
                        break;
                    }
                }

                // Check if file exists
                if (string.IsNullOrEmpty(encryptedFilePath) || !System.IO.File.Exists(encryptedFilePath))
                {
                    Console.WriteLine($"? File does not exist in any expected folder: {safeFileName}");
                    return NotFound($"File not found: {fileName}");
                }

                // Additional security: Verify the resolved path is within the expected directory
                string resolvedPath = Path.GetFullPath(encryptedFilePath);
                string resolvedFolder = Path.GetFullPath(folderPath);
                if (!resolvedPath.StartsWith(resolvedFolder))
                {
                    Console.WriteLine("? Security: Path traversal attempt detected");
                    return BadRequest("Invalid file path");
                }

                Console.WriteLine($"? File exists. Size: {new FileInfo(encryptedFilePath).Length} bytes");

                // Decrypt the file USING CONFIGURATION-BASED KEY
                byte[] decryptedBytes = DecryptFile(encryptedFilePath);
                Console.WriteLine($"? File decrypted. Decrypted size: {decryptedBytes.Length} bytes");

                // Verify it's actually a PDF
                bool isPdf = IsPdfFile(decryptedBytes);
                Console.WriteLine($"?? Is PDF: {isPdf}");

                if (!isPdf)
                {
                    Console.WriteLine("? File is not a valid PDF");
                    return BadRequest("Only PDF files can be viewed");
                }

                // ? CRITICAL: Set headers to FORCE inline viewing and PREVENT download
                Response.Headers["Content-Disposition"] = "inline";
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

                // Additional security headers to prevent download
                Response.Headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'self'";
                Response.Headers["X-Content-Type-Options"] = "nosniff";

                // Cache control to prevent caching of sensitive documents
                Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                Console.WriteLine($"?? Returning PDF for INLINE VIEWING ONLY (download disabled)");

                // Return as PDF without any filename parameter
                return File(decryptedBytes, "application/pdf");
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"?? DECRYPTION ERROR: {ex.Message}");
                return BadRequest("Failed to decrypt file. Invalid encryption or corrupted file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? ERROR in ViewPDF: {ex.Message}");
                Console.WriteLine($"?? Stack Trace: {ex.StackTrace}");
                return StatusCode(500, $"Error viewing PDF: {ex.Message}");
            }
        }



        // UPDATED CheckFileType METHOD
        [HttpGet]
        public IActionResult CheckFileType(string fileName, string fileType = "validid")
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return Unauthorized();
            }

            // Define possible directories to search based on file type
            List<string> possibleFolders = new List<string>();

            // NEW: Search in form-specific folders first
            if (fileType.ToLower() == "doctorprescription" || fileType.ToLower() == "deathcertificate")
            {
                // These file types could be in Hospital, Funeral, or Other assistance
                possibleFolders.Add(Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage"));
                possibleFolders.Add(Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage"));
                possibleFolders.Add(Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage"));
            }
            else if (fileType.ToLower() == "medicalcertificate")
            {
                // Medical certificates are only in Other assistance
                possibleFolders.Add(Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage"));
            }
            else
            {
                // Default for valid ID images
                possibleFolders.Add(Path.Combine(environment.WebRootPath, "Validimg"));
            }

            string safeFileName = Path.GetFileName(fileName);
            string? filePath = null;

            // Search for the file in possible folders
            foreach (var folder in possibleFolders)
            {
                string testPath = Path.Combine(folder, safeFileName);
                if (System.IO.File.Exists(testPath))
                {
                    filePath = testPath;
                    break;
                }
            }

            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return Json(new { exists = false, isPdf = false });
            }

            try
            {
                byte[] decryptedData = DecryptFile(filePath);
                bool isPdf = IsPdfFile(decryptedData);

                return Json(new
                {
                    exists = true,
                    isPdf = isPdf,
                    fileSize = decryptedData.Length
                });
            }
            catch (Exception ex)
            {
                return Json(new { exists = true, isPdf = false, error = ex.Message });
            }
        }

        // Notification Preferences Management
        [HttpGet]
        public JsonResult GetNotificationPreferences()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            var user = context.UserAccount.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return Json(new { success = false, error = "User not found" });
            }

            return Json(new
            {
                success = true,
                preferences = new
                {
                    preferEmail = user.PreferEmailNotification,
                    preferSms = user.PreferSmsNotification,
                    preferInApp = user.PreferInAppNotification
                }
            });
        }

        [HttpPost]
        public JsonResult UpdateNotificationPreferences(bool preferEmail, bool preferSms, bool preferInApp)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, error = "User not authenticated" });
            }

            var user = context.UserAccount.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return Json(new { success = false, error = "User not found" });
            }

            user.PreferEmailNotification = preferEmail;
            user.PreferSmsNotification = preferSms;
            user.PreferInAppNotification = preferInApp;

            context.SaveChanges();

            return Json(new { success = true, message = "Notification preferences updated successfully" });
        }

        // API endpoint to get user's applications for real-time status checking
        [HttpGet]
        public IActionResult GetUserApplications()
        {
            try
            {
                var userIdString = HttpContext.Session.GetString("UserId");
                bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;

                if (string.IsNullOrEmpty(userIdString) && !isAuthenticated)
                {
                    return Json(new { success = false, error = "User not authenticated" });
                }

                int userId = 0;
                if (!string.IsNullOrEmpty(userIdString))
                {
                    int.TryParse(userIdString, out userId);
                }
                else if (isAuthenticated)
                {
                    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(userIdClaim))
                    {
                        int.TryParse(userIdClaim, out userId);
                    }
                }

                if (userId == 0)
                {
                    return Json(new { success = false, error = "Invalid user ID" });
                }

                // Get user's applications
                var hospitalBills = context.HospitalAssistance
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(h => new
                    {
                        h.Id,
                        h.Status,
                        h.Status2,
                        h.Status3,
                        h.CreatedAt,
                        h.ProcessAt,
                        Type = "Hospital"
                    })
                    .ToList();

                var medicalForms = context.OtherAssistance
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(m => new
                    {
                        m.Id,
                        m.Status,
                        m.Status2,
                        m.Status3,
                        m.CreatedAt,
                        m.ProcessAt,
                        Type = "Other"
                    })
                    .ToList();

                var funeralForms = context.FuneralAssistance
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new
                    {
                        f.Id,
                        f.Status,
                        f.Status2,
                        f.Status3,
                        f.CreatedAt,
                        f.ProcessAt,
                        Type = "Funeral"
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    hospitalBills = hospitalBills,
                    medicalForms = medicalForms,
                    funeralForms = funeralForms
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // FEEDBACK ACTIONS
        // ═══════════════════════════════════════════════════════════════

        [HttpGet]
        public IActionResult Feedback(string? assistanceType = null, int? assistanceId = null, int? userId = null)
        {
            ViewBag.AssistanceType = assistanceType;
            ViewBag.AssistanceId = assistanceId;
            ViewBag.UserId = userId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(Feedback feedback)
        {
            try
            {
                // Set IP address
                feedback.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                feedback.SubmittedAt = _dateTimeService.Now;

                context.Feedbacks.Add(feedback);
                await context.SaveChangesAsync();

                // Send admin notification for feedback
                try
                {
                    string userName = "Anonymous";
                    if (feedback.UserId.HasValue)
                    {
                        var verifiedAccount = await context.VerifiedAccount.FirstOrDefaultAsync(v => v.UserId == feedback.UserId.Value);
                        if (verifiedAccount != null)
                        {
                            // Construct full name from VerifiedAccount
                            var nameParts = new List<string>
                            {
                                verifiedAccount.Firstname,
                                verifiedAccount.Middlename,
                                verifiedAccount.Lastname
                            };
                            if (!string.IsNullOrWhiteSpace(verifiedAccount.Suffix) && verifiedAccount.Suffix != "None")
                            {
                                nameParts.Add(verifiedAccount.Suffix);
                            }
                            userName = string.Join(" ", nameParts.Where(p => !string.IsNullOrWhiteSpace(p)));
                        }
                    }

                    await _adminNotificationService.SendAdminNotificationAsync(
                        "feedback_submitted",
                        "Feedback",
                        feedback.Id,
                        feedback.UserId,
                        userName,
                        "New Feedback Submitted",
                        $"{userName} submitted feedback for {feedback.AssistanceType}.",
                        "/Feedbacksreport"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send admin notification for feedback: {ex.Message}");
                }

                return Json(new { success = true, message = "Thank you for your feedback!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "An error occurred while submitting your feedback. Please try again." });
            }
        }

        // ==============================================
        // ?? DECRYPTION API FOR ADDITIONAL INFORMATION
        // ==============================================
        [HttpPost]
        public IActionResult DecryptField(string fieldValue, string formType, int formId)
        {
            try
            {
                // Get current user info
                var userIdString = HttpContext.Session.GetString("UserId");
                var isAdmin = HttpContext.Session.GetString("AdminFullname");
                var isSuperadmin = HttpContext.Session.GetString("IsSuperadmin");

                // Authorization check
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Check if user has permission to decrypt
                bool canDecrypt = false;

                if (isAdmin == "true" || isSuperadmin == "true")
                {
                    // Admin and Superadmin can decrypt all records
                    canDecrypt = true;
                }
                else
                {
                    // Regular users can only decrypt their own records
                    int? recordUserId = null;

                    switch (formType)
                    {
                        case "Hospital":
                            recordUserId = context.HospitalAssistance
                                .Where(h => h.Id == formId)
                                .Select(h => h.UserId)
                                .FirstOrDefault();
                            break;
                        case "Funeral":
                            recordUserId = context.FuneralAssistance
                                .Where(f => f.Id == formId)
                                .Select(f => f.UserId)
                                .FirstOrDefault();
                            break;
                        case "Other":
                            recordUserId = context.OtherAssistance
                                .Where(o => o.Id == formId)
                                .Select(o => o.UserId)
                                .FirstOrDefault();
                            break;
                    }

                    canDecrypt = (recordUserId == userId);
                }

                if (!canDecrypt)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Check if fieldValue is empty or null
                if (string.IsNullOrEmpty(fieldValue))
                {
                    return Json(new { success = true, data = "" });
                }

                // Check if fieldValue is only whitespace
                if (string.IsNullOrWhiteSpace(fieldValue))
                {
                    return Json(new { success = true, data = "" });
                }

                // Try to decrypt the field
                string decryptedValue;

                Console.WriteLine($"=== Processing Field for Form {formType} ID {formId} ===");
                Console.WriteLine($"Field value (first 100 chars): {fieldValue.Substring(0, Math.Min(100, fieldValue.Length))}");
                Console.WriteLine($"Field value length: {fieldValue.Length}");
                Console.WriteLine($"Is Base64: {IsBase64String(fieldValue)}");

                try
                {
                    // Check if the value looks like Base64 encrypted data
                    if (IsBase64String(fieldValue))
                    {
                        Console.WriteLine($"Attempting to decrypt Base64 field...");

                        // Attempt decryption
                        decryptedValue = _aesEncryptionService.Decrypt(fieldValue);

                        Console.WriteLine($"✓ Successfully decrypted! Decrypted length: {decryptedValue.Length}");
                        Console.WriteLine($"Decrypted value (first 50 chars): {decryptedValue.Substring(0, Math.Min(50, decryptedValue.Length))}");
                    }
                    else
                    {
                        // Not Base64, likely plain text - return as is
                        decryptedValue = fieldValue;
                        Console.WriteLine($"✓ Not Base64 - treating as plain text");
                    }
                }
                catch (Exception decryptEx)
                {
                    // Decryption failed
                    Console.WriteLine($"✗ DECRYPTION FAILED!");
                    Console.WriteLine($"Error Type: {decryptEx.GetType().Name}");
                    Console.WriteLine($"Error Message: {decryptEx.Message}");
                    Console.WriteLine($"Stack Trace: {decryptEx.StackTrace}");

                    // Return error message instead of encrypted data
                    decryptedValue = $"[Decryption Error: {decryptEx.Message}]";
                }

                return Json(new { success = true, data = decryptedValue });
            }
            catch (Exception ex)
            {
                // Log detailed error information for debugging
                Console.WriteLine($"=== DecryptField Error ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"Field value length: {fieldValue?.Length ?? 0}");
                Console.WriteLine($"Form type: {formType}, Form ID: {formId}");
                Console.WriteLine($"==========================");

                // Return error to frontend for display
                return Json(new { success = false, message = $"Decryption failed: {ex.Message}" });
            }
        }

        // Helper method to check if a string is valid Base64
        private bool IsBase64String(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Base64 string length should be divisible by 4
            if (value.Length % 4 != 0)
                return false;

            // Check if string contains only valid Base64 characters
            try
            {
                Convert.FromBase64String(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ========================================================
        // ?? DECRYPT ENCRYPTED IMAGES (ID CARDS)
        // ========================================================
        [HttpPost]
        public IActionResult DecryptImage(string imagePath)
        {
            try
            {
                // Get current user info
                var userIdString = HttpContext.Session.GetString("UserId");
                var adminFullname = HttpContext.Session.GetString("AdminFullname");
                var isAdmin = HttpContext.Session.GetString("IsAdmin");
                var isSuperadmin = HttpContext.Session.GetString("IsSuperadmin");

                // Authorization check - support both regular users and admins
                bool isUserAuthenticated = !string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId);
                bool isAdminAuthenticated = !string.IsNullOrEmpty(adminFullname) && 
                                           (isAdmin == "true" || isSuperadmin == "true");
                
                if (!isUserAuthenticated && !isAdminAuthenticated)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                Console.WriteLine($"=== DecryptImage Debug ===");
                Console.WriteLine($"Image path: {imagePath}");
                Console.WriteLine($"User authenticated: {isUserAuthenticated}, Admin authenticated: {isAdminAuthenticated}");

                // Construct full file path
                var webRootPath = environment.WebRootPath;
                var fullPath = Path.Combine(webRootPath, imagePath.TrimStart('/').Replace("/", "\\"));

                Console.WriteLine($"Full path: {fullPath}");

                // Check if file exists
                if (!System.IO.File.Exists(fullPath))
                {
                    Console.WriteLine($"File not found: {fullPath}");
                    return Json(new { success = false, message = "Image file not found" });
                }

                // Read encrypted image bytes
                byte[] encryptedBytes = System.IO.File.ReadAllBytes(fullPath);
                Console.WriteLine($"Read {encryptedBytes.Length} encrypted bytes");

                // Initialize AES encryption helper
                var encryptionHelper = new AesEncryptionHelper(_configuration);

                // Decrypt the image
                byte[] decryptedBytes = encryptionHelper.DecryptBytes(encryptedBytes);
                Console.WriteLine($"Decrypted to {decryptedBytes.Length} bytes");

                // Convert to Base64 for frontend display
                string base64Image = Convert.ToBase64String(decryptedBytes);

                // Determine image type from file extension
                string extension = Path.GetExtension(fullPath).ToLower();
                string mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "image/jpeg"
                };

                return Json(new
                {
                    success = true,
                    imageData = $"data:{mimeType};base64,{base64Image}",
                    mimeType = mimeType
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== DecryptImage Error ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"==========================");

                return Json(new { success = false, message = $"Decryption failed: {ex.Message}" });
            }
        }

        // ========================================================
        // ?? ENCRYPT FIELD (FOR RE-ENCRYPTION)
        // ========================================================
        [HttpPost]
        public IActionResult EncryptField(string fieldValue, string formType, int formId)
        {
            try
            {
                // Get current user info
                var userIdString = HttpContext.Session.GetString("UserId");
                var isAdmin = HttpContext.Session.GetString("AdminFullname");
                var isSuperadmin = HttpContext.Session.GetString("IsSuperadmin");

                // Authorization check
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                Console.WriteLine($"=== EncryptField Debug ===");
                Console.WriteLine($"Form type: {formType}");
                Console.WriteLine($"Form ID: {formId}");
                Console.WriteLine($"User ID: {userId}");
                Console.WriteLine($"Field value length: {fieldValue?.Length ?? 0}");

                // Check if user has permission to encrypt
                bool canEncrypt = false;

                if (isAdmin == "true" || isSuperadmin == "true")
                {
                    // Admin and Superadmin can encrypt all records
                    canEncrypt = true;
                }
                else
                {
                    // Regular users can only encrypt their own records
                    int? recordUserId = null;

                    switch (formType)
                    {
                        case "Hospital":
                            var hospitalRecord = context.HospitalAssistance.Find(formId);
                            recordUserId = hospitalRecord?.UserId;
                            break;
                        case "Funeral":
                            var funeralRecord = context.FuneralAssistance.Find(formId);
                            recordUserId = funeralRecord?.UserId;
                            break;
                        case "Other":
                            var otherRecord = context.OtherAssistance.Find(formId);
                            recordUserId = otherRecord?.UserId;
                            break;
                    }

                    canEncrypt = recordUserId.HasValue && recordUserId.Value == userId;
                }

                if (!canEncrypt)
                {
                    return Json(new { success = false, message = "You don't have permission to encrypt this record" });
                }

                // Encrypt the field value
                string encryptedValue = _aesEncryptionService.Encrypt(fieldValue);
                Console.WriteLine($"✓ Field encrypted successfully");

                return Json(new { success = true, data = encryptedValue });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EncryptField Error ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"==========================");

                return Json(new { success = false, message = $"Encryption failed: {ex.Message}" });
            }
        }

        // ========================================================
        // ?? PASSWORD VERIFICATION FOR VIEWING ENCRYPTED DATA
        // ========================================================
        [HttpPost]
        public IActionResult VerifyPasswordForDecryption(string password)
        {
            try
            {
                // Get current user info - CHECK BOTH REGULAR USER AND ADMIN
                var userIdString = HttpContext.Session.GetString("UserId");
                var adminFullname = HttpContext.Session.GetString("AdminFullname");
                var adminUserId = HttpContext.Session.GetString("AdminUserId");
                var sessionPassword = HttpContext.Session.GetString("UserPassword") ??
                                     HttpContext.Session.GetString("AdminPassword");
                var isGoogleUser = HttpContext.Session.GetString("IsGoogleUser");
                var isAdmin = HttpContext.Session.GetString("IsAdmin");

                // DEBUG LOGGING
                Console.WriteLine("=== PASSWORD VERIFICATION DEBUG ===");
                Console.WriteLine($"UserId: '{userIdString}'");
                Console.WriteLine($"AdminFullname: '{adminFullname}'");
                Console.WriteLine($"AdminUserId: '{adminUserId}'");
                Console.WriteLine($"SessionPassword exists: {!string.IsNullOrEmpty(sessionPassword)}");
                Console.WriteLine($"IsGoogleUser: '{isGoogleUser}'");
                Console.WriteLine($"IsAdmin: '{isAdmin}'");
                Console.WriteLine($"All session keys: {string.Join(", ", HttpContext.Session.Keys)}");

                // CHECK 1: If we have either UserId OR AdminFullname, we consider the user authenticated
                bool isAuthenticated =
                    (!string.IsNullOrEmpty(userIdString) && int.TryParse(userIdString, out int userId)) ||
                    !string.IsNullOrEmpty(adminFullname) ||
                    !string.IsNullOrEmpty(adminUserId);

                if (!isAuthenticated)
                {
                    Console.WriteLine("ERROR: No authentication found - no UserId, AdminFullname, or AdminUserId");
                    return Json(new
                    {
                        success = false,
                        message = "User not authenticated. Please log in again.",
                        debugInfo = new
                        {
                            hasUserId = !string.IsNullOrEmpty(userIdString),
                            hasAdminFullname = !string.IsNullOrEmpty(adminFullname),
                            hasAdminUserId = !string.IsNullOrEmpty(adminUserId)
                        }
                    });
                }

                if (string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Password is required" });
                }

                // Google users don't have passwords, so they're automatically verified
                if (isGoogleUser == "true")
                {
                    return Json(new { success = true, message = "Verified (Google user)" });
                }

                if (string.IsNullOrEmpty(sessionPassword))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Session expired. Please log in again.",
                        debugInfo = new
                        {
                            sessionKeys = HttpContext.Session.Keys.ToList(),
                            sessionId = HttpContext.Session.Id
                        }
                    });
                }

                // Verify the typed password matches the session password
                if (password == sessionPassword)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Password verified successfully",
                        userType = !string.IsNullOrEmpty(adminFullname) ? "Admin" : "User"
                    });
                }
                else
                {
                    Console.WriteLine($"Password mismatch. Input: '{password}', Session: '{sessionPassword}'");
                    return Json(new { success = false, message = "Invalid password" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Password verification error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Verification failed: " + ex.Message });
            }
        }

        // ADD THIS HELPER METHOD INSIDE YOUR CONTROLLER CLASS
        private string DecryptFieldText(string encryptedText)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedText))
                    return "";

                // If it's not Base64, it's probably already decrypted
                if (!IsBase64String(encryptedText))
                    return encryptedText;

                // Convert from Base64
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                // Use the same decryption logic as your DecryptFile method
                using var memoryStream = new MemoryStream(encryptedBytes);

                // Read IV (first 16 bytes)
                byte[] iv = new byte[16];
                memoryStream.Read(iv, 0, iv.Length);

                // Get AES key from your configuration helper
                var aesHelper = new AesEncryptionHelper(_configuration);
                var keyField = typeof(AesEncryptionHelper).GetField("_aesKey",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (keyField == null)
                    return "[Key Error]";

                byte[]? key = keyField.GetValue(aesHelper) as byte[];
                if (key == null)
                    return "[Key Error]";

                // Decrypt
                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptedStream = new MemoryStream();
                using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(decryptedStream);
                }

                // Convert to text
                return Encoding.UTF8.GetString(decryptedStream.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DecryptFieldText error: {ex.Message}");
                // Return original text if decryption fails
                return encryptedText ?? "[Error]";
            }
        }
    }
}

