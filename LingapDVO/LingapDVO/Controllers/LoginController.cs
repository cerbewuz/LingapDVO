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

        public LoginController(ApplicationDbContext context, IWebHostEnvironment environment, SmsService smsService)
        {
            this.context = context;
            this.environment = environment;
            _smsService = smsService;
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

                // Use hardcoded AES-256 helper to encrypt password
                string encryptedPassword = AesEncryptionHelper.Encrypt(generatedPassword);

                user = new RegisterAcc
                {
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
                HttpContext.Session.SetString("SecurityQuestions", verifiedUser.SecurityQuestions ?? "");
                HttpContext.Session.SetString("Securityanswer", verifiedUser.Securityanswer ?? "");
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
                RedirectUri = Url.Action("Homepage", "Dashboard") // always redirect here
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

            // ✅ Check if user already exists
            var existingUser = context.RegisterAcc.FirstOrDefault(u => u.Email == email);
            RegisterAcc user;

            if (existingUser == null)
            {
                // ===========================
                // 🔑 Create auto Google user with AES-256 encryption
                // ===========================

                // ✅ Generate random password (internal use only)
                string generatedPassword = "GOOG-" + Guid.NewGuid().ToString("N").Substring(0, 12);

                // Use hardcoded AES-256 helper to encrypt password
                string encryptedPassword = AesEncryptionHelper.Encrypt(generatedPassword);

                user = new RegisterAcc
                {
                    Email = email,
                    Username = name ?? "GoogleUser",
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
                HttpContext.Session.SetString("SecurityQuestions", verifiedUser.SecurityQuestions ?? "");
                HttpContext.Session.SetString("Securityanswer", verifiedUser.Securityanswer ?? "");
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
            HttpContext.Session.SetString("GoogleEmail", email ?? "");
            HttpContext.Session.SetString("GoogleName", name ?? "");
            HttpContext.Session.SetString("Username", name ?? "Google User");

            // ===========================
            // 🧭 Redirect Logic
            // ===========================
            if (verifiedUser == null)
            {
                // First-time Google login or unverified user
                return RedirectToAction("Accountverification", "Login");
            }
            else
            {
                // Already verified — straight to homepage
                return RedirectToAction("Homepage", "Dashboard");
            }
        }

        public IActionResult Login()
        {
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
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
                    string secretKey = "6Lfdj1orAAAAAKINUvegNElqk5Fld8S9qASq8jtP";
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
                    // Use hardcoded AES-256 helper to decrypt password
                    string decryptedPassword = AesEncryptionHelper.Decrypt(registerAccUser.Password);

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
                            HttpContext.Session.SetString("Email", registerAccUser.Email ?? "");
                            HttpContext.Session.SetString("SecurityQuestions", verifiedUser.SecurityQuestions ?? "");
                            HttpContext.Session.SetString("Securityanswer", verifiedUser.Securityanswer ?? "");
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

        public IActionResult VerifyOTP()
        {
            return View();
        }

        public IActionResult Register()
        {
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

        [HttpPost]
        public IActionResult Register(RegisterAccDto registerAccDto)
        {
            if (!ModelState.IsValid)
            {
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

                // ==========================
                // 🔑 AES-256 PASSWORD ENCRYPTION
                // ==========================
                // Use hardcoded AES-256 helper to encrypt password
                string encryptedPassword = AesEncryptionHelper.Encrypt(registerAccDto.Password);

                var registercacc = new RegisterAcc
                {
                    Email = registerAccDto.Email,
                    Username = registerAccDto.Username,
                    Password = encryptedPassword,
                    Status = "Active"
                };

                context.RegisterAcc.Add(registercacc);
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
                // Generate unique encrypted timestamp for filenames
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp = AesEncryptionHelper.EncryptTimestamp(timestamp);
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
                    // Use hardcoded AES-256 helper to encrypt file stream
                    byte[] encryptedData = AesEncryptionHelper.EncryptStream(VerifyaccountDto.ValidFrontID!.OpenReadStream());
                    fileStream.Write(encryptedData, 0, encryptedData.Length);
                }

                // ==========================
                // 🔙 Encrypt Back ID
                // ==========================
                string backFileName = safeEncryptedTimestamp + "_back.enc";
                string backPath = Path.Combine(validFolder, backFileName);
                using (var fileStream = new FileStream(backPath, FileMode.Create))
                {
                    // Use hardcoded AES-256 helper to encrypt file stream
                    byte[] encryptedData = AesEncryptionHelper.EncryptStream(VerifyaccountDto.ValidBackID!.OpenReadStream());
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
                    SecurityQuestions = VerifyaccountDto.SecurityQuestions,
                    Securityanswer = VerifyaccountDto.Securityanswer,
                    Phonenumber = VerifyaccountDto.Phonenumber,
                };

                context.Verifyaccount.Add(verifyaccount);
                context.SaveChanges();

                return Redirect("/Homepage");
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
                registerDto.SecurityQuestions = existingUser.SecurityQuestions;
                registerDto.Securityanswer = existingUser.Securityanswer;

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
                existingUser.SecurityQuestions = registerDto.SecurityQuestions;
                existingUser.Securityanswer = registerDto.Securityanswer;

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