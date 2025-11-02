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

        //renvic edit sa grammar
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
                // Automatically set status to "Processing"
                fillupformhospitalBill.Status = "Processing";
                fillupformhospitalBill.ForCMOPERSONNEL = fillupformHospitalbilldto.ForCMOPERSONNEL;
                fillupformhospitalBill.Comments = fillupformHospitalbilldto.Comments;
                fillupformhospitalBill.Processby = fillupformHospitalbilldto.Processby;
                fillupformhospitalBill.ProcessAt = DateTime.Now;

                context.SaveChanges();

                // Get the user's info from RegisterAcc
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == fillupformhospitalBill.UserId);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get user's first name from VerifyAccount
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);
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
                • Application Type: Hospital Bill Assistance
                • Status: Processing
                • Date Updated: {DateTime.Now:MMMM dd, yyyy HH:mm tt}

                REMARKS:
                {fillupformHospitalbilldto.Comments ?? "N/A"}

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
                TempData["ErrorMessage"] = "Medical and laboratory record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Automatically set status to "Processing"
                medicalandlabform.Status = "Processing";
                medicalandlabform.Comments = medicalandlabformDto.Comments;
                medicalandlabform.Processby = medicalandlabformDto.Processby;
                medicalandlabform.ProcessAt = DateTime.Now;

                context.SaveChanges();

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == medicalandlabform.UserId);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // ✅ Get user's first name from VerifyAccount table
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // ✅ Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // ✅ Prepare email content
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
                    {medicalandlabformDto.Comments ?? "N/A"}

                    Thank you for your patience. We will notify you once your application status has been updated.

                    Sincerely,
                    {fromName}
                    LINGAP DVO Medical Assistance Program";

                    // ✅ Send email
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

        // Renvic edit sa grammar
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
                TempData["ErrorMessage"] = "Funeral and burial record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // ✅ Automatically set status to "Processing"
                funeralburialform.Status = "Processing";
                funeralburialform.Comments = funeralburialformDto.Comments;
                funeralburialform.Processby = funeralburialformDto.Processby;
                funeralburialform.ProcessAt = DateTime.Now;

                context.SaveChanges();

                // ✅ Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == funeralburialform.UserId);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // ✅ Get user's first name from VerifyAccount table
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // ✅ Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // ✅ Prepare email content
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
                                {funeralburialformDto.Comments ?? "N/A"}

                                Thank you for your patience. We will notify you once your application status has been updated.

                                Sincerely,
                                {fromName}
                                LINGAP DVO Funeral and Burial Assistance Program";

                    // ✅ Send email
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
        // COMPLETE HOSPITAL BILL CONTROLLER - WITH EMBEDDED AES ENCRYPTION HELPER
        // ====================================

        // ╔═══════════════════════════════════════════════════════════════════════════╗
        // ║                    AES-256 ENCRYPTION HELPER CLASS                        ║
        // ║         Secure AES-256 Implementation using Configuration                 ║
        // ╚═══════════════════════════════════════════════════════════════════════════╝
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
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
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
                        byte[] decryptedFront = DecryptFile(frontPath);
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
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED
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
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;

                            // PDF DETECTION
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

                // ⭐ DEATH CERTIFICATE - UPDATED
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
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                            // PDF DETECTION
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

        public IActionResult MedicalandlabformUpdateprocessingstatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.Medicalandlabform.Find(id);
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
                        debugMessages.Add("✅ Front ID decrypted");
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
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

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

                // ⭐ MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"📄 Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"📂 Full path: {medicalPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(medicalPath)}");

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

                            debugMessages.Add($"✅ Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"🔍 IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Medical Certificate in database");
                }

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

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
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            return View();
        }
        public IActionResult FuneralburialformUpdateprocessingstatus(int id)
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

            // Basic ViewData setup
            ViewData["Status2"] = funeralburialform.Status2;
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

            // Type of assistance
            var typeAssistanceRaw = funeralburialform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = funeralburialform.ForCMOPERSONNEL ?? "";
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
                if (!string.IsNullOrEmpty(funeralburialform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, funeralburialform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(funeralburialform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, funeralburialform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, funeralburialform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {funeralburialform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;

                            // PDF DETECTION
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

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, funeralburialform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {funeralburialform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

                            // PDF DETECTION
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
            ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
            ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;
            ViewData["Comments"] = funeralburialform.Comments;

            return View();
        }

        //for approving statuses
        public IActionResult FillupformHospitalBillapprovedstatus(int id)
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
            ViewData["Status2"] = fillupformhospitalBill.Status2;
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
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
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
                        byte[] decryptedFront = DecryptFile(frontPath);
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
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED
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
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;

                            // PDF DETECTION
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

                // ⭐ DEATH CERTIFICATE - UPDATED
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
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                            // PDF DETECTION
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

        public IActionResult Funeralburialapprovedstatus(int id)
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

            // Basic ViewData setup
            ViewData["Status2"] = funeralburialform.Status2;
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

            // Type of assistance
            var typeAssistanceRaw = funeralburialform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = funeralburialform.ForCMOPERSONNEL ?? "";
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
                if (!string.IsNullOrEmpty(funeralburialform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, funeralburialform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(funeralburialform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, funeralburialform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, funeralburialform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {funeralburialform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;

                            // PDF DETECTION
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

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, funeralburialform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {funeralburialform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

                            // PDF DETECTION
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
            ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
            ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;
            ViewData["Comments"] = funeralburialform.Comments;

            return View();
        }

        public IActionResult Medicalandlabformapprovedsstatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.Medicalandlabform.Find(id);
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
                        debugMessages.Add("✅ Front ID decrypted");
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
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

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

                // ⭐ MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"📄 Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"📂 Full path: {medicalPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(medicalPath)}");

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

                            debugMessages.Add($"✅ Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"🔍 IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Medical Certificate in database");
                }

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

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
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            return View();
        }

        //For not approved statuses
        public IActionResult FillupformHospitalBillDisapprovedstatus(int id)
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
            ViewData["Status2"] = fillupformhospitalBill.Status2;
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
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
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
                        byte[] decryptedFront = DecryptFile(frontPath);
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
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED
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
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;

                            // PDF DETECTION
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

                // ⭐ DEATH CERTIFICATE - UPDATED
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
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                            // PDF DETECTION
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

        public IActionResult FuneralburialDisapprovedstatus(int id)
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

            // Basic ViewData setup
            ViewData["Status2"] = funeralburialform.Status2;
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

            // Type of assistance
            var typeAssistanceRaw = funeralburialform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = funeralburialform.ForCMOPERSONNEL ?? "";
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
                if (!string.IsNullOrEmpty(funeralburialform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, funeralburialform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(funeralburialform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, funeralburialform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, funeralburialform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {funeralburialform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;

                            // PDF DETECTION
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

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, funeralburialform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {funeralburialform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

                            // PDF DETECTION
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
            ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
            ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;
            ViewData["Comments"] = funeralburialform.Comments;

            return View();
        }

        public IActionResult MedicalandlabformDisapprovedstatus(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.Medicalandlabform.Find(id);
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
                        debugMessages.Add("✅ Front ID decrypted");
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
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

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

                // ⭐ MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"📄 Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"📂 Full path: {medicalPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(medicalPath)}");

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

                            debugMessages.Add($"✅ Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"🔍 IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Medical Certificate in database");
                }

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

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
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            return View();
        }




        //Claimed statuses
        public IActionResult FillupformHospitalBillUpdatestatuClaimeddocs(int id)
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
            ViewData["Status3"] = fillupformhospitalBill.Status3;
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
            // DECRYPTION SECTION - UPDATED TO USE CONFIGURATION-BASED KEY
            // ====================================
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
                        byte[] decryptedFront = DecryptFile(frontPath);
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
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED
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
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;

                            // PDF DETECTION
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

                // ⭐ DEATH CERTIFICATE - UPDATED
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
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                            // PDF DETECTION
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

        public IActionResult MedicalandlabformstatusUpdateClaimeddocs(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var medicallabform = context.Medicalandlabform.Find(id);
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
                        debugMessages.Add("✅ Front ID decrypted");
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
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

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

                // ⭐ MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"📄 Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"📂 Full path: {medicalPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(medicalPath)}");

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

                            debugMessages.Add($"✅ Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"🔍 IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"🔍 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"❌ Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("❌ Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("ℹ️ No Medical Certificate in database");
                }

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

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
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            return View();
        }
        public IActionResult FuneralburialapprovedstatusUpdateClaimeddocs(int id)
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

            // Basic ViewData setup
            ViewData["Status3"] = funeralburialform.Status3;
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

            // Type of assistance
            var typeAssistanceRaw = funeralburialform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = funeralburialform.ForCMOPERSONNEL ?? "";
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
                if (!string.IsNullOrEmpty(funeralburialform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, funeralburialform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(funeralburialform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, funeralburialform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, funeralburialform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {funeralburialform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;

                            // PDF DETECTION
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

                // ⭐ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(funeralburialform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, funeralburialform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {funeralburialform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

                            // PDF DETECTION
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
            ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
            ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;
            ViewData["Comments"] = funeralburialform.Comments;

            return View();
        }
            // UPDATED ViewPDF METHOD
            [HttpGet]
        public IActionResult ViewPDF(string fileName, string fileType)
        {
            try
            {
                // Authentication check
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
                {
                    return Unauthorized("Please log in to view documents");
                }

                Console.WriteLine($"🔍 ViewPDF called - FileName: {fileName}, FileType: {fileType}");

                // Validate inputs
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

                Console.WriteLine($"📁 Folder path: {folderPath}");

                string encryptedFilePath = Path.Combine(folderPath, safeFileName);
                Console.WriteLine($"📄 Full file path: {encryptedFilePath}");

                // Additional security: Verify the resolved path is within the expected directory
                string resolvedPath = Path.GetFullPath(encryptedFilePath);
                string resolvedFolder = Path.GetFullPath(folderPath);
                if (!resolvedPath.StartsWith(resolvedFolder))
                {
                    Console.WriteLine("❌ Security: Path traversal attempt detected");
                    return BadRequest("Invalid file path");
                }

                // Check if file exists
                if (!System.IO.File.Exists(encryptedFilePath))
                {
                    Console.WriteLine($"❌ File does not exist: {encryptedFilePath}");
                    return NotFound($"File not found: {fileName}");
                }

                Console.WriteLine($"✅ File exists. Size: {new FileInfo(encryptedFilePath).Length} bytes");

                // Decrypt the file USING CONFIGURATION-BASED KEY
                byte[] decryptedBytes = DecryptFile(encryptedFilePath);
                Console.WriteLine($"✅ File decrypted. Decrypted size: {decryptedBytes.Length} bytes");

                // Verify it's actually a PDF
                bool isPdf = IsPdfFile(decryptedBytes);
                Console.WriteLine($"📊 Is PDF: {isPdf}");

                if (!isPdf)
                {
                    Console.WriteLine("❌ File is not a valid PDF");
                    return BadRequest("Only PDF files can be viewed");
                }

                // ⭐ CRITICAL: Set headers to FORCE inline viewing and PREVENT download
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

                Console.WriteLine($"📤 Returning PDF for INLINE VIEWING ONLY (download disabled)");

                // Return as PDF without any filename parameter
                return File(decryptedBytes, "application/pdf");
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine($"💥 DECRYPTION ERROR: {ex.Message}");
                return BadRequest("Failed to decrypt file. Invalid encryption or corrupted file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ERROR in ViewPDF: {ex.Message}");
                Console.WriteLine($"💥 Stack Trace: {ex.StackTrace}");
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


        //approving and unpproving status
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
                // Update record
                fillupformhospitalBill.Status2 = fillupformHospitalbilldto.Status2;
                fillupformhospitalBill.ForCMOPERSONNEL = fillupformHospitalbilldto.ForCMOPERSONNEL;
                fillupformhospitalBill.Comments = fillupformHospitalbilldto.Comments;
                fillupformhospitalBill.Processby = fillupformHospitalbilldto.Processby;
                fillupformhospitalBill.Result = DateTime.Now;
                context.SaveChanges();

                // Get user info from RegisterAcc table
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == fillupformhospitalBill.UserId);

                // ✅ Send automatic email only if user exists and has an email
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get user's first name from AccountVerification table
                    var accountVerification = context.Verifyaccount
                        .FirstOrDefault(av => av.UserId == fillupformhospitalBill.UserId);

                    string userFirstName = "Valued Applicant";

                    if (accountVerification != null && !string.IsNullOrEmpty(accountVerification.Firstname))
                    {
                        userFirstName = accountVerification.Firstname;
                    }
                    else if (!string.IsNullOrEmpty(user.Username))
                    {
                        // Fallback to username if first name not found in AccountVerification
                        userFirstName = user.Username;
                    }

                    var status = fillupformHospitalbilldto.Status2?.Trim().ToLower();
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, userFirstName);

                    string subject;
                    string body;

                    if (status == "approved")
                    {
                        // ✅ Approved message
                        subject = "Hospital Bill Assistance Application Approved - LINGAP DVO";
                        body = $@"
                                Dear {userFirstName},

                                We are pleased to inform you that your Hospital Bill Assistance Application has been successfully approved.

                                APPLICATION DETAILS:
                                • Application Type: Hospital Bill Assistance
                                • Date Approved: {DateTime.Now:MMMM dd, yyyy}

                                REMARKS:
                                {fillupformHospitalbilldto.Comments ?? "Your application has met all the necessary requirements and has been processed accordingly."}

                                NEXT STEPS:
                                Our team will coordinate with the concerned healthcare facility regarding the financial assistance. You may expect further communication from either our office or the hospital administration within the next 3-5 working days.

                                Should you require any clarification or have additional inquiries, please do not hesitate to contact our support team at [Support Email/Phone Number].

                                We are committed to supporting you through this process and hope this assistance provides you with the relief needed during this time.

                                Sincerely,
                                {fromName}
                                LINGAP DVO Medical Assistance Program";
                                                    }
                                                    else if (status == "disapproved")
                                                    {
                                                        // ❌ Unapproved message – same structure, but message adjusted and Comments prioritized
                                                        subject = "Hospital Bill Assistance Application Update - LINGAP DVO";
                                                        body = $@"
                                Dear {userFirstName},

                                We would like to inform you that your Hospital Bill Assistance Application was not approved.

                                APPLICATION DETAILS:
                                • Application Type: Hospital Bill Assistance
                                • Date Reviewed: {DateTime.Now:MMMM dd, yyyy}

                                REMARKS:
                                {fillupformHospitalbilldto.Comments ?? "Please contact our office for further clarification regarding this decision."}

                                NEXT STEPS:
                                If you wish to clarify the result or provide additional supporting documents, please contact our support team at [Support Email/Phone Number] within the next few working days.

                                We remain committed to assisting applicants and encourage you to stay in touch with our office for future programs or available assistance.

                                Sincerely,
                                {fromName}
                                LINGAP DVO Medical Assistance Program";
                    }
                    else
                    {
                        // Skip sending for other statuses
                        subject = null;
                        body = null;
                    }

                    if (!string.IsNullOrEmpty(subject))
                    {
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
                }

                TempData["SuccessMessage"] = "Hospital bill status updated successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(fillupformHospitalbilldto);
            }
        }



        [HttpPost]
        public IActionResult MedicalandlabformUpdateprocessingstatus(int id, MedicalandlabformDto medicalandlabformDto)
        {
            var medicallabform = context.Medicalandlabform.Find(id);

            if (medicallabform == null)
            {
                TempData["ErrorMessage"] = "Medical assistance record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update record
                medicallabform.Status2 = medicalandlabformDto.Status2;
                medicallabform.ForCMOPERSONNEL = medicalandlabformDto.ForCMOPERSONNEL;
                medicallabform.Comments = medicalandlabformDto.Comments;
                medicallabform.Processby = medicalandlabformDto.Processby;
                medicallabform.Result = DateTime.Now;
                context.SaveChanges();

                // Get user info from RegisterAcc table
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == medicallabform.UserId);

                // ✅ Send automatic email only if user exists and has an email
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get user's first name from AccountVerification table
                    var accountVerification = context.Verifyaccount
                        .FirstOrDefault(av => av.UserId == medicallabform.UserId);

                    string userFirstName = "Valued Applicant";

                    if (accountVerification != null && !string.IsNullOrEmpty(accountVerification.Firstname))
                    {
                        userFirstName = accountVerification.Firstname;
                    }
                    else if (!string.IsNullOrEmpty(user.Username))
                    {
                        // Fallback to username if first name not found in AccountVerification
                        userFirstName = user.Username;
                    }

                    var status = medicalandlabformDto.Status2?.Trim().ToLower();
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, userFirstName);

                    string subject;
                    string body;

                    if (status == "approved")
                    {
                        // ✅ Approved message
                        subject = "Medical Assistance Application Approved - LINGAP DVO";
                        body = $@"
                                Dear {userFirstName},

                                We are pleased to inform you that your Medical Assistance Application has been successfully approved.

                                APPLICATION DETAILS:
                                • Application Type: Medical Assistance
                                • Date Approved: {DateTime.Now:MMMM dd, yyyy}

                                REMARKS:
                                {medicalandlabformDto.Comments ?? "Your application has met all the necessary requirements and has been processed accordingly."}

                                NEXT STEPS:
                                Our team will coordinate with the concerned healthcare facility regarding the financial assistance. You may expect further communication from either our office or the hospital administration within the next 3–5 working days.

                                Should you require any clarification or have additional inquiries, please do not hesitate to contact our support team at [Support Email/Phone Number].

                                We are committed to supporting you through this process and hope this assistance provides you with the relief needed during this time.

                                Sincerely,
                                {fromName}
                                LINGAP DVO Medical Assistance Program";
                                                    }
                                                    else if (status == "disapproved")
                                                    {
                                                        // ❌ Unapproved message – same structure but prioritizes comments
                                                        subject = "Medical Assistance Application Update - LINGAP DVO";
                                                        body = $@"
                                Dear {userFirstName},

                                We would like to inform you that your Medical Assistance Application was not approved.

                                APPLICATION DETAILS:
                                • Application Type: Medical Assistance
                                • Date Reviewed: {DateTime.Now:MMMM dd, yyyy}

                                REMARKS:
                                {medicalandlabformDto.Comments ?? "Please contact our office for further clarification regarding this decision."}

                                NEXT STEPS:
                                If you wish to clarify the result or provide additional supporting documents, please contact our support team at [Support Email/Phone Number] within the next few working days.

                                We remain committed to assisting applicants and encourage you to stay in touch with our office for future programs or available assistance.

                                Sincerely,
                                {fromName}
                                LINGAP DVO Medical Assistance Program";
                    }
                    else
                    {
                        // Skip sending for other statuses
                        subject = null;
                        body = null;
                    }

                    // ✅ Send email only if applicable
                    if (!string.IsNullOrEmpty(subject))
                    {
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
                }

                TempData["SuccessMessage"] = "Medical assistance status updated successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(medicalandlabformDto);
            }
        }


        [HttpPost]
        public IActionResult FuneralburialformUpdateprocessingstatus(int id, FuneralburialformDto funeralburialformDto)
        {
            var funeralburialform = context.Funeralburialform.Find(id);

            if (funeralburialform == null)
            {
                TempData["ErrorMessage"] = "Funeral assistance record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // ✅ Update record
                funeralburialform.Status2 = funeralburialformDto.Status2;
                funeralburialform.ForCMOPERSONNEL = funeralburialformDto.ForCMOPERSONNEL;
                funeralburialform.Comments = funeralburialformDto.Comments;
                funeralburialform.Processby = funeralburialformDto.Processby;
                funeralburialform.Result = DateTime.Now;
                context.SaveChanges();

                // ✅ Get user info from RegisterAcc table
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == funeralburialform.UserId);

                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // ✅ Get user's first name from Verifyaccount table
                    var accountVerification = context.Verifyaccount
                        .FirstOrDefault(av => av.UserId == funeralburialform.UserId);

                    string userFirstName = "Valued Applicant";

                    if (accountVerification != null && !string.IsNullOrEmpty(accountVerification.Firstname))
                    {
                        userFirstName = accountVerification.Firstname;
                    }
                    else if (!string.IsNullOrEmpty(user.Username))
                    {
                        userFirstName = user.Username;
                    }

                    // ✅ Load email configuration
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, userFirstName);

                    string subject;
                    string body;

                    var status = funeralburialformDto.Status2?.Trim().ToLower();

                    if (status == "approved")
                    {
                        // ✅ Approved message
                        subject = "Funeral Assistance Application Approved - LINGAP DVO";
                        body = $@"
                        Dear {userFirstName},

                        We are pleased to inform you that your Funeral Assistance Application has been successfully approved.

                        APPLICATION DETAILS:
                        • Application Type: Funeral Assistance
                        • Date Approved: {DateTime.Now:MMMM dd, yyyy}

                        REMARKS:
                        {funeralburialformDto.Comments ?? "Your application has met all the necessary requirements and has been processed accordingly."}

                        NEXT STEPS:
                        Our team will coordinate with the concerned funeral home or facility regarding the financial assistance. You may expect further communication from our office within the next 3–5 working days.

                        Should you require any clarification or have additional inquiries, please do not hesitate to contact our support team at [Support Email/Phone Number].

                        We are committed to supporting you through this process and hope this assistance brings you some relief during this time.

                        Sincerely,
                        {fromName}
                        LINGAP DVO Funeral Assistance Program";
                    }
                    else if (status == "disapproved")
                    {
                        // ❌ Unapproved message
                        subject = "Funeral Assistance Application Update - LINGAP DVO";
                        body = $@"
                        Dear {userFirstName},

                        We would like to inform you that your Funeral Assistance Application was not approved.

                        APPLICATION DETAILS:
                        • Application Type: Funeral Assistance
                        • Date Reviewed: {DateTime.Now:MMMM dd, yyyy}

                        REMARKS:
                        {funeralburialformDto.Comments ?? "Please contact our office for further clarification regarding this decision."}

                        NEXT STEPS:
                        If you wish to clarify the result or provide additional supporting documents, please contact our support team at [Support Email/Phone Number] within the next few working days.

                        We remain committed to assisting applicants and encourage you to stay in touch with our office for future programs or available assistance.

                        Sincerely,
                        {fromName}
                        LINGAP DVO Funeral Assistance Program";
                    }
                    else
                    {
                        subject = null;
                        body = null;
                    }

                    // ✅ Send email only if applicable
                    if (!string.IsNullOrEmpty(subject))
                    {
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
                }

                TempData["SuccessMessage"] = "Funeral assistance status updated successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(funeralburialformDto);
            }
        }



        // ✅ For Approved Statuses to Claimed
        [HttpPost]
        public IActionResult FillupformHospitalBillapprovedstatus(int id, FillupformHospitalBillDto fillupformHospitalBillDto)
        {
            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);

            if (fillupformhospitalBill == null)
            {
                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // ✅ Update record
                fillupformhospitalBill.Status3 = fillupformHospitalBillDto.Status3;
                fillupformhospitalBill.ForCMOPERSONNEL = fillupformHospitalBillDto.ForCMOPERSONNEL;
                fillupformhospitalBill.Comments = fillupformHospitalBillDto.Comments;
                fillupformhospitalBill.Processby = fillupformHospitalBillDto.Processby;
                fillupformhospitalBill.ClaimedAt = DateTime.Now;

                context.SaveChanges();

                // ✅ Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == fillupformhospitalBill.UserId);

                // ✅ Only send email if status is "Claimed"
                if (fillupformHospitalBillDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // ✅ Get first name from VerifyAccount
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // ✅ Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // ✅ Compose email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, firstName);

                    string subject = "Hospital Bill Assistance Claimed - LINGAP DVO";
                    string body = $@"
                        Dear {firstName},

                        We are pleased to inform you that your Hospital Bill Assistance has been successfully claimed as of {DateTime.Now:MMMM dd, yyyy}.

                        APPLICATION DETAILS:
                        • Application Type: Hospital Bill Assistance
                        • Status: Claimed
                        • Processed By: {fillupformHospitalBillDto.Processby ?? "LINGAP Personnel"}
                        • Date Claimed: {DateTime.Now:MMMM dd, yyyy HH:mm tt}

                        REMARKS:
                        {fillupformHospitalBillDto.Comments ?? "Your claim has been processed and recorded successfully."}

                        Thank you for your patience and cooperation throughout the process.  
                        Should you have any further questions, please contact our support team at [Support Email/Phone Number].

                        Sincerely,  
                        {fromName}  
                        LINGAP DVO Medical Assistance Program
                        ";

                    // ✅ Send email
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
                return View(fillupformHospitalBillDto);
            }
        }

        [HttpPost]
        public IActionResult Medicalandlabformapprovedsstatus(int id, MedicalandlabformDto medicalandlabformDto)
        {
            var medicallabform = context.Medicalandlabform.Find(id);

            if (medicallabform == null)
            {
                TempData["ErrorMessage"] = "Medical and laboratory record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // ✅ Update record
                medicallabform.Status3 = medicalandlabformDto.Status3;
                medicallabform.ForCMOPERSONNEL = medicalandlabformDto.ForCMOPERSONNEL;
                medicallabform.Comments = medicalandlabformDto.Comments;
                medicallabform.Processby = medicalandlabformDto.Processby;
                medicallabform.ClaimedAt = DateTime.Now;
                context.SaveChanges();

                // ✅ Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == medicallabform.UserId);

                // ✅ Only send email if status is "Claimed"
                if (medicalandlabformDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true &&
                    user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // ✅ Get user's first name from VerifyAccount
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == user.Id);
                    var firstName = verifyAccount?.Firstname ?? user.Username ?? "Applicant";

                    // ✅ Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // ✅ Compose email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, firstName);

                    string subject = "Medical and Laboratory Assistance Claimed - LINGAP DVO";
                    string body = $@"
                            Dear {firstName},

                            We are pleased to inform you that your Medical and Laboratory Assistance has been successfully claimed as of {DateTime.Now:MMMM dd, yyyy}.

                            APPLICATION DETAILS:
                            • Application Type: Medical and Laboratory Assistance
                            • Status: Claimed
                            • Processed By: {medicalandlabformDto.Processby ?? "LINGAP Personnel"}
                            • Date Claimed: {DateTime.Now:MMMM dd, yyyy HH:mm tt}

                            REMARKS:
                            {medicalandlabformDto.Comments ?? "Your claim has been processed and recorded successfully."}

                            Thank you for your patience and cooperation throughout the process.  
                            Should you have any further questions, please contact our support team at [Support Email/Phone Number].

                            Sincerely,  
                            {fromName}  
                            LINGAP DVO Medical Assistance Program
";

                    // ✅ Send email
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
                return View(medicalandlabformDto);
            }
        }


        [HttpPost]
        public IActionResult Funeralburialapprovedstatus(int id, FuneralburialformDto funeralburialformDto)
        {
            var funeralburialform = context.Funeralburialform.Find(id);

            if (funeralburialform == null)
            {
                TempData["ErrorMessage"] = "Funeral and burial record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update record
                funeralburialform.Status3 = funeralburialformDto.Status3;
                funeralburialform.ForCMOPERSONNEL = funeralburialformDto.ForCMOPERSONNEL;
                funeralburialform.Comments = funeralburialformDto.Comments;
                funeralburialform.Processby = funeralburialformDto.Processby;
                funeralburialform.ClaimedAt = DateTime.Now;
                context.SaveChanges();

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == funeralburialform.UserId);

                // ✅ Send automatic email only if Claimed
                if (funeralburialformDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings from configuration
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                    {
                        throw new ArgumentException("Email settings are missing.");
                    }

                    // Compose auto-generated email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Username ?? "User");

                    string subject = "Your LINGAP Funeral Assistance Has Been Claimed";
                    string body = $@"
                            Dear {user.Username ?? "Valued Applicant"},

                            We are pleased to inform you that your LINGAP Funeral Assistance has been successfully claimed as of {DateTime.Now:MMMM dd, yyyy}.

                            APPLICATION DETAILS:
                            • Application Type: Funeral Assistance  
                            • Status: Claimed  
                            • Processed By: {funeralburialformDto.Processby ?? "LINGAP Personnel"}

                            REMARKS:
                            {funeralburialformDto.Comments ?? "Your claim has been processed and recorded successfully."}

                            Thank you for your patience and cooperation throughout the process.  
                            Should you have any further questions, feel free to contact our support team.

                            Best regards,  
                            {fromName}  
                            LINGAP DVO Medical Assistance Program
                            ";

                    // ✅ Send email with proper using blocks
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
                return View(funeralburialformDto);
            }
        }

        // Priorities page with priority system
        public IActionResult Priorities()
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

            return View(viewModel);
        }

        // Get notification count for sidebar badge
        [HttpGet]
        public IActionResult GetNotificationCount()
        {
            try
            {
                var now = DateTime.Now;

                // Get all applications
                var hospitalBills = context.FillupformHospitalBill.ToList();
                var medicalLabForms = context.Medicalandlabform.ToList();
                var funeralburialform = context.Funeralburialform.ToList();

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
                foreach (var form in funeralburialform)
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


    }


}
