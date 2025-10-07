using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

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
                return Redirect("/Admin");
            }

            try
            {
                // Update status and comments
                fillupformhospitalBill.Status = fillupformHospitalbilldto.Status;
                fillupformhospitalBill.ForCMOPERSONNEL = fillupformHospitalbilldto.ForCMOPERSONNEL;
                fillupformhospitalBill.Comments = fillupformHospitalbilldto.Comments;
                fillupformhospitalBill.Processby = fillupformHospitalbilldto.Processby;
                fillupformhospitalBill.ProcessAt = DateTime.Now;
                context.SaveChanges();

                // Get the user's email using UserId
                var user = context.Useraccount.FirstOrDefault(u => u.Id == fillupformhospitalBill.UserId);
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
                return Redirect("/Admin");
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

        public IActionResult Analyticsdashboard()
        {
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

        // ====================================
        // COMPLETE HOSPITAL BILL CONTROLLER - FIXED VERSION
        // ====================================

        // 1. DECRYPTION HELPER METHOD
        private byte[] DecryptFile(string encryptedFilePath, string masterPassword)
        {
            byte[] encryptedData = System.IO.File.ReadAllBytes(encryptedFilePath);
            using var memoryStream = new MemoryStream(encryptedData);

            byte[] salt = new byte[16];
            memoryStream.Read(salt, 0, salt.Length);

            using var pbkdf2 = new Rfc2898DeriveBytes(masterPassword, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(32);

            byte[] iv = new byte[16];
            memoryStream.Read(iv, 0, iv.Length);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptedStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(decryptedStream);
            }

            return decryptedStream.ToArray();
        }

        // PDF DETECTION HELPER METHOD
        private bool IsPdfFile(byte[] data)
        {
            try
            {
                // Check PDF magic number (%PDF)
                if (data.Length >= 4 && data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46)
                    return true;

                // Also check for PDF in text (case insensitive)
                if (data.Length >= 1000)
                {
                    string beginning = Encoding.UTF8.GetString(data, 0, Math.Min(1000, data.Length));
                    if (beginning.Contains("%PDF") || beginning.Contains("%pdf"))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // 2. MAIN VIEW METHOD - WITH PROPER PDF DETECTION
        public IActionResult FillupformHospitalBillUpdateprocessingstatus(int id)
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

            // Basic ViewData setup
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

            // Type of assistance
            var typeAssistanceRaw = fillupformhospitalBill.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = fillupformhospitalBill.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION WITH PROPER PDF DETECTION
            // ====================================
            string masterPassword = "SuperAdminMasterKey123!";
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(fillupformhospitalBill.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, fillupformhospitalBill.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath, masterPassword);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(fillupformhospitalBill.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, fillupformhospitalBill.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath, masterPassword);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - FIXED PDF DETECTION
                if (!string.IsNullOrEmpty(fillupformhospitalBill.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, fillupformhospitalBill.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {fillupformhospitalBill.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath, masterPassword);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;

                            // ⭐⭐⭐ PROPER PDF DETECTION ⭐⭐⭐
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"✅ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"🔍 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Doctor Prescription in database");
                }

                // ⭐ DEATH CERTIFICATE - FIXED PDF DETECTION
                if (!string.IsNullOrEmpty(fillupformhospitalBill.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, fillupformhospitalBill.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {fillupformhospitalBill.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath, masterPassword);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                            // ⭐⭐⭐ PROPER PDF DETECTION ⭐⭐⭐
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"✅ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"🔍 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"❌ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = fillupformhospitalBill.Validfrontimage;
            ViewData["ValidBackimage"] = fillupformhospitalBill.ValidBackimage;
            ViewData["Comments"] = fillupformhospitalBill.Comments;

            return View();
        }

        // 3. DOWNLOAD METHOD - IMPROVED CONTENT TYPE DETECTION
        [HttpGet]
        public IActionResult DownloadDecryptedImage(string fileName, string fileType = "validid")
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            string masterPassword = "SuperAdminMasterKey123!";
            string folder;

            switch (fileType.ToLower())
            {
                case "doctorprescription":
                    folder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    break;
                case "deathcertificate":
                    folder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    break;
                default:
                    folder = Path.Combine(environment.WebRootPath, "Validimg");
                    break;
            }

            string filePath = Path.Combine(folder, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            try
            {
                byte[] decryptedData = DecryptFile(filePath, masterPassword);
                string downloadName = fileName.Replace(".enc", "");

                // ⭐ IMPROVED CONTENT TYPE DETECTION
                string contentType;
                if (IsPdfFile(decryptedData))
                {
                    contentType = "application/pdf";
                    // Ensure download name has .pdf extension
                    if (!downloadName.ToLower().EndsWith(".pdf"))
                        downloadName += ".pdf";
                }
                else
                {
                    // Fallback to file extension detection for images
                    contentType = fileName.ToLower().Contains(".png") ? "image/png" :
                                 fileName.ToLower().Contains(".jpg") || fileName.ToLower().Contains(".jpeg") ? "image/jpeg" :
                                 "application/octet-stream";
                }

                return File(decryptedData, contentType, downloadName);
            }
            catch (Exception ex)
            {
                return BadRequest("Decryption failed: " + ex.Message);
            }
        }

        // 4. VIEW PDF METHOD - FIXED PATH ISSUE
        [HttpGet]
        public IActionResult ViewPDF(string fileName, string fileType)
        {
            try
            {
                Console.WriteLine($"🔍 ViewPDF called - FileName: {fileName}, FileType: {fileType}");

                if (string.IsNullOrEmpty(fileName))
                {
                    Console.WriteLine("❌ FileName is null or empty");
                    return BadRequest("FileName is required");
                }

                if (string.IsNullOrEmpty(fileType))
                {
                    Console.WriteLine("❌ FileType is null or empty");
                    return BadRequest("FileType is required");
                }

                // Define the directory based on file type
                string folderPath = fileType.ToLower() switch
                {
                    "doctorprescription" => Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage"),
                    "deathcertificate" => Path.Combine(environment.WebRootPath, "Funeralimg"),
                    _ => Path.Combine(environment.WebRootPath, "Validimg")
                };

                Console.WriteLine($"📁 Folder path: {folderPath}");

                string encryptedFilePath = Path.Combine(folderPath, fileName);
                Console.WriteLine($"📄 Full file path: {encryptedFilePath}");

                // Check if file exists
                if (!System.IO.File.Exists(encryptedFilePath))
                {
                    Console.WriteLine($"❌ File does not exist: {encryptedFilePath}");
                    return NotFound($"File not found: {fileName}");
                }

                Console.WriteLine($"✅ File exists. Size: {new FileInfo(encryptedFilePath).Length} bytes");

                // Decrypt the file
                string masterPassword = "SuperAdminMasterKey123!";
                byte[] decryptedBytes = DecryptFile(encryptedFilePath, masterPassword);
                Console.WriteLine($"✅ File decrypted. Decrypted size: {decryptedBytes.Length} bytes");

                // Check if it's a PDF
                bool isPdf = IsPdfFile(decryptedBytes);
                Console.WriteLine($"📊 Is PDF: {isPdf}");

                // Always return as PDF for now
                string downloadName = Path.GetFileNameWithoutExtension(fileName) + ".pdf";
                Console.WriteLine($"📤 Returning file as: {downloadName}");

                return File(decryptedBytes, "application/pdf", downloadName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ERROR in ViewPDF: {ex.Message}");
                Console.WriteLine($"💥 Stack Trace: {ex.StackTrace}");
                return BadRequest($"Error: {ex.Message}");
            }
        }



        // 5. QUICK PDF CHECK METHOD (Optional helper)
        [HttpGet]
        public IActionResult CheckFileType(string fileName, string fileType = "validid")
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return Unauthorized();
            }

            string masterPassword = "SuperAdminMasterKey123!";
            string folder;

            switch (fileType.ToLower())
            {
                case "doctorprescription":
                    folder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    break;
                case "deathcertificate":
                    folder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    break;
                default:
                    folder = Path.Combine(environment.WebRootPath, "Validimg");
                    break;
            }

            string filePath = Path.Combine(folder, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return Json(new { exists = false, isPdf = false });
            }

            try
            {
                byte[] decryptedData = DecryptFile(filePath, masterPassword);
                bool isPdf = IsPdfFile(decryptedData);

                return Json(new
                {
                    exists = true,
                    isPdf = isPdf,
                    fileSize = decryptedData.Length
                });
            }
            catch (Exception ex)
            {
                return Json(new { exists = true, isPdf = false, error = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult FillupformHospitalBillUpdateprocessingstatus(int id, FillupformHospitalBillDto fillupformHospitalbilldto)
        {
            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);

            if (fillupformhospitalBill == null)
            {
                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update status and comments
                fillupformhospitalBill.Status2 = fillupformHospitalbilldto.Status2;
                fillupformhospitalBill.ForCMOPERSONNEL = fillupformHospitalbilldto.ForCMOPERSONNEL;
                fillupformhospitalBill.Comments = fillupformHospitalbilldto.Comments;
                fillupformhospitalBill.Processby = fillupformHospitalbilldto.Processby;
                fillupformhospitalBill.Result = DateTime.Now;
                context.SaveChanges();

                // Get the user's email using UserId
                var user = context.Useraccount.FirstOrDefault(u => u.Id == fillupformhospitalBill.UserId);
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
                }
                ;

                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(fillupformHospitalbilldto);
            }
        }


    }


}
