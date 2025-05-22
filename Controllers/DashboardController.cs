using iText.Commons.Actions.Data;
using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Net;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace LingapDVO.Controllers
{
    public class Dashboard : Controller
    {
        public readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;

        public Dashboard(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            this.context = context;
            this.environment = environment;
        }
        public IActionResult Index()
        {

            return View();
        }

        public IActionResult Landingpage()
        {
            return View();
        }

        public IActionResult Listofpartners()
        {
            return View();
        }

        public IActionResult Homepage()
        {  // Prevent caching
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            // Check session

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
           }


            ViewBag.Fullname = HttpContext.Session.GetString("Fullname");
            ViewBag.ImageFilename = HttpContext.Session.GetString("ImageFilename"); // Add this line if not yet set
            return View();
        }

        public IActionResult Userprofile(int id)
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

        public IActionResult FillupformHospitalBill()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }
            return View();
        }

        [HttpPost]
        public IActionResult FillupformHospitalBill(FillupformHospitalBillDto fillupformHospitalbilldto)
        {
            if (string.IsNullOrEmpty(fillupformHospitalbilldto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }
            // Get the current user's ID from the session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                // If user is not logged in, redirect to login page
                return RedirectToAction("Login", "Login");
            }

            if (fillupformHospitalbilldto.IdBackimage == null && fillupformHospitalbilldto.IdFrontimage == null &&
                fillupformHospitalbilldto.DoctorPrescriptionimage == null && fillupformHospitalbilldto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("ImageFile", "The image file is required");
            }

            if (!ModelState.IsValid)
            {
                return View(fillupformHospitalbilldto);
            }

            try
            {
                // Generate unique filenames
                string newFileNameFront = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                          Path.GetExtension(fillupformHospitalbilldto.IdFrontimage!.FileName);

                string newFileNameBack = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                       Path.GetExtension(fillupformHospitalbilldto.IdBackimage!.FileName);

                string newFileNamePrescription = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                       Path.GetExtension(fillupformHospitalbilldto.DoctorPrescriptionimage!.FileName);

                string? newFileNameDeathCertificate = null;

                // Save image to wwwroot/UsersImg
                string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "Funeralimg");

                // Save Front Image
                string filePathFront = Path.Combine(uploadsFolder, newFileNameFront);
                using (var stream = new FileStream(filePathFront, FileMode.Create))
                {
                    fillupformHospitalbilldto.IdFrontimage.CopyTo(stream);
                }

                // Save Back Image
                string filePathBack = Path.Combine(uploadsFolder, newFileNameBack);
                using (var stream = new FileStream(filePathBack, FileMode.Create))
                {
                    fillupformHospitalbilldto.IdBackimage.CopyTo(stream);
                }

                // Save Prescription Image
                string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                using (var stream = new FileStream(filePathPrescription, FileMode.Create))
                {
                    fillupformHospitalbilldto.DoctorPrescriptionimage.CopyTo(stream);
                }

                if (fillupformHospitalbilldto.DeathCertificateimage != null)
                {
                    newFileNameDeathCertificate = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                        Path.GetExtension(fillupformHospitalbilldto.DeathCertificateimage.FileName);

                    string filePathDeathCertificate = Path.Combine(uploadsFolder2, newFileNameDeathCertificate);
                    using (var stream = new FileStream(filePathDeathCertificate, FileMode.Create))
                    {
                        fillupformHospitalbilldto.DeathCertificateimage.CopyTo(stream);
                    }
                }
                // Map data to entity
                FillupformHospitalBill fillupformHospitalBill = new FillupformHospitalBill()
                {
                    UserId = userId,
                    // Patient Details
                    Lastname = fillupformHospitalbilldto.Lastname,
                    Firstname = fillupformHospitalbilldto.Firstname,
                    Middlename = fillupformHospitalbilldto.Middlename,
                    Suffix = fillupformHospitalbilldto.Suffix,
                    BlkLotStreet = fillupformHospitalbilldto.BlkLotStreet,
                    SubVill = fillupformHospitalbilldto.SubVill,
                    Brgy = fillupformHospitalbilldto.Brgy,
                    District = fillupformHospitalbilldto.District,
                    Sex = fillupformHospitalbilldto.Sex,
                    PhilHealth = fillupformHospitalbilldto.PhilHealth,
                    PhilHealthNo = fillupformHospitalbilldto.PhilHealthNo,
                    Dateofbirth = fillupformHospitalbilldto.Dateofbirth,
                    Age = fillupformHospitalbilldto.Age,

                    // Requestor Details
                    RLastname = fillupformHospitalbilldto.RLastname,
                    RFirstname = fillupformHospitalbilldto.RFirstname,
                    RMiddlename = fillupformHospitalbilldto.RMiddlename,
                    RSuffix = fillupformHospitalbilldto.RSuffix,
                    RBlkLotStreet = fillupformHospitalbilldto.RBlkLotStreet,
                    RSubVill = fillupformHospitalbilldto.RSubVill,
                    RBrgy = fillupformHospitalbilldto.RBrgy,
                    RDistrict = fillupformHospitalbilldto.RDistrict,
                    RelationshipPatient = fillupformHospitalbilldto.RelationshipPatient,
                    ContactNo = fillupformHospitalbilldto.ContactNo,

                    // Assistance Type
                    Typeassistance = fillupformHospitalbilldto.Typeassistance,
                    ForCMOPERSONNEL = fillupformHospitalbilldto.ForCMOPERSONNEL,

                    // Image Paths
                    Validfrontimage = newFileNameFront,
                    ValidBackimage = newFileNameBack,
                    DoctorPrescription = newFileNamePrescription,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    Status = "Pending",


                    // Created Timestamp
                    CreatedAt = DateTime.Now
                };

                context.FillupformHospitalBill.Add(fillupformHospitalBill);
                context.SaveChanges();

                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (DbUpdateException ex)
            {
                // Exception handling remains the same
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

                    return View(fillupformHospitalbilldto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(fillupformHospitalbilldto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(fillupformHospitalbilldto);
            }
        }

        public IActionResult FillupformHospitalBilledit(int id)
        {
            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);

          
            if (fillupformhospitalBill == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
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


            return View();

        }

        [HttpPost]
        public IActionResult FillupformHospitalBilledit(int id, FillupformHospitalBillDto fillupformHospitalbilldto)
        {
            var fillupformhospitalBill = context.FillupformHospitalBill.Find(id);

            if (fillupformhospitalBill == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            if (string.IsNullOrEmpty(fillupformHospitalbilldto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // Remove validation requirements for images if they're not provided
            if (fillupformHospitalbilldto.IdFrontimage == null) ModelState.Remove("IdFrontimage");
            if (fillupformHospitalbilldto.IdBackimage == null) ModelState.Remove("IdBackimage");
            if (fillupformHospitalbilldto.DoctorPrescriptionimage == null) ModelState.Remove("DoctorPrescriptionimage");
            if (fillupformHospitalbilldto.DeathCertificateimage == null) ModelState.Remove("DeathCertificateimage");

            if (!ModelState.IsValid)
            {
                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = fillupformhospitalBill.Validfrontimage;
                ViewData["ValidBackimage"] = fillupformhospitalBill.ValidBackimage;
                ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;
                ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                return View(fillupformHospitalbilldto);
            }

            try
            {
                // Update text properties
                fillupformhospitalBill.Lastname = fillupformHospitalbilldto.Lastname ?? fillupformhospitalBill.Lastname;
                fillupformhospitalBill.Firstname = fillupformHospitalbilldto.Firstname ?? fillupformhospitalBill.Firstname;
                fillupformhospitalBill.Middlename = fillupformHospitalbilldto.Middlename ?? fillupformhospitalBill.Middlename;
                fillupformhospitalBill.Suffix = fillupformHospitalbilldto.Suffix ?? fillupformhospitalBill.Suffix;
                fillupformhospitalBill.BlkLotStreet = fillupformHospitalbilldto.BlkLotStreet ?? fillupformhospitalBill.BlkLotStreet;
                fillupformhospitalBill.SubVill = fillupformHospitalbilldto.SubVill ?? fillupformhospitalBill.SubVill;
                fillupformhospitalBill.Brgy = fillupformHospitalbilldto.Brgy ?? fillupformhospitalBill.Brgy;
                fillupformhospitalBill.District = fillupformHospitalbilldto.District ?? fillupformhospitalBill.District;
                fillupformhospitalBill.Sex = fillupformHospitalbilldto.Sex ?? fillupformhospitalBill.Sex;
                fillupformhospitalBill.PhilHealth = fillupformHospitalbilldto.PhilHealth ?? fillupformhospitalBill.PhilHealth;
                fillupformhospitalBill.PhilHealthNo = fillupformHospitalbilldto.PhilHealthNo;
                fillupformhospitalBill.Dateofbirth = fillupformHospitalbilldto.Dateofbirth ?? fillupformhospitalBill.Dateofbirth;
                fillupformhospitalBill.Age = fillupformHospitalbilldto.Age ?? fillupformhospitalBill.Age;

                // Requestor Details
                fillupformhospitalBill.RLastname = fillupformHospitalbilldto.RLastname;
                fillupformhospitalBill.RFirstname = fillupformHospitalbilldto.RFirstname;
                fillupformhospitalBill.RMiddlename = fillupformHospitalbilldto.RMiddlename;
                fillupformhospitalBill.RSuffix = fillupformHospitalbilldto.RSuffix;
                fillupformhospitalBill.RBlkLotStreet = fillupformHospitalbilldto.RBlkLotStreet;
                fillupformhospitalBill.RSubVill = fillupformHospitalbilldto.RSubVill;
                fillupformhospitalBill.RBrgy = fillupformHospitalbilldto.RBrgy;
                fillupformhospitalBill.RDistrict = fillupformHospitalbilldto.RDistrict;
                fillupformhospitalBill.RelationshipPatient = fillupformHospitalbilldto.RelationshipPatient;
                fillupformhospitalBill.ContactNo = fillupformHospitalbilldto.ContactNo;

                // Assistance Type
                fillupformhospitalBill.Typeassistance = fillupformHospitalbilldto.Typeassistance ?? fillupformhospitalBill.Typeassistance;
                fillupformhospitalBill.ForCMOPERSONNEL = fillupformHospitalbilldto.ForCMOPERSONNEL;

                // Handle ID Front image
                if (fillupformHospitalbilldto.IdFrontimage != null)
                {
                    string newFileNameFront = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(fillupformHospitalbilldto.IdFrontimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameFront);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(fillupformhospitalBill.Validfrontimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, fillupformhospitalBill.Validfrontimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fillupformHospitalbilldto.IdFrontimage.CopyTo(stream);
                    }

                    fillupformhospitalBill.Validfrontimage = newFileNameFront;
                }

                // Handle ID Back image
                if (fillupformHospitalbilldto.IdBackimage != null)
                {
                    string newFileNameBack = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(fillupformHospitalbilldto.IdBackimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameBack);

                    if (!string.IsNullOrEmpty(fillupformhospitalBill.ValidBackimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, fillupformhospitalBill.ValidBackimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fillupformHospitalbilldto.IdBackimage.CopyTo(stream);
                    }

                    fillupformhospitalBill.ValidBackimage = newFileNameBack;
                }

                // Handle Doctor Prescription image
                if (fillupformHospitalbilldto.DoctorPrescriptionimage != null)
                {
                    string newFileNamePrescription = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(fillupformHospitalbilldto.DoctorPrescriptionimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    string filePath = Path.Combine(uploadsFolder, newFileNamePrescription);

                    if (!string.IsNullOrEmpty(fillupformhospitalBill.DoctorPrescription))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, fillupformhospitalBill.DoctorPrescription);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fillupformHospitalbilldto.DoctorPrescriptionimage.CopyTo(stream);
                    }

                    fillupformhospitalBill.DoctorPrescription = newFileNamePrescription;
                }

                // Handle Death Certificate image
                if (fillupformHospitalbilldto.DeathCertificateimage != null)
                {
                    string newFileNameDeathCertificate = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(fillupformHospitalbilldto.DeathCertificateimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameDeathCertificate);

                    if (!string.IsNullOrEmpty(fillupformhospitalBill.DeathCertificate))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, fillupformhospitalBill.DeathCertificate);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fillupformHospitalbilldto.DeathCertificateimage.CopyTo(stream);
                    }

                    fillupformhospitalBill.DeathCertificate = newFileNameDeathCertificate;
                }

                context.SaveChanges();
                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);

                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = fillupformhospitalBill.Validfrontimage;
                ViewData["ValidBackimage"] = fillupformhospitalBill.ValidBackimage;
                ViewData["DoctorPrescription"] = fillupformhospitalBill.DoctorPrescription;
                ViewData["DeathCertificate"] = fillupformhospitalBill.DeathCertificate;

                return View(fillupformHospitalbilldto);
            }
        }

        public IActionResult FillupformHospitalBilldelete(int id)
        {
            var fillupformHospitalbill = context.FillupformHospitalBill.Find(id);
            if (fillupformHospitalbill == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Instead of deleting files and record, just update the status
            fillupformHospitalbill.Status = "Removed";
            context.FillupformHospitalBill.Update(fillupformHospitalbill);
            context.SaveChanges();

            return RedirectToAction("Homepage", "Dashboard");
        }

        public IActionResult Medicalandlabform()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Medicalandlabform(MedicalandlabformDto medicalandlabformdto)
        {
            if (string.IsNullOrEmpty(medicalandlabformdto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }
            // Get the current user's ID from the session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                // If user is not logged in, redirect to login page
                return RedirectToAction("Login", "Login");
            }

            if (medicalandlabformdto.IdBackimage == null && medicalandlabformdto.IdFrontimage == null &&
                medicalandlabformdto.DoctorPrescriptionimage == null && medicalandlabformdto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("ImageFile", "The image file is required");
            }

            if (!ModelState.IsValid)
            {
                return View(medicalandlabformdto);
            }

            try
            {
                // Generate unique filenames
                string newFileNameFront = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                          Path.GetExtension(medicalandlabformdto.IdFrontimage!.FileName);

                string newFileNameBack = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                       Path.GetExtension(medicalandlabformdto.IdBackimage!.FileName);

                string newFileNamePrescription = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                       Path.GetExtension(medicalandlabformdto.DoctorPrescriptionimage!.FileName);

                string? newFileNameDeathCertificate = null;

                // Save image to wwwroot/UsersImg
                string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "Funeralimg");

                // Save Front Image
                string filePathFront = Path.Combine(uploadsFolder, newFileNameFront);
                using (var stream = new FileStream(filePathFront, FileMode.Create))
                {
                    medicalandlabformdto.IdFrontimage.CopyTo(stream);
                }

                // Save Back Image
                string filePathBack = Path.Combine(uploadsFolder, newFileNameBack);
                using (var stream = new FileStream(filePathBack, FileMode.Create))
                {
                    medicalandlabformdto.IdBackimage.CopyTo(stream);
                }

                // Save Prescription Image
                string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                using (var stream = new FileStream(filePathPrescription, FileMode.Create))
                {
                    medicalandlabformdto.DoctorPrescriptionimage.CopyTo(stream);
                }

                if (medicalandlabformdto.DeathCertificateimage != null)
                {
                    newFileNameDeathCertificate = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                        Path.GetExtension(medicalandlabformdto.DeathCertificateimage.FileName);

                    string filePathDeathCertificate = Path.Combine(uploadsFolder2, newFileNameDeathCertificate);
                    using (var stream = new FileStream(filePathDeathCertificate, FileMode.Create))
                    {
                        medicalandlabformdto.DeathCertificateimage.CopyTo(stream);
                    }
                }
                // Map data to entity
                Medicalandlabform medicalandlabform = new Medicalandlabform()

                {
                    UserId = userId,
                    // Patient Details
                    Lastname = medicalandlabformdto.Lastname,
                    Firstname = medicalandlabformdto.Firstname,
                    Middlename = medicalandlabformdto.Middlename,
                    Suffix = medicalandlabformdto.Suffix,
                    BlkLotStreet = medicalandlabformdto.BlkLotStreet,
                    SubVill = medicalandlabformdto.SubVill,
                    Brgy = medicalandlabformdto.Brgy,
                    District = medicalandlabformdto.District,
                    Sex = medicalandlabformdto.Sex,
                    PhilHealth = medicalandlabformdto.PhilHealth,
                    PhilHealthNo = medicalandlabformdto.PhilHealthNo,
                    Dateofbirth = medicalandlabformdto.Dateofbirth,
                    Age = medicalandlabformdto.Age,

                    // Requestor Details
                    RLastname = medicalandlabformdto.RLastname,
                    RFirstname = medicalandlabformdto.RFirstname,
                    RMiddlename = medicalandlabformdto.RMiddlename,
                    RSuffix = medicalandlabformdto.RSuffix,
                    RBlkLotStreet = medicalandlabformdto.RBlkLotStreet,
                    RSubVill = medicalandlabformdto.RSubVill,
                    RBrgy = medicalandlabformdto.RBrgy,
                    RDistrict = medicalandlabformdto.RDistrict,
                    RelationshipPatient = medicalandlabformdto.RelationshipPatient,
                    ContactNo = medicalandlabformdto.ContactNo,

                    // Assistance Type
                    Typeassistance = medicalandlabformdto.Typeassistance,
                    ForCMOPERSONNEL = medicalandlabformdto.ForCMOPERSONNEL,

                    // Image Paths
                    Validfrontimage = newFileNameFront,
                    ValidBackimage = newFileNameBack,
                    DoctorPrescription = newFileNamePrescription,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    Status = "Pending",


                    // Created Timestamp
                    CreatedAt = DateTime.Now
                };

                context.Medicalandlabform.Add(medicalandlabform);
                context.SaveChanges();

                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (DbUpdateException ex)
            {
                // Exception handling remains the same
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

                    return View(medicalandlabformdto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(medicalandlabformdto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(medicalandlabformdto);
            }
        }

        public IActionResult Medicalandlabformedit(int id)
        {
            var medicalandlabform = context.Medicalandlabform.Find(id);


            if (medicalandlabform == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
            ViewData["Id"] = medicalandlabform.Id;
            ViewData["Lastname"] = medicalandlabform.Lastname;
            ViewData["Firstname"] = medicalandlabform.Firstname;
            ViewData["Middlename"] = medicalandlabform.Middlename;
            ViewData["Suffix"] = medicalandlabform.Suffix;
            ViewData["BlkLotStreet"] = medicalandlabform.BlkLotStreet;
            ViewData["SubVill"] = medicalandlabform.SubVill;
            ViewData["Brgy"] =  medicalandlabform.Brgy;
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

            return View();
 
        }

        [HttpPost]
        public IActionResult Medicalandlabformedit(int id, MedicalandlabformDto medicalandlabformdto)
        {
            var medicalandlabform = context.Medicalandlabform.Find(id);

            if (medicalandlabform == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            if (string.IsNullOrEmpty(medicalandlabformdto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // Remove validation requirements for images if they're not provided
            if (medicalandlabformdto.IdFrontimage == null) ModelState.Remove("IdFrontimage");
            if (medicalandlabformdto.IdBackimage == null) ModelState.Remove("IdBackimage");
            if (medicalandlabformdto.DoctorPrescriptionimage == null) ModelState.Remove("DoctorPrescriptionimage");
            if (medicalandlabformdto.DeathCertificateimage == null) ModelState.Remove("DeathCertificateimage");

            if (!ModelState.IsValid)
            {
                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = medicalandlabform.Validfrontimage;
                ViewData["ValidBackimage"] = medicalandlabform.ValidBackimage;
                ViewData["DoctorPrescription"] = medicalandlabform.DoctorPrescription;
                ViewData["DeathCertificate"] = medicalandlabform.DeathCertificate;

                return View(medicalandlabformdto);
            }

            try
            {
                // Update text properties
                medicalandlabform.Lastname = medicalandlabformdto.Lastname ?? medicalandlabform.Lastname;
                medicalandlabform.Firstname = medicalandlabformdto.Firstname ?? medicalandlabform.Firstname;
                medicalandlabform.Middlename = medicalandlabformdto.Middlename ?? medicalandlabform.Middlename;
                medicalandlabform.Suffix = medicalandlabformdto.Suffix ?? medicalandlabform.Suffix;
                medicalandlabform.BlkLotStreet = medicalandlabformdto.BlkLotStreet ?? medicalandlabform.BlkLotStreet;
                medicalandlabform.SubVill = medicalandlabformdto.SubVill ?? medicalandlabform.SubVill;
                medicalandlabform.Brgy = medicalandlabformdto.Brgy ?? medicalandlabform.Brgy;
                medicalandlabform.District = medicalandlabformdto.District ?? medicalandlabform.District;
                medicalandlabform.Sex = medicalandlabformdto.Sex ?? medicalandlabform.Sex;
                medicalandlabform.PhilHealth = medicalandlabformdto.PhilHealth ?? medicalandlabform.PhilHealth;
                medicalandlabform.PhilHealthNo = medicalandlabformdto.PhilHealthNo;
                medicalandlabform.Dateofbirth = medicalandlabformdto.Dateofbirth ?? medicalandlabform.Dateofbirth;
                medicalandlabform.Age = medicalandlabformdto.Age ?? medicalandlabform.Age;

                // Requestor Details
                medicalandlabform.RLastname = medicalandlabformdto.RLastname;
                medicalandlabform.RFirstname = medicalandlabformdto.RFirstname;
                medicalandlabform.RMiddlename = medicalandlabformdto.RMiddlename;
                medicalandlabform.RSuffix = medicalandlabformdto.RSuffix;
                medicalandlabform.RBlkLotStreet = medicalandlabformdto.RBlkLotStreet;
                medicalandlabform.RSubVill = medicalandlabformdto.RSubVill;
                medicalandlabform.RBrgy = medicalandlabformdto.RBrgy;
                medicalandlabform.RDistrict = medicalandlabformdto.RDistrict;
                medicalandlabform.RelationshipPatient = medicalandlabformdto.RelationshipPatient;
                medicalandlabform.ContactNo = medicalandlabformdto.ContactNo;

                // Assistance Type
                medicalandlabform.Typeassistance = medicalandlabformdto.Typeassistance ?? medicalandlabform.Typeassistance;
                medicalandlabform.ForCMOPERSONNEL = medicalandlabformdto.ForCMOPERSONNEL;

                // Handle ID Front image
                if (medicalandlabformdto.IdFrontimage != null)
                {
                    string newFileNameFront = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(medicalandlabformdto.IdFrontimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameFront);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(medicalandlabform.Validfrontimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, medicalandlabform.Validfrontimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        medicalandlabformdto.IdFrontimage.CopyTo(stream);
                    }

                    medicalandlabform.Validfrontimage = newFileNameFront;
                }

                // Handle ID Back image
                if (medicalandlabformdto.IdBackimage != null)
                {
                    string newFileNameBack = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(medicalandlabformdto.IdBackimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameBack);

                    if (!string.IsNullOrEmpty(medicalandlabform.ValidBackimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, medicalandlabform.ValidBackimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        medicalandlabformdto.IdBackimage.CopyTo(stream);
                    }

                    medicalandlabform.ValidBackimage = newFileNameBack;
                }

                // Handle Doctor Prescription image
                if (medicalandlabformdto.DoctorPrescriptionimage != null)
                {
                    string newFileNamePrescription = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(medicalandlabformdto.DoctorPrescriptionimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    string filePath = Path.Combine(uploadsFolder, newFileNamePrescription);

                    if (!string.IsNullOrEmpty(medicalandlabform.DoctorPrescription))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, medicalandlabform.DoctorPrescription);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        medicalandlabformdto.DoctorPrescriptionimage.CopyTo(stream);
                    }

                    medicalandlabform.DoctorPrescription = newFileNamePrescription;
                }

                // Handle Death Certificate image
                if (medicalandlabformdto.DeathCertificateimage != null)
                {
                    string newFileNameDeathCertificate = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(medicalandlabformdto.DeathCertificateimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameDeathCertificate);

                    if (!string.IsNullOrEmpty(medicalandlabform.DeathCertificate))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, medicalandlabform.DeathCertificate);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        medicalandlabformdto.DeathCertificateimage.CopyTo(stream);
                    }

                    medicalandlabform.DeathCertificate = newFileNameDeathCertificate;
                }

                context.SaveChanges();
                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);

                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = medicalandlabform.Validfrontimage;
                ViewData["ValidBackimage"] = medicalandlabform.ValidBackimage;
                ViewData["DoctorPrescription"] = medicalandlabform.DoctorPrescription;
                ViewData["DeathCertificate"] = medicalandlabform.DeathCertificate;

                return View(medicalandlabformdto);
            }
        }

        public IActionResult Medicalandlabformedelete(int id)
        {
            var medicalandlabform = context.Medicalandlabform.Find(id);
            if (medicalandlabform == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Instead of deleting files and record, just update the status
            medicalandlabform.Status = "Removed";
            context.Medicalandlabform.Update(medicalandlabform);
            context.SaveChanges();

            return RedirectToAction("Homepage", "Dashboard");
        }

        public IActionResult Funeralburialform()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }
            return View();
        }



        [HttpPost]
        public IActionResult Funeralburialform(FuneralburialformDto funeralburialformdto)
        {
            if (string.IsNullOrEmpty(funeralburialformdto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }
            // Get the current user's ID from the session
            if (!int.TryParse(HttpContext.Session.GetString("UserId"), out int userId))
            {
                // If user is not logged in, redirect to login page
                return RedirectToAction("Login", "Login");
            }

            if (funeralburialformdto.IdBackimage == null && funeralburialformdto.IdFrontimage == null &&
              funeralburialformdto.DoctorPrescriptionimage == null && funeralburialformdto.DeathCertificateimage == null)
            {
                ModelState.AddModelError("ImageFile", "The image file is required");
            }

            if (!ModelState.IsValid)
            {
                return View(funeralburialformdto);
            }

            try
            {
                // Generate unique filenames
                string newFileNameFront = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                          Path.GetExtension(funeralburialformdto.IdFrontimage!.FileName);

                string newFileNameBack = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                       Path.GetExtension(funeralburialformdto.IdBackimage!.FileName);

                string newFileNamePrescription = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                                       Path.GetExtension(funeralburialformdto.DoctorPrescriptionimage!.FileName);

                string? newFileNameDeathCertificate = null;

                // Save image to wwwroot/UsersImg
                string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                string uploadsFolder1 = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                string uploadsFolder2 = Path.Combine(environment.WebRootPath, "Funeralimg");

                // Save Front Image
                string filePathFront = Path.Combine(uploadsFolder, newFileNameFront);
                using (var stream = new FileStream(filePathFront, FileMode.Create))
                {
                    funeralburialformdto.IdFrontimage.CopyTo(stream);
                }

                // Save Back Image
                string filePathBack = Path.Combine(uploadsFolder, newFileNameBack);
                using (var stream = new FileStream(filePathBack, FileMode.Create))
                {
                    funeralburialformdto.IdBackimage.CopyTo(stream);
                }

                // Save Prescription Image
                string filePathPrescription = Path.Combine(uploadsFolder1, newFileNamePrescription);
                using (var stream = new FileStream(filePathPrescription, FileMode.Create))
                {
                    funeralburialformdto.DoctorPrescriptionimage.CopyTo(stream);
                }

                if (funeralburialformdto.DeathCertificateimage != null)
                {
                    newFileNameDeathCertificate = DateTime.Now.ToString("yyyyMMddHHmmssfff") +
                        Path.GetExtension(funeralburialformdto.DeathCertificateimage.FileName);

                    string filePathDeathCertificate = Path.Combine(uploadsFolder2, newFileNameDeathCertificate);
                    using (var stream = new FileStream(filePathDeathCertificate, FileMode.Create))
                    {
                        funeralburialformdto.DeathCertificateimage.CopyTo(stream);
                    }
                }
                // Map data to entity
                Funeralburialform funeralburialform = new Funeralburialform()
                {
                    UserId = userId,
                    // Patient Details
                    Lastname = funeralburialformdto.Lastname,
                    Firstname = funeralburialformdto.Firstname,
                    Middlename = funeralburialformdto.Middlename,
                    Suffix = funeralburialformdto.Suffix,
                    BlkLotStreet = funeralburialformdto.BlkLotStreet,
                    SubVill = funeralburialformdto.SubVill,
                    Brgy = funeralburialformdto.Brgy,
                    District = funeralburialformdto.District,
                    Sex = funeralburialformdto.Sex,
                    PhilHealth = funeralburialformdto.PhilHealth,
                    PhilHealthNo = funeralburialformdto.PhilHealthNo,
                    Dateofbirth = funeralburialformdto.Dateofbirth,
                    Age = funeralburialformdto.Age,

                    // Requestor Details
                    RLastname = funeralburialformdto.RLastname,
                    RFirstname = funeralburialformdto.RFirstname,
                    RMiddlename = funeralburialformdto.RMiddlename,
                    RSuffix = funeralburialformdto.RSuffix,
                    RBlkLotStreet = funeralburialformdto.RBlkLotStreet,
                    RSubVill = funeralburialformdto.RSubVill,
                    RDistrict = funeralburialformdto.RDistrict,
                    RBrgy = funeralburialformdto.RBrgy,
                    RelationshipPatient = funeralburialformdto.RelationshipPatient,
                    ContactNo = funeralburialformdto.ContactNo,

                    // Assistance Type
                    Typeassistance = funeralburialformdto.Typeassistance,
                    ForCMOPERSONNEL = funeralburialformdto.ForCMOPERSONNEL,

                    // Image Paths
                    Validfrontimage = newFileNameFront,
                    ValidBackimage = newFileNameBack,
                    DoctorPrescription = newFileNamePrescription,
                    DeathCertificate = newFileNameDeathCertificate ?? string.Empty,
                    Status = "Pending",


                    // Created Timestamp
                    CreatedAt = DateTime.Now
                };

                context.Funeralburialform.Add(funeralburialform);
                context.SaveChanges();

                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (DbUpdateException ex)
            {
                // Exception handling remains the same
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

                    return View(funeralburialformdto);
                }

                ModelState.AddModelError("", "A database error occurred while saving your data.");
                return View(funeralburialformdto);
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(funeralburialformdto);
            }
        }

        public IActionResult Funeralburialformedit(int id)
        {
            var funeralburialform = context.Funeralburialform.Find(id);


            if (funeralburialform == null)
            {
                return NotFound();
            }

            // Add all form field values to ViewData
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


            return View();

        }

        [HttpPost]
        public IActionResult Funeralburialformedit(int id, FuneralburialformDto funeralburialformdto)
        {
            var funeralburialform = context.Funeralburialform.Find(id);

            if (funeralburialform == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            if (string.IsNullOrEmpty(funeralburialformdto.PhilHealthNo))
            {
                ModelState.Remove("PhilHealthNo");
            }

            // Remove validation requirements for images if they're not provided
            if (funeralburialformdto.IdFrontimage == null) ModelState.Remove("IdFrontimage");
            if (funeralburialformdto.IdBackimage == null) ModelState.Remove("IdBackimage");
            if (funeralburialformdto.DoctorPrescriptionimage == null) ModelState.Remove("DoctorPrescriptionimage");
            if (funeralburialformdto.DeathCertificateimage == null) ModelState.Remove("DeathCertificateimage");

            if (!ModelState.IsValid)
            {
                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
                ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;
                ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;
                ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

                return View(funeralburialformdto);
            }

            try
            {
                // Update text properties
                funeralburialform.Lastname = funeralburialformdto.Lastname ?? funeralburialform.Lastname;
                funeralburialform.Firstname = funeralburialformdto.Firstname ?? funeralburialform.Firstname;
                funeralburialform.Middlename = funeralburialformdto.Middlename ?? funeralburialform.Middlename;
                funeralburialform.Suffix = funeralburialformdto.Suffix ?? funeralburialform.Suffix;
                funeralburialform.BlkLotStreet = funeralburialformdto.BlkLotStreet ?? funeralburialform.BlkLotStreet;
                funeralburialform.SubVill = funeralburialformdto.SubVill ?? funeralburialform.SubVill;
                funeralburialform.Brgy = funeralburialformdto.Brgy ?? funeralburialform.Brgy;
                funeralburialform.District = funeralburialformdto.District ?? funeralburialform.District;
                funeralburialform.Sex = funeralburialformdto.Sex ?? funeralburialform.Sex;
                funeralburialform.PhilHealth = funeralburialformdto.PhilHealth ?? funeralburialform.PhilHealth;
                funeralburialform.PhilHealthNo = funeralburialformdto.PhilHealthNo;
                funeralburialform.Dateofbirth = funeralburialformdto.Dateofbirth ?? funeralburialform.Dateofbirth;
                funeralburialform.Age = funeralburialformdto.Age ?? funeralburialform.Age;

                // Requestor Details
                funeralburialform.RLastname = funeralburialformdto.RLastname;
                funeralburialform.RFirstname = funeralburialformdto.RFirstname;
                funeralburialform.RMiddlename = funeralburialformdto.RMiddlename;
                funeralburialform.RSuffix = funeralburialformdto.RSuffix;
                funeralburialform.RBlkLotStreet = funeralburialformdto.RBlkLotStreet;
                funeralburialform.RSubVill = funeralburialformdto.RSubVill;
                funeralburialform.RBrgy = funeralburialformdto.RBrgy;
                funeralburialform.RDistrict = funeralburialformdto.RDistrict;
                funeralburialform.RelationshipPatient = funeralburialformdto.RelationshipPatient;
                funeralburialform.ContactNo = funeralburialformdto.ContactNo;

                // Assistance Type
                funeralburialform.Typeassistance = funeralburialformdto.Typeassistance ?? funeralburialform.Typeassistance;
                funeralburialform.ForCMOPERSONNEL = funeralburialformdto.ForCMOPERSONNEL;

                // Handle ID Front image
                if (funeralburialformdto.IdFrontimage != null)
                {
                    string newFileNameFront = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(funeralburialformdto.IdFrontimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameFront);

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(funeralburialform.Validfrontimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, funeralburialform.Validfrontimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        funeralburialformdto.IdFrontimage.CopyTo(stream);
                    }

                    funeralburialform.Validfrontimage = newFileNameFront;
                }

                // Handle ID Back image
                if (funeralburialformdto.IdBackimage != null)
                {
                    string newFileNameBack = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(funeralburialformdto.IdBackimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Validimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameBack);

                    if (!string.IsNullOrEmpty(funeralburialform.ValidBackimage))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, funeralburialform.ValidBackimage);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        funeralburialformdto.IdBackimage.CopyTo(stream);
                    }

                    funeralburialform.ValidBackimage = newFileNameBack;
                }

                // Handle Doctor Prescription image
                if (funeralburialformdto.DoctorPrescriptionimage != null)
                {
                    string newFileNamePrescription = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(funeralburialformdto.DoctorPrescriptionimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "DoctorPrescriptionimage");
                    string filePath = Path.Combine(uploadsFolder, newFileNamePrescription);

                    if (!string.IsNullOrEmpty(funeralburialform.DoctorPrescription))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, funeralburialform.DoctorPrescription);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        funeralburialformdto.DoctorPrescriptionimage.CopyTo(stream);
                    }

                    funeralburialform.DoctorPrescription = newFileNamePrescription;
                }

                // Handle Death Certificate image
                if (funeralburialformdto.DeathCertificateimage != null)
                {
                    string newFileNameDeathCertificate = DateTime.Now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(funeralburialformdto.DeathCertificateimage.FileName);
                    string uploadsFolder = Path.Combine(environment.WebRootPath, "Funeralimg");
                    string filePath = Path.Combine(uploadsFolder, newFileNameDeathCertificate);

                    if (!string.IsNullOrEmpty(funeralburialform.DeathCertificate))
                    {
                        string oldFilePath = Path.Combine(uploadsFolder, funeralburialform.DeathCertificate);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        funeralburialformdto.DeathCertificateimage.CopyTo(stream);
                    }

                    funeralburialform.DeathCertificate = newFileNameDeathCertificate;
                }

                context.SaveChanges();
                return RedirectToAction("Homepage", "Dashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);

                // Populate ViewData with current image paths
                ViewData["Validfrontimage"] = funeralburialform.Validfrontimage;
                ViewData["ValidBackimage"] = funeralburialform.ValidBackimage;
                ViewData["DoctorPrescription"] = funeralburialform.DoctorPrescription;
                ViewData["DeathCertificate"] = funeralburialform.DeathCertificate;

                return View(funeralburialformdto );
            }
        }


        public IActionResult Funeralburialformedelete(int id)
        {
            var Funeralburialform = context.Funeralburialform.Find(id);
            if (Funeralburialform == null)
            {
                return RedirectToAction("Homepage", "Dashboard");
            }

            // Instead of deleting files and record, just update the status
            Funeralburialform.Status = "Removed";
            context.Funeralburialform.Update(Funeralburialform);
            context.SaveChanges();

            return RedirectToAction("Homepage", "Dashboard");
        }





        public IActionResult Uploads()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
            {
                // If the UserId is not in session or invalid, redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Get data from database filtered by userId
            var hospitalBills = context.FillupformHospitalBill
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.Medicalandlabform
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var funeralburialform = context.Funeralburialform
             .Where(f => f.UserId == userId)
             .OrderByDescending(f => f.CreatedAt)
             .ToList();

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalBills = hospitalBills,
                MedicalLabForms = medicalLabForms,
                Funeralburialform = funeralburialform //
            };

            // Pass the view model to the view
            return View(viewModel);

        }

        public IActionResult Maps()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
            {
                // If the UserId is not in session or invalid, redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Get data from database filtered by userId
            var hospitalBills = context.FillupformHospitalBill
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.Medicalandlabform
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalBills = hospitalBills,
                MedicalLabForms = medicalLabForms
            };

            // Pass the view model to the view
            return View();
        }

        public IActionResult Eligibilitychecking()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            var userIdString = HttpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdString, out int userId))
            {
                // If the UserId is not in session or invalid, redirect to login
                return RedirectToAction("Login", "Account");
            }

            // Get data from database filtered by userId
            var hospitalBills = context.FillupformHospitalBill
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var medicalLabForms = context.Medicalandlabform
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            var funeralburialform = context.Funeralburialform
             .Where(f => f.UserId == userId)
             .OrderByDescending(f => f.CreatedAt)
             .ToList();

            // Create and populate the view model
            var viewModel = new CombinedFormsViewModel
            {
                HospitalBills = hospitalBills,
                MedicalLabForms = medicalLabForms,
                Funeralburialform = funeralburialform //
            };

            // Pass the view model to the view
            return View(viewModel);
        }


        public IActionResult Fillupformhospitalbillview(int id)
        {

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
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
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
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
            ViewData["Processby"] = funeralburialform.Processby;
            return View();
        }

        public IActionResult Medicalandlabformview(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return RedirectToAction("Landingpage", "Dashboard");
            }

            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
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
            ViewData["Processby"] = medicalandlabform.Processby;
            return View();
        }

    }
}
