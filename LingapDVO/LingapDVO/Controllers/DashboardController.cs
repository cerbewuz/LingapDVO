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

        public Dashboard(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration, FormSubmissionSecurityService securityService)
        {
            this.context = context;
            this.environment = environment;
            _configuration = configuration;
            _securityService = securityService;
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
                // ✅ Convert session UserId (string) → int
                int.TryParse(userIdString, out userId);
                ViewBag.Username = HttpContext.Session.GetString("Username");
                ViewBag.Profilepicture = HttpContext.Session.GetString("Profilepicture");
            }
            else if (isAuthenticated)
            {
                string username = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value
                                   ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                                   ?? "User";
                ViewBag.Username = username; 
                ViewBag.Profilepicture = HttpContext.Session.GetString("Profilepicture");
            }

            // ✅ Check if user has completed verification
            var verification = context.Verifyaccount.FirstOrDefault(v => v.UserId == userId);
            bool isVerified = verification != null;
            ViewBag.IsVerified = isVerified;

            // ✅ Now you can safely filter only the logged-in user's data
            var hospitalBills = context.FillupformHospitalBill
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.Medicalandlabform
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var funeralburialform = context.Funeralburialform
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Find the latest document overall
            var allDocs = new List<dynamic>();

            if (hospitalBills.Any())
                allDocs.Add(new { Type = "Hospital Bill", Data = hospitalBills.First() });
            if (medicalLabForms.Any())
                allDocs.Add(new { Type = "Medical/Lab Form", Data = medicalLabForms.First() });
            if (funeralburialform.Any())
                allDocs.Add(new { Type = "Funeral/Burial Form", Data = funeralburialform.First() });

            var latestDoc = allDocs
                .OrderByDescending(d => d.Data.CreatedAt)
                .FirstOrDefault();

            var viewModel = new CombinedFormsViewModel
            {
                HospitalBills = hospitalBills,
                MedicalLabForms = medicalLabForms,
                Funeralburialform = funeralburialform
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

             ViewBag.Id = HttpContext.Session.GetString("UserId");
             ViewBag.IDnumber = HttpContext.Session.GetString("IDnumber");
             ViewBag.IDtype = HttpContext.Session.GetString("IDtype");
             ViewBag.Profilepicture = HttpContext.Session.GetString("Profilepicture");
             ViewBag.Firstname = HttpContext.Session.GetString("Firstname");
             ViewBag.Middlename = HttpContext.Session.GetString("Middlename");
             ViewBag.Lastname = HttpContext.Session.GetString("Lastname");
             ViewBag.Suffix = HttpContext.Session.GetString("Suffix");
             ViewBag.BlkLotStreet = HttpContext.Session.GetString("BlkLotStreet");
             ViewBag.SubVill = HttpContext.Session.GetString("SubVill");
             ViewBag.District = HttpContext.Session.GetString("District");
             ViewBag.Barangay = HttpContext.Session.GetString("Barangay");



             ViewBag.Username = HttpContext.Session.GetString("Username");
             ViewBag.Email = HttpContext.Session.GetString("Email");
             ViewBag.Phonenumber = HttpContext.Session.GetString("Phonenumber");          
             ViewBag.Dateofbirth = HttpContext.Session.GetString("Dateofbirth");
             ViewBag.Gender = HttpContext.Session.GetString("Gender");
             ViewBag.SecurityQuestions = HttpContext.Session.GetString("SecurityQuestions");
        
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

        public async Task<IActionResult> FillupformHospitalBill()
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

            // 🔒 SECURITY: Generate form submission token
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




            return View();
        }

        // ====================================
        // COMPLETE HOSPITAL BILL CONTROLLER - WITH EMBEDDED AES ENCRYPTION HELPER
        // ====================================

        // ╔═══════════════════════════════════════════════════════════════════════════╗
        // ║                    AES-256 ENCRYPTION HELPER CLASS                        ║
        // ║         Secure AES-256 Implementation using Configuration                 ║
        // ╚═══════════════════════════════════════════════════════════════════════════╝
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
        public IActionResult FillupformHospitalBill(FillupformHospitalBillDto fillupformHospitalbilldto)
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
            var oneMonthAgo = DateTime.Now.AddMonths(-1);

            // Check for forms with Status = "Approved" within the last month
            var hasRecentApproval = context.FillupformHospitalBill
                .Any(f => f.UserId == userId && f.Status2 == "Approved" && f.CreatedAt >= oneMonthAgo);

            if (hasRecentApproval)
            {
                // Get the most recent approved form to show the exact date
                var recentApprovedForm = context.FillupformHospitalBill
                    .Where(f => f.UserId == userId && f.Status2 == "Approved")
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefault();

                string approvedDate = recentApprovedForm?.CreatedAt.ToString("MMMM dd, yyyy") ?? "recently";

                ModelState.AddModelError("", $"You cannot submit a new form because you already have an approved request dated {approvedDate}. Please wait one month from {approvedDate} before submitting another application.");
                return View(fillupformHospitalbilldto);
            }

            // SECOND: Check for any pending or processing forms (user can only have one form at a time)
            var hasPendingForm = context.FillupformHospitalBill
                .Any(f => f.UserId == userId && (f.Status == "Pending" || f.Status == "Processing"));

            if (hasPendingForm)
            {
                ModelState.AddModelError("", "You already have a form that is currently pending or being processed. Please wait until it's approved before submitting a new one.");
                return View(fillupformHospitalbilldto);
            }

            // If neither condition above is met, proceed with form submission

            // Optional field handling
            if (string.IsNullOrEmpty(fillupformHospitalbilldto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // MODIFIED: Image validation - Only check for prescription and death certificate images
            // Remove ID image validation since we'll use the existing ones from user account
            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");

            if (fillupformHospitalbilldto.DoctorPrescriptionimage == null && fillupformHospitalbilldto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one image file (Doctor Prescription or Death Certificate) is required");
            }

            if (!ModelState.IsValid)
            {
                return View(fillupformHospitalbilldto);
            }

            try
            {
                // ===========================
                // 🔑 AES-256 FILE ENCRYPTION
                // ===========================
                // Use configuration-based AES helper
                var aesHelper = new AesEncryptionHelper(_configuration);

                // Generate unique encrypted timestamp for filenames
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeEncryptedTimestamp = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // Generate encrypted filenames
                string? newFileNamePrescription = null;
                string? newFileNameDeathCertificate = null;

                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "Funeralimg");

                // Ensure directories exist
                Directory.CreateDirectory(uploadsFolder1);
                Directory.CreateDirectory(uploadsFolder2);

                // Encrypt and Save Prescription Image if provided
                if (fillupformHospitalbilldto.DoctorPrescriptionimage != null)
                {
                    newFileNamePrescription = safeEncryptedTimestamp + "_prescription.enc";
                    string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                    using (var fileStream = new FileStream(filePathPrescription, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(fillupformHospitalbilldto.DoctorPrescriptionimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Death Certificate Image if provided
                if (fillupformHospitalbilldto.DeathCertificateimage != null)
                {
                    newFileNameDeathCertificate = safeEncryptedTimestamp + "_deathcert.enc";
                    string filePathDeathCertificate = Path.Combine(uploadsFolder2, newFileNameDeathCertificate);
                    using (var fileStream = new FileStream(filePathDeathCertificate, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(fillupformHospitalbilldto.DeathCertificateimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Map data to entity
                FillupformHospitalBill fillupformHospitalBill = new FillupformHospitalBill()
                {
                    UserId = userId,
                    // Patient Details
                    Lastname = fillupformHospitalbilldto.Lastname,
                    Firstname = fillupformHospitalbilldto.Firstname,
                    Middlename = fillupformHospitalbilldto.Middlename,
                    Suffix = fillupformHospitalbilldto.Suffix,
                    BlkLotStreet = fillupformHospitalbilldto.BlkLotStreet,
                    SubVill = fillupformHospitalbilldto.SubVill,
                    Brgy = fillupformHospitalbilldto.Brgy,
                    District = fillupformHospitalbilldto.District,
                    Sex = fillupformHospitalbilldto.Sex,
                    PhilHealth = fillupformHospitalbilldto.PhilHealth,
                    PhilHealthNo = fillupformHospitalbilldto.PhilHealthNo,
                    Dateofbirth = fillupformHospitalbilldto.Dateofbirth,
                    Age = fillupformHospitalbilldto.Age,

                    // Requestor Details
                    RLastname = fillupformHospitalbilldto.RLastname,
                    RFirstname = fillupformHospitalbilldto.RFirstname,
                    RMiddlename = fillupformHospitalbilldto.RMiddlename,
                    RSuffix = fillupformHospitalbilldto.RSuffix,
                    RBlkLotStreet = fillupformHospitalbilldto.RBlkLotStreet,
                    RSubVill = fillupformHospitalbilldto.RSubVill,
                    RBrgy = fillupformHospitalbilldto.RBrgy,
                    RDistrict = fillupformHospitalbilldto.RDistrict,
                    RelationshipPatient = fillupformHospitalbilldto.RelationshipPatient,
                    ContactNo = fillupformHospitalbilldto.ContactNo,

                    // Assistance Type
                    Typeassistance = fillupformHospitalbilldto.Typeassistance,
                    ForCMOPERSONNEL = fillupformHospitalbilldto.ForCMOPERSONNEL,

                    // MODIFIED: Use existing ID images from user account instead of new uploads
                    Validfrontimage = userFrontID,
                    ValidBackimage = userBackID,
                    DoctorPrescription = newFileNamePrescription ?? string.Empty,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = DateTime.Now
                };

                context.FillupformHospitalBill.Add(fillupformHospitalBill);
                context.SaveChanges();

                // ✅ SUCCESS: Set the success flag to trigger the modal
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

                return View(fillupformHospitalbilldto);
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

                    return View(fillupformHospitalbilldto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(fillupformHospitalbilldto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(fillupformHospitalbilldto);
            }
        }
        public IActionResult FillupformHospitalBilledit(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }


            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);
            if (fillupformhospitalBill == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = fillupformhospitalBill.Status;
            ViewData["Id"] = fillupformhospitalBill.Id;
            ViewData["Lastname"] = fillupformhospitalBill.Lastname;
            ViewData["Firstname"] = fillupformhospitalBill.Firstname;
            ViewData["Middlename"] = fillupformhospitalBill.Middlename;
            ViewData["Suffix"] = fillupformhospitalBill.Suffix;
            ViewData["BlkLotStreet"] = fillupformhospitalBill.BlkLotStreet;
            ViewData["SubVill"] = fillupformhospitalBill.SubVill;
            ViewData["Brgy"] = fillupformhospitalBill.Brgy;
            ViewData["District"] = fillupformhospitalBill.District;
            ViewData["Sex"] = fillupformhospitalBill.Sex;
            ViewData["PhilHealth"] = fillupformhospitalBill.PhilHealth;
            ViewData["PhilHealthNo"] = fillupformhospitalBill.PhilHealthNo;
            ViewData["Dateofbirth"] = fillupformhospitalBill.Dateofbirth;
            ViewData["Age"] = fillupformhospitalBill.Age;

            // Requestor details
            ViewData["RLastname"] = fillupformhospitalBill.RLastname;
            ViewData["RFirstname"] = fillupformhospitalBill.RFirstname;
            ViewData["RMiddlename"] = fillupformhospitalBill.RMiddlename;
            ViewData["RSuffix"] = fillupformhospitalBill.RSuffix;
            ViewData["RBlkLotStreet"] = fillupformhospitalBill.RBlkLotStreet;
            ViewData["RSubVill"] = fillupformhospitalBill.RSubVill;
            ViewData["RBrgy"] = fillupformhospitalBill.RBrgy;
            ViewData["RDistrict"] = fillupformhospitalBill.RDistrict;
            ViewData["RelationshipPatient"] = fillupformhospitalBill.RelationshipPatient;
            ViewData["ContactNo"] = fillupformhospitalBill.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = fillupformhospitalBill.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = fillupformhospitalBill.ForCMOPERSONNEL ?? "";
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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(fillupformhospitalBill.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, fillupformhospitalBill.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(fillupformhospitalBill.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, fillupformhospitalBill.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED
                if (!string.IsNullOrEmpty(fillupformhospitalBill.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, fillupformhospitalBill.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {fillupformhospitalBill.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"✅ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"🔍 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Doctor Prescription in database");
                }

                // ⭐ DEATH CERTIFICATE - UPDATED
                if (!string.IsNullOrEmpty(fillupformhospitalBill.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, fillupformhospitalBill.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {fillupformhospitalBill.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"✅ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"🔍 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"❌ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = fillupformhospitalBill.Validfrontimage;
            ViewData["ValidBackimage"] = fillupformhospitalBill.ValidBackimage;
            ViewData["Comments"] = fillupformhospitalBill.Comments;

            return View();

        }

        [HttpPost]
        public IActionResult FillupformHospitalBilledit(int id, FillupformHospitalBillDto dto)
        {
            // ✅ 1. Check user session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                return RedirectToAction("Login", "Login");
            }

            // ✅ 2. Get existing record
            var existing = context.FillupformHospitalBill.Find(id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Form not found.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // ✅ 3. Security checks
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

            // ✅ 4. Remove validations for optional fields
            if (string.IsNullOrEmpty(dto.PhilHealthNo))
                ModelState.Remove("PhilHealthNo");

            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");

            // ✅ Require at least one doc ONLY if both existing docs are empty and no new upload
            if (string.IsNullOrEmpty(existing.DoctorPrescription) &&
                string.IsNullOrEmpty(existing.DeathCertificate) &&
                dto.DoctorPrescriptionimage == null &&
                dto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one document is required.");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate view
                ViewData["CurrentDoctorPrescription"] = existing.DoctorPrescription;
                ViewData["CurrentDeathCertificate"] = existing.DeathCertificate;
                ViewData["CurrentValidFront"] = existing.Validfrontimage;
                ViewData["CurrentValidBack"] = existing.ValidBackimage;
                return View(dto);
            }

            try
            {
                var aesHelper = new AesEncryptionHelper(_configuration);
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeName = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // ✅ 5. Update Doctor Prescription (optional)
                if (dto.DoctorPrescriptionimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
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

                // ✅ 6. Update Death Certificate (optional)
                if (dto.DeathCertificateimage != null)
                {
                    string folder = Path.Combine(environment.WebRootPath, "Funeralimg");
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

                // ✅ 7. Update text fields safely
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

                // ✅ Requestor details
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

                // ✅ Assistance info
                existing.Typeassistance = dto.Typeassistance ?? existing.Typeassistance;
                existing.ForCMOPERSONNEL = dto.ForCMOPERSONNEL ?? existing.ForCMOPERSONNEL;

                // ✅ 8. Update ID images (if session updated)
                string frontID = HttpContext.Session.GetString("FrontID") ?? "";
                string backID = HttpContext.Session.GetString("BackID") ?? "";
                if (!string.IsNullOrEmpty(frontID)) existing.Validfrontimage = frontID;
                if (!string.IsNullOrEmpty(backID)) existing.ValidBackimage = backID;

                // ✅ 9. Update timestamp properly
                existing.CreatedAt = DateTime.Now;

                // ✅ 10. Save changes
                context.Entry(existing).State = EntityState.Modified;
                context.SaveChanges();

                TempData["SuccessMessage"] = "Form updated successfully!";
                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the form: " + ex.Message);

                ViewData["CurrentDoctorPrescription"] = existing.DoctorPrescription;
                ViewData["CurrentDeathCertificate"] = existing.DeathCertificate;
                ViewData["CurrentValidFront"] = existing.Validfrontimage;
                ViewData["CurrentValidBack"] = existing.ValidBackimage;

                return View(dto);
            }
        }


        public IActionResult FillupformHospitalBilldelete(int id)
        {
            var fillupformHospitalbill = context.FillupformHospitalBill.Find(id);
            if (fillupformHospitalbill == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Instead of deleting files and record, just update the status
            fillupformHospitalbill.Status = "Removed";
            context.FillupformHospitalBill.Update(fillupformHospitalbill);
            context.SaveChanges();

            return RedirectToAction("Homepage", "Dashboard");
        }

        public IActionResult Medicalandlabform()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
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

            return View();
        }
        //1
        [HttpPost]
        public IActionResult Medicalandlabform(MedicalandlabformDto medicalandlabformdto)
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
            var oneMonthAgo = DateTime.Now.AddMonths(-1);

            // Check for forms with Status = "Approved" within the last month
            var hasRecentApproval = context.Medicalandlabform
                .Any(f => f.UserId == userId && f.Status == "Approved" && f.CreatedAt >= oneMonthAgo);

            if (hasRecentApproval)
            {
                // Get the most recent approved form to show the exact date
                var recentApprovedForm = context.Medicalandlabform
                    .Where(f => f.UserId == userId && f.Status == "Approved")
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefault();

                string approvedDate = recentApprovedForm?.CreatedAt.ToString("MMMM dd, yyyy") ?? "recently";

                ModelState.AddModelError("", $"You cannot submit a new form because you already have an approved request dated {approvedDate}. Please wait one month from {approvedDate} before submitting another application.");
                return View(medicalandlabformdto);
            }

            // SECOND: Check for any pending or processing forms (user can only have one form at a time)
            var hasPendingForm = context.Medicalandlabform
                .Any(f => f.UserId == userId && (f.Status == "Pending" || f.Status == "Processing"));

            if (hasPendingForm)
            {
                ModelState.AddModelError("", "You already have a form that is currently pending or being processed. Please wait until it's approved before submitting a new one.");
                return View(medicalandlabformdto);
            }

            // If neither condition above is met, proceed with form submission

            // Optional field handling
            if (string.IsNullOrEmpty(medicalandlabformdto.PhilHealthNo))
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
            if (medicalandlabformdto.DoctorPrescriptionimage == null &&
                medicalandlabformdto.DeathCertificateimage == null &&
                medicalandlabformdto.MedCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one medical document (Doctor Prescription, Death Certificate, or Medical Certificate) is required");
            }

            if (!ModelState.IsValid)
            {
                return View(medicalandlabformdto);
            }

            try
            {
                // ===========================
                // 🔑 AES-256 FILE ENCRYPTION
                // ===========================
                // Use configuration-based AES helper
                var aesHelper = new AesEncryptionHelper(_configuration);

                // Generate unique encrypted timestamp for filenames
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeEncryptedTimestamp = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // Generate encrypted filenames
                string? newFileNamePrescription = null;
                string? newFileNameDeathCertificate = null;
                string? newFileNameMedCertificate = null;

                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "Funeralimg");
                string uploadsFolder3 = Path.Combine(environment.WebRootPath, "MedCertificateimage");

                // Ensure directories exist
                Directory.CreateDirectory(uploadsFolder1);
                Directory.CreateDirectory(uploadsFolder2);
                Directory.CreateDirectory(uploadsFolder3);

                // Encrypt and Save Prescription Image if provided
                if (medicalandlabformdto.DoctorPrescriptionimage != null)
                {
                    newFileNamePrescription = safeEncryptedTimestamp + "_prescription.enc";
                    string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                    using (var fileStream = new FileStream(filePathPrescription, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(medicalandlabformdto.DoctorPrescriptionimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Death Certificate Image if provided
                if (medicalandlabformdto.DeathCertificateimage != null)
                {
                    newFileNameDeathCertificate = safeEncryptedTimestamp + "_deathcert.enc";
                    string filePathDeathCertificate = Path.Combine(uploadsFolder2, newFileNameDeathCertificate);
                    using (var fileStream = new FileStream(filePathDeathCertificate, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(medicalandlabformdto.DeathCertificateimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Medical Certificate Image if provided
                if (medicalandlabformdto.MedCertificateimage != null)
                {
                    newFileNameMedCertificate = safeEncryptedTimestamp + "_medcert.enc";
                    string filePathMedCertificate = Path.Combine(uploadsFolder3, newFileNameMedCertificate);
                    using (var fileStream = new FileStream(filePathMedCertificate, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(medicalandlabformdto.MedCertificateimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Map data to entity
                Medicalandlabform medicalandlabform = new Medicalandlabform()
                {
                    UserId = userId,
                    // Patient Details
                    Lastname = medicalandlabformdto.Lastname,
                    Firstname = medicalandlabformdto.Firstname,
                    Middlename = medicalandlabformdto.Middlename,
                    Suffix = medicalandlabformdto.Suffix,
                    BlkLotStreet = medicalandlabformdto.BlkLotStreet,
                    SubVill = medicalandlabformdto.SubVill,
                    Brgy = medicalandlabformdto.Brgy,
                    District = medicalandlabformdto.District,
                    Sex = medicalandlabformdto.Sex,
                    PhilHealth = medicalandlabformdto.PhilHealth,
                    PhilHealthNo = medicalandlabformdto.PhilHealthNo,
                    Dateofbirth = medicalandlabformdto.Dateofbirth,
                    Age = medicalandlabformdto.Age,

                    // Requestor Details
                    RLastname = medicalandlabformdto.RLastname,
                    RFirstname = medicalandlabformdto.RFirstname,
                    RMiddlename = medicalandlabformdto.RMiddlename,
                    RSuffix = medicalandlabformdto.RSuffix,
                    RBlkLotStreet = medicalandlabformdto.RBlkLotStreet,
                    RSubVill = medicalandlabformdto.RSubVill,
                    RBrgy = medicalandlabformdto.RBrgy,
                    RDistrict = medicalandlabformdto.RDistrict,
                    RelationshipPatient = medicalandlabformdto.RelationshipPatient,
                    ContactNo = medicalandlabformdto.ContactNo,

                    // Assistance Type
                    Typeassistance = medicalandlabformdto.Typeassistance,
                    ForCMOPERSONNEL = medicalandlabformdto.ForCMOPERSONNEL,

                    // MODIFIED: Use existing ID images from user account instead of new uploads
                    Validfrontimage = userFrontID,
                    ValidBackimage = userBackID,
                    DoctorPrescription = newFileNamePrescription ?? string.Empty,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    MedCertificate = newFileNameMedCertificate ?? string.Empty,
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = DateTime.Now
                };

                context.Medicalandlabform.Add(medicalandlabform);
                context.SaveChanges();

                // ✅ SUCCESS: Set the success flag to trigger the modal
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

                return View(medicalandlabformdto);
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

                    return View(medicalandlabformdto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(medicalandlabformdto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(medicalandlabformdto);
            }
        }

        public IActionResult Medicalandlabformedit(int id)
        {
            var medicalandlabform = context.Medicalandlabform.Find(id);


            if (medicalandlabform == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Id"] = medicalandlabform.Id;
            ViewData["Lastname"] = medicalandlabform.Lastname;
            ViewData["Firstname"] = medicalandlabform.Firstname;
            ViewData["Middlename"] = medicalandlabform.Middlename;
            ViewData["Suffix"] = medicalandlabform.Suffix;
            ViewData["BlkLotStreet"] = medicalandlabform.BlkLotStreet;
            ViewData["SubVill"] = medicalandlabform.SubVill;
            ViewData["Brgy"] =  medicalandlabform.Brgy;
            ViewData["District"] = medicalandlabform.District;
            ViewData["Sex"] = medicalandlabform.Sex;
            ViewData["PhilHealth"] = medicalandlabform.PhilHealth;
            ViewData["PhilHealthNo"] = medicalandlabform.PhilHealthNo;
            ViewData["Dateofbirth"] = medicalandlabform.Dateofbirth;
            ViewData["Age"] = medicalandlabform.Age;

            // Requestor details
            ViewData["RLastname"] = medicalandlabform.RLastname;
            ViewData["RFirstname"] = medicalandlabform.RFirstname;
            ViewData["RMiddlename"] = medicalandlabform.RMiddlename;
            ViewData["RSuffix"] = medicalandlabform.RSuffix;
            ViewData["RBlkLotStreet"] = medicalandlabform.RBlkLotStreet;
            ViewData["RSubVill"] = medicalandlabform.RSubVill;
            ViewData["RBrgy"] = medicalandlabform.RBrgy;
            ViewData["RDistrict"] = medicalandlabform.RDistrict;
            ViewData["RelationshipPatient"] = medicalandlabform.RelationshipPatient;
            ViewData["ContactNo"] = medicalandlabform.ContactNo;

            // Type of assistance and CMO details
            var typeAssistanceRaw = medicalandlabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw = medicalandlabform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] = medicalandlabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicalandlabform.ValidBackimage;

            ViewData["DoctorPrescription"] = medicalandlabform.DoctorPrescription;
            ViewData["DeathCertificate"] = medicalandlabform.DeathCertificate;

            return View();
 
        }

        [HttpPost]
        public IActionResult Medicalandlabformedit(int id, MedicalandlabformDto medicalandlabformdto)
        {
            var medicalandlabform = context.Medicalandlabform.Find(id);

            if (medicalandlabform == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            if (string.IsNullOrEmpty(medicalandlabformdto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // Remove validation requirements for images if they're not provided
            if (medicalandlabformdto.IdFrontimage == null) ModelState.Remove("IdFrontimage");
            if (medicalandlabformdto.IdBackimage == null) ModelState.Remove("IdBackimage");
            if (medicalandlabformdto.DoctorPrescriptionimage == null) ModelState.Remove("DoctorPrescriptionimage");
            if (medicalandlabformdto.DeathCertificateimage == null) ModelState.Remove("DeathCertificateimage");

            if (!ModelState.IsValid)
            {
                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = medicalandlabform.Validfrontimage;
                ViewData["ValidBackimage"] = medicalandlabform.ValidBackimage;
                ViewData["DoctorPrescription"] = medicalandlabform.DoctorPrescription;
                ViewData["DeathCertificate"] = medicalandlabform.DeathCertificate;

                return View(medicalandlabformdto);
            }

            try
            {
                // Update text properties
                medicalandlabform.Lastname = medicalandlabformdto.Lastname ?? medicalandlabform.Lastname;
                medicalandlabform.Firstname = medicalandlabformdto.Firstname ?? medicalandlabform.Firstname;
                medicalandlabform.Middlename = medicalandlabformdto.Middlename ?? medicalandlabform.Middlename;
                medicalandlabform.Suffix = medicalandlabformdto.Suffix ?? medicalandlabform.Suffix;
                medicalandlabform.BlkLotStreet = medicalandlabformdto.BlkLotStreet ?? medicalandlabform.BlkLotStreet;
                medicalandlabform.SubVill = medicalandlabformdto.SubVill ?? medicalandlabform.SubVill;
                medicalandlabform.Brgy = medicalandlabformdto.Brgy ?? medicalandlabform.Brgy;
                medicalandlabform.District = medicalandlabformdto.District ?? medicalandlabform.District;
                medicalandlabform.Sex = medicalandlabformdto.Sex ?? medicalandlabform.Sex;
                medicalandlabform.PhilHealth = medicalandlabformdto.PhilHealth ?? medicalandlabform.PhilHealth;
                medicalandlabform.PhilHealthNo = medicalandlabformdto.PhilHealthNo;
                medicalandlabform.Dateofbirth = medicalandlabformdto.Dateofbirth ?? medicalandlabform.Dateofbirth;
                medicalandlabform.Age = medicalandlabformdto.Age ?? medicalandlabform.Age;

                // Requestor Details
                medicalandlabform.RLastname = medicalandlabformdto.RLastname;
                medicalandlabform.RFirstname = medicalandlabformdto.RFirstname;
                medicalandlabform.RMiddlename = medicalandlabformdto.RMiddlename;
                medicalandlabform.RSuffix = medicalandlabformdto.RSuffix;
                medicalandlabform.RBlkLotStreet = medicalandlabformdto.RBlkLotStreet;
                medicalandlabform.RSubVill = medicalandlabformdto.RSubVill;
                medicalandlabform.RBrgy = medicalandlabformdto.RBrgy;
                medicalandlabform.RDistrict = medicalandlabformdto.RDistrict;
                medicalandlabform.RelationshipPatient = medicalandlabformdto.RelationshipPatient;
                medicalandlabform.ContactNo = medicalandlabformdto.ContactNo;

                // Assistance Type
                medicalandlabform.Typeassistance = medicalandlabformdto.Typeassistance ?? medicalandlabform.Typeassistance;
                medicalandlabform.ForCMOPERSONNEL = medicalandlabformdto.ForCMOPERSONNEL;

                // Handle ID Front image
                if (medicalandlabformdto.IdFrontimage != null)
                {
                    string newFileNameFront = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(medicalandlabformdto.IdFrontimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameFront);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(medicalandlabform.Validfrontimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, medicalandlabform.Validfrontimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        medicalandlabformdto.IdFrontimage.CopyTo(stream);
                    }

                    medicalandlabform.Validfrontimage = newFileNameFront;
                }

                // Handle ID Back image
                if (medicalandlabformdto.IdBackimage != null)
                {
                    string newFileNameBack = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(medicalandlabformdto.IdBackimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameBack);

                    if (!string.IsNullOrEmpty(medicalandlabform.ValidBackimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, medicalandlabform.ValidBackimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        medicalandlabformdto.IdBackimage.CopyTo(stream);
                    }

                    medicalandlabform.ValidBackimage = newFileNameBack;
                }

                // Handle Doctor Prescription image
                if (medicalandlabformdto.DoctorPrescriptionimage != null)
                {
                    string newFileNamePrescription = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(medicalandlabformdto.DoctorPrescriptionimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    string filePath = Path.Combine(uploadsFolder, newFileNamePrescription);

                    if (!string.IsNullOrEmpty(medicalandlabform.DoctorPrescription))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, medicalandlabform.DoctorPrescription);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        medicalandlabformdto.DoctorPrescriptionimage.CopyTo(stream);
                    }

                    medicalandlabform.DoctorPrescription = newFileNamePrescription;
                }

                // Handle Death Certificate image
                if (medicalandlabformdto.DeathCertificateimage != null)
                {
                    string newFileNameDeathCertificate = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(medicalandlabformdto.DeathCertificateimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameDeathCertificate);

                    if (!string.IsNullOrEmpty(medicalandlabform.DeathCertificate))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, medicalandlabform.DeathCertificate);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        medicalandlabformdto.DeathCertificateimage.CopyTo(stream);
                    }

                    medicalandlabform.DeathCertificate = newFileNameDeathCertificate;
                }

                context.SaveChanges();
                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);

                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = medicalandlabform.Validfrontimage;
                ViewData["ValidBackimage"] = medicalandlabform.ValidBackimage;
                ViewData["DoctorPrescription"] = medicalandlabform.DoctorPrescription;
                ViewData["DeathCertificate"] = medicalandlabform.DeathCertificate;

                return View(medicalandlabformdto);
            }
        }

        public IActionResult Medicalandlabformedelete(int id)
        {
            var medicalandlabform = context.Medicalandlabform.Find(id);
            if (medicalandlabform == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Instead of deleting files and record, just update the status
            medicalandlabform.Status = "Removed";
            context.Medicalandlabform.Update(medicalandlabform);
            context.SaveChanges();

            return RedirectToAction("Homepage", "Dashboard");
        }

        public IActionResult Funeralburialform()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
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
            return View();
        }



        [HttpPost]
        public IActionResult Funeralburialform(FuneralburialformDto funeralburialformdto)
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
            var oneMonthAgo = DateTime.Now.AddMonths(-1);

            // Check for forms with Status = "Approved" within the last month
            var hasRecentApproval = context.Funeralburialform
                .Any(f => f.UserId == userId && f.Status == "Approved" && f.CreatedAt >= oneMonthAgo);

            if (hasRecentApproval)
            {
                // Get the most recent approved form to show the exact date
                var recentApprovedForm = context.Funeralburialform
                    .Where(f => f.UserId == userId && f.Status == "Approved")
                    .OrderByDescending(f => f.CreatedAt)
                    .FirstOrDefault();

                string approvedDate = recentApprovedForm?.CreatedAt.ToString("MMMM dd, yyyy") ?? "recently";

                ModelState.AddModelError("", $"You cannot submit a new form because you already have an approved request dated {approvedDate}. Please wait one month from {approvedDate} before submitting another application.");
                return View(funeralburialformdto);
            }

            // SECOND: Check for any pending or processing forms (user can only have one form at a time)
            var hasPendingForm = context.Funeralburialform
                .Any(f => f.UserId == userId && (f.Status == "Pending" || f.Status == "Processing"));

            if (hasPendingForm)
            {
                ModelState.AddModelError("", "You already have a form that is currently pending or being processed. Please wait until it's approved before submitting a new one.");
                return View(funeralburialformdto);
            }

            // If neither condition above is met, proceed with form submission

            // Optional field handling
            if (string.IsNullOrEmpty(funeralburialformdto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // MODIFIED: Image validation - Remove ID image validation since we'll use existing ones from user account
            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");
            ModelState.Remove("DoctorPrescriptionimage");
            ModelState.Remove("DeathCertificateimage");

            // NEW VALIDATION: At least one of the medical documents must be provided
            if (funeralburialformdto.DoctorPrescriptionimage == null &&
                funeralburialformdto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("DoctorPrescriptionimage", "At least one medical document (Doctor Prescription or Death Certificate) is required");
            }

            if (!ModelState.IsValid)
            {
                return View(funeralburialformdto);
            }

            try
            {
                // ===========================
                // 🔑 AES-256 FILE ENCRYPTION
                // ===========================
                // Use configuration-based AES helper
                var aesHelper = new AesEncryptionHelper(_configuration);

                // Generate unique encrypted timestamp for filenames
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeEncryptedTimestamp = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // Generate encrypted filenames
                string? newFileNamePrescription = null;
                string? newFileNameDeathCertificate = null;

                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "Funeralimg");

                // Ensure directories exist
                Directory.CreateDirectory(uploadsFolder1);
                Directory.CreateDirectory(uploadsFolder2);

                // Encrypt and Save Prescription Image if provided
                if (funeralburialformdto.DoctorPrescriptionimage != null)
                {
                    newFileNamePrescription = safeEncryptedTimestamp + "_prescription.enc";
                    string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                    using (var fileStream = new FileStream(filePathPrescription, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(funeralburialformdto.DoctorPrescriptionimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Death Certificate Image if provided
                if (funeralburialformdto.DeathCertificateimage != null)
                {
                    newFileNameDeathCertificate = safeEncryptedTimestamp + "_deathcert.enc";
                    string filePathDeathCertificate = Path.Combine(uploadsFolder2, newFileNameDeathCertificate);
                    using (var fileStream = new FileStream(filePathDeathCertificate, FileMode.Create))
                    {
                        // Use configuration-based AES helper to encrypt file stream
                        byte[] encryptedData = aesHelper.EncryptStream(funeralburialformdto.DeathCertificateimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Map data to entity
                Funeralburialform funeralburialform = new Funeralburialform()
                {
                    UserId = userId,
                    // Patient Details
                    Lastname = funeralburialformdto.Lastname,
                    Firstname = funeralburialformdto.Firstname,
                    Middlename = funeralburialformdto.Middlename,
                    Suffix = funeralburialformdto.Suffix,
                    BlkLotStreet = funeralburialformdto.BlkLotStreet,
                    SubVill = funeralburialformdto.SubVill,
                    Brgy = funeralburialformdto.Brgy,
                    District = funeralburialformdto.District,
                    Sex = funeralburialformdto.Sex,
                    PhilHealth = funeralburialformdto.PhilHealth,
                    PhilHealthNo = funeralburialformdto.PhilHealthNo,
                    Dateofbirth = funeralburialformdto.Dateofbirth,
                    Age = funeralburialformdto.Age,

                    // Requestor Details
                    RLastname = funeralburialformdto.RLastname,
                    RFirstname = funeralburialformdto.RFirstname,
                    RMiddlename = funeralburialformdto.RMiddlename,
                    RSuffix = funeralburialformdto.RSuffix,
                    RBlkLotStreet = funeralburialformdto.RBlkLotStreet,
                    RSubVill = funeralburialformdto.RSubVill,
                    RBrgy = funeralburialformdto.RBrgy,
                    RDistrict = funeralburialformdto.RDistrict,
                    RelationshipPatient = funeralburialformdto.RelationshipPatient,
                    ContactNo = funeralburialformdto.ContactNo,

                    // Assistance Type
                    Typeassistance = funeralburialformdto.Typeassistance,
                    ForCMOPERSONNEL = funeralburialformdto.ForCMOPERSONNEL,

                    // MODIFIED: Use existing ID images from user account instead of new uploads
                    Validfrontimage = userFrontID,
                    ValidBackimage = userBackID,
                    DoctorPrescription = newFileNamePrescription ?? string.Empty,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = DateTime.Now
                };

                context.Funeralburialform.Add(funeralburialform);
                context.SaveChanges();

                // ✅ SUCCESS: Set the success flag to trigger the modal
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

                return View(funeralburialformdto);
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

                    return View(funeralburialformdto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(funeralburialformdto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(funeralburialformdto);
            }
        }

        public IActionResult Funeralburialformedit(int id)
        {
            var funeralburialform = context.Funeralburialform.Find(id);


            if (funeralburialform == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Id"] = funeralburialform.Id;
            ViewData["Lastname"] = funeralburialform.Lastname;
            ViewData["Firstname"] = funeralburialform.Firstname;
            ViewData["Middlename"] = funeralburialform.Middlename;
            ViewData["Suffix"] = funeralburialform.Suffix;
            ViewData["BlkLotStreet"] = funeralburialform.BlkLotStreet;
            ViewData["SubVill"] = funeralburialform.SubVill;
            ViewData["Brgy"] = funeralburialform.Brgy;
            ViewData["District"] = funeralburialform.District;
            ViewData["Sex"] = funeralburialform.Sex;
            ViewData["PhilHealth"] = funeralburialform.PhilHealth;
            ViewData["PhilHealthNo"] = funeralburialform.PhilHealthNo;
            ViewData["Dateofbirth"] = funeralburialform.Dateofbirth;
            ViewData["Age"] = funeralburialform.Age;

            // Requestor details
            ViewData["RLastname"] = funeralburialform.RLastname;
            ViewData["RFirstname"] = funeralburialform.RFirstname;
            ViewData["RMiddlename"] = funeralburialform.RMiddlename;
            ViewData["RSuffix"] = funeralburialform.RSuffix;
            ViewData["RBlkLotStreet"] = funeralburialform.RBlkLotStreet;
            ViewData["RSubVill"] = funeralburialform.RSubVill;
            ViewData["RBrgy"] = funeralburialform.RBrgy;
            ViewData["RDistrict"] = funeralburialform.RDistrict;
            ViewData["RelationshipPatient"] = funeralburialform.RelationshipPatient;
            ViewData["ContactNo"] = funeralburialform.ContactNo;

            // Type of assistance and CMO details
            var typeAssistanceRaw = funeralburialform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw = funeralburialform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
            ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;

            ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;
            ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;


            return View();

        }

        [HttpPost]
        public IActionResult Funeralburialformedit(int id, FuneralburialformDto funeralburialformdto)
        {
            var funeralburialform = context.Funeralburialform.Find(id);

            if (funeralburialform == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            if (string.IsNullOrEmpty(funeralburialformdto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // Remove validation requirements for images if they're not provided
            if (funeralburialformdto.IdFrontimage == null) ModelState.Remove("IdFrontimage");
            if (funeralburialformdto.IdBackimage == null) ModelState.Remove("IdBackimage");
            if (funeralburialformdto.DoctorPrescriptionimage == null) ModelState.Remove("DoctorPrescriptionimage");
            if (funeralburialformdto.DeathCertificateimage == null) ModelState.Remove("DeathCertificateimage");

            if (!ModelState.IsValid)
            {
                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
                ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;
                ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;
                ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

                return View(funeralburialformdto);
            }

            try
            {
                // Update text properties
                funeralburialform.Lastname = funeralburialformdto.Lastname ?? funeralburialform.Lastname;
                funeralburialform.Firstname = funeralburialformdto.Firstname ?? funeralburialform.Firstname;
                funeralburialform.Middlename = funeralburialformdto.Middlename ?? funeralburialform.Middlename;
                funeralburialform.Suffix = funeralburialformdto.Suffix ?? funeralburialform.Suffix;
                funeralburialform.BlkLotStreet = funeralburialformdto.BlkLotStreet ?? funeralburialform.BlkLotStreet;
                funeralburialform.SubVill = funeralburialformdto.SubVill ?? funeralburialform.SubVill;
                funeralburialform.Brgy = funeralburialformdto.Brgy ?? funeralburialform.Brgy;
                funeralburialform.District = funeralburialformdto.District ?? funeralburialform.District;
                funeralburialform.Sex = funeralburialformdto.Sex ?? funeralburialform.Sex;
                funeralburialform.PhilHealth = funeralburialformdto.PhilHealth ?? funeralburialform.PhilHealth;
                funeralburialform.PhilHealthNo = funeralburialformdto.PhilHealthNo;
                funeralburialform.Dateofbirth = funeralburialformdto.Dateofbirth ?? funeralburialform.Dateofbirth;
                funeralburialform.Age = funeralburialformdto.Age ?? funeralburialform.Age;

                // Requestor Details
                funeralburialform.RLastname = funeralburialformdto.RLastname;
                funeralburialform.RFirstname = funeralburialformdto.RFirstname;
                funeralburialform.RMiddlename = funeralburialformdto.RMiddlename;
                funeralburialform.RSuffix = funeralburialformdto.RSuffix;
                funeralburialform.RBlkLotStreet = funeralburialformdto.RBlkLotStreet;
                funeralburialform.RSubVill = funeralburialformdto.RSubVill;
                funeralburialform.RBrgy = funeralburialformdto.RBrgy;
                funeralburialform.RDistrict = funeralburialformdto.RDistrict;
                funeralburialform.RelationshipPatient = funeralburialformdto.RelationshipPatient;
                funeralburialform.ContactNo = funeralburialformdto.ContactNo;

                // Assistance Type
                funeralburialform.Typeassistance = funeralburialformdto.Typeassistance ?? funeralburialform.Typeassistance;
                funeralburialform.ForCMOPERSONNEL = funeralburialformdto.ForCMOPERSONNEL;

                // Handle ID Front image
                if (funeralburialformdto.IdFrontimage != null)
                {
                    string newFileNameFront = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(funeralburialformdto.IdFrontimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameFront);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(funeralburialform.Validfrontimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, funeralburialform.Validfrontimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        funeralburialformdto.IdFrontimage.CopyTo(stream);
                    }

                    funeralburialform.Validfrontimage = newFileNameFront;
                }

                // Handle ID Back image
                if (funeralburialformdto.IdBackimage != null)
                {
                    string newFileNameBack = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(funeralburialformdto.IdBackimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameBack);

                    if (!string.IsNullOrEmpty(funeralburialform.ValidBackimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, funeralburialform.ValidBackimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        funeralburialformdto.IdBackimage.CopyTo(stream);
                    }

                    funeralburialform.ValidBackimage = newFileNameBack;
                }

                // Handle Doctor Prescription image
                if (funeralburialformdto.DoctorPrescriptionimage != null)
                {
                    string newFileNamePrescription = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(funeralburialformdto.DoctorPrescriptionimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    string filePath = Path.Combine(uploadsFolder, newFileNamePrescription);

                    if (!string.IsNullOrEmpty(funeralburialform.DoctorPrescription))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, funeralburialform.DoctorPrescription);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        funeralburialformdto.DoctorPrescriptionimage.CopyTo(stream);
                    }

                    funeralburialform.DoctorPrescription = newFileNamePrescription;
                }

                // Handle Death Certificate image
                if (funeralburialformdto.DeathCertificateimage != null)
                {
                    string newFileNameDeathCertificate = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(funeralburialformdto.DeathCertificateimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameDeathCertificate);

                    if (!string.IsNullOrEmpty(funeralburialform.DeathCertificate))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, funeralburialform.DeathCertificate);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        funeralburialformdto.DeathCertificateimage.CopyTo(stream);
                    }

                    funeralburialform.DeathCertificate = newFileNameDeathCertificate;
                }

                context.SaveChanges();
                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);

                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
                ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;
                ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;
                ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

                return View(funeralburialformdto );
            }
        }


        public IActionResult Funeralburialformedelete(int id)
        {
            var Funeralburialform = context.Funeralburialform.Find(id);
            if (Funeralburialform == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Instead of deleting files and record, just update the status
            Funeralburialform.Status = "Removed";
            context.Funeralburialform.Update(Funeralburialform);
            context.SaveChanges();

            return RedirectToAction("Homepage", "Dashboard");
        }





        public IActionResult Uploads()
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
            var hospitalBills = context.FillupformHospitalBill
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.Medicalandlabform
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var funeralburialform = context.Funeralburialform
             .Where(f => f.UserId == userId)
             .OrderByDescending(f => f.CreatedAt)
             .ToList();

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalBills = hospitalBills,
                MedicalLabForms = medicalLabForms,
                Funeralburialform = funeralburialform //
            };

            // Pass the view model to the view
            return View(viewModel);

        }

        public IActionResult Maps()
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
            var hospitalBills = context.FillupformHospitalBill
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.Medicalandlabform
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalBills = hospitalBills,
                MedicalLabForms = medicalLabForms
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
            var hospitalBills = context.FillupformHospitalBill
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.Medicalandlabform
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var funeralburialform = context.Funeralburialform
             .Where(f => f.UserId == userId)
             .OrderByDescending(f => f.CreatedAt)
             .ToList();

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalBills = hospitalBills,
                MedicalLabForms = medicalLabForms,
                Funeralburialform = funeralburialform //
            };

            // Pass the view model to the view
            return View(viewModel);
        }


        public IActionResult Fillupformhospitalbillview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }


            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);
            if (fillupformhospitalBill == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = fillupformhospitalBill.Status;
            ViewData["Id"] = fillupformhospitalBill.Id;
            ViewData["Lastname"] = fillupformhospitalBill.Lastname;
            ViewData["Firstname"] = fillupformhospitalBill.Firstname;
            ViewData["Middlename"] = fillupformhospitalBill.Middlename;
            ViewData["Suffix"] = fillupformhospitalBill.Suffix;
            ViewData["BlkLotStreet"] = fillupformhospitalBill.BlkLotStreet;
            ViewData["SubVill"] = fillupformhospitalBill.SubVill;
            ViewData["Brgy"] = fillupformhospitalBill.Brgy;
            ViewData["District"] = fillupformhospitalBill.District;
            ViewData["Sex"] = fillupformhospitalBill.Sex;
            ViewData["PhilHealth"] = fillupformhospitalBill.PhilHealth;
            ViewData["PhilHealthNo"] = fillupformhospitalBill.PhilHealthNo;
            ViewData["Dateofbirth"] = fillupformhospitalBill.Dateofbirth;
            ViewData["Age"] = fillupformhospitalBill.Age;

            // Requestor details
            ViewData["RLastname"] = fillupformhospitalBill.RLastname;
            ViewData["RFirstname"] = fillupformhospitalBill.RFirstname;
            ViewData["RMiddlename"] = fillupformhospitalBill.RMiddlename;
            ViewData["RSuffix"] = fillupformhospitalBill.RSuffix;
            ViewData["RBlkLotStreet"] = fillupformhospitalBill.RBlkLotStreet;
            ViewData["RSubVill"] = fillupformhospitalBill.RSubVill;
            ViewData["RBrgy"] = fillupformhospitalBill.RBrgy;
            ViewData["RDistrict"] = fillupformhospitalBill.RDistrict;
            ViewData["RelationshipPatient"] = fillupformhospitalBill.RelationshipPatient;
            ViewData["ContactNo"] = fillupformhospitalBill.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = fillupformhospitalBill.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = fillupformhospitalBill.ForCMOPERSONNEL ?? "";
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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(fillupformhospitalBill.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, fillupformhospitalBill.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(fillupformhospitalBill.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, fillupformhospitalBill.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED
                if (!string.IsNullOrEmpty(fillupformhospitalBill.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, fillupformhospitalBill.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {fillupformhospitalBill.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"✅ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"🔍 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Doctor Prescription in database");
                }

                // ⭐ DEATH CERTIFICATE - UPDATED
                if (!string.IsNullOrEmpty(fillupformhospitalBill.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, fillupformhospitalBill.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {fillupformhospitalBill.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"✅ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"🔍 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"❌ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = fillupformhospitalBill.Validfrontimage;
            ViewData["ValidBackimage"] = fillupformhospitalBill.ValidBackimage;
            ViewData["Comments"] = fillupformhospitalBill.Comments;

            return View();
        }

        public IActionResult Funeralburialformview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }


            var funeralburialform = context.Funeralburialform.Find(id);
            if (funeralburialform == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status2"] = funeralburialform.Status2;
            ViewData["Id"] = funeralburialform.Id;
            ViewData["Lastname"] = funeralburialform.Lastname;
            ViewData["Firstname"] = funeralburialform.Firstname;
            ViewData["Middlename"] = funeralburialform.Middlename;
            ViewData["Suffix"] = funeralburialform.Suffix;
            ViewData["BlkLotStreet"] = funeralburialform.BlkLotStreet;
            ViewData["SubVill"] = funeralburialform.SubVill;
            ViewData["Brgy"] = funeralburialform.Brgy;
            ViewData["District"] = funeralburialform.District;
            ViewData["Sex"] = funeralburialform.Sex;
            ViewData["PhilHealth"] = funeralburialform.PhilHealth;
            ViewData["PhilHealthNo"] = funeralburialform.PhilHealthNo;
            ViewData["Dateofbirth"] = funeralburialform.Dateofbirth;
            ViewData["Age"] = funeralburialform.Age;

            // Requestor details
            ViewData["RLastname"] = funeralburialform.RLastname;
            ViewData["RFirstname"] = funeralburialform.RFirstname;
            ViewData["RMiddlename"] = funeralburialform.RMiddlename;
            ViewData["RSuffix"] = funeralburialform.RSuffix;
            ViewData["RBlkLotStreet"] = funeralburialform.RBlkLotStreet;
            ViewData["RSubVill"] = funeralburialform.RSubVill;
            ViewData["RBrgy"] = funeralburialform.RBrgy;
            ViewData["RDistrict"] = funeralburialform.RDistrict;
            ViewData["RelationshipPatient"] = funeralburialform.RelationshipPatient;
            ViewData["ContactNo"] = funeralburialform.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = funeralburialform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = funeralburialform.ForCMOPERSONNEL ?? "";
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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(funeralburialform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, funeralburialform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(funeralburialform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, funeralburialform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, funeralburialform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {funeralburialform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"✅ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"🔍 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Doctor Prescription in database");
                }

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, funeralburialform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {funeralburialform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"✅ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"🔍 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"❌ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
            ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;
            ViewData["Comments"] = funeralburialform.Comments;

            return View();
        }

        public IActionResult Medicalandlabformview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.Medicalandlabform.Find(id);
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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "MedCertificateimage");

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
                        debugMessages.Add("✅ Front ID decrypted");
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
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

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

                            debugMessages.Add($"✅ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"🔍 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Doctor Prescription in database");
                }

                // ⭐ MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"📄 Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"📂 Full path: {medicalPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(medicalPath)}");

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

                            debugMessages.Add($"✅ Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"🔍 IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Medical Certificate in database");
                }

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

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

                            debugMessages.Add($"✅ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"🔍 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Death Certificate in database");
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

            return View();
        }


        // UPDATED ViewPDF METHOD
        [HttpGet]
        public IActionResult ViewPDF(string fileName, string fileType)
        {
            try
            {
                // Authentication check
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
                {
                    return Unauthorized("Please log in to view documents");
                }

                Console.WriteLine($"🔍 ViewPDF called - FileName: {fileName}, FileType: {fileType}");

                // Validate inputs
                if (string.IsNullOrEmpty(fileName))
                {
                    Console.WriteLine("❌ FileName is null or empty");
                    return BadRequest("FileName is required");
                }

                if (string.IsNullOrEmpty(fileType))
                {
                    Console.WriteLine("❌ FileType is null or empty");
                    return BadRequest("FileType is required");
                }

                // Security: Prevent directory traversal
                string safeFileName = Path.GetFileName(fileName);

                // Define the directory based on file type
                string folderPath = fileType.ToLower() switch
                {
                    "doctorprescription" => Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage"),
                    "deathcertificate" => Path.Combine(environment.WebRootPath, "Funeralimg"),
                    "medicalcertificate" => Path.Combine(environment.WebRootPath, "MedCertificateimage"),
                    _ => Path.Combine(environment.WebRootPath, "Validimg")
                };

                Console.WriteLine($"📁 Folder path: {folderPath}");

                string encryptedFilePath = Path.Combine(folderPath, safeFileName);
                Console.WriteLine($"📄 Full file path: {encryptedFilePath}");

                // Additional security: Verify the resolved path is within the expected directory
                string resolvedPath = Path.GetFullPath(encryptedFilePath);
                string resolvedFolder = Path.GetFullPath(folderPath);
                if (!resolvedPath.StartsWith(resolvedFolder))
                {
                    Console.WriteLine("❌ Security: Path traversal attempt detected");
                    return BadRequest("Invalid file path");
                }

                // Check if file exists
                if (!System.IO.File.Exists(encryptedFilePath))
                {
                    Console.WriteLine($"❌ File does not exist: {encryptedFilePath}");
                    return NotFound($"File not found: {fileName}");
                }

                Console.WriteLine($"✅ File exists. Size: {new FileInfo(encryptedFilePath).Length} bytes");

                // Decrypt the file USING CONFIGURATION-BASED KEY
                byte[] decryptedBytes = DecryptFile(encryptedFilePath);
                Console.WriteLine($"✅ File decrypted. Decrypted size: {decryptedBytes.Length} bytes");

                // Verify it's actually a PDF
                bool isPdf = IsPdfFile(decryptedBytes);
                Console.WriteLine($"📊 Is PDF: {isPdf}");

                if (!isPdf)
                {
                    Console.WriteLine("❌ File is not a valid PDF");
                    return BadRequest("Only PDF files can be viewed");
                }

                // ⭐ CRITICAL: Set headers to FORCE inline viewing and PREVENT download
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

                Console.WriteLine($"📤 Returning PDF for INLINE VIEWING ONLY (download disabled)");

                // Return as PDF without any filename parameter
                return File(decryptedBytes, "application/pdf");
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"💥 DECRYPTION ERROR: {ex.Message}");
                return BadRequest("Failed to decrypt file. Invalid encryption or corrupted file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ERROR in ViewPDF: {ex.Message}");
                Console.WriteLine($"💥 Stack Trace: {ex.StackTrace}");
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

            string folder;

            switch (fileType.ToLower())
            {
                case "doctorprescription":
                    folder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    break;
                case "deathcertificate":
                    folder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    break;
                case "medicalcertificate":
                    folder = Path.Combine(environment.WebRootPath, "MedCertificateimage");
                    break;
                default:
                    folder = Path.Combine(environment.WebRootPath, "Validimg");
                    break;
            }

            string safeFileName = Path.GetFileName(fileName);
            string filePath = Path.Combine(folder, safeFileName);

            if (!System.IO.File.Exists(filePath))
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





    }
}
