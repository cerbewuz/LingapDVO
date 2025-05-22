using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;

namespace LingapDVO.Controllers
{
    public class Adminuser : Controller
    {

        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration _configuration;

        public Adminuser(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            this.context = context;
            this.environment = environment;
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Admin()
        {

            // Prevent caching
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            // Check session


            // More robust session check
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Landingpage", "Dashboard"); // Redirect to your login page
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

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalBills = hospitalBills,
                MedicalLabForms = medicalLabForms,
                Funeralburialform = funeralburialform
            };

            // Pass the view model to the view
            return View(viewModel);
        }

        public IActionResult FillupformHospitalBillUpdatestatus(int id)
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


            return View();

        }


        [HttpPost]
        public IActionResult FillupformHospitalBillupdatestatus(int id, FillupformHospitalBillDto fillupformHospitalbilldto)
        {
            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);

            if (fillupformhospitalBill == null)
            {
                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return RedirectToAction();
            }

            try
            {
                // Update status and comments
                fillupformhospitalBill.Status = fillupformHospitalbilldto.Status;
                fillupformhospitalBill.Comments = fillupformHospitalbilldto.Comments;
                fillupformhospitalBill.Processby = fillupformHospitalbilldto.Processby;
                context.SaveChanges();

                // Get the user's email using UserId
                var user = context.Register.FirstOrDefault(u => u.Id == fillupformhospitalBill.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings from configuration
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    // Null check for email settings
                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                    {
                        throw new ArgumentException("Email address or display name is missing.");
                    }

                    // Compose and send the email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, "User");

                    string subject = "Hospital Bill Assistance Update";
                    string body = $"Your hospital bill status has been updated.\n\nStatus: {fillupformHospitalbilldto.Status}\nComments: {fillupformHospitalbilldto.Comments}";

                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    };

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                };

                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return RedirectToAction();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(fillupformHospitalbilldto);
            }
        }

        public IActionResult Medicalandlabformstatus(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
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


            return View();

        }


        [HttpPost]
        public IActionResult Medicalandlabformstatus(int id, MedicalandlabformDto medicalandlabformDto)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicalandlabform = context.Medicalandlabform.Find(id);

            if (medicalandlabform == null)
            {
                return RedirectToAction();
            }

            try
            {
                // Update status and comments
                medicalandlabform.Status = medicalandlabformDto.Status;
                medicalandlabform.Comments = medicalandlabformDto.Comments;
                medicalandlabform.Processby = medicalandlabformDto.Processby;
                context.SaveChanges();

                // Get the user's email using UserId
                var user = context.Register.FirstOrDefault(u => u.Id == medicalandlabform.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings from configuration
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    // Null check for email settings
                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                    {
                        throw new ArgumentException("Email address or display name is missing.");
                    }

                    // Compose and send the email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, "User");

                    string subject = "Medical and laboratory Procedure Update";
                    string body = $"Your Medical and laboratory Procedure status has been updated.\n\nStatus: {medicalandlabformDto.Status}\nComments: {medicalandlabformDto.Comments}";

                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    };

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }

                return RedirectToAction();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(medicalandlabformDto);
            }
        }

        public IActionResult Funeralburialformstatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
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


            return View();

        }

        [HttpPost]
        public IActionResult Funeralburialformstatus(int id, FuneralburialformDto funeralburialformDto)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }


            var funeralburialform = context.Funeralburialform.Find(id);

            if (funeralburialform == null)
            {
                return RedirectToAction();
            }

            try
            {
                // Update status and comments
                funeralburialform.Status = funeralburialformDto.Status;
                funeralburialform.Comments = funeralburialformDto.Comments;
                funeralburialform.Processby = funeralburialformDto.Processby;
                context.SaveChanges();

                // Get the user's email using UserId
                var user = context.Register.FirstOrDefault(u => u.Id == funeralburialform.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings from configuration
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    // Null check for email settings
                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                    {
                        throw new ArgumentException("Email address or display name is missing.");
                    }

                    // Compose and send the email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, "User");

                    string subject = "Funeral and burial assistance update";
                    string body = $"Your Funeral and burial assistance status has been updated.\n\nStatus: {funeralburialformDto.Status}\nComments: {funeralburialformDto.Comments}";

                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    };

                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }

                return RedirectToAction();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(funeralburialformDto);
            }
        }



    }


}
