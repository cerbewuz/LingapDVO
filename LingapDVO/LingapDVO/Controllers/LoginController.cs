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
                return RedirectToAction("Login", "Login");
            }

            var claims = result.Principal.Claims.ToList();
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            HttpContext.Session.SetString("FacebookEmail", email ?? "");
            HttpContext.Session.SetString("FacebookName", name ?? "");
            HttpContext.Session.SetString("Username", name ?? "Facebook User");

            return RedirectToAction("Homepage", "Dashboard");
        }

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse", "Login")
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        [Route("/signin-google")]
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                return RedirectToAction("Login", "Login");
            }

            var claims = result.Principal?.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            HttpContext.Session.SetString("GoogleEmail", email ?? "");
            HttpContext.Session.SetString("GoogleName", name ?? "");
            HttpContext.Session.SetString("Username", name ?? "Google User");

            return RedirectToAction("Homepage", "Dashboard");
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

                    // Set session for admin
                    HttpContext.Session.SetString("UserId", superadmin.Id.ToString());
                    HttpContext.Session.SetString("AdminFullname", superadmin.Fullname);
                    HttpContext.Session.SetString("Username", superadmin.Username);
                    HttpContext.Session.SetString("Email", superadmin.Email);
                    HttpContext.Session.SetString("IsSuperadmin", "true");

                    // Return JSON for AJAX requests
                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            redirectUrl = Url.Action("Superadmin", "Superadmin")
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

                    // Return JSON for AJAX requests
                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            redirectUrl = Url.Action("Analyticsdashboard", "Adminuser")
                        });
                    }

                    return RedirectToAction("Analyticsdashboard", "Adminuser");
                }

                // Check if it's a RegisterAcc user (new registration)
                var registerAccUser = context.RegisterAcc.FirstOrDefault(u =>
                    u.Email == loginModel.Username || u.Username == loginModel.Username);

                if (registerAccUser != null)
                {
                    string DecryptPassword(string encryptedPassword)
                    {
                        byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);

                        using var memoryStream = new MemoryStream(encryptedBytes);
                        byte[] salt = new byte[16];
                        byte[] iv = new byte[16];
                        memoryStream.Read(salt, 0, salt.Length);
                        memoryStream.Read(iv, 0, iv.Length);

                        string masterPassword = "SuperAdminMasterKey123!";
                        using var pbkdf2 = new Rfc2898DeriveBytes(masterPassword, salt, 100_000, HashAlgorithmName.SHA256);
                        byte[] key = pbkdf2.GetBytes(32);

                        using var aes = Aes.Create();
                        aes.Key = key;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using var decryptor = aes.CreateDecryptor();
                        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                        using var reader = new StreamReader(cryptoStream);

                        return reader.ReadToEnd();
                    }

                    string decryptedPassword = DecryptPassword(registerAccUser.Password);

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

                        // Return JSON for AJAX requests
                        if (IsAjaxRequest())
                        {
                            return Json(new
                            {
                                success = true,
                                redirectUrl = Url.Action("Homepage", "Dashboard")
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
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterAccDto registerAccDto)
        {
            if (!ModelState.IsValid)
                return View(registerAccDto);

            try
            {
                // 🔎 Check for existing email or username before saving
                if (context.RegisterAcc.Any(u => u.Email == registerAccDto.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(registerAccDto);
                }

                if (context.RegisterAcc.Any(u => u.Username == registerAccDto.Username))
                {
                    ModelState.AddModelError("Username", "This username is already taken.");
                    return View(registerAccDto);
                }

                // ==========================
                // 🔑 MASTER PASSWORD SECTION
                // ==========================
                string masterPassword = "SuperAdminMasterKey123!";
                byte[] salt = RandomNumberGenerator.GetBytes(16);
                byte[] iv = RandomNumberGenerator.GetBytes(16);

                using var pbkdf2 = new Rfc2898DeriveBytes(masterPassword, salt, 100_000, HashAlgorithmName.SHA256);
                byte[] key = pbkdf2.GetBytes(32);

                string EncryptPassword(string password)
                {
                    using var aes = Aes.Create();
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var encryptor = aes.CreateEncryptor();
                    using var memoryStream = new MemoryStream();

                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    using (var writer = new StreamWriter(cryptoStream))
                    {
                        writer.Write(password);
                    }

                    byte[] encryptedData = memoryStream.ToArray();
                    byte[] combinedData = new byte[salt.Length + iv.Length + encryptedData.Length];

                    Buffer.BlockCopy(salt, 0, combinedData, 0, salt.Length);
                    Buffer.BlockCopy(iv, 0, combinedData, salt.Length, iv.Length);
                    Buffer.BlockCopy(encryptedData, 0, combinedData, salt.Length + iv.Length, encryptedData.Length);

                    return Convert.ToBase64String(combinedData);
                }

                string encryptedPassword = EncryptPassword(registerAccDto.Password);

                var registercacc = new RegisterAcc
                {
                    Email = registerAccDto.Email,
                    Username = registerAccDto.Username,
                    Password = encryptedPassword,
                };

                context.RegisterAcc.Add(registercacc);
                context.SaveChanges();

                TempData["SuccessMessage"] = "Registration successful! You can now log in.";
                return RedirectToAction("Login", "Login");
            }
            catch (DbUpdateException dbEx)
            {
                // 🧱 Handle SQL unique constraint error
                if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("IX_RegisterAcc_Email"))
                    ModelState.AddModelError("Email", "This email is already registered.");
                else if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("IX_RegisterAcc_Username"))
                    ModelState.AddModelError("Username", "This username is already taken.");
                else
                    ModelState.AddModelError("", "A database error occurred while saving. Please try again.");

                return View(registerAccDto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
                return View(registerAccDto);
            }
        }


        // ... rest of your existing methods (Accountverification, Registeredit, etc.) remain the same

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