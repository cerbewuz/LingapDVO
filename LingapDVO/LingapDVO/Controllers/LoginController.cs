using BCrypt.Net;
using iText.Commons.Actions.Contexts;
using iText.Commons.Actions.Data;
using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace LingapDVO.Controllers
{
    public class LoginController : Controller
    {
        public readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly SmsService _smsService;
        private readonly IConfiguration _configuration;

        public LoginController(ApplicationDbContext context, IWebHostEnvironment environment, SmsService smsService, IConfiguration configuration)
        {
            this.context = context;
            this.environment = environment;
            _smsService = smsService;
            _configuration = configuration;
        }

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

                // Log key info for debugging
                System.Diagnostics.Debug.WriteLine($"AES Key Length: {keyHex.Length} characters");

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
                    System.Diagnostics.Debug.WriteLine($"Hex string padded from {hex.Length - 1} to {hex.Length} characters");
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
            public string Encrypt(string plainText)
            {
                // Step 2.1: Convert plaintext string to bytes
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                // Step 2.2: Create AES algorithm instance
                using var aes = Aes.Create();

                // Step 2.3: Set the 256-bit key from configuration (32 bytes)
                aes.Key = _aesKey;

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
            public string Decrypt(string encryptedText)
            {
                // Step 3.1: Convert Base64 string back to bytes
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                // Step 3.2: Create AES algorithm instance
                using var aes = Aes.Create();

                // Step 3.3: Set the same 256-bit key from configuration (32 bytes)
                aes.Key = _aesKey;

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
            public byte[] EncryptStream(Stream inputStream)
            {
                // Step 4.1: Create AES algorithm instance
                using var aes = Aes.Create();

                // Step 4.2: Set the 256-bit key from configuration (32 bytes)
                aes.Key = _aesKey;

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
            public string EncryptTimestamp(string timestamp)
            {
                // Step 5.1: Create AES algorithm instance
                using var aes = Aes.Create();

                // Step 5.2: Set the 256-bit key from configuration
                aes.Key = _aesKey;

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
            // Add this method to your controller class (before the Action methods)
            private string NormalizePhilippineName(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return "";

                string normalized = name.ToLower().Trim();

                // Remove periods and extra spaces
                normalized = normalized.Replace(".", "").Replace("  ", " ").Trim();

                // Normalize Filipino compound name particles
                var normalizationMap = new Dictionary<string, string>
    {
        { "dela cruz", "delacruz" },
        { "de la cruz", "delacruz" },
        { "delos santos", "delossantos" },
        { "de los santos", "delossantos" },
        { "delos reyes", "delosreyes" },
        { "de los reyes", "delosreyes" },
        { "dela rosa", "delarosa" },
        { "de la rosa", "delarosa" },
        { "dela paz", "delapaz" },
        { "de la paz", "delapaz" },
        { "del rosario", "delrosario" },
        { "de guzman", "deguzman" },
        { "san jose", "sanjose" },
        { "san juan", "sanjuan" },
        { "santa maria", "santamaria" },
        { "santa cruz", "santacruz" }
    };

                // Apply normalization
                foreach (var mapping in normalizationMap)
                {
                    normalized = normalized.Replace(mapping.Key, mapping.Value);
                }

                return normalized;
            }
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult FacebookLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("FacebookCallback", "Login")
            };
            return Challenge(properties, FacebookDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> FacebookCallback()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                var error = result.Properties?.GetString(".error");
                var errorDescription = result.Properties?.GetString(".error.description");
                return Redirect("/Login");

            }

            var claims = result.Principal.Claims.ToList();
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Your Facebook account did not provide an email address.";
                return RedirectToAction("Login", "Login");
            }

            // ✅ Check if user already exists
            var existingUser = context.RegisterAcc.FirstOrDefault(u => u.Email == email);
            RegisterAcc user;

            if (existingUser == null)
            {
                // ===========================
                // 🔑 Create auto Facebook user with AES-256 encryption
                // ===========================

                // ✅ Generate random password (internal use only)
                string generatedPassword = "FB-" + Guid.NewGuid().ToString("N").Substring(0, 12);

                // Use AES-256 helper from configuration to encrypt password
                var aesHelper = new AesEncryptionHelper(_configuration);
                string encryptedPassword = aesHelper.Encrypt(generatedPassword);

                user = new RegisterAcc
                {
                    FirstName = name?.Split(' ').FirstOrDefault() ?? "Facebook",
                    MiddleName = "",
                    LastName = name?.Split(' ').LastOrDefault() ?? "User",
                    Suffix = "",
                    Email = email,
                    Username = name ?? "FacebookUser",
                    Password = encryptedPassword,
                    Status = "Active"
                };

                context.RegisterAcc.Add(user);
                context.SaveChanges();
            }
            else
            {
                user = existingUser;
            }

            // ✅ Store user ID in session
            HttpContext.Session.SetString("UserId", user.Id.ToString());

            // ===========================
            // 🔍 Check if verified
            // ===========================
            var verifiedUser = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);

            if (verifiedUser != null)
            {
                // ✅ Store full verified session
                HttpContext.Session.SetString("IDtype", verifiedUser.IDtype ?? "");
                HttpContext.Session.SetString("IDnumber", verifiedUser.IDnumber ?? "");
                HttpContext.Session.SetString("Firstname", verifiedUser.Firstname ?? "");
                HttpContext.Session.SetString("Middlename", verifiedUser.Middlename ?? "");
                HttpContext.Session.SetString("Lastname", verifiedUser.Lastname ?? "");
                HttpContext.Session.SetString("Gender", verifiedUser.Gender ?? "");
                HttpContext.Session.SetString("Suffix", verifiedUser.Suffix ?? "");
                HttpContext.Session.SetString("Dateofbirth", verifiedUser.Dateofbirth ?? "");
                HttpContext.Session.SetString("BlkLotStreet", verifiedUser.BlkLotStreet ?? "");
                HttpContext.Session.SetString("SubVill", verifiedUser.SubVill ?? "");
                HttpContext.Session.SetString("District", verifiedUser.District ?? "");
                HttpContext.Session.SetString("Barangay", verifiedUser.Barangay ?? "");
                HttpContext.Session.SetString("CivilStatus", verifiedUser.CivilStatus ?? "");
                HttpContext.Session.SetString("FrontID", verifiedUser.FrontID ?? "");
                HttpContext.Session.SetString("BackID", verifiedUser.BackID ?? "");
                HttpContext.Session.SetString("IsVerifiedUser", "true");
                HttpContext.Session.SetString("IsRegisteredUser", "true");
            }
            else
            {
                // ⚙️ Not verified yet — store basic session info
                HttpContext.Session.SetString("Email", user.Email ?? "");
                HttpContext.Session.SetString("Username", user.Username ?? "");
                HttpContext.Session.SetString("IsRegisteredUser", "true");
                HttpContext.Session.SetString("IsVerifiedUser", "false");
            }

            // ✅ Always store Facebook metadata
            HttpContext.Session.SetString("FacebookEmail", email ?? "");
            HttpContext.Session.SetString("FacebookName", name ?? "");
            HttpContext.Session.SetString("Username", name ?? "Facebook User");

            // ===========================
            // 🧭 Redirect Logic
            // ===========================
            if (verifiedUser == null)
            {
                // First time Facebook login or unverified user
                return RedirectToAction("Accountverification", "Login");
            }
            else
            {
                // Already verified — straight to homepage
                return RedirectToAction("Homepage", "Dashboard");
            }
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("Homepage", "Dashboard") 
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [Route("/Auth/GoogleCallback")]
        public async Task<IActionResult> GoogleCallback()
        {
            // Authenticate using cookie scheme (Google auth already completed)
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Google authentication failed.";
                return RedirectToAction("Login", "Login");
            }

            var claims = result.Principal?.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Google login failed. No email returned.";
                return RedirectToAction("Login", "Login");
            }

            // ===========================
            // 🔐 COMPREHENSIVE CREDENTIAL VALIDATION LOGIC
            // ===========================

            // ===========================
            // 📝 Parse Google Name (Flexible Handling)
            // ===========================
            string googleFullName = name ?? "";
            string[] nameParts = googleFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Handle different name formats from Google
            string googleFirstName = "Google";
            string googleLastName = "User";
            string googleMiddleName = "";

            if (nameParts.Length == 1)
            {
                // Only one name provided (e.g., "John")
                googleFirstName = nameParts[0];
            }
            else if (nameParts.Length == 2)
            {
                // First and Last name (e.g., "John Doe")
                googleFirstName = nameParts[0];
                googleLastName = nameParts[1];
            }
            else if (nameParts.Length >= 3)
            {
                // First, Middle(s), and Last name (e.g., "John Lee Doe")
                googleFirstName = nameParts[0];
                googleLastName = nameParts[nameParts.Length - 1];
                googleMiddleName = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
            }

            // ===========================
            // 🔍 Check for Existing Users
            // ===========================

            // Check for existing users with same email
            var userWithSameEmail = context.RegisterAcc.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

            // Check for existing users with matching name (FLEXIBLE MATCHING)
            // Match if First + Last names match, regardless of middle name
            // This handles cases where:
            // - Registered: "John Lee Cameron Doe" (First: John Lee, Middle: Cameron, Last: Doe)
            // - Google: "John Lee Doe" (First: John Lee, Last: Doe) ✅ MATCH
            var usersWithSameName = context.RegisterAcc
                .Where(u => u.FirstName.ToLower() == googleFirstName.ToLower() &&
                           u.LastName.ToLower() == googleLastName.ToLower())
                .ToList();

            // SCENARIO 1: Both email AND full name match - Allow login (existing user)
            if (userWithSameEmail != null &&
                usersWithSameName.Any(u => u.Id == userWithSameEmail.Id))
            {
                // ✅ Perfect match - existing user logging in
                var user = userWithSameEmail;

                // Store session data
                HttpContext.Session.SetString("UserId", user.Id.ToString());

                // ===========================
                // 🔍 Check if verified
                // ===========================
                var verifiedUser = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);

                if (verifiedUser != null)
                {
                    // ✅ Store full verified session
                    HttpContext.Session.SetString("IDtype", verifiedUser.IDtype ?? "");
                    HttpContext.Session.SetString("IDnumber", verifiedUser.IDnumber ?? "");
                    HttpContext.Session.SetString("Firstname", verifiedUser.Firstname ?? "");
                    HttpContext.Session.SetString("Middlename", verifiedUser.Middlename ?? "");
                    HttpContext.Session.SetString("Lastname", verifiedUser.Lastname ?? "");
                    HttpContext.Session.SetString("Gender", verifiedUser.Gender ?? "");
                    HttpContext.Session.SetString("Suffix", verifiedUser.Suffix ?? "");
                    HttpContext.Session.SetString("Dateofbirth", verifiedUser.Dateofbirth ?? "");
                    HttpContext.Session.SetString("BlkLotStreet", verifiedUser.BlkLotStreet ?? "");
                    HttpContext.Session.SetString("SubVill", verifiedUser.SubVill ?? "");
                    HttpContext.Session.SetString("District", verifiedUser.District ?? "");
                    HttpContext.Session.SetString("Barangay", verifiedUser.Barangay ?? "");
                    HttpContext.Session.SetString("CivilStatus", verifiedUser.CivilStatus ?? "");
                    HttpContext.Session.SetString("FrontID", verifiedUser.FrontID ?? "");
                    HttpContext.Session.SetString("BackID", verifiedUser.BackID ?? "");
                    HttpContext.Session.SetString("IsVerifiedUser", "true");
                    HttpContext.Session.SetString("IsRegisteredUser", "true");
                }
                else
                {
                    // ⚙️ Not verified yet — store basic session info
                    HttpContext.Session.SetString("Email", user.Email ?? "");
                    HttpContext.Session.SetString("Username", user.Username ?? "");
                    HttpContext.Session.SetString("IsRegisteredUser", "true");
                    HttpContext.Session.SetString("IsVerifiedUser", "false");
                }

                // ✅ Always store Google metadata
                HttpContext.Session.SetString("GoogleEmail", email);
                HttpContext.Session.SetString("GoogleName", googleFullName);
                HttpContext.Session.SetString("Username", user.Username ?? googleFullName);

                // Redirect based on verification status
                if (verifiedUser == null)
                {
                    return RedirectToAction("Accountverification", "Login");
                }
                else
                {
                    return RedirectToAction("Homepage", "Dashboard");
                }
            }

            // SCENARIO 2: Email exists but different name - Block with modal
            if (userWithSameEmail != null &&
                !usersWithSameName.Any(u => u.Id == userWithSameEmail.Id))
            {
                TempData["GoogleCredentialConflict"] = "email";
                TempData["GoogleConflictType"] = "Email Already Taken";
                TempData["GoogleConflictMessage"] = $"The email <strong>{email}</strong> is already registered with a different name. Please use the account associated with this email or contact support if you believe this is an error.";
                return RedirectToAction("Login", "Login");
            }

            // SCENARIO 3: Name exists but different email - Block with modal
            if (userWithSameEmail == null && usersWithSameName.Any())
            {
                TempData["GoogleCredentialConflict"] = "name";
                TempData["GoogleConflictType"] = "Name Already Taken";
                TempData["GoogleConflictMessage"] = $"An account with the name <strong>{googleFullName}</strong> already exists with a different email address. Please use a different Google account or contact support if you believe this is an error.";
                return RedirectToAction("Login", "Login");
            }

            // SCENARIO 4: No matches - Create new user and redirect to verification
            if (userWithSameEmail == null && !usersWithSameName.Any())
            {
                // ===========================
                // 🔑 Create new Google user with AES-256 encryption
                // ===========================

                // ✅ Generate random password (internal use only)
                string generatedPassword = "GOOG-" + Guid.NewGuid().ToString("N").Substring(0, 12);

                // Use AES-256 helper from configuration to encrypt password
                var aesHelper = new AesEncryptionHelper(_configuration);
                string encryptedPassword = aesHelper.Encrypt(generatedPassword);

                var newUser = new RegisterAcc
                {
                    FirstName = googleFirstName,
                    MiddleName = googleMiddleName,
                    LastName = googleLastName,
                    Suffix = "",
                    Email = email,
                    Username = googleFullName,
                    Password = encryptedPassword,
                    Status = "Active"
                };

                context.RegisterAcc.Add(newUser);
                context.SaveChanges();

                // Store session data for new user
                HttpContext.Session.SetString("UserId", newUser.Id.ToString());
                HttpContext.Session.SetString("Username", newUser.Username);
                HttpContext.Session.SetString("Email", newUser.Email);
                HttpContext.Session.SetString("GoogleEmail", email);
                HttpContext.Session.SetString("GoogleName", googleFullName);
                HttpContext.Session.SetString("IsRegisteredUser", "true");
                HttpContext.Session.SetString("IsVerifiedUser", "false");

                // ✅ Redirect new user to Account Verification
                TempData["WelcomeMessage"] = $"Welcome, {googleFirstName}! Please complete your account verification to access all features.";
                return RedirectToAction("Accountverification", "Login");
            }

            // Fallback (should never reach here)
            TempData["ErrorMessage"] = "An unexpected error occurred during Google sign-in. Please try again.";
            return RedirectToAction("Login", "Login");
        }

        public IActionResult Login(bool timeout = false)
        {
            // Handle session timeout message
            if (timeout)
            {
                TempData["TimeoutMessage"] = "You have been logged out due to inactivity. If you wish to continue, you can still sign in again.";
            }

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            if (TempData["TimeoutMessage"] != null)
            {
                ViewBag.TimeoutMessage = TempData["TimeoutMessage"];
            }

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            // Check for superadmin session
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("IsSuperadmin")))
            {
                return RedirectToAction("Superadmin", "Superadmin");
            }

            // Check for admin session
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("IsAdmin")))
            {
                return RedirectToAction("Analyticsdashboard", "Adminuser");
            }

            // If a regular user is logged in, redirect to the homepage
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Pass reCAPTCHA site key to view
            ViewBag.ReCaptchaSiteKey = _configuration["Security:ReCaptcha:SiteKey"] ?? "6Lfdj1orAAAAANkMj0kOMkNb8nLKFYlDL_9eZVhS";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto loginModel)
        {
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            // Check for existing cooldown cookie
            if (Request.Cookies.TryGetValue("LoginCooldown", out var cooldownValue) &&
                DateTime.TryParse(cooldownValue, out var cooldownUntil))
            {
                if (cooldownUntil > DateTime.Now)
                {
                    var remainingSeconds = (cooldownUntil - DateTime.Now).Seconds;
                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = false,
                            errorType = "cooldown",
                            title = "Login Temporarily Disabled",
                            message = $"Too many failed attempts. Please try again after {remainingSeconds} seconds."
                        });
                    }
                    ModelState.AddModelError("", $"Too many failed attempts. Please try again after {remainingSeconds} seconds.");
                    return View(loginModel);
                }
            }

            // Verify reCAPTCHA
            string recaptchaResponse = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        errorType = "recaptcha",
                        title = "reCAPTCHA Required",
                        message = "Please complete the reCAPTCHA verification."
                    });
                }
                ModelState.AddModelError("", "Please complete the reCAPTCHA.");
                return View(loginModel);
            }

            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    string secretKey = _configuration["Security:ReCaptcha:SecretKey"]
                        ?? throw new InvalidOperationException("ReCaptcha secret key not found in configuration");
                    var response = await httpClient.GetStringAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaResponse}");
                    var captchaResult = System.Text.Json.JsonDocument.Parse(response);
                    bool isSuccess = captchaResult.RootElement.GetProperty("success").GetBoolean();

                    if (!isSuccess)
                    {
                        if (IsAjaxRequest())
                        {
                            return Json(new
                            {
                                success = false,
                                errorType = "recaptcha",
                                title = "reCAPTCHA Verification Failed",
                                message = "reCAPTCHA verification failed. Please try again."
                            });
                        }
                        ModelState.AddModelError("", "reCAPTCHA verification failed.");
                        return View(loginModel);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"reCAPTCHA verification error: {ex.Message}");
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        errorType = "recaptcha",
                        title = "Verification Error",
                        message = "Error verifying reCAPTCHA. Please try again."
                    });
                }
                ModelState.AddModelError("", "Error verifying reCAPTCHA. Please try again.");
                return View(loginModel);
            }

            if (string.IsNullOrEmpty(loginModel.Username))
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        errorType = "validation",
                        title = "Username Required",
                        message = "Username is required."
                    });
                }
                ModelState.AddModelError("Username", "Username is required");
                return View(loginModel);
            }

            if (string.IsNullOrEmpty(loginModel.Password))
            {
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        errorType = "validation",
                        title = "Password Required",
                        message = "Password is required."
                    });
                }
                ModelState.AddModelError("Password", "Password is required");
                return View(loginModel);
            }

            try
            {
                // Check if the login is a Superadmin first
                var superadmin = context.Superadminaccount.FirstOrDefault(a =>
                    a.Username == loginModel.Username);
                if (superadmin != null && BCrypt.Net.BCrypt.Verify(loginModel.Password, superadmin.Password))
                {
                    // Reset failed attempts on successful login
                    Response.Cookies.Delete("FailedAttempts");
                    Response.Cookies.Delete("LoginCooldown");

                    // Set session for superadmin
                    HttpContext.Session.SetString("UserId", superadmin.Id.ToString());
                    HttpContext.Session.SetString("AdminFullname", superadmin.Fullname);
                    HttpContext.Session.SetString("Username", superadmin.Username);
                    HttpContext.Session.SetString("Email", superadmin.Email);
                    HttpContext.Session.SetString("IsSuperadmin", "true");

                    // Return JSON for AJAX requests with userType
                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            redirectUrl = Url.Action("Superadmin", "Superadmin"),
                            userType = "Superadmin" // Added userType
                        });
                    }

                    return RedirectToAction("Superadmin", "Superadmin");
                }

                // Check if the login is an Admin
                var admin = context.Adminaccount.FirstOrDefault(a =>
                    a.Username == loginModel.Username);
                if (admin != null && BCrypt.Net.BCrypt.Verify(loginModel.Password, admin.Password))
                {
                    // Check if user is inactive
                    if (admin.Status == "Removed")
                    {
                        if (IsAjaxRequest())
                        {
                            return Json(new
                            {
                                success = false,
                                errorType = "account",
                                title = "Account Removed",
                                message = "Your account is Removed. Please contact support."
                            });
                        }
                        ModelState.AddModelError("Username", "Your account is Removed. Please contact support.");
                        return View(loginModel);
                    }

                    // Reset failed attempts on successful login
                    Response.Cookies.Delete("FailedAttempts");
                    Response.Cookies.Delete("LoginCooldown");

                    HttpContext.Session.SetString("IsAdmin", "true");
                    HttpContext.Session.SetString("AdminFullname", admin.Fullname);

                    // Return JSON for AJAX requests with userType
                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            redirectUrl = Url.Action("Analyticsdashboard", "Adminuser"),
                            userType = "Admin" 
                        });
                    }

                    return RedirectToAction("Analyticsdashboard", "Adminuser");
                }

                // Check if it's a RegisterAcc user (new registration)
                var registerAccUser = context.RegisterAcc.FirstOrDefault(u =>
                    u.Email == loginModel.Username || u.Username == loginModel.Username);

                if (registerAccUser != null)
                {
                    // Use AES-256 helper from configuration to decrypt password
                    var aesHelper = new AesEncryptionHelper(_configuration);
                    string decryptedPassword = aesHelper.Decrypt(registerAccUser.Password);

                    if (decryptedPassword == loginModel.Password)
                    {
                        // Reset failed attempts on successful login
                        Response.Cookies.Delete("FailedAttempts");
                        Response.Cookies.Delete("LoginCooldown");

                        // Check if user has verified account data using RegisterAcc ID
                        var verifiedUser = context.Verifyaccount
                            .FirstOrDefault(v => v.UserId == registerAccUser.Id);

                        if (verifiedUser != null)
                        {
                            // Set session for verified user (complete profile)
                            HttpContext.Session.SetString("UserId", registerAccUser.Id.ToString());
                            HttpContext.Session.SetString("IDtype", verifiedUser.IDtype ?? "");
                            HttpContext.Session.SetString("IDnumber", verifiedUser.IDnumber ?? "");
                            HttpContext.Session.SetString("Firstname", verifiedUser.Firstname ?? "");
                            HttpContext.Session.SetString("Middlename", verifiedUser.Middlename ?? "");
                            HttpContext.Session.SetString("Lastname", verifiedUser.Lastname ?? "");
                            HttpContext.Session.SetString("Gender", verifiedUser.Gender ?? "");
                            HttpContext.Session.SetString("Suffix", verifiedUser.Suffix ?? "");
                            HttpContext.Session.SetString("Dateofbirth", verifiedUser.Dateofbirth ?? "");
                            HttpContext.Session.SetString("BlkLotStreet", verifiedUser.BlkLotStreet ?? "");
                            HttpContext.Session.SetString("SubVill", verifiedUser.SubVill ?? "");
                            HttpContext.Session.SetString("District", verifiedUser.District ?? "");
                            HttpContext.Session.SetString("Barangay", verifiedUser.Barangay ?? "");
                            HttpContext.Session.SetString("CivilStatus", verifiedUser.CivilStatus ?? "");
                            HttpContext.Session.SetString("Email", registerAccUser.Email ?? "");
                            HttpContext.Session.SetString("FrontID", verifiedUser.FrontID ?? "");
                            HttpContext.Session.SetString("BackID", verifiedUser.BackID ?? "");
                            HttpContext.Session.SetString("IsVerifiedUser", "true");
                            HttpContext.Session.SetString("IsRegisteredUser", "true");
                        }
                        else
                        {
                            // Set session for basic RegisterAcc user only
                            HttpContext.Session.SetString("UserId", registerAccUser.Id.ToString());
                            HttpContext.Session.SetString("Email", registerAccUser.Email ?? "");
                            HttpContext.Session.SetString("Username", registerAccUser.Username ?? "");
                            HttpContext.Session.SetString("IsRegisteredUser", "true");
                            HttpContext.Session.SetString("IsVerifiedUser", "false");
                        }

                        // Return JSON for AJAX requests with userType
                        if (IsAjaxRequest())
                        {
                            return Json(new
                            {
                                success = true,
                                redirectUrl = Url.Action("Homepage", "Dashboard"),
                                userType = "User" // Added userType for regular users
                            });
                        }

                        return RedirectToAction("Homepage", "Dashboard");
                    }
                }

                // If none of the above worked, increment failed attempts
                int failedAttempts = Request.Cookies.TryGetValue("FailedAttempts", out var attempts) ?
                    int.Parse(attempts) + 1 : 1;

                Response.Cookies.Append("FailedAttempts", failedAttempts.ToString(), new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(30),
                    HttpOnly = true,
                    Secure = true
                });

                string errorMessage;
                if (failedAttempts >= 3)
                {
                    // Set cooldown cookie for 30 seconds
                    Response.Cookies.Append("LoginCooldown", DateTime.Now.AddSeconds(30).ToString(), new CookieOptions
                    {
                        Expires = DateTime.Now.AddSeconds(30),
                        HttpOnly = true,
                        Secure = true
                    });

                    errorMessage = "Too many failed attempts. Please try again after 30 seconds.";
                }
                else
                {
                    errorMessage = $"Invalid username or password. Attempts remaining: {3 - failedAttempts}";
                }

                // Return JSON for AJAX requests
                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        errorType = "credentials",
                        title = "Login Failed",
                        message = errorMessage
                    });
                }

                ModelState.AddModelError("Username", errorMessage);
                return View(loginModel);
            }
            catch (Exception)
            {
                string errorMessage = "An unexpected error occurred. Please try again.";

                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = false,
                        errorType = "system",
                        title = "System Error",
                        message = errorMessage
                    });
                }

                ModelState.AddModelError("", errorMessage);
                return View(loginModel);
            }
        }

        // Helper method to check if request is AJAX
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.Headers["Content-Type"] == "application/json";
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Login");
        }

        /// <summary>
        /// Check if the user's session is still valid
        /// Used by session-timeout.js to detect server-side session expiration
        /// </summary>
        [HttpGet]
        public IActionResult CheckSession()
        {
            // Check if user has an active session
            bool hasUsername = !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
            bool hasSuperadmin = !string.IsNullOrEmpty(HttpContext.Session.GetString("IsSuperadmin"));
            bool hasAdmin = !string.IsNullOrEmpty(HttpContext.Session.GetString("IsAdmin"));

            bool isValid = hasUsername || hasSuperadmin || hasAdmin;

            return Json(new { isValid = isValid });
        }

        public IActionResult VerifyOTP()
        {
            return View();
        }

        public IActionResult Register()
        {
            // ═══════════════════════════════════════════════════════════════
            // 🔒 GENERATE ANTI-MANIPULATION REGISTRATION TOKEN
            // ═══════════════════════════════════════════════════════════════

            // Get client information
            string ipAddress = GetClientIpAddress();
            string userAgent = Request.Headers["User-Agent"].ToString() ?? "Unknown";

            // Generate unique cryptographic token
            string registrationToken = GenerateSecureToken();

            // Store token in database with expiration (10 minutes)
            var tokenRecord = new RegistrationToken
            {
                Token = registrationToken,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(10),
                IsUsed = false,
                IsRevoked = false
            };

            context.RegistrationTokens.Add(tokenRecord);
            context.SaveChanges();

            // Clean up expired tokens (older than 1 hour)
            CleanupExpiredTokens();

            // Pass token to view via ViewBag (will be embedded in hidden field)
            ViewBag.RegistrationToken = registrationToken;

            // Pass reCAPTCHA site key to view
            ViewBag.ReCaptchaSiteKey = _configuration["Security:ReCaptcha:SiteKey"] ?? "6Lfdj1orAAAAANkMj0kOMkNb8nLKFYlDL_9eZVhS";

            // ✅ NEW CODE: Check for success parameter and show modal
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            // Check if redirected with success parameter
            if (Request.Query["success"] == "true")
            {
                ViewBag.ShowSuccessModal = true;
            }

            return View();
        }

        // ═══════════════════════════════════════════════════════════════
        // 🔐 SECURITY HELPER METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Generate cryptographically secure random token
        /// </summary>
        private string GenerateSecureToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] tokenData = new byte[64];
                rng.GetBytes(tokenData);

                // Combine with timestamp for uniqueness
                string timestamp = DateTime.Now.Ticks.ToString();
                string combined = Convert.ToBase64String(tokenData) + timestamp;

                // Hash the combination for additional security
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return Convert.ToBase64String(hashBytes);
                }
            }
        }

        /// <summary>
        /// Get client IP address (handles proxies and load balancers)
        /// </summary>
        private string GetClientIpAddress()
        {
            // Check for forwarded IP (behind proxy/load balancer)
            string forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for real IP header
            string realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to remote IP
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Cleanup expired registration tokens
        /// </summary>
        private void CleanupExpiredTokens()
        {
            try
            {
                var oneHourAgo = DateTime.Now.AddHours(-1);
                var expiredTokens = context.RegistrationTokens
                    .Where(t => t.ExpiresAt < oneHourAgo)
                    .ToList();

                if (expiredTokens.Any())
                {
                    context.RegistrationTokens.RemoveRange(expiredTokens);
                    context.SaveChanges();
                }
            }
            catch
            {
                // Ignore cleanup errors - not critical
            }
        }

        /// <summary>
        /// Determine the source of the registration request
        /// </summary>
        private string DetermineRequestSource()
        {
            // Check if it's a form submission
            if (Request.HasFormContentType)
            {
                return "WEB_FORM";
            }

            // Check if it's an AJAX request
            if (IsAjaxRequest())
            {
                return "AJAX";
            }

            // Check content type for API calls
            string contentType = Request.ContentType ?? "";
            if (contentType.Contains("application/json"))
            {
                return "API";
            }

            // Check if referrer is from same domain
            string referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && referer.Contains(Request.Host.Host))
            {
                return "WEB_FORM";
            }

            // If none of the above, mark as unknown (suspicious)
            return "UNKNOWN";
        }

        /// <summary>
        /// Extract browser name from User Agent string
        /// </summary>
        private string GetBrowserName(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            userAgent = userAgent.ToLower();

            if (userAgent.Contains("edge")) return "Edge";
            if (userAgent.Contains("chrome")) return "Chrome";
            if (userAgent.Contains("firefox")) return "Firefox";
            if (userAgent.Contains("safari")) return "Safari";
            if (userAgent.Contains("opera")) return "Opera";
            if (userAgent.Contains("msie") || userAgent.Contains("trident")) return "IE";

            return "Unknown";
        }

        // ===========================
        // 🔍 REAL-TIME DUPLICATE CHECKING API ENDPOINTS
        // ===========================

        /// <summary>
        /// Check if email already exists in database (Real-time validation)
        /// </summary>
        [HttpGet]
        public JsonResult CheckEmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { exists = false });
            }

            bool exists = context.RegisterAcc.Any(u => u.Email.ToLower() == email.ToLower());
            return Json(new { exists = exists });
        }

        /// <summary>
        /// Check if username already exists in database (Real-time validation)
        /// </summary>
        [HttpGet]
        public JsonResult CheckUsernameExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Json(new { exists = false });
            }

            bool exists = context.RegisterAcc.Any(u => u.Username.ToLower() == username.ToLower());
            return Json(new { exists = exists });
        }

        /// <summary>
        /// Check if a person with the same full name already exists (Real-time validation)
        /// This helps prevent duplicate registrations by the same person
        /// </summary>
        [HttpGet]
        // ═══════════════════════════════════════════════════════════════
        // 🇵🇭 PHILIPPINE NAME NORMALIZATION HELPER
        // ═══════════════════════════════════════════════════════════════
        private string NormalizePhilippineName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            string normalized = name.ToLower().Trim();

            // Remove periods commonly found in Philippine names
            normalized = normalized.Replace(".", "");

            // Normalize Filipino compound name particles
            normalized = normalized
                .Replace("dela cruz", "delacruz")
                .Replace("de la cruz", "delacruz")
                .Replace("delos santos", "delossantos")
                .Replace("de los santos", "delossantos")
                .Replace("delos reyes", "delosreyes")
                .Replace("de los reyes", "delosreyes")
                .Replace("dela rosa", "delarosa")
                .Replace("de la rosa", "delarosa")
                .Replace("dela paz", "delapaz")
                .Replace("de la paz", "delapaz")
                .Replace("del rosario", "delrosario")
                .Replace("de guzman", "deguzman")
                .Replace("san jose", "sanjose")
                .Replace("san juan", "sanjuan")
                .Replace("santa maria", "santamaria")
                .Replace("santa cruz", "santacruz");

            // Remove extra whitespace
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        public JsonResult CheckNameExists(string firstName, string middleName, string lastName, string suffix)
        {
            // Normalize inputs first (client-side)
            firstName = NormalizePhilippineName(firstName);
            middleName = NormalizePhilippineName(middleName);
            lastName = NormalizePhilippineName(lastName);
            suffix = suffix?.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                return Json(new { exists = false, message = "" });
            }

            // Pull data from DB first, then do the NormalizePhilippineName comparisons in memory
            var users = context.RegisterAcc.AsEnumerable(); // This line moves it to client-side processing

            bool exactMatch = users.Any(u =>
                NormalizePhilippineName(u.FirstName) == firstName &&
                NormalizePhilippineName(u.MiddleName) == middleName &&
                NormalizePhilippineName(u.LastName) == lastName &&
                ((u.Suffix ?? "").ToLower() == suffix)
            );

            if (exactMatch)
            {
                return Json(new
                {
                    exists = true,
                    message = "A user with this exact name already exists in our system."
                });
            }

            bool similarMatch = users.Any(u =>
                NormalizePhilippineName(u.FirstName) == firstName &&
                NormalizePhilippineName(u.MiddleName) == middleName &&
                NormalizePhilippineName(u.LastName) == lastName
            );

            if (similarMatch)
            {
                return Json(new
                {
                    exists = false,
                    warning = true,
                    message = "A user with a similar name already exists. Please ensure you are not creating a duplicate account."
                });
            }

            return Json(new { exists = false, message = "" });
        }


        [HttpPost]
        public IActionResult Register(RegisterAccDto registerAccDto)
        {
            // ═══════════════════════════════════════════════════════════════
            // 🔒 ANTI-MANIPULATION SECURITY LAYER 1: TOKEN VALIDATION
            // ═══════════════════════════════════════════════════════════════

            string ipAddress = GetClientIpAddress();
            string userAgent = Request.Headers["User-Agent"].ToString() ?? "Unknown";
            string fullName = $"{registerAccDto.FirstName} {registerAccDto.MiddleName} {registerAccDto.LastName} {registerAccDto.Suffix}".Trim();

            // Create audit log for this attempt
            var auditLog = new RegistrationAuditLog
            {
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Email = registerAccDto.Email,
                Username = registerAccDto.Username,
                FullName = fullName,
                Action = "ATTEMPT",
                Source = DetermineRequestSource(),
                RegistrationToken = registerAccDto.RegistrationToken,
                AttemptedAt = DateTime.Now,
                SuspiciousActivity = false,
                HasValidToken = false
            };

            // Validate token exists in request
            if (string.IsNullOrWhiteSpace(registerAccDto.RegistrationToken))
            {
                auditLog.Action = "BLOCKED";
                auditLog.Reason = "Missing registration token - possible backend manipulation";
                auditLog.SuspiciousActivity = true;
                auditLog.SuspiciousReasons = "NO_TOKEN";
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                ModelState.AddModelError("", "🚫 Security Error: Invalid registration request. Please refresh the page and try again.");

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = new List<string> { "Security validation failed. Please refresh the page." } });
                }
                return View(registerAccDto);
            }

            // Validate token exists in database
            var tokenRecord = context.RegistrationTokens
                .FirstOrDefault(t => t.Token == registerAccDto.RegistrationToken);

            if (tokenRecord == null)
            {
                auditLog.Action = "BLOCKED";
                auditLog.Reason = "Token not found in database - possible forgery or direct API call";
                auditLog.SuspiciousActivity = true;
                auditLog.SuspiciousReasons = "INVALID_TOKEN";
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                ModelState.AddModelError("", "🚫 Security Error: Invalid security token. Please refresh the page and try again.");

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = new List<string> { "Invalid security token. Please refresh the page." } });
                }
                return View(registerAccDto);
            }

            // Check if token has expired
            if (tokenRecord.ExpiresAt < DateTime.Now)
            {
                auditLog.Action = "BLOCKED";
                auditLog.Reason = "Token expired - session took too long or replay attack";
                auditLog.SuspiciousActivity = true;
                auditLog.SuspiciousReasons = "EXPIRED_TOKEN";
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                ModelState.AddModelError("", "⏱️ Session Expired: Your registration session has expired. Please refresh the page and try again.");

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = new List<string> { "Session expired. Please refresh the page." } });
                }
                return View(registerAccDto);
            }

            // Check if token has already been used
            if (tokenRecord.IsUsed)
            {
                auditLog.Action = "BLOCKED";
                auditLog.Reason = $"Token already used - possible replay attack (previously used for: {tokenRecord.UsedByEmail})";
                auditLog.SuspiciousActivity = true;
                auditLog.SuspiciousReasons = "TOKEN_REUSE";
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                ModelState.AddModelError("", "🚫 Security Error: This registration token has already been used. Please refresh the page.");

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = new List<string> { "Security token already used. Please refresh the page." } });
                }
                return View(registerAccDto);
            }

            // Check if token is revoked
            if (tokenRecord.IsRevoked)
            {
                auditLog.Action = "BLOCKED";
                auditLog.Reason = "Token revoked - administrative action or security incident";
                auditLog.SuspiciousActivity = true;
                auditLog.SuspiciousReasons = "REVOKED_TOKEN";
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                ModelState.AddModelError("", "🚫 Access Denied: This registration session has been revoked. Please contact support.");

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = new List<string> { "Access denied. Please contact support." } });
                }
                return View(registerAccDto);
            }

            // ═══════════════════════════════════════════════════════════════
            // 🔒 ANTI-MANIPULATION SECURITY LAYER 2: IP/USER AGENT VALIDATION
            // ═══════════════════════════════════════════════════════════════

            List<string> suspiciousReasons = new List<string>();

            // Validate IP address matches
            if (tokenRecord.IpAddress != ipAddress)
            {
                suspiciousReasons.Add($"IP_MISMATCH: Token IP={tokenRecord.IpAddress}, Request IP={ipAddress}");
                auditLog.SuspiciousActivity = true;
            }

            // Validate User Agent matches (some variation allowed for browser updates)
            if (!tokenRecord.UserAgent.Contains(GetBrowserName(userAgent)) ||
                !userAgent.Contains(GetBrowserName(tokenRecord.UserAgent)))
            {
                suspiciousReasons.Add($"USERAGENT_MISMATCH: Token UA={tokenRecord.UserAgent}, Request UA={userAgent}");
                auditLog.SuspiciousActivity = true;
            }

            // Check for API/direct database manipulation attempts
            if (auditLog.Source == "API" || auditLog.Source == "DIRECT_DB" || auditLog.Source == "UNKNOWN")
            {
                suspiciousReasons.Add($"SUSPICIOUS_SOURCE: {auditLog.Source}");
                auditLog.SuspiciousActivity = true;
            }

            // Log suspicious activity but allow to continue (might be legitimate edge cases)
            if (suspiciousReasons.Any())
            {
                auditLog.SuspiciousReasons = string.Join("; ", suspiciousReasons);
            }

            // Mark token as valid for audit
            auditLog.HasValidToken = true;

            // ═══════════════════════════════════════════════════════════════
            // 🔒 ANTI-MANIPULATION SECURITY LAYER 3: RATE LIMITING
            // ═══════════════════════════════════════════════════════════════

            // Check for multiple registration attempts from same IP in last hour
            var recentAttemptsFromIp = context.RegistrationAuditLogs
                .Where(log => log.IpAddress == ipAddress &&
                              log.AttemptedAt > DateTime.Now.AddHours(-1))
                .Count();

            if (recentAttemptsFromIp > 5)
            {
                auditLog.Action = "BLOCKED";
                auditLog.Reason = $"Rate limit exceeded - {recentAttemptsFromIp} attempts in last hour";
                auditLog.SuspiciousActivity = true;
                auditLog.SuspiciousReasons = (auditLog.SuspiciousReasons ?? "") + "; RATE_LIMIT_EXCEEDED";
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                ModelState.AddModelError("", "⚠️ Too Many Attempts: You have exceeded the registration limit. Please try again later.");

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = new List<string> { "Too many registration attempts. Please try again later." } });
                }
                return View(registerAccDto);
            }

            // ═══════════════════════════════════════════════════════════════
            // ✅ PROCEED WITH NORMAL VALIDATION
            // ═══════════════════════════════════════════════════════════════

            if (!ModelState.IsValid)
            {
                auditLog.Action = "FAILED";
                auditLog.Reason = "Model validation failed";
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                // Return JSON errors for AJAX requests
                if (IsAjaxRequest())
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return Json(new { success = false, errors = errors });
                }
                return View(registerAccDto);
            }

            try
            {
                // 🔎 Check for existing email or username before saving
                if (context.RegisterAcc.Any(u => u.Email == registerAccDto.Email))
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, errors = new List<string> { "This email is already registered." } });
                    }
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(registerAccDto);
                }

                if (context.RegisterAcc.Any(u => u.Username == registerAccDto.Username))
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, errors = new List<string> { "This username is already taken." } });
                    }
                    ModelState.AddModelError("Username", "This username is already taken.");
                    return View(registerAccDto);
                }

                // 🔎 Check for duplicate full name (exact match)
                var normalizedSuffix = registerAccDto.Suffix?.Trim().ToLower() ?? "";
                bool duplicateName = context.RegisterAcc.Any(u =>
                    u.FirstName.ToLower() == registerAccDto.FirstName.Trim().ToLower() &&
                    u.MiddleName.ToLower() == registerAccDto.MiddleName.Trim().ToLower() &&
                    u.LastName.ToLower() == registerAccDto.LastName.Trim().ToLower() &&
                    (u.Suffix == null ? "" : u.Suffix.ToLower()) == normalizedSuffix
                );

                if (duplicateName)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = false, errors = new List<string> { "A user with this exact name already exists in our system. Each person is allowed only one account." } });
                    }
                    ModelState.AddModelError("", "A user with this exact name already exists in our system. Each person is allowed only one account.");
                    return View(registerAccDto);
                }

                // ==========================
                // 🔑 AES-256 PASSWORD ENCRYPTION
                // ==========================
                // Use AES-256 helper from configuration to encrypt password
                var aesHelper = new AesEncryptionHelper(_configuration);
                string encryptedPassword = aesHelper.Encrypt(registerAccDto.Password);

                var registercacc = new RegisterAcc
                {
                    FirstName = registerAccDto.FirstName,
                    MiddleName = registerAccDto.MiddleName,
                    LastName = registerAccDto.LastName,
                    Suffix = registerAccDto.Suffix,
                    Email = registerAccDto.Email,
                    Username = registerAccDto.Username,
                    Password = encryptedPassword,
                    Status = "Active"
                };

                context.RegisterAcc.Add(registercacc);
                context.SaveChanges();

                // ═══════════════════════════════════════════════════════════════
                // 🔒 MARK TOKEN AS USED & LOG SUCCESS
                // ═══════════════════════════════════════════════════════════════

                // Mark the registration token as used
                tokenRecord.IsUsed = true;
                tokenRecord.UsedAt = DateTime.Now;
                tokenRecord.UsedByEmail = registerAccDto.Email;
                context.SaveChanges();

                // Log successful registration
                auditLog.Action = "SUCCESS";
                auditLog.Reason = "Registration completed successfully";
                auditLog.RegisteredUserId = registercacc.Id;
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                // ✅ SUCCESS: Return JSON for AJAX
                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = "Registration successful!" });
                }
                else
                {
                    // For non-AJAX submissions, redirect with success parameter
                    return RedirectToAction("Register", "Login", new { success = "true" });
                }
            }
            catch (DbUpdateException dbEx)
            {
                // Log database error
                auditLog.Action = "FAILED";
                auditLog.Reason = $"Database error: {dbEx.Message}";
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                // 🧱 Handle SQL unique constraint error
                string errorMessage = "A database error occurred while saving. Please try again.";

                if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("IX_RegisterAcc_Email"))
                    errorMessage = "This email is already registered.";
                else if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("IX_RegisterAcc_Username"))
                    errorMessage = "This username is already taken.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = new List<string> { errorMessage } });
                }

                ModelState.AddModelError("", errorMessage);
                return View(registerAccDto);
            }
            catch (Exception ex)
            {
                // Log unexpected error
                auditLog.Action = "FAILED";
                auditLog.Reason = $"Unexpected error: {ex.Message}";
                context.RegistrationAuditLogs.Add(auditLog);
                context.SaveChanges();

                string errorMessage = "An unexpected error occurred. Please try again.";

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = new List<string> { errorMessage } });
                }

                ModelState.AddModelError("", errorMessage);
                return View(registerAccDto);
            }
        }

        public IActionResult Accountverification()
        {
            // Get the current user's ID from the session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                // If user is not logged in, redirect to login page
                return RedirectToAction("Login", "Login");
            }

            // Retrieve the registered user's information
            var registeredUser = context.RegisterAcc.FirstOrDefault(r => r.Id == userId);
            if (registeredUser == null)
            {
                return RedirectToAction("Login", "Login");
            }

            // Pass the registered user's name to ViewBag for comparison
            ViewBag.RegisteredFirstName = registeredUser.FirstName;
            ViewBag.RegisteredMiddleName = registeredUser.MiddleName;
            ViewBag.RegisteredLastName = registeredUser.LastName;
            ViewBag.RegisteredSuffix = registeredUser.Suffix ?? "";

            return View();
        }


        [HttpPost]
        public IActionResult Accountverification(VerifyaccountDto VerifyaccountDto)
        {
            // Get the current user's ID from the session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                // If user is not logged in, redirect to login page
                return RedirectToAction("Login", "Login");
            }

            // ═══════════════════════════════════════════════════════════════
            // 🔍 CHECK FOR DUPLICATE VERIFICATION
            // ═══════════════════════════════════════════════════════════════
            var existingVerification = context.Verifyaccount.FirstOrDefault(v => v.UserId == userId);
            if (existingVerification != null)
            {
                ModelState.AddModelError("", "Your account has already been verified. You cannot verify again.");
                return View(VerifyaccountDto);
            }

            // ═══════════════════════════════════════════════════════════════
            // 🔍 NAME MATCHING VALIDATION (ID vs Registered Account)
            // ═══════════════════════════════════════════════════════════════
            var registeredUser = context.RegisterAcc.FirstOrDefault(r => r.Id == userId);
            if (registeredUser == null)
            {
                ModelState.AddModelError("", "User account not found. Please login again.");
                return RedirectToAction("Login", "Login");
            }

            // Pass registered user info back to view for comparison (ALWAYS do this)
            ViewBag.RegisteredFirstName = registeredUser.FirstName;
            ViewBag.RegisteredMiddleName = registeredUser.MiddleName;
            ViewBag.RegisteredLastName = registeredUser.LastName;
            ViewBag.RegisteredSuffix = registeredUser.Suffix ?? "";

            // Normalize names for comparison
            string regFirstName = NormalizePhilippineName(registeredUser.FirstName ?? "");
            string regMiddleName = NormalizePhilippineName(registeredUser.MiddleName ?? "");
            string regLastName = NormalizePhilippineName(registeredUser.LastName ?? "");

            string idFirstName = NormalizePhilippineName(VerifyaccountDto.Firstname ?? "");
            string idMiddleName = NormalizePhilippineName(VerifyaccountDto.Middlename ?? "");
            string idLastName = NormalizePhilippineName(VerifyaccountDto.Lastname ?? "");

            // Debug logging (you can remove this in production)
            Console.WriteLine($"=== NAME VALIDATION DEBUG ===");
            Console.WriteLine($"Registered: {registeredUser.FirstName} {registeredUser.LastName}");
            Console.WriteLine($"Registered (normalized): {regFirstName} {regLastName}");
            Console.WriteLine($"ID: {VerifyaccountDto.Firstname} {VerifyaccountDto.Lastname}");
            Console.WriteLine($"ID (normalized): {idFirstName} {idLastName}");
            Console.WriteLine($"First Name Match: {regFirstName == idFirstName}");
            Console.WriteLine($"Last Name Match: {regLastName == idLastName}");

            // Check if names match (allow some flexibility for middle names)
            bool firstNameMatches = !string.IsNullOrEmpty(regFirstName) &&
                                   !string.IsNullOrEmpty(idFirstName) &&
                                   regFirstName.Equals(idFirstName, StringComparison.OrdinalIgnoreCase);

            bool lastNameMatches = !string.IsNullOrEmpty(regLastName) &&
                                  !string.IsNullOrEmpty(idLastName) &&
                                  regLastName.Equals(idLastName, StringComparison.OrdinalIgnoreCase);

            // Validation fails if either first name or last name doesn't match
            if (!firstNameMatches || !lastNameMatches)
            {
                ModelState.AddModelError("", "Name does not match with the registered name. Please use your own valid ID.");
                TempData["ErrorMessage"] = "Name does not match with the registered name. Please use your own valid ID.";

                // Log the mismatch for debugging
                Console.WriteLine($"❌ NAME VALIDATION FAILED: Registered='{regFirstName} {regLastName}' vs ID='{idFirstName} {idLastName}'");

                return View(VerifyaccountDto);
            }

            // ═══════════════════════════════════════════════════════════════
            // 🏙️ SERVER-SIDE DAVAO CITY VALIDATION
            // ═══════════════════════════════════════════════════════════════
            // List of valid Davao City barangays
            var davaoBarangays = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Acacia", "Agdao", "Alambre", "Alejandro Navarro", "Alfonso Angliongto Sr.",
        "Angalan", "Baguio Proper", "Baliok", "Bangkas Heights", "Baracatan",
        "Bato", "Bayabas", "Biao Escuela", "Biao Guianga", "Binugao",
        "Bucana", "Buhangin Proper", "Cabantian", "Cadalian", "Calinan Proper",
        "Callawa", "Camansi", "Carmen", "Catalunan Grande", "Catalunan Pequeño",
        "Catigan", "Cawayan", "Centro (San Juan)", "Colosas", "Communal",
        "Crossing Bayabas", "Dacudao", "Dalagdag", "Daliao", "Dalican",
        "Datu Salumay", "Dominga", "Eden", "Fatima (Benowang)", "Gatungan",
        "Gov. Paciano Bangoy", "Gov. Vicente Duterte", "Gumalang", "Gumitan",
        "Indangan", "Kap. Tomas Monteverde Sr.", "Kilate", "Lamanan",
        "Lampianao", "Langub", "Lapu-lapu", "Leon Garcia Sr.", "Los Amigos",
        "Lubogan", "Lumiad", "Ma-a", "Mabuhay", "Madapo", "Magtuod",
        "Mahayag", "Malabog", "Malagos", "Malamba", "Malandog", "Mampising",
        "Manambulan", "Mandug", "Manuel Guianga", "Mapula", "Marapangi",
        "Marilog Proper", "Matina Aplaya", "Matina Crossing", "Matina Pangi",
        "Mintal", "Mudiang", "Mulig", "New Carmen", "New Valencia", "Pampanga",
        "Panacan", "Pandaitan", "Panorama", "Paquibato Proper", "Paradise Embak",
        "Rafael Castillo", "Salapawan", "Salaysay", "Saloy", "San Antonio",
        "San Isidro", "Sasa", "Sirib", "Suawan", "Tacunan", "Tagakpan",
        "Tagluno", "Tagurano", "Talomo Proper", "Talomo River", "Tamurayan",
        "Tibungco", "Tigatto", "Tungkalan", "Ubalde", "Ugac", "Ula",
        "Vicente Hizon Sr.", "Waan", "Wangan", "Wilfredo Aquino", "Wines"
    };

            if (!davaoBarangays.Contains(VerifyaccountDto.Barangay))
            {
                ModelState.AddModelError("Barangay",
                    "This service is only available for Davao City residents. " +
                    "Please select a valid Davao City barangay.");
                return View(VerifyaccountDto);
            }

            // File validation
            if (VerifyaccountDto.ValidFrontID == null)
                ModelState.AddModelError("ValidFrontID", "Front ID image is required");
            if (VerifyaccountDto.ValidBackID == null)
                ModelState.AddModelError("ValidBackID", "Back ID image is required");

            if (!ModelState.IsValid)
                return View(VerifyaccountDto);

            try
            {
                // ==========================
                // 🔑 AES-256 FILE ENCRYPTION
                // ==========================
                // Create AES helper instance with configuration
                var aesHelper = new AesEncryptionHelper(_configuration);

                // Generate unique encrypted timestamp for filenames
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = aesHelper.EncryptTimestamp(timestamp);
                string safeEncryptedTimestamp = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // ==========================
                // 🪪 Encrypt Front ID
                // ==========================
                string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
                Directory.CreateDirectory(validFolder);
                string frontFileName = safeEncryptedTimestamp + "_front.enc";
                string frontPath = Path.Combine(validFolder, frontFileName);
                using (var fileStream = new FileStream(frontPath, FileMode.Create))
                {
                    // Use AES-256 helper from configuration to encrypt file stream
                    byte[] encryptedData = aesHelper.EncryptStream(VerifyaccountDto.ValidFrontID!.OpenReadStream());
                    fileStream.Write(encryptedData, 0, encryptedData.Length);
                }

                // ==========================
                // 🔙 Encrypt Back ID
                // ==========================
                string backFileName = safeEncryptedTimestamp + "_back.enc";
                string backPath = Path.Combine(validFolder, backFileName);
                using (var fileStream = new FileStream(backPath, FileMode.Create))
                {
                    // Use AES-256 helper from configuration to encrypt file stream
                    byte[] encryptedData = aesHelper.EncryptStream(VerifyaccountDto.ValidBackID!.OpenReadStream());
                    fileStream.Write(encryptedData, 0, encryptedData.Length);
                }

                // ==========================
                // 🗃 Save to Database
                // ==========================
                Verifyaccount verifyaccount = new Verifyaccount()
                {
                    UserId = userId,
                    FrontID = frontFileName,
                    BackID = backFileName,
                    IDtype = VerifyaccountDto.IDtype,
                    IDnumber = VerifyaccountDto.IDnumber,
                    Lastname = VerifyaccountDto.Lastname,
                    Firstname = VerifyaccountDto.Firstname,
                    Middlename = VerifyaccountDto.Middlename,
                    Suffix = VerifyaccountDto.Suffix,
                    Gender = VerifyaccountDto.Gender,
                    Dateofbirth = VerifyaccountDto.Dateofbirth,
                    BlkLotStreet = VerifyaccountDto.BlkLotStreet,
                    SubVill = VerifyaccountDto.SubVill,
                    Barangay = VerifyaccountDto.Barangay,
                    District = VerifyaccountDto.District,
                    Phonenumber = VerifyaccountDto.Phonenumber,
                    CivilStatus = VerifyaccountDto.CivilStatus,
                };

                context.Verifyaccount.Add(verifyaccount);
                context.SaveChanges();

                // ═══════════════════════════════════════════════════════════════
                //  UPDATE SESSION WITH VERIFIED USER DATA
                // ═══════════════════════════════════════════════════════════════
                HttpContext.Session.SetString("IDtype", verifyaccount.IDtype ?? "");
                HttpContext.Session.SetString("IDnumber", verifyaccount.IDnumber ?? "");
                HttpContext.Session.SetString("Firstname", verifyaccount.Firstname ?? "");
                HttpContext.Session.SetString("Middlename", verifyaccount.Middlename ?? "");
                HttpContext.Session.SetString("Lastname", verifyaccount.Lastname ?? "");
                HttpContext.Session.SetString("Gender", verifyaccount.Gender ?? "");
                HttpContext.Session.SetString("Suffix", verifyaccount.Suffix ?? "");
                HttpContext.Session.SetString("Dateofbirth", verifyaccount.Dateofbirth ?? "");
                HttpContext.Session.SetString("BlkLotStreet", verifyaccount.BlkLotStreet ?? "");
                HttpContext.Session.SetString("SubVill", verifyaccount.SubVill ?? "");
                HttpContext.Session.SetString("District", verifyaccount.District ?? "");
                HttpContext.Session.SetString("Barangay", verifyaccount.Barangay ?? "");
                HttpContext.Session.SetString("CivilStatus", verifyaccount.CivilStatus ?? "");
                HttpContext.Session.SetString("FrontID", verifyaccount.FrontID ?? "");
                HttpContext.Session.SetString("BackID", verifyaccount.BackID ?? "");
                HttpContext.Session.SetString("IsVerifiedUser", "true");

                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
                return View(VerifyaccountDto);
            }
        }

        public IActionResult Registeredit(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            ViewBag.Id = HttpContext.Session.GetString("UserId");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Email = HttpContext.Session.GetString("Email");

            return View();
        }

        [HttpPost]
        public IActionResult Registeredit(int id, RegisterDto registerDto, string currentPassword)
        {
            var existingUser = context.Register.FirstOrDefault(r => r.Id == id);
            if (existingUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Skip validation for image if not provided
            if (registerDto.ImageFile == null)
            {
                ModelState.Remove("ImageFile");
            }

            // Verify current password if user is trying to change password
            if (!string.IsNullOrWhiteSpace(registerDto.Password))
            {
                // Enhanced current password validation
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is required to change your password.");
                    TempData["PasswordError"] = "Current password is required.";
                }
                else if (!BCrypt.Net.BCrypt.Verify(currentPassword, existingUser.Password))
                {
                    ModelState.AddModelError("CurrentPassword", "The current password you entered is incorrect.");
                    TempData["PasswordError"] = "Current password was wrong. Please try again.";

                    // Add client-side validation trigger
                    ViewBag.TriggerPasswordValidation = true;
                }
            }
            else
            {
                // Skip password validation if empty (user is not changing password)
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate form data with existing values
                ViewData["ImageFileName"] = existingUser.ImageFilename;
                registerDto.Fullname = existingUser.Fullname;
                registerDto.Username = existingUser.Username;
                registerDto.Email = existingUser.Email;
                registerDto.Phonenumber = existingUser.Phonenumber;
                registerDto.Dateofbirth = existingUser.Dateofbirth;
                registerDto.Gender = existingUser.Gender;
                registerDto.Address = existingUser.Address;

                // Return to view with enhanced error information
                return View(registerDto);
            }

            try
            {
                // Handle image upload
                string uploadsFolder = Path.Combine(environment.WebRootPath, "UsersImg");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (registerDto.ImageFile != null)
                {
                    string newFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(registerDto.ImageFile.FileName);
                    string newFilePath = Path.Combine(uploadsFolder, newFileName);
                    using (var stream = new FileStream(newFilePath, FileMode.Create))
                    {
                        registerDto.ImageFile.CopyTo(stream);
                    }

                    // Delete old image
                    if (!string.IsNullOrEmpty(existingUser.ImageFilename))
                    {
                        string oldImagePath = Path.Combine(uploadsFolder, existingUser.ImageFilename);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    existingUser.ImageFilename = newFileName;
                }

                // Update user properties
                existingUser.Fullname = registerDto.Fullname;
                existingUser.Username = registerDto.Username;
                existingUser.Email = registerDto.Email;
                existingUser.Phonenumber = registerDto.Phonenumber;
                existingUser.Dateofbirth = registerDto.Dateofbirth;
                existingUser.Gender = registerDto.Gender;
                existingUser.Address = registerDto.Address;

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(registerDto.Password))
                {
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
                    existingUser.Password = hashedPassword;
                    TempData["SuccessMessage"] = "Your password has been updated successfully.";
                }

                context.SaveChanges();
                TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving changes: " + ex.Message);
                ViewData["ImageFileName"] = existingUser.ImageFilename;
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return View(registerDto);
            }
        }

        // Password validation helper method
        private PasswordValidationResult ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(password))
            {
                errors.Add("Password is required.");
                return new PasswordValidationResult { IsValid = false, Errors = errors };
            }

            // Check minimum length
            if (password.Length < 8)
            {
                errors.Add("Password must be at least 8 characters long.");
            }

            // Check for uppercase letters
            if (!password.Any(char.IsUpper))
            {
                errors.Add("Password must contain at least one uppercase letter.");
            }

            // Check for lowercase letters
            if (!password.Any(char.IsLower))
            {
                errors.Add("Password must contain at least one lowercase letter.");
            }

            // Check for numbers
            if (!password.Any(char.IsDigit))
            {
                errors.Add("Password must contain at least one number.");
            }

            // Check for special characters
            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                errors.Add("Password must contain at least one special character.");
            }

            return new PasswordValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        // Password validation result class
        public class PasswordValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }
    }
}