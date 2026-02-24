using iText.Commons.Actions.Data;
using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace LingapDVO.Controllers
{
    public class SuperadminController : Controller
    {
        public readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment environment;
        private readonly ISessionConfigurationService _sessionConfig;
        private readonly IAesEncryptionService _aesEncryptionService;
        private readonly IConfiguration _configuration;

        public SuperadminController(ApplicationDbContext context, IWebHostEnvironment environment, ISessionConfigurationService sessionConfig, IAesEncryptionService aesEncryptionService, IConfiguration configuration)
        {
            this.context = context;
            this.environment = environment;
            _sessionConfig = sessionConfig;
            _aesEncryptionService = aesEncryptionService;
            _configuration = configuration;
        }

        private class AesEncryptionHelper
        {
            private readonly byte[] _aesKey;

            public AesEncryptionHelper(IConfiguration configuration)
            {
                string keyHex = configuration["Security:AesEncryption:Key"]
                    ?? throw new InvalidOperationException("AES encryption key not found in configuration");

                keyHex = keyHex.Trim().Replace(" ", "").Replace("-", "").Replace(":", "");

                _aesKey = SafeConvertHexStringToByteArray(keyHex);

                if (_aesKey.Length != 32)
                    throw new InvalidOperationException($"AES key must be 32 bytes. Current: {_aesKey.Length}");
            }

            private static byte[] SafeConvertHexStringToByteArray(string hex)
            {
                hex = hex.Trim().Replace(" ", "").Replace("-", "").Replace(":", "");
                if (hex.Length % 2 != 0) hex = "0" + hex;
                byte[] bytes = new byte[hex.Length / 2];
                for (int i = 0; i < hex.Length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                return bytes;
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

            public string EncryptFilename(string originalFileName)
            {
                using var aes = Aes.Create();
                aes.Key = _aesKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var memoryStream = new MemoryStream();
                memoryStream.Write(aes.IV, 0, aes.IV.Length);

                byte[] inputBytes = Encoding.UTF8.GetBytes(originalFileName);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                memoryStream.Write(encryptedBytes, 0, encryptedBytes.Length);

                string base64 = Convert.ToBase64String(memoryStream.ToArray());
                return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
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

            public string DecryptFilename(string encryptedFileName)
            {
                string base64 = encryptedFileName.Replace("-", "+").Replace("_", "/");
                int padding = (4 - (base64.Length % 4)) % 4;
                base64 += new string('=', padding);
                byte[] encryptedData = Convert.FromBase64String(base64);

                using var aes = Aes.Create();
                aes.Key = _aesKey;
                byte[] iv = new byte[16];
                Array.Copy(encryptedData, 0, iv, 0, 16);
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] cipherText = new byte[encryptedData.Length - 16];
                Array.Copy(encryptedData, 16, cipherText, 0, cipherText.Length);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        // DECRYPTION HELPER FOR TEXT FIELDS
        private string DecryptFieldText(string? encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return "N/A";
            try
            {
                return _aesEncryptionService.Decrypt(encryptedText);
            }
            catch
            {
                return "Error decrypting data";
            }
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

            var UserAccount = context.UserAccount
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
                UserAccount = UserAccount,
                Adminaccount = Admin
            };

            // Pass the view model to the view
            return View(viewModel);
        }

        public IActionResult HospitalAssistanceview(int id)
        {
            if (HttpContext.Session.GetString("IsSuperadmin") != "true" && string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
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
            ViewData["RelationshipPatient"] = HospitalAssistance.RelationshipPatient;
            ViewData["ContactNo"] = HospitalAssistance.ContactNo;

            // Type of assistance
            ViewData["Typeassistance"] = HospitalAssistance.Typeassistance;
            var typeAssistanceRaw = HospitalAssistance.Typeassistance ?? "";
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            ViewData["ForCMOPERSONNEL"] = HospitalAssistance.ForCMOPERSONNEL;
            var cmoPersonnelRaw = HospitalAssistance.ForCMOPERSONNEL ?? "";
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // Tracking and status fields
            ViewData["CreatedAt"] = HospitalAssistance.CreatedAt;
            ViewData["ProcessAt"] = HospitalAssistance.ProcessAt;
            ViewData["Result"] = HospitalAssistance.Result;
            ViewData["ClaimedAt"] = HospitalAssistance.ClaimedAt;
            ViewData["Status2"] = HospitalAssistance.Status2;
            ViewData["Status3"] = HospitalAssistance.Status3;
            ViewData["RetakeReason"] = HospitalAssistance.RetakeReason;
            ViewData["Comments"] = HospitalAssistance.Comments;
            ViewData["Processby"] = HospitalAssistance.Processby;

            // Decrypt text fields for detail view
            ViewData["HospitalFacilityName"] = DecryptFieldText(HospitalAssistance.HospitalFacilityName);
            ViewData["HospitalFacilityAddress"] = DecryptFieldText(HospitalAssistance.HospitalFacilityAddress);
            ViewData["DiagnosisMedicalCondition"] = DecryptFieldText(HospitalAssistance.DiagnosisMedicalCondition);
            ViewData["HospitalBillCost"] = DecryptFieldText(HospitalAssistance.HospitalBillCost);
            ViewData["AdmissionDate"] = DecryptFieldText(HospitalAssistance.AdmissionDate);
            ViewData["DischargeDate"] = DecryptFieldText(HospitalAssistance.DischargeDate);
            ViewData["WardRoomType"] = DecryptFieldText(HospitalAssistance.WardRoomType);

            // Pass encrypted values for the "Encrypted" display mode
            ViewData["HospitalFacilityNameEncrypted"] = HospitalAssistance.HospitalFacilityName;
            ViewData["HospitalFacilityAddressEncrypted"] = HospitalAssistance.HospitalFacilityAddress;
            ViewData["DiagnosisMedicalConditionEncrypted"] = HospitalAssistance.DiagnosisMedicalCondition;
            ViewData["HospitalBillCostEncrypted"] = HospitalAssistance.HospitalBillCost;
            ViewData["AdmissionDateEncrypted"] = HospitalAssistance.AdmissionDate;
            ViewData["DischargeDateEncrypted"] = HospitalAssistance.DischargeDate;
            ViewData["WardRoomTypeEncrypted"] = HospitalAssistance.WardRoomType;

            // File Handling - Decrypt for display
            string validFolder = Path.Combine(environment.WebRootPath, "Validimg");
            string hospitalFolder = Path.Combine(environment.WebRootPath, "HospitalAssistanceFileStorage");

            try {
                // Documents (Doctor Prescription / Death Certificate)
                if (!string.IsNullOrEmpty(HospitalAssistance.DoctorPrescription)) {
                    string path = Path.Combine(hospitalFolder, HospitalAssistance.DoctorPrescription);
                    if (System.IO.File.Exists(path)) {
                        byte[] decrypted = DecryptFile(path);
                        ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decrypted);
                        ViewData["IsDoctorPrescriptionPdf"] = IsPdfFile(decrypted);
                        ViewData["DoctorPrescription"] = HospitalAssistance.DoctorPrescription;
                    }
                }
                if (!string.IsNullOrEmpty(HospitalAssistance.DeathCertificate)) {
                    string path = Path.Combine(hospitalFolder, HospitalAssistance.DeathCertificate);
                    if (System.IO.File.Exists(path)) {
                        byte[] decrypted = DecryptFile(path);
                        ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decrypted);
                        ViewData["IsDeathCertificatePdf"] = IsPdfFile(decrypted);
                        ViewData["DeathCertificate"] = HospitalAssistance.DeathCertificate;
                    }
                }
                
                // Note: IDs are usually already handled via direct filename or session, 
                // but we'll pass the filenames for the toggle logic in the view
                ViewData["Validfrontimage"] = HospitalAssistance.Validfrontimage;
                ViewData["ValidBackimage"] = HospitalAssistance.ValidBackimage;
            } catch { }

            return View("Fillupformhospitalbillview");
        }

        // Helper for file decryption
        private byte[] DecryptFile(string encryptedFilePath)
        {
            byte[] encryptedData = System.IO.File.ReadAllBytes(encryptedFilePath);
            using var memoryStream = new MemoryStream(encryptedData);
            byte[] iv = new byte[16];
            memoryStream.Read(iv, 0, iv.Length);

            var aesHelper = new AesEncryptionHelper(_configuration);
            var keyField = typeof(AesEncryptionHelper).GetField("_aesKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            byte[]? key = keyField?.GetValue(aesHelper) as byte[];

            using var aes = Aes.Create();
            aes.Key = key!;
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

        private bool IsPdfFile(byte[] data)
        {
            if (data == null || data.Length < 4) return false;
            return data[0] == 0x25 && data[1] == 0x50 && data[2] == 0x44 && data[3] == 0x46;
        }

        public IActionResult FuneralAssistanceview(int id)
        {
            if (HttpContext.Session.GetString("IsSuperadmin") != "true" && string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Superadmin", "Superadmin");
            }

            var FuneralAssistance = context.FuneralAssistance.Find(id);
    
            if (FuneralAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] = FuneralAssistance.Status;
            ViewData["Id"] = FuneralAssistance.Id;
            ViewData["Lastname"] = FuneralAssistance.Lastname;
            ViewData["Firstname"] = FuneralAssistance.Firstname;
            ViewData["Middlename"] = FuneralAssistance.Middlename;
            ViewData["Suffix"] = FuneralAssistance.Suffix;
            ViewData["BlkLotStreet"] = FuneralAssistance.BlkLotStreet;
            ViewData["SubVill"] = FuneralAssistance.SubVill;
            ViewData["Brgy"] = FuneralAssistance.Brgy;
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
            ViewData["ContactNo"] = FuneralAssistance.ContactNo;

            // Type of assistance
            ViewData["Typeassistance"] = FuneralAssistance.Typeassistance;
            var typeAssistanceRaw = FuneralAssistance.Typeassistance ?? "";
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            ViewData["ForCMOPERSONNEL"] = FuneralAssistance.ForCMOPERSONNEL;
            var cmoPersonnelRaw = FuneralAssistance.ForCMOPERSONNEL ?? "";
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // Tracking and status fields
            ViewData["CreatedAt"] = FuneralAssistance.CreatedAt;
            ViewData["ProcessAt"] = FuneralAssistance.ProcessAt;
            ViewData["Result"] = FuneralAssistance.Result;
            ViewData["ClaimedAt"] = FuneralAssistance.ClaimedAt;
            ViewData["Status2"] = FuneralAssistance.Status2;
            ViewData["Status3"] = FuneralAssistance.Status3;
            ViewData["RetakeReason"] = FuneralAssistance.RetakeReason;
            ViewData["Comments"] = FuneralAssistance.Comments;
            ViewData["Processby"] = FuneralAssistance.Processby;

            // Decrypt text fields for detail view
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

            // Pass encrypted values for the "Encrypted" display mode
            ViewData["DeceasedPersonNameEncrypted"] = FuneralAssistance.DeceasedPersonName;
            ViewData["RelationshipToDeceasedEncrypted"] = FuneralAssistance.RelationshipToDeceased;
            ViewData["DateOfDeathEncrypted"] = FuneralAssistance.DateOfDeath;
            ViewData["TimeOfDeathEncrypted"] = FuneralAssistance.TimeOfDeath;
            ViewData["CauseOfDeathEncrypted"] = FuneralAssistance.CauseOfDeath;
            ViewData["FuneralHomeNameEncrypted"] = FuneralAssistance.FuneralHomeName;
            ViewData["FuneralHomeAddressEncrypted"] = FuneralAssistance.FuneralHomeAddress;
            ViewData["BurialCremationDateEncrypted"] = FuneralAssistance.BurialCremationDate;
            ViewData["BurialCremationTimeEncrypted"] = FuneralAssistance.BurialCremationTime;
            ViewData["BurialCremationTypeEncrypted"] = FuneralAssistance.BurialCremationType;

            // File Handling
            string hospitalFolder = Path.Combine(environment.WebRootPath, "FuneralAssistanceFileStorage");
            try {
                if (!string.IsNullOrEmpty(FuneralAssistance.DoctorPrescription)) {
                    string path = Path.Combine(hospitalFolder, FuneralAssistance.DoctorPrescription);
                    if (System.IO.File.Exists(path)) {
                        byte[] decrypted = DecryptFile(path);
                        ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decrypted);
                        ViewData["IsDoctorPrescriptionPdf"] = IsPdfFile(decrypted);
                    }
                }
                if (!string.IsNullOrEmpty(FuneralAssistance.DeathCertificate)) {
                    string path = Path.Combine(hospitalFolder, FuneralAssistance.DeathCertificate);
                    if (System.IO.File.Exists(path)) {
                        byte[] decrypted = DecryptFile(path);
                        ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decrypted);
                        ViewData["IsDeathCertificatePdf"] = IsPdfFile(decrypted);
                    }
                }
                ViewData["Validfrontimage"] = FuneralAssistance.Validfrontimage;
                ViewData["ValidBackimage"] = FuneralAssistance.ValidBackimage;
            } catch { }

            return View("Funeralburialformview");
        }

        public IActionResult OtherAssistanceview(int id)
        {
            if (HttpContext.Session.GetString("IsSuperadmin") != "true" && string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return RedirectToAction("Superadmin", "Superadmin");
            }
            var    OtherAssistance = context.OtherAssistance.Find(id);

            if (   OtherAssistance == null)
            {
                return NotFound();
            }

            // Basic ViewData setup
            ViewData["Status"] =    OtherAssistance.Status;
            ViewData["Id"] =    OtherAssistance.Id;
            ViewData["Lastname"] =    OtherAssistance.Lastname;
            ViewData["Firstname"] =    OtherAssistance.Firstname;
            ViewData["Middlename"] =    OtherAssistance.Middlename;
            ViewData["Suffix"] =    OtherAssistance.Suffix;
            ViewData["BlkLotStreet"] =    OtherAssistance.BlkLotStreet;
            ViewData["SubVill"] =    OtherAssistance.SubVill;
            ViewData["Brgy"] =    OtherAssistance.Brgy;
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
            ViewData["RelationshipPatient"] =    OtherAssistance.RelationshipPatient;
            ViewData["ContactNo"] =    OtherAssistance.ContactNo;

            // Type of assistance
            ViewData["Typeassistance"] = OtherAssistance.Typeassistance;
            var typeAssistanceRaw = OtherAssistance.Typeassistance ?? "";
            var parsed = typeAssistanceRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedAssistance"] = parsed;

            // CMO Personnel
            ViewData["ForCMOPERSONNEL"] = OtherAssistance.ForCMOPERSONNEL;
            var cmoPersonnelRaw = OtherAssistance.ForCMOPERSONNEL ?? "";
            var parsedCMO = cmoPersonnelRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(':', 2))
                .ToDictionary(x => x[0].Trim(), x => x.Length > 1 ? x[1].Trim() : "");
            ViewData["CheckedCMOPERSONNEL"] = parsedCMO;

            // Tracking and status fields
            ViewData["CreatedAt"] = OtherAssistance.CreatedAt;
            ViewData["ProcessAt"] = OtherAssistance.ProcessAt;
            ViewData["Result"] = OtherAssistance.Result;
            ViewData["ClaimedAt"] = OtherAssistance.ClaimedAt;
            ViewData["Status2"] = OtherAssistance.Status2;
            ViewData["Status3"] = OtherAssistance.Status3;
            ViewData["RetakeReason"] = OtherAssistance.RetakeReason;
            ViewData["Comments"] = OtherAssistance.Comments;
            ViewData["Processby"] = OtherAssistance.Processby;

            // Decrypt fields
            ViewData["MedicineName"] = DecryptFieldText(OtherAssistance.MedicineName);
            ViewData["MedicineQuantity"] = DecryptFieldText(OtherAssistance.MedicineQuantity);
            ViewData["MedicineCost"] = DecryptFieldText(OtherAssistance.MedicineCost);
            ViewData["PrescribingDoctor"] = DecryptFieldText(OtherAssistance.PrescribingDoctor);
            ViewData["DoctorContactDetail"] = DecryptFieldText(OtherAssistance.DoctorContactDetail);

            ViewData["LaboratoryCenterName"] = DecryptFieldText(OtherAssistance.LaboratoryCenterName);
            ViewData["LaboratoryCenterAddress"] = DecryptFieldText(OtherAssistance.LaboratoryCenterAddress);
            ViewData["TestName"] = DecryptFieldText(OtherAssistance.TestName);
            ViewData["TestCost"] = DecryptFieldText(OtherAssistance.TestCost);
            ViewData["TestOtherInfo"] = DecryptFieldText(OtherAssistance.TestOtherInfo);

            ViewData["TherapyFacilityName"] = DecryptFieldText(OtherAssistance.TherapyFacilityName);
            ViewData["TherapyFacilityAddress"] = DecryptFieldText(OtherAssistance.TherapyFacilityAddress);
            ViewData["TherapyFacilityContact"] = DecryptFieldText(OtherAssistance.TherapyFacilityContact);
            ViewData["TherapyType"] = DecryptFieldText(OtherAssistance.TherapyType);

            ViewData["EquipmentName"] = DecryptFieldText(OtherAssistance.EquipmentName);
            ViewData["EquipmentBrand"] = DecryptFieldText(OtherAssistance.EquipmentBrand);
            ViewData["EquipmentCategory"] = DecryptFieldText(OtherAssistance.EquipmentCategory);
            ViewData["EquipmentQuantity"] = DecryptFieldText(OtherAssistance.EquipmentQuantity);
            ViewData["EquipmentCost"] = DecryptFieldText(OtherAssistance.EquipmentCost);

            // Pass encrypted values for the "Encrypted" display mode
            ViewData["MedicineNameEncrypted"] = OtherAssistance.MedicineName;
            ViewData["MedicineQuantityEncrypted"] = OtherAssistance.MedicineQuantity;
            ViewData["MedicineCostEncrypted"] = OtherAssistance.MedicineCost;
            ViewData["PrescribingDoctorEncrypted"] = OtherAssistance.PrescribingDoctor;
            ViewData["DoctorContactDetailEncrypted"] = OtherAssistance.DoctorContactDetail;

            ViewData["LaboratoryCenterNameEncrypted"] = OtherAssistance.LaboratoryCenterName;
            ViewData["LaboratoryCenterAddressEncrypted"] = OtherAssistance.LaboratoryCenterAddress;
            ViewData["TestNameEncrypted"] = OtherAssistance.TestName;
            ViewData["TestCostEncrypted"] = OtherAssistance.TestCost;
            ViewData["TestOtherInfoEncrypted"] = OtherAssistance.TestOtherInfo;

            ViewData["TherapyFacilityNameEncrypted"] = OtherAssistance.TherapyFacilityName;
            ViewData["TherapyFacilityAddressEncrypted"] = OtherAssistance.TherapyFacilityAddress;
            ViewData["TherapyFacilityContactEncrypted"] = OtherAssistance.TherapyFacilityContact;
            ViewData["TherapyTypeEncrypted"] = OtherAssistance.TherapyType;

            ViewData["EquipmentNameEncrypted"] = OtherAssistance.EquipmentName;
            ViewData["EquipmentBrandEncrypted"] = OtherAssistance.EquipmentBrand;
            ViewData["EquipmentCategoryEncrypted"] = OtherAssistance.EquipmentCategory;
            ViewData["EquipmentQuantityEncrypted"] = OtherAssistance.EquipmentQuantity;
            ViewData["EquipmentCostEncrypted"] = OtherAssistance.EquipmentCost;

            // File Handling
            string hospitalFolder = Path.Combine(environment.WebRootPath, "OtherAssistanceFileStorage");
            try {
                if (!string.IsNullOrEmpty(OtherAssistance.DoctorPrescription)) {
                    string path = Path.Combine(hospitalFolder, OtherAssistance.DoctorPrescription);
                    if (System.IO.File.Exists(path)) {
                        byte[] decrypted = DecryptFile(path);
                        ViewData["DoctorPrescriptionBase64"] = Convert.ToBase64String(decrypted);
                        ViewData["IsDoctorPrescriptionPdf"] = IsPdfFile(decrypted);
                    }
                }
                if (!string.IsNullOrEmpty(OtherAssistance.MedCertificate)) {
                    string path = Path.Combine(hospitalFolder, OtherAssistance.MedCertificate);
                    if (System.IO.File.Exists(path)) {
                        byte[] decrypted = DecryptFile(path);
                        ViewData["MedicalCertificateBase64"] = Convert.ToBase64String(decrypted);
                        ViewData["IsMedicalCertificatePdf"] = IsPdfFile(decrypted);
                    }
                }
                if (!string.IsNullOrEmpty(OtherAssistance.DeathCertificate)) {
                    string path = Path.Combine(hospitalFolder, OtherAssistance.DeathCertificate);
                    if (System.IO.File.Exists(path)) {
                        byte[] decrypted = DecryptFile(path);
                        ViewData["DeathCertificateBase64"] = Convert.ToBase64String(decrypted);
                        ViewData["IsDeathCertificatePdf"] = IsPdfFile(decrypted);
                    }
                }
                ViewData["Validfrontimage"] = OtherAssistance.Validfrontimage;
                ViewData["ValidBackimage"] = OtherAssistance.ValidBackimage;
            } catch { }

            return View("Medicalandlabformview");
        }

        public IActionResult Choice()
        {                 
            return View(); 
        }

        // ========================================================
        // 🔒 SECURITY: PASSWORD VERIFICATION FOR DECRYPTION
        // ========================================================
        [HttpPost]
        public IActionResult VerifyPasswordForDecryption(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return Json(new { success = false, message = "Password is required" });
            }

            // Get superadmin details from session
            string? superadminUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(superadminUsername))
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            // Verify if the user is indeed a superadmin
            if (HttpContext.Session.GetString("IsSuperadmin") != "true")
            {
                // If not superadmin, check if they are a regular admin
                string? adminUsername = HttpContext.Session.GetString("Username");
                var admin = context.Adminaccount.FirstOrDefault(a => a.Username == adminUsername);
                if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.Password))
                {
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Unauthorized access" });
            }

            // Verify superadmin password
            var superadmin = context.Superadminaccount.FirstOrDefault(s => s.Username == superadminUsername);
            if (superadmin != null && BCrypt.Net.BCrypt.Verify(password, superadmin.Password))
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Incorrect password. Access denied." });
        }

        [HttpPost]
        public IActionResult DecryptField(string fieldName, string formType, int formId)
        {
            // Security Check: Ensure user is authorized
            if (HttpContext.Session.GetString("IsSuperadmin") != "true" && string.IsNullOrEmpty(HttpContext.Session.GetString("AdminFullname")))
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                string encryptedValue = "";

                if (formType == "Hospital")
                {
                    var form = context.HospitalAssistance.Find(formId);
                    if (form == null) return Json(new { success = false, message = "Form not found" });

                    encryptedValue = fieldName switch
                    {
                        "HospitalFacilityName" => form.HospitalFacilityName ?? "",
                        "HospitalFacilityAddress" => form.HospitalFacilityAddress ?? "",
                        "DiagnosisMedicalCondition" => form.DiagnosisMedicalCondition ?? "",
                        "HospitalBillCost" => form.HospitalBillCost ?? "",
                        "AdmissionDate" => form.AdmissionDate ?? "",
                        "DischargeDate" => form.DischargeDate ?? "",
                        "WardRoomType" => form.WardRoomType ?? "",
                        _ => ""
                    };
                }
                else if (formType == "Other")
                {
                    var form = context.OtherAssistance.Find(formId);
                    if (form == null) return Json(new { success = false, message = "Form not found" });

                    encryptedValue = fieldName switch
                    {
                        "MedicineName" => form.MedicineName ?? "",
                        "MedicineQuantity" => form.MedicineQuantity ?? "",
                        "MedicineCost" => form.MedicineCost ?? "",
                        "PrescribingDoctor" => form.PrescribingDoctor ?? "",
                        "DoctorContactDetail" => form.DoctorContactDetail ?? "",
                        "LaboratoryCenterName" => form.LaboratoryCenterName ?? "",
                        "LaboratoryCenterAddress" => form.LaboratoryCenterAddress ?? "",
                        "TestName" => form.TestName ?? "",
                        "TestCost" => form.TestCost ?? "",
                        "TestOtherInfo" => form.TestOtherInfo ?? "",
                        "TherapyFacilityName" => form.TherapyFacilityName ?? "",
                        "TherapyFacilityAddress" => form.TherapyFacilityAddress ?? "",
                        "TherapyFacilityContact" => form.TherapyFacilityContact ?? "",
                        "TherapyType" => form.TherapyType ?? "",
                        "EquipmentName" => form.EquipmentName ?? "",
                        "EquipmentBrand" => form.EquipmentBrand ?? "",
                        "EquipmentCategory" => form.EquipmentCategory ?? "",
                        "EquipmentQuantity" => form.EquipmentQuantity ?? "",
                        "EquipmentCost" => form.EquipmentCost ?? "",
                        _ => ""
                    };
                }
                else if (formType == "Funeral")
                {
                    var form = context.FuneralAssistance.Find(formId);
                    if (form == null) return Json(new { success = false, message = "Form not found" });

                    encryptedValue = fieldName switch
                    {
                        "DeceasedPersonName" => form.DeceasedPersonName ?? "",
                        "RelationshipToDeceased" => form.RelationshipToDeceased ?? "",
                        "DateOfDeath" => form.DateOfDeath ?? "",
                        "TimeOfDeath" => form.TimeOfDeath ?? "",
                        "CauseOfDeath" => form.CauseOfDeath ?? "",
                        "FuneralHomeName" => form.FuneralHomeName ?? "",
                        "FuneralHomeAddress" => form.FuneralHomeAddress ?? "",
                        "BurialCremationDate" => form.BurialCremationDate ?? "",
                        "BurialCremationTime" => form.BurialCremationTime ?? "",
                        "BurialCremationType" => form.BurialCremationType ?? "",
                        _ => ""
                    };
                }

                if (string.IsNullOrEmpty(encryptedValue))
                {
                    return Json(new { success = true, data = "N/A" });
                }

                string decryptedValue = DecryptFieldText(encryptedValue);
                return Json(new { success = true, data = decryptedValue });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DecryptImage(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    return Json(new { success = false, message = "Image path is required" });
                }

                // Get current user info - Superadmin context
                var isSuperadmin = HttpContext.Session.GetString("IsSuperadmin");

                if (isSuperadmin != "true")
                {
                    return Json(new { success = false, message = "Superadmin not authenticated" });
                }

                // Construct full file path
                var webRootPath = environment.WebRootPath;
                var fullPath = Path.Combine(webRootPath, imagePath.TrimStart('/').Replace("/", "\\"));

                // Check if file exists
                if (!System.IO.File.Exists(fullPath))
                {
                    return Json(new { success = false, message = "Image file not found" });
                }

                // Read encrypted image bytes
                byte[] encryptedBytes = System.IO.File.ReadAllBytes(fullPath);

                // Initialize AES encryption helper
                var encryptionHelper = new AesEncryptionHelper(_configuration);

                // Decrypt the image using helper directly
                // Note: encryptionHelper.DecryptBytes is missing in my previous implementation of AesEncryptionHelper in this controller.
                // I should add it or use the same logic.
                
                using var aes = Aes.Create();
                var keyField = typeof(AesEncryptionHelper).GetField("_aesKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                byte[]? key = keyField?.GetValue(encryptionHelper) as byte[];
                
                byte[] iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.Key = key!;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(encryptedBytes, 16, encryptedBytes.Length - 16);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var resultStream = new MemoryStream();
                cryptoStream.CopyTo(resultStream);
                byte[] decryptedBytes = resultStream.ToArray();

                // Convert to Base64 for frontend display
                string base64Image = Convert.ToBase64String(decryptedBytes);

                // Determine image type from file extension
                string extension = Path.GetExtension(fullPath).ToLower();
                string mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "image/jpeg"
                };

                return Json(new
                {
                    success = true,
                    imageData = $"data:{mimeType};base64,{base64Image}",
                    mimeType = mimeType
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Decryption failed: {ex.Message}" });
            }
        }

        private bool IsBase64String(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s.Length % 4 != 0 || s.Contains(" ") || s.Contains("\t") || s.Contains("\r") || s.Contains("\n"))
                return false;
            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
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
            catch (Exception)
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
            var user = context.UserAccount.Find(id);
            if (user == null)
            {
                return RedirectToAction("Superadmin");
            }

            // Instead of deleting files and record, just update the status
            user.Status = "Removed";
            context.UserAccount.Update(user);
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

