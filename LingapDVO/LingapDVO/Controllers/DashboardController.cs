using iText.Commons.Actions.Data;
using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

        public Dashboard(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
        }

        // ╔═══════════════════════════════════════════════════════════════════════════╗
        // ║                    AES-256 ENCRYPTION HELPER CLASS                        ║
        // ║         Hardcoded AES-256 Implementation with Detailed Comments           ║
        // ╚═══════════════════════════════════════════════════════════════════════════╝
        private static class AesEncryptionHelper
        {
            // ┌─────────────────────────────────────────────────────────────────────┐
            // │ STEP 1: Define hardcoded 256-bit (32 bytes) encryption key          │
            // │ This is a fixed key for AES-256 encryption                          │
            // │ In production, store this securely (e.g., Azure Key Vault)          │
            // └─────────────────────────────────────────────────────────────────────┘
            private static readonly byte[] HARDCODED_AES_KEY = new byte[32]
            {
                0x2B, 0x7E, 0x15, 0x16, 0x28, 0xAE, 0xD2, 0xA6,
                0xAB, 0xF7, 0x15, 0x88, 0x09, 0xCF, 0x4F, 0x3C,
                0x76, 0x2E, 0x71, 0x60, 0xF3, 0x8B, 0x4D, 0xA5,
                0x6A, 0x78, 0x4D, 0x90, 0x45, 0x19, 0x03, 0xCB
            };

            // ┌─────────────────────────────────────────────────────────────────────┐
            // │ STEP 2: Encrypt string data using AES-256-CBC                       │
            // │ Process:                                                             │
            // │   1. Generate random 16-byte IV (Initialization Vector)             │
            // │   2. Create AES cipher with 256-bit key                             │
            // │   3. Configure CBC mode with PKCS7 padding                          │
            // │   4. Encrypt the plaintext data                                     │
            // │   5. Combine IV + encrypted data                                    │
            // │   6. Return Base64-encoded result                                   │
            // └─────────────────────────────────────────────────────────────────────┘
            public static string Encrypt(string plainText)
            {
                // Step 2.1: Convert plaintext string to bytes
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                // Step 2.2: Create AES algorithm instance
                using var aes = Aes.Create();

                // Step 2.3: Set the hardcoded 256-bit key (32 bytes)
                aes.Key = HARDCODED_AES_KEY;

                // Step 2.4: Generate random 128-bit IV (16 bytes) for each encryption
                // This ensures same plaintext produces different ciphertext each time
                aes.GenerateIV();

                // Step 2.5: Set cipher mode to CBC (Cipher Block Chaining)
                aes.Mode = CipherMode.CBC;

                // Step 2.6: Set padding mode to PKCS7 (standard padding for AES)
                aes.Padding = PaddingMode.PKCS7;

                // Step 2.7: Perform the encryption operation
                using var encryptor = aes.CreateEncryptor();
                using var memoryStream = new MemoryStream();

                // Step 2.8: Write IV at the beginning (needed for decryption)
                memoryStream.Write(aes.IV, 0, aes.IV.Length);

                // Step 2.9: Create crypto stream for encryption
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    // Step 2.10: Write plaintext to crypto stream (encrypts automatically)
                    writer.Write(plainText);
                }

                // Step 2.11: Get encrypted data as byte array (IV + encrypted data)
                byte[] encryptedData = memoryStream.ToArray();

                // Step 2.12: Convert to Base64 for safe storage/transmission
                return Convert.ToBase64String(encryptedData);
            }

            // ┌─────────────────────────────────────────────────────────────────────┐
            // │ STEP 3: Decrypt string data using AES-256-CBC                       │
            // │ Process:                                                             │
            // │   1. Decode Base64 string to bytes                                  │
            // │   2. Extract IV from first 16 bytes                                 │
            // │   3. Extract encrypted data from remaining bytes                    │
            // │   4. Create AES cipher with same key                                │
            // │   5. Configure CBC mode with PKCS7 padding                          │
            // │   6. Decrypt the data                                               │
            // │   7. Return plaintext string                                        │
            // └─────────────────────────────────────────────────────────────────────┘
            public static string Decrypt(string encryptedText)
            {
                // Step 3.1: Convert Base64 string back to bytes
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                // Step 3.2: Create AES algorithm instance
                using var aes = Aes.Create();

                // Step 3.3: Set the same hardcoded 256-bit key (32 bytes)
                aes.Key = HARDCODED_AES_KEY;

                // Step 3.4: Extract IV from first 16 bytes of encrypted data
                byte[] iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.IV = iv;

                // Step 3.5: Set cipher mode to CBC (same as encryption)
                aes.Mode = CipherMode.CBC;

                // Step 3.6: Set padding mode to PKCS7 (same as encryption)
                aes.Padding = PaddingMode.PKCS7;

                // Step 3.7: Perform the decryption operation
                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(encryptedBytes, 16, encryptedBytes.Length - 16);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);

                // Step 3.8: Read decrypted plaintext from stream
                return reader.ReadToEnd();
            }

            // ┌─────────────────────────────────────────────────────────────────────┐
            // │ STEP 4: Encrypt file/stream data using AES-256-CBC                  │
            // │ Process:                                                             │
            // │   1. Generate random 16-byte IV                                     │
            // │   2. Create AES cipher with 256-bit key                             │
            // │   3. Configure CBC mode with PKCS7 padding                          │
            // │   4. Encrypt the file stream                                        │
            // │   5. Return IV + encrypted data as byte array                       │
            // └─────────────────────────────────────────────────────────────────────┘
            public static byte[] EncryptStream(Stream inputStream)
            {
                // Step 4.1: Create AES algorithm instance
                using var aes = Aes.Create();

                // Step 4.2: Set the hardcoded 256-bit key (32 bytes)
                aes.Key = HARDCODED_AES_KEY;

                // Step 4.3: Generate random 128-bit IV (16 bytes)
                aes.GenerateIV();

                // Step 4.4: Set cipher mode to CBC
                aes.Mode = CipherMode.CBC;

                // Step 4.5: Set padding mode to PKCS7
                aes.Padding = PaddingMode.PKCS7;

                // Step 4.6: Create output stream to hold IV + encrypted data
                using var memoryStream = new MemoryStream();

                // Step 4.7: Write IV at the beginning (16 bytes)
                memoryStream.Write(aes.IV, 0, aes.IV.Length);

                // Step 4.8: Create encryptor and encrypt the input stream
                using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    // Step 4.9: Copy input stream to crypto stream (encrypts automatically)
                    inputStream.CopyTo(cryptoStream);
                }

                // Step 4.10: Return complete encrypted data (IV + ciphertext)
                return memoryStream.ToArray();
            }

            // ┌─────────────────────────────────────────────────────────────────────┐
            // │ STEP 5: Encrypt timestamp string using AES-256-CBC                  │
            // │ Used for generating unique encrypted filenames                      │
            // │ Returns only the ciphertext (without IV) as Base64                  │
            // └─────────────────────────────────────────────────────────────────────┘
            public static string EncryptTimestamp(string timestamp)
            {
                // Step 5.1: Create AES algorithm instance
                using var aes = Aes.Create();

                // Step 5.2: Set the hardcoded 256-bit key
                aes.Key = HARDCODED_AES_KEY;

                // Step 5.3: Generate random IV
                aes.GenerateIV();

                // Step 5.4: Set cipher mode to CBC
                aes.Mode = CipherMode.CBC;

                // Step 5.5: Set padding mode to PKCS7
                aes.Padding = PaddingMode.PKCS7;

                // Step 5.6: Create encryptor
                using var encryptor = aes.CreateEncryptor();

                // Step 5.7: Convert timestamp to bytes
                byte[] inputBytes = Encoding.UTF8.GetBytes(timestamp);

                // Step 5.8: Encrypt timestamp bytes
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

                // Step 5.9: Return Base64-encoded encrypted timestamp
                return Convert.ToBase64String(encryptedBytes);
            }
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

        public IActionResult FillupformHospitalBill()
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
                // Generate unique encrypted timestamp for filenames
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = AesEncryptionHelper.EncryptTimestamp(timestamp);
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
                        // Use hardcoded AES-256 helper to encrypt file stream
                        byte[] encryptedData = AesEncryptionHelper.EncryptStream(fillupformHospitalbilldto.DoctorPrescriptionimage.OpenReadStream());
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
                        // Use hardcoded AES-256 helper to encrypt file stream
                        byte[] encryptedData = AesEncryptionHelper.EncryptStream(fillupformHospitalbilldto.DeathCertificateimage.OpenReadStream());
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
            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);

          
            if (fillupformhospitalBill == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
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

            // Type of assistance and CMO details
            var typeAssistanceRaw = fillupformhospitalBill.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw = fillupformhospitalBill.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] = fillupformhospitalBill.Validfrontimage;
            ViewData["ValidBackimage"] = fillupformhospitalBill.ValidBackimage;

            ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;
            ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;


            return View();

        }

        [HttpPost]
        public IActionResult FillupformHospitalBilledit(int id, FillupformHospitalBillDto fillupformHospitalbilldto)
        {
            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);

            if (fillupformhospitalBill == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            if (string.IsNullOrEmpty(fillupformHospitalbilldto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // Remove validation requirements for images if they're not provided
            if (fillupformHospitalbilldto.IdFrontimage == null) ModelState.Remove("IdFrontimage");
            if (fillupformHospitalbilldto.IdBackimage == null) ModelState.Remove("IdBackimage");
            if (fillupformHospitalbilldto.DoctorPrescriptionimage == null) ModelState.Remove("DoctorPrescriptionimage");
            if (fillupformHospitalbilldto.DeathCertificateimage == null) ModelState.Remove("DeathCertificateimage");

            if (!ModelState.IsValid)
            {
                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = fillupformhospitalBill.Validfrontimage;
                ViewData["ValidBackimage"] = fillupformhospitalBill.ValidBackimage;
                ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;
                ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                return View(fillupformHospitalbilldto);
            }

            try
            {
                // Update text properties
                fillupformhospitalBill.Lastname = fillupformHospitalbilldto.Lastname ?? fillupformhospitalBill.Lastname;
                fillupformhospitalBill.Firstname = fillupformHospitalbilldto.Firstname ?? fillupformhospitalBill.Firstname;
                fillupformhospitalBill.Middlename = fillupformHospitalbilldto.Middlename ?? fillupformhospitalBill.Middlename;
                fillupformhospitalBill.Suffix = fillupformHospitalbilldto.Suffix ?? fillupformhospitalBill.Suffix;
                fillupformhospitalBill.BlkLotStreet = fillupformHospitalbilldto.BlkLotStreet ?? fillupformhospitalBill.BlkLotStreet;
                fillupformhospitalBill.SubVill = fillupformHospitalbilldto.SubVill ?? fillupformhospitalBill.SubVill;
                fillupformhospitalBill.Brgy = fillupformHospitalbilldto.Brgy ?? fillupformhospitalBill.Brgy;
                fillupformhospitalBill.District = fillupformHospitalbilldto.District ?? fillupformhospitalBill.District;
                fillupformhospitalBill.Sex = fillupformHospitalbilldto.Sex ?? fillupformhospitalBill.Sex;
                fillupformhospitalBill.PhilHealth = fillupformHospitalbilldto.PhilHealth ?? fillupformhospitalBill.PhilHealth;
                fillupformhospitalBill.PhilHealthNo = fillupformHospitalbilldto.PhilHealthNo;
                fillupformhospitalBill.Dateofbirth = fillupformHospitalbilldto.Dateofbirth ?? fillupformhospitalBill.Dateofbirth;
                fillupformhospitalBill.Age = fillupformHospitalbilldto.Age ?? fillupformhospitalBill.Age;

                // Requestor Details
                fillupformhospitalBill.RLastname = fillupformHospitalbilldto.RLastname;
                fillupformhospitalBill.RFirstname = fillupformHospitalbilldto.RFirstname;
                fillupformhospitalBill.RMiddlename = fillupformHospitalbilldto.RMiddlename;
                fillupformhospitalBill.RSuffix = fillupformHospitalbilldto.RSuffix;
                fillupformhospitalBill.RBlkLotStreet = fillupformHospitalbilldto.RBlkLotStreet;
                fillupformhospitalBill.RSubVill = fillupformHospitalbilldto.RSubVill;
                fillupformhospitalBill.RBrgy = fillupformHospitalbilldto.RBrgy;
                fillupformhospitalBill.RDistrict = fillupformHospitalbilldto.RDistrict;
                fillupformhospitalBill.RelationshipPatient = fillupformHospitalbilldto.RelationshipPatient;
                fillupformhospitalBill.ContactNo = fillupformHospitalbilldto.ContactNo;

                // Assistance Type
                fillupformhospitalBill.Typeassistance = fillupformHospitalbilldto.Typeassistance ?? fillupformhospitalBill.Typeassistance;
                fillupformhospitalBill.ForCMOPERSONNEL = fillupformHospitalbilldto.ForCMOPERSONNEL;

                // Handle ID Front image
                if (fillupformHospitalbilldto.IdFrontimage != null)
                {
                    string newFileNameFront = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(fillupformHospitalbilldto.IdFrontimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameFront);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(fillupformhospitalBill.Validfrontimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, fillupformhospitalBill.Validfrontimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fillupformHospitalbilldto.IdFrontimage.CopyTo(stream);
                    }

                    fillupformhospitalBill.Validfrontimage = newFileNameFront;
                }

                // Handle ID Back image
                if (fillupformHospitalbilldto.IdBackimage != null)
                {
                    string newFileNameBack = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(fillupformHospitalbilldto.IdBackimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameBack);

                    if (!string.IsNullOrEmpty(fillupformhospitalBill.ValidBackimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, fillupformhospitalBill.ValidBackimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fillupformHospitalbilldto.IdBackimage.CopyTo(stream);
                    }

                    fillupformhospitalBill.ValidBackimage = newFileNameBack;
                }

                // Handle Doctor Prescription image
                if (fillupformHospitalbilldto.DoctorPrescriptionimage != null)
                {
                    string newFileNamePrescription = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(fillupformHospitalbilldto.DoctorPrescriptionimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    string filePath = Path.Combine(uploadsFolder, newFileNamePrescription);

                    if (!string.IsNullOrEmpty(fillupformhospitalBill.DoctorPrescription))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, fillupformhospitalBill.DoctorPrescription);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fillupformHospitalbilldto.DoctorPrescriptionimage.CopyTo(stream);
                    }

                    fillupformhospitalBill.DoctorPrescription = newFileNamePrescription;
                }

                // Handle Death Certificate image
                if (fillupformHospitalbilldto.DeathCertificateimage != null)
                {
                    string newFileNameDeathCertificate = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(fillupformHospitalbilldto.DeathCertificateimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameDeathCertificate);

                    if (!string.IsNullOrEmpty(fillupformhospitalBill.DeathCertificate))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, fillupformhospitalBill.DeathCertificate);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fillupformHospitalbilldto.DeathCertificateimage.CopyTo(stream);
                    }

                    fillupformhospitalBill.DeathCertificate = newFileNameDeathCertificate;
                }

                context.SaveChanges();
                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);

                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = fillupformhospitalBill.Validfrontimage;
                ViewData["ValidBackimage"] = fillupformhospitalBill.ValidBackimage;
                ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;
                ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                return View(fillupformHospitalbilldto);
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

        [HttpPost]
        public IActionResult Medicalandlabform(MedicalandlabformDto medicalandlabformdto)
        {
            // Get the current user's ID from the session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                // If user is not logged in, redirect to login page
                return RedirectToAction("Login", "Login");
            }

            // Get the user's ID filenames from session (if available)
            string userFrontID = HttpContext.Session.GetString("FrontID") ?? "";
            string userBackID = HttpContext.Session.GetString("BackID") ?? "";

            // FIRST: Check for recently approved forms (1-month cooldown)
            var oneMonthAgo = DateTime.Now.AddMonths(-1);

            var hasRecentApproval = context.Medicalandlabform.Any(f => f.UserId == userId && f.Status == "Approved" && f.CreatedAt >= oneMonthAgo);


            if (hasRecentApproval)
            {
                ModelState.AddModelError("", "You cannot submit a new form because you have an approved form within the last month. Please wait until one month has passed since your last approval.");
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

            // Optional field handling
            if (string.IsNullOrEmpty(medicalandlabformdto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // MODIFIED: Image validation - Make all image fields optional but require at least one document
            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");
            ModelState.Remove("DoctorPrescriptionimage");
            ModelState.Remove("DeathCertificateimage");
            ModelState.Remove("MedCertificateimage"); // Added Medical Certificate as optional

            // NEW VALIDATION: Check if user has existing ID images in session, otherwise require ID upload
            bool hasExistingIDs = !string.IsNullOrEmpty(userFrontID) && !string.IsNullOrEmpty(userBackID);

            if (!hasExistingIDs && (medicalandlabformdto.IdFrontimage == null || medicalandlabformdto.IdBackimage == null))
            {
                ModelState.AddModelError("IdFrontimage", "ID images are required when no existing IDs are found in your account.");
            }

            // NEW VALIDATION: At least one of the medical documents must be provided
            if (medicalandlabformdto.DoctorPrescriptionimage == null &&
                medicalandlabformdto.DeathCertificateimage == null &&
                medicalandlabformdto.MedCertificateimage == null)
            {
                ModelState.AddModelError("", "At least one medical document (Doctor Prescription, Death Certificate, or Medical Certificate) is required");
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
                // Generate unique encrypted timestamp for filenames
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = AesEncryptionHelper.EncryptTimestamp(timestamp);
                string safeEncryptedTimestamp = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // Generate encrypted filenames
                string? newFileNameFront = null;
                string? newFileNameBack = null;
                string? newFileNamePrescription = null;
                string? newFileNameDeathCertificate = null;
                string? newFileNameMedCertificate = null; // Added for Medical Certificate

                string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "Funeralimg");
                string uploadsFolder3 = Path.Combine(environment.WebRootPath, "MedCertificateimage"); // Added folder for Medical Certificate

                // Ensure directories exist
                Directory.CreateDirectory(uploadsFolder);
                Directory.CreateDirectory(uploadsFolder1);
                Directory.CreateDirectory(uploadsFolder2);
                Directory.CreateDirectory(uploadsFolder3); // Ensure Medical Certificate directory exists

                // Encrypt and Save Front ID Image if provided and no existing ID
                if (!hasExistingIDs && medicalandlabformdto.IdFrontimage != null)
                {
                    newFileNameFront = safeEncryptedTimestamp + "_frontid.enc";
                    string filePathFront = Path.Combine(uploadsFolder, newFileNameFront);
                    using (var fileStream = new FileStream(filePathFront, FileMode.Create))
                    {
                        byte[] encryptedData = AesEncryptionHelper.EncryptStream(medicalandlabformdto.IdFrontimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Back ID Image if provided and no existing ID
                if (!hasExistingIDs && medicalandlabformdto.IdBackimage != null)
                {
                    newFileNameBack = safeEncryptedTimestamp + "_backid.enc";
                    string filePathBack = Path.Combine(uploadsFolder, newFileNameBack);
                    using (var fileStream = new FileStream(filePathBack, FileMode.Create))
                    {
                        byte[] encryptedData = AesEncryptionHelper.EncryptStream(medicalandlabformdto.IdBackimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // Encrypt and Save Prescription Image if provided
                if (medicalandlabformdto.DoctorPrescriptionimage != null)
                {
                    newFileNamePrescription = safeEncryptedTimestamp + "_prescription.enc";
                    string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                    using (var fileStream = new FileStream(filePathPrescription, FileMode.Create))
                    {
                        byte[] encryptedData = AesEncryptionHelper.EncryptStream(medicalandlabformdto.DoctorPrescriptionimage.OpenReadStream());
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
                        byte[] encryptedData = AesEncryptionHelper.EncryptStream(medicalandlabformdto.DeathCertificateimage.OpenReadStream());
                        fileStream.Write(encryptedData, 0, encryptedData.Length);
                    }
                }

                // NEW: Encrypt and Save Medical Certificate Image if provided
                if (medicalandlabformdto.MedCertificateimage != null)
                {
                    newFileNameMedCertificate = safeEncryptedTimestamp + "_medcert.enc";
                    string filePathMedCertificate = Path.Combine(uploadsFolder3, newFileNameMedCertificate);
                    using (var fileStream = new FileStream(filePathMedCertificate, FileMode.Create))
                    {
                        byte[] encryptedData = AesEncryptionHelper.EncryptStream(medicalandlabformdto.MedCertificateimage.OpenReadStream());
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

                    // MODIFIED: Use existing ID images from user account if available, otherwise use new uploads
                    Validfrontimage = hasExistingIDs ? userFrontID : newFileNameFront ?? string.Empty,
                    ValidBackimage = hasExistingIDs ? userBackID : newFileNameBack ?? string.Empty,
                    DoctorPrescription = newFileNamePrescription ?? string.Empty,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    MedCertificate = newFileNameMedCertificate ?? string.Empty, // Added Medical Certificate
                    Status = "Pending",

                    // Created Timestamp
                    CreatedAt = DateTime.Now
                };

                context.Medicalandlabform.Add(medicalandlabform);
                context.SaveChanges();

                ViewBag.Success = true;


                return RedirectToAction("Homepage", "Dashboard");
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

            // Check for forms with Status = "Approved" within the last month across all form types
            var hasRecentApproval = context.Funeralburialform
                .Any(f => f.UserId == userId && f.Status2 == "Approved" && f.CreatedAt >= oneMonthAgo);

            if (hasRecentApproval)
            {
                // Get the most recent approved form to show the exact date
                var recentApprovedForm = context.Funeralburialform
                    .Where(f => f.UserId == userId && f.Status2 == "Approved")
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

            // Optional field handling
            if (string.IsNullOrEmpty(funeralburialformdto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // MODIFIED: Image validation - Remove ID image validation and make both prescription and death certificate optional
            ModelState.Remove("IdFrontimage");
            ModelState.Remove("IdBackimage");
            ModelState.Remove("DoctorPrescriptionimage"); // Make doctor prescription optional
            ModelState.Remove("DeathCertificateimage");   // Death certificate is already optional

            // NEW VALIDATION: At least one of the documents must be provided
            if (funeralburialformdto.DoctorPrescriptionimage == null && funeralburialformdto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("", "At least one document (Doctor Prescription or Death Certificate) is required");
                return View(funeralburialformdto);
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
                // Generate unique encrypted timestamp for filenames
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = AesEncryptionHelper.EncryptTimestamp(timestamp);
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
                        byte[] encryptedData = AesEncryptionHelper.EncryptStream(funeralburialformdto.DoctorPrescriptionimage.OpenReadStream());
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
                        byte[] encryptedData = AesEncryptionHelper.EncryptStream(funeralburialformdto.DeathCertificateimage.OpenReadStream());
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
                ViewBag.Success = true;

                return RedirectToAction("Homepage", "Dashboard");
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

            // Add all form field values to ViewData
            ViewData["Status"] = funeralburialform.Status;
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
            ViewData["Comments"] = funeralburialform.Comments;
            ViewData["Processby"] = funeralburialform.Processby;
            return View();
        }

        public IActionResult Medicalandlabformview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicalandlabform = context.Medicalandlabform.Find(id);

            if (medicalandlabform == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Status"] = medicalandlabform.Status;
            ViewData["Id"] = medicalandlabform.Id;
            ViewData["Lastname"] = medicalandlabform.Lastname;
            ViewData["Firstname"] = medicalandlabform.Firstname;
            ViewData["Middlename"] = medicalandlabform.Middlename;
            ViewData["Suffix"] = medicalandlabform.Suffix;
            ViewData["BlkLotStreet"] = medicalandlabform.BlkLotStreet;
            ViewData["SubVill"] = medicalandlabform.SubVill;
            ViewData["Brgy"] = medicalandlabform.Brgy;
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
            ViewData["Comments"] = medicalandlabform.Comments;
            ViewData["Processby"] = medicalandlabform.Processby;
            return View();
        }

   

    }
}
