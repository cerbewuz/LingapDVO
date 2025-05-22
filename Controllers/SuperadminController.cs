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

        public SuperadminController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
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
            var hospitalBills = context.FillupformHospitalBill
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.Medicalandlabform
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var funeralburialform = context.Funeralburialform
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var Register = context.Register
              .OrderByDescending(f => f.Id)
              .ToList();

            var Admin = context.Adminaccount
              .OrderByDescending(f => f.Id)
              .ToList();


            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalBills = hospitalBills,
                MedicalLabForms = medicalLabForms,
                Funeralburialform = funeralburialform,
                Register = Register,
                Adminaccount = Admin
            };

            // Pass the view model to the view
            return View(viewModel);
        }

        public IActionResult Fillupformhospitalbillview(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);


            if (fillupformhospitalBill == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
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
            ViewData["Comments"] = fillupformhospitalBill.Comments;
            ViewData["Processby"] = fillupformhospitalBill.Processby;


            return View();
        }

        public IActionResult Funeralburialformview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Superadmin", "Superadmin");
            }

            var  funeralburialform = context.Funeralburialform.Find(id);
    
            if ( funeralburialform == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Status"] =  funeralburialform.Status;
            ViewData["Id"] =  funeralburialform.Id;
            ViewData["Lastname"] =  funeralburialform.Lastname;
            ViewData["Firstname"] =  funeralburialform.Firstname;
            ViewData["Middlename"] =  funeralburialform.Middlename;
            ViewData["Suffix"] =  funeralburialform.Suffix;
            ViewData["BlkLotStreet"] =  funeralburialform.BlkLotStreet;
            ViewData["SubVill"] =  funeralburialform.SubVill;
            ViewData["Brgy"] =  funeralburialform.Brgy;
            ViewData["District"] =  funeralburialform.District;
            ViewData["Sex"] =  funeralburialform.Sex;
            ViewData["PhilHealth"] =  funeralburialform.PhilHealth;
            ViewData["PhilHealthNo"] =  funeralburialform.PhilHealthNo;
            ViewData["Dateofbirth"] =  funeralburialform.Dateofbirth;
            ViewData["Age"] =  funeralburialform.Age;

            // Requestor details
            ViewData["RLastname"] =  funeralburialform.RLastname;
            ViewData["RFirstname"] =  funeralburialform.RFirstname;
            ViewData["RMiddlename"] =  funeralburialform.RMiddlename;
            ViewData["RSuffix"] =  funeralburialform.RSuffix;
            ViewData["RBlkLotStreet"] =  funeralburialform.RBlkLotStreet;
            ViewData["RSubVill"] =  funeralburialform.RSubVill;
            ViewData["RBrgy"] =  funeralburialform.RBrgy;
            ViewData["RDistrict"] =  funeralburialform.RDistrict;
            ViewData["RelationshipPatient"] =  funeralburialform.RelationshipPatient;
            ViewData["ContactNo"] =  funeralburialform.ContactNo;

            // Type of assistance and CMO details
            var typeAssistanceRaw =  funeralburialform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw =  funeralburialform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] =  funeralburialform.Validfrontimage;
            ViewData["ValidBackimage"] =  funeralburialform.ValidBackimage;

            ViewData["DoctorPrescription"] =  funeralburialform.DoctorPrescription;
            ViewData["DeathCertificate"] =  funeralburialform.DeathCertificate;
            ViewData["Comments"] =  funeralburialform.Comments;
            ViewData["Processby"] =  funeralburialform.Processby;
            return View();
        }

        public IActionResult Medicalandlabformview(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Superadmin", "Superadmin");
            }
            var    medicalandlabform = context.Medicalandlabform.Find(id);

            if (   medicalandlabform == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Status"] =    medicalandlabform.Status;
            ViewData["Id"] =    medicalandlabform.Id;
            ViewData["Lastname"] =    medicalandlabform.Lastname;
            ViewData["Firstname"] =    medicalandlabform.Firstname;
            ViewData["Middlename"] =    medicalandlabform.Middlename;
            ViewData["Suffix"] =    medicalandlabform.Suffix;
            ViewData["BlkLotStreet"] =    medicalandlabform.BlkLotStreet;
            ViewData["SubVill"] =    medicalandlabform.SubVill;
            ViewData["Brgy"] =    medicalandlabform.Brgy;
            ViewData["District"] =    medicalandlabform.District;
            ViewData["Sex"] =    medicalandlabform.Sex;
            ViewData["PhilHealth"] =    medicalandlabform.PhilHealth;
            ViewData["PhilHealthNo"] =    medicalandlabform.PhilHealthNo;
            ViewData["Dateofbirth"] =    medicalandlabform.Dateofbirth;
            ViewData["Age"] =    medicalandlabform.Age;

            // Requestor details
            ViewData["RLastname"] =    medicalandlabform.RLastname;
            ViewData["RFirstname"] =    medicalandlabform.RFirstname;
            ViewData["RMiddlename"] =    medicalandlabform.RMiddlename;
            ViewData["RSuffix"] =    medicalandlabform.RSuffix;
            ViewData["RBlkLotStreet"] =    medicalandlabform.RBlkLotStreet;
            ViewData["RSubVill"] =    medicalandlabform.RSubVill;
            ViewData["RBrgy"] =    medicalandlabform.RBrgy;
            ViewData["RDistrict"] =    medicalandlabform.RDistrict;
            ViewData["RelationshipPatient"] =    medicalandlabform.RelationshipPatient;
            ViewData["ContactNo"] =    medicalandlabform.ContactNo;

            // Type of assistance and CMO details
            var typeAssistanceRaw =    medicalandlabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw =    medicalandlabform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] =    medicalandlabform.Validfrontimage;
            ViewData["ValidBackimage"] =    medicalandlabform.ValidBackimage;

            ViewData["DoctorPrescription"] =    medicalandlabform.DoctorPrescription;
            ViewData["DeathCertificate"] =    medicalandlabform.DeathCertificate;
            ViewData["Comments"] =    medicalandlabform.Comments;
            ViewData["Processby"] =    medicalandlabform.Processby;
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
                // Password not being changed — skip validation
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
