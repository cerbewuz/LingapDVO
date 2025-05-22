using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using iText.Commons.Actions.Data;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using Microsoft.AspNetCore.Mvc.Rendering;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Authorization;


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

        public IActionResult Login()
        {

            // Prevent browser from caching the login page
            // Prevent browser from caching the login page
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            // Check if a superadmin or admin session exists
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                // Check if it's a superadmin session
                return RedirectToAction("Superadmin", "Superadmin");
            }

            // Check if a superadmin or admin session exists
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                // Check if it's a superadmin session
                return RedirectToAction("Admin", "Adminuser");
            }

            // If a regular user is logged in, redirect to the homepage
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Homepage", "Dashboard");
            }


            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginDto loginModel)
        {
            // Prevent browser from caching the login page
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

            // Verify with Google reCAPTCHA API using HttpClient instead of WebClient
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    string secretKey = "6Lef3DgrAAAAAPgoJtzEr8sYd-j3tDl-WHYGCO_S";
                    var response = httpClient.GetStringAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaResponse}").Result;
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
                // Check if the login is an Admin first
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

                    return RedirectToAction("Superadmin", "Superadmin");
                }

                // Check if the login is an Admin first
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
                    return RedirectToAction("Admin", "Adminuser");
                }

                // Check if it's a regular user
                var user = context.Register.FirstOrDefault(u =>
                    u.Username == loginModel.Username);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginModel.Password, user.Password))
                {
                    // Increment failed attempts
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

                    return View(loginModel);
                }

                // Check if user is inactive
                if (user.Status == "Removed")
                {
                    ModelState.AddModelError("Username", "Your account is Removed. Please contact support.");
                    return View(loginModel);
                }

                // Reset failed attempts on successful login
                Response.Cookies.Delete("FailedAttempts");
                Response.Cookies.Delete("LoginCooldown");

                // Set session for user
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Fullname", user.Fullname);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("Phonenumber", user.Phonenumber);
                HttpContext.Session.SetString("Address", user.Address);
                HttpContext.Session.SetString("Dateofbirth", user.Dateofbirth);
                HttpContext.Session.SetString("Gender", user.Gender);
                HttpContext.Session.SetString("ImageFilename", user.ImageFilename);
                HttpContext.Session.SetString("SecurityQuestions", user.SecurityQuestions);
                HttpContext.Session.SetString("Securityanswer", user.Securityanswer);

                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(loginModel);
            }
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Login");
        }



        public IActionResult VerifyOTP()
        {
            return View();
        }



        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterDto registerDto)
        {
            if (registerDto.ImageFile == null)
            {
                ModelState.AddModelError("ImageFile", "The image file is required");
            }

            if (!ModelState.IsValid)
            {
                return View(registerDto);
            }

            try
            {
                // Generate unique filename
                string newFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                     Path.GetExtension(registerDto.ImageFile!.FileName);

                // Save image to wwwroot/UsersImg
                string uploadsFolder = Path.Combine(environment.WebRootPath, "UsersImg");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filePath = Path.Combine(uploadsFolder, newFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    registerDto.ImageFile.CopyTo(stream);
                }

                 string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                // Map data to entity
                Register register = new Register()
                {
                    Fullname = registerDto.Fullname,
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    Phonenumber = registerDto.Phonenumber,
                    Password = hashedPassword,
                    Dateofbirth = registerDto.Dateofbirth,
                    Gender = registerDto.Gender,
                    Address = registerDto.Address,
                    ImageFilename = newFileName,
                    SecurityQuestions = registerDto.SecurityQuestions,
                    Securityanswer = registerDto.Securityanswer,
                    Status = "Active"
                };

                context.Register.Add(register);
                context.SaveChanges();

                return RedirectToAction("Admin", "Adminuser");
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

                    return View(registerDto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(registerDto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(registerDto);
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

        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find user in Register table
            var user = context.Register.FirstOrDefault(u => u.Username == model.Username);
            if (user == null)
            {
                // Generic message for security
                TempData["Message"] = "If the information is correct, password has been reset.";
                return RedirectToAction("Login");
            }

            // Verify security question and answer
            if (user.SecurityQuestions != model.SecurityQuestion ||
                user.Securityanswer != model.SecurityAnswer)
            {
                // Generic message for security
                TempData["Message"] = "If the information is correct, password has been reset.";
                return RedirectToAction("Login");
            }

            // Update password
            user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            context.SaveChanges();

            TempData["SuccessMessage"] = "Password has been reset successfully!";
            return RedirectToAction("Login");
        }






    }
}
