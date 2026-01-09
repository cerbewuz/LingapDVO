using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using LingapDVO.Models;
using LingapDVO.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace LingapDVO.Scripts
{
    public class DummyDataGenerator
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly Random _random;
        private readonly AesEncryptionHelper _aesHelper;

        // Returns status2 with weighted distribution: 15% Disapprove, 35% Approve, 50% Approve+Claim (indicated as "ApproveClaim")
        private string GetRandomStatus2()
        {
            int roll = _random.Next(100);
            if (roll < 15) return "Disapprove";
            if (roll < 15 + 35) return "Approve";
            return "ApproveClaim"; // caller should translate to Approve and mark as Claimed
        }

        private static readonly string[] FilipinoFirstNames = {
            "Jose", "Maria", "Juan", "Ana", "Pedro", "Rosa", "Carlos", "Luz", "Ramon", "Carmen",
            "Antonio", "Josefa", "Francisco", "Teresa", "Manuel", "Elena", "Miguel", "Sofia",
            "Gabriel", "Isabel", "Rafael", "Cristina", "Angel", "Patricia", "Luis", "Angela",
            "Fernando", "Gloria", "Roberto", "Victoria", "Ricardo", "Lourdes", "Eduardo", "Cecilia",
            "Rodrigo", "Remedios", "Salvador", "Rosario", "Andres", "Esperanza", "Mario", "Beatriz",
            "Alberto", "Pilar", "Enrique", "Consuelo", "Vicente", "Dolores", "Alfredo", "Mercedes"
        };

        private static readonly string[] FilipinoLastNames = {
            "Santos", "Reyes", "Cruz", "Bautista", "Garcia", "Mendoza", "Torres", "Flores",
            "Rivera", "Gonzales", "Ramos", "Aquino", "Villanueva", "Castillo", "De Leon",
            "Navarro", "Fernandez", "Soriano", "Mercado", "Santiago", "Aguilar", "Miranda",
            "Valdez", "Pascual", "Domingo", "Castro", "Evangelista", "Dela Cruz", "Lopez",
            "Diaz", "Manalo", "Tolentino", "Marquez", "Salazar", "Francisco", "Ortega",
            "Medina", "Velasco", "Hernandez", "Ocampo", "Gutierrez", "Morales", "Bonifacio"
        };

        private static readonly string[] BarangaysInDavao = {
            "Agdao", "Buhangin", "Bunawan", "Calinan", "Marilog", "Paquibato", "Poblacion",
            "Talomo", "Toril", "Tugbok", "Baguio", "Matina", "Sasa", "Leon Garcia",
            "Mintal", "Catalunan Grande", "Catalunan Pequeño", "Ma-a", "Ula", "Lanang"
        };

        public DummyDataGenerator(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _random = new Random();
            _aesHelper = new AesEncryptionHelper(configuration);
        }

        public async Task GenerateAllDummyDataAsync()
        {
            Console.WriteLine("Starting generation of 250 realistic Filipino dummy data entries...");

            for (int i = 0; i < 250; i++)
            {
                try
                {
                    // Create user account
                    var user = await CreateUserAccountAsync(i);

                    // Create verified account with encrypted IDs
                    var verifyAccount = await CreateVerifiedAccountAsync(user);

                    // Randomly assign assistance types (user can apply for one type)
                    int assistanceType = _random.Next(0, 3);

                    switch (assistanceType)
                    {
                        case 0:
                            await CreateHospitalAssistanceApplicationAsync(user, verifyAccount);
                            break;
                        case 1:
                            await CreateOtherAssistanceApplicationAsync(user, verifyAccount);
                            break;
                        case 2:
                            await CreateFuneralAssistanceApplicationAsync(user, verifyAccount);
                            break;
                    }

                    if ((i + 1) % 50 == 0)
                    {
                        Console.WriteLine($"Progress: {i + 1}/250 entries created...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating entry {i + 1}: {ex.Message}");
                }
            }

            Console.WriteLine("Dummy data generation completed!");
        }

        private async Task<RegisterAcc> CreateUserAccountAsync(int index)
        {
            string firstName = FilipinoFirstNames[_random.Next(FilipinoFirstNames.Length)];
            string lastName = FilipinoLastNames[_random.Next(FilipinoLastNames.Length)];
            string email = $"{firstName.ToLower()}.{lastName.ToLower()}{_random.Next(100, 999)}@example.com";
            string username = $"{firstName.ToLower()}{lastName.ToLower()}{_random.Next(100, 999)}";

            var user = new RegisterAcc
            {
                FirstName = firstName,
                MiddleName = FilipinoFirstNames[_random.Next(FilipinoFirstNames.Length)],
                LastName = lastName,
                Email = email,
                Username = username,
                Password = BCrypt.Net.BCrypt.HashPassword("DummyPass123!")
            };

            _context.RegisterAcc.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        private async Task<Verifyaccount> CreateVerifiedAccountAsync(RegisterAcc user)
        {
            // Generate realistic original filenames
            string validIdOriginal = $"ValidID_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.jpg";
            string backIdOriginal = $"BackID_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.jpg";

            // Encrypt filenames using real AES-256 encryption
            string encryptedValidId = _aesHelper.EncryptFilename(validIdOriginal) + ".enc";
            string encryptedBackId = _aesHelper.EncryptFilename(backIdOriginal) + ".enc";

            var verifyAccount = new Verifyaccount
            {
                UserId = user.Id,
                Firstname = user.FirstName,
                Middlename = user.MiddleName,
                Lastname = user.LastName,
                FrontID = TruncateToMaxLength(encryptedValidId, 100),
                BackID = TruncateToMaxLength(encryptedBackId, 100),
                Barangay = BarangaysInDavao[_random.Next(BarangaysInDavao.Length)],
                Phonenumber = $"09{_random.Next(100000000, 999999999)}",
                Dateofbirth = DateTime.Now.AddYears(-_random.Next(18, 65)).AddDays(-_random.Next(0, 365)).ToString("yyyy-MM-dd"),
                Gender = _random.Next(2) == 0 ? "Male" : "Female",
                CivilStatus = new[] { "Single", "Married", "Widowed", "Separated" }[_random.Next(4)],
                IDtype = new[] { "umid", "phil-id", "driver-license" }[_random.Next(3)],
                IDnumber = $"{_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}",
                BlkLotStreet = $"{_random.Next(1, 999)} Main St",
                SubVill = "Subdivision " + _random.Next(1, 20)
            };

            _context.Verifyaccount.Add(verifyAccount);
            await _context.SaveChangesAsync();

            return verifyAccount;
        }

        private async Task CreateHospitalAssistanceApplicationAsync(RegisterAcc user, Verifyaccount verifyAccount)
        {
            // Generate original document filenames
            string validIdOriginal = $"HospitalValidID_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string validBackOriginal = $"HospitalBackID_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string prescriptionOriginal = $"Prescription_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string deathCertOriginal = $"DeathCert_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";

            // Encrypt all filenames
            string encryptedValidId = _aesHelper.EncryptFilename(validIdOriginal) + ".enc";
            string encryptedValidBack = _aesHelper.EncryptFilename(validBackOriginal) + ".enc";
            string encryptedPrescription = _aesHelper.EncryptFilename(prescriptionOriginal) + ".enc";
            string encryptedDeathCert = _aesHelper.EncryptFilename(deathCertOriginal) + ".enc";

            // Determine status distribution: Disapprove 20%, otherwise Approve
            string status2 = GetRandomStatus2();

            // Translate ApproveClaim to Approve + Claimed; 100% of ApproveClaim are claimed per distribution requirement
            string status3 = "";
            if (status2 == "ApproveClaim")
            {
                status2 = "Approve";
                status3 = "Claimed";
            }

            // For plain Approve, 35% of those could be claimed based on original behavior (optional). Keep none to respect 50% total Claim proportion.
            int grantedAmount = status2 == "Approve" ? _random.Next(5000, 50000) : 0;

            var application = new HospitalAssistance
            {
                UserId = user.Id,
                Firstname = user.FirstName,
                Middlename = user.MiddleName,
                Lastname = user.LastName,
                Suffix = "",
                Validfrontimage = TruncateToMaxLength(encryptedValidId, 100),
                ValidBackimage = TruncateToMaxLength(encryptedValidBack, 100),
                DoctorPrescription = TruncateToMaxLength(encryptedPrescription, 100),
                DeathCertificate = TruncateToMaxLength(encryptedDeathCert, 100),
                Typeassistance = TruncateToMaxLength("Medical Bills, Medicines, Laboratory", 100),
                BlkLotStreet = verifyAccount.BlkLotStreet,
                SubVill = verifyAccount.SubVill,
                Brgy = verifyAccount.Barangay,
                Sex = verifyAccount.Gender,
                PhilHealth = _random.Next(2) == 0 ? "Yes" : "No",
                PhilHealthNo = _random.Next(2) == 0 ? $"12-{_random.Next(100000000, 999999999)}" : "",
                Dateofbirth = verifyAccount.Dateofbirth,
                Age = CalculateAge(verifyAccount.Dateofbirth).ToString(),
                RLastname = user.LastName,
                RFirstname = user.FirstName,
                RMiddlename = user.MiddleName,
                RSuffix = "",
                RBlkLotStreet = verifyAccount.BlkLotStreet,
                RSubVill = verifyAccount.SubVill,
                RBrgy = verifyAccount.Barangay,
                RelationshipPatient = "Self",
                Status = status2 == "Approve" || status2 == "Disapprove" || status2 == "Retake" ? "Processing" : "Pending",
                Status2 = status2,
                Status3 = status3,
                ForCMOPERSONNEL = status2 == "Approve" ? TruncateToMaxLength($"Amount:{grantedAmount} MedCert,HospBill", 100) : "",
                CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 60)),
                ProcessAt = status2 != "Pending" ? DateTime.Now.AddDays(-_random.Next(1, 30)) : DateTime.MinValue,
                Result = status2 == "Approve" || status2 == "Disapprove" ? DateTime.Now.AddDays(-_random.Next(1, 15)) : DateTime.MinValue,
                ClaimedAt = status3 == "Claimed" ? DateTime.Now.AddDays(-_random.Next(1, 7)) : DateTime.MinValue
            };

            _context.HospitalAssistance.Add(application);
            await _context.SaveChangesAsync();

            // Create notifications
            await CreateNotificationsForApplication(user.Id, "HospitalAssistance", status2, status3, application.Id, application.CreatedAt);
        }

        private async Task CreateOtherAssistanceApplicationAsync(RegisterAcc user, Verifyaccount verifyAccount)
        {
            // Generate original document filenames
            string validIdOriginal = $"OtherValidID_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string validBackOriginal = $"OtherBackID_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string medCertOriginal = $"MedCert_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string prescriptionOriginal = $"Prescription_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";

            // Encrypt all filenames
            string encryptedValidId = _aesHelper.EncryptFilename(validIdOriginal) + ".enc";
            string encryptedValidBack = _aesHelper.EncryptFilename(validBackOriginal) + ".enc";
            string encryptedMedCert = _aesHelper.EncryptFilename(medCertOriginal) + ".enc";
            string encryptedPrescription = _aesHelper.EncryptFilename(prescriptionOriginal) + ".enc";

            string status2 = GetRandomStatus2();

            string status3 = "";
            if (status2 == "ApproveClaim")
            {
                status2 = "Approve";
                status3 = "Claimed";
            }

            int grantedAmount = status2 == "Approve" ? _random.Next(3000, 30000) : 0;

            var application = new OtherAssistance
            {
                UserId = user.Id,
                Firstname = user.FirstName,
                Middlename = user.MiddleName,
                Lastname = user.LastName,
                Suffix = "",
                Validfrontimage = TruncateToMaxLength(encryptedValidId, 100),
                ValidBackimage = TruncateToMaxLength(encryptedValidBack, 100),
                MedCertificate = TruncateToMaxLength(encryptedMedCert, 100),
                DoctorPrescription = TruncateToMaxLength(encryptedPrescription, 100),
                DeathCertificate = "",
                Typeassistance = TruncateToMaxLength("Laboratory, Medicines, Medical Supplies", 100),
                BlkLotStreet = verifyAccount.BlkLotStreet,
                SubVill = verifyAccount.SubVill,
                Brgy = verifyAccount.Barangay,
                Sex = verifyAccount.Gender,
                PhilHealth = _random.Next(2) == 0 ? "Yes" : "No",
                PhilHealthNo = _random.Next(2) == 0 ? $"12-{_random.Next(100000000, 999999999)}" : "",
                Dateofbirth = verifyAccount.Dateofbirth,
                Age = CalculateAge(verifyAccount.Dateofbirth).ToString(),
                RLastname = user.LastName,
                RFirstname = user.FirstName,
                RMiddlename = user.MiddleName,
                RSuffix = "",
                RBlkLotStreet = verifyAccount.BlkLotStreet,
                RSubVill = verifyAccount.SubVill,
                RBrgy = verifyAccount.Barangay,
                RelationshipPatient = "Self",
                Status = status2 == "Approve" || status2 == "Disapprove" || status2 == "Retake" ? "Processing" : "Pending",
                Status2 = status2,
                Status3 = status3,
                ForCMOPERSONNEL = status2 == "Approve" ? TruncateToMaxLength($"Amount:{grantedAmount} MedCert,Rx", 100) : "",
                CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 60)),
                ProcessAt = status2 != "Pending" ? DateTime.Now.AddDays(-_random.Next(1, 30)) : DateTime.MinValue,
                Result = status2 == "Approve" || status2 == "Disapprove" ? DateTime.Now.AddDays(-_random.Next(1, 15)) : DateTime.MinValue,
                ClaimedAt = status3 == "Claimed" ? DateTime.Now.AddDays(-_random.Next(1, 7)) : DateTime.MinValue
            };

            _context.OtherAssistance.Add(application);
            await _context.SaveChangesAsync();

            await CreateNotificationsForApplication(user.Id, "OtherAssistance", status2, status3, application.Id, application.CreatedAt);
        }

        private async Task CreateFuneralAssistanceApplicationAsync(RegisterAcc user, Verifyaccount verifyAccount)
        {
            // Generate original document filenames
            string validIdOriginal = $"FuneralValidID_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string validBackOriginal = $"FuneralBackID_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string deathCertOriginal = $"DeathCert_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string prescriptionOriginal = $"Prescription_{user.FirstName}_{user.LastName}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";

            // Encrypt all filenames
            string encryptedValidId = _aesHelper.EncryptFilename(validIdOriginal) + ".enc";
            string encryptedValidBack = _aesHelper.EncryptFilename(validBackOriginal) + ".enc";
            string encryptedDeathCert = _aesHelper.EncryptFilename(deathCertOriginal) + ".enc";
            string encryptedPrescription = _aesHelper.EncryptFilename(prescriptionOriginal) + ".enc";

            string status2 = GetRandomStatus2();

            string status3 = "";
            if (status2 == "ApproveClaim")
            {
                status2 = "Approve";
                status3 = "Claimed";
            }

            int grantedAmount = status2 == "Approve" ? _random.Next(10000, 50000) : 0;

            var application = new FuneralAssistance
            {
                UserId = user.Id,
                Firstname = user.FirstName,
                Middlename = user.MiddleName,
                Lastname = user.LastName,
                Suffix = "",
                Validfrontimage = TruncateToMaxLength(encryptedValidId, 100),
                ValidBackimage = TruncateToMaxLength(encryptedValidBack, 100),
                DeathCertificate = TruncateToMaxLength(encryptedDeathCert, 100),
                DoctorPrescription = TruncateToMaxLength(encryptedPrescription, 100),
                Typeassistance = TruncateToMaxLength("Funeral Services, Burial Assistance", 100),
                BlkLotStreet = verifyAccount.BlkLotStreet,
                SubVill = verifyAccount.SubVill,
                Brgy = verifyAccount.Barangay,
                Sex = verifyAccount.Gender,
                PhilHealth = _random.Next(2) == 0 ? "Yes" : "No",
                PhilHealthNo = _random.Next(2) == 0 ? $"12-{_random.Next(100000000, 999999999)}" : "",
                Dateofbirth = verifyAccount.Dateofbirth,
                Age = CalculateAge(verifyAccount.Dateofbirth).ToString(),
                RLastname = user.LastName,
                RFirstname = user.FirstName,
                RMiddlename = user.MiddleName,
                RSuffix = "",
                RBlkLotStreet = verifyAccount.BlkLotStreet,
                RSubVill = verifyAccount.SubVill,
                RBrgy = verifyAccount.Barangay,
                RelationshipPatient = "Self",
                Status = status2 == "Approve" || status2 == "Disapprove" || status2 == "Retake" ? "Processing" : "Pending",
                Status2 = status2,
                Status3 = status3,
                ForCMOPERSONNEL = status2 == "Approve" ? TruncateToMaxLength($"Amount:{grantedAmount} DeathCert,BurialPermit", 100) : "",
                CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 60)),
                ProcessAt = status2 != "Pending" ? DateTime.Now.AddDays(-_random.Next(1, 30)) : DateTime.MinValue,
                Result = status2 == "Approve" || status2 == "Disapprove" ? DateTime.Now.AddDays(-_random.Next(1, 15)) : DateTime.MinValue,
                ClaimedAt = status3 == "Claimed" ? DateTime.Now.AddDays(-_random.Next(1, 7)) : DateTime.MinValue
            };

            _context.FuneralAssistance.Add(application);
            await _context.SaveChangesAsync();

            await CreateNotificationsForApplication(user.Id, "FuneralAssistance", status2, status3, application.Id, application.CreatedAt);
        }

        private async Task CreateNotificationsForApplication(int userId, string assistanceType, string status2, string status3, int applicationId, DateTime appCreatedAt)
        {
            // Notification 1: Application Submitted
            var notif1 = new Notification
            {
                UserId = userId,
                RecipientType = 1, // User notification
                ApplicationType = assistanceType,
                ApplicationId = applicationId,
                Title = "Application Submitted",
                Message = $"Your {assistanceType} application has been submitted successfully.",
                Type = "submitted",
                ProcessStage = "submitted",
                CreatedAt = appCreatedAt,
                EventTimestamp = appCreatedAt,
                IsRead = true,
                SentViaInApp = true
            };
            _context.Notifications.Add(notif1);

            // Notification 2: Status update (Approve/Disapprove/Retake)
            if (status2 != "Processing" && status2 != "Pending")
            {
                string message2 = status2 switch
                {
                    "Approve" => $"Your {assistanceType} application has been approved.",
                    "Disapprove" => $"Your {assistanceType} application has been disapproved.",
                    "Retake" => $"Please retake the required documents for your {assistanceType} application.",
                    _ => ""
                };

                var notif2 = new Notification
                {
                    UserId = userId,
                    RecipientType = 1,
                    ApplicationType = assistanceType,
                    ApplicationId = applicationId,
                    Title = $"Application {status2}",
                    Message = message2,
                    Type = status2.ToLower(),
                    ProcessStage = "result",
                    Status2 = status2,
                    CreatedAt = appCreatedAt.AddDays(_random.Next(1, 5)),
                    EventTimestamp = appCreatedAt.AddDays(_random.Next(1, 5)),
                    IsRead = _random.Next(2) == 0,
                    SentViaInApp = true
                };
                _context.Notifications.Add(notif2);
            }

            // Notification 3: Claimed status
            if (status3 == "Claimed")
            {
                var notif3 = new Notification
                {
                    UserId = userId,
                    RecipientType = 1,
                    ApplicationType = assistanceType,
                    ApplicationId = applicationId,
                    Title = "Assistance Claimed",
                    Message = $"Your {assistanceType} assistance has been claimed successfully.",
                    Type = "claimed",
                    ProcessStage = "claimed",
                    Status3 = status3,
                    CreatedAt = appCreatedAt.AddDays(_random.Next(6, 15)),
                    EventTimestamp = appCreatedAt.AddDays(_random.Next(6, 15)),
                    IsRead = _random.Next(2) == 0,
                    SentViaInApp = true
                };
                _context.Notifications.Add(notif3);
            }

            await _context.SaveChangesAsync();
        }

        private int CalculateAge(string dateOfBirth)
        {
            if (DateTime.TryParse(dateOfBirth, out DateTime dob))
            {
                var today = DateTime.Today;
                var age = today.Year - dob.Year;
                if (dob.Date > today.AddYears(-age)) age--;
                return age;
            }
            return _random.Next(18, 65);
        }

        private string TruncateToMaxLength(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength);
        }

        /// <summary>
        /// AES-256 Encryption Helper - Matches the system's implementation in LoginController
        /// </summary>
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
                {
                    throw new InvalidOperationException($"AES key must be 32 bytes (256 bits). Current length: {_aesKey.Length} bytes");
                }
            }

            public string EncryptFilename(string originalFileName)
            {
                using var aes = Aes.Create();
                aes.Key = _aesKey;
                aes.GenerateIV(); // Random IV for each encryption
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var memoryStream = new MemoryStream();

                // Write IV to the beginning
                memoryStream.Write(aes.IV, 0, aes.IV.Length);

                // Encrypt the filename
                byte[] inputBytes = Encoding.UTF8.GetBytes(originalFileName);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                memoryStream.Write(encryptedBytes, 0, encryptedBytes.Length);

                // Convert to Base64 and make filesystem-safe
                string base64 = Convert.ToBase64String(memoryStream.ToArray());
                return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
            }

            private static byte[] SafeConvertHexStringToByteArray(string hex)
            {
                if (hex.Length % 2 != 0)
                    throw new ArgumentException("Hex string must have an even number of characters");

                byte[] bytes = new byte[hex.Length / 2];
                for (int i = 0; i < hex.Length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                return bytes;
            }
        }
    }
}
