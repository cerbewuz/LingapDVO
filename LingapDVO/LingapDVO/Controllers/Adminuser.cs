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

                // Get the user's email using UserId
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == fillupformhospitalBill.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings from configuration
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                    {
                        throw new ArgumentException("Email address or display name is missing.");
                    }

                    // Compose and send the email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Email?? "User");

                    string subject = "Hospital Bill Assistance Update";
                    string body = $@"
                                    Dear {user.Email ?? "User"},

                                    Your hospital bill request is now being processed.

                                    Status: Processing  
                                    Comments: {fillupformHospitalbilldto.Comments ?? "N/A"}  

                                    Thank you for your patience.

                                    Best regards,  
                                    {fromName}  
                                    {DateTime.Now:MMMM dd, yyyy HH:mm tt}
                                    ";

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

                // Get the user's email using UserId
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == medicalandlabform.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings from configuration
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                    {
                        throw new ArgumentException("Email address or display name is missing.");
                    }

                    // Compose and send the email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Email ?? "User");

                    string subject = "Medical and Laboratory Assistance Update";
                    string body = $@"
                            Dear {user.Email ?? "User"},

                            Your medical and laboratory request is now being processed.

                            Status: Processing  
                            Comments: {medicalandlabformDto.Comments ?? "N/A"}  

                            Thank you for your patience.

                            Best regards,  
                            {fromName}  
                            {DateTime.Now:MMMM dd, yyyy HH:mm tt}
                            ";

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
                // Automatically set status to "Processing"
                funeralburialform.Status = "Processing";
                funeralburialform.Comments = funeralburialformDto.Comments;
                funeralburialform.Processby = funeralburialformDto.Processby;
                funeralburialform.ProcessAt = DateTime.Now;

                context.SaveChanges();

                // Get the user's email using UserId
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == funeralburialform.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings from configuration
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                    {
                        throw new ArgumentException("Email address or display name is missing.");
                    }

                    // Compose and send the email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Email ?? "User");

                    string subject = "Funeral and Burial Assistance Update";
                    string body = $@"
                            Dear {user.Email ?? "User"},

                            Your funeral and burial assistance request is now being processed.

                            Status: Processing  
                            Comments: {funeralburialformDto.Comments ?? "N/A"}  

                            Thank you for your patience.

                            Best regards,  
                            {fromName}  
                            {DateTime.Now:MMMM dd, yyyy HH:mm tt}
                            ";

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
            // DECRYPTION SECTION WITH PROPER PDF DETECTION
            // ====================================
            string masterPassword = "SuperAdminMasterKey123!";
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
                        byte[] decryptedFront = DecryptFile(frontPath, masterPassword);
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
                        byte[] decryptedBack = DecryptFile(backPath, masterPassword);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - FIXED PDF DETECTION
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
                            byte[] decryptedPresc = DecryptFile(prescPath, masterPassword);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = medicallabform.DoctorPrescription;

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

                // ⭐ MEDICAL CERTIFICATE - SPECIFIC TO MEDICAL/LAB FORM
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
                            byte[] decryptedMedical = DecryptFile(medicalPath, masterPassword);
                            ViewData["MedicalCertificateBase64"] = Convert.ToBase64String(decryptedMedical);
                            ViewData["MedicalCertificate"] = medicallabform.MedCertificate;

                            // ⭐⭐⭐ PROPER PDF DETECTION ⭐⭐⭐
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

                // ⭐ DEATH CERTIFICATE - FIXED PDF DETECTION
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
                            byte[] decryptedDeath = DecryptFile(deathPath, masterPassword);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = medicallabform.DeathCertificate;

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
                if (!string.IsNullOrEmpty(funeralburialform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, funeralburialform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath, masterPassword);
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
                        byte[] decryptedBack = DecryptFile(backPath, masterPassword);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - FIXED PDF DETECTION
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
                            byte[] decryptedPresc = DecryptFile(prescPath, masterPassword);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;

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
                            byte[] decryptedDeath = DecryptFile(deathPath, masterPassword);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

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
                if (!string.IsNullOrEmpty(funeralburialform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, funeralburialform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath, masterPassword);
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
                        byte[] decryptedBack = DecryptFile(backPath, masterPassword);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - FIXED PDF DETECTION
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
                            byte[] decryptedPresc = DecryptFile(prescPath, masterPassword);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;

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
                            byte[] decryptedDeath = DecryptFile(deathPath, masterPassword);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

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



            var  medicalandlabform = context.Medicalandlabform.Find(id);
            if ( medicalandlabform == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status2"] =  medicalandlabform.Status2;
            ViewData["Id"] =  medicalandlabform.Id;
            ViewData["Lastname"] =  medicalandlabform.Lastname;
            ViewData["Firstname"] =  medicalandlabform.Firstname;
            ViewData["Middlename"] =  medicalandlabform.Middlename;
            ViewData["Suffix"] =  medicalandlabform.Suffix;
            ViewData["BlkLotStreet"] =  medicalandlabform.BlkLotStreet;
            ViewData["SubVill"] =  medicalandlabform.SubVill;
            ViewData["Brgy"] =  medicalandlabform.Brgy;
            ViewData["District"] =  medicalandlabform.District;
            ViewData["Sex"] =  medicalandlabform.Sex;
            ViewData["PhilHealth"] =  medicalandlabform.PhilHealth;
            ViewData["PhilHealthNo"] =  medicalandlabform.PhilHealthNo;
            ViewData["Dateofbirth"] =  medicalandlabform.Dateofbirth;
            ViewData["Age"] =  medicalandlabform.Age;

            // Requestor details
            ViewData["RLastname"] =  medicalandlabform.RLastname;
            ViewData["RFirstname"] =  medicalandlabform.RFirstname;
            ViewData["RMiddlename"] =  medicalandlabform.RMiddlename;
            ViewData["RSuffix"] =  medicalandlabform.RSuffix;
            ViewData["RBlkLotStreet"] =  medicalandlabform.RBlkLotStreet;
            ViewData["RSubVill"] =  medicalandlabform.RSubVill;
            ViewData["RBrgy"] =  medicalandlabform.RBrgy;
            ViewData["RDistrict"] =  medicalandlabform.RDistrict;
            ViewData["RelationshipPatient"] =  medicalandlabform.RelationshipPatient;
            ViewData["ContactNo"] =  medicalandlabform.ContactNo;

            // Type of assistance
            var typeAssistanceRaw =  medicalandlabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw =  medicalandlabform.ForCMOPERSONNEL ?? "";
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
                if (!string.IsNullOrEmpty( medicalandlabform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder,  medicalandlabform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath, masterPassword);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty( medicalandlabform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder,  medicalandlabform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath, masterPassword);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - FIXED PDF DETECTION
                if (!string.IsNullOrEmpty( medicalandlabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder,  medicalandlabform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: { medicalandlabform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath, masterPassword);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] =  medicalandlabform.DoctorPrescription;

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
                if (!string.IsNullOrEmpty( medicalandlabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder,  medicalandlabform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: { medicalandlabform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath, masterPassword);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] =  medicalandlabform.DeathCertificate;

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
            ViewData["Validfrontimage"] =  medicalandlabform.Validfrontimage;
            ViewData["ValidBackimage"] =  medicalandlabform.ValidBackimage;
            ViewData["Comments"] =  medicalandlabform.Comments;

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
            ViewData["Comments"] = fillupformhospitalBill.Comments;
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


        public IActionResult MedicalandlabformstatusUpdateClaimeddocs(int id)
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

            // Basic ViewData setup
            ViewData["Status3"] = medicalandlabform.Status3;
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

            // Type of assistance
            var typeAssistanceRaw = medicalandlabform.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = medicalandlabform.ForCMOPERSONNEL ?? "";
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
                if (!string.IsNullOrEmpty(medicalandlabform.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, medicalandlabform.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath, masterPassword);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                        debugMessages.Add("✅ Front ID decrypted");
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(medicalandlabform.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, medicalandlabform.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath, masterPassword);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                        debugMessages.Add("✅ Back ID decrypted");
                    }
                }

                // ⭐ DOCTOR PRESCRIPTION - FIXED PDF DETECTION
                if (!string.IsNullOrEmpty(medicalandlabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicalandlabform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {medicalandlabform.DoctorPrescription}");
                    debugMessages.Add($"📂 Full path: {prescPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(prescPath)}");

                    if (System.IO.File.Exists(prescPath))
                    {
                        try
                        {
                            byte[] decryptedPresc = DecryptFile(prescPath, masterPassword);
                            ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedPresc);
                            ViewData["DoctorPrescription"] = medicalandlabform.DoctorPrescription;

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
                if (!string.IsNullOrEmpty(medicalandlabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicalandlabform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {medicalandlabform.DeathCertificate}");
                    debugMessages.Add($"📂 Full path: {deathPath}");
                    debugMessages.Add($"📁 File exists: {System.IO.File.Exists(deathPath)}");

                    if (System.IO.File.Exists(deathPath))
                    {
                        try
                        {
                            byte[] decryptedDeath = DecryptFile(deathPath, masterPassword);
                            ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDeath);
                            ViewData["DeathCertificate"] = medicalandlabform.DeathCertificate;

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
            ViewData["Validfrontimage"] = medicalandlabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicalandlabform.ValidBackimage;
            ViewData["Comments"] = medicalandlabform.Comments;

            return View();
        }

        public IActionResult FuneralburialapprovedstatusUpdateClaimeddocs(int id)
        {
            return View();
        }



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

                // Define the directory based on file type - ADD MEDICAL CERTIFICATE CASE
                string folderPath = fileType.ToLower() switch
                {
                    "doctorprescription" => Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage"),
                    "deathcertificate" => Path.Combine(environment.WebRootPath, "Funeralimg"),
                    "medicalcertificate" => Path.Combine(environment.WebRootPath, "MedCertificateimage"), // ADD THIS LINE
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

                // Decrypt the file
                string masterPassword = "SuperAdminMasterKey123!";
                byte[] decryptedBytes = DecryptFile(encryptedFilePath, masterPassword);
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
                // Remove any filename reference to avoid browser download prompts
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



        // Keep the CheckFileType helper method as-is
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
                case "medicalcertificate": // ADD THIS CASE
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

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == fillupformhospitalBill.UserId);

                // ✅ Send automatic email only if Approved
                if (fillupformHospitalbilldto.Status2?.Equals("Approved", StringComparison.OrdinalIgnoreCase) == true && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Compose auto-generated email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Username ?? "User");

                    string subject = "Hospitall bill Assistance Application Approved - LINGAP DVO";
                    string body = $@"
                            Dear {user.Username ?? "Valued Applicant"},

                            We are pleased to inform you that your Hospitall bill Assistance  Application has been successfully approved.

                            APPLICATION DETAILS:
                            • Application Type: Hospitall bill Assistance
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

                    // Send email
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
                TempData["ErrorMessage"] = "Hospital bill record not found.";
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

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == medicallabform.UserId);

                // ✅ Send automatic email only if Approved
                if (medicalandlabformDto.Status2?.Equals("Approved", StringComparison.OrdinalIgnoreCase) == true && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Compose auto-generated email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Username ?? "User");

                    string subject = "Medical Assistance Application Approved - LINGAP DVO";
                    string body = $@"
                            Dear {user.Username ?? "Valued Applicant"},

                            We are pleased to inform you that your Medical Assistance Application has been successfully approved.

                            APPLICATION DETAILS:
                            • Application Type: Medical Assistance Application
                            • Date Approved: {DateTime.Now:MMMM dd, yyyy}

                            REMARKS:
                            {medicalandlabformDto.Comments ?? "Your application has met all the necessary requirements and has been processed accordingly."}

                            NEXT STEPS:
                            Our team will coordinate with the concerned healthcare facility regarding the financial assistance. You may expect further communication from either our office or the hospital administration within the next 3-5 working days.

                            Should you require any clarification or have additional inquiries, please do not hesitate to contact our support team at [Support Email/Phone Number].

                            We are committed to supporting you through this process and hope this assistance provides you with the relief needed during this time.

                            Sincerely,

                            {fromName}
                            LINGAP DVO Medical Assistance Program";

                    // Send email
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

                TempData["SuccessMessage"] = "Hospital bill status updated successfully.";
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
                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update record
                funeralburialform.Status2 = funeralburialformDto.Status2;
                funeralburialform.ForCMOPERSONNEL = funeralburialformDto.ForCMOPERSONNEL;
                funeralburialform.Comments = funeralburialformDto.Comments;
                funeralburialform.Processby = funeralburialformDto.Processby;
                funeralburialform.Result = DateTime.Now;
                context.SaveChanges();

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == funeralburialform.UserId);

                // ✅ Send automatic email only if Approved
                if (funeralburialformDto.Status2?.Equals("Approved", StringComparison.OrdinalIgnoreCase) == true && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Compose auto-generated email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Username ?? "User");

                    string subject = "Funeral Assistance Application Approved - LINGAP DVO";
                    string body = $@"
                            Dear {user.Username ?? "Valued Applicant"},

                            We are pleased to inform you that your Funeral Assistance Application has been successfully approved.

                            APPLICATION DETAILS:
                            • Application Type: Funeral Assistance Application
                            • Date Approved: {DateTime.Now:MMMM dd, yyyy}

                            REMARKS:
                            {funeralburialformDto.Comments ?? "Your application has met all the necessary requirements and has been processed accordingly."}

                            NEXT STEPS:
                            Our team will coordinate with the concerned healthcare facility regarding the financial assistance. You may expect further communication from either our office or the hospital administration within the next 3-5 working days.

                            Should you require any clarification or have additional inquiries, please do not hesitate to contact our support team at [Support Email/Phone Number].

                            We are committed to supporting you through this process and hope this assistance provides you with the relief needed during this time.

                            Sincerely,

                            {fromName}
                            LINGAP DVO Medical Assistance Program";

                    // Send email
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

                TempData["SuccessMessage"] = "Hospital bill status updated successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(funeralburialformDto);
            }
        }

        //For Approved Statuses to claimed 
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
                // Update record
                fillupformhospitalBill.Status3 = fillupformHospitalBillDto.Status3;
                fillupformhospitalBill.ForCMOPERSONNEL = fillupformHospitalBillDto.ForCMOPERSONNEL;
                fillupformhospitalBill.Comments = fillupformHospitalBillDto.Comments;
                fillupformhospitalBill.Processby = fillupformHospitalBillDto.Processby;
                fillupformhospitalBill.ClaimedAt = DateTime.Now;
                context.SaveChanges();

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == fillupformhospitalBill.UserId);

                // ✅ Send automatic email only if Claimed
                if (fillupformHospitalBillDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Compose auto-generated email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Username ?? "User");

                    string subject = "Your LINGAP Assistance Has Been Claimed";
                    string body = $@"
                                    Dear {user.Username ?? "Valued Applicant"},

                                    We are glad to inform you that your LINGAP Hospital Bill Assistance has been successfully **claimed** as of {DateTime.Now:MMMM dd, yyyy}.

                                    APPLICATION DETAILS:
                                    • Application Type: Hospital Bill Assistance  
                                    • Status: Claimed  
                                    • Processed By: {fillupformHospitalBillDto.Processby ?? "LINGAP Personnel"}

                                    REMARKS:
                                    {fillupformHospitalBillDto.Comments ?? "Your claim has been processed and recorded successfully."}

                                    Thank you for your patience and cooperation throughout the process.  
                                    Should you have any further questions, feel free to contact our support team at [Support Email/Phone Number].

                                    Best regards,  
                                    {fromName}  
                                    LINGAP DVO Medical Assistance Program
                                    ";

                    // Send email
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

                TempData["SuccessMessage"] = "Hospital bill claimed successfully.";
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
            var medicallabform  = context.Medicalandlabform.Find(id);

            if (medicallabform  == null)
            {
                TempData["ErrorMessage"] = "Hospital bill record not found.";
                return Redirect("/Admin");
            }

            try
            {
                // Update record
                medicallabform .Status3 = medicalandlabformDto.Status3;
                medicallabform .ForCMOPERSONNEL = medicalandlabformDto.ForCMOPERSONNEL;
                medicallabform .Comments = medicalandlabformDto.Comments;
                medicallabform .Processby = medicalandlabformDto.Processby;
                medicallabform .ClaimedAt = DateTime.Now;
                context.SaveChanges();

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == medicallabform .UserId);

                // ✅ Send automatic email only if Claimed
                if (medicalandlabformDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Compose auto-generated email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Username ?? "User");

                    string subject = "Your LINGAP Medical Assistance Has Been Claimed";
                    string body = $@"
                                    Dear {user.Username ?? "Valued Applicant"},

                                    We are glad to inform you that your LINGAP Medical Assistance has been successfully **claimed** as of {DateTime.Now:MMMM dd, yyyy}.

                                    APPLICATION DETAILS:
                                    • Application Type: Medical Assistance  
                                    • Status: Claimed  
                                    • Processed By: {medicalandlabformDto.Processby ?? "LINGAP Personnel"}

                                    REMARKS:
                                    {medicalandlabformDto.Comments ?? "Your claim has been processed and recorded successfully."}

                                    Thank you for your patience and cooperation throughout the process.  
                                    Should you have any further questions, feel free to contact our support team at [Support Email/Phone Number].

                                    Best regards,  
                                    {fromName}  
                                    LINGAP DVO Medical Assistance Program
                                    ";

                    // Send email
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

                TempData["SuccessMessage"] = "Hospital bill claimed successfully.";
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
                TempData["ErrorMessage"] = "Hospital bill record not found.";
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
                    // Get email settings
                    var fromEmail = _configuration["EmailSettings:FromEmail"];
                    var fromName = _configuration["EmailSettings:FromName"];
                    var fromPassword = _configuration["EmailSettings:FromPassword"];

                    if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromName))
                        throw new ArgumentException("Email settings are missing.");

                    // Compose auto-generated email
                    var fromAddress = new MailAddress(fromEmail, fromName);
                    var toAddress = new MailAddress(user.Email, user.Username ?? "User");

                    string subject = "Your LINGAP Assistance Has Been Claimed";
                    string body = $@"
                                    Dear {user.Username ?? "Valued Applicant"},

                                    We are glad to inform you that your LINGAP  Funeral Assistance has been successfully **claimed** as of {DateTime.Now:MMMM dd, yyyy}.

                                    APPLICATION DETAILS:
                                    • Application Type: Funeral Assistance  
                                    • Status: Claimed  
                                    • Processed By: {funeralburialformDto.Processby ?? "LINGAP Personnel"}

                                    REMARKS:
                                    {funeralburialformDto.Comments ?? "Your claim has been processed and recorded successfully."}

                                    Thank you for your patience and cooperation throughout the process.  
                                    Should you have any further questions, feel free to contact our support team at [Support Email/Phone Number].

                                    Best regards,  
                                    {fromName}  
                                    LINGAP DVO Medical Assistance Program
                                    ";

                    // Send email
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

                TempData["SuccessMessage"] = "Hospital bill claimed successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating status: " + ex.Message);
                return View(funeralburialformDto);
            }
        }



    }


}
