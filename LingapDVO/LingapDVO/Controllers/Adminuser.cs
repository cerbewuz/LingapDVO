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
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace LingapDVO.Controllers
{
    public class Adminuser : Controller
    {

        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration _configuration;
        private readonly ISessionConfigurationService _sessionConfig;
        private readonly IMultiChannelNotificationService _notificationService;
        private readonly PriorityTrackingService _priorityService;

        public Adminuser(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration, ISessionConfigurationService sessionConfig, IMultiChannelNotificationService notificationService, PriorityTrackingService priorityService)
        {
            this.context = context;
            this.environment = environment;
            _configuration = configuration;
            _sessionConfig = sessionConfig;
            _notificationService = notificationService;
            _priorityService = priorityService;
        }
        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> Admin()
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
            var hospitalBills = context.HospitalAssistance
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.OtherAssistance
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var FuneralAssistance = context.FuneralAssistance
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Get priority counts
            var (highPriority, mediumPriority, totalPriority) = await _priorityService.GetPriorityCountsAsync();

            // Pass counts to view via ViewBag
            ViewBag.HighPriorityCount = highPriority;
            ViewBag.MediumPriorityCount = mediumPriority;
            ViewBag.TotalPriorityCount = totalPriority;

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalAssistance = hospitalBills,
                OtherAssistance = medicalLabForms,
                FuneralAssistance = FuneralAssistance
            };

            // Pass the view model to the view
            return View(viewModel);
        }


        public IActionResult HospitalAssistancePendingStatus(int id)
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


            return View();

        }

        //renvic edit sa grammar
        [HttpPost]
        public IActionResult HospitalAssistancePendingStatus(int id, HospitalAssistanceDto HospitalAssistanceDto)
        {
            var HospitalAssistance = context.HospitalAssistance.Find(id);

            if (HospitalAssistance == null)
            {
                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Automatically set status to "Processing"
                HospitalAssistance.Status = "Processing";
                HospitalAssistance.ForCMOPERSONNEL = HospitalAssistanceDto.ForCMOPERSONNEL;
                HospitalAssistance.Comments = HospitalAssistanceDto.Comments;
                HospitalAssistance.Processby = HospitalAssistanceDto.Processby;
                HospitalAssistance.ProcessAt = DateTime.Now;

                context.SaveChanges();

                // Send multi-channel notification (In-App, SMS, Email based on preferences)
                var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == HospitalAssistance.UserId);
                var applicantName = verifyAccount?.Firstname ?? "Applicant";
                _ = _notificationService.SendStatusChangeNotificationAsync(
                    HospitalAssistance.UserId,
                    applicantName,
                    "HospitalBill",
                    "Processing",
                    HospitalAssistance.Id
                );

                // Get the user's info from RegisterAcc
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == HospitalAssistance.UserId);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get user's first name from VerifyAccount (reuse existing verifyAccount variable)
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // Get email settings from configuration
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Compose the email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, firstName);

                    string subject = "Hospital Bill Assistance Update - LINGAP DVO";
                    string body = $@"
                Dear {firstName},

                Your Hospital Bill Assistance application is now being processed.

                APPLICATION DETAILS:
                � Application Type: Hospital Bill Assistance
                � Status: Processing
                � Date Updated: {DateTime.Now:MMMM dd, yyyy HH:mm tt}

                REMARKS:
                {HospitalAssistanceDto.Comments ?? "N/A"}

                Thank you for your patience. We will notify you once your application status is updated.

                Sincerely,
                {fromName}
                LINGAP DVO Medical Assistance Program";

                    // Send the email safely
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    })
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }

                TempData["SuccessMessage"] = "Hospital bill status set to 'Processing' and email sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(HospitalAssistanceDto);
            }
        }



        public IActionResult OtherAssistancePendingStatus(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var OtherAssistance = context.OtherAssistance.Find(id);


            if (OtherAssistance == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Status"] = OtherAssistance.Status;
            ViewData["Id"] = OtherAssistance.Id;
            ViewData["Lastname"] = OtherAssistance.Lastname;
            ViewData["Firstname"] = OtherAssistance.Firstname;
            ViewData["Middlename"] = OtherAssistance.Middlename;
            ViewData["Suffix"] = OtherAssistance.Suffix;
            ViewData["BlkLotStreet"] = OtherAssistance.BlkLotStreet;
            ViewData["SubVill"] = OtherAssistance.SubVill;
            ViewData["Brgy"] = OtherAssistance.Brgy;
            ViewData["District"] = OtherAssistance.District;
            ViewData["Sex"] = OtherAssistance.Sex;
            ViewData["PhilHealth"] = OtherAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = OtherAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = OtherAssistance.Dateofbirth;
            ViewData["Age"] = OtherAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = OtherAssistance.RLastname;
            ViewData["RFirstname"] = OtherAssistance.RFirstname;
            ViewData["RMiddlename"] = OtherAssistance.RMiddlename;
            ViewData["RSuffix"] = OtherAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = OtherAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = OtherAssistance.RSubVill;
            ViewData["RBrgy"] = OtherAssistance.RBrgy;
            ViewData["RDistrict"] = OtherAssistance.RDistrict;
            ViewData["RelationshipPatient"] = OtherAssistance.RelationshipPatient;
            ViewData["ContactNo"] = OtherAssistance.ContactNo;

            // Type of assistance and CMO details
            var typeAssistanceRaw = OtherAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw = OtherAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] = OtherAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = OtherAssistance.ValidBackimage;

            ViewData["DoctorPrescription"] = OtherAssistance.DoctorPrescription;
            ViewData["DeathCertificate"] = OtherAssistance.DeathCertificate;
            ViewData["Comments"] = OtherAssistance.Comments;


            return View();

        }



        [HttpPost]
        public IActionResult OtherAssistancePendingStatus(int id, OtherAssistanceDto OtherAssistanceDto) // ← Change method name
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var OtherAssistance = context.OtherAssistance.Find(id);

            if (OtherAssistance == null)
            {
                TempData["ErrorMessage"] = "Medical and laboratory record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Automatically set status to "Processing"
                OtherAssistance.Status = "Processing";
                OtherAssistance.Comments = OtherAssistanceDto.Comments;
                OtherAssistance.Processby = OtherAssistanceDto.Processby;
                OtherAssistance.ProcessAt = DateTime.Now;

                context.SaveChanges();

                // Send multi-channel notification
                var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == OtherAssistance.UserId);
                var applicantName = verifyAccount?.Firstname ?? "Applicant";
                _ = _notificationService.SendStatusChangeNotificationAsync(
                    OtherAssistance.UserId,
                    applicantName,
                    "Medical",
                    "Processing",
                    OtherAssistance.Id
                );

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == OtherAssistance.UserId);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get user's first name from VerifyAccount table
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Prepare email content
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, firstName);

                    string subject = "Medical and Laboratory Assistance Update - LINGAP DVO";
                    string body = $@"
            Dear {firstName},

            Your Medical and Laboratory Assistance application is now being processed.

            APPLICATION DETAILS:
            • Application Type: Medical and Laboratory Assistance
            • Status: Processing
            • Date Updated: {DateTime.Now:MMMM dd, yyyy HH:mm tt}

            REMARKS:
            {OtherAssistanceDto.Comments ?? "N/A"}

            Thank you for your patience. We will notify you once your application status has been updated.

            Sincerely,
            {fromName}
            LINGAP DVO Medical Assistance Program";

                    // Send email
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    })
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }

                TempData["SuccessMessage"] = "Medical and laboratory status set to 'Processing' and email sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(OtherAssistanceDto);
            }
        }



        public IActionResult FuneralAssistancePendingStatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var FuneralAssistance = context.FuneralAssistance.Find(id);


            if (FuneralAssistance == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Status"] = FuneralAssistance.Status;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
            ViewData["District"] = FuneralAssistance.District;
            ViewData["Sex"] = FuneralAssistance.Sex;
            ViewData["PhilHealth"] = FuneralAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = FuneralAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = FuneralAssistance.Dateofbirth;
            ViewData["Age"] = FuneralAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = FuneralAssistance.RLastname;
            ViewData["RFirstname"] = FuneralAssistance.RFirstname;
            ViewData["RMiddlename"] = FuneralAssistance.RMiddlename;
            ViewData["RSuffix"] = FuneralAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = FuneralAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = FuneralAssistance.RSubVill;
            ViewData["RBrgy"] = FuneralAssistance.RBrgy;
            ViewData["RDistrict"] = FuneralAssistance.RDistrict;
            ViewData["RelationshipPatient"] = FuneralAssistance.RelationshipPatient;
            ViewData["ContactNo"] = FuneralAssistance.ContactNo;

            // Type of assistance and CMO details
            var typeAssistanceRaw = FuneralAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;

            // Parse checkbox values into a Dictionary<string, string>
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedAssistance"] = parsed; // Pass dictionary to the view


            // ForCMOPERSONNEL handling
            var cmoPersonnelRaw = FuneralAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;

            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");

            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;

            ViewData["DoctorPrescription"] = FuneralAssistance.DoctorPrescription;
            ViewData["DeathCertificate"] = FuneralAssistance.DeathCertificate;
            ViewData["Comments"] = FuneralAssistance.Comments;


            return View();

        }

        // Renvic edit sa grammar
        [HttpPost]
        public IActionResult FuneralAssistancePendingStatus(int id, FuneralAssistanceDto FuneralAssistanceDto) // ← Change method name
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var FuneralAssistance = context.FuneralAssistance.Find(id);

            if (FuneralAssistance == null)
            {
                TempData["ErrorMessage"] = "Funeral and burial record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // ? Automatically set status to "Processing"
                FuneralAssistance.Status = "Processing";
                FuneralAssistance.Comments = FuneralAssistanceDto.Comments;
                FuneralAssistance.Processby = FuneralAssistanceDto.Processby;
                FuneralAssistance.ProcessAt = DateTime.Now;

                context.SaveChanges();

                // Send multi-channel notification
                var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == FuneralAssistance.UserId);
                var applicantName = verifyAccount?.Firstname ?? "Applicant";
                _ = _notificationService.SendStatusChangeNotificationAsync(
                    FuneralAssistance.UserId,
                    applicantName,
                    "Funeral",
                    "Processing",
                    FuneralAssistance.Id
                );

                // ? Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == FuneralAssistance.UserId);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // ? Get user's first name from VerifyAccount table (reuse existing verifyAccount variable)
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // ? Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // ? Prepare email content
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, firstName);

                    string subject = "Funeral and Burial Assistance Update - LINGAP DVO";
                    string body = $@"
            Dear {firstName},

            Your Funeral and Burial Assistance application is now being processed.

            APPLICATION DETAILS:
            • Application Type: Funeral and Burial Assistance
            • Status: Processing
            • Date Updated: {DateTime.Now:MMMM dd, yyyy HH:mm tt}

            REMARKS:
            {FuneralAssistanceDto.Comments ?? "N/A"}

            Thank you for your patience. We will notify you once your application status has been updated.

            Sincerely,
            {fromName}
            LINGAP DVO Funeral and Burial Assistance Program";

                    // ? Send email
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    })
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }

                TempData["SuccessMessage"] = "Funeral and burial status set to 'Processing' and email sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(FuneralAssistanceDto);
            }
        }

        public async Task<IActionResult> Analyticsdashboard()
        {
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

            // Get priority counts
            var (highPriority, mediumPriority, totalPriority) = await _priorityService.GetPriorityCountsAsync();

            // Pass counts to view via ViewBag
            ViewBag.HighPriorityCount = highPriority;
            ViewBag.MediumPriorityCount = mediumPriority;
            ViewBag.TotalPriorityCount = totalPriority;

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalAssistance = hospitalBills,
                OtherAssistance = medicalLabForms,
                FuneralAssistance = FuneralAssistance
            };

            // Pass the view model to the view
            return View(viewModel);
        }
        // ====================================
        // COMPLETE HOSPITAL BILL CONTROLLER - WITH EMBEDDED AES ENCRYPTION HELPER
        // ====================================

        //---------------------------------------------------------------------------//
        //                     AES-256 ENCRYPTION HELPER CLASS                       //
        //          Secure AES-256 Implementation using Configuration                //
        //---------------------------------------------------------------------------//
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

            public string Encrypt(string plainText)
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

                using var aes = Aes.Create();
                aes.Key = _aesKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var memoryStream = new MemoryStream();

                memoryStream.Write(aes.IV, 0, aes.IV.Length);

                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(plainText);
                }

                byte[] encryptedData = memoryStream.ToArray();
                return Convert.ToBase64String(encryptedData);
            }

            public string Decrypt(string encryptedText)
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                using var aes = Aes.Create();
                aes.Key = _aesKey;

                byte[] iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.IV = iv;

                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(encryptedBytes, 16, encryptedBytes.Length - 16);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);

                return reader.ReadToEnd();
            }

            public byte[] EncryptStream(Stream inputStream)
            {
                using var aes = Aes.Create();
                aes.Key = _aesKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var memoryStream = new MemoryStream();
                memoryStream.Write(aes.IV, 0, aes.IV.Length);

                using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    inputStream.CopyTo(cryptoStream);
                }

                return memoryStream.ToArray();
            }

            public string EncryptTimestamp(string timestamp)
            {
                using var aes = Aes.Create();
                aes.Key = _aesKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                byte[] inputBytes = Encoding.UTF8.GetBytes(timestamp);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);

                return Convert.ToBase64String(encryptedBytes);
            }
        }

        // 1. DECRYPTION HELPER METHOD USING CONFIGURATION-BASED AES KEY
        private byte[] DecryptFile(string encryptedFilePath)
        {
            byte[] encryptedData = System.IO.File.ReadAllBytes(encryptedFilePath);
            using var memoryStream = new MemoryStream(encryptedData);

            // Read IV from the beginning of the file (first 16 bytes)
            byte[] iv = new byte[16];
            memoryStream.Read(iv, 0, iv.Length);

            // Use configuration-based AES helper
            var aesHelper = new AesEncryptionHelper(_configuration);

            // Get the key using reflection
            var keyField = typeof(AesEncryptionHelper).GetField("_aesKey",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (keyField == null)
                throw new InvalidOperationException("Cannot access AES key");

            byte[] key = (byte[])keyField.GetValue(aesHelper);

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

        // 2. MAIN VIEW METHOD - UPDATED TO USE CONFIGURATION-BASED KEY
        public IActionResult HospitalAssistanceProcessingStatus(int id)
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

            // Basic ViewData setup
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

            // Type of assistance
            var typeAssistanceRaw = HospitalAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = HospitalAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(HospitalAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, HospitalAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(HospitalAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, HospitalAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, HospitalAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {HospitalAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = HospitalAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? DEATH CERTIFICATE - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, HospitalAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {HospitalAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = HospitalAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = HospitalAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = HospitalAssistance.ValidBackimage;
            ViewData["Comments"] = HospitalAssistance.Comments;

            return View();
        }

        public IActionResult OtherAssistanceProcessingStatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.OtherAssistance.Find(id);
            if (medicallabform == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = medicallabform.Status;
            ViewData["Id"] = medicallabform.Id;
            ViewData["Lastname"] = medicallabform.Lastname;
            ViewData["Firstname"] = medicallabform.Firstname;
            ViewData["Middlename"] = medicallabform.Middlename;
            ViewData["Suffix"] = medicallabform.Suffix;
            ViewData["BlkLotStreet"] = medicallabform.BlkLotStreet;
            ViewData["SubVill"] = medicallabform.SubVill;
            ViewData["Brgy"] = medicallabform.Brgy;
            ViewData["District"] = medicallabform.District;
            ViewData["Sex"] = medicallabform.Sex;
            ViewData["PhilHealth"] = medicallabform.PhilHealth;
            ViewData["PhilHealthNo"] = medicallabform.PhilHealthNo;
            ViewData["Dateofbirth"] = medicallabform.Dateofbirth;
            ViewData["Age"] = medicallabform.Age;

            // Requestor details
            ViewData["RLastname"] = medicallabform.RLastname;
            ViewData["RFirstname"] = medicallabform.RFirstname;
            ViewData["RMiddlename"] = medicallabform.RMiddlename;
            ViewData["RSuffix"] = medicallabform.RSuffix;
            ViewData["RBlkLotStreet"] = medicallabform.RBlkLotStreet;
            ViewData["RSubVill"] = medicallabform.RSubVill;
            ViewData["RBrgy"] = medicallabform.RBrgy;
            ViewData["RDistrict"] = medicallabform.RDistrict;
            ViewData["RelationshipPatient"] = medicallabform.RelationshipPatient;
            ViewData["ContactNo"] = medicallabform.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = medicallabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = medicallabform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "MedCertificateimage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(medicallabform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, medicallabform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(medicallabform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, medicallabform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = medicallabform.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"?? Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"?? Full path: {medicalPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(medicalPath)}");

                    if (System.IO.File.Exists(medicalPath))
                    {
                        try
                        {
                            byte[] decryptedMedical = DecryptFile(medicalPath);
                            ViewData["MedicalCertificateBase64"] = Convert.ToBase64String(decryptedMedical);
                            ViewData["MedicalCertificate"] = medicallabform.MedCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedMedical);
                            ViewData["IsMedicalCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"?? IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Medical Certificate in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = medicallabform.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            return View();
        }
        public IActionResult FuneralAssistanceProcessingStatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var FuneralAssistance = context.FuneralAssistance.Find(id);
            if (FuneralAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status2"] = FuneralAssistance.Status2;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
            ViewData["District"] = FuneralAssistance.District;
            ViewData["Sex"] = FuneralAssistance.Sex;
            ViewData["PhilHealth"] = FuneralAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = FuneralAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = FuneralAssistance.Dateofbirth;
            ViewData["Age"] = FuneralAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = FuneralAssistance.RLastname;
            ViewData["RFirstname"] = FuneralAssistance.RFirstname;
            ViewData["RMiddlename"] = FuneralAssistance.RMiddlename;
            ViewData["RSuffix"] = FuneralAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = FuneralAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = FuneralAssistance.RSubVill;
            ViewData["RBrgy"] = FuneralAssistance.RBrgy;
            ViewData["RDistrict"] = FuneralAssistance.RDistrict;
            ViewData["RelationshipPatient"] = FuneralAssistance.RelationshipPatient;
            ViewData["ContactNo"] = FuneralAssistance.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = FuneralAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = FuneralAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(FuneralAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, FuneralAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(FuneralAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, FuneralAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, FuneralAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {FuneralAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = FuneralAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, FuneralAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {FuneralAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = FuneralAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            ViewData["Comments"] = FuneralAssistance.Comments;

            return View();
        }

        //for approving statuses
        public IActionResult HospitalAssistanceApproveStatus(int id)
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

            // Basic ViewData setup
            ViewData["Status2"] = HospitalAssistance.Status2;
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

            // Type of assistance
            var typeAssistanceRaw = HospitalAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = HospitalAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(HospitalAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, HospitalAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(HospitalAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, HospitalAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, HospitalAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {HospitalAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = HospitalAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? DEATH CERTIFICATE - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, HospitalAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {HospitalAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = HospitalAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = HospitalAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = HospitalAssistance.ValidBackimage;
            ViewData["Comments"] = HospitalAssistance.Comments;

            return View();
        }

        public IActionResult FuneralAssistanceApproveStatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var FuneralAssistance = context.FuneralAssistance.Find(id);
            if (FuneralAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status2"] = FuneralAssistance.Status2;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
            ViewData["District"] = FuneralAssistance.District;
            ViewData["Sex"] = FuneralAssistance.Sex;
            ViewData["PhilHealth"] = FuneralAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = FuneralAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = FuneralAssistance.Dateofbirth;
            ViewData["Age"] = FuneralAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = FuneralAssistance.RLastname;
            ViewData["RFirstname"] = FuneralAssistance.RFirstname;
            ViewData["RMiddlename"] = FuneralAssistance.RMiddlename;
            ViewData["RSuffix"] = FuneralAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = FuneralAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = FuneralAssistance.RSubVill;
            ViewData["RBrgy"] = FuneralAssistance.RBrgy;
            ViewData["RDistrict"] = FuneralAssistance.RDistrict;
            ViewData["RelationshipPatient"] = FuneralAssistance.RelationshipPatient;
            ViewData["ContactNo"] = FuneralAssistance.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = FuneralAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = FuneralAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(FuneralAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, FuneralAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(FuneralAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, FuneralAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, FuneralAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {FuneralAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = FuneralAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, FuneralAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {FuneralAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = FuneralAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            ViewData["Comments"] = FuneralAssistance.Comments;

            return View();
        }

        public IActionResult OtherAssistanceApproveStatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.OtherAssistance.Find(id);
            if (medicallabform == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status2"] = medicallabform.Status2;
            ViewData["Id"] = medicallabform.Id;
            ViewData["Lastname"] = medicallabform.Lastname;
            ViewData["Firstname"] = medicallabform.Firstname;
            ViewData["Middlename"] = medicallabform.Middlename;
            ViewData["Suffix"] = medicallabform.Suffix;
            ViewData["BlkLotStreet"] = medicallabform.BlkLotStreet;
            ViewData["SubVill"] = medicallabform.SubVill;
            ViewData["Brgy"] = medicallabform.Brgy;
            ViewData["District"] = medicallabform.District;
            ViewData["Sex"] = medicallabform.Sex;
            ViewData["PhilHealth"] = medicallabform.PhilHealth;
            ViewData["PhilHealthNo"] = medicallabform.PhilHealthNo;
            ViewData["Dateofbirth"] = medicallabform.Dateofbirth;
            ViewData["Age"] = medicallabform.Age;

            // Requestor details
            ViewData["RLastname"] = medicallabform.RLastname;
            ViewData["RFirstname"] = medicallabform.RFirstname;
            ViewData["RMiddlename"] = medicallabform.RMiddlename;
            ViewData["RSuffix"] = medicallabform.RSuffix;
            ViewData["RBlkLotStreet"] = medicallabform.RBlkLotStreet;
            ViewData["RSubVill"] = medicallabform.RSubVill;
            ViewData["RBrgy"] = medicallabform.RBrgy;
            ViewData["RDistrict"] = medicallabform.RDistrict;
            ViewData["RelationshipPatient"] = medicallabform.RelationshipPatient;
            ViewData["ContactNo"] = medicallabform.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = medicallabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = medicallabform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "MedCertificateimage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(medicallabform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, medicallabform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(medicallabform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, medicallabform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = medicallabform.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"?? Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"?? Full path: {medicalPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(medicalPath)}");

                    if (System.IO.File.Exists(medicalPath))
                    {
                        try
                        {
                            byte[] decryptedMedical = DecryptFile(medicalPath);
                            ViewData["MedicalCertificateBase64"] = Convert.ToBase64String(decryptedMedical);
                            ViewData["MedicalCertificate"] = medicallabform.MedCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedMedical);
                            ViewData["IsMedicalCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"?? IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Medical Certificate in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = medicallabform.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            return View();
        }

        //For not approved statuses
        public IActionResult HospitalAssistanceDisapproveStatus(int id)
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

            // Basic ViewData setup
            ViewData["Status2"] = HospitalAssistance.Status2;
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

            // Type of assistance
            var typeAssistanceRaw = HospitalAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = HospitalAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(HospitalAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, HospitalAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(HospitalAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, HospitalAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, HospitalAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {HospitalAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = HospitalAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? DEATH CERTIFICATE - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, HospitalAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {HospitalAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = HospitalAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = HospitalAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = HospitalAssistance.ValidBackimage;
            ViewData["Comments"] = HospitalAssistance.Comments;

            return View();
        }

        public IActionResult FuneralAssistanceDisapproveStatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var FuneralAssistance = context.FuneralAssistance.Find(id);
            if (FuneralAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status2"] = FuneralAssistance.Status2;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
            ViewData["District"] = FuneralAssistance.District;
            ViewData["Sex"] = FuneralAssistance.Sex;
            ViewData["PhilHealth"] = FuneralAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = FuneralAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = FuneralAssistance.Dateofbirth;
            ViewData["Age"] = FuneralAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = FuneralAssistance.RLastname;
            ViewData["RFirstname"] = FuneralAssistance.RFirstname;
            ViewData["RMiddlename"] = FuneralAssistance.RMiddlename;
            ViewData["RSuffix"] = FuneralAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = FuneralAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = FuneralAssistance.RSubVill;
            ViewData["RBrgy"] = FuneralAssistance.RBrgy;
            ViewData["RDistrict"] = FuneralAssistance.RDistrict;
            ViewData["RelationshipPatient"] = FuneralAssistance.RelationshipPatient;
            ViewData["ContactNo"] = FuneralAssistance.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = FuneralAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = FuneralAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(FuneralAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, FuneralAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(FuneralAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, FuneralAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, FuneralAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {FuneralAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = FuneralAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, FuneralAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {FuneralAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = FuneralAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            ViewData["Comments"] = FuneralAssistance.Comments;

            return View();
        }

        public IActionResult OtherAssistanceDisapproveStatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.OtherAssistance.Find(id);
            if (medicallabform == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status2"] = medicallabform.Status2;
            ViewData["Id"] = medicallabform.Id;
            ViewData["Lastname"] = medicallabform.Lastname;
            ViewData["Firstname"] = medicallabform.Firstname;
            ViewData["Middlename"] = medicallabform.Middlename;
            ViewData["Suffix"] = medicallabform.Suffix;
            ViewData["BlkLotStreet"] = medicallabform.BlkLotStreet;
            ViewData["SubVill"] = medicallabform.SubVill;
            ViewData["Brgy"] = medicallabform.Brgy;
            ViewData["District"] = medicallabform.District;
            ViewData["Sex"] = medicallabform.Sex;
            ViewData["PhilHealth"] = medicallabform.PhilHealth;
            ViewData["PhilHealthNo"] = medicallabform.PhilHealthNo;
            ViewData["Dateofbirth"] = medicallabform.Dateofbirth;
            ViewData["Age"] = medicallabform.Age;

            // Requestor details
            ViewData["RLastname"] = medicallabform.RLastname;
            ViewData["RFirstname"] = medicallabform.RFirstname;
            ViewData["RMiddlename"] = medicallabform.RMiddlename;
            ViewData["RSuffix"] = medicallabform.RSuffix;
            ViewData["RBlkLotStreet"] = medicallabform.RBlkLotStreet;
            ViewData["RSubVill"] = medicallabform.RSubVill;
            ViewData["RBrgy"] = medicallabform.RBrgy;
            ViewData["RDistrict"] = medicallabform.RDistrict;
            ViewData["RelationshipPatient"] = medicallabform.RelationshipPatient;
            ViewData["ContactNo"] = medicallabform.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = medicallabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = medicallabform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "MedCertificateimage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(medicallabform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, medicallabform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(medicallabform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, medicallabform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = medicallabform.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"?? Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"?? Full path: {medicalPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(medicalPath)}");

                    if (System.IO.File.Exists(medicalPath))
                    {
                        try
                        {
                            byte[] decryptedMedical = DecryptFile(medicalPath);
                            ViewData["MedicalCertificateBase64"] = Convert.ToBase64String(decryptedMedical);
                            ViewData["MedicalCertificate"] = medicallabform.MedCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedMedical);
                            ViewData["IsMedicalCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"?? IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Medical Certificate in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = medicallabform.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            return View();
        }




        //Claimed statuses
        public IActionResult HospitalAssistanceClaimStatus(int id)
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

            // Basic ViewData setup
            ViewData["Status3"] = HospitalAssistance.Status3;
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

            // Type of assistance
            var typeAssistanceRaw = HospitalAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = HospitalAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(HospitalAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, HospitalAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(HospitalAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, HospitalAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, HospitalAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {HospitalAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = HospitalAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? DEATH CERTIFICATE - UPDATED
                if (!string.IsNullOrEmpty(HospitalAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, HospitalAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {HospitalAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = HospitalAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = HospitalAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = HospitalAssistance.ValidBackimage;
            ViewData["Comments"] = HospitalAssistance.Comments;

            return View();
        }

        public IActionResult OtherAssistanceClaimStatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.OtherAssistance.Find(id);
            if (medicallabform == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status3"] = medicallabform.Status3;
            ViewData["Id"] = medicallabform.Id;
            ViewData["Lastname"] = medicallabform.Lastname;
            ViewData["Firstname"] = medicallabform.Firstname;
            ViewData["Middlename"] = medicallabform.Middlename;
            ViewData["Suffix"] = medicallabform.Suffix;
            ViewData["BlkLotStreet"] = medicallabform.BlkLotStreet;
            ViewData["SubVill"] = medicallabform.SubVill;
            ViewData["Brgy"] = medicallabform.Brgy;
            ViewData["District"] = medicallabform.District;
            ViewData["Sex"] = medicallabform.Sex;
            ViewData["PhilHealth"] = medicallabform.PhilHealth;
            ViewData["PhilHealthNo"] = medicallabform.PhilHealthNo;
            ViewData["Dateofbirth"] = medicallabform.Dateofbirth;
            ViewData["Age"] = medicallabform.Age;

            // Requestor details
            ViewData["RLastname"] = medicallabform.RLastname;
            ViewData["RFirstname"] = medicallabform.RFirstname;
            ViewData["RMiddlename"] = medicallabform.RMiddlename;
            ViewData["RSuffix"] = medicallabform.RSuffix;
            ViewData["RBlkLotStreet"] = medicallabform.RBlkLotStreet;
            ViewData["RSubVill"] = medicallabform.RSubVill;
            ViewData["RBrgy"] = medicallabform.RBrgy;
            ViewData["RDistrict"] = medicallabform.RDistrict;
            ViewData["RelationshipPatient"] = medicallabform.RelationshipPatient;
            ViewData["ContactNo"] = medicallabform.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = medicallabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = medicallabform.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "MedCertificateimage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(medicallabform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, medicallabform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(medicallabform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, medicallabform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = medicallabform.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"?? Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"?? Full path: {medicalPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(medicalPath)}");

                    if (System.IO.File.Exists(medicalPath))
                    {
                        try
                        {
                            byte[] decryptedMedical = DecryptFile(medicalPath);
                            ViewData["MedicalCertificateBase64"] = Convert.ToBase64String(decryptedMedical);
                            ViewData["MedicalCertificate"] = medicallabform.MedCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedMedical);
                            ViewData["IsMedicalCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"?? IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Medical Certificate in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = medicallabform.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            return View();
        }
        public IActionResult FuneralAssistanceClaimStatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var FuneralAssistance = context.FuneralAssistance.Find(id);
            if (FuneralAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status3"] = FuneralAssistance.Status3;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
            ViewData["District"] = FuneralAssistance.District;
            ViewData["Sex"] = FuneralAssistance.Sex;
            ViewData["PhilHealth"] = FuneralAssistance.PhilHealth;
            ViewData["PhilHealthNo"] = FuneralAssistance.PhilHealthNo;
            ViewData["Dateofbirth"] = FuneralAssistance.Dateofbirth;
            ViewData["Age"] = FuneralAssistance.Age;

            // Requestor details
            ViewData["RLastname"] = FuneralAssistance.RLastname;
            ViewData["RFirstname"] = FuneralAssistance.RFirstname;
            ViewData["RMiddlename"] = FuneralAssistance.RMiddlename;
            ViewData["RSuffix"] = FuneralAssistance.RSuffix;
            ViewData["RBlkLotStreet"] = FuneralAssistance.RBlkLotStreet;
            ViewData["RSubVill"] = FuneralAssistance.RSubVill;
            ViewData["RBrgy"] = FuneralAssistance.RBrgy;
            ViewData["RDistrict"] = FuneralAssistance.RDistrict;
            ViewData["RelationshipPatient"] = FuneralAssistance.RelationshipPatient;
            ViewData["ContactNo"] = FuneralAssistance.ContactNo;

            // Type of assistance
            var typeAssistanceRaw = FuneralAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = FuneralAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // ====================================
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "Funeralimg");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(FuneralAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, FuneralAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("? Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(FuneralAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, FuneralAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("? Back ID decrypted");
                    }
                }

                // ? DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, FuneralAssistance.DoctorPrescription);
                    debugMessages.Add($"?? Doctor Prescription filename: {FuneralAssistance.DoctorPrescription}");
                    debugMessages.Add($"?? Full path: {prescPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = FuneralAssistance.DoctorPrescription;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedPresc);
                            ViewData["IsDoctorPrescriptionPdf"] = isPdf;

                            debugMessages.Add($"? Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"?? IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Doctor Prescription in database");
                }

                // ? DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, FuneralAssistance.DeathCertificate);
                    debugMessages.Add($"?? Death Certificate filename: {FuneralAssistance.DeathCertificate}");
                    debugMessages.Add($"?? Full path: {deathPath}");
                    debugMessages.Add($"?? File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = FuneralAssistance.DeathCertificate;

                            // PDF DETECTION
                            bool isPdf = IsPdfFile(decryptedDeath);
                            ViewData["IsDeathCertificatePdf"] = isPdf;

                            debugMessages.Add($"? Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"?? IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"?? PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"? Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("? Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("?? No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            ViewData["Comments"] = FuneralAssistance.Comments;

            return View();
        }
            // UPDATED ViewPDF METHOD
            [HttpGet]
        public IActionResult ViewPDF(string fileName, string fileType)
        {
            try
            {
                // Authentication check - allow both user and admin access
                var userId = HttpContext.Session.GetString("UserId");
                var adminId = HttpContext.Session.GetString("AdminFullname");

                if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(adminId))
                {
                    return Unauthorized("Please log in to view documents");
                }

                Console.WriteLine($"?? ViewPDF called - FileName: {fileName}, FileType: {fileType}");

                // Validate inputs
                if (string.IsNullOrEmpty(fileName))
                {
                    Console.WriteLine("? FileName is null or empty");
                    return BadRequest("FileName is required");
                }

                if (string.IsNullOrEmpty(fileType))
                {
                    Console.WriteLine("? FileType is null or empty");
                    return BadRequest("FileType is required");
                }

                // Security: Prevent directory traversal
                string safeFileName = Path.GetFileName(fileName);

                // Define the directory based on file type
                string folderPath = fileType.ToLower() switch
                {
                    "doctorprescription" => Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage"),
                    "deathcertificate" => Path.Combine(environment.WebRootPath, "Funeralimg"),
                    "medicalcertificate" => Path.Combine(environment.WebRootPath, "MedCertificateimage"),
                    _ => Path.Combine(environment.WebRootPath, "Validimg")
                };

                Console.WriteLine($"?? Folder path: {folderPath}");

                string encryptedFilePath = Path.Combine(folderPath, safeFileName);
                Console.WriteLine($"?? Full file path: {encryptedFilePath}");

                // Additional security: Verify the resolved path is within the expected directory
                string resolvedPath = Path.GetFullPath(encryptedFilePath);
                string resolvedFolder = Path.GetFullPath(folderPath);
                if (!resolvedPath.StartsWith(resolvedFolder))
                {
                    Console.WriteLine("? Security: Path traversal attempt detected");
                    return BadRequest("Invalid file path");
                }

                // Check if file exists
                if (!System.IO.File.Exists(encryptedFilePath))
                {
                    Console.WriteLine($"? File does not exist: {encryptedFilePath}");
                    return NotFound($"File not found: {fileName}");
                }

                Console.WriteLine($"? File exists. Size: {new FileInfo(encryptedFilePath).Length} bytes");

                // Decrypt the file USING CONFIGURATION-BASED KEY
                byte[] decryptedBytes = DecryptFile(encryptedFilePath);
                Console.WriteLine($"? File decrypted. Decrypted size: {decryptedBytes.Length} bytes");

                // Verify it's actually a PDF
                bool isPdf = IsPdfFile(decryptedBytes);
                Console.WriteLine($"?? Is PDF: {isPdf}");

                if (!isPdf)
                {
                    Console.WriteLine("? File is not a valid PDF");
                    return BadRequest("Only PDF files can be viewed");
                }

                // ? CRITICAL: Set headers to FORCE inline viewing and PREVENT download
                Response.Headers["Content-Disposition"] = "inline";
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

                // Additional security headers to prevent download
                Response.Headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'self'";
                Response.Headers["X-Content-Type-Options"] = "nosniff";

                // Cache control to prevent caching of sensitive documents
                Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
                Response.Headers["Pragma"] = "no-cache";
                Response.Headers["Expires"] = "0";

                Console.WriteLine($"?? Returning PDF for INLINE VIEWING ONLY (download disabled)");

                // Return as PDF without any filename parameter
                return File(decryptedBytes, "application/pdf");
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"?? DECRYPTION ERROR: {ex.Message}");
                return BadRequest("Failed to decrypt file. Invalid encryption or corrupted file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? ERROR in ViewPDF: {ex.Message}");
                Console.WriteLine($"?? Stack Trace: {ex.StackTrace}");
                return StatusCode(500, $"Error viewing PDF: {ex.Message}");
            }
        }

        // UPDATED CheckFileType METHOD
        [HttpGet]
        public IActionResult CheckFileType(string fileName, string fileType = "validid")
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return Unauthorized();
            }

            string folder;

            switch (fileType.ToLower())
            {
                case "doctorprescription":
                    folder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    break;
                case "deathcertificate":
                    folder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    break;
                case "medicalcertificate":
                    folder = Path.Combine(environment.WebRootPath, "MedCertificateimage");
                    break;
                default:
                    folder = Path.Combine(environment.WebRootPath, "Validimg");
                    break;
            }

            string safeFileName = Path.GetFileName(fileName);
            string filePath = Path.Combine(folder, safeFileName);

            if (!System.IO.File.Exists(filePath))
            {
                return Json(new { exists = false, isPdf = false });
            }

            try
            {
                byte[] decryptedData = DecryptFile(filePath);
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

        //111
        //approving and unpproving status
        [HttpPost]
        public IActionResult HospitalAssistanceProcessingStatus(int id, HospitalAssistanceDto HospitalAssistanceDto)
        {
            var HospitalAssistance = context.HospitalAssistance.Find(id);

            if (HospitalAssistance == null)
            {
                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update record
                HospitalAssistance.Status2 = HospitalAssistanceDto.Status2;
                HospitalAssistance.ForCMOPERSONNEL = HospitalAssistanceDto.ForCMOPERSONNEL;
                HospitalAssistance.Comments = HospitalAssistanceDto.Comments;
                HospitalAssistance.Processby = HospitalAssistanceDto.Processby;
                HospitalAssistance.Result = DateTime.Now;
                context.SaveChanges();

                // Send multi-channel notification (In-App, SMS, Email based on preferences)
                var status = HospitalAssistanceDto.Status2?.Trim();
                if (!string.IsNullOrEmpty(status) && (status.Equals("Approve", StringComparison.OrdinalIgnoreCase) || status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase)))
                {
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == HospitalAssistance.UserId);
                    var applicantName = verifyAccount?.Firstname ?? "Applicant";

                    _ = _notificationService.SendStatusChangeNotificationAsync(
                        HospitalAssistance.UserId,
                        applicantName,
                        "HospitalBill",
                        status,
                        HospitalAssistance.Id
                    );

                    // ✅ ADDED EMAIL FEATURE
                    // Get the user's info from RegisterAcc
                    var user = context.RegisterAcc.FirstOrDefault(u => u.Id == HospitalAssistance.UserId);

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        // Get user's first name from VerifyAccount
                        var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                        // Get email settings from configuration
                        var fromEmail = _configuration["EmailSettings:FromEmail"];
                        var fromName = _configuration["EmailSettings:FromName"];
                        var fromPassword = _configuration["EmailSettings:FromPassword"];

                        if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                            throw new ArgumentException("Email settings are missing.");

                        // Compose the email based on status
                        var fromAddress = new MailAddress(fromEmail, fromName);
                        var toAddress = new MailAddress(user.Email, firstName);

                        string subject = "";
                        string body = "";

                        if (status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
                        {
                            subject = "Congratulations! Your Hospital Bill Assistance Has Been Approved - LINGAP DVO";
                            body = $@"
                            Dear {firstName},

                            We are pleased to inform you that your Hospital Bill Assistance application has been APPROVED.

                            APPLICATION DETAILS:
                            • Application Type: Hospital Bill Assistance
                            • Application ID: {HospitalAssistance.Id}
                            • Status: Approve
                            • Date Approved: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}
                            • Processed By: {HospitalAssistanceDto.Processby}

                            REMARKS:
                            {HospitalAssistanceDto.Comments ?? "No additional remarks provided."}

                            NEXT STEPS:
                            Please visit our office to complete the necessary documentation and receive your assistance.

                            Thank you for choosing LINGAP DVO. We are committed to supporting your healthcare needs.

                            Sincerely,
                            {fromName}
                            LINGAP DVO Medical Assistance Program

                            Note: This is an automated email. Please do not reply to this message.";
                                                    }
                                                    else if (status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        subject = "Update on Your Hospital Bill Assistance Application - LINGAP DVO";
                                                        body = $@"
                            Dear {firstName},

                            After careful review, we regret to inform you that your Hospital Bill Assistance application has been DISAPPROVED.

                            APPLICATION DETAILS:
                            • Application Type: Hospital Bill Assistance
                            • Application ID: {HospitalAssistance.Id}
                            • Status: Disapprove
                            • Date Updated: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}
                            • Processed By: {HospitalAssistanceDto.Processby}

                            REMARKS:
                            {HospitalAssistanceDto.Comments ?? "Please contact our office for more information about this decision."}

                            If you have questions or would like to discuss this decision further, please visit our office during business hours.

                            We appreciate your understanding.

                            Sincerely,
                            {fromName}
                            LINGAP DVO Medical Assistance Program

                            Note: This is an automated email. Please do not reply to this message.";
                        }

                        // Send the email safely
                        using (var smtp = new SmtpClient("smtp.gmail.com", 587)
                        {
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                        })
                        using (var message = new MailMessage(fromAddress, toAddress)
                        {
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = false
                        })
                        {
                            smtp.Send(message);
                        }

                        // Log email sent successfully
                        Console.WriteLine($"Status update email sent to {user.Email} for application {HospitalAssistance.Id} - Status: {status}");
                    }
                }

                TempData["SuccessMessage"] = $"Hospital bill status updated to '{HospitalAssistanceDto.Status2}' and notifications sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in HospitalAssistanceProcessingStatus: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(HospitalAssistanceDto);
            }
        }


        [HttpPost]
        public IActionResult OtherAssistanceProcessingStatus(int id, OtherAssistanceDto OtherAssistanceDto)
        {
            var medicallabform = context.OtherAssistance.Find(id);

            if (medicallabform == null)
            {
                TempData["ErrorMessage"] = "Medical assistance record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update record
                medicallabform.Status2 = OtherAssistanceDto.Status2;
                medicallabform.ForCMOPERSONNEL = OtherAssistanceDto.ForCMOPERSONNEL;
                medicallabform.Comments = OtherAssistanceDto.Comments;
                medicallabform.Processby = OtherAssistanceDto.Processby;
                medicallabform.Result = DateTime.Now;
                context.SaveChanges();

                // Send multi-channel notification (In-App, SMS, Email based on preferences)
                var status = OtherAssistanceDto.Status2?.Trim();
                if (!string.IsNullOrEmpty(status) && (status.Equals("Approve", StringComparison.OrdinalIgnoreCase) || status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase)))
                {
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == medicallabform.UserId);
                    var applicantName = verifyAccount?.Firstname ?? "Applicant";

                    _ = _notificationService.SendStatusChangeNotificationAsync(
                        medicallabform.UserId,
                        applicantName,
                        "Medical",
                        status,
                        medicallabform.Id
                    );

                    // ✅ ADDED EMAIL FEATURE
                    // Get the user's info from RegisterAcc
                    var user = context.RegisterAcc.FirstOrDefault(u => u.Id == medicallabform.UserId);

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        // Get user's first name from VerifyAccount
                        var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                        // Get email settings from configuration
                        var fromEmail = _configuration["EmailSettings:FromEmail"];
                        var fromName = _configuration["EmailSettings:FromName"];
                        var fromPassword = _configuration["EmailSettings:FromPassword"];

                        if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                            throw new ArgumentException("Email settings are missing.");

                        // Compose the email based on status
                        var fromAddress = new MailAddress(fromEmail, fromName);
                        var toAddress = new MailAddress(user.Email, firstName);

                        string subject = "";
                        string body = "";

                        if (status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
                        {
                            subject = "Congratulations! Your Medical Assistance Has Been Approve - LINGAP DVO";
                            body = $@"
                    Dear {firstName},

                    We are pleased to inform you that your Medical Assistance application has been APPROVED.

                    APPLICATION DETAILS:
                    • Application Type: Medical Assistance
                    • Application ID: {medicallabform.Id}
                    • Status: Approve
                    • Date Approve: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}
                    • Processed By: {OtherAssistanceDto.Processby}

                    REMARKS:
                    {OtherAssistanceDto.Comments ?? "No additional remarks provided."}

                    NEXT STEPS:
                    Please visit our office to complete the necessary documentation and receive your assistance.

                    Thank you for choosing LINGAP DVO. We are committed to supporting your healthcare needs.

                    Sincerely,
                    {fromName}
                    LINGAP DVO Medical Assistance Program

                    Note: This is an automated email. Please do not reply to this message.";
                        }
                        else if (status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase))
                        {
                            subject = "Update on Your Medical Assistance Application - LINGAP DVO";
                            body = $@"
                    Dear {firstName},

                    After careful review, we regret to inform you that your Medical Assistance application has been DISAPPROVE.

                    APPLICATION DETAILS:
                    • Application Type: Medical Assistance
                    • Application ID: {medicallabform.Id}
                    • Status: Disapprove
                    • Date Updated: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}
                    • Processed By: {OtherAssistanceDto.Processby}

                    REMARKS:
                    {OtherAssistanceDto.Comments ?? "Please contact our office for more information about this decision."}

                    If you have questions or would like to discuss this decision further, please visit our office during business hours.

                    We appreciate your understanding.

                    Sincerely,
                    {fromName}
                    LINGAP DVO Medical Assistance Program

                    Note: This is an automated email. Please do not reply to this message.";
                        }

                        // Send the email safely
                        using (var smtp = new SmtpClient("smtp.gmail.com", 587)
                        {
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                        })
                        using (var message = new MailMessage(fromAddress, toAddress)
                        {
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = false
                        })
                        {
                            smtp.Send(message);
                        }

                        // Log email sent successfully
                        Console.WriteLine($"Status update email sent to {user.Email} for application {medicallabform.Id} - Status: {status}");
                    }
                }

                TempData["SuccessMessage"] = $"Medical assistance status updated to '{OtherAssistanceDto.Status2}' and notifications sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in OtherAssistanceUpdateprocessingstatus: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(OtherAssistanceDto);
            }
        }

        [HttpPost]
        public IActionResult FuneralAssistanceProcessingStatus(int id, FuneralAssistanceDto FuneralAssistanceDto)
        {
            var FuneralAssistance = context.FuneralAssistance.Find(id);

            if (FuneralAssistance == null)
            {
                TempData["ErrorMessage"] = "Funeral assistance record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update record
                FuneralAssistance.Status2 = FuneralAssistanceDto.Status2;
                FuneralAssistance.ForCMOPERSONNEL = FuneralAssistanceDto.ForCMOPERSONNEL;
                FuneralAssistance.Comments = FuneralAssistanceDto.Comments;
                FuneralAssistance.Processby = FuneralAssistanceDto.Processby;
                FuneralAssistance.Result = DateTime.Now;
                context.SaveChanges();

                // Send multi-channel notification (In-App, SMS, Email based on preferences)
                var status = FuneralAssistanceDto.Status2?.Trim();
                if (!string.IsNullOrEmpty(status) && (status.Equals("Approve", StringComparison.OrdinalIgnoreCase) || status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase)))
                {
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == FuneralAssistance.UserId);
                    var applicantName = verifyAccount?.Firstname ?? "Applicant";

                    _ = _notificationService.SendStatusChangeNotificationAsync(
                        FuneralAssistance.UserId,
                        applicantName,
                        "Funeral",
                        status,
                        FuneralAssistance.Id
                    );

                    // ✅ ADDED EMAIL FEATURE
                    // Get the user's info from RegisterAcc
                    var user = context.RegisterAcc.FirstOrDefault(u => u.Id == FuneralAssistance.UserId);

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        // Get user's first name from VerifyAccount
                        var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                        // Get email settings from configuration
                        var fromEmail = _configuration["EmailSettings:FromEmail"];
                        var fromName = _configuration["EmailSettings:FromName"];
                        var fromPassword = _configuration["EmailSettings:FromPassword"];

                        if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                            throw new ArgumentException("Email settings are missing.");

                        // Compose the email based on status
                        var fromAddress = new MailAddress(fromEmail, fromName);
                        var toAddress = new MailAddress(user.Email, firstName);

                        string subject = "";
                        string body = "";

                        if (status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
                        {
                            subject = "Congratulations! Your Funeral Assistance Has Been Approve - LINGAP DVO";
                            body = $@"
                            Dear {firstName},

                            We are pleased to inform you that your Funeral Assistance application has been APPROVE.

                            APPLICATION DETAILS:
                            • Application Type: Funeral Assistance
                            • Application ID: {FuneralAssistance.Id}
                            • Status: Approve
                            • Date Approve: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}
                            • Processed By: {FuneralAssistanceDto.Processby}

                            REMARKS:
                            {FuneralAssistanceDto.Comments ?? "No additional remarks provided."}

                            NEXT STEPS:
                            Please visit our office to complete the necessary documentation and receive your assistance.

                            Thank you for choosing LINGAP DVO. We are committed to supporting your needs during this difficult time.

                            Sincerely,
                            {fromName}
                            LINGAP DVO Funeral Assistance Program

                            Note: This is an automated email. Please do not reply to this message.";
                                        }
                                        else if (status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase))
                                        {
                                            subject = "Update on Your Funeral Assistance Application - LINGAP DVO";
                                            body = $@"
                            Dear {firstName},

                            After careful review, we regret to inform you that your Funeral Assistance application has been DISAPPROVE.

                            APPLICATION DETAILS:
                            • Application Type: Funeral Assistance
                            • Application ID: {FuneralAssistance.Id}
                            • Status: Disapprove
                            • Date Updated: {DateTime.Now:MMMM dd, yyyy 'at' hh:mm tt}
                            • Processed By: {FuneralAssistanceDto.Processby}

                            REMARKS:
                            {FuneralAssistanceDto.Comments ?? "Please contact our office for more information about this decision."}

                            If you have questions or would like to discuss this decision further, please visit our office during business hours.

                            We appreciate your understanding.

                            Sincerely,
                            {fromName}
                            LINGAP DVO Funeral Assistance Program

                            Note: This is an automated email. Please do not reply to this message.";
                                        }

                        // Send the email safely
                        using (var smtp = new SmtpClient("smtp.gmail.com", 587)
                        {
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                        })
                        using (var message = new MailMessage(fromAddress, toAddress)
                        {
                            Subject = subject,
                            Body = body,
                            IsBodyHtml = false
                        })
                        {
                            smtp.Send(message);
                        }

                        // Log email sent successfully
                        Console.WriteLine($"Status update email sent to {user.Email} for application {FuneralAssistance.Id} - Status: {status}");
                    }
                }

                TempData["SuccessMessage"] = $"Funeral assistance status updated to '{FuneralAssistanceDto.Status2}' and notifications sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in FuneralAssistanceUpdateprocessingstatus: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(FuneralAssistanceDto);
            }
        }



        // ? For Approved Statuses to Claimed 
        [HttpPost] 
        public IActionResult HospitalAssistanceApproveStatus(int id, HospitalAssistanceDto HospitalAssistanceDto)
        {
            var HospitalAssistance = context.HospitalAssistance.Find(id);

            if (HospitalAssistance == null)
            {
                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // ? Update record
                HospitalAssistance.Status3 = HospitalAssistanceDto.Status3;
                HospitalAssistance.ForCMOPERSONNEL = HospitalAssistanceDto.ForCMOPERSONNEL;
                HospitalAssistance.Comments = HospitalAssistanceDto.Comments;
                HospitalAssistance.Processby = HospitalAssistanceDto.Processby;
                HospitalAssistance.ClaimedAt = DateTime.Now;

                context.SaveChanges();

                // ? Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == HospitalAssistance.UserId);

                // ? Only send email if status is "Claimed"
                if (HospitalAssistanceDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // ? Get first name from VerifyAccount
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // ? Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // ? Compose email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, firstName);

                    string subject = "Hospital Bill Assistance Claimed - LINGAP DVO";
                    string body = $@"
                        Dear {firstName},

                        We are pleased to inform you that your Hospital Bill Assistance has been successfully claimed as of {DateTime.Now:MMMM dd, yyyy}.

                        APPLICATION DETAILS:
                        � Application Type: Hospital Bill Assistance
                        � Status: Claimed
                        � Processed By: {HospitalAssistanceDto.Processby ?? "LINGAP Personnel"}
                        � Date Claimed: {DateTime.Now:MMMM dd, yyyy HH:mm tt}

                        REMARKS:
                        {HospitalAssistanceDto.Comments ?? "Your claim has been processed and recorded successfully."}

                        Thank you for your patience and cooperation throughout the process.  
                        Should you have any further questions, please contact our support team at [Support Email/Phone Number].

                        Sincerely,  
                        {fromName}  
                        LINGAP DVO Medical Assistance Program
                        ";

                    // ? Send email
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    })
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }

                TempData["SuccessMessage"] = "Hospital bill claimed successfully and email sent.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(HospitalAssistanceDto);
            }
        }

        [HttpPost]
        public IActionResult OtherAssistanceApproveStatus(int id, OtherAssistanceDto OtherAssistanceDto)
        {
            var otherAssistance = context.OtherAssistance.Find(id);

            if (otherAssistance == null)
            {
                TempData["ErrorMessage"] = "Medical and laboratory record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update record
                otherAssistance.Status3 = OtherAssistanceDto.Status3;
                otherAssistance.ForCMOPERSONNEL = OtherAssistanceDto.ForCMOPERSONNEL;
                otherAssistance.Comments = OtherAssistanceDto.Comments;
                otherAssistance.Processby = OtherAssistanceDto.Processby;
                otherAssistance.ClaimedAt = DateTime.Now;

                context.SaveChanges();

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == otherAssistance.UserId);

                // Only send email if status is "Claimed"
                if (OtherAssistanceDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true
                    && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get first name from VerifyAccount or fallback
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Compose email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, firstName);

                    string subject = "Medical and Laboratory Assistance Claimed - LINGAP DVO";
                    string body = $@"
                    Dear {firstName},

                    We are pleased to inform you that your Medical and Laboratory Assistance has been successfully claimed as of {DateTime.Now:MMMM dd, yyyy}.

                    APPLICATION DETAILS:
                    • Application Type: Medical and Laboratory Assistance
                    • Application ID: {otherAssistance.Id}
                    • Status: Claimed
                    • Processed By: {OtherAssistanceDto.Processby ?? "LINGAP Personnel"}
                    • Date Claimed: {DateTime.Now:MMMM dd, yyyy HH:mm tt}

                    REMARKS:
                    {OtherAssistanceDto.Comments ?? "Your claim has been processed and recorded successfully."}

                    Thank you for your patience and cooperation throughout the process.  
                    Should you have any further questions, please contact our support team at [Support Email/Phone Number].

                    Sincerely,  
                    {fromName}  
                    LINGAP DVO Medical Assistance Program
                    ";

                    // Send email
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    })
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }

                TempData["SuccessMessage"] = "Medical and laboratory claim processed successfully and email sent.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(OtherAssistanceDto);
            }
        }

        [HttpPost]
        public IActionResult FuneralAssistanceApproveStatus(int id, FuneralAssistanceDto FuneralAssistanceDto)
        {
            var funeralAssistance = context.FuneralAssistance.Find(id);

            if (funeralAssistance == null)
            {
                TempData["ErrorMessage"] = "Funeral and burial record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update record
                funeralAssistance.Status3 = FuneralAssistanceDto.Status3;
                funeralAssistance.ForCMOPERSONNEL = FuneralAssistanceDto.ForCMOPERSONNEL;
                funeralAssistance.Comments = FuneralAssistanceDto.Comments;
                funeralAssistance.Processby = FuneralAssistanceDto.Processby;
                funeralAssistance.ClaimedAt = DateTime.Now;

                context.SaveChanges();

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == funeralAssistance.UserId);

                // Only send email if status is "Claimed"
                if (FuneralAssistanceDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true
                    && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get first name from VerifyAccount or fallback to username
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Compose email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, firstName);

                    string subject = "Your LINGAP Funeral Assistance Has Been Claimed";
                    string body = $@"
                    Dear {firstName},

                    We are pleased to inform you that your LINGAP Funeral Assistance has been successfully claimed as of {DateTime.Now:MMMM dd, yyyy}.

                    APPLICATION DETAILS:
                    • Application Type: Funeral Assistance
                    • Status: Claimed
                    • Processed By: {FuneralAssistanceDto.Processby ?? "LINGAP Personnel"}
                    • Date Claimed: {DateTime.Now:MMMM dd, yyyy HH:mm tt}

                    REMARKS:
                    {FuneralAssistanceDto.Comments ?? "Your claim has been processed and recorded successfully."}

                    Thank you for your patience and cooperation throughout the process.  
                    Should you have any further questions, please contact our support team at [Support Email/Phone Number].

                    Sincerely,  
                    {fromName}  
                    LINGAP DVO Medical Assistance Program
                    ";

                    // Send email
                    using (var smtp = new SmtpClient("smtp.gmail.com", 587)
                    {
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    })
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }

                TempData["SuccessMessage"] = "Funeral and burial assistance marked as 'Claimed' and email sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(FuneralAssistanceDto);
            }
        }


        // Priorities page with priority system
        public async Task<IActionResult> Priorities()
        {
            // Prevent caching
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            // Check session
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            // Get all data from the database
            var hospitalBills = context.HospitalAssistance
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.OtherAssistance
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var FuneralAssistance = context.FuneralAssistance
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Get priority counts
            var (highPriority, mediumPriority, totalPriority) = await _priorityService.GetPriorityCountsAsync();

            // Pass counts to view via ViewBag
            ViewBag.HighPriorityCount = highPriority;
            ViewBag.MediumPriorityCount = mediumPriority;
            ViewBag.TotalPriorityCount = totalPriority;

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalAssistance = hospitalBills,
                OtherAssistance = medicalLabForms,
                FuneralAssistance = FuneralAssistance
            };

            return View(viewModel);
        }

        // Get notification count for sidebar badge
        [HttpGet]
        public IActionResult GetNotificationCount()
        {
            try
            {
                var now = DateTime.Now;

                // Get ONLY Pending and Processing applications
                // Completed statuses (Approve, Disapprove, Claimed) are excluded
                var hospitalBills = context.HospitalAssistance
                    .Where(h => h.Status2 == "Pending" || h.Status2 == "Processing")
                    .ToList();
                var medicalLabForms = context.OtherAssistance
                    .Where(m => m.Status2 == "Pending" || m.Status2 == "Processing")
                    .ToList();
                var FuneralAssistance = context.FuneralAssistance
                    .Where(f => f.Status2 == "Pending" || f.Status2 == "Processing")
                    .ToList();

                int priorityCount = 0;

                // Count hospital bills with priority (1+ hours since submission)
                foreach (var bill in hospitalBills)
                {
                    var hoursSinceSubmission = (now - bill.CreatedAt).TotalHours;
                    if (hoursSinceSubmission >= 1)
                    {
                        priorityCount++;
                    }
                }

                // Count medical lab forms with priority
                foreach (var form in medicalLabForms)
                {
                    var hoursSinceSubmission = (now - form.CreatedAt).TotalHours;
                    if (hoursSinceSubmission >= 1)
                    {
                        priorityCount++;
                    }
                }

                // Count funeral forms with priority
                foreach (var form in FuneralAssistance)
                {
                    var hoursSinceSubmission = (now - form.CreatedAt).TotalHours;
                    if (hoursSinceSubmission >= 1)
                    {
                        priorityCount++;
                    }
                }

                return Json(new { count = priorityCount });
            }
            catch (Exception ex)
            {
                return Json(new { count = 0, error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // FEEDBACK ANALYTICS
        // ═══════════════════════════════════════════════════════════════

        public IActionResult FeedbackAnalytics()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetFeedbackStatistics(DateTime? startDate, DateTime? endDate, string assistanceType = null)
        {
            try
            {
                var query = context.Feedbacks.AsQueryable();

                // Apply date filter
                if (startDate.HasValue)
                {
                    query = query.Where(f => f.SubmittedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    query = query.Where(f => f.SubmittedAt <= endDate.Value.AddDays(1));
                }

                // Apply assistance type filter
                if (!string.IsNullOrEmpty(assistanceType))
                {
                    query = query.Where(f => f.AssistanceType == assistanceType);
                }

                var feedbacks = query.ToList();
                var totalFeedbacks = feedbacks.Count;

                // Rating Statistics (1-8 questions, values 1-6)
                var ratingStats = new
                {
                    r1_ServiceSatisfaction = CalculateRatingDistribution(feedbacks.Select(f => f.R1_ServiceSatisfaction)),
                    r2_TimeSpent = CalculateRatingDistribution(feedbacks.Select(f => f.R2_TimeSpent)),
                    r3_ProcessFollowed = CalculateRatingDistribution(feedbacks.Select(f => f.R3_ProcessFollowed)),
                    r4_ProcessSimplicity = CalculateRatingDistribution(feedbacks.Select(f => f.R4_ProcessSimplicity)),
                    r5_InformationAccess = CalculateRatingDistribution(feedbacks.Select(f => f.R5_InformationAccess)),
                    r6_FairPayment = CalculateRatingDistribution(feedbacks.Select(f => f.R6_FairPayment)),
                    r7_Fairness = CalculateRatingDistribution(feedbacks.Select(f => f.R7_Fairness)),
                    r8_EmployeeCourtesy = CalculateRatingDistribution(feedbacks.Select(f => f.R8_EmployeeCourtesy))
                };

                // Demographics
                var demographics = new
                {
                    sex = feedbacks.Where(f => !string.IsNullOrEmpty(f.Sex))
                        .GroupBy(f => f.Sex)
                        .Select(g => new { label = g.Key, count = g.Count() })
                        .ToList(),
                    typeOfClient = feedbacks.Where(f => !string.IsNullOrEmpty(f.TypeOfClient))
                        .GroupBy(f => f.TypeOfClient)
                        .Select(g => new { label = g.Key, count = g.Count() })
                        .ToList()
                };

                // CC Knowledge
                var ccKnowledge = new
                {
                    q1_Knowledge = feedbacks.Where(f => !string.IsNullOrEmpty(f.Q1_CCKnowledge))
                        .GroupBy(f => f.Q1_CCKnowledge)
                        .Select(g => new { label = g.Key, count = g.Count() })
                        .ToList(),
                    q2_Visibility = feedbacks.Where(f => !string.IsNullOrEmpty(f.Q2_CCVisibility))
                        .GroupBy(f => f.Q2_CCVisibility)
                        .Select(g => new { label = g.Key, count = g.Count() })
                        .ToList(),
                    q3_Helpfulness = feedbacks.Where(f => !string.IsNullOrEmpty(f.Q3_CCHelpfulness))
                        .GroupBy(f => f.Q3_CCHelpfulness)
                        .Select(g => new { label = g.Key, count = g.Count() })
                        .ToList()
                };

                // Assistance Type Distribution
                var assistanceTypeDistribution = feedbacks.Where(f => !string.IsNullOrEmpty(f.AssistanceType))
                    .GroupBy(f => f.AssistanceType)
                    .Select(g => new { label = g.Key, count = g.Count() })
                    .ToList();

                // Timeline (last 30 days)
                var timeline = feedbacks.Where(f => f.SubmittedAt >= DateTime.UtcNow.AddDays(-30))
                    .GroupBy(f => f.SubmittedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
                    .ToList();

                // Recent Feedback with Remarks
                var recentFeedback = feedbacks
                    .OrderByDescending(f => f.SubmittedAt)
                    .Take(10)
                    .Select(f => new
                    {
                        id = f.Id,
                        name = f.Name ?? "Anonymous",
                        assistanceType = f.AssistanceType ?? "N/A",
                        submittedAt = f.SubmittedAt.ToString("yyyy-MM-dd HH:mm"),
                        commendation = f.Commendation,
                        suggestion = f.Suggestion,
                        request = f.Request,
                        complaint = f.Complaint,
                        averageRating = CalculateAverageRating(f)
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    totalFeedbacks = totalFeedbacks,
                    ratingStats = ratingStats,
                    demographics = demographics,
                    ccKnowledge = ccKnowledge,
                    assistanceTypeDistribution = assistanceTypeDistribution,
                    timeline = timeline,
                    recentFeedback = recentFeedback
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private object CalculateRatingDistribution(IEnumerable<int?> ratings)
        {
            var validRatings = ratings.Where(r => r.HasValue).Select(r => r.Value).ToList();
            var total = validRatings.Count;

            if (total == 0)
            {
                return new
                {
                    average = 0,
                    distribution = new int[] { 0, 0, 0, 0, 0, 0 },
                    total = 0
                };
            }

            var distribution = new int[6];
            for (int i = 1; i <= 6; i++)
            {
                distribution[i - 1] = validRatings.Count(r => r == i);
            }

            var average = validRatings.Average();

            return new
            {
                average = Math.Round(average, 2),
                distribution = distribution,
                total = total
            };
        }

        private double CalculateAverageRating(Feedback f)
        {
            var ratings = new[] {
                f.R1_ServiceSatisfaction,
                f.R2_TimeSpent,
                f.R3_ProcessFollowed,
                f.R4_ProcessSimplicity,
                f.R5_InformationAccess,
                f.R6_FairPayment,
                f.R7_Fairness,
                f.R8_EmployeeCourtesy
            }.Where(r => r.HasValue).Select(r => r.Value).ToList();

            return ratings.Any() ? Math.Round(ratings.Average(), 2) : 0;
        }


    }


}
