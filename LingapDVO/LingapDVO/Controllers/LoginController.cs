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
                    ModelState.AddModelError("", $"Too many failed attempts. Please try again after {(cooldownUntil - DateTime.Now).Seconds} seconds.");
                    return View(loginModel);
                }
            }

            // Verify reCAPTCHA
            string recaptchaResponse = Request.Form["g-recaptcha-response"];
            if (string.IsNullOrEmpty(recaptchaResponse))
            {
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
                        ModelState.AddModelError("", "reCAPTCHA verification failed.");
                        return View(loginModel);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"reCAPTCHA verification error: {ex.Message}");
                ModelState.AddModelError("", "Error verifying reCAPTCHA. Please try again.");
                return View(loginModel);
            }

            if (string.IsNullOrEmpty(loginModel.Username))
            {
                ModelState.AddModelError("Username", "Username is required");
                return View(loginModel);
            }

            if (string.IsNullOrEmpty(loginModel.Password))
            {
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
                        return Json(new { success = true, redirectUrl = Url.Action("Superadmin", "Superadmin") });
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
                        return Json(new { success = true, redirectUrl = Url.Action("Analyticsdashboard", "Adminuser") });
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
                            return Json(new { success = true, redirectUrl = Url.Action("Homepage", "Dashboard") });
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

                if (failedAttempts >= 3)
                {
                    // Set cooldown cookie for 30 seconds
                    Response.Cookies.Append("LoginCooldown", DateTime.Now.AddSeconds(30).ToString(), new CookieOptions
                    {
                        Expires = DateTime.Now.AddSeconds(30),
                        HttpOnly = true,
                        Secure = true
                    });

                    ModelState.AddModelError("", "Too many failed attempts. Please try again after 30 seconds.");
                }
                else
                {
                    ModelState.AddModelError("Username", $"Invalid username or password. Attempts remaining: {3 - failedAttempts}");
                }

                // Return JSON for AJAX requests
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
                }

                return View(loginModel);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");

                // Return JSON for AJAX requests
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, errors = new[] { "An unexpected error occurred. Please try again." } });
                }

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
                // Check for existing email or username before saving
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

                // Return the view with success message
                ViewBag.SuccessMessage = "Registration successful! You can now log in.";
                return View(registerAccDto);
            }
            catch (DbUpdateException dbEx)
            {
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
                string masterPassword = "SuperAdminMasterKey123!";
                byte[] salt = RandomNumberGenerator.GetBytes(16);

                using var pbkdf2 = new Rfc2898DeriveBytes(masterPassword, salt, 100_000, HashAlgorithmName.SHA256);
                byte[] key = pbkdf2.GetBytes(32);

                byte[] EncryptFile(Stream inputStream)
                {
                    using var aes = Aes.Create();
                    aes.Key = key;
                    aes.GenerateIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var memoryStream = new MemoryStream();
                    memoryStream.Write(salt, 0, salt.Length);
                    memoryStream.Write(aes.IV, 0, aes.IV.Length);

                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        inputStream.CopyTo(cryptoStream);
                    }

                    return memoryStream.ToArray();
                }

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                string encryptedTimestamp;
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using var encryptor = aes.CreateEncryptor();
                    byte[] inputBytes = Encoding.UTF8.GetBytes(timestamp);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                    encryptedTimestamp = Convert.ToBase64String(encryptedBytes);
                }

                string safeEncryptedTimestamp = new string(encryptedTimestamp.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

                // Encrypt Front ID
                string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
                Directory.CreateDirectory(validFolder);
                string frontFileName = safeEncryptedTimestamp + "_front.enc";
                string frontPath = Path.Combine(validFolder, frontFileName);
                using (var fileStream = new FileStream(frontPath, FileMode.Create))
                {
                    byte[] encryptedData = EncryptFile(VerifyaccountDto.ValidFrontID!.OpenReadStream());
                    fileStream.Write(encryptedData, 0, encryptedData.Length);
                }

                // Encrypt Back ID
                string backFileName = safeEncryptedTimestamp + "_back.enc";
                string backPath = Path.Combine(validFolder, backFileName);
                using (var fileStream = new FileStream(backPath, FileMode.Create))
                {
                    byte[] encryptedData = EncryptFile(VerifyaccountDto.ValidBackID!.OpenReadStream());
                    fileStream.Write(encryptedData, 0, encryptedData.Length);
                }

                // Save to Database
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
            ViewBag.ImageFilename = HttpContext.Session.GetString("ImageFilename");
            ViewBag.Fullname = HttpContext.Session.GetString("Fullname");
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Email = HttpContext.Session.GetString("Email");
            ViewBag.Phonenumber = HttpContext.Session.GetString("Phonenumber");
            ViewBag.Address = HttpContext.Session.GetString("Address");
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
    }
}