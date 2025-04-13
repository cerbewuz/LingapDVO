using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Controllers
{
    public class LoginController : Controller
    {
        public readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;

        public LoginController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
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

                // Map data to entity
                Register register = new Register()
                {
                    Fullname = registerDto.Fullname,
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    Phonenumber = registerDto.Phonenumber,
                    Password = registerDto.Password,
                    Dateofbirth = registerDto.Dateofbirth,
                    Gender = registerDto.Gender,
                    Address = registerDto.Address,
                    ImageFilename = newFileName,
                    SecurityQuestions = registerDto.SecurityQuestions,
                    Securityanswer = registerDto.Securityanswer
                };

                context.Register.Add(register);
                context.SaveChanges();

                return RedirectToAction("Login", "Login");
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




    }
}
