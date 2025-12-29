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
        private readonly IDateTimeService _dateTimeService;
        private readonly IAesEncryptionService _aesEncryptionService;

        public Adminuser(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration, ISessionConfigurationService sessionConfig, IMultiChannelNotificationService notificationService, PriorityTrackingService priorityService, IDateTimeService dateTimeService, IAesEncryptionService aesEncryptionService)
        {
            this.context = context;
            this.environment = environment;
            _configuration = configuration;
            _sessionConfig = sessionConfig;
            _notificationService = notificationService;
            _priorityService = priorityService;
            _dateTimeService = dateTimeService;
            _aesEncryptionService = aesEncryptionService;
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

            // Get all data from the database without filtering by userId, but exclude removed applications
            var hospitalBills = context.HospitalAssistance
                .Where(f => f.Status != "Removed")
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.OtherAssistance
                .Where(f => f.Status != "Removed")
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var FuneralAssistance = context.FuneralAssistance
                .Where(f => f.Status != "Removed")
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

            // Additional Information - Encrypted Fields
            ViewData["HospitalFacilityName"] = HospitalAssistance.HospitalFacilityName;
            ViewData["HospitalFacilityAddress"] = HospitalAssistance.HospitalFacilityAddress;
            ViewData["DiagnosisMedicalCondition"] = HospitalAssistance.DiagnosisMedicalCondition;
            ViewData["HospitalBillCost"] = HospitalAssistance.HospitalBillCost;
            ViewData["AdmissionDate"] = HospitalAssistance.AdmissionDate;
            ViewData["DischargeDate"] = HospitalAssistance.DischargeDate;
            ViewData["WardRoomType"] = HospitalAssistance.WardRoomType;

            return View();

        }

        //renvic edit sa grammar
        [HttpPost]
        public async Task<IActionResult> HospitalAssistancePendingStatus(int id, HospitalAssistanceDto HospitalAssistanceDto)
        {
            try
            {
                var HospitalAssistance = context.HospitalAssistance.Find(id);

                if (HospitalAssistance == null)
                {
                    TempData["ErrorMessage"] = "Hospital bill record not found.";
                    return Redirect("/Admin");
                }

                // Automatically set status to "Processing"
                HospitalAssistance.Status = "Processing";
                HospitalAssistance.ForCMOPERSONNEL = HospitalAssistanceDto.ForCMOPERSONNEL;
                HospitalAssistance.Comments = HospitalAssistanceDto.Comments;
                HospitalAssistance.Processby = HospitalAssistanceDto.Processby;
                HospitalAssistance.ProcessAt = _dateTimeService.Now;

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

                    string subject = "Your Hospital Bill Help is Being Checked - LingapDVO";
                    string body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background-color: #0066cc; color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #0066cc; margin: 20px 0; border-radius: 4px; }}
        .details-box h3 {{ margin-top: 0; color: #0066cc; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📋 We Are Checking Your Application</h1>
        </div>
        <div class='content'>
            <p class='greeting'>Hello {firstName},</p>
            <p class='message'>
                Good news! We are now checking your Hospital Bill Help request.
                We will send you another message soon to let you know the result.
            </p>

            <div class='details-box'>
                <h3>YOUR APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Help Type:</span>
                    <span class='detail-value'>Hospital Bill Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value'><strong>Being Checked</strong></span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Updated:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Notes:</span>
                    <span class='detail-value'>{HospitalAssistanceDto.Comments ?? "None"}</span>
                </div>
            </div>

            <p class='message'>
                Thank you for waiting. We will update you within 1-2 hours.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Medical Help Program</strong></p>
            <p>This is an automatic message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

                    // Send the email safely using async
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
                        IsBodyHtml = true
                    })
                    {
                        await smtp.SendMailAsync(message);
                    }

                    Console.WriteLine($"Processing status email sent to {user.Email} for application {HospitalAssistance.Id}");
                }

                TempData["SuccessMessage"] = "Hospital bill status set to 'Processing' and notifications sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HospitalAssistancePendingStatus: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = "An error occurred while updating status: " + ex.Message;
                return Redirect("/Admin");
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

            // Additional Information - Encrypted Fields (All fields for conditional display)
            ViewData["MedicineName"] = OtherAssistance.MedicineName;
            ViewData["MedicineQuantity"] = OtherAssistance.MedicineQuantity;
            ViewData["MedicineCost"] = OtherAssistance.MedicineCost;
            ViewData["PrescribingDoctor"] = OtherAssistance.PrescribingDoctor;
            ViewData["DoctorContactDetail"] = OtherAssistance.DoctorContactDetail;
            ViewData["LaboratoryCenterName"] = OtherAssistance.LaboratoryCenterName;
            ViewData["LaboratoryCenterAddress"] = OtherAssistance.LaboratoryCenterAddress;
            ViewData["TestName"] = OtherAssistance.TestName;
            ViewData["TestCost"] = OtherAssistance.TestCost;
            ViewData["TestOtherInfo"] = OtherAssistance.TestOtherInfo;
            ViewData["TherapyFacilityName"] = OtherAssistance.TherapyFacilityName;
            ViewData["TherapyFacilityAddress"] = OtherAssistance.TherapyFacilityAddress;
            ViewData["TherapyFacilityContact"] = OtherAssistance.TherapyFacilityContact;
            ViewData["TherapyType"] = OtherAssistance.TherapyType;
            ViewData["EquipmentName"] = OtherAssistance.EquipmentName;
            ViewData["EquipmentBrand"] = OtherAssistance.EquipmentBrand;
            ViewData["EquipmentCategory"] = OtherAssistance.EquipmentCategory;
            ViewData["EquipmentQuantity"] = OtherAssistance.EquipmentQuantity;
            ViewData["EquipmentCost"] = OtherAssistance.EquipmentCost;

            return View();

        }



        [HttpPost]
        public async Task<IActionResult> OtherAssistancePendingStatus(int id, OtherAssistanceDto OtherAssistanceDto)
        {
            try
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

                // Automatically set status to "Processing"
                OtherAssistance.Status = "Processing";
                OtherAssistance.Comments = OtherAssistanceDto.Comments;
                OtherAssistance.Processby = OtherAssistanceDto.Processby;
                OtherAssistance.ProcessAt = _dateTimeService.Now;

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

                    string subject = "Your Medical Help is Being Checked - LingapDVO";
                    string body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background-color: #0066cc; color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #0066cc; margin: 20px 0; border-radius: 4px; }}
        .details-box h3 {{ margin-top: 0; color: #0066cc; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📋 We Are Checking Your Application</h1>
        </div>
        <div class='content'>
            <p class='greeting'>Hello {firstName},</p>
            <p class='message'>
                Good news! We are now checking your Medical and Laboratory Help request.
                We will send you another message soon to let you know the result.
            </p>

            <div class='details-box'>
                <h3>YOUR APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Help Type:</span>
                    <span class='detail-value'>Medical and Laboratory Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value'><strong>Being Checked</strong></span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Updated:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Notes:</span>
                    <span class='detail-value'>{OtherAssistanceDto.Comments ?? "None"}</span>
                </div>
            </div>

            <p class='message'>
                Thank you for waiting. We will update you within 1-2 hours.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Medical Help Program</strong></p>
            <p>This is an automatic message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

                    // Send email using async
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
                        IsBodyHtml = true
                    })
                    {
                        await smtp.SendMailAsync(message);
                    }

                    Console.WriteLine($"Processing status email sent to {user.Email} for Medical and Laboratory application {OtherAssistance.Id}");
                }

                TempData["SuccessMessage"] = "Medical and laboratory status set to 'Processing' and notifications sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OtherAssistancePendingStatus: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = "An error occurred while updating status: " + ex.Message;
                return Redirect("/Admin");
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

            // Additional Information - Encrypted Fields
            ViewData["DeceasedPersonName"] = FuneralAssistance.DeceasedPersonName;
            ViewData["RelationshipToDeceased"] = FuneralAssistance.RelationshipToDeceased;
            ViewData["DateOfDeath"] = FuneralAssistance.DateOfDeath;
            ViewData["TimeOfDeath"] = FuneralAssistance.TimeOfDeath;
            ViewData["CauseOfDeath"] = FuneralAssistance.CauseOfDeath;
            ViewData["FuneralHomeName"] = FuneralAssistance.FuneralHomeName;
            ViewData["FuneralHomeAddress"] = FuneralAssistance.FuneralHomeAddress;
            ViewData["BurialCremationDate"] = FuneralAssistance.BurialCremationDate;
            ViewData["BurialCremationTime"] = FuneralAssistance.BurialCremationTime;
            ViewData["BurialCremationType"] = FuneralAssistance.BurialCremationType;

            return View();

        }

        // Renvic edit sa grammar
        [HttpPost]
        public async Task<IActionResult> FuneralAssistancePendingStatus(int id, FuneralAssistanceDto FuneralAssistanceDto)
        {
            try
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

                // Automatically set status to "Processing"
                FuneralAssistance.Status = "Processing";
                FuneralAssistance.Comments = FuneralAssistanceDto.Comments;
                FuneralAssistance.Processby = FuneralAssistanceDto.Processby;
                FuneralAssistance.ProcessAt = _dateTimeService.Now;

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

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == FuneralAssistance.UserId);

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

                    string subject = "Your Funeral Help is Being Checked - LingapDVO";
                    string body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background-color: #6c757d; color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #6c757d; margin: 20px 0; border-radius: 4px; }}
        .details-box h3 {{ margin-top: 0; color: #6c757d; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📋 We Are Checking Your Application</h1>
        </div>
        <div class='content'>
            <p class='greeting'>Hello {firstName},</p>
            <p class='message'>
                We are now checking your Funeral and Burial Help request.
                We will send you another message soon to let you know the result.
            </p>

            <div class='details-box'>
                <h3>YOUR APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Help Type:</span>
                    <span class='detail-value'>Funeral and Burial Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value'><strong>Being Checked</strong></span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Updated:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Notes:</span>
                    <span class='detail-value'>{FuneralAssistanceDto.Comments ?? "None"}</span>
                </div>
            </div>

            <p class='message'>
                Thank you for waiting. We will update you within 1-2 hours.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Funeral Help Program</strong></p>
            <p>This is an automatic message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

                    // Send email using async
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
                        IsBodyHtml = true
                    })
                    {
                        await smtp.SendMailAsync(message);
                    }

                    Console.WriteLine($"Processing status email sent to {user.Email} for Funeral and Burial application {FuneralAssistance.Id}");
                }

                TempData["SuccessMessage"] = "Funeral and burial status set to 'Processing' and notifications sent successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FuneralAssistancePendingStatus: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = "An error occurred while updating status: " + ex.Message;
                return Redirect("/Admin");
            }
        }

        public async Task<IActionResult> Analyticsdashboard()
        {
            // Get all data from the database without filtering by userId, but exclude removed applications
            var hospitalBills = context.HospitalAssistance
                .Where(f => f.Status != "Removed")
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.OtherAssistance
                .Where(f => f.Status != "Removed")
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var FuneralAssistance = context.FuneralAssistance
                .Where(f => f.Status != "Removed")
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

            byte[]? key = keyField.GetValue(aesHelper) as byte[];
            if (key == null)
                throw new InvalidOperationException("AES key is null");

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

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

            // ============================================
            // ADD DECRYPTION FOR THESE 7 FIELDS ONLY
            // ============================================

            // Use your existing DecryptFile logic to decrypt text fields
            ViewData["HospitalFacilityName"] = DecryptFieldText(HospitalAssistance.HospitalFacilityName);
            ViewData["HospitalFacilityAddress"] = DecryptFieldText(HospitalAssistance.HospitalFacilityAddress);
            ViewData["DiagnosisMedicalCondition"] = DecryptFieldText(HospitalAssistance.DiagnosisMedicalCondition);
            ViewData["HospitalBillCost"] = DecryptFieldText(HospitalAssistance.HospitalBillCost);
            ViewData["AdmissionDate"] = DecryptFieldText(HospitalAssistance.AdmissionDate);
            ViewData["DischargeDate"] = DecryptFieldText(HospitalAssistance.DischargeDate);
            ViewData["WardRoomType"] = DecryptFieldText(HospitalAssistance.WardRoomType);

            return View();
        }

        // ADD THIS HELPER METHOD INSIDE YOUR CONTROLLER CLASS
        private string DecryptFieldText(string encryptedText)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedText))
                    return "";

                // If it's not Base64, it's probably already decrypted
                if (!IsBase64String(encryptedText))
                    return encryptedText;

                // Convert from Base64
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                // Use the same decryption logic as your DecryptFile method
                using var memoryStream = new MemoryStream(encryptedBytes);

                // Read IV (first 16 bytes)
                byte[] iv = new byte[16];
                memoryStream.Read(iv, 0, iv.Length);

                // Get AES key from your configuration helper
                var aesHelper = new AesEncryptionHelper(_configuration);
                var keyField = typeof(AesEncryptionHelper).GetField("_aesKey",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (keyField == null)
                    return "[Key Error]";

                byte[]? key = keyField.GetValue(aesHelper) as byte[];
                if (key == null)
                    return "[Key Error]";

                // Decrypt
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

                // Convert to text
                return Encoding.UTF8.GetString(decryptedStream.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DecryptFieldText error: {ex.Message}");
                // Return original text if decryption fails
                return encryptedText ?? "[Error]";
            }
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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");

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
                        debugMessages.Add("✓ Front ID decrypted");
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
                        debugMessages.Add("✓ Back ID decrypted");
                    }
                }

                // ✓ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, medicallabform.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {medicallabform.DoctorPrescription}");
                    debugMessages.Add($"📄 Full path: {prescPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(prescPath)}");

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

                            debugMessages.Add($"✓ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"📄 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Doctor Prescription in database");
                }

                // ✓ MEDICAL CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.MedCertificate))
                {
                    string medicalPath = Path.Combine(medicalCertificateFolder, medicallabform.MedCertificate);
                    debugMessages.Add($"📄 Medical Certificate filename: {medicallabform.MedCertificate}");
                    debugMessages.Add($"📄 Full path: {medicalPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(medicalPath)}");

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

                            debugMessages.Add($"✓ Medical Certificate decrypted - {decryptedMedical.Length} bytes");
                            debugMessages.Add($"📄 IsMedicalCertificatePdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Medical Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Medical Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Medical Certificate in database");
                }

                // ✓ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(medicallabform.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, medicallabform.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {medicallabform.DeathCertificate}");
                    debugMessages.Add($"📄 Full path: {deathPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(deathPath)}");

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

                            debugMessages.Add($"✓ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"📄 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"⚠️ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = medicallabform.Validfrontimage;
            ViewData["ValidBackimage"] = medicallabform.ValidBackimage;
            ViewData["Comments"] = medicallabform.Comments;

            // ============================================
            // ADD DECRYPTION FOR THESE 19 FIELDS ONLY
            // ============================================

            // Medicine Assistance Fields
            ViewData["MedicineName"] = DecryptFieldText(medicallabform.MedicineName);
            ViewData["MedicineQuantity"] = DecryptFieldText(medicallabform.MedicineQuantity);
            ViewData["MedicineCost"] = DecryptFieldText(medicallabform.MedicineCost);
            ViewData["PrescribingDoctor"] = DecryptFieldText(medicallabform.PrescribingDoctor);
            ViewData["DoctorContactDetail"] = DecryptFieldText(medicallabform.DoctorContactDetail);

            // Laboratory Assistance Fields
            ViewData["LaboratoryCenterName"] = DecryptFieldText(medicallabform.LaboratoryCenterName);
            ViewData["LaboratoryCenterAddress"] = DecryptFieldText(medicallabform.LaboratoryCenterAddress);
            ViewData["TestName"] = DecryptFieldText(medicallabform.TestName);
            ViewData["TestCost"] = DecryptFieldText(medicallabform.TestCost);
            ViewData["TestOtherInfo"] = DecryptFieldText(medicallabform.TestOtherInfo);

            // Therapy Assistance Fields
            ViewData["TherapyFacilityName"] = DecryptFieldText(medicallabform.TherapyFacilityName);
            ViewData["TherapyFacilityAddress"] = DecryptFieldText(medicallabform.TherapyFacilityAddress);
            ViewData["TherapyFacilityContact"] = DecryptFieldText(medicallabform.TherapyFacilityContact);
            ViewData["TherapyType"] = DecryptFieldText(medicallabform.TherapyType);

            // Equipment Assistance Fields
            ViewData["EquipmentName"] = DecryptFieldText(medicallabform.EquipmentName);
            ViewData["EquipmentBrand"] = DecryptFieldText(medicallabform.EquipmentBrand);
            ViewData["EquipmentCategory"] = DecryptFieldText(medicallabform.EquipmentCategory);
            ViewData["EquipmentQuantity"] = DecryptFieldText(medicallabform.EquipmentQuantity);
            ViewData["EquipmentCost"] = DecryptFieldText(medicallabform.EquipmentCost);

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");

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
                        debugMessages.Add("✓ Front ID decrypted");
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
                        debugMessages.Add("✓ Back ID decrypted");
                    }
                }

                // ✓ DOCTOR PRESCRIPTION - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription))
                {
                    string prescPath = Path.Combine(doctorPrescriptionFolder, FuneralAssistance.DoctorPrescription);
                    debugMessages.Add($"📄 Doctor Prescription filename: {FuneralAssistance.DoctorPrescription}");
                    debugMessages.Add($"📄 Full path: {prescPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(prescPath)}");

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

                            debugMessages.Add($"✓ Doctor Prescription decrypted - {decryptedPresc.Length} bytes");
                            debugMessages.Add($"📄 IsDoctorPrescriptionPdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Doctor Prescription decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Doctor Prescription file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Doctor Prescription in database");
                }

                // ✓ DEATH CERTIFICATE - UPDATED TO USE CONFIGURATION-BASED KEY
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string deathPath = Path.Combine(deathCertificateFolder, FuneralAssistance.DeathCertificate);
                    debugMessages.Add($"📄 Death Certificate filename: {FuneralAssistance.DeathCertificate}");
                    debugMessages.Add($"📄 Full path: {deathPath}");
                    debugMessages.Add($"📄 File exists: {System.IO.File.Exists(deathPath)}");

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

                            debugMessages.Add($"✓ Death Certificate decrypted - {decryptedDeath.Length} bytes");
                            debugMessages.Add($"📄 IsDeathCertificatePdf = {isPdf}");
                            debugMessages.Add($"📄 PDF Magic Number Detected: {(isPdf ? "YES" : "NO")}");
                        }
                        catch (Exception ex)
                        {
                            debugMessages.Add($"✗ Death Certificate decryption failed: {ex.Message}");
                        }
                    }
                    else
                    {
                        debugMessages.Add("✗ Death Certificate file NOT FOUND");
                    }
                }
                else
                {
                    debugMessages.Add("📄 No Death Certificate in database");
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"⚠️ GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            ViewData["Comments"] = FuneralAssistance.Comments;

            // ============================================
            // ADD DECRYPTION FOR THESE 10 FIELDS ONLY
            // ============================================

            // Funeral Assistance Fields
            ViewData["DeceasedPersonName"] = DecryptFieldText(FuneralAssistance.DeceasedPersonName);
            ViewData["RelationshipToDeceased"] = DecryptFieldText(FuneralAssistance.RelationshipToDeceased);
            ViewData["DateOfDeath"] = DecryptFieldText(FuneralAssistance.DateOfDeath);
            ViewData["TimeOfDeath"] = DecryptFieldText(FuneralAssistance.TimeOfDeath);
            ViewData["CauseOfDeath"] = DecryptFieldText(FuneralAssistance.CauseOfDeath);
            ViewData["FuneralHomeName"] = DecryptFieldText(FuneralAssistance.FuneralHomeName);
            ViewData["FuneralHomeAddress"] = DecryptFieldText(FuneralAssistance.FuneralHomeAddress);
            ViewData["BurialCremationDate"] = DecryptFieldText(FuneralAssistance.BurialCremationDate);
            ViewData["BurialCremationTime"] = DecryptFieldText(FuneralAssistance.BurialCremationTime);
            ViewData["BurialCremationType"] = DecryptFieldText(FuneralAssistance.BurialCremationType);

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

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

            // Additional Information - Encrypted Fields
            ViewData["HospitalFacilityName"] = HospitalAssistance.HospitalFacilityName;
            ViewData["HospitalFacilityAddress"] = HospitalAssistance.HospitalFacilityAddress;
            ViewData["DiagnosisMedicalCondition"] = HospitalAssistance.DiagnosisMedicalCondition;
            ViewData["HospitalBillCost"] = HospitalAssistance.HospitalBillCost;
            ViewData["AdmissionDate"] = HospitalAssistance.AdmissionDate;
            ViewData["DischargeDate"] = HospitalAssistance.DischargeDate;
            ViewData["WardRoomType"] = HospitalAssistance.WardRoomType;

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");

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

            // Additional Information - Encrypted Fields
            ViewData["DeceasedPersonName"] = FuneralAssistance.DeceasedPersonName;
            ViewData["RelationshipToDeceased"] = FuneralAssistance.RelationshipToDeceased;
            ViewData["DateOfDeath"] = FuneralAssistance.DateOfDeath;
            ViewData["TimeOfDeath"] = FuneralAssistance.TimeOfDeath;
            ViewData["CauseOfDeath"] = FuneralAssistance.CauseOfDeath;
            ViewData["FuneralHomeName"] = FuneralAssistance.FuneralHomeName;
            ViewData["FuneralHomeAddress"] = FuneralAssistance.FuneralHomeAddress;
            ViewData["BurialCremationDate"] = FuneralAssistance.BurialCremationDate;
            ViewData["BurialCremationTime"] = FuneralAssistance.BurialCremationTime;
            ViewData["BurialCremationType"] = FuneralAssistance.BurialCremationType;

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");

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

            // Additional Information - Encrypted Fields (All fields for conditional display)
            ViewData["MedicineName"] = medicallabform.MedicineName;
            ViewData["MedicineQuantity"] = medicallabform.MedicineQuantity;
            ViewData["MedicineCost"] = medicallabform.MedicineCost;
            ViewData["PrescribingDoctor"] = medicallabform.PrescribingDoctor;
            ViewData["DoctorContactDetail"] = medicallabform.DoctorContactDetail;
            ViewData["LaboratoryCenterName"] = medicallabform.LaboratoryCenterName;
            ViewData["LaboratoryCenterAddress"] = medicallabform.LaboratoryCenterAddress;
            ViewData["TestName"] = medicallabform.TestName;
            ViewData["TestCost"] = medicallabform.TestCost;
            ViewData["TestOtherInfo"] = medicallabform.TestOtherInfo;
            ViewData["TherapyFacilityName"] = medicallabform.TherapyFacilityName;
            ViewData["TherapyFacilityAddress"] = medicallabform.TherapyFacilityAddress;
            ViewData["TherapyFacilityContact"] = medicallabform.TherapyFacilityContact;
            ViewData["TherapyType"] = medicallabform.TherapyType;
            ViewData["EquipmentName"] = medicallabform.EquipmentName;
            ViewData["EquipmentBrand"] = medicallabform.EquipmentBrand;
            ViewData["EquipmentCategory"] = medicallabform.EquipmentCategory;
            ViewData["EquipmentQuantity"] = medicallabform.EquipmentQuantity;
            ViewData["EquipmentCost"] = medicallabform.EquipmentCost;

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

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

            // Additional Information - Encrypted Fields
            ViewData["HospitalFacilityName"] = HospitalAssistance.HospitalFacilityName;
            ViewData["HospitalFacilityAddress"] = HospitalAssistance.HospitalFacilityAddress;
            ViewData["DiagnosisMedicalCondition"] = HospitalAssistance.DiagnosisMedicalCondition;
            ViewData["HospitalBillCost"] = HospitalAssistance.HospitalBillCost;
            ViewData["AdmissionDate"] = HospitalAssistance.AdmissionDate;
            ViewData["DischargeDate"] = HospitalAssistance.DischargeDate;
            ViewData["WardRoomType"] = HospitalAssistance.WardRoomType;

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");

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

            // Additional Information - Encrypted Fields
            ViewData["DeceasedPersonName"] = FuneralAssistance.DeceasedPersonName;
            ViewData["RelationshipToDeceased"] = FuneralAssistance.RelationshipToDeceased;
            ViewData["DateOfDeath"] = FuneralAssistance.DateOfDeath;
            ViewData["TimeOfDeath"] = FuneralAssistance.TimeOfDeath;
            ViewData["CauseOfDeath"] = FuneralAssistance.CauseOfDeath;
            ViewData["FuneralHomeName"] = FuneralAssistance.FuneralHomeName;
            ViewData["FuneralHomeAddress"] = FuneralAssistance.FuneralHomeAddress;
            ViewData["BurialCremationDate"] = FuneralAssistance.BurialCremationDate;
            ViewData["BurialCremationTime"] = FuneralAssistance.BurialCremationTime;
            ViewData["BurialCremationType"] = FuneralAssistance.BurialCremationType;

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");

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

            // Additional Information - Encrypted Fields (All fields for conditional display)
            ViewData["MedicineName"] = medicallabform.MedicineName;
            ViewData["MedicineQuantity"] = medicallabform.MedicineQuantity;
            ViewData["MedicineCost"] = medicallabform.MedicineCost;
            ViewData["PrescribingDoctor"] = medicallabform.PrescribingDoctor;
            ViewData["DoctorContactDetail"] = medicallabform.DoctorContactDetail;
            ViewData["LaboratoryCenterName"] = medicallabform.LaboratoryCenterName;
            ViewData["LaboratoryCenterAddress"] = medicallabform.LaboratoryCenterAddress;
            ViewData["TestName"] = medicallabform.TestName;
            ViewData["TestCost"] = medicallabform.TestCost;
            ViewData["TestOtherInfo"] = medicallabform.TestOtherInfo;
            ViewData["TherapyFacilityName"] = medicallabform.TherapyFacilityName;
            ViewData["TherapyFacilityAddress"] = medicallabform.TherapyFacilityAddress;
            ViewData["TherapyFacilityContact"] = medicallabform.TherapyFacilityContact;
            ViewData["TherapyType"] = medicallabform.TherapyType;
            ViewData["EquipmentName"] = medicallabform.EquipmentName;
            ViewData["EquipmentBrand"] = medicallabform.EquipmentBrand;
            ViewData["EquipmentCategory"] = medicallabform.EquipmentCategory;
            ViewData["EquipmentQuantity"] = medicallabform.EquipmentQuantity;
            ViewData["EquipmentCost"] = medicallabform.EquipmentCost;

            return View();
        }




        // ====================================
        // RETAKE STATUSES - View-only pages for applications awaiting user resubmission
        // ====================================

        public IActionResult HospitalAssistanceRetakeStatus(int id)
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

            // Retake reason
            ViewData["RetakeReason"] = HospitalAssistance.RetakeReason;
            ViewData["Comments"] = HospitalAssistance.Comments;

            // Decryption for images
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

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
                    }
                }

                // Doctor Prescription / Supporting Document
                if (!string.IsNullOrEmpty(HospitalAssistance.DoctorPrescription))
                {
                    string docPath = Path.Combine(doctorPrescriptionFolder, HospitalAssistance.DoctorPrescription);
                    if (System.IO.File.Exists(docPath))
                    {
                        byte[] decryptedDoc = DecryptFile(docPath);
                        ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedDoc);
                        ViewData["DoctorPrescription"] = HospitalAssistance.DoctorPrescription;
                        ViewData["IsDoctorPrescriptionPdf"] = IsPdfFile(decryptedDoc);
                    }
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

            // Additional Information - Encrypted Fields
            ViewData["HospitalFacilityName"] = HospitalAssistance.HospitalFacilityName;
            ViewData["HospitalFacilityAddress"] = HospitalAssistance.HospitalFacilityAddress;
            ViewData["DiagnosisMedicalCondition"] = HospitalAssistance.DiagnosisMedicalCondition;
            ViewData["HospitalBillCost"] = HospitalAssistance.HospitalBillCost;
            ViewData["AdmissionDate"] = HospitalAssistance.AdmissionDate;
            ViewData["DischargeDate"] = HospitalAssistance.DischargeDate;
            ViewData["WardRoomType"] = HospitalAssistance.WardRoomType;

            // Create DTO for the view
            var dto = new HospitalAssistanceDto
            {
                Lastname = HospitalAssistance.Lastname,
                Firstname = HospitalAssistance.Firstname,
                Middlename = HospitalAssistance.Middlename,
                Suffix = HospitalAssistance.Suffix,
                BlkLotStreet = HospitalAssistance.BlkLotStreet,
                SubVill = HospitalAssistance.SubVill,
                Brgy = HospitalAssistance.Brgy,
                District = HospitalAssistance.District,
                Sex = HospitalAssistance.Sex,
                PhilHealth = HospitalAssistance.PhilHealth,
                PhilHealthNo = HospitalAssistance.PhilHealthNo,
                Dateofbirth = HospitalAssistance.Dateofbirth,
                Age = HospitalAssistance.Age,
                RLastname = HospitalAssistance.RLastname,
                RFirstname = HospitalAssistance.RFirstname,
                RMiddlename = HospitalAssistance.RMiddlename,
                RSuffix = HospitalAssistance.RSuffix,
                RBlkLotStreet = HospitalAssistance.RBlkLotStreet,
                RSubVill = HospitalAssistance.RSubVill,
                RBrgy = HospitalAssistance.RBrgy,
                RDistrict = HospitalAssistance.RDistrict,
                RelationshipPatient = HospitalAssistance.RelationshipPatient,
                ContactNo = HospitalAssistance.ContactNo,
                Typeassistance = HospitalAssistance.Typeassistance,
                ForCMOPERSONNEL = HospitalAssistance.ForCMOPERSONNEL,
                Comments = HospitalAssistance.RetakeReason ?? HospitalAssistance.Comments,
                Status2 = HospitalAssistance.Status2
            };

            return View(dto);
        }

        public IActionResult OtherAssistanceRetakeStatus(int id)
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

            // Basic ViewData setup
            ViewData["Status2"] = OtherAssistance.Status2;
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

            // Type of assistance
            var typeAssistanceRaw = OtherAssistance.Typeassistance ?? "";
            ViewData["Typeassistance"] = typeAssistanceRaw;
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            var cmoPersonnelRaw = OtherAssistance.ForCMOPERSONNEL ?? "";
            ViewData["ForCMOPERSONNEL"] = cmoPersonnelRaw;
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // Retake reason
            ViewData["RetakeReason"] = OtherAssistance.RetakeReason;
            ViewData["Comments"] = OtherAssistance.Comments;

            // Decryption for images
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string otherAssistanceFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");

            var debugMessages = new List<string>();

            try
            {
                // Front ID
                if (!string.IsNullOrEmpty(OtherAssistance.Validfrontimage))
                {
                    string frontPath = Path.Combine(validFolder, OtherAssistance.Validfrontimage);
                    if (System.IO.File.Exists(frontPath))
                    {
                        byte[] decryptedFront = DecryptFile(frontPath);
                        ViewData["ValidfrontimageBase64"] = Convert.ToBase64String(decryptedFront);
                    }
                }

                // Back ID
                if (!string.IsNullOrEmpty(OtherAssistance.ValidBackimage))
                {
                    string backPath = Path.Combine(validFolder, OtherAssistance.ValidBackimage);
                    if (System.IO.File.Exists(backPath))
                    {
                        byte[] decryptedBack = DecryptFile(backPath);
                        ViewData["ValidBackimageBase64"] = Convert.ToBase64String(decryptedBack);
                    }
                }

                // Supporting Document
                if (!string.IsNullOrEmpty(OtherAssistance.DoctorPrescription))
                {
                    string docPath = Path.Combine(otherAssistanceFolder, OtherAssistance.DoctorPrescription);
                    if (System.IO.File.Exists(docPath))
                    {
                        byte[] decryptedDoc = DecryptFile(docPath);
                        ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decryptedDoc);
                        ViewData["DoctorPrescription"] = OtherAssistance.DoctorPrescription;
                        ViewData["IsDoctorPrescriptionPdf"] = IsPdfFile(decryptedDoc);
                    }
                }
            }
            catch (Exception ex)
            {
                debugMessages.Add($"? GENERAL ERROR: {ex.Message}");
                ViewData["DecryptionError"] = "Unable to decrypt files: " + ex.Message;
            }

            ViewData["DebugMessages"] = debugMessages;
            ViewData["Validfrontimage"] = OtherAssistance.Validfrontimage;
            ViewData["ValidBackimage"] = OtherAssistance.ValidBackimage;

            // Additional Information - Encrypted Fields (All fields for conditional display)
            ViewData["MedicineName"] = OtherAssistance.MedicineName;
            ViewData["MedicineQuantity"] = OtherAssistance.MedicineQuantity;
            ViewData["MedicineCost"] = OtherAssistance.MedicineCost;
            ViewData["PrescribingDoctor"] = OtherAssistance.PrescribingDoctor;
            ViewData["DoctorContactDetail"] = OtherAssistance.DoctorContactDetail;
            ViewData["LaboratoryCenterName"] = OtherAssistance.LaboratoryCenterName;
            ViewData["LaboratoryCenterAddress"] = OtherAssistance.LaboratoryCenterAddress;
            ViewData["TestName"] = OtherAssistance.TestName;
            ViewData["TestCost"] = OtherAssistance.TestCost;
            ViewData["TestOtherInfo"] = OtherAssistance.TestOtherInfo;
            ViewData["TherapyFacilityName"] = OtherAssistance.TherapyFacilityName;
            ViewData["TherapyFacilityAddress"] = OtherAssistance.TherapyFacilityAddress;
            ViewData["TherapyFacilityContact"] = OtherAssistance.TherapyFacilityContact;
            ViewData["TherapyType"] = OtherAssistance.TherapyType;
            ViewData["EquipmentName"] = OtherAssistance.EquipmentName;
            ViewData["EquipmentBrand"] = OtherAssistance.EquipmentBrand;
            ViewData["EquipmentCategory"] = OtherAssistance.EquipmentCategory;
            ViewData["EquipmentQuantity"] = OtherAssistance.EquipmentQuantity;
            ViewData["EquipmentCost"] = OtherAssistance.EquipmentCost;

            // Create DTO for the view
            var dto = new OtherAssistanceDto
            {
                Lastname = OtherAssistance.Lastname,
                Firstname = OtherAssistance.Firstname,
                Middlename = OtherAssistance.Middlename,
                Suffix = OtherAssistance.Suffix,
                BlkLotStreet = OtherAssistance.BlkLotStreet,
                SubVill = OtherAssistance.SubVill,
                Brgy = OtherAssistance.Brgy,
                District = OtherAssistance.District,
                Sex = OtherAssistance.Sex,
                PhilHealth = OtherAssistance.PhilHealth,
                PhilHealthNo = OtherAssistance.PhilHealthNo,
                Dateofbirth = OtherAssistance.Dateofbirth,
                Age = OtherAssistance.Age,
                RLastname = OtherAssistance.RLastname,
                RFirstname = OtherAssistance.RFirstname,
                RMiddlename = OtherAssistance.RMiddlename,
                RSuffix = OtherAssistance.RSuffix,
                RBlkLotStreet = OtherAssistance.RBlkLotStreet,
                RSubVill = OtherAssistance.RSubVill,
                RBrgy = OtherAssistance.RBrgy,
                RDistrict = OtherAssistance.RDistrict,
                RelationshipPatient = OtherAssistance.RelationshipPatient,
                ContactNo = OtherAssistance.ContactNo,
                Typeassistance = OtherAssistance.Typeassistance,
                ForCMOPERSONNEL = OtherAssistance.ForCMOPERSONNEL,
                Comments = OtherAssistance.RetakeReason ?? OtherAssistance.Comments,
                Status2 = OtherAssistance.Status2
            };

            return View(dto);
        }

        public IActionResult FuneralAssistanceRetakeStatus(int id)
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

            // Retake reason
            ViewData["RetakeReason"] = FuneralAssistance.RetakeReason;
            ViewData["Comments"] = FuneralAssistance.Comments;

            // Decryption for images
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string funeralAssistanceFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");

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
                    }
                }

                // Death Certificate / Supporting Document
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate))
                {
                    string docPath = Path.Combine(funeralAssistanceFolder, FuneralAssistance.DeathCertificate);
                    if (System.IO.File.Exists(docPath))
                    {
                        byte[] decryptedDoc = DecryptFile(docPath);
                        ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decryptedDoc);
                        ViewData["DeathCertificate"] = FuneralAssistance.DeathCertificate;
                        ViewData["IsDeathCertificatePdf"] = IsPdfFile(decryptedDoc);
                    }
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

            // Additional Information - Encrypted Fields
            ViewData["DeceasedPersonName"] = FuneralAssistance.DeceasedPersonName;
            ViewData["RelationshipToDeceased"] = FuneralAssistance.RelationshipToDeceased;
            ViewData["DateOfDeath"] = FuneralAssistance.DateOfDeath;
            ViewData["TimeOfDeath"] = FuneralAssistance.TimeOfDeath;
            ViewData["CauseOfDeath"] = FuneralAssistance.CauseOfDeath;
            ViewData["FuneralHomeName"] = FuneralAssistance.FuneralHomeName;
            ViewData["FuneralHomeAddress"] = FuneralAssistance.FuneralHomeAddress;
            ViewData["BurialCremationDate"] = FuneralAssistance.BurialCremationDate;
            ViewData["BurialCremationTime"] = FuneralAssistance.BurialCremationTime;
            ViewData["BurialCremationType"] = FuneralAssistance.BurialCremationType;

            // Create DTO for the view
            var dto = new FuneralAssistanceDto
            {
                Lastname = FuneralAssistance.Lastname,
                Firstname = FuneralAssistance.Firstname,
                Middlename = FuneralAssistance.Middlename,
                Suffix = FuneralAssistance.Suffix,
                BlkLotStreet = FuneralAssistance.BlkLotStreet,
                SubVill = FuneralAssistance.SubVill,
                Brgy = FuneralAssistance.Brgy,
                District = FuneralAssistance.District,
                Sex = FuneralAssistance.Sex,
                PhilHealth = FuneralAssistance.PhilHealth,
                PhilHealthNo = FuneralAssistance.PhilHealthNo,
                Dateofbirth = FuneralAssistance.Dateofbirth,
                Age = FuneralAssistance.Age,
                RLastname = FuneralAssistance.RLastname,
                RFirstname = FuneralAssistance.RFirstname,
                RMiddlename = FuneralAssistance.RMiddlename,
                RSuffix = FuneralAssistance.RSuffix,
                RBlkLotStreet = FuneralAssistance.RBlkLotStreet,
                RSubVill = FuneralAssistance.RSubVill,
                RBrgy = FuneralAssistance.RBrgy,
                RDistrict = FuneralAssistance.RDistrict,
                RelationshipPatient = FuneralAssistance.RelationshipPatient,
                ContactNo = FuneralAssistance.ContactNo,
                Typeassistance = FuneralAssistance.Typeassistance,
                ForCMOPERSONNEL = FuneralAssistance.ForCMOPERSONNEL,
                Comments = FuneralAssistance.RetakeReason ?? FuneralAssistance.Comments,
                Status2 = FuneralAssistance.Status2
            };

            return View(dto);
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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

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

            // Additional Information - Encrypted Fields
            ViewData["HospitalFacilityName"] = HospitalAssistance.HospitalFacilityName;
            ViewData["HospitalFacilityAddress"] = HospitalAssistance.HospitalFacilityAddress;
            ViewData["DiagnosisMedicalCondition"] = HospitalAssistance.DiagnosisMedicalCondition;
            ViewData["HospitalBillCost"] = HospitalAssistance.HospitalBillCost;
            ViewData["AdmissionDate"] = HospitalAssistance.AdmissionDate;
            ViewData["DischargeDate"] = HospitalAssistance.DischargeDate;
            ViewData["WardRoomType"] = HospitalAssistance.WardRoomType;

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            string medicalCertificateFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");

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

            // Additional Information - Encrypted Fields (All fields for conditional display)
            ViewData["MedicineName"] = medicallabform.MedicineName;
            ViewData["MedicineQuantity"] = medicallabform.MedicineQuantity;
            ViewData["MedicineCost"] = medicallabform.MedicineCost;
            ViewData["PrescribingDoctor"] = medicallabform.PrescribingDoctor;
            ViewData["DoctorContactDetail"] = medicallabform.DoctorContactDetail;
            ViewData["LaboratoryCenterName"] = medicallabform.LaboratoryCenterName;
            ViewData["LaboratoryCenterAddress"] = medicallabform.LaboratoryCenterAddress;
            ViewData["TestName"] = medicallabform.TestName;
            ViewData["TestCost"] = medicallabform.TestCost;
            ViewData["TestOtherInfo"] = medicallabform.TestOtherInfo;
            ViewData["TherapyFacilityName"] = medicallabform.TherapyFacilityName;
            ViewData["TherapyFacilityAddress"] = medicallabform.TherapyFacilityAddress;
            ViewData["TherapyFacilityContact"] = medicallabform.TherapyFacilityContact;
            ViewData["TherapyType"] = medicallabform.TherapyType;
            ViewData["EquipmentName"] = medicallabform.EquipmentName;
            ViewData["EquipmentBrand"] = medicallabform.EquipmentBrand;
            ViewData["EquipmentCategory"] = medicallabform.EquipmentCategory;
            ViewData["EquipmentQuantity"] = medicallabform.EquipmentQuantity;
            ViewData["EquipmentCost"] = medicallabform.EquipmentCost;

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
            string doctorPrescriptionFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
            string deathCertificateFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");

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

            // Additional Information - Encrypted Fields
            ViewData["DeceasedPersonName"] = FuneralAssistance.DeceasedPersonName;
            ViewData["RelationshipToDeceased"] = FuneralAssistance.RelationshipToDeceased;
            ViewData["DateOfDeath"] = FuneralAssistance.DateOfDeath;
            ViewData["TimeOfDeath"] = FuneralAssistance.TimeOfDeath;
            ViewData["CauseOfDeath"] = FuneralAssistance.CauseOfDeath;
            ViewData["FuneralHomeName"] = FuneralAssistance.FuneralHomeName;
            ViewData["FuneralHomeAddress"] = FuneralAssistance.FuneralHomeAddress;
            ViewData["BurialCremationDate"] = FuneralAssistance.BurialCremationDate;
            ViewData["BurialCremationTime"] = FuneralAssistance.BurialCremationTime;
            ViewData["BurialCremationType"] = FuneralAssistance.BurialCremationType;

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

                // Define possible directories to search based on file type
                List<string> possibleFolders = new List<string>();

                // NEW: Search in form-specific folders first
                if (fileType.ToLower() == "doctorprescription" || fileType.ToLower() == "deathcertificate")
                {
                    // These file types could be in Hospital, Funeral, or Other assistance
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage"));
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage"));
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage"));
                }
                else if (fileType.ToLower() == "medicalcertificate")
                {
                    // Medical certificates are only in Other assistance
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage"));
                }
                else
                {
                    // Default for valid ID images
                    possibleFolders.Add(Path.Combine(environment.WebRootPath, "Validimg"));
                }

                Console.WriteLine($"?? Searching in {possibleFolders.Count} possible folders");

                // Search for the file in possible folders
                string? encryptedFilePath = null;
                string? folderPath = null;

                foreach (var folder in possibleFolders)
                {
                    string testPath = Path.Combine(folder, safeFileName);
                    if (System.IO.File.Exists(testPath))
                    {
                        encryptedFilePath = testPath;
                        folderPath = folder;
                        Console.WriteLine($"? File found in: {folderPath}");
                        break;
                    }
                }

                // Check if file exists
                if (string.IsNullOrEmpty(encryptedFilePath) || !System.IO.File.Exists(encryptedFilePath))
                {
                    Console.WriteLine($"? File does not exist in any expected folder: {safeFileName}");
                    return NotFound($"File not found: {fileName}");
                }

                // Additional security: Verify the resolved path is within the expected directory
                string resolvedPath = Path.GetFullPath(encryptedFilePath);
                string resolvedFolder = Path.GetFullPath(folderPath);
                if (!resolvedPath.StartsWith(resolvedFolder))
                {
                    Console.WriteLine("? Security: Path traversal attempt detected");
                    return BadRequest("Invalid file path");
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HospitalAssistanceProcessingStatus(int id, [FromForm] HospitalAssistanceDto HospitalAssistanceDto)
        {
            try
            {
                var HospitalAssistance = context.HospitalAssistance.Find(id);

                if (HospitalAssistance == null)
                {
                    TempData["ErrorMessage"] = "Hospital bill record not found.";
                    return Redirect("/Admin");
                }

                // Validate required fields
                if (string.IsNullOrEmpty(HospitalAssistanceDto.Status2))
                {
                    TempData["ErrorMessage"] = "Status is required.";
                    return RedirectToAction("HospitalAssistanceProcessingStatus", new { id = id });
                }

                // Validate CMO Details Section ONLY for Approve status
                var status = HospitalAssistanceDto.Status2?.Trim();
                if (!string.IsNullOrEmpty(status) && status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
                {
                    var cmoReflection = HospitalAssistanceDto.ForCMOPERSONNEL ?? "";

                    // Check if at least one supporting document is selected (should be in the CMO reflection)
                    if (string.IsNullOrWhiteSpace(cmoReflection) || !cmoReflection.Contains("Docs:"))
                    {
                        TempData["ErrorMessage"] = "Please select at least one supporting document in the CMO Details section.";
                        return RedirectToAction("HospitalAssistanceProcessingStatus", new { id = id });
                    }


                    // Extract and validate GrantedAmount
                    if (!cmoReflection.Contains("GrantedAmount:") || string.IsNullOrWhiteSpace(cmoReflection.Split("GrantedAmount:").Skip(1).FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()))
                    {
                        TempData["ErrorMessage"] = "Please enter the Amount Granted in the CMO Details section.";
                        return RedirectToAction("HospitalAssistanceProcessingStatus", new { id = id });
                    }

                    // Validate that granted amount is greater than 0
                    var grantedAmountStr = cmoReflection.Split("GrantedAmount:").Skip(1).FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                    if (!decimal.TryParse(grantedAmountStr, out decimal grantedAmount) || grantedAmount <= 0)
                    {
                        TempData["ErrorMessage"] = "Amount Granted must be greater than 0 in the CMO Details section.";
                        return RedirectToAction("HospitalAssistanceProcessingStatus", new { id = id });
                    }
                }

                // Update record
                HospitalAssistance.Status2 = HospitalAssistanceDto.Status2;
                HospitalAssistance.ForCMOPERSONNEL = HospitalAssistanceDto.ForCMOPERSONNEL;
                HospitalAssistance.Comments = HospitalAssistanceDto.Comments;
                HospitalAssistance.Processby = HospitalAssistanceDto.Processby;
                HospitalAssistance.Result = _dateTimeService.Now;

                // Handle Retake status - keep Status as is, mark as retake, DON'T move to Pending
                if (!string.IsNullOrEmpty(status) && status.Equals("Retake", StringComparison.OrdinalIgnoreCase))
                {
                    // DON'T change Status - keep it as "Processing" so it stays visible in admin processing
                    // Status2 = "Retake" is what identifies it as a retake application
                    HospitalAssistance.IsRetakeApplication = true;
                    HospitalAssistance.RetakeRequestedAt = _dateTimeService.Now;

                    // Extract retake reason from Comments (format: "RETAKE REQUEST: reason")
                    if (!string.IsNullOrEmpty(HospitalAssistanceDto.Comments) && HospitalAssistanceDto.Comments.StartsWith("RETAKE REQUEST: "))
                    {
                        HospitalAssistance.RetakeReason = HospitalAssistanceDto.Comments.Substring("RETAKE REQUEST: ".Length);
                    }
                    else
                    {
                        HospitalAssistance.RetakeReason = HospitalAssistanceDto.Comments;
                    }
                }

                await context.SaveChangesAsync();

                // Send multi-channel notification
                if (!string.IsNullOrEmpty(status) && (status.Equals("Approve", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Retake", StringComparison.OrdinalIgnoreCase)))
                {
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == HospitalAssistance.UserId);
                    var applicantName = verifyAccount?.Firstname ?? "Applicant";

                    // Send notification (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.SendStatusChangeNotificationAsync(
                                HospitalAssistance.UserId,
                                applicantName,
                                "HospitalAssistance",
                                status,
                                HospitalAssistance.Id,
                                status.Equals("Retake", StringComparison.OrdinalIgnoreCase) ? HospitalAssistance.RetakeReason : HospitalAssistanceDto.Comments,
                                HospitalAssistanceDto.Processby
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Notification error: {ex.Message}");
                        }
                    });

                    // Send email synchronously (wait for it to complete)
                    try
                    {
                        await SendHospitalStatusEmailWithRetry(HospitalAssistance, HospitalAssistanceDto, status, applicantName);
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Email sending failed after retries: {emailEx.Message}");
                        // Log but don't fail the main operation
                    }
                }

                TempData["SuccessMessage"] = $"Hospital bill status updated to '{HospitalAssistanceDto.Status2}' successfully.";
                
                // Redirect with appropriate status parameter based on the action
                string redirectStatus = status?.ToLower() switch
                {
                    "approve" => "approve",
                    "disapprove" => "disapprove",
                    "retake" => "retakes",
                    _ => "processing"
                };
                return Redirect($"/Admin?status={redirectStatus}");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating status: " + ex.Message;
                return Redirect("/Admin");
            }
        }

        // New method with retry logic for Hospital Assistance
        private async Task SendHospitalStatusEmailWithRetry(HospitalAssistance assistance, HospitalAssistanceDto dto, string status, string firstName, int maxRetries = 3)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    await SendHospitalStatusEmail(assistance, dto, status, firstName);
                    Console.WriteLine($"Hospital email sent successfully on attempt {retryCount + 1}");
                    return; // Success - exit method
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    Console.WriteLine($"Hospital email attempt {retryCount} failed: {ex.Message}");

                    if (retryCount < maxRetries)
                    {
                        // Wait before retry (exponential backoff)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                    }
                }
            }

            // If we get here, all retries failed
            throw new Exception($"Failed to send hospital email after {maxRetries} attempts", lastException);
        }

        private async Task SendHospitalStatusEmail(HospitalAssistance assistance, HospitalAssistanceDto dto, string status, string firstName)
        {
            try
            {
                // Get user with explicit null checking
                var user = await context.RegisterAcc.FindAsync(assistance.UserId);

                if (user == null)
                {
                    Console.WriteLine($"User not found for UserId: {assistance.UserId}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    Console.WriteLine($"Email not found for UserId: {assistance.UserId}");
                    return;
                }

                // Validate email configuration
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];

                if (string.IsNullOrWhiteSpace(fromEmail) ||
                    string.IsNullOrWhiteSpace(fromName) ||
                    string.IsNullOrWhiteSpace(fromPassword))
                {
                    throw new Exception("Email configuration is missing or incomplete.");
                }

                var fromAddress = new MailAddress(fromEmail, fromName);
                var toAddress = new MailAddress(user.Email, firstName);

                string subject = GetHospitalEmailSubject(status);
                string body = GetHospitalEmailBody(status, firstName, assistance, dto);

                // Configure SMTP with better settings
                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                    smtp.Timeout = 30000; // 30 seconds timeout

                    using (var message = new MailMessage(fromAddress, toAddress))
                    {
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;
                        message.Priority = MailPriority.High;

                        await smtp.SendMailAsync(message);
                    }
                }

                Console.WriteLine($"Hospital email sent successfully to {user.Email} for application {assistance.Id} - Status: {status}");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP Error: {smtpEx.Message}");
                Console.WriteLine($"Status Code: {smtpEx.StatusCode}");
                throw; // Re-throw to trigger retry
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hospital email sending error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to trigger retry
            }
        }

        private string GetHospitalEmailSubject(string status)
        {
            return status.ToLower() switch
            {
                "approve" => "🎉 Good News! Your Hospital Bill Assistance is Approved - LingapDVO",
                "disapprove" => "📋 Update on Your Hospital Bill Assistance Application - LingapDVO",
                "retake" => "🔄 Action Required - Please Resubmit Your Hospital Bill Assistance Documents - LingapDVO",
                _ => "📋 Hospital Bill Assistance Application Update - LingapDVO"
            };
        }

        private string GetHospitalEmailBody(string status, string firstName, HospitalAssistance assistance, HospitalAssistanceDto dto)
        {
            if (status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                return GetHospitalApprovalEmailBody(firstName, assistance, dto);
            }
            else if (status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase))
            {
                return GetHospitalDisapprovalEmailBody(firstName, assistance, dto);
            }
            else if (status.Equals("Retake", StringComparison.OrdinalIgnoreCase))
            {
                return GetHospitalRetakeEmailBody(firstName, assistance, dto);
            }

            return ""; // Default empty body
        }

        private string GetHospitalApprovalEmailBody(string firstName, HospitalAssistance assistance, HospitalAssistanceDto dto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #28a745, #20c997); color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #28a745; margin: 20px 0; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .details-box h3 {{ margin-top: 0; color: #28a745; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .next-steps {{ background-color: #e8f5e8; padding: 20px; border-radius: 4px; margin: 20px 0; }}
        .next-steps h3 {{ color: #28a745; margin-top: 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
        .celebrate {{ text-align: center; font-size: 48px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Application Approved!</h1>
        </div>
        <div class='content'>
            <div class='celebrate'>✅</div>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                We are delighted to inform you that your Hospital Bill Assistance application has been <strong>APPROVED</strong>!
            </p>

            <div class='details-box'>
                <h3>📋 APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Hospital Bill Assistance</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Application ID:</span>
                    <span class='detail-value'>{assistance.Id}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value' style='color: #28a745; font-weight: bold;'>APPROVED</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Approved:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{dto.Processby}</span>
                </div>
            </div>

            <div class='details-box'>
                <h3>💬 REMARKS</h3>
                <p>{dto.Comments ?? "Your application has been processed successfully. No additional remarks provided."}</p>
            </div>

            <div class='next-steps'>
                <h3>📝 NEXT STEPS</h3>
                <p>Please visit our office to complete the necessary documentation and receive your assistance. Bring valid ID and any other required documents.</p>
            </div>

            <p class='message'>
                Thank you for using LingapDVO. We are here to help with your healthcare needs.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Hospital Bill Assistance Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetHospitalDisapprovalEmailBody(string firstName, HospitalAssistance assistance, HospitalAssistanceDto dto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #dc3545, #e5533f); color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #dc3545; margin: 20px 0; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .details-box h3 {{ margin-top: 0; color: #dc3545; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .contact-info {{ background-color: #f8f9fa; padding: 20px; border-radius: 4px; margin: 20px 0; border: 1px solid #dee2e6; }}
        .contact-info h3 {{ color: #495057; margin-top: 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
        .status-icon {{ text-align: center; font-size: 48px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📋 Application Update</h1>
        </div>
        <div class='content'>
            <div class='status-icon'>❌</div>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                After careful review, we regret to inform you that your Hospital Bill Assistance application has been <strong>DISAPPROVED</strong>.
            </p>

            <div class='details-box'>
                <h3>📋 APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Hospital Bill Assistance</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Application ID:</span>
                    <span class='detail-value'>{assistance.Id}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value' style='color: #dc3545; font-weight: bold;'>DISAPPROVED</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Updated:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{dto.Processby}</span>
                </div>
            </div>

            <div class='details-box'>
                <h3>💬 REMARKS</h3>
                <p>{dto.Comments ?? "Please contact our office for more information about this decision."}</p>
            </div>

            <div class='contact-info'>
                <h3>🤝 NEED MORE INFORMATION?</h3>
                <p>If you have questions or would like to discuss this decision further, please visit our office during office hours. Our staff will be happy to assist you.</p>
            </div>

            <p class='message'>
                We appreciate your understanding and thank you for considering LingapDVO for your healthcare needs.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Hospital Bill Assistance Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetHospitalRetakeEmailBody(string firstName, HospitalAssistance assistance, HospitalAssistanceDto dto)
        {
            var retakeReason = assistance.RetakeReason ?? dto.Comments ?? "Please review and resubmit your documents.";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #ff9800, #ff5722); color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #ff9800; margin: 20px 0; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .details-box h3 {{ margin-top: 0; color: #ff9800; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .reason-box {{ margin-top: 25px; padding: 20px; background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 4px; }}
        .reason-box h3 {{ color: #856404; margin-top: 0; font-size: 16px; }}
        .reason-box p {{ color: #856404; margin: 0; }}
        .steps-box {{ margin-top: 25px; padding: 20px; background-color: #e7f3ff; border-left: 4px solid #0066cc; border-radius: 4px; }}
        .steps-box h3 {{ color: #0066cc; margin-top: 0; font-size: 16px; }}
        .steps-box ol {{ margin: 10px 0; padding-left: 20px; }}
        .steps-box li {{ margin: 8px 0; color: #555; }}
        .button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #ff9800, #ff5722); color: white; text-decoration: none; border-radius: 5px; font-weight: 600; margin-top: 15px; text-align: center; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
        .action-icon {{ text-align: center; font-size: 48px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔄 Action Required - Resubmit Documents</h1>
        </div>
        <div class='content'>
            <div class='action-icon'>📝</div>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                We have reviewed your Hospital Bill Assistance application and need you to resubmit some documents.
                Your application details will remain the same - you only need to upload the corrected documents.
            </p>

            <div class='details-box'>
                <h3>📋 APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Hospital Bill Assistance</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Application ID:</span>
                    <span class='detail-value'>{assistance.Id}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value' style='color: #ff9800; font-weight: bold;'>RESUBMIT REQUIRED</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Updated:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{dto.Processby}</span>
                </div>
            </div>

            <div class='reason-box'>
                <h3>📋 DOCUMENTS NEEDING CORRECTION</h3>
                <p>{retakeReason}</p>
            </div>

            <div class='steps-box'>
                <h3>📝 HOW TO RESUBMIT YOUR DOCUMENTS</h3>
                <ol>
                    <li>Log in to your LingapDVO account</li>
                    <li>Go to Application Tracking section</li>
                    <li>Find this application and click on it</li>
                    <li>Upload the required corrected documents</li>
                    <li>Submit the updated application</li>
                </ol>
                <a href='https://lingap.online/Applicationtracking' class='button'>Go to Application Tracking</a>
            </div>

            <p class='message' style='margin-top: 20px;'>
                If you have any questions or need assistance, please visit our office during office hours or contact our support team.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Hospital Bill Assistance Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OtherAssistanceProcessingStatus(int id, [FromForm] OtherAssistanceDto OtherAssistanceDto)
        {
            try
            {
                var medicallabform = context.OtherAssistance.Find(id);

                if (medicallabform == null)
                {
                    TempData["ErrorMessage"] = "Medical assistance record not found.";
                    return Redirect("/Admin");
                }

                // Validate required fields
                if (string.IsNullOrEmpty(OtherAssistanceDto.Status2))
                {
                    TempData["ErrorMessage"] = "Status is required.";
                    return RedirectToAction("OtherAssistanceProcessingStatus", new { id = id });
                }

                // Validate CMO Details Section ONLY for Approve status
                var status = OtherAssistanceDto.Status2?.Trim();
                if (!string.IsNullOrEmpty(status) && status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
                {
                    var cmoReflection = OtherAssistanceDto.ForCMOPERSONNEL ?? "";

                    // Check if at least one supporting document is selected (should be in the CMO reflection)
                    if (string.IsNullOrWhiteSpace(cmoReflection) || !cmoReflection.Contains("Docs:"))
                    {
                        TempData["ErrorMessage"] = "Please select at least one supporting document in the CMO Details section.";
                        return RedirectToAction("OtherAssistanceProcessingStatus", new { id = id });
                    }

                    // Extract and validate GrantedAmount
                    if (!cmoReflection.Contains("GrantedAmount:") || string.IsNullOrWhiteSpace(cmoReflection.Split("GrantedAmount:").Skip(1).FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()))
                    {
                        TempData["ErrorMessage"] = "Please enter the Amount Granted in the CMO Details section.";
                        return RedirectToAction("OtherAssistanceProcessingStatus", new { id = id });
                    }

                    // Validate that granted amount is greater than 0
                    var grantedAmountStr = cmoReflection.Split("GrantedAmount:").Skip(1).FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                    if (!decimal.TryParse(grantedAmountStr, out decimal grantedAmount) || grantedAmount <= 0)
                    {
                        TempData["ErrorMessage"] = "Amount Granted must be greater than 0 in the CMO Details section.";
                        return RedirectToAction("OtherAssistanceProcessingStatus", new { id = id });
                    }
                }

                // Update record
                medicallabform.Status2 = OtherAssistanceDto.Status2;
                medicallabform.ForCMOPERSONNEL = OtherAssistanceDto.ForCMOPERSONNEL;
                medicallabform.Comments = OtherAssistanceDto.Comments;
                medicallabform.Processby = OtherAssistanceDto.Processby;
                medicallabform.Result = _dateTimeService.Now;

                // Handle Retake status - keep Status as is, mark as retake, DON'T move to Pending
                if (!string.IsNullOrEmpty(status) && status.Equals("Retake", StringComparison.OrdinalIgnoreCase))
                {
                    // DON'T change Status - keep it as "Processing" so it stays visible in admin processing
                    // Status2 = "Retake" is what identifies it as a retake application
                    medicallabform.IsRetakeApplication = true;
                    medicallabform.RetakeRequestedAt = _dateTimeService.Now;

                    // Extract retake reason from Comments (format: "RETAKE REQUEST: reason")
                    if (!string.IsNullOrEmpty(OtherAssistanceDto.Comments) && OtherAssistanceDto.Comments.StartsWith("RETAKE REQUEST: "))
                    {
                        medicallabform.RetakeReason = OtherAssistanceDto.Comments.Substring("RETAKE REQUEST: ".Length);
                    }
                    else
                    {
                        medicallabform.RetakeReason = OtherAssistanceDto.Comments;
                    }
                }

                await context.SaveChangesAsync();

                // Send multi-channel notification
                if (!string.IsNullOrEmpty(status) && (status.Equals("Approve", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Retake", StringComparison.OrdinalIgnoreCase)))
                {
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == medicallabform.UserId);
                    var applicantName = verifyAccount?.Firstname ?? "Applicant";

                    // Send notification (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.SendStatusChangeNotificationAsync(
                                medicallabform.UserId,
                                applicantName,
                                "OtherAssistance",
                                status,
                                medicallabform.Id,
                                status.Equals("Retake", StringComparison.OrdinalIgnoreCase) ? medicallabform.RetakeReason : OtherAssistanceDto.Comments,
                                OtherAssistanceDto.Processby
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Notification error: {ex.Message}");
                        }
                    });

                    // Send email synchronously (wait for it to complete)
                    try
                    {
                        await SendStatusEmailWithRetry(medicallabform, OtherAssistanceDto, status, applicantName);
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Email sending failed after retries: {emailEx.Message}");
                        // Log but don't fail the main operation
                    }
                }

                TempData["SuccessMessage"] = $"Medical assistance status updated to '{OtherAssistanceDto.Status2}' successfully.";
                
                // Redirect with appropriate status parameter based on the action
                string redirectStatus = status?.ToLower() switch
                {
                    "approve" => "approve",
                    "disapprove" => "disapprove",
                    "retake" => "retakes",
                    _ => "processing"
                };
                return Redirect($"/Admin?status={redirectStatus}");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating status: " + ex.Message;
                return Redirect("/Admin");
            }
        }

        // New method with retry logic
        private async Task SendStatusEmailWithRetry(OtherAssistance assistance, OtherAssistanceDto dto, string status, string firstName, int maxRetries = 3)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    await SendStatusEmail(assistance, dto, status, firstName);
                    Console.WriteLine($"Email sent successfully on attempt {retryCount + 1}");
                    return; // Success - exit method
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    Console.WriteLine($"Email attempt {retryCount} failed: {ex.Message}");

                    if (retryCount < maxRetries)
                    {
                        // Wait before retry (exponential backoff)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                    }
                }
            }

            // If we get here, all retries failed
            throw new Exception($"Failed to send email after {maxRetries} attempts", lastException);
        }

        private async Task SendStatusEmail(OtherAssistance assistance, OtherAssistanceDto dto, string status, string firstName)
        {
            try
            {
                // Get user with explicit null checking
                var user = await context.RegisterAcc.FindAsync(assistance.UserId);

                if (user == null)
                {
                    Console.WriteLine($"User not found for UserId: {assistance.UserId}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    Console.WriteLine($"Email not found for UserId: {assistance.UserId}");
                    return;
                }

                // Validate email configuration
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];

                if (string.IsNullOrWhiteSpace(fromEmail) ||
                    string.IsNullOrWhiteSpace(fromName) ||
                    string.IsNullOrWhiteSpace(fromPassword))
                {
                    throw new Exception("Email configuration is missing or incomplete.");
                }

                var fromAddress = new MailAddress(fromEmail, fromName);
                var toAddress = new MailAddress(user.Email, firstName);

                string subject = GetEmailSubject(status);
                string body = GetEmailBody(status, firstName, assistance, dto);

                // Configure SMTP with better settings
                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                    smtp.Timeout = 30000; // 30 seconds timeout

                    using (var message = new MailMessage(fromAddress, toAddress))
                    {
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;
                        message.Priority = MailPriority.High;

                        // Add reply-to if needed
                        // message.ReplyToList.Add(new MailAddress("noreply@lingapdvo.com"));

                        await smtp.SendMailAsync(message);
                    }
                }

                Console.WriteLine($"Email sent successfully to {user.Email} for application {assistance.Id} - Status: {status}");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP Error: {smtpEx.Message}");
                Console.WriteLine($"Status Code: {smtpEx.StatusCode}");
                throw; // Re-throw to trigger retry
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to trigger retry
            }
        }

        private string GetEmailSubject(string status)
        {
            return status.ToLower() switch
            {
                "approve" => "🎉 Good News! Your Medical Help is Approved - LingapDVO",
                "disapprove" => "📋 Update on Your Medical Help Application - LingapDVO",
                "retake" => "🔄 Action Required - Please Resubmit Your Medical Help Documents - LingapDVO",
                _ => "📋 Medical Help Application Update - LingapDVO"
            };
        }

        private string GetEmailBody(string status, string firstName, OtherAssistance assistance, OtherAssistanceDto dto)
        {
            if (status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                return GetApprovalEmailBody(firstName, assistance, dto);
            }
            else if (status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase))
            {
                return GetDisapprovalEmailBody(firstName, assistance, dto);
            }
            else if (status.Equals("Retake", StringComparison.OrdinalIgnoreCase))
            {
                return GetRetakeEmailBody(firstName, assistance, dto);
            }

            return ""; // Default empty body
        }

        private string GetApprovalEmailBody(string firstName, OtherAssistance assistance, OtherAssistanceDto dto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #28a745, #20c997); color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #28a745; margin: 20px 0; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .details-box h3 {{ margin-top: 0; color: #28a745; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .next-steps {{ background-color: #e8f5e8; padding: 20px; border-radius: 4px; margin: 20px 0; }}
        .next-steps h3 {{ color: #28a745; margin-top: 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
        .celebrate {{ text-align: center; font-size: 48px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Medical Help Approved!</h1>
        </div>
        <div class='content'>
            <div class='celebrate'>✅</div>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                We are delighted to inform you that your Medical Help application has been <strong>APPROVED</strong>!
            </p>

            <div class='details-box'>
                <h3>📋 APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Medical Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Application ID:</span>
                    <span class='detail-value'>{assistance.Id}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value' style='color: #28a745; font-weight: bold;'>APPROVED</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Approved:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{dto.Processby}</span>
                </div>
            </div>

            <div class='details-box'>
                <h3>💬 REMARKS</h3>
                <p>{dto.Comments ?? "Your application has been processed successfully. No additional remarks provided."}</p>
            </div>

            <div class='next-steps'>
                <h3>📝 NEXT STEPS</h3>
                <p>Please visit our office to complete the necessary documentation and receive your assistance. Bring valid ID and any other required documents.</p>
            </div>

            <p class='message'>
                Thank you for using LingapDVO. We are here to help with your healthcare needs.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Medical Help Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetDisapprovalEmailBody(string firstName, OtherAssistance assistance, OtherAssistanceDto dto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #dc3545, #e5533f); color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #dc3545; margin: 20px 0; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .details-box h3 {{ margin-top: 0; color: #dc3545; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .contact-info {{ background-color: #f8f9fa; padding: 20px; border-radius: 4px; margin: 20px 0; border: 1px solid #dee2e6; }}
        .contact-info h3 {{ color: #495057; margin-top: 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
        .status-icon {{ text-align: center; font-size: 48px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📋 Application Update</h1>
        </div>
        <div class='content'>
            <div class='status-icon'>❌</div>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                After careful review, we regret to inform you that your Medical Help application has been <strong>DISAPPROVED</strong>.
            </p>

            <div class='details-box'>
                <h3>📋 APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Medical Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Application ID:</span>
                    <span class='detail-value'>{assistance.Id}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value' style='color: #dc3545; font-weight: bold;'>DISAPPROVED</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Updated:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{dto.Processby}</span>
                </div>
            </div>

            <div class='details-box'>
                <h3>💬 REMARKS</h3>
                <p>{dto.Comments ?? "Please contact our office for more information about this decision."}</p>
            </div>

            <div class='contact-info'>
                <h3>🤝 NEED MORE INFORMATION?</h3>
                <p>If you have questions or would like to discuss this decision further, please visit our office during office hours. Our staff will be happy to assist you.</p>
            </div>

            <p class='message'>
                We appreciate your understanding and thank you for considering LingapDVO for your healthcare needs.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Medical Help Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetRetakeEmailBody(string firstName, OtherAssistance assistance, OtherAssistanceDto dto)
        {
            var retakeReason = assistance.RetakeReason ?? dto.Comments ?? "Please review and resubmit your documents.";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #ff9800, #ff5722); color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #ff9800; margin: 20px 0; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .details-box h3 {{ margin-top: 0; color: #ff9800; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .reason-box {{ margin-top: 25px; padding: 20px; background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 4px; }}
        .reason-box h3 {{ color: #856404; margin-top: 0; font-size: 16px; }}
        .reason-box p {{ color: #856404; margin: 0; }}
        .steps-box {{ margin-top: 25px; padding: 20px; background-color: #e7f3ff; border-left: 4px solid #0066cc; border-radius: 4px; }}
        .steps-box h3 {{ color: #0066cc; margin-top: 0; font-size: 16px; }}
        .steps-box ol {{ margin: 10px 0; padding-left: 20px; }}
        .steps-box li {{ margin: 8px 0; color: #555; }}
        .button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #ff9800, #ff5722); color: white; text-decoration: none; border-radius: 5px; font-weight: 600; margin-top: 15px; text-align: center; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
        .action-icon {{ text-align: center; font-size: 48px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔄 Action Required - Resubmit Documents</h1>
        </div>
        <div class='content'>
            <div class='action-icon'>📝</div>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                We have reviewed your Medical Help application and need you to resubmit some documents.
                Your application details will remain the same - you only need to upload the corrected documents.
            </p>

            <div class='details-box'>
                <h3>📋 APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Medical Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Application ID:</span>
                    <span class='detail-value'>{assistance.Id}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value' style='color: #ff9800; font-weight: bold;'>RESUBMIT REQUIRED</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Updated:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{dto.Processby}</span>
                </div>
            </div>

            <div class='reason-box'>
                <h3>📋 DOCUMENTS NEEDING CORRECTION</h3>
                <p>{retakeReason}</p>
            </div>

            <div class='steps-box'>
                <h3>📝 HOW TO RESUBMIT YOUR DOCUMENTS</h3>
                <ol>
                    <li>Log in to your LingapDVO account</li>
                    <li>Go to Application Tracking section</li>
                    <li>Find this application and click on it</li>
                    <li>Upload the required corrected documents</li>
                    <li>Submit the updated application</li>
                </ol>
                <a href='https://lingap.online/Applicationtracking' class='button'>Go to Application Tracking</a>
            </div>

            <p class='message' style='margin-top: 20px;'>
                If you have any questions or need assistance, please visit our office during office hours or contact our support team.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Medical Help Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FuneralAssistanceProcessingStatus(int id, [FromForm] FuneralAssistanceDto FuneralAssistanceDto)
        {
            try
            {
                var funeralAssistance = context.FuneralAssistance.Find(id);

                if (funeralAssistance == null)
                {
                    TempData["ErrorMessage"] = "Funeral assistance record not found.";
                    return Redirect("/Admin");
                }

                // Validate required fields
                if (string.IsNullOrEmpty(FuneralAssistanceDto.Status2))
                {
                    TempData["ErrorMessage"] = "Status is required.";
                    return RedirectToAction("FuneralAssistanceProcessingStatus", new { id = id });
                }

                // Validate CMO Details Section ONLY for Approve status
                var status = FuneralAssistanceDto.Status2?.Trim();
                if (!string.IsNullOrEmpty(status) && status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
                {
                    var cmoReflection = FuneralAssistanceDto.ForCMOPERSONNEL ?? "";

                    // Check if at least one supporting document is selected (should be in the CMO reflection)
                    if (string.IsNullOrWhiteSpace(cmoReflection) || !cmoReflection.Contains("Docs:"))
                    {
                        TempData["ErrorMessage"] = "Please select at least one supporting document in the CMO Details section.";
                        return RedirectToAction("FuneralAssistanceProcessingStatus", new { id = id });
                    }


                    // Extract and validate GrantedAmount
                    if (!cmoReflection.Contains("GrantedAmount:") || string.IsNullOrWhiteSpace(cmoReflection.Split("GrantedAmount:").Skip(1).FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim()))
                    {
                        TempData["ErrorMessage"] = "Please enter the Amount Granted in the CMO Details section.";
                        return RedirectToAction("FuneralAssistanceProcessingStatus", new { id = id });
                    }

                    // Validate that granted amount is greater than 0
                    var grantedAmountStr = cmoReflection.Split("GrantedAmount:").Skip(1).FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                    if (!decimal.TryParse(grantedAmountStr, out decimal grantedAmount) || grantedAmount <= 0)
                    {
                        TempData["ErrorMessage"] = "Amount Granted must be greater than 0 in the CMO Details section.";
                        return RedirectToAction("FuneralAssistanceProcessingStatus", new { id = id });
                    }
                }

                // Update record
                funeralAssistance.Status2 = FuneralAssistanceDto.Status2;
                funeralAssistance.ForCMOPERSONNEL = FuneralAssistanceDto.ForCMOPERSONNEL;
                funeralAssistance.Comments = FuneralAssistanceDto.Comments;
                funeralAssistance.Processby = FuneralAssistanceDto.Processby;
                funeralAssistance.Result = _dateTimeService.Now;

                // Handle Retake status - move back to Pending and track retake information
                if (!string.IsNullOrEmpty(status) && status.Equals("Retake", StringComparison.OrdinalIgnoreCase))
                {
                    funeralAssistance.Status = "Pending"; // Move back to Pending
                    funeralAssistance.IsRetakeApplication = true;
                    funeralAssistance.RetakeRequestedAt = _dateTimeService.Now;

                    // Extract retake reason from Comments (format: "RETAKE REQUEST: reason")
                    if (!string.IsNullOrEmpty(FuneralAssistanceDto.Comments) && FuneralAssistanceDto.Comments.StartsWith("RETAKE REQUEST: "))
                    {
                        funeralAssistance.RetakeReason = FuneralAssistanceDto.Comments.Substring("RETAKE REQUEST: ".Length);
                    }
                    else
                    {
                        funeralAssistance.RetakeReason = FuneralAssistanceDto.Comments;
                    }
                }

                await context.SaveChangesAsync();

                // Send multi-channel notification
                if (!string.IsNullOrEmpty(status) && (status.Equals("Approve", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase) ||
                    status.Equals("Retake", StringComparison.OrdinalIgnoreCase)))
                {
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == funeralAssistance.UserId);
                    var applicantName = verifyAccount?.Firstname ?? "Applicant";

                    // Send notification (fire and forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.SendStatusChangeNotificationAsync(
                                funeralAssistance.UserId,
                                applicantName,
                                "FuneralAssistance",
                                status,
                                funeralAssistance.Id,
                                status.Equals("Retake", StringComparison.OrdinalIgnoreCase) ? funeralAssistance.RetakeReason : FuneralAssistanceDto.Comments,
                                FuneralAssistanceDto.Processby
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Notification error: {ex.Message}");
                        }
                    });

                    // Send email synchronously (wait for it to complete)
                    try
                    {
                        await SendFuneralStatusEmailWithRetry(funeralAssistance, FuneralAssistanceDto, status, applicantName);
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"Email sending failed after retries: {emailEx.Message}");
                        // Log but don't fail the main operation
                    }
                }

                TempData["SuccessMessage"] = $"Funeral assistance status updated to '{FuneralAssistanceDto.Status2}' successfully.";
                
                // Redirect with appropriate status parameter based on the action
                string redirectStatus = status?.ToLower() switch
                {
                    "approve" => "approve",
                    "disapprove" => "disapprove",
                    "retake" => "retakes",
                    _ => "processing"
                };
                return Redirect($"/Admin?status={redirectStatus}");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while updating status: " + ex.Message;
                return Redirect("/Admin");
            }
        }

        // New method with retry logic for Funeral Assistance
        private async Task SendFuneralStatusEmailWithRetry(FuneralAssistance assistance, FuneralAssistanceDto dto, string status, string firstName, int maxRetries = 3)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < maxRetries)
            {
                try
                {
                    await SendFuneralStatusEmail(assistance, dto, status, firstName);
                    Console.WriteLine($"Funeral email sent successfully on attempt {retryCount + 1}");
                    return; // Success - exit method
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retryCount++;
                    Console.WriteLine($"Funeral email attempt {retryCount} failed: {ex.Message}");

                    if (retryCount < maxRetries)
                    {
                        // Wait before retry (exponential backoff)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                    }
                }
            }

            // If we get here, all retries failed
            throw new Exception($"Failed to send funeral email after {maxRetries} attempts", lastException);
        }

        private async Task SendFuneralStatusEmail(FuneralAssistance assistance, FuneralAssistanceDto dto, string status, string firstName)
        {
            try
            {
                // Get user with explicit null checking
                var user = await context.RegisterAcc.FindAsync(assistance.UserId);

                if (user == null)
                {
                    Console.WriteLine($"User not found for UserId: {assistance.UserId}");
                    return;
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    Console.WriteLine($"Email not found for UserId: {assistance.UserId}");
                    return;
                }

                // Validate email configuration
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];

                if (string.IsNullOrWhiteSpace(fromEmail) ||
                    string.IsNullOrWhiteSpace(fromName) ||
                    string.IsNullOrWhiteSpace(fromPassword))
                {
                    throw new Exception("Email configuration is missing or incomplete.");
                }

                var fromAddress = new MailAddress(fromEmail, fromName);
                var toAddress = new MailAddress(user.Email, firstName);

                string subject = GetFuneralEmailSubject(status);
                string body = GetFuneralEmailBody(status, firstName, assistance, dto);

                // Configure SMTP with better settings
                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                    smtp.Timeout = 30000; // 30 seconds timeout

                    using (var message = new MailMessage(fromAddress, toAddress))
                    {
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;
                        message.Priority = MailPriority.High;

                        await smtp.SendMailAsync(message);
                    }
                }

                Console.WriteLine($"Funeral email sent successfully to {user.Email} for application {assistance.Id} - Status: {status}");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP Error: {smtpEx.Message}");
                Console.WriteLine($"Status Code: {smtpEx.StatusCode}");
                throw; // Re-throw to trigger retry
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Funeral email sending error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to trigger retry
            }
        }

        private string GetFuneralEmailSubject(string status)
        {
            return status.ToLower() switch
            {
                "approve" => "🎉 Good News! Your Funeral Help is Approved - LingapDVO",
                "disapprove" => "📋 Update on Your Funeral Help Application - LingapDVO",
                "retake" => "🔄 Action Required - Please Resubmit Your Funeral Help Documents - LingapDVO",
                _ => "📋 Funeral Help Application Update - LingapDVO"
            };
        }

        private string GetFuneralEmailBody(string status, string firstName, FuneralAssistance assistance, FuneralAssistanceDto dto)
        {
            if (status.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                return GetFuneralApprovalEmailBody(firstName, assistance, dto);
            }
            else if (status.Equals("Disapprove", StringComparison.OrdinalIgnoreCase))
            {
                return GetFuneralDisapprovalEmailBody(firstName, assistance, dto);
            }
            else if (status.Equals("Retake", StringComparison.OrdinalIgnoreCase))
            {
                return GetFuneralRetakeEmailBody(firstName, assistance, dto);
            }

            return ""; // Default empty body
        }

        private string GetFuneralApprovalEmailBody(string firstName, FuneralAssistance assistance, FuneralAssistanceDto dto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #28a745, #20c997); color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #28a745; margin: 20px 0; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .details-box h3 {{ margin-top: 0; color: #28a745; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .next-steps {{ background-color: #e8f5e8; padding: 20px; border-radius: 4px; margin: 20px 0; }}
        .next-steps h3 {{ color: #28a745; margin-top: 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
        .celebrate {{ text-align: center; font-size: 48px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Funeral Help Approved!</h1>
        </div>
        <div class='content'>
            <div class='celebrate'>✅</div>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                We are pleased to inform you that your Funeral Help application has been <strong>APPROVED</strong>.
                Our deepest condolences for your loss, and we hope this assistance provides some comfort during this difficult time.
            </p>

            <div class='details-box'>
                <h3>📋 APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Funeral Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Application ID:</span>
                    <span class='detail-value'>{assistance.Id}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value' style='color: #28a745; font-weight: bold;'>APPROVED</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Approved:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{dto.Processby}</span>
                </div>
            </div>

            <div class='details-box'>
                <h3>💬 REMARKS</h3>
                <p>{dto.Comments ?? "Your application has been processed successfully. No additional remarks provided."}</p>
            </div>

            <div class='next-steps'>
                <h3>📝 NEXT STEPS</h3>
                <p>Please visit our office to complete the necessary documentation and receive your assistance. Bring valid ID and any other required documents related to the funeral arrangements.</p>
            </div>

            <p class='message'>
                Our thoughts are with you and your family during this time of loss.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Funeral Help Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetFuneralDisapprovalEmailBody(string firstName, FuneralAssistance assistance, FuneralAssistanceDto dto)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #dc3545, #e5533f); color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #dc3545; margin: 20px 0; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .details-box h3 {{ margin-top: 0; color: #dc3545; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .contact-info {{ background-color: #f8f9fa; padding: 20px; border-radius: 4px; margin: 20px 0; border: 1px solid #dee2e6; }}
        .contact-info h3 {{ color: #495057; margin-top: 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
        .status-icon {{ text-align: center; font-size: 48px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📋 Application Update</h1>
        </div>
        <div class='content'>
            <div class='status-icon'>❌</div>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                After careful review, we regret to inform you that your Funeral Help application has been <strong>DISAPPROVED</strong>.
            </p>

            <div class='details-box'>
                <h3>📋 APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Funeral Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Application ID:</span>
                    <span class='detail-value'>{assistance.Id}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value' style='color: #dc3545; font-weight: bold;'>DISAPPROVED</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Updated:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{dto.Processby}</span>
                </div>
            </div>

            <div class='details-box'>
                <h3>💬 REMARKS</h3>
                <p>{dto.Comments ?? "Please contact our office for more information about this decision."}</p>
            </div>

            <div class='contact-info'>
                <h3>🤝 NEED MORE INFORMATION?</h3>
                <p>If you have questions or would like to discuss this decision further, please visit our office during office hours. Our staff will be happy to assist you.</p>
            </div>

            <p class='message'>
                We appreciate your understanding during this difficult time.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Funeral Help Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetFuneralRetakeEmailBody(string firstName, FuneralAssistance assistance, FuneralAssistanceDto dto)
        {
            var retakeReason = assistance.RetakeReason ?? dto.Comments ?? "Please review and resubmit your documents.";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background: linear-gradient(135deg, #ff9800, #ff5722); color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #ff9800; margin: 20px 0; border-radius: 4px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .details-box h3 {{ margin-top: 0; color: #ff9800; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .reason-box {{ margin-top: 25px; padding: 20px; background-color: #fff3cd; border-left: 4px solid #ffc107; border-radius: 4px; }}
        .reason-box h3 {{ color: #856404; margin-top: 0; font-size: 16px; }}
        .reason-box p {{ color: #856404; margin: 0; }}
        .steps-box {{ margin-top: 25px; padding: 20px; background-color: #e7f3ff; border-left: 4px solid #0066cc; border-radius: 4px; }}
        .steps-box h3 {{ color: #0066cc; margin-top: 0; font-size: 16px; }}
        .steps-box ol {{ margin: 10px 0; padding-left: 20px; }}
        .steps-box li {{ margin: 8px 0; color: #555; }}
        .button {{ display: inline-block; padding: 12px 30px; background: linear-gradient(135deg, #ff9800, #ff5722); color: white; text-decoration: none; border-radius: 5px; font-weight: 600; margin-top: 15px; text-align: center; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
        .action-icon {{ text-align: center; font-size: 48px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔄 Action Required - Resubmit Documents</h1>
        </div>
        <div class='content'>
            <div class='action-icon'>📝</div>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                We have reviewed your Funeral Help application and need you to resubmit some documents.
                Your application details will remain the same - you only need to upload the corrected documents.
            </p>

            <div class='details-box'>
                <h3>📋 APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Funeral Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Application ID:</span>
                    <span class='detail-value'>{assistance.Id}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value' style='color: #ff9800; font-weight: bold;'>RESUBMIT REQUIRED</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Updated:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{dto.Processby}</span>
                </div>
            </div>

            <div class='reason-box'>
                <h3>📋 DOCUMENTS NEEDING CORRECTION</h3>
                <p>{retakeReason}</p>
            </div>

            <div class='steps-box'>
                <h3>📝 HOW TO RESUBMIT YOUR DOCUMENTS</h3>
                <ol>
                    <li>Log in to your LingapDVO account</li>
                    <li>Go to Application Tracking section</li>
                    <li>Find this application and click on it</li>
                    <li>Upload the required corrected documents</li>
                    <li>Submit the updated application</li>
                </ol>
                <a href='https://lingap.online/Applicationtracking' class='button'>Go to Application Tracking</a>
            </div>

            <p class='message' style='margin-top: 20px;'>
                If you have any questions or need assistance, please visit our office during office hours or contact our support team.
            </p>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Funeral Help Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
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
                HospitalAssistance.ClaimedAt = _dateTimeService.Now;

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

                    string subject = "Hospital Bill Assistance Received - LingapDVO";
                    string body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background-color: #dc143c; color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #dc143c; margin: 20px 0; border-radius: 4px; }}
        .details-box h3 {{ margin-top: 0; color: #dc143c; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .feedback-section {{ margin-top: 25px; padding: 20px; background-color: #e8f4f8; border-left: 4px solid #0066cc; border-radius: 4px; }}
        .feedback-section h3 {{ color: #0066cc; margin-top: 0; font-size: 18px; }}
        .feedback-section p {{ color: #555; margin-bottom: 15px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #0066cc; color: white; text-decoration: none; border-radius: 5px; font-weight: 600; margin-top: 10px; }}
        .button:hover {{ background-color: #0052a3; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Assistance Received</h1>
        </div>
        <div class='content'>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                We are happy to tell you that your Hospital Bill Assistance has been received.
                Thank you for your patience and cooperation throughout the process.
            </p>

            <div class='details-box'>
                <h3>APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Hospital Bill Assistance</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value'><strong>Claimed</strong></span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{HospitalAssistanceDto.Processby ?? "LINGAP Personnel"}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Claimed:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                {(!string.IsNullOrEmpty(HospitalAssistanceDto.Comments) ? $@"
                <div class='detail-item'>
                    <span class='detail-label'>Remarks:</span>
                    <span class='detail-value'>{HospitalAssistanceDto.Comments}</span>
                </div>" : "")}
            </div>

            <div class='feedback-section'>
                <h3>📝 Tell Us What You Think!</h3>
                <p>We want to know what you think. Please take a moment to tell us about your experience with our service. Your feedback helps us serve you better.</p>
                <a href='https://lingap.online/Feedback' class='button'>Give Feedback</a>
            </div>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Medical Help Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

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
                        Body = body,
                        IsBodyHtml = true
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
        public async Task<IActionResult> OtherAssistanceApproveStatus(int id, OtherAssistanceDto OtherAssistanceDto)
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
                otherAssistance.ClaimedAt = _dateTimeService.Now;

                context.SaveChanges();

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == otherAssistance.UserId);

                // Only send email if status is "Claimed"
                if (OtherAssistanceDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get first name from VerifyAccount
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

                    string subject = "Medical and Laboratory Assistance Received - LingapDVO";
                    string body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background-color: #dc143c; color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #dc143c; margin: 20px 0; border-radius: 4px; }}
        .details-box h3 {{ margin-top: 0; color: #dc143c; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .feedback-section {{ margin-top: 25px; padding: 20px; background-color: #e8f4f8; border-left: 4px solid #0066cc; border-radius: 4px; }}
        .feedback-section h3 {{ color: #0066cc; margin-top: 0; font-size: 18px; }}
        .feedback-section p {{ color: #555; margin-bottom: 15px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #0066cc; color: white; text-decoration: none; border-radius: 5px; font-weight: 600; margin-top: 10px; }}
        .button:hover {{ background-color: #0052a3; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Assistance Received</h1>
        </div>
        <div class='content'>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                We are happy to tell you that your Medical and Laboratory Assistance has been received.
                Thank you for your patience and cooperation throughout the process.
            </p>

            <div class='details-box'>
                <h3>APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Medical and Laboratory Assistance</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value'><strong>Claimed</strong></span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{OtherAssistanceDto.Processby ?? "LINGAP Personnel"}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Claimed:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                {(!string.IsNullOrEmpty(OtherAssistanceDto.Comments) ? $@"
                <div class='detail-item'>
                    <span class='detail-label'>Remarks:</span>
                    <span class='detail-value'>{OtherAssistanceDto.Comments}</span>
                </div>" : "")}
            </div>

            <div class='feedback-section'>
                <h3>📝 Tell Us What You Think!</h3>
                <p>We want to know what you think. Please take a moment to tell us about your experience with our service. Your feedback helps us serve you better.</p>
                <a href='https://lingap.online/Feedback' class='button'>Give Feedback</a>
            </div>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Medical Help Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

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
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        await smtp.SendMailAsync(message);
                    }

                    Console.WriteLine($"Claimed status email sent to {user.Email} for application {otherAssistance.Id}");
                }

                // Send multi-channel notification for all status changes (preserving your existing functionality)
                var status = OtherAssistanceDto.Status3?.Trim();
                if (!string.IsNullOrEmpty(status))
                {
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == otherAssistance.UserId);
                    var applicantName = verifyAccount?.Firstname ?? "Applicant";

                    _ = _notificationService.SendStatusChangeNotificationAsync(
                        otherAssistance.UserId,
                        applicantName,
                        "Medical and Laboratory",
                        status,
                        otherAssistance.Id
                    );
                }

                TempData["SuccessMessage"] = $"Medical and laboratory status updated to '{OtherAssistanceDto.Status3}' successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OtherAssistanceApproveStatus: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = "An error occurred while updating status: " + ex.Message;
                return Redirect("/Admin");
            }
        }
        [HttpPost]
        public async Task<IActionResult> FuneralAssistanceApproveStatus(int id, FuneralAssistanceDto FuneralAssistanceDto)
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
                funeralAssistance.ClaimedAt = _dateTimeService.Now;

                context.SaveChanges();

                // Get user info
                var user = context.RegisterAcc.FirstOrDefault(u => u.Id == funeralAssistance.UserId);

                // Only send email if status is "Claimed"
                if (FuneralAssistanceDto.Status3?.Equals("Claimed", StringComparison.OrdinalIgnoreCase) == true
                    && user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Get first name from VerifyAccount
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

                    string subject = "Funeral Help Received - LingapDVO";
                    string body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background-color: #ffffff; }}
        .header {{ background-color: #dc143c; color: white; padding: 30px 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: bold; }}
        .content {{ padding: 30px 20px; background-color: #f9f9f9; }}
        .greeting {{ font-size: 18px; color: #333; margin-bottom: 20px; }}
        .message {{ font-size: 16px; color: #555; margin-bottom: 25px; line-height: 1.8; }}
        .details-box {{ background-color: #fff; padding: 20px; border-left: 4px solid #dc143c; margin: 20px 0; border-radius: 4px; }}
        .details-box h3 {{ margin-top: 0; color: #dc143c; font-size: 16px; }}
        .detail-item {{ margin: 10px 0; padding: 8px 0; border-bottom: 1px solid #eee; }}
        .detail-label {{ font-weight: 600; color: #333; display: inline-block; width: 140px; }}
        .detail-value {{ color: #555; }}
        .feedback-section {{ margin-top: 25px; padding: 20px; background-color: #e8f4f8; border-left: 4px solid #0066cc; border-radius: 4px; }}
        .feedback-section h3 {{ color: #0066cc; margin-top: 0; font-size: 18px; }}
        .feedback-section p {{ color: #555; margin-bottom: 15px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #0066cc; color: white; text-decoration: none; border-radius: 5px; font-weight: 600; margin-top: 10px; }}
        .button:hover {{ background-color: #0052a3; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; background-color: #f0f0f0; border-radius: 0 0 8px 8px; }}
        .footer p {{ margin: 5px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Assistance Received</h1>
        </div>
        <div class='content'>
            <p class='greeting'>Dear {firstName},</p>
            <p class='message'>
                We are happy to tell you that your Funeral Help has been received.
                Thank you for your patience and cooperation throughout the process.
            </p>

            <div class='details-box'>
                <h3>APPLICATION DETAILS</h3>
                <div class='detail-item'>
                    <span class='detail-label'>Application Type:</span>
                    <span class='detail-value'>Funeral Help</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Status:</span>
                    <span class='detail-value'><strong>Claimed</strong></span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Processed By:</span>
                    <span class='detail-value'>{FuneralAssistanceDto.Processby ?? "LINGAP Personnel"}</span>
                </div>
                <div class='detail-item'>
                    <span class='detail-label'>Date Claimed:</span>
                    <span class='detail-value'>{_dateTimeService.Now:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                {(!string.IsNullOrEmpty(FuneralAssistanceDto.Comments) ? $@"
                <div class='detail-item'>
                    <span class='detail-label'>Remarks:</span>
                    <span class='detail-value'>{FuneralAssistanceDto.Comments}</span>
                </div>" : "")}
            </div>

            <div class='feedback-section'>
                <h3>📝 Tell Us What You Think!</h3>
                <p>We want to know what you think. Please take a moment to tell us about your experience with our service. Your feedback helps us serve you better.</p>
                <a href='https://lingap.online/Feedback' class='button'>Give Feedback</a>
            </div>
        </div>
        <div class='footer'>
            <p><strong>LingapDVO Medical Help Program</strong></p>
            <p>This is an automated message. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

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
                        Body = body,
                        IsBodyHtml = true
                    })
                    {
                        await smtp.SendMailAsync(message);
                    }
                }

                // Optional: Add multi-channel notification for all status changes like in OtherAssistance
                var status = FuneralAssistanceDto.Status3?.Trim();
                if (!string.IsNullOrEmpty(status))
                {
                    var verifyAccount = context.Verifyaccount.FirstOrDefault(v => v.UserId == funeralAssistance.UserId);
                    var applicantName = verifyAccount?.Firstname ?? "Applicant";

                    _ = _notificationService.SendStatusChangeNotificationAsync(
                        funeralAssistance.UserId,
                        applicantName,
                        "Funeral Help",
                        status,
                        funeralAssistance.Id
                    );
                }

                TempData["SuccessMessage"] = $"Funeral assistance status updated to '{FuneralAssistanceDto.Status3}' successfully.";
                return Redirect("/Admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in FuneralAssistanceApproveStatus: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                TempData["ErrorMessage"] = "An error occurred while updating status: " + ex.Message;
                return Redirect("/Admin");
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
                var now = _dateTimeService.Now;

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
        public IActionResult GetFeedbackStatistics(DateTime? startDate, DateTime? endDate, string? assistanceType = null)
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
                var timeline = feedbacks.Where(f => f.SubmittedAt >= _dateTimeService.Now.AddDays(-30))
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
            var validRatings = ratings.Where(r => r.HasValue).Select(r => r!.Value).ToList();
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
            }.Where(r => r.HasValue).Select(r => r!.Value).ToList();

            return ratings.Any() ? Math.Round(ratings.Average(), 2) : 0;
        }

        // TEMPORARY: Action to generate 200 realistic dummy data records
        // This will be removed after data is generated
        public async Task<IActionResult> GenerateDummyData()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "true")
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            try
            {
                // First, clear existing test data if needed (be careful with this in production!)
                // Uncomment below to clear existing data:
                // context.HospitalAssistance.RemoveRange(context.HospitalAssistance);
                // context.OtherAssistance.RemoveRange(context.OtherAssistance);
                // context.FuneralAssistance.RemoveRange(context.FuneralAssistance);
                // await context.SaveChangesAsync();

                var random = new Random(42); // Fixed seed for reproducibility
                var now = _dateTimeService.Now;

                // Arrays of realistic Filipino data
                var firstNames = new[] { "Juan", "Maria", "Jose", "Ana", "Pedro", "Rosa", "Miguel", "Elena", "Roberto", "Carmen", "Luis", "Sofia", "Carlos", "Isabella", "Diego", "Gabriela", "Fernando", "Valentina", "Antonio", "Camila", "Manuel", "Victoria", "Rafael", "Lucia", "Andres", "Mariana", "Jorge", "Paula", "Ricardo", "Diana", "Francisco", "Catalina", "Alejandro", "Daniela", "Martin", "Adriana", "Javier", "Natalia", "Raul", "Angela" };
                var lastNames = new[] { "Santos", "Reyes", "Cruz", "Bautista", "Garcia", "Gonzales", "Flores", "Mendoza", "Torres", "Lopez", "Ramos", "Rivera", "Gomez", "Hernandez", "Perez", "Martinez", "Rodriguez", "Fernandez", "Morales", "Castillo", "Aquino", "Villarosa", "Dela Cruz", "Villanueva", "Santiago", "Medina", "Roxas", "Ocampo", "Aguilar", "Soriano", "Navarro", "Pascual", "Lim", "Tan", "Ong", "Chua", "Wong", "Lee", "Chan", "Go" };
                var middleNames = new[] { "Santos", "Cruz", "Reyes", "Garcia", "Bautista", "Flores", "Lopez", "Ramos", "Perez", "Gonzales", "Mendoza", "Torres", "Rivera", "Martinez", "Gomez", "Rodriguez", "Fernandez", "Morales", "Castillo", "Aquino" };
                var barangays = new[] { "Agdao", "Bankerohan", "Buhangin", "Bunawan", "Calinan", "Daliao", "Davao City Proper", "Lanang", "Ma-a", "Matina", "Mintal", "Panacan", "Sasa", "Talomo", "Tibungco", "Toril", "Tugbok", "Ula", "Bago Oshiro", "Bago Gallera", "Catalunan Grande", "Catalunan Pequeño", "Dumoy", "Indangan", "Leon Garcia", "Marilog", "Paquibato", "Shrine Hills", "Tacunan", "Waan", "Wilfredo Aquino" };
                var streets = new[] { "Roxas Avenue", "J.P. Laurel Avenue", "C.M. Recto Street", "Bonifacio Street", "Rizal Street", "Quirino Avenue", "Osmeña Boulevard", "Mabini Street", "Aguinaldo Street", "Luna Street", "Del Pilar Street", "Quezon Boulevard", "Magallanes Street", "Lapu-Lapu Street", "San Pedro Street" };
                var assistanceTypes = new[] { "Hospital Bill Assistance", "Medicines", "Laboratory", "Medical and Surgical Procedures" };
                var funeralTypes = new[] { "Funeral and Burial Assistance" };
                var sexes = new[] { "Male", "Female" };
                var philHealthStatuses = new[] { "Yes", "No" };
                var relationships = new[] { "Parent", "Spouse", "Child", "Sibling", "Relative", "Guardian", "Friend" };
                var processors = new[] { "Admin Santos", "Admin Garcia", "Admin Cruz", "Admin Reyes", "Admin Flores" };

                // IP addresses and user agents for realistic data
                var ipAddresses = new[] { "192.168.1.100", "10.0.0.50", "172.16.0.10", "192.168.0.25", "10.1.1.75" };
                var userAgents = new[] {
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0",
                    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1"
                };

                // STEP 1: Create unique user accounts (RegisterAcc)
                var createdUsers = new List<RegisterAcc>();
                var userCount = 50; // Create 50 unique users

                for (int i = 0; i < userCount; i++)
                {
                    var regDate = now.AddDays(-random.Next(185, 365)); // Registered 6-12 months ago
                    var firstName = firstNames[random.Next(firstNames.Length)];
                    var middleName = middleNames[random.Next(middleNames.Length)];
                    var lastName = lastNames[random.Next(lastNames.Length)];
                    var username = $"{firstName.ToLower()}.{lastName.ToLower()}{i}";
                    var email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}@gmail.com";

                    var user = new RegisterAcc
                    {
                        FirstName = firstName,
                        MiddleName = middleName,
                        LastName = lastName,
                        Suffix = random.Next(10) > 8 ? (random.Next(2) == 0 ? "Jr." : "Sr.") : "",
                        Email = email,
                        Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                        Username = username,
                        Status = "active",
                        Profilepicture = "default-avatar.png",
                        PreferEmailNotification = true,
                        PreferSmsNotification = random.Next(2) == 0,
                        PreferInAppNotification = true
                    };

                    context.RegisterAcc.Add(user);
                    createdUsers.Add(user);
                }

                // Save users to get their IDs
                await context.SaveChangesAsync();

                // STEP 2: Create RegistrationTokens and RegistrationAuditLogs for each user
                foreach (var user in createdUsers)
                {
                    var regDate = now.AddDays(-random.Next(185, 365));
                    var tokenCreatedAt = regDate.AddSeconds(-random.Next(10, 60));
                    var token = $"REG-TOKEN-{Guid.NewGuid():N}";
                    var ip = ipAddresses[random.Next(ipAddresses.Length)];
                    var userAgent = userAgents[random.Next(userAgents.Length)];

                    // Create RegistrationToken
                    var regToken = new RegistrationToken
                    {
                        Token = token,
                        IpAddress = ip,
                        UserAgent = userAgent,
                        CreatedAt = tokenCreatedAt,
                        ExpiresAt = tokenCreatedAt.AddMinutes(30),
                        IsUsed = true,
                        UsedAt = regDate,
                        UsedByEmail = user.Email,
                        IsRevoked = false
                    };
                    context.Add(regToken);

                    // Create RegistrationAuditLog
                    var auditLog = new RegistrationAuditLog
                    {
                        IpAddress = ip,
                        UserAgent = userAgent,
                        Email = user.Email,
                        Username = user.Username,
                        FullName = $"{user.LastName} {user.FirstName} {user.MiddleName}",
                        Action = "SUCCESS",
                        Source = "WEB_FORM",
                        Reason = "User registered successfully",
                        RegistrationToken = token,
                        HasValidToken = true,
                        SuspiciousActivity = false,
                        AttemptedAt = regDate,
                        RegisteredUserId = user.Id
                    };
                    context.Add(auditLog);
                }

                await context.SaveChangesAsync();

                var generatedRecords = 0;

                // Distribute 200 records: 80 Hospital, 70 Medical/Lab, 50 Funeral
                var hospitalCount = 80;
                var medicalCount = 70;
                var funeralCount = 50;

                // STEP 3: Generate Hospital Assistance records with random user assignment
                for (int i = 0; i < hospitalCount; i++)
                {
                    var createdDate = now.AddDays(-random.Next(1, 180)); // Within last 6 months
                    var status = DetermineStatus(random);
                    var age = random.Next(18, 80);
                    var dateOfBirth = _dateTimeService.Now.AddYears(-age).ToString("yyyy-MM-dd");
                    var assignedUser = createdUsers[random.Next(createdUsers.Count)]; // Random user

                    var record = new HospitalAssistance
                    {
                        UserId = assignedUser.Id,
                        Lastname = lastNames[random.Next(lastNames.Length)],
                        Firstname = firstNames[random.Next(firstNames.Length)],
                        Middlename = middleNames[random.Next(middleNames.Length)],
                        Suffix = random.Next(10) > 7 ? (random.Next(2) == 0 ? "Jr." : "Sr.") : "",
                        BlkLotStreet = $"{random.Next(1, 500)} {streets[random.Next(streets.Length)]}",
                        SubVill = random.Next(10) > 5 ? $"Phase {random.Next(1, 5)}" : "",
                        Brgy = barangays[random.Next(barangays.Length)],
                        District = $"District {random.Next(1, 4)}",
                        Sex = sexes[random.Next(sexes.Length)],
                        PhilHealth = philHealthStatuses[random.Next(philHealthStatuses.Length)],
                        PhilHealthNo = random.Next(2) == 0 ? $"12-{random.Next(100000000, 999999999)}-{random.Next(0, 10)}" : "",
                        Dateofbirth = dateOfBirth,
                        Age = age.ToString(),

                        // Requestor details
                        RLastname = lastNames[random.Next(lastNames.Length)],
                        RFirstname = firstNames[random.Next(firstNames.Length)],
                        RMiddlename = middleNames[random.Next(middleNames.Length)],
                        RSuffix = "",
                        RBlkLotStreet = $"{random.Next(1, 500)} {streets[random.Next(streets.Length)]}",
                        RSubVill = random.Next(10) > 5 ? $"Phase {random.Next(1, 5)}" : "",
                        RBrgy = barangays[random.Next(barangays.Length)],
                        RDistrict = $"District {random.Next(1, 4)}",
                        RelationshipPatient = relationships[random.Next(relationships.Length)],
                        ContactNo = $"09{random.Next(100000000, 999999999)}",

                        Typeassistance = "Hospital Bill Assistance",
                        ForCMOPERSONNEL = "",
                        Validfrontimage = "dummy_id_front.jpg",
                        ValidBackimage = "dummy_id_back.jpg",
                        DoctorPrescription = "dummy_prescription.jpg",
                        DeathCertificate = "",

                        CreatedAt = createdDate,
                        ProcessAt = status.Status != "pending" ? createdDate.AddMinutes(random.Next(5, 30)) : DateTime.MinValue,
                        Status = status.Status,
                        Processby = status.Status != "pending" ? processors[random.Next(processors.Length)] : "",
                        Comments = status.Comments,
                        Result = status.Status2 != "" ? createdDate.AddMinutes(GenerateProcessingTime(random)) : DateTime.MinValue,
                        Status2 = status.Status2,
                        ClaimedAt = status.Status3 == "claimed" ? createdDate.AddDays(random.Next(7, 30)) : DateTime.MinValue,
                        Status3 = status.Status3
                    };

                    context.HospitalAssistance.Add(record);
                    generatedRecords++;
                }

                // Generate Medical/Laboratory Assistance records
                for (int i = 0; i < medicalCount; i++)
                {
                    var createdDate = now.AddDays(-random.Next(1, 180));
                    var status = DetermineStatus(random);
                    var age = random.Next(18, 80);
                    var dateOfBirth = _dateTimeService.Now.AddYears(-age).ToString("yyyy-MM-dd");
                    var assignedUser = createdUsers[random.Next(createdUsers.Count)]; // Random user

                    var record = new OtherAssistance
                    {
                        UserId = assignedUser.Id,
                        Lastname = lastNames[random.Next(lastNames.Length)],
                        Firstname = firstNames[random.Next(firstNames.Length)],
                        Middlename = middleNames[random.Next(middleNames.Length)],
                        Suffix = random.Next(10) > 7 ? (random.Next(2) == 0 ? "Jr." : "Sr.") : "",
                        BlkLotStreet = $"{random.Next(1, 500)} {streets[random.Next(streets.Length)]}",
                        SubVill = random.Next(10) > 5 ? $"Phase {random.Next(1, 5)}" : "",
                        Brgy = barangays[random.Next(barangays.Length)],
                        District = $"District {random.Next(1, 4)}",
                        Sex = sexes[random.Next(sexes.Length)],
                        PhilHealth = philHealthStatuses[random.Next(philHealthStatuses.Length)],
                        PhilHealthNo = random.Next(2) == 0 ? $"12-{random.Next(100000000, 999999999)}-{random.Next(0, 10)}" : "",
                        Dateofbirth = dateOfBirth,
                        Age = age.ToString(),

                        // Requestor details
                        RLastname = lastNames[random.Next(lastNames.Length)],
                        RFirstname = firstNames[random.Next(firstNames.Length)],
                        RMiddlename = middleNames[random.Next(middleNames.Length)],
                        RSuffix = "",
                        RBlkLotStreet = $"{random.Next(1, 500)} {streets[random.Next(streets.Length)]}",
                        RSubVill = random.Next(10) > 5 ? $"Phase {random.Next(1, 5)}" : "",
                        RBrgy = barangays[random.Next(barangays.Length)],
                        RDistrict = $"District {random.Next(1, 4)}",
                        RelationshipPatient = relationships[random.Next(relationships.Length)],
                        ContactNo = $"09{random.Next(100000000, 999999999)}",

                        Typeassistance = assistanceTypes[random.Next(1, assistanceTypes.Length)], // Skip first one (Hospital Bill)
                        ForCMOPERSONNEL = "",
                        Validfrontimage = "dummy_id_front.jpg",
                        ValidBackimage = "dummy_id_back.jpg",
                        DoctorPrescription = "dummy_prescription.jpg",
                        DeathCertificate = "",
                        MedCertificate = "dummy_med_cert.jpg",

                        CreatedAt = createdDate,
                        ProcessAt = status.Status != "pending" ? createdDate.AddMinutes(random.Next(5, 30)) : DateTime.MinValue,
                        Status = status.Status,
                        Processby = status.Status != "pending" ? processors[random.Next(processors.Length)] : "",
                        Comments = status.Comments,
                        Result = status.Status2 != "" ? createdDate.AddMinutes(GenerateProcessingTime(random)) : DateTime.MinValue,
                        Status2 = status.Status2,
                        ClaimedAt = status.Status3 == "claimed" ? createdDate.AddDays(random.Next(7, 30)) : DateTime.MinValue,
                        Status3 = status.Status3
                    };

                    context.OtherAssistance.Add(record);
                    generatedRecords++;
                }

                // Generate Funeral Help records
                for (int i = 0; i < funeralCount; i++)
                {
                    var createdDate = now.AddDays(-random.Next(1, 180));
                    var status = DetermineStatus(random);
                    var age = random.Next(18, 80);
                    var dateOfBirth = _dateTimeService.Now.AddYears(-age).ToString("yyyy-MM-dd");
                    var assignedUser = createdUsers[random.Next(createdUsers.Count)]; // Random user

                    var record = new FuneralAssistance
                    {
                        UserId = assignedUser.Id,
                        Lastname = lastNames[random.Next(lastNames.Length)],
                        Firstname = firstNames[random.Next(firstNames.Length)],
                        Middlename = middleNames[random.Next(middleNames.Length)],
                        Suffix = random.Next(10) > 7 ? (random.Next(2) == 0 ? "Jr." : "Sr.") : "",
                        BlkLotStreet = $"{random.Next(1, 500)} {streets[random.Next(streets.Length)]}",
                        SubVill = random.Next(10) > 5 ? $"Phase {random.Next(1, 5)}" : "",
                        Brgy = barangays[random.Next(barangays.Length)],
                        District = $"District {random.Next(1, 4)}",
                        Sex = sexes[random.Next(sexes.Length)],
                        PhilHealth = philHealthStatuses[random.Next(philHealthStatuses.Length)],
                        PhilHealthNo = random.Next(2) == 0 ? $"12-{random.Next(100000000, 999999999)}-{random.Next(0, 10)}" : "",
                        Dateofbirth = dateOfBirth,
                        Age = age.ToString(),

                        // Requestor details
                        RLastname = lastNames[random.Next(lastNames.Length)],
                        RFirstname = firstNames[random.Next(firstNames.Length)],
                        RMiddlename = middleNames[random.Next(middleNames.Length)],
                        RSuffix = "",
                        RBlkLotStreet = $"{random.Next(1, 500)} {streets[random.Next(streets.Length)]}",
                        RSubVill = random.Next(10) > 5 ? $"Phase {random.Next(1, 5)}" : "",
                        RBrgy = barangays[random.Next(barangays.Length)],
                        RDistrict = $"District {random.Next(1, 4)}",
                        RelationshipPatient = relationships[random.Next(relationships.Length)],
                        ContactNo = $"09{random.Next(100000000, 999999999)}",

                        Typeassistance = "Funeral and Burial Assistance",
                        ForCMOPERSONNEL = "",
                        Validfrontimage = "dummy_id_front.jpg",
                        ValidBackimage = "dummy_id_back.jpg",
                        DoctorPrescription = "",
                        DeathCertificate = "dummy_death_cert.jpg",

                        CreatedAt = createdDate,
                        ProcessAt = status.Status != "pending" ? createdDate.AddMinutes(random.Next(5, 30)) : DateTime.MinValue,
                        Status = status.Status,
                        Processby = status.Status != "pending" ? processors[random.Next(processors.Length)] : "",
                        Comments = status.Comments,
                        Result = status.Status2 != "" ? createdDate.AddMinutes(GenerateProcessingTime(random)) : DateTime.MinValue,
                        Status2 = status.Status2,
                        ClaimedAt = status.Status3 == "claimed" ? createdDate.AddDays(random.Next(7, 30)) : DateTime.MinValue,
                        Status3 = status.Status3
                    };

                    context.FuneralAssistance.Add(record);
                    generatedRecords++;
                }

                // Save all applications first to get their IDs
                await context.SaveChangesAsync();

                // STEP 4: Generate FormSubmissionTokens and FormSubmissionAuditLogs for each application
                // Optimize: Batch operations to avoid N+1 query problem
                var tokensToAdd = new List<FormSubmissionToken>();
                var auditLogsToAdd = new List<FormSubmissionAuditLog>();

                // Generate tokens and audit logs for Hospital Assistance
                var hospitalApps = context.HospitalAssistance.Where(h => h.CreatedAt >= now.AddDays(-180)).ToList();
                foreach (var app in hospitalApps)
                {
                    var tokenCreatedAt = app.CreatedAt.AddSeconds(-random.Next(10, 60)); // Token created before submission
                    var token = $"TOKEN-HOSP-{Guid.NewGuid():N}";
                    var ip = ipAddresses[random.Next(ipAddresses.Length)];
                    var userAgent = userAgents[random.Next(userAgents.Length)];

                    // Create FormSubmissionToken
                    tokensToAdd.Add(new FormSubmissionToken
                    {
                        Token = token,
                        FormType = "HospitalBill",
                        UserId = app.UserId,
                        IpAddress = ip,
                        UserAgent = userAgent,
                        CreatedAt = tokenCreatedAt,
                        ExpiresAt = tokenCreatedAt.AddMinutes(30),
                        IsUsed = true,
                        UsedAt = app.CreatedAt,
                        SubmittedFormId = app.Id,
                        IsRevoked = false
                    });

                    // Create FormSubmissionAuditLog
                    auditLogsToAdd.Add(new FormSubmissionAuditLog
                    {
                        FormType = "HospitalBill",
                        UserId = app.UserId,
                        IpAddress = ip,
                        UserAgent = userAgent,
                        PatientName = $"{app.Lastname} {app.Firstname} {app.Middlename}",
                        RequestorName = $"{app.RLastname} {app.RFirstname} {app.RMiddlename}",
                        Action = "SUCCESS",
                        Source = "WEB_FORM",
                        Reason = "Form submitted successfully",
                        SubmissionToken = token,
                        HasValidToken = true,
                        SuspiciousActivity = false,
                        FormDataHash = $"SHA256-{Guid.NewGuid():N}",
                        AttemptedAt = app.CreatedAt,
                        SubmittedFormId = app.Id,
                        IsDuplicate = false
                    });
                }

                // Generate tokens and audit logs for Medical/Laboratory Assistance
                var otherApps = context.OtherAssistance.Where(o => o.CreatedAt >= now.AddDays(-180)).ToList();
                foreach (var app in otherApps)
                {
                    var tokenCreatedAt = app.CreatedAt.AddSeconds(-random.Next(10, 60));
                    var token = $"TOKEN-MED-{Guid.NewGuid():N}";
                    var ip = ipAddresses[random.Next(ipAddresses.Length)];
                    var userAgent = userAgents[random.Next(userAgents.Length)];

                    // Create FormSubmissionToken
                    tokensToAdd.Add(new FormSubmissionToken
                    {
                        Token = token,
                        FormType = "MedicalLab",
                        UserId = app.UserId,
                        IpAddress = ip,
                        UserAgent = userAgent,
                        CreatedAt = tokenCreatedAt,
                        ExpiresAt = tokenCreatedAt.AddMinutes(30),
                        IsUsed = true,
                        UsedAt = app.CreatedAt,
                        SubmittedFormId = app.Id,
                        IsRevoked = false
                    });

                    // Create FormSubmissionAuditLog
                    auditLogsToAdd.Add(new FormSubmissionAuditLog
                    {
                        FormType = "MedicalLab",
                        UserId = app.UserId,
                        IpAddress = ip,
                        UserAgent = userAgent,
                        PatientName = $"{app.Lastname} {app.Firstname} {app.Middlename}",
                        RequestorName = $"{app.RLastname} {app.RFirstname} {app.RMiddlename}",
                        Action = "SUCCESS",
                        Source = "WEB_FORM",
                        Reason = "Form submitted successfully",
                        SubmissionToken = token,
                        HasValidToken = true,
                        SuspiciousActivity = false,
                        FormDataHash = $"SHA256-{Guid.NewGuid():N}",
                        AttemptedAt = app.CreatedAt,
                        SubmittedFormId = app.Id,
                        IsDuplicate = false
                    });
                }

                // Generate tokens and audit logs for Funeral Help
                var funeralApps = context.FuneralAssistance.Where(f => f.CreatedAt >= now.AddDays(-180)).ToList();
                foreach (var app in funeralApps)
                {
                    var tokenCreatedAt = app.CreatedAt.AddSeconds(-random.Next(10, 60));
                    var token = $"TOKEN-FUN-{Guid.NewGuid():N}";
                    var ip = ipAddresses[random.Next(ipAddresses.Length)];
                    var userAgent = userAgents[random.Next(userAgents.Length)];

                    // Create FormSubmissionToken
                    tokensToAdd.Add(new FormSubmissionToken
                    {
                        Token = token,
                        FormType = "Funeral",
                        UserId = app.UserId,
                        IpAddress = ip,
                        UserAgent = userAgent,
                        CreatedAt = tokenCreatedAt,
                        ExpiresAt = tokenCreatedAt.AddMinutes(30),
                        IsUsed = true,
                        UsedAt = app.CreatedAt,
                        SubmittedFormId = app.Id,
                        IsRevoked = false
                    });

                    // Create FormSubmissionAuditLog
                    auditLogsToAdd.Add(new FormSubmissionAuditLog
                    {
                        FormType = "Funeral",
                        UserId = app.UserId,
                        IpAddress = ip,
                        UserAgent = userAgent,
                        PatientName = $"{app.Lastname} {app.Firstname} {app.Middlename}",
                        RequestorName = $"{app.RLastname} {app.RFirstname} {app.RMiddlename}",
                        Action = "SUCCESS",
                        Source = "WEB_FORM",
                        Reason = "Form submitted successfully",
                        SubmissionToken = token,
                        HasValidToken = true,
                        SuspiciousActivity = false,
                        FormDataHash = $"SHA256-{Guid.NewGuid():N}",
                        AttemptedAt = app.CreatedAt,
                        SubmittedFormId = app.Id,
                        IsDuplicate = false
                    });
                }

                // Batch add all tokens and audit logs
                context.AddRange(tokensToAdd);
                context.AddRange(auditLogsToAdd);

                // Save tokens and audit logs
                await context.SaveChangesAsync();

                // STEP 5: Generate Feedbacks for claimed applications (about 30% of claimed applications)
                var serviceTypes = new[] { "Hospital Bill Assistance", "Other Assistance", "Funeral Help" };
                var offices = new[] { "City Health Office", "Social Welfare Office", "CDVO", "Mayor's Office" };
                var clientTypes = new[] { "Citizen", "Business", "Government Employee", "Senior Citizen", "PWD" };
                var ccResponses = new[] { "Yes", "No", "Not Sure", "Somewhat" };

                var feedbackCount = 0;

                // Generate feedbacks for claimed Hospital Assistance
                var claimedHospital = context.HospitalAssistance
                    .Where(h => h.Status3 == "claimed" && h.CreatedAt >= now.AddDays(-180))
                    .ToList();

                foreach (var app in claimedHospital)
                {
                    if (random.Next(100) < 30) // 30% chance of feedback
                    {
                        var user = createdUsers.FirstOrDefault(u => u.Id == app.UserId);
                        if (user != null)
                        {
                            var feedback = new Feedback
                            {
                                UserId = user.Id,
                                Name = $"{app.RLastname} {app.RFirstname} {app.RMiddlename}",
                                Office = offices[random.Next(offices.Length)],
                                ServiceAvailed = "Hospital Bill Assistance",
                                Contact = app.ContactNo,
                                Sex = app.Sex,
                                TypeOfClient = clientTypes[random.Next(clientTypes.Length)],
                                AssistanceType = "HospitalBill",
                                AssistanceId = app.Id,
                                Q1_CCKnowledge = ccResponses[random.Next(ccResponses.Length)],
                                Q2_CCVisibility = ccResponses[random.Next(ccResponses.Length)],
                                Q3_CCHelpfulness = ccResponses[random.Next(ccResponses.Length)],
                                R1_ServiceSatisfaction = random.Next(4, 9), // Rating 4-8
                                R2_TimeSpent = random.Next(4, 9),
                                R3_ProcessFollowed = random.Next(4, 9),
                                R4_ProcessSimplicity = random.Next(4, 9),
                                R5_InformationAccess = random.Next(4, 9),
                                R6_FairPayment = random.Next(4, 9),
                                R7_Fairness = random.Next(4, 9),
                                R8_EmployeeCourtesy = random.Next(4, 9),
                                Commendation = random.Next(2) == 0 ? "The staff were very helpful and courteous." : null,
                                Suggestion = random.Next(2) == 0 ? "Please add more seating in the waiting area." : null,
                                Request = null,
                                Complaint = random.Next(10) == 0 ? "The waiting time was a bit long." : null,
                                Signature = $"{user.FirstName}_{user.LastName}",
                                SubmittedAt = app.ClaimedAt.AddDays(random.Next(1, 7)), // Feedback within 7 days of claiming
                                IpAddress = ipAddresses[random.Next(ipAddresses.Length)]
                            };
                            context.Add(feedback);
                            feedbackCount++;
                        }
                    }
                }

                // Generate feedbacks for claimed Medical/Lab Assistance
                var claimedMedical = context.OtherAssistance
                    .Where(o => o.Status3 == "claimed" && o.CreatedAt >= now.AddDays(-180))
                    .ToList();

                foreach (var app in claimedMedical)
                {
                    if (random.Next(100) < 30) // 30% chance of feedback
                    {
                        var user = createdUsers.FirstOrDefault(u => u.Id == app.UserId);
                        if (user != null)
                        {
                            var feedback = new Feedback
                            {
                                UserId = user.Id,
                                Name = $"{app.RLastname} {app.RFirstname} {app.RMiddlename}",
                                Office = offices[random.Next(offices.Length)],
                                ServiceAvailed = app.Typeassistance,
                                Contact = app.ContactNo,
                                Sex = app.Sex,
                                TypeOfClient = clientTypes[random.Next(clientTypes.Length)],
                                AssistanceType = "Medical",
                                AssistanceId = app.Id,
                                Q1_CCKnowledge = ccResponses[random.Next(ccResponses.Length)],
                                Q2_CCVisibility = ccResponses[random.Next(ccResponses.Length)],
                                Q3_CCHelpfulness = ccResponses[random.Next(ccResponses.Length)],
                                R1_ServiceSatisfaction = random.Next(4, 9),
                                R2_TimeSpent = random.Next(4, 9),
                                R3_ProcessFollowed = random.Next(4, 9),
                                R4_ProcessSimplicity = random.Next(4, 9),
                                R5_InformationAccess = random.Next(4, 9),
                                R6_FairPayment = random.Next(4, 9),
                                R7_Fairness = random.Next(4, 9),
                                R8_EmployeeCourtesy = random.Next(4, 9),
                                Commendation = random.Next(2) == 0 ? "Excellent service, thank you!" : null,
                                Suggestion = random.Next(2) == 0 ? "Online tracking of application status would be helpful." : null,
                                Request = null,
                                Complaint = random.Next(10) == 0 ? "Process could be streamlined." : null,
                                Signature = $"{user.FirstName}_{user.LastName}",
                                SubmittedAt = app.ClaimedAt.AddDays(random.Next(1, 7)),
                                IpAddress = ipAddresses[random.Next(ipAddresses.Length)]
                            };
                            context.Add(feedback);
                            feedbackCount++;
                        }
                    }
                }

                // Generate feedbacks for claimed Funeral Help
                var claimedFuneral = context.FuneralAssistance
                    .Where(f => f.Status3 == "claimed" && f.CreatedAt >= now.AddDays(-180))
                    .ToList();

                foreach (var app in claimedFuneral)
                {
                    if (random.Next(100) < 30) // 30% chance of feedback
                    {
                        var user = createdUsers.FirstOrDefault(u => u.Id == app.UserId);
                        if (user != null)
                        {
                            var feedback = new Feedback
                            {
                                UserId = user.Id,
                                Name = $"{app.RLastname} {app.RFirstname} {app.RMiddlename}",
                                Office = offices[random.Next(offices.Length)],
                                ServiceAvailed = "Funeral and Burial Assistance",
                                Contact = app.ContactNo,
                                Sex = app.Sex,
                                TypeOfClient = clientTypes[random.Next(clientTypes.Length)],
                                AssistanceType = "Funeral",
                                AssistanceId = app.Id,
                                Q1_CCKnowledge = ccResponses[random.Next(ccResponses.Length)],
                                Q2_CCVisibility = ccResponses[random.Next(ccResponses.Length)],
                                Q3_CCHelpfulness = ccResponses[random.Next(ccResponses.Length)],
                                R1_ServiceSatisfaction = random.Next(4, 9),
                                R2_TimeSpent = random.Next(4, 9),
                                R3_ProcessFollowed = random.Next(4, 9),
                                R4_ProcessSimplicity = random.Next(4, 9),
                                R5_InformationAccess = random.Next(4, 9),
                                R6_FairPayment = random.Next(4, 9),
                                R7_Fairness = random.Next(4, 9),
                                R8_EmployeeCourtesy = random.Next(4, 9),
                                Commendation = random.Next(2) == 0 ? "Very helpful during difficult time. Thank you." : null,
                                Suggestion = random.Next(2) == 0 ? "More information about requirements would help." : null,
                                Request = null,
                                Complaint = random.Next(10) == 0 ? "Needed faster processing for emergency cases." : null,
                                Signature = $"{user.FirstName}_{user.LastName}",
                                SubmittedAt = app.ClaimedAt.AddDays(random.Next(1, 7)),
                                IpAddress = ipAddresses[random.Next(ipAddresses.Length)]
                            };
                            context.Add(feedback);
                            feedbackCount++;
                        }
                    }
                }

                await context.SaveChangesAsync();

                var totalTokens = 200; // Total form submission tokens
                var totalAuditLogs = 200; // Total form submission audit logs
                var totalRegTokens = userCount; // Total registration tokens
                var totalRegAuditLogs = userCount; // Total registration audit logs

                return Ok(new {
                    success = true,
                    message = $"Successfully generated complete dummy data with users, applications, tokens, audit logs, and feedbacks!",
                    details = new {
                        users = userCount,
                        registrationTokens = totalRegTokens,
                        registrationAuditLogs = totalRegAuditLogs,
                        hospital = hospitalCount,
                        medical = medicalCount,
                        funeral = funeralCount,
                        formSubmissionTokens = totalTokens,
                        formSubmissionAuditLogs = totalAuditLogs,
                        feedbacks = feedbackCount
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error generating dummy data: {ex.Message}" });
            }
        }

        // Helper method to determine realistic status progression
        // Updated to follow realistic application lifecycle through the system
        // Most applications should complete successfully (claimed)
        private (string Status, string Status2, string Status3, string Comments) DetermineStatus(Random random)
        {
            var roll = random.Next(100);

            // 12% Pending (new applications just submitted) - NOT in priorities
            if (roll < 12)
            {
                return ("pending", "", "", "");
            }
            // 10% Processing (being reviewed by admin) - NOT in priorities
            else if (roll < 22)
            {
                return ("processing", "", "", "Under review by admin");
            }
            // 25% Approved but not claimed - IN PRIORITY POOL
            // These are waiting for the user to claim their assistance
            else if (roll < 47)
            {
                var comments = new[] {
                    "Application approved. Documents verified.",
                    "Assistance granted. All requirements met.",
                    "Approved for processing. Complete documentation provided.",
                    "Application successful. Ready for assistance.",
                    "Verified and approved. Proceed to claiming."
                };
                return ("processing", "approve", "", comments[random.Next(comments.Length)]);
            }
            // 48% Claimed (successfully completed the entire process) - NOT in priorities
            // This represents the majority of applications that complete successfully
            else if (roll < 95)
            {
                var comments = new[] {
                    "Application approved. Documents verified. Assistance claimed.",
                    "Assistance granted and received.",
                    "Approved and claimed. Process completed.",
                    "Application successful. Assistance provided.",
                    "Verified, approved, and claimed successfully."
                };
                return ("processing", "approve", "claimed", comments[random.Next(comments.Length)]);
            }
            // 5% Disapproved (less than 8% as required) - IN PRIORITY POOL
            // Small percentage of applications that don't meet requirements
            else
            {
                var comments = new[] {
                    "Incomplete documentation. Please provide missing requirements.",
                    "Application does not meet eligibility criteria.",
                    "Documents need verification. Please resubmit.",
                    "Duplicate application found.",
                    "Applicant does not qualify for this type of assistance.",
                    "Required supporting documents not provided.",
                    "Application information inconsistent with requirements."
                };
                return ("processing", "disapprove", "", comments[random.Next(comments.Length)]);
            }
        }

        // Helper method to generate realistic processing times
        // Returns processing time in minutes
        // Balanced distribution for realistic priority tracking
        private int GenerateProcessingTime(Random random)
        {
            var roll = random.Next(100);

            // 65% processed in less than 1 hour (10-55 minutes) - No priority
            // Most applications are processed quickly
            if (roll < 65)
            {
                return random.Next(10, 56);
            }
            // 20% processed in 1-2 hours (60-119 minutes) - Medium priority
            // Some applications need more review time
            else if (roll < 85)
            {
                return random.Next(60, 120);
            }
            // 15% processed in 2-4 hours (120-240 minutes) - High priority
            // Few applications require extended processing
            else
            {
                return random.Next(120, 241);
            }
        }

        // ==============================================
        // DECRYPTION API FOR ADDITIONAL INFORMATION
        // ==============================================
        [HttpPost]
        public IActionResult DecryptField(string fieldValue, string formType, int formId)
        {
            try
            {
                // Get current user info
                var userIdString = HttpContext.Session.GetString("UserId");
                var isAdmin = HttpContext.Session.GetString("AdminFullname");
                var isSuperadmin = HttpContext.Session.GetString("IsSuperadmin");

                // Authorization check
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                // Check if user has permission to decrypt
                bool canDecrypt = false;

                if (isAdmin == "true" || isSuperadmin == "true")
                {
                    // Admin and Superadmin can decrypt all records
                    canDecrypt = true;
                }
                else
                {
                    // Regular users can only decrypt their own records
                    int? recordUserId = null;

                    switch (formType)
                    {
                        case "Hospital":
                            recordUserId = context.HospitalAssistance
                                .Where(h => h.Id == formId)
                                .Select(h => h.UserId)
                                .FirstOrDefault();
                            break;
                        case "Funeral":
                            recordUserId = context.FuneralAssistance
                                .Where(f => f.Id == formId)
                                .Select(f => f.UserId)
                                .FirstOrDefault();
                            break;
                        case "Other":
                            recordUserId = context.OtherAssistance
                                .Where(o => o.Id == formId)
                                .Select(o => o.UserId)
                                .FirstOrDefault();
                            break;
                    }

                    canDecrypt = (recordUserId == userId);
                }

                if (!canDecrypt)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Check if fieldValue is empty or null
                if (string.IsNullOrEmpty(fieldValue))
                {
                    return Json(new { success = true, data = "" });
                }

                // Check if fieldValue is only whitespace
                if (string.IsNullOrWhiteSpace(fieldValue))
                {
                    return Json(new { success = true, data = "" });
                }

                // Try to decrypt the field
                string decryptedValue;

                Console.WriteLine($"=== Processing Field for Form {formType} ID {formId} ===");
                Console.WriteLine($"Field value (first 100 chars): {fieldValue.Substring(0, Math.Min(100, fieldValue.Length))}");
                Console.WriteLine($"Field value length: {fieldValue.Length}");
                Console.WriteLine($"Is Base64: {IsBase64String(fieldValue)}");

                try
                {
                    // Check if the value looks like Base64 encrypted data
                    if (IsBase64String(fieldValue))
                    {
                        Console.WriteLine($"Attempting to decrypt Base64 field...");

                        // Attempt decryption
                        decryptedValue = _aesEncryptionService.Decrypt(fieldValue);

                        Console.WriteLine($"✓ Successfully decrypted! Decrypted length: {decryptedValue.Length}");
                        Console.WriteLine($"Decrypted value (first 50 chars): {decryptedValue.Substring(0, Math.Min(50, decryptedValue.Length))}");
                    }
                    else
                    {
                        // Not Base64, likely plain text - return as is
                        decryptedValue = fieldValue;
                        Console.WriteLine($"✓ Not Base64 - treating as plain text");
                    }
                }
                catch (Exception decryptEx)
                {
                    // Decryption failed
                    Console.WriteLine($"✗ DECRYPTION FAILED!");
                    Console.WriteLine($"Error Type: {decryptEx.GetType().Name}");
                    Console.WriteLine($"Error Message: {decryptEx.Message}");
                    Console.WriteLine($"Stack Trace: {decryptEx.StackTrace}");

                    // Return error message instead of encrypted data
                    decryptedValue = $"[Decryption Error: {decryptEx.Message}]";
                }

                return Json(new { success = true, data = decryptedValue });
            }
            catch (Exception ex)
            {
                // Log detailed error information for debugging
                Console.WriteLine($"=== DecryptField Error ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine($"Field value length: {fieldValue?.Length ?? 0}");
                Console.WriteLine($"Form type: {formType}, Form ID: {formId}");
                Console.WriteLine($"==========================");

                // Return error to frontend for display
                return Json(new { success = false, message = $"Decryption failed: {ex.Message}" });
            }
        }

        // Helper method to check if a string is valid Base64
        private bool IsBase64String(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Base64 string length should be divisible by 4
            if (value.Length % 4 != 0)
                return false;

            // Check if string contains only valid Base64 characters
            try
            {
                Convert.FromBase64String(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ========================================================
        // PASSWORD VERIFICATION FOR VIEWING ENCRYPTED DATA
        // ========================================================
        [HttpPost]
        public IActionResult VerifyPasswordForDecryption(string password)
        {
            try
            {
                // Get current user info
                var userIdString = HttpContext.Session.GetString("UserId");
                var sessionPassword = HttpContext.Session.GetString("UserPassword");
                var isGoogleUser = HttpContext.Session.GetString("IsGoogleUser");

                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                if (string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Password is required" });
                }

                // Google users don't have passwords, so they're automatically verified
                if (isGoogleUser == "true")
                {
                    return Json(new { success = true, message = "Verified (Google user)" });
                }

                if (string.IsNullOrEmpty(sessionPassword))
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                // Verify the typed password matches the session password
                // This confirms user intent without hitting the database
                if (password == sessionPassword)
                {
                    return Json(new { success = true, message = "Password verified successfully" });
                }
                else
                {
                    return Json(new { success = false, message = "Invalid password" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Password verification error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = "Verification failed: " + ex.Message });
            }
        }


    }


}
