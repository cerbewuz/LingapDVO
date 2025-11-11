using iText.Commons.Actions.Data;
using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace LingapDVO.Controllers
{
    public class SuperadminController : Controller
    {
        public readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly ISessionConfigurationService _sessionConfig;

        public SuperadminController(ApplicationDbContext context, IWebHostEnvironment environment, ISessionConfigurationService sessionConfig)
        {
            this.context = context;
            this.environment = environment;
            _sessionConfig = sessionConfig;
        }
        public IActionResult Index()
        {
            return View();
        }

            public IActionResult Superadmin()
            {
            
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";


            if (HttpContext.Session.GetString("IsSuperadmin") != "true")
            {
                    return RedirectToAction("Landingpage", "Dashboard");
            }


            // Get all data from the database without filtering by userId
            var hospitalBills = context.HospitalAssistance
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.OtherAssistance
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var FuneralAssistance = context.FuneralAssistance
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var RegisterAcc = context.RegisterAcc
              .OrderByDescending(f => f.Id)
              .ToList();

            var Admin = context.Adminaccount
              .OrderByDescending(f => f.Id)
              .ToList();


            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalAssistance = hospitalBills,
                OtherAssistance = medicalLabForms,
                FuneralAssistance = FuneralAssistance,
                RegisterAcc = RegisterAcc,
                Adminaccount = Admin
            };

            // Pass the view model to the view
            return View(viewModel);
        }

        public IActionResult HospitalAssistanceview(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var HospitalAssistance = context.HospitalAssistance.Find(id);


            if (HospitalAssistance == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
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

            // Type of assistance and CMO details
            var typeAssistanceRaw = HospitalAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw = HospitalAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] = HospitalAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = HospitalAssistance.ValidBackimage;

            ViewData["DoctorPrescription"] = HospitalAssistance.DoctorPrescription;
            ViewData["DeathCertificate"] = HospitalAssistance.DeathCertificate;
            ViewData["Comments"] = HospitalAssistance.Comments;
            ViewData["Processby"] = HospitalAssistance.Processby;


            return View();
        }

        public IActionResult FuneralAssistanceview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Superadmin", "Superadmin");
            }

            var  FuneralAssistance = context.FuneralAssistance.Find(id);
    
            if ( FuneralAssistance == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Status"] =  FuneralAssistance.Status;
            ViewData["Id"] =  FuneralAssistance.Id;
            ViewData["Lastname"] =  FuneralAssistance.Lastname;
            ViewData["Firstname"] =  FuneralAssistance.Firstname;
            ViewData["Middlename"] =  FuneralAssistance.Middlename;
            ViewData["Suffix"] =  FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] =  FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] =  FuneralAssistance.SubVill;
            ViewData["Brgy"] =  FuneralAssistance.Brgy;
            ViewData["District"] =  FuneralAssistance.District;
            ViewData["Sex"] =  FuneralAssistance.Sex;
            ViewData["PhilHealth"] =  FuneralAssistance.PhilHealth;
            ViewData["PhilHealthNo"] =  FuneralAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] =  FuneralAssistance.Dateofbirth;
            ViewData["Age"] =  FuneralAssistance.Age;

            // Requestor details
            ViewData["RLastname"] =  FuneralAssistance.RLastname;
            ViewData["RFirstname"] =  FuneralAssistance.RFirstname;
            ViewData["RMiddlename"] =  FuneralAssistance.RMiddlename;
            ViewData["RSuffix"] =  FuneralAssistance.RSuffix;
            ViewData["RBlkLotStreet"] =  FuneralAssistance.RBlkLotStreet;
            ViewData["RSubVill"] =  FuneralAssistance.RSubVill;
            ViewData["RBrgy"] =  FuneralAssistance.RBrgy;
            ViewData["RDistrict"] =  FuneralAssistance.RDistrict;
            ViewData["RelationshipPatient"] =  FuneralAssistance.RelationshipPatient;
            ViewData["ContactNo"] =  FuneralAssistance.ContactNo;

            // Type of assistance and CMO details
            var typeAssistanceRaw =  FuneralAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw =  FuneralAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] =  FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] =  FuneralAssistance.ValidBackimage;

            ViewData["DoctorPrescription"] =  FuneralAssistance.DoctorPrescription;
            ViewData["DeathCertificate"] =  FuneralAssistance.DeathCertificate;
            ViewData["Comments"] =  FuneralAssistance.Comments;
            ViewData["Processby"] =  FuneralAssistance.Processby;
            return View();
        }

        public IActionResult OtherAssistanceview(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Superadmin", "Superadmin");
            }
            var    OtherAssistance = context.OtherAssistance.Find(id);

            if (   OtherAssistance == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Status"] =    OtherAssistance.Status;
            ViewData["Id"] =    OtherAssistance.Id;
            ViewData["Lastname"] =    OtherAssistance.Lastname;
            ViewData["Firstname"] =    OtherAssistance.Firstname;
            ViewData["Middlename"] =    OtherAssistance.Middlename;
            ViewData["Suffix"] =    OtherAssistance.Suffix;
            ViewData["BlkLotStreet"] =    OtherAssistance.BlkLotStreet;
            ViewData["SubVill"] =    OtherAssistance.SubVill;
            ViewData["Brgy"] =    OtherAssistance.Brgy;
            ViewData["District"] =    OtherAssistance.District;
            ViewData["Sex"] =    OtherAssistance.Sex;
            ViewData["PhilHealth"] =    OtherAssistance.PhilHealth;
            ViewData["PhilHealthNo"] =    OtherAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] =    OtherAssistance.Dateofbirth;
            ViewData["Age"] =    OtherAssistance.Age;

            // Requestor details
            ViewData["RLastname"] =    OtherAssistance.RLastname;
            ViewData["RFirstname"] =    OtherAssistance.RFirstname;
            ViewData["RMiddlename"] =    OtherAssistance.RMiddlename;
            ViewData["RSuffix"] =    OtherAssistance.RSuffix;
            ViewData["RBlkLotStreet"] =    OtherAssistance.RBlkLotStreet;
            ViewData["RSubVill"] =    OtherAssistance.RSubVill;
            ViewData["RBrgy"] =    OtherAssistance.RBrgy;
            ViewData["RDistrict"] =    OtherAssistance.RDistrict;
            ViewData["RelationshipPatient"] =    OtherAssistance.RelationshipPatient;
            ViewData["ContactNo"] =    OtherAssistance.ContactNo;

            // Type of assistance and CMO details
            var typeAssistanceRaw =    OtherAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw =    OtherAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] =    OtherAssistance.Validfrontimage;
            ViewData["ValidBackimage"] =    OtherAssistance.ValidBackimage;

            ViewData["DoctorPrescription"] =    OtherAssistance.DoctorPrescription;
            ViewData["DeathCertificate"] =    OtherAssistance.DeathCertificate;
            ViewData["Comments"] =    OtherAssistance.Comments;
            ViewData["Processby"] =    OtherAssistance.Processby;
            return View();
        }

        public IActionResult Choice()
        {                 
            return View(); 
        }

        public IActionResult Superadminchangepass()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Superadmin", "Superadmin");
            }

            ViewBag.Id = HttpContext.Session.GetString("UserId");
            ViewBag.Fullname = HttpContext.Session.GetString("AdminFullname"); 
            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.Email = HttpContext.Session.GetString("Email");
            return View();
        }

        [HttpPost]
        public IActionResult Superadminchangepass(int id, SuperadminaccountDto superadminaccountdto, string currentPassword)
        {
            var existingUser = context.Superadminaccount.FirstOrDefault(r => r.Id == id);

            if (existingUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Superadmin", "Superadmin");
            }

            // Validate password change only if new password is entered
            if (!string.IsNullOrWhiteSpace(superadminaccountdto.Password))
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is required to change your password.");
                    TempData["PasswordError"] = "Current password is required.";
                }
                else if (!BCrypt.Net.BCrypt.Verify(currentPassword, existingUser.Password))
                {
                    ModelState.AddModelError("CurrentPassword", "The current password you entered is incorrect.");
                    TempData["PasswordError"] = "Current password was wrong. Please try again.";
                    ViewBag.TriggerPasswordValidation = true;
                }
            }
            else
            {
                // Password not being changed � skip validation
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate form with original user data
                superadminaccountdto.Fullname = existingUser.Fullname;
                superadminaccountdto.Username = existingUser.Username;
                superadminaccountdto.Email = existingUser.Email;

                return View(superadminaccountdto);
            }

            try
            {
                // Update password if a new one was entered
                if (!string.IsNullOrWhiteSpace(superadminaccountdto.Password))
                {
                    existingUser.Password = BCrypt.Net.BCrypt.HashPassword(superadminaccountdto.Password);
                    TempData["SuccessMessage"] = "Your password has been updated successfully.";
                }

                context.SaveChanges();
                TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                return View(superadminaccountdto);
            }
        }

        public IActionResult Admincreateaccount()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Admincreateaccount(int Id, AdminaccountDto adminaccountdto)
        {
            if (!ModelState.IsValid)
            {
                return View(adminaccountdto);
            }

            // Hash the password using BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminaccountdto.Password);

            var adminAccount = new Adminaccount
            {
                Fullname = adminaccountdto.Fullname,
                Username = adminaccountdto.Username,
                Password = hashedPassword ,// Store the hashed password
                Status = "Active"
            };

            context.Adminaccount.Add(adminAccount);
            context.SaveChanges();

            return RedirectToAction("Superadmin"); // Change this to your actual list action

        }


        public IActionResult Users()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Superadmin", "Superadmin");
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
 
        public IActionResult RemoveUser(int id)
        {
            var register = context.Register.Find(id);
            if (register == null)
            {
                return RedirectToAction("Superadmin");
            }

            // Instead of deleting files and record, just update the status
            register.Status = "Removed";
            context.Register.Update(register);
            context.SaveChanges();

            return RedirectToAction("Superadmin");
        }


        public IActionResult RemoveAdminacc(int id)
        {
            var adminaccount = context.Adminaccount.Find(id);
            if (adminaccount == null)
            {
                return RedirectToAction("Superadmin");
            }

            // Instead of deleting files and record, just update the status
            adminaccount.Status = "Removed";
            context.Adminaccount.Update(adminaccount);
            context.SaveChanges();

            return RedirectToAction("Superadmin");
        }

    }
}
