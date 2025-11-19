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

        public Dashboard(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration, FormSubmissionSecurityService securityService, ISessionConfigurationService sessionConfig, IDateTimeService dateTimeService)
        {
            this.context = context;
            this.environment = environment;
            _configuration = configuration;
            _securityService = securityService;
            _sessionConfig = sessionConfig;
            _dateTimeService = dateTimeService;
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
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == userId);
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
                    var user = context.RegisterAcc.FirstOrDefault(u => u.Id == userId);
                    ViewBag.Profilepicture = user?.Profilepicture ?? "";
                }
                else
                {
                    ViewBag.Profilepicture = "";
                }
            }

            // ? Check if user has completed verification
            var verification = context.Verifyaccount.FirstOrDefault(v => v.UserId == userId);
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
                 var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == userId);

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
                     ViewBag.District = verifyAccount.District ?? "";
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
                     ViewBag.District = HttpContext.Session.GetString("District");
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
                 var user = context.RegisterAcc.FirstOrDefault(u => u.Id == userIdForPicture);
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
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == userId);
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
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == userId);
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
            ViewBag.District = HttpContext.Session.GetString("District");
            ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
            ViewBag.Gender = HttpContext.Session.GetString("Gender");
            ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");

            ViewBag.FrontID = HttpContext.Session.GetString("FrontID");
            ViewBag.BackID = HttpContext.Session.GetString("BackID");

            // Get phone number from Verifyaccount table
            var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == userId);
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

            byte[] key = (byte[])keyField.GetValue(aesHelper);

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
        public IActionResult HospitalAssistance(HospitalAssistanceDto HospitalAssistanceDto)
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

            // MODIFIED: Image validation - Only check for prescription and death certificate images
            // Remove ID image validation since we'll use the existing ones from user account
            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");

            if (HospitalAssistanceDto.DoctorPrescriptionimage == null && HospitalAssistanceDto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one image file (Doctor Prescription or Death Certificate) is required");
            }

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

                // Generate unique encrypted timestamp for filenames
                string timestamp = _dateTimeService.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeEncryptedTimestamp = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // Generate encrypted filenames
                string? newFileNamePrescription = null;
                string? newFileNameDeathCertificate = null;

                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

                // Ensure directories exist
                Directory.CreateDirectory(uploadsFolder1);
                Directory.CreateDirectory(uploadsFolder2);

                // Encrypt and Save Prescription Image if provided
                if (HospitalAssistanceDto.DoctorPrescriptionimage != null)
                {
                    newFileNamePrescription = safeEncryptedTimestamp + "_prescription.enc";
                    string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                    using (var fileStream = new FileStream(filePathPrescription, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(HospitalAssistanceDto.DoctorPrescriptionimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Death Certificate Image if provided
                if (HospitalAssistanceDto.DeathCertificateimage != null)
                {
                    newFileNameDeathCertificate = safeEncryptedTimestamp + "_deathcert.enc";
                    string filePathDeathCertificate = Path.Combine(uploadsFolder2, newFileNameDeathCertificate);
                    using (var fileStream = new FileStream(filePathDeathCertificate, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(HospitalAssistanceDto.DeathCertificateimage.OpenReadStream());
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
                    District = HospitalAssistanceDto.District,
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
                    RDistrict = HospitalAssistanceDto.RDistrict,
                    RelationshipPatient = HospitalAssistanceDto.RelationshipPatient,
                    ContactNo = HospitalAssistanceDto.ContactNo,

                    // Assistance Type
                    Typeassistance = HospitalAssistanceDto.Typeassistance,
                    ForCMOPERSONNEL = HospitalAssistanceDto.ForCMOPERSONNEL,

                    // MODIFIED: Use existing ID images from user account instead of new uploads
                    Validfrontimage = userFrontID,
                    ValidBackimage = userBackID,
                    DoctorPrescription = newFileNamePrescription ?? string.Empty,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = _dateTimeService.Now
                };

                context.HospitalAssistance.Add(HospitalAssistance);
                context.SaveChanges();

                // ? SUCCESS: Set the success flag to trigger the modal
                ViewBag.Success = true;

                // Also set the session data again to repopulate the form
                ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
                ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
                ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
                ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
                ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
                ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
                ViewBag.District = HttpContext.Session.GetString("District");
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
            ViewData["District"] = HospitalAssistance.District;
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
            ViewData["RDistrict"] = HospitalAssistance.RDistrict;
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

            return View();

        }

        [HttpPost]
        public IActionResult HospitalAssistanceEdit(int id, HospitalAssistanceDto dto)
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

            if (existing.Status != "Pending")
            {
                TempData["ErrorMessage"] = "You can only edit forms that are in 'Pending' status.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ? 4. Remove validations for optional fields
            if (string.IsNullOrEmpty(dto.PhilHealthNo))
                ModelState.Remove("PhilHealthNo");

            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");

            // ? Require at least one doc ONLY if both existing docs are empty and no new upload
            if (string.IsNullOrEmpty(existing.DoctorPrescription) &&
                string.IsNullOrEmpty(existing.DeathCertificate) &&
                dto.DoctorPrescriptionimage == null &&
                dto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one document is required.");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate view data for the form
                ViewData["Id"] = existing.Id;
                ViewData["Lastname"] = existing.Lastname;
                ViewData["Firstname"] = existing.Firstname;
                ViewData["Middlename"] = existing.Middlename;
                ViewData["Suffix"] = existing.Suffix;
                ViewData["BlkLotStreet"] = existing.BlkLotStreet;
                ViewData["SubVill"] = existing.SubVill;
                ViewData["Brgy"] = existing.Brgy;
                ViewData["District"] = existing.District;
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
                ViewData["RDistrict"] = existing.RDistrict;
                ViewData["RelationshipPatient"] = existing.RelationshipPatient;
                ViewData["ContactNo"] = existing.ContactNo;

                ViewData["Typeassistance"] = existing.Typeassistance;
                ViewData["ForCMOPERSONNEL"] = existing.ForCMOPERSONNEL;

                ViewData["CurrentDoctorPrescription"] = existing.DoctorPrescription;
                ViewData["CurrentDeathCertificate"] = existing.DeathCertificate;
                ViewData["CurrentValidFront"] = existing.Validfrontimage;
                ViewData["CurrentValidBack"] = existing.ValidBackimage;

                return View(dto);
            }

            try
            {
                var aesHelper = new AesEncryptionHelper(_configuration);
                string timestamp = _dateTimeService.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeName = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // ? 5. Update Doctor Prescription (optional)
                if (dto.DoctorPrescriptionimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.DoctorPrescription))
                    {
                        string oldPath = Path.Combine(folder, existing.DoctorPrescription);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_prescription.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(dto.DoctorPrescriptionimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.DoctorPrescription = fileName;
                }

                // ? 6. Update Death Certificate (optional)
                if (dto.DeathCertificateimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.DeathCertificate))
                    {
                        string oldPath = Path.Combine(folder, existing.DeathCertificate);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_deathcert.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(dto.DeathCertificateimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.DeathCertificate = fileName;
                }

                // ? 7. Update text fields safely
                existing.Lastname = dto.Lastname ?? existing.Lastname;
                existing.Firstname = dto.Firstname ?? existing.Firstname;
                existing.Middlename = dto.Middlename ?? existing.Middlename;
                existing.Suffix = dto.Suffix ?? existing.Suffix;
                existing.BlkLotStreet = dto.BlkLotStreet ?? existing.BlkLotStreet;
                existing.SubVill = dto.SubVill ?? existing.SubVill;
                existing.Brgy = dto.Brgy ?? existing.Brgy;
                existing.District = dto.District ?? existing.District;
                existing.Sex = dto.Sex ?? existing.Sex;
                existing.PhilHealth = dto.PhilHealth ?? existing.PhilHealth;
                existing.PhilHealthNo = dto.PhilHealthNo ?? existing.PhilHealthNo;
                existing.Dateofbirth = dto.Dateofbirth ?? existing.Dateofbirth;
                existing.Age = dto.Age ?? existing.Age;

                // ? Requestor details
                existing.RLastname = dto.RLastname ?? existing.RLastname;
                existing.RFirstname = dto.RFirstname ?? existing.RFirstname;
                existing.RMiddlename = dto.RMiddlename ?? existing.RMiddlename;
                existing.RSuffix = dto.RSuffix ?? existing.RSuffix;
                existing.RBlkLotStreet = dto.RBlkLotStreet ?? existing.RBlkLotStreet;
                existing.RSubVill = dto.RSubVill ?? existing.RSubVill;
                existing.RBrgy = dto.RBrgy ?? existing.RBrgy;
                existing.RDistrict = dto.RDistrict ?? existing.RDistrict;
                existing.RelationshipPatient = dto.RelationshipPatient ?? existing.RelationshipPatient;
                existing.ContactNo = dto.ContactNo ?? existing.ContactNo;

                // ? Assistance info
                existing.Typeassistance = dto.Typeassistance ?? existing.Typeassistance;
                existing.ForCMOPERSONNEL = dto.ForCMOPERSONNEL ?? existing.ForCMOPERSONNEL;

                // ? 8. Update ID images (if session updated)
                string frontID = HttpContext.Session.GetString("FrontID") ?? "";
                string backID = HttpContext.Session.GetString("BackID") ?? "";
                if (!string.IsNullOrEmpty(frontID)) existing.Validfrontimage = frontID;
                if (!string.IsNullOrEmpty(backID)) existing.ValidBackimage = backID;

                // ? 9. Update timestamp properly
                existing.CreatedAt = _dateTimeService.Now;

                // ? 10. Save changes
                context.Entry(existing).State = EntityState.Modified;
                context.SaveChanges();

                // ? 11. Set success flag and RAF number for modal - Convert int to string for varchar field
                ViewBag.Success = true;
                TempData["SuccessRAF"] = id.ToString(); // This converts int to string

                // ? 12. Repopulate view data for success display
                ViewData["Id"] = existing.Id;
                ViewData["Lastname"] = existing.Lastname;
                ViewData["Firstname"] = existing.Firstname;
                ViewData["Middlename"] = existing.Middlename;
                ViewData["Suffix"] = existing.Suffix;
                ViewData["BlkLotStreet"] = existing.BlkLotStreet;
                ViewData["SubVill"] = existing.SubVill;
                ViewData["Brgy"] = existing.Brgy;
                ViewData["District"] = existing.District;
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
                ViewData["RDistrict"] = existing.RDistrict;
                ViewData["RelationshipPatient"] = existing.RelationshipPatient;
                ViewData["ContactNo"] = existing.ContactNo;

                ViewData["Typeassistance"] = existing.Typeassistance;
                ViewData["ForCMOPERSONNEL"] = existing.ForCMOPERSONNEL;

                ViewData["CurrentDoctorPrescription"] = existing.DoctorPrescription;
                ViewData["CurrentDeathCertificate"] = existing.DeathCertificate;
                ViewData["CurrentValidFront"] = existing.Validfrontimage;
                ViewData["CurrentValidBack"] = existing.ValidBackimage;

                return View(dto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the form: " + ex.Message);

                ViewData["Id"] = existing.Id;
                ViewData["Lastname"] = existing.Lastname;
                ViewData["Firstname"] = existing.Firstname;
                ViewData["Middlename"] = existing.Middlename;
                ViewData["Suffix"] = existing.Suffix;
                ViewData["BlkLotStreet"] = existing.BlkLotStreet;
                ViewData["SubVill"] = existing.SubVill;
                ViewData["Brgy"] = existing.Brgy;
                ViewData["District"] = existing.District;
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
                ViewData["RDistrict"] = existing.RDistrict;
                ViewData["RelationshipPatient"] = existing.RelationshipPatient;
                ViewData["ContactNo"] = existing.ContactNo;

                ViewData["Typeassistance"] = existing.Typeassistance;
                ViewData["ForCMOPERSONNEL"] = existing.ForCMOPERSONNEL;

                ViewData["CurrentDoctorPrescription"] = existing.DoctorPrescription;
                ViewData["CurrentDeathCertificate"] = existing.DeathCertificate;
                ViewData["CurrentValidFront"] = existing.Validfrontimage;
                ViewData["CurrentValidBack"] = existing.ValidBackimage;

                return View(dto);
            }
        }

        public IActionResult HospitalAssistancedelete(int id)
        {
            var HospitalAssistance = context.HospitalAssistance.Find(id);
            if (HospitalAssistance == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Instead of deleting files and record, just update the status
            HospitalAssistance.Status = "Removed";
            context.HospitalAssistance.Update(HospitalAssistance);
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
            ViewBag.District = HttpContext.Session.GetString("District");
            ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
            ViewBag.Gender = HttpContext.Session.GetString("Gender");
            ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");

            ViewBag.FrontID = HttpContext.Session.GetString("FrontID");
            ViewBag.BackID = HttpContext.Session.GetString("BackID");

            // Get phone number from Verifyaccount table
            var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == userId);
            ViewBag.Phonenumber = verifyAccount?.Phonenumber ?? "";

            return View();
        }
        //1
        [HttpPost]
        public IActionResult OtherAssistance(OtherAssistanceDto OtherAssistanceDto)
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
            ModelState.Remove("DoctorPrescriptionimage");
            ModelState.Remove("DeathCertificateimage");
            ModelState.Remove("MedCertificateimage");

            // NEW VALIDATION: At least one of the medical documents must be provided
            if (OtherAssistanceDto.DoctorPrescriptionimage == null &&
                OtherAssistanceDto.DeathCertificateimage == null &&
                OtherAssistanceDto.MedCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one medical document (Doctor Prescription, Death Certificate, or Medical Certificate) is required");
            }

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

                // Generate unique encrypted timestamp for filenames
                string timestamp = _dateTimeService.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeEncryptedTimestamp = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // Generate encrypted filenames
                string? newFileNamePrescription = null;
                string? newFileNameDeathCertificate = null;
                string? newFileNameMedCertificate = null;

                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
                string uploadsFolder3 = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");

                // Ensure directories exist
                Directory.CreateDirectory(uploadsFolder1);
                Directory.CreateDirectory(uploadsFolder2);
                Directory.CreateDirectory(uploadsFolder3);

                // Encrypt and Save Prescription Image if provided
                if (OtherAssistanceDto.DoctorPrescriptionimage != null)
                {
                    newFileNamePrescription = safeEncryptedTimestamp + "_prescription.enc";
                    string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                    using (var fileStream = new FileStream(filePathPrescription, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(OtherAssistanceDto.DoctorPrescriptionimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Death Certificate Image if provided
                if (OtherAssistanceDto.DeathCertificateimage != null)
                {
                    newFileNameDeathCertificate = safeEncryptedTimestamp + "_deathcert.enc";
                    string filePathDeathCertificate = Path.Combine(uploadsFolder2, newFileNameDeathCertificate);
                    using (var fileStream = new FileStream(filePathDeathCertificate, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(OtherAssistanceDto.DeathCertificateimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Medical Certificate Image if provided
                if (OtherAssistanceDto.MedCertificateimage != null)
                {
                    newFileNameMedCertificate = safeEncryptedTimestamp + "_medcert.enc";
                    string filePathMedCertificate = Path.Combine(uploadsFolder3, newFileNameMedCertificate);
                    using (var fileStream = new FileStream(filePathMedCertificate, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(OtherAssistanceDto.MedCertificateimage.OpenReadStream());
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
                    District = OtherAssistanceDto.District,
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
                    RDistrict = OtherAssistanceDto.RDistrict,
                    RelationshipPatient = OtherAssistanceDto.RelationshipPatient,
                    ContactNo = OtherAssistanceDto.ContactNo,

                    // Assistance Type
                    Typeassistance = OtherAssistanceDto.Typeassistance,
                    ForCMOPERSONNEL = OtherAssistanceDto.ForCMOPERSONNEL,

                    // MODIFIED: Use existing ID images from user account instead of new uploads
                    Validfrontimage = userFrontID,
                    ValidBackimage = userBackID,
                    DoctorPrescription = newFileNamePrescription ?? string.Empty,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    MedCertificate = newFileNameMedCertificate ?? string.Empty,
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = _dateTimeService.Now
                };

                context.OtherAssistance.Add(OtherAssistance);
                context.SaveChanges();

                // ? SUCCESS: Set the success flag to trigger the modal
                ViewBag.Success = true;

                // Also set the session data again to repopulate the form
                ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
                ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
                ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
                ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
                ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
                ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
                ViewBag.District = HttpContext.Session.GetString("District");
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
            ViewData["District"] = medicallabform.District;
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
            ViewData["RDistrict"] = medicallabform.RDistrict;
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
                        debugMessages.Add("? Front ID decrypted");
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
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

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

                // ? MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"?? Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"?? Full path: {medicalPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(medicalPath)}");

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

                            debugMessages.Add($"? Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"?? IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Medical Certificate in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

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
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            return View();

        }

        [HttpPost]
        public IActionResult OtherAssistanceedit(int id, OtherAssistanceDto OtherAssistanceDto)
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

            if (existing.Status != "Pending")
            {
                TempData["ErrorMessage"] = "You can only edit forms that are in 'Pending' status.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ? 4. Remove validations for optional fields
            if (string.IsNullOrEmpty(OtherAssistanceDto.PhilHealthNo))
                ModelState.Remove("PhilHealthNo");

            // Remove validation requirements for images if they're not provided
            if (OtherAssistanceDto.IdFrontimage == null) ModelState.Remove("IdFrontimage");
            if (OtherAssistanceDto.IdBackimage == null) ModelState.Remove("IdBackimage");
            if (OtherAssistanceDto.DoctorPrescriptionimage == null) ModelState.Remove("DoctorPrescriptionimage");
            if (OtherAssistanceDto.DeathCertificateimage == null) ModelState.Remove("DeathCertificateimage");
            if (OtherAssistanceDto.MedCertificateimage == null) ModelState.Remove("MedCertificateimage");

            // ? Require at least one medical document ONLY if all existing docs are empty and no new upload
            if (string.IsNullOrEmpty(existing.DoctorPrescription) &&
                string.IsNullOrEmpty(existing.DeathCertificate) &&
                string.IsNullOrEmpty(existing.MedCertificate) &&
                OtherAssistanceDto.DoctorPrescriptionimage == null &&
                OtherAssistanceDto.DeathCertificateimage == null &&
                OtherAssistanceDto.MedCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one medical document is required.");
            }

            if (!ModelState.IsValid)
            {
                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = existing.Validfrontimage;
                ViewData["ValidBackimage"] = existing.ValidBackimage;
                ViewData["DoctorPrescription"] = existing.DoctorPrescription;
                ViewData["DeathCertificate"] = existing.DeathCertificate;
                ViewData["MedCertificate"] = existing.MedCertificate;

                return View(OtherAssistanceDto);
            }

            try
            {
                var aesHelper = new AesEncryptionHelper(_configuration);
                string timestamp = _dateTimeService.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeName = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // ? 5. Update Doctor Prescription (optional)
                if (OtherAssistanceDto.DoctorPrescriptionimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.DoctorPrescription))
                    {
                        string oldPath = Path.Combine(folder, existing.DoctorPrescription);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_prescription.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(OtherAssistanceDto.DoctorPrescriptionimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.DoctorPrescription = fileName;
                }

                // ? 6. Update Death Certificate (optional)
                if (OtherAssistanceDto.DeathCertificateimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.DeathCertificate))
                    {
                        string oldPath = Path.Combine(folder, existing.DeathCertificate);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_deathcert.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(OtherAssistanceDto.DeathCertificateimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.DeathCertificate = fileName;
                }

                // ? 7. Update Medical Certificate (optional)
                if (OtherAssistanceDto.MedCertificateimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.MedCertificate))
                    {
                        string oldPath = Path.Combine(folder, existing.MedCertificate);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_medcert.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(OtherAssistanceDto.MedCertificateimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.MedCertificate = fileName;
                }

                // ? 8. Update ID images (if new ones provided)
                if (OtherAssistanceDto.IdFrontimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "Validimg");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.Validfrontimage))
                    {
                        string oldPath = Path.Combine(folder, existing.Validfrontimage);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_front.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(OtherAssistanceDto.IdFrontimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.Validfrontimage = fileName;
                }

                if (OtherAssistanceDto.IdBackimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "Validimg");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.ValidBackimage))
                    {
                        string oldPath = Path.Combine(folder, existing.ValidBackimage);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_back.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(OtherAssistanceDto.IdBackimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.ValidBackimage = fileName;
                }

                // ? 9. Update text fields safely
                existing.Lastname = OtherAssistanceDto.Lastname ?? existing.Lastname;
                existing.Firstname = OtherAssistanceDto.Firstname ?? existing.Firstname;
                existing.Middlename = OtherAssistanceDto.Middlename ?? existing.Middlename;
                existing.Suffix = OtherAssistanceDto.Suffix ?? existing.Suffix;
                existing.BlkLotStreet = OtherAssistanceDto.BlkLotStreet ?? existing.BlkLotStreet;
                existing.SubVill = OtherAssistanceDto.SubVill ?? existing.SubVill;
                existing.Brgy = OtherAssistanceDto.Brgy ?? existing.Brgy;
                existing.District = OtherAssistanceDto.District ?? existing.District;
                existing.Sex = OtherAssistanceDto.Sex ?? existing.Sex;
                existing.PhilHealth = OtherAssistanceDto.PhilHealth ?? existing.PhilHealth;
                existing.PhilHealthNo = OtherAssistanceDto.PhilHealthNo ?? existing.PhilHealthNo;
                existing.Dateofbirth = OtherAssistanceDto.Dateofbirth ?? existing.Dateofbirth;
                existing.Age = OtherAssistanceDto.Age ?? existing.Age;

                // ? Requestor details
                existing.RLastname = OtherAssistanceDto.RLastname ?? existing.RLastname;
                existing.RFirstname = OtherAssistanceDto.RFirstname ?? existing.RFirstname;
                existing.RMiddlename = OtherAssistanceDto.RMiddlename ?? existing.RMiddlename;
                existing.RSuffix = OtherAssistanceDto.RSuffix ?? existing.RSuffix;
                existing.RBlkLotStreet = OtherAssistanceDto.RBlkLotStreet ?? existing.RBlkLotStreet;
                existing.RSubVill = OtherAssistanceDto.RSubVill ?? existing.RSubVill;
                existing.RBrgy = OtherAssistanceDto.RBrgy ?? existing.RBrgy;
                existing.RDistrict = OtherAssistanceDto.RDistrict ?? existing.RDistrict;
                existing.RelationshipPatient = OtherAssistanceDto.RelationshipPatient ?? existing.RelationshipPatient;
                existing.ContactNo = OtherAssistanceDto.ContactNo ?? existing.ContactNo;

                // ? Assistance info
                existing.Typeassistance = OtherAssistanceDto.Typeassistance ?? existing.Typeassistance;
                existing.ForCMOPERSONNEL = OtherAssistanceDto.ForCMOPERSONNEL ?? existing.ForCMOPERSONNEL;

                // ? 10. Update timestamp properly
                existing.CreatedAt = _dateTimeService.Now;

                // ? 11. Save changes
                context.Entry(existing).State = EntityState.Modified;
                context.SaveChanges();

                // ? 12. Set success flag and RAF number for modal
                ViewBag.Success = true;
                TempData["SuccessRAF"] = id.ToString();

                // ? 13. Repopulate view data for success display
                ViewData["Validfrontimage"] = existing.Validfrontimage;
                ViewData["ValidBackimage"] = existing.ValidBackimage;
                ViewData["DoctorPrescription"] = existing.DoctorPrescription;
                ViewData["DeathCertificate"] = existing.DeathCertificate;
                ViewData["MedCertificate"] = existing.MedCertificate;

                return View(OtherAssistanceDto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the form: " + ex.Message);

                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = existing.Validfrontimage;
                ViewData["ValidBackimage"] = existing.ValidBackimage;
                ViewData["DoctorPrescription"] = existing.DoctorPrescription;
                ViewData["DeathCertificate"] = existing.DeathCertificate;
                ViewData["MedCertificate"] = existing.MedCertificate;

                return View(OtherAssistanceDto);
            }
        }

        public IActionResult OtherAssistanceedelete(int id)
        {
            var OtherAssistance = context.OtherAssistance.Find(id);
            if (OtherAssistance == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Instead of deleting files and record, just update the status
            OtherAssistance.Status = "Removed";
            context.OtherAssistance.Update(OtherAssistance);
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
            ViewBag.District = HttpContext.Session.GetString("District");
            ViewBag.Barangay = HttpContext.Session.GetString("Barangay");
            ViewBag.Gender = HttpContext.Session.GetString("Gender");
            ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");

            ViewBag.FrontID = HttpContext.Session.GetString("FrontID");
            ViewBag.BackID = HttpContext.Session.GetString("BackID");

            // Get phone number from Verifyaccount table
            var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == userId);
            ViewBag.Phonenumber = verifyAccount?.Phonenumber ?? "";

            return View();
        }



        [HttpPost]
        public IActionResult FuneralAssistance(FuneralAssistanceDto FuneralAssistanceDto)
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
            ModelState.Remove("DoctorPrescriptionimage");
            ModelState.Remove("DeathCertificateimage");

            // NEW VALIDATION: At least one of the medical documents must be provided
            if (FuneralAssistanceDto.DoctorPrescriptionimage == null &&
                FuneralAssistanceDto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one medical document (Doctor Prescription or Death Certificate) is required");
            }

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

                // Generate unique encrypted timestamp for filenames
                string timestamp = _dateTimeService.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeEncryptedTimestamp = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // Generate encrypted filenames
                string? newFileNamePrescription = null;
                string? newFileNameDeathCertificate = null;

                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");

                // Ensure directories exist
                Directory.CreateDirectory(uploadsFolder1);
                Directory.CreateDirectory(uploadsFolder2);

                // Encrypt and Save Prescription Image if provided
                if (FuneralAssistanceDto.DoctorPrescriptionimage != null)
                {
                    newFileNamePrescription = safeEncryptedTimestamp + "_prescription.enc";
                    string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                    using (var fileStream = new FileStream(filePathPrescription, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(FuneralAssistanceDto.DoctorPrescriptionimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Death Certificate Image if provided
                if (FuneralAssistanceDto.DeathCertificateimage != null)
                {
                    newFileNameDeathCertificate = safeEncryptedTimestamp + "_deathcert.enc";
                    string filePathDeathCertificate = Path.Combine(uploadsFolder2, newFileNameDeathCertificate);
                    using (var fileStream = new FileStream(filePathDeathCertificate, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(FuneralAssistanceDto.DeathCertificateimage.OpenReadStream());
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
                    District = FuneralAssistanceDto.District,
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
                    RDistrict = FuneralAssistanceDto.RDistrict,
                    RelationshipPatient = FuneralAssistanceDto.RelationshipPatient,
                    ContactNo = FuneralAssistanceDto.ContactNo,

                    // Assistance Type - Set to default "Funeral Assistance" since no checkboxes
                    Typeassistance = "Funeral Assistance",
                    ForCMOPERSONNEL = FuneralAssistanceDto.ForCMOPERSONNEL,

                    // MODIFIED: Use existing ID images from user account instead of new uploads
                    Validfrontimage = userFrontID,
                    ValidBackimage = userBackID,
                    DoctorPrescription = newFileNamePrescription ?? string.Empty,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = _dateTimeService.Now
                };

                context.FuneralAssistance.Add(FuneralAssistance);
                context.SaveChanges();

                // ? SUCCESS: Set the success flag to trigger the modal
                ViewBag.Success = true;

                // Also set the session data again to repopulate the form
                ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
                ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
                ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
                ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
                ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
                ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
                ViewBag.District = HttpContext.Session.GetString("District");
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
            ViewData["Status2"] = FuneralAssistance.Status2;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
            ViewData["District"] = FuneralAssistance.District;
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
            ViewData["RDistrict"] = FuneralAssistance.RDistrict;
            ViewData["RelationshipPatient"] = FuneralAssistance.RelationshipPatient;
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
                        debugMessages.Add("? Front ID decrypted");
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
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, FuneralAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {FuneralAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

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

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, FuneralAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {FuneralAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

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
            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            ViewData["Comments"] = FuneralAssistance.Comments;

            return View();

        }

        [HttpPost]
        public IActionResult FuneralAssistanceEdit(int id, FuneralAssistanceDto FuneralAssistanceDto)
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

            if (existing.Status != "Pending")
            {
                TempData["ErrorMessage"] = "You can only edit forms that are in 'Pending' status.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ? 4. Remove validations for optional fields
            if (string.IsNullOrEmpty(FuneralAssistanceDto.PhilHealthNo))
                ModelState.Remove("PhilHealthNo");

            // Remove validation requirements for images if they're not provided
            if (FuneralAssistanceDto.IdFrontimage == null) ModelState.Remove("IdFrontimage");
            if (FuneralAssistanceDto.IdBackimage == null) ModelState.Remove("IdBackimage");
            if (FuneralAssistanceDto.DoctorPrescriptionimage == null) ModelState.Remove("DoctorPrescriptionimage");
            if (FuneralAssistanceDto.DeathCertificateimage == null) ModelState.Remove("DeathCertificateimage");

            // ? Require at least one medical document ONLY if both existing docs are empty and no new upload
            if (string.IsNullOrEmpty(existing.DoctorPrescription) &&
                string.IsNullOrEmpty(existing.DeathCertificate) &&
                FuneralAssistanceDto.DoctorPrescriptionimage == null &&
                FuneralAssistanceDto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one medical document (Doctor Prescription or Death Certificate) is required.");
            }

            if (!ModelState.IsValid)
            {
                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = existing.Validfrontimage;
                ViewData["ValidBackimage"] = existing.ValidBackimage;
                ViewData["DoctorPrescription"] = existing.DoctorPrescription;
                ViewData["DeathCertificate"] = existing.DeathCertificate;

                return View(FuneralAssistanceDto);
            }

            try
            {
                var aesHelper = new AesEncryptionHelper(_configuration);
                string timestamp = _dateTimeService.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeName = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // ? 5. Update Doctor Prescription (optional)
                if (FuneralAssistanceDto.DoctorPrescriptionimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.DoctorPrescription))
                    {
                        string oldPath = Path.Combine(folder, existing.DoctorPrescription);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_prescription.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(FuneralAssistanceDto.DoctorPrescriptionimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.DoctorPrescription = fileName;
                }

                // ? 6. Update Death Certificate (optional)
                if (FuneralAssistanceDto.DeathCertificateimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.DeathCertificate))
                    {
                        string oldPath = Path.Combine(folder, existing.DeathCertificate);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_deathcert.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(FuneralAssistanceDto.DeathCertificateimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.DeathCertificate = fileName;
                }

                // ? 7. Update ID images (if new ones provided)
                if (FuneralAssistanceDto.IdFrontimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "Validimg");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.Validfrontimage))
                    {
                        string oldPath = Path.Combine(folder, existing.Validfrontimage);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_front.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(FuneralAssistanceDto.IdFrontimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.Validfrontimage = fileName;
                }

                if (FuneralAssistanceDto.IdBackimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "Validimg");
                    Directory.CreateDirectory(folder);

                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(existing.ValidBackimage))
                    {
                        string oldPath = Path.Combine(folder, existing.ValidBackimage);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    string fileName = safeName + "_back.enc";
                    string filePath = Path.Combine(folder, fileName);

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        byte[] encrypted = aesHelper.EncryptStream(FuneralAssistanceDto.IdBackimage.OpenReadStream());
                        fs.Write(encrypted, 0, encrypted.Length);
                    }

                    existing.ValidBackimage = fileName;
                }

                // ? 8. Update text fields safely
                existing.Lastname = FuneralAssistanceDto.Lastname ?? existing.Lastname;
                existing.Firstname = FuneralAssistanceDto.Firstname ?? existing.Firstname;
                existing.Middlename = FuneralAssistanceDto.Middlename ?? existing.Middlename;
                existing.Suffix = FuneralAssistanceDto.Suffix ?? existing.Suffix;
                existing.BlkLotStreet = FuneralAssistanceDto.BlkLotStreet ?? existing.BlkLotStreet;
                existing.SubVill = FuneralAssistanceDto.SubVill ?? existing.SubVill;
                existing.Brgy = FuneralAssistanceDto.Brgy ?? existing.Brgy;
                existing.District = FuneralAssistanceDto.District ?? existing.District;
                existing.Sex = FuneralAssistanceDto.Sex ?? existing.Sex;
                existing.PhilHealth = FuneralAssistanceDto.PhilHealth ?? existing.PhilHealth;
                existing.PhilHealthNo = FuneralAssistanceDto.PhilHealthNo ?? existing.PhilHealthNo;
                existing.Dateofbirth = FuneralAssistanceDto.Dateofbirth ?? existing.Dateofbirth;
                existing.Age = FuneralAssistanceDto.Age ?? existing.Age;

                // ? Requestor details
                existing.RLastname = FuneralAssistanceDto.RLastname ?? existing.RLastname;
                existing.RFirstname = FuneralAssistanceDto.RFirstname ?? existing.RFirstname;
                existing.RMiddlename = FuneralAssistanceDto.RMiddlename ?? existing.RMiddlename;
                existing.RSuffix = FuneralAssistanceDto.RSuffix ?? existing.RSuffix;
                existing.RBlkLotStreet = FuneralAssistanceDto.RBlkLotStreet ?? existing.RBlkLotStreet;
                existing.RSubVill = FuneralAssistanceDto.RSubVill ?? existing.RSubVill;
                existing.RBrgy = FuneralAssistanceDto.RBrgy ?? existing.RBrgy;
                existing.RDistrict = FuneralAssistanceDto.RDistrict ?? existing.RDistrict;
                existing.RelationshipPatient = FuneralAssistanceDto.RelationshipPatient ?? existing.RelationshipPatient;
                existing.ContactNo = FuneralAssistanceDto.ContactNo ?? existing.ContactNo;

                // ? Assistance info
                existing.Typeassistance = FuneralAssistanceDto.Typeassistance ?? existing.Typeassistance;
                existing.ForCMOPERSONNEL = FuneralAssistanceDto.ForCMOPERSONNEL ?? existing.ForCMOPERSONNEL;

                // ? 9. Update timestamp properly
                existing.CreatedAt = _dateTimeService.Now;

                // ? 10. Save changes
                context.Entry(existing).State = EntityState.Modified;
                context.SaveChanges();

                // ? 11. Set success flag and RAF number for modal
                ViewBag.Success = true;
                TempData["SuccessRAF"] = id.ToString();

                // ? 12. Repopulate view data for success display
                ViewData["Validfrontimage"] = existing.Validfrontimage;
                ViewData["ValidBackimage"] = existing.ValidBackimage;
                ViewData["DoctorPrescription"] = existing.DoctorPrescription;
                ViewData["DeathCertificate"] = existing.DeathCertificate;

                return View(FuneralAssistanceDto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the form: " + ex.Message);

                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = existing.Validfrontimage;
                ViewData["ValidBackimage"] = existing.ValidBackimage;
                ViewData["DoctorPrescription"] = existing.DoctorPrescription;
                ViewData["DeathCertificate"] = existing.DeathCertificate;

                return View(FuneralAssistanceDto);
            }
        }


        public IActionResult FuneralAssistanceedelete(int id)
        {
            var FuneralAssistance = context.FuneralAssistance.Find(id);
            if (FuneralAssistance == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Instead of deleting files and record, just update the status
            FuneralAssistance.Status = "Removed";
            context.FuneralAssistance.Update(FuneralAssistance);
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

            // Get all applications for this user
            var allHospitalBills = context.HospitalAssistance
                .Where(f => f.UserId == userId)
                .ToList();

            var allMedicalLabForms = context.OtherAssistance
                .Where(f => f.UserId == userId)
                .ToList();

            var allFuneralAssistance = context.FuneralAssistance
                .Where(f => f.UserId == userId)
                .ToList();

            // Auto-archive applications older than 1 month and update database
            foreach (var app in allHospitalBills.Where(a => a.CreatedAt < oneMonthAgo && !a.IsArchived))
            {
                app.IsArchived = true;
            }
            foreach (var app in allMedicalLabForms.Where(a => a.CreatedAt < oneMonthAgo && !a.IsArchived))
            {
                app.IsArchived = true;
            }
            foreach (var app in allFuneralAssistance.Where(a => a.CreatedAt < oneMonthAgo && !a.IsArchived))
            {
                app.IsArchived = true;
            }

            // Save changes to database if any applications were archived
            if (context.ChangeTracker.HasChanges())
            {
                context.SaveChanges();
            }

            // Separate into active and archived lists
            var activeHospitalBills = allHospitalBills
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var archivedHospitalBills = allHospitalBills
                .Where(f => f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var activeMedicalLabForms = allMedicalLabForms
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var archivedMedicalLabForms = allMedicalLabForms
                .Where(f => f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var activeFuneralAssistance = allFuneralAssistance
                .Where(f => !f.IsArchived)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var archivedFuneralAssistance = allFuneralAssistance
                .Where(f => f.IsArchived)
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

        public IActionResult Nearbyoffices()
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

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalAssistance = hospitalBills,
                OtherAssistance = medicalLabForms
            };

            // Pass the view model to the view
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
            ViewData["District"] = HospitalAssistance.District;
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
            ViewData["RDistrict"] = HospitalAssistance.RDistrict;
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
            ViewData["Status2"] = FuneralAssistance.Status2;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
            ViewData["District"] = FuneralAssistance.District;
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
            ViewData["RDistrict"] = FuneralAssistance.RDistrict;
            ViewData["RelationshipPatient"] = FuneralAssistance.RelationshipPatient;
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
                        debugMessages.Add("? Front ID decrypted");
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
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, FuneralAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {FuneralAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

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

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, FuneralAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {FuneralAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

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
            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            ViewData["Comments"] = FuneralAssistance.Comments;

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
            ViewData["District"] = medicallabform.District;
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
            ViewData["RDistrict"] = medicallabform.RDistrict;
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
                        debugMessages.Add("? Front ID decrypted");
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
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

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

                // ? MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"?? Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"?? Full path: {medicalPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(medicalPath)}");

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

                            debugMessages.Add($"? Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"?? IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Medical Certificate in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

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
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

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
                string encryptedFilePath = null;
                string folderPath = null;

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
            string filePath = null;

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

            var user = context.RegisterAcc.FirstOrDefault(u => u.Id == userId);
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

            var user = context.RegisterAcc.FirstOrDefault(u => u.Id == userId);
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
                        Type = "Medical"
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
        public IActionResult Feedback(string assistanceType = null, int? assistanceId = null, int? userId = null)
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

                return Json(new { success = true, message = "Thank you for your feedback!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while submitting your feedback. Please try again." });
            }
        }

    }
}
