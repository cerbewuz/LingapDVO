using LingapDVO.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LingapDVO.Services
{
    /// <summary>
    /// TEMPORARY: Generates 200 realistic records following all system workflows
    /// RUN ONCE and DELETE this file after
    /// </summary>
    public class TempDataPopulator
    {
        private readonly ApplicationDbContext _context;
        private readonly Random _random = new Random();

        private readonly string[] _barangays = new[]
        {
            "Poblacion", "Matina", "Buhangin", "Talomo", "Toril", "Tugbok", "Calinan", "Marilog",
            "Paquibato", "Baguio", "Agdao", "Sasa", "Panacan", "Catalunan Grande", "Catalunan Pequeño",
            "Ma-a", "Bangkal", "Mintal", "Bago Aplaya", "Matina Crossing", "Lanang", "Ecoland",
            "Tibungco", "Magtuod", "Dacudao", "Leon Garcia", "Ula", "Bunawan", "Lasang", "Tacunan"
        };

        private readonly string[] _firstNames = new[]
        {
            "Juan", "Maria", "Jose", "Ana", "Pedro", "Rosa", "Miguel", "Carmen", "Antonio", "Luz",
            "Francisco", "Elena", "Manuel", "Sofia", "Roberto", "Isabel", "Carlos", "Teresa", "Luis", "Patricia",
            "Ramon", "Angela", "Fernando", "Gloria", "Ricardo", "Cristina", "Rafael", "Beatriz", "Alberto", "Victoria",
            "Eduardo", "Diana", "Javier", "Cecilia", "Enrique", "Margarita", "Rodrigo", "Lucia", "Diego", "Rosario",
            "Gabriel", "Josefa", "Sergio", "Dolores", "Martin", "Laura", "Alejandro", "Pilar", "Jorge", "Mercedes"
        };

        private readonly string[] _lastNames = new[]
        {
            "Santos", "Reyes", "Cruz", "Bautista", "Garcia", "Gonzales", "Flores", "Mendoza", "Torres", "Rivera",
            "Ramos", "Lopez", "Hernandez", "Perez", "Fernandez", "Castillo", "Morales", "Aquino", "Valdez", "Santiago",
            "Dela Cruz", "Villanueva", "Francisco", "Soriano", "Pascual", "Mercado", "Diaz", "Castro", "Navarro", "Rodriguez",
            "Evangelista", "San Jose", "Alvarez", "Rosales", "Magno", "Trinidad", "Tolentino", "Domingo", "Salazar", "Miranda"
        };

        private readonly string[] _middleNames = new[]
        {
            "Santos", "Cruz", "Reyes", "Garcia", "Lopez", "Ramos", "Torres", "Flores", "Morales", "Castro"
        };

        private readonly string[] _hospitalTypes = new[]
        {
            "Hospital Bill Payment", "Medical Supplies", "Medicines", "Laboratory Tests", "Diagnostic Procedures"
        };

        private readonly string[] _medicalTypes = new[]
        {
            "Medical Certificate", "Laboratory Tests", "Dental Services", "Medical Consultation", "Therapy Sessions"
        };

        private readonly string[] _adminNames = new[]
        {
            "Admin Rodriguez", "Admin Santos", "Admin Cruz", "Admin Reyes", "Admin Garcia"
        };

        private readonly string[] _comments = new[]
        {
            "All documents verified and complete.",
            "Application approved after review.",
            "Requirements met, ready for processing.",
            "Approved as per guidelines.",
            "All supporting documents validated.",
            "Does not meet eligibility criteria.",
            "Incomplete documentation.",
            "Duplicate application detected.",
            "Income threshold not met."
        };

        public TempDataPopulator(ApplicationDbContext context)
        {
            _context = context;
        }

        public void PopulateData()
        {
            Console.WriteLine("🚀 Starting temporary data population - 200 records...");

            var users = CreateUsers(50); // 50 users
            Console.WriteLine($"✓ Created {users.Count} users");

            var applications = CreateApplications(users, 200); // 200 total applications
            Console.WriteLine($"✓ Created {applications.Count} applications");

            CreateFeedback(applications, 80); // 80 feedback entries
            Console.WriteLine($"✓ Created feedback entries");

            _context.SaveChanges();
            Console.WriteLine("✅ COMPLETE! 200 dummy records created.");
            Console.WriteLine("⚠️ IMPORTANT: Delete TempDataPopulator.cs and TempDataController.cs now!");
        }

        private List<Useraccount> CreateUsers(int count)
        {
            var users = new List<Useraccount>();

            for (int i = 0; i < count; i++)
            {
                var firstName = GetRandom(_firstNames);
                var lastName = GetRandom(_lastNames);
                var middleName = GetRandom(_middleNames);
                var num = _random.Next(1000, 9999);
                var username = $"{firstName.ToLower()}.{lastName.ToLower()}{num}";
                var email = $"{firstName.ToLower()}.{lastName.ToLower()}{num}@gmail.com";

                if (_context.Useraccount.Any(u => u.Username == username || u.Email == email))
                    continue;

                var regToken = new RegistrationToken
                {
                    Token = Guid.NewGuid().ToString(),
                    IpAddress = "127.0.0.1",
                    UserAgent = "Mozilla/5.0",
                    CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 180)),
                    ExpiresAt = DateTime.Now.AddHours(1),
                    IsUsed = true,
                    UsedAt = DateTime.Now.AddDays(-_random.Next(1, 180)),
                    UsedByEmail = email,
                    IsRevoked = false
                };
                _context.RegistrationTokens.Add(regToken);

                var auditLog = new RegistrationAuditLog
                {
                    IpAddress = "127.0.0.1",
                    UserAgent = "Mozilla/5.0",
                    Email = email,
                    Username = username,
                    FullName = $"{firstName} {middleName} {lastName}",
                    Action = "SUCCESS",
                    Source = "WEB",
                    RegistrationToken = regToken.Token,
                    AttemptedAt = regToken.CreatedAt,
                    SuspiciousActivity = false,
                    HasValidToken = true
                };
                _context.RegistrationAuditLogs.Add(auditLog);

                var registerAcc = new RegisterAcc
                {
                    FirstName = firstName,
                    MiddleName = middleName,
                    LastName = lastName,
                    Suffix = _random.Next(0, 10) > 8 ? "Jr." : "",
                    Email = email,
                    Username = username,
                    Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    ConfirmPassword = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    ContactNo = $"09{_random.Next(100000000, 999999999)}",
                    BlkLotStreet = $"Block {_random.Next(1, 50)} Lot {_random.Next(1, 30)}",
                    SubdivisionVillage = $"Subdivision {_random.Next(1, 20)}",
                    Barangay = GetRandom(_barangays),
                    District = $"District {_random.Next(1, 4)}",
                    City = "Davao City",
                    ZipCode = "8000",
                    Dateofbirth = GetRandomBirthDate(),
                    Age = _random.Next(18, 70).ToString(),
                    Sex = _random.Next(0, 2) == 0 ? "Male" : "Female",
                    CreatedAt = regToken.CreatedAt,
                    RegistrationToken = regToken.Token
                };
                _context.RegisterAcc.Add(registerAcc);

                var useraccount = new Useraccount
                {
                    FirstName = firstName,
                    MiddleName = middleName,
                    LastName = lastName,
                    Suffix = registerAcc.Suffix,
                    Email = email,
                    Username = username,
                    Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    ContactNo = registerAcc.ContactNo,
                    BlkLotStreet = registerAcc.BlkLotStreet,
                    SubdivisionVillage = registerAcc.SubdivisionVillage,
                    Barangay = registerAcc.Barangay,
                    District = registerAcc.District,
                    City = "Davao City",
                    ZipCode = "8000",
                    Dateofbirth = registerAcc.Dateofbirth,
                    Age = registerAcc.Age,
                    Sex = registerAcc.Sex,
                    CreatedAt = regToken.CreatedAt
                };
                _context.Useraccount.Add(useraccount);

                var verifyAccount = new Verifyaccount
                {
                    Email = email,
                    Username = username,
                    VerificationCode = _random.Next(100000, 999999).ToString(),
                    IsVerified = true,
                    CreatedAt = regToken.CreatedAt,
                    VerifiedAt = regToken.CreatedAt.AddMinutes(5)
                };
                _context.Verifyaccount.Add(verifyAccount);

                _context.SaveChanges();
                users.Add(useraccount);
            }

            return users;
        }

        private List<object> CreateApplications(List<Useraccount> users, int totalCount)
        {
            var applications = new List<object>();
            int hospitalCount = 80;
            int medicalCount = 70;
            int funeralCount = 50;

            // Create Hospital Applications
            for (int i = 0; i < hospitalCount && users.Count > 0; i++)
            {
                var user = users[i % users.Count];
                var app = CreateHospitalApp(user);
                applications.Add(app);
            }

            // Create Medical Applications
            for (int i = 0; i < medicalCount && users.Count > 0; i++)
            {
                var user = users[i % users.Count];
                var app = CreateMedicalApp(user);
                applications.Add(app);
            }

            // Create Funeral Applications
            for (int i = 0; i < funeralCount && users.Count > 0; i++)
            {
                var user = users[i % users.Count];
                var app = CreateFuneralApp(user);
                applications.Add(app);
            }

            return applications;
        }

        private HospitalAssistance CreateHospitalApp(Useraccount user)
        {
            var createdDate = DateTime.Now.AddDays(-_random.Next(1, 180));
            var statusInfo = GetRandomStatus(createdDate);

            var formToken = new FormSubmissionToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = user.Id,
                FormType = "HospitalAssistance",
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                CreatedAt = createdDate.AddMinutes(-2),
                ExpiresAt = createdDate.AddHours(1),
                IsUsed = true,
                UsedAt = createdDate,
                IsRevoked = false
            };
            _context.FormSubmissionTokens.Add(formToken);

            var auditLog = new FormSubmissionAuditLog
            {
                UserId = user.Id,
                FormType = "HospitalAssistance",
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                FormSubmissionToken = formToken.Token,
                Action = "SUCCESS",
                Source = "WEB",
                SubmittedAt = createdDate,
                SuspiciousActivity = false,
                HasValidToken = true
            };
            _context.FormSubmissionAuditLogs.Add(auditLog);

            var app = new HospitalAssistance
            {
                UserId = user.Id,
                Lastname = user.LastName,
                Firstname = user.FirstName,
                Middlename = user.MiddleName,
                Suffix = user.Suffix,
                BlkLotStreet = user.BlkLotStreet,
                SubVill = user.SubdivisionVillage,
                Brgy = user.Barangay,
                District = user.District,
                Sex = user.Sex,
                PhilHealth = _random.Next(0, 10) > 3 ? "Yes" : "No",
                PhilHealthNo = _random.Next(0, 10) > 3 ? $"12-{_random.Next(100000000, 999999999)}" : "",
                Dateofbirth = user.Dateofbirth,
                Age = user.Age,
                RLastname = user.LastName,
                RFirstname = user.FirstName,
                RMiddlename = user.MiddleName,
                RSuffix = user.Suffix,
                RBlkLotStreet = user.BlkLotStreet,
                RSubVill = user.SubdivisionVillage,
                RBrgy = user.Barangay,
                RDistrict = user.District,
                RelationshipPatient = "Self",
                ContactNo = user.ContactNo,
                Typeassistance = GetRandom(_hospitalTypes),
                ForCMOPERSONNEL = "",
                Validfrontimage = $"valid_front_{Guid.NewGuid()}.jpg",
                ValidBackimage = $"valid_back_{Guid.NewGuid()}.jpg",
                DoctorPrescription = $"prescription_{Guid.NewGuid()}.jpg",
                DeathCertificate = "",
                CreatedAt = createdDate,
                ProcessAt = statusInfo.ProcessAt,
                Status = statusInfo.Status,
                Processby = statusInfo.Processby,
                Comments = statusInfo.Comments,
                Result = statusInfo.ResultAt,
                Status2 = statusInfo.Status2,
                ClaimedAt = statusInfo.ClaimedAt,
                Status3 = statusInfo.Status3
            };

            _context.HospitalAssistance.Add(app);
            return app;
        }

        private OtherAssistance CreateMedicalApp(Useraccount user)
        {
            var createdDate = DateTime.Now.AddDays(-_random.Next(1, 180));
            var statusInfo = GetRandomStatus(createdDate);

            var formToken = new FormSubmissionToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = user.Id,
                FormType = "OtherAssistance",
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                CreatedAt = createdDate.AddMinutes(-2),
                ExpiresAt = createdDate.AddHours(1),
                IsUsed = true,
                UsedAt = createdDate,
                IsRevoked = false
            };
            _context.FormSubmissionTokens.Add(formToken);

            var auditLog = new FormSubmissionAuditLog
            {
                UserId = user.Id,
                FormType = "OtherAssistance",
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                FormSubmissionToken = formToken.Token,
                Action = "SUCCESS",
                Source = "WEB",
                SubmittedAt = createdDate,
                SuspiciousActivity = false,
                HasValidToken = true
            };
            _context.FormSubmissionAuditLogs.Add(auditLog);

            var app = new OtherAssistance
            {
                UserId = user.Id,
                Lastname = user.LastName,
                Firstname = user.FirstName,
                Middlename = user.MiddleName,
                Suffix = user.Suffix,
                BlkLotStreet = user.BlkLotStreet,
                SubVill = user.SubdivisionVillage,
                Brgy = user.Barangay,
                District = user.District,
                Sex = user.Sex,
                PhilHealth = _random.Next(0, 10) > 3 ? "Yes" : "No",
                PhilHealthNo = _random.Next(0, 10) > 3 ? $"12-{_random.Next(100000000, 999999999)}" : "",
                Dateofbirth = user.Dateofbirth,
                Age = user.Age,
                RLastname = user.LastName,
                RFirstname = user.FirstName,
                RMiddlename = user.MiddleName,
                RSuffix = user.Suffix,
                RBlkLotStreet = user.BlkLotStreet,
                RSubVill = user.SubdivisionVillage,
                RBrgy = user.Barangay,
                RDistrict = user.District,
                RelationshipPatient = "Self",
                ContactNo = user.ContactNo,
                Typeassistance = GetRandom(_medicalTypes),
                ForCMOPERSONNEL = "",
                Validfrontimage = $"valid_front_{Guid.NewGuid()}.jpg",
                ValidBackimage = $"valid_back_{Guid.NewGuid()}.jpg",
                DoctorPrescription = $"prescription_{Guid.NewGuid()}.jpg",
                DeathCertificate = "",
                MedCertificate = $"medcert_{Guid.NewGuid()}.jpg",
                CreatedAt = createdDate,
                ProcessAt = statusInfo.ProcessAt,
                Status = statusInfo.Status,
                Processby = statusInfo.Processby,
                Comments = statusInfo.Comments,
                Result = statusInfo.ResultAt,
                Status2 = statusInfo.Status2,
                ClaimedAt = statusInfo.ClaimedAt,
                Status3 = statusInfo.Status3
            };

            _context.OtherAssistance.Add(app);
            return app;
        }

        private FuneralAssistance CreateFuneralApp(Useraccount user)
        {
            var createdDate = DateTime.Now.AddDays(-_random.Next(1, 180));
            var statusInfo = GetRandomStatus(createdDate);

            var formToken = new FormSubmissionToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = user.Id,
                FormType = "FuneralAssistance",
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                CreatedAt = createdDate.AddMinutes(-2),
                ExpiresAt = createdDate.AddHours(1),
                IsUsed = true,
                UsedAt = createdDate,
                IsRevoked = false
            };
            _context.FormSubmissionTokens.Add(formToken);

            var auditLog = new FormSubmissionAuditLog
            {
                UserId = user.Id,
                FormType = "FuneralAssistance",
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                FormSubmissionToken = formToken.Token,
                Action = "SUCCESS",
                Source = "WEB",
                SubmittedAt = createdDate,
                SuspiciousActivity = false,
                HasValidToken = true
            };
            _context.FormSubmissionAuditLogs.Add(auditLog);

            var app = new FuneralAssistance
            {
                UserId = user.Id,
                Lastname = user.LastName,
                Firstname = user.FirstName,
                Middlename = user.MiddleName,
                Suffix = user.Suffix,
                BlkLotStreet = user.BlkLotStreet,
                SubVill = user.SubdivisionVillage,
                Brgy = user.Barangay,
                District = user.District,
                Sex = user.Sex,
                PhilHealth = _random.Next(0, 10) > 3 ? "Yes" : "No",
                PhilHealthNo = _random.Next(0, 10) > 3 ? $"12-{_random.Next(100000000, 999999999)}" : "",
                Dateofbirth = user.Dateofbirth,
                Age = _random.Next(40, 95).ToString(),
                RLastname = user.LastName,
                RFirstname = user.FirstName,
                RMiddlename = user.MiddleName,
                RSuffix = user.Suffix,
                RBlkLotStreet = user.BlkLotStreet,
                RSubVill = user.SubdivisionVillage,
                RBrgy = user.Barangay,
                RDistrict = user.District,
                RelationshipPatient = GetRandomRelationship(),
                ContactNo = user.ContactNo,
                Typeassistance = "Funeral and Burial Assistance",
                ForCMOPERSONNEL = "",
                Validfrontimage = $"valid_front_{Guid.NewGuid()}.jpg",
                ValidBackimage = $"valid_back_{Guid.NewGuid()}.jpg",
                DoctorPrescription = "",
                DeathCertificate = $"deathcert_{Guid.NewGuid()}.jpg",
                CreatedAt = createdDate,
                ProcessAt = statusInfo.ProcessAt,
                Status = statusInfo.Status,
                Processby = statusInfo.Processby,
                Comments = statusInfo.Comments,
                Result = statusInfo.ResultAt,
                Status2 = statusInfo.Status2,
                ClaimedAt = statusInfo.ClaimedAt,
                Status3 = statusInfo.Status3
            };

            _context.FuneralAssistance.Add(app);
            return app;
        }

        private void CreateFeedback(List<object> applications, int count)
        {
            var feedbackCount = 0;

            foreach (var app in applications.Take(count))
            {
                if (app is HospitalAssistance hospitalApp &&
                    (hospitalApp.Status2 == "Approve" || hospitalApp.Status3?.ToLower() == "claimed"))
                {
                    CreateFeedbackForApp(hospitalApp.UserId, "Hospital Bill Assistance", hospitalApp.ClaimedAt != DateTime.MinValue ? hospitalApp.ClaimedAt : hospitalApp.Result);
                    feedbackCount++;
                }
                else if (app is OtherAssistance medicalApp &&
                         (medicalApp.Status2 == "Approve" || medicalApp.Status3?.ToLower() == "claimed"))
                {
                    CreateFeedbackForApp(medicalApp.UserId, "Medical and Laboratory Assistance", medicalApp.ClaimedAt != DateTime.MinValue ? medicalApp.ClaimedAt : medicalApp.Result);
                    feedbackCount++;
                }
                else if (app is FuneralAssistance funeralApp &&
                         (funeralApp.Status2 == "Approve" || funeralApp.Status3?.ToLower() == "claimed"))
                {
                    CreateFeedbackForApp(funeralApp.UserId, "Funeral and Burial Assistance", funeralApp.ClaimedAt != DateTime.MinValue ? funeralApp.ClaimedAt : funeralApp.Result);
                    feedbackCount++;
                }

                if (feedbackCount >= count) break;
            }
        }

        private void CreateFeedbackForApp(int userId, string assistanceType, DateTime submittedDate)
        {
            var user = _context.Useraccount.Find(userId);
            if (user == null) return;

            var feedback = new Feedback
            {
                UserId = userId,
                Name = $"{user.FirstName} {user.LastName}",
                Sex = user.Sex,
                AssistanceType = assistanceType,
                TypeOfClient = _random.Next(0, 3) == 0 ? "Citizen" : (_random.Next(0, 2) == 0 ? "Business" : "Government"),

                Q1_Knowledge = GetRandomKnowledgeResponse(),

                R1_ServiceSatisfaction = _random.Next(3, 6),
                R2_TimeSpent = _random.Next(3, 6),
                R3_ProcessFollowed = _random.Next(3, 6),
                R4_ProcessSimplicity = _random.Next(3, 6),
                R5_InformationAccess = _random.Next(3, 6),
                R6_FairPayment = _random.Next(3, 6),
                R7_Fairness = _random.Next(3, 6),
                R8_EmployeeCourtesy = _random.Next(4, 6),

                Q2_Commendation = _random.Next(0, 3) == 0 ? "Excellent service and very helpful staff!" : null,
                Q3_Suggestion = _random.Next(0, 4) == 0 ? "Could improve processing time." : null,
                Q4_Complaint = _random.Next(0, 10) == 0 ? "Long waiting time." : null,

                SubmittedAt = submittedDate.AddDays(_random.Next(1, 7))
            };

            _context.Feedbacks.Add(feedback);
        }

        private string GetRandomKnowledgeResponse()
        {
            var responses = new[]
            {
                "Facebook/Social Media",
                "Friend/Relative",
                "Government Website",
                "Posters/Flyers",
                "Walk-in Inquiry"
            };
            return GetRandom(responses);
        }

        private StatusProgression GetRandomStatus(DateTime createdDate)
        {
            var roll = _random.Next(0, 100);

            // 45% Claimed
            if (roll < 45)
            {
                var processAt = createdDate.AddMinutes(_random.Next(30, 120));
                var resultAt = processAt.AddMinutes(_random.Next(30, 180));
                var claimedAt = resultAt.AddDays(_random.Next(1, 7));

                return new StatusProgression
                {
                    Status = "processing",
                    ProcessAt = processAt,
                    Processby = GetRandom(_adminNames),
                    Status2 = "Approve",
                    ResultAt = resultAt,
                    Comments = "All documents verified and complete.",
                    Status3 = "Claimed",
                    ClaimedAt = claimedAt
                };
            }
            // 25% Approved (not claimed)
            else if (roll < 70)
            {
                var processAt = createdDate.AddMinutes(_random.Next(30, 120));
                var resultAt = processAt.AddMinutes(_random.Next(30, 180));

                return new StatusProgression
                {
                    Status = "processing",
                    ProcessAt = processAt,
                    Processby = GetRandom(_adminNames),
                    Status2 = "Approve",
                    ResultAt = resultAt,
                    Comments = "Application approved after review.",
                    Status3 = "",
                    ClaimedAt = DateTime.MinValue
                };
            }
            // 15% Disapproved
            else if (roll < 85)
            {
                var processAt = createdDate.AddMinutes(_random.Next(30, 120));
                var resultAt = processAt.AddMinutes(_random.Next(30, 180));

                return new StatusProgression
                {
                    Status = "processing",
                    ProcessAt = processAt,
                    Processby = GetRandom(_adminNames),
                    Status2 = "Disapprove",
                    ResultAt = resultAt,
                    Comments = GetRandom(new[] { "Does not meet eligibility criteria.", "Incomplete documentation.", "Duplicate application detected." }),
                    Status3 = "",
                    ClaimedAt = DateTime.MinValue
                };
            }
            // 10% Processing
            else if (roll < 95)
            {
                var processAt = createdDate.AddMinutes(_random.Next(30, 120));

                return new StatusProgression
                {
                    Status = "processing",
                    ProcessAt = processAt,
                    Processby = GetRandom(_adminNames),
                    Status2 = "",
                    ResultAt = DateTime.MinValue,
                    Comments = "",
                    Status3 = "",
                    ClaimedAt = DateTime.MinValue
                };
            }
            // 5% Pending
            else
            {
                return new StatusProgression
                {
                    Status = "pending",
                    ProcessAt = DateTime.MinValue,
                    Processby = "",
                    Status2 = "",
                    ResultAt = DateTime.MinValue,
                    Comments = "",
                    Status3 = "",
                    ClaimedAt = DateTime.MinValue
                };
            }
        }

        private string GetRandomBirthDate()
        {
            var year = DateTime.Now.Year - _random.Next(18, 70);
            var month = _random.Next(1, 13);
            var day = _random.Next(1, 29);
            return $"{month:D2}/{day:D2}/{year}";
        }

        private string GetRandomRelationship()
        {
            return GetRandom(new[] { "Son", "Daughter", "Spouse", "Sibling", "Parent", "Relative" });
        }

        private T GetRandom<T>(T[] array)
        {
            return array[_random.Next(array.Length)];
        }

        private class StatusProgression
        {
            public string Status { get; set; } = "";
            public DateTime ProcessAt { get; set; }
            public string Processby { get; set; } = "";
            public string Status2 { get; set; } = "";
            public DateTime ResultAt { get; set; }
            public string Comments { get; set; } = "";
            public string Status3 { get; set; } = "";
            public DateTime ClaimedAt { get; set; }
        }
    }
}
