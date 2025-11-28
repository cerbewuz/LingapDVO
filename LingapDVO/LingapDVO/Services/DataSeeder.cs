using LingapDVO.Models;
using System.Security.Cryptography;
using System.Text;

namespace LingapDVO.Services
{

    /// <summary>
    /// Data seeder for generating 250 dummy applications with encrypted ID files
    /// </summary>
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IDateTimeService _dateTimeService;
        private readonly Random _random = new Random(42); // Seed for reproducibility
        private readonly byte[] _aesKey;

        // Filipino first names
        private readonly string[] _firstNames = new[]
        {
            "Maria", "Jose", "Juan", "Ana", "Pedro", "Rosa", "Miguel", "Carmen", "Antonio", "Luz",
            "Francisco", "Teresa", "Manuel", "Isabel", "Ramon", "Elena", "Carlos", "Josefina", "Luis", "Angela",
            "Roberto", "Patricia", "Eduardo", "Margarita", "Ricardo", "Gloria", "Fernando", "Cecilia", "Jorge", "Beatriz",
            "Alberto", "Victoria", "Alejandro", "Rosario", "Rafael", "Marina", "Gabriel", "Catalina", "Daniel", "Remedios",
            "Pablo", "Felicidad", "Sergio", "Esperanza", "Arturo", "Dolores", "Rodrigo", "Pilar", "Enrique", "Concepcion",
            "Javier", "Mercedes", "Diego", "Soledad", "Oscar", "Amparo", "Felix", "Socorro", "Raul", "Paz",
            "Armando", "Fe", "Lorenzo", "Caridad", "Vicente", "Asuncion", "Ernesto", "Trinidad", "Alfredo", "Milagros",
            "Guillermo", "Purificacion", "Salvador", "Natividad", "Emilio", "Presentacion", "Benjamin", "Incarnacion", "Julio", "Consolacion",
            "Marco", "Corazon", "Leonardo", "Estrella", "Andres", "Perla", "Marcos", "Cristina", "Ruben", "Adelaida",
            "Mario", "Lourdes", "Cesar", "Magdalena", "Orlando", "Fé", "Jaime", "Teresita", "Felipe", "Rosita"
        };

        // Filipino last names
        private readonly string[] _lastNames = new[]
        {
            "Santos", "Reyes", "Cruz", "Bautista", "Ocampo", "Garcia", "Mendoza", "Torres", "Lopez", "Gonzales",
            "Rivera", "Flores", "Ramos", "Castro", "Martinez", "Hernandez", "Aquino", "Sanchez", "Dela Cruz", "Francisco",
            "Mercado", "Fernandez", "Villanueva", "Santiago", "Navarro", "Diaz", "Manuel", "Soriano", "Castillo", "Valdez",
            "Pascual", "Gutierrez", "Domingo", "Aguilar", "Jimenez", "Robles", "De Leon", "Velasco", "Romero", "Morales",
            "Mejia", "Marquez", "Salazar", "Vargas", "Rosales", "Herrera", "Miranda", "Velasquez", "Medina", "Luna",
            "Alvarez", "Cordero", "Perez", "Gomez", "Silva", "Alonzo", "Tolentino", "Manalang", "Magno", "Salvador",
            "Estrada", "Zamora", "Alonso", "Valencia", "Ibarra", "Ignacio", "Caballero", "Vasquez", "Ortega", "Tan",
            "Lim", "Ng", "Lee", "Chua", "Ong", "Go", "Sy", "Chan", "Dy", "Yu",
            "Dizon", "Pangan", "Angeles", "Manalo", "Galang", "David", "Simon", "Lorenzo", "Pinto", "Salcedo",
            "Espinosa", "Pineda", "Salas", "Montes", "Suarez", "Padilla", "Maldonado", "Cuevas", "Coronel", "Fuentes"
        };

        // Davao barangays
        private readonly string[] _barangays = new[]
        {
            "Acacia", "Agdao", "Alambre", "Alejandra Navarro (Lasang)", "Alfonso Angliongto Sr.", "Angalan",
            "Bago Aplaya", "Bago Gallera", "Bago Oshiro", "Balengaeng", "Baliok", "Bangkas Heights",
            "Bantol", "Baracatan", "Bato", "Bayabas", "Biao Escuela", "Biao Guianga",
            "Binugao", "Bucana", "Buda", "Buhangin Proper", "Bunawan Proper", "Cabantian",
            "Cadalian", "Calinan Poblacion", "Callawa", "Camansi", "Carmen", "Catalunan Grande",
            "Catalunan Pequeño", "Cawayan", "Centro (Pob.)", "Colosas", "Communal", "Crossing Bayabas",
            "Dacudao", "Dalag", "Dalagdag", "Daliao", "Daliaon Plantation", "Datu Salumay",
            "Dominga", "Dumoy", "Eden", "Fatima (Benowang)", "Gatungan", "Gov. Paciano Bangoy",
            "Gov. Vicente Duterte", "Gumalang", "Gumitan", "Hizon", "Ilang", "Inayangan",
            "Indangan", "Kap. Tomas Monteverde, Sr.", "Kilate", "Lacson", "Lamanan", "Lampianao"
        };

        // Davao districts
        private readonly string[] _districts = new[]
        {
            "Poblacion District", "Agdao District", "Buhangin District", "Calinan District",
            "Marilog District", "Paquibato District", "Talomo District", "Toril District",
            "Tugbok District", "Baguio District", "Leon Garcia District"
        };

        // ID types (database naming)
        private readonly string[] _idTypes = new[]
        {
            "phil-id", "driver-license", "umid"
        };

        // Assistance types for each form
        private readonly string[] _hospitalAssistanceTypes = new[]
        {
            "Medicine", "Laboratory", "Hospital Bill", "Medical Supplies", "Dialysis", "Chemotherapy"
        };

        private readonly string[] _funeralAssistanceTypes = new[]
        {
            "Burial Assistance", "Funeral Services", "Casket", "Memorial Services"
        };

        private readonly string[] _otherAssistanceTypes = new[]
        {
            "Laboratory Test", "Medical Procedure", "X-Ray", "Ultrasound", "CT Scan", "MRI"
        };

        // Status options
        private readonly string[] _statuses = new[]
        {
            "Waiting",
"Processing", "Approve", "Pending", "Rejected"
        };

        public DataSeeder(ApplicationDbContext context, IWebHostEnvironment environment, IDateTimeService dateTimeService, string aesKeyHex)
        {
            _context = context;
            _environment = environment;
            _dateTimeService = dateTimeService;
            _aesKey = HexStringToByteArray(aesKeyHex);
        }

        // Weighted status2 generator: 15% Disapprove, 35% Approve, 50% Approve+Claim (ApproveClaim)
        private string GetWeightedStatus2()
        {
            int roll = _random.Next(100);
            if (roll < 15) return "Disapprove";
            if (roll < 15 + 35) return "Approve";
            return "ApproveClaim";
        }

        private byte[] HexStringToByteArray(string hex)
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

        public async Task SeedDataAsync()
        {
            Console.WriteLine("Starting data seeding with 250 dummy applications...");

            // Ensure directories exist - matching system's folder structure
            string validImgPath = Path.Combine(_environment.WebRootPath, "Validimg");
            string hospitalStoragePath = Path.Combine(_environment.WebRootPath, "HospitalAssistanceFileStorage");
            string funeralStoragePath = Path.Combine(_environment.WebRootPath, "FuneralAssistanceFileStorage");
            string otherStoragePath = Path.Combine(_environment.WebRootPath, "OtherAssistanceFileStorage");

            Directory.CreateDirectory(validImgPath);
            Directory.CreateDirectory(hospitalStoragePath);
            Directory.CreateDirectory(funeralStoragePath);
            Directory.CreateDirectory(otherStoragePath);

            // Generate 250 applications
            List<object> applications = new List<object>();

            for (int i = 0; i < 250; i++)
            {
                // Randomly select form type
                int formType = _random.Next(0, 3);
                string idType = _idTypes[_random.Next(_idTypes.Length)];

                // Generate person data
                var personData = GeneratePersonData(i, idType);

                // Create encrypted ID files in Validimg folder
                string frontIdFileName = await CreateEncryptedIdFile(validImgPath, $"front_{i + 1}_{idType.Replace(" ", "_")}.enc", true, idType);
                string backIdFileName = await CreateEncryptedIdFile(validImgPath, $"back_{i + 1}_{idType.Replace(" ", "_")}.enc", false, idType);

                switch (formType)
                {
                    case 0: // Hospital Assistance
                        var hospital = CreateHospitalAssistance(personData, frontIdFileName, backIdFileName, hospitalStoragePath);
                        _context.HospitalAssistance.Add(hospital);
                        break;
                    case 1: // Funeral Assistance
                        var funeral = CreateFuneralAssistance(personData, frontIdFileName, backIdFileName, funeralStoragePath);
                        _context.FuneralAssistance.Add(funeral);
                        break;
                    case 2: // Other Assistance
                        var other = CreateOtherAssistance(personData, frontIdFileName, backIdFileName, otherStoragePath);
                        _context.OtherAssistance.Add(other);
                        break;
                }

                if ((i + 1) % 50 == 0)
                {
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Saved {i + 1} applications...");
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Data seeding completed successfully! 250 applications created.");
        }

        private Dictionary<string, string> GeneratePersonData(int index, string idType)
        {
            string firstName = _firstNames[_random.Next(_firstNames.Length)];
            string middleName = _firstNames[_random.Next(_firstNames.Length)];
            string lastName = _lastNames[_random.Next(_lastNames.Length)];
            string suffix = _random.Next(0, 10) == 0 ? (new[] { "Jr.", "Sr.", "II", "III" })[_random.Next(4)] : "";
            string sex = _random.Next(0, 2) == 0 ? "Male" : "Female";
            int age = _random.Next(18, 85);
            DateTime birthDate = _dateTimeService.Now.AddYears(-age).AddDays(-_random.Next(0, 365));

            return new Dictionary<string, string>
            {
                ["FirstName"] = firstName,
                ["MiddleName"] = middleName,
                ["LastName"] = lastName,
                ["Suffix"] = suffix,
                ["Sex"] = sex,
                ["Age"] = age.ToString(),
                ["DateOfBirth"] = birthDate.ToString("yyyy-MM-dd"),
                ["BlkLotStreet"] = $"{_random.Next(1, 200)} {new[] { "Main St", "Roxas Ave", "Bonifacio St", "Rizal Ave", "JP Laurel Ave" }[_random.Next(5)]}",
                ["SubVill"] = new[] { "Sunset Village", "Palm Grove", "Green Hills", "Riverside", "Mountain View", "" }[_random.Next(6)],
                ["Barangay"] = _barangays[_random.Next(_barangays.Length)],
                ["District"] = _districts[_random.Next(_districts.Length)],
                ["PhilHealth"] = _random.Next(0, 2) == 0 ? "Yes" : "No",
                ["PhilHealthNo"] = _random.Next(0, 2) == 0 ? $"{_random.Next(100000000, 999999999)}" : "",
                ["ContactNo"] = $"09{_random.Next(100000000, 999999999)}",
                ["IdType"] = idType
            };
        }

        private HospitalAssistance CreateHospitalAssistance(Dictionary<string, string> personData, string frontId, string backId, string storagePath)
        {
            bool hasDifferentRequestor = _random.Next(0, 3) == 0; // 33% chance
            var prescriptionFile = CreateEncryptedPrescriptionFile(storagePath, $"prescription_{Guid.NewGuid()}.enc").Result;

            int daysAgo = _random.Next(1, 60);
            DateTime createdAt = _dateTimeService.Now.AddDays(-daysAgo);
            DateTime processAt = createdAt.AddHours(_random.Next(2, 48));

            // Determine weighted status2 and translate to status/status3
            string status2 = GetWeightedStatus2();
            string status3 = "";
            if (status2 == "ApproveClaim")
            {
                status2 = "Approve";
                status3 = "Claimed";
            }
            string status = status2 == "" ? "Waiting" : "Processing";
            string processby = status != "Waiting" ? new[] { "Dr. Santos", "Dr. Reyes", "Dr. Cruz", "Dr. Bautista" }[_random.Next(4)] : "";

            // Resolve requestor fields (use applicant values when requestor is same)
            string rLastname = hasDifferentRequestor ? _lastNames[_random.Next(_lastNames.Length)] : personData["LastName"];
            string rFirstname = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["FirstName"];
            string rMiddlename = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["MiddleName"];
            string rSuffix = hasDifferentRequestor && _random.Next(0, 10) == 0 ? "Jr." : "";
            string rBlkLotStreet = personData["BlkLotStreet"];
            string rSubVill = personData["SubVill"];
            string rBrgy = personData["Barangay"];
            string rDistrict = personData["District"];
            string rRelationship = hasDifferentRequestor ? new[] { "Spouse", "Child", "Parent", "Sibling", "Relative" }[_random.Next(5)] : "Self";

            return new HospitalAssistance
            {
                UserId = _random.Next(1, 51), // Assuming 50 users exist
                Lastname = personData["LastName"],
                Firstname = personData["FirstName"],
                Middlename = personData["MiddleName"],
                Suffix = personData["Suffix"],
                BlkLotStreet = personData["BlkLotStreet"],
                SubVill = personData["SubVill"],
                Brgy = personData["Barangay"],
                District = personData["District"],
                Sex = personData["Sex"],
                PhilHealth = personData["PhilHealth"],
                PhilHealthNo = personData["PhilHealthNo"],
                Dateofbirth = personData["DateOfBirth"],
                Age = personData["Age"],
                RLastname = rLastname,
                RFirstname = rFirstname,
                RMiddlename = rMiddlename,
                RSuffix = rSuffix,
                RBlkLotStreet = rBlkLotStreet,
                RSubVill = rSubVill,
                RBrgy = rBrgy,
                RDistrict = rDistrict,
                RelationshipPatient = rRelationship,
                ContactNo = personData["ContactNo"],
                Typeassistance = _hospitalAssistanceTypes[_random.Next(_hospitalAssistanceTypes.Length)],
                Validfrontimage = frontId,
                ValidBackimage = backId,
                DoctorPrescription = prescriptionFile,
                DeathCertificate = "",
                CreatedAt = createdAt,
                ProcessAt = status != "Waiting" ? processAt : default(DateTime),
                Status = status,
                Processby = processby,
                Status2 = status2,
                Result = status2 == "Approve" ? processAt.AddDays(_random.Next(1, 7)) : default(DateTime),
                ClaimedAt = status3 == "Claimed" ? processAt.AddDays(_random.Next(7, 14)) : default(DateTime),
                Status3 = status3,
                IsArchived = daysAgo > 30
            };
        }

        private FuneralAssistance CreateFuneralAssistance(Dictionary<string, string> personData, string frontId, string backId, string storagePath)
        {
            bool hasDifferentRequestor = _random.Next(0, 2) == 0; // 50% chance for funeral (usually different person)
            var deathCertFile = CreateEncryptedDeathCertFile(storagePath, $"deathcert_{Guid.NewGuid()}.enc").Result;

            int daysAgo = _random.Next(1, 60);
            DateTime createdAt = _dateTimeService.Now.AddDays(-daysAgo);
            DateTime processAt = createdAt.AddHours(_random.Next(2, 48));

            string status2 = GetWeightedStatus2();
            string status3 = "";
            if (status2 == "ApproveClaim")
            {
                status2 = "Approve";
                status3 = "Claimed";
            }
            string status = status2 == "" ? "Waiting" : "Processing";
            string processby = status != "Waiting" ? new[] { "Ms. Santos", "Ms. Reyes", "Mr. Cruz", "Ms. Bautista" }[_random.Next(4)] : "";

            // Resolve requestor fields (use applicant values when requestor is same)
            string rLastname = hasDifferentRequestor ? _lastNames[_random.Next(_lastNames.Length)] : personData["LastName"];
            string rFirstname = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["FirstName"];
            string rMiddlename = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["MiddleName"];
            string rSuffix = hasDifferentRequestor && _random.Next(0, 10) == 0 ? "Jr." : "";
            string rBlkLotStreet = personData["BlkLotStreet"];
            string rSubVill = personData["SubVill"];
            string rBrgy = personData["Barangay"];
            string rDistrict = personData["District"];
            string rRelationship = hasDifferentRequestor ? new[] { "Spouse", "Child", "Parent", "Sibling", "Relative" }[_random.Next(5)] : "Self";

            return new FuneralAssistance
            {
                UserId = _random.Next(1, 51),
                Lastname = personData["LastName"],
                Firstname = personData["FirstName"],
                Middlename = personData["MiddleName"],
                Suffix = personData["Suffix"],
                BlkLotStreet = personData["BlkLotStreet"],
                SubVill = personData["SubVill"],
                Brgy = personData["Barangay"],
                District = personData["District"],
                Sex = personData["Sex"],
                PhilHealth = personData["PhilHealth"],
                PhilHealthNo = personData["PhilHealthNo"],
                Dateofbirth = personData["DateOfBirth"],
                Age = personData["Age"],
                RLastname = hasDifferentRequestor ? _lastNames[_random.Next(_lastNames.Length)] : personData["LastName"],
                RFirstname = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["FirstName"],
                RMiddlename = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["MiddleName"],
                RSuffix = hasDifferentRequestor && _random.Next(0, 10) == 0 ? "Jr." : "",
                RBlkLotStreet = personData["BlkLotStreet"],
                RSubVill = personData["SubVill"],
                RBrgy = personData["Barangay"],
                RDistrict = personData["District"],
                RelationshipPatient = hasDifferentRequestor ? new[] { "Spouse", "Child", "Parent", "Sibling", "Relative" }[_random.Next(5)] : "Self",
                ContactNo = personData["ContactNo"],
                Typeassistance = _funeralAssistanceTypes[_random.Next(_funeralAssistanceTypes.Length)],
                Validfrontimage = frontId,
                ValidBackimage = backId,
                DoctorPrescription = "",
                DeathCertificate = deathCertFile,
                CreatedAt = createdAt,
                ProcessAt = status != "Waiting" ? processAt : default(DateTime),
                Status = status,
                Processby = processby,
                Status2 = status2,
                Result = status2 == "Approve" ? processAt.AddDays(_random.Next(1, 7)) : default(DateTime),
                ClaimedAt = status3 == "Claimed" ? processAt.AddDays(_random.Next(7, 14)) : default(DateTime),
                Status3 = status3,
                IsArchived = daysAgo > 30
            };
        }

        private OtherAssistance CreateOtherAssistance(Dictionary<string, string> personData, string frontId, string backId, string storagePath)
        {
            bool hasDifferentRequestor = _random.Next(0, 3) == 0; // 33% chance
            var medCertFile = CreateEncryptedMedCertFile(storagePath, $"medcert_{Guid.NewGuid()}.enc").Result;
            var prescriptionFile = CreateEncryptedPrescriptionFile(storagePath, $"prescription_{Guid.NewGuid()}.enc").Result;

            int daysAgo = _random.Next(1, 60);
            DateTime createdAt = _dateTimeService.Now.AddDays(-daysAgo);
            DateTime processAt = createdAt.AddHours(_random.Next(2, 48));

            string status2 = GetWeightedStatus2();
            string status3 = "";
            if (status2 == "ApproveClaim")
            {
                status2 = "Approve";
                status3 = "Claimed";
            }
            string status = status2 == "" ? "Waiting" : "Processing";
            string processby = status != "Waiting" ? new[] { "Lab Tech Santos", "Lab Tech Reyes", "Dr. Cruz", "Dr. Mendoza" }[_random.Next(4)] : "";

            // Resolve requestor fields (use applicant values when requestor is same)
            string rLastname = hasDifferentRequestor ? _lastNames[_random.Next(_lastNames.Length)] : personData["LastName"];
            string rFirstname = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["FirstName"];
            string rMiddlename = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["MiddleName"];
            string rSuffix = hasDifferentRequestor && _random.Next(0, 10) == 0 ? "Jr." : "";
            string rBlkLotStreet = personData["BlkLotStreet"];
            string rSubVill = personData["SubVill"];
            string rBrgy = personData["Barangay"];
            string rDistrict = personData["District"];
            string rRelationship = hasDifferentRequestor ? new[] { "Spouse", "Child", "Parent", "Sibling", "Relative" }[_random.Next(5)] : "Self";

            return new OtherAssistance
            {
                UserId = _random.Next(1, 51),
                Lastname = personData["LastName"],
                Firstname = personData["FirstName"],
                Middlename = personData["MiddleName"],
                Suffix = personData["Suffix"],
                BlkLotStreet = personData["BlkLotStreet"],
                SubVill = personData["SubVill"],
                Brgy = personData["Barangay"],
                District = personData["District"],
                Sex = personData["Sex"],
                PhilHealth = personData["PhilHealth"],
                PhilHealthNo = personData["PhilHealthNo"],
                Dateofbirth = personData["DateOfBirth"],
                Age = personData["Age"],
                RLastname = hasDifferentRequestor ? _lastNames[_random.Next(_lastNames.Length)] : personData["LastName"],
                RFirstname = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["FirstName"],
                RMiddlename = hasDifferentRequestor ? _firstNames[_random.Next(_firstNames.Length)] : personData["MiddleName"],
                RSuffix = hasDifferentRequestor && _random.Next(0, 10) == 0 ? "Jr." : "",
                RBlkLotStreet = personData["BlkLotStreet"],
                RSubVill = personData["SubVill"],
                RBrgy = personData["Barangay"],
                RDistrict = personData["District"],
                RelationshipPatient = hasDifferentRequestor ? new[] { "Spouse", "Child", "Parent", "Sibling", "Relative" }[_random.Next(5)] : "Self",
                ContactNo = personData["ContactNo"],
                Typeassistance = _otherAssistanceTypes[_random.Next(_otherAssistanceTypes.Length)],
                Validfrontimage = frontId,
                ValidBackimage = backId,
                DoctorPrescription = prescriptionFile,
                DeathCertificate = "",
                MedCertificate = medCertFile,
                CreatedAt = createdAt,
                ProcessAt = status != "Waiting" ? processAt : default(DateTime),
                Status = status,
                Processby = processby,
                Status2 = status2,
                Result = status2 == "Approve" ? processAt.AddDays(_random.Next(1, 7)) : default(DateTime),
                ClaimedAt = status3 == "Claimed" ? processAt.AddDays(_random.Next(7, 14)) : default(DateTime),
                Status3 = status3,
                IsArchived = daysAgo > 30
            };
        }

        private async Task<string> CreateEncryptedIdFile(string directory, string fileName, bool isFront, string idType)
        {
            // Create a dummy ID image (simple text-based placeholder)
            string idContent = GenerateDummyIdContent(isFront, idType);
            byte[] plainBytes = Encoding.UTF8.GetBytes(idContent);

            // Encrypt the file
            byte[] encryptedBytes = EncryptFileBytes(plainBytes);

            // Save to disk
            string filePath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(filePath, encryptedBytes);

            return fileName;
        }

        private async Task<string> CreateEncryptedPrescriptionFile(string directory, string fileName)
        {
            string content = GenerateDummyPrescription();
            byte[] plainBytes = Encoding.UTF8.GetBytes(content);
            byte[] encryptedBytes = EncryptFileBytes(plainBytes);

            string filePath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(filePath, encryptedBytes);

            return fileName;
        }

        private async Task<string> CreateEncryptedDeathCertFile(string directory, string fileName)
        {
            string content = GenerateDummyDeathCertificate();
            byte[] plainBytes = Encoding.UTF8.GetBytes(content);
            byte[] encryptedBytes = EncryptFileBytes(plainBytes);

            string filePath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(filePath, encryptedBytes);

            return fileName;
        }

        private async Task<string> CreateEncryptedMedCertFile(string directory, string fileName)
        {
            string content = GenerateDummyMedicalCertificate();
            byte[] plainBytes = Encoding.UTF8.GetBytes(content);
            byte[] encryptedBytes = EncryptFileBytes(plainBytes);

            string filePath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(filePath, encryptedBytes);

            return fileName;
        }

        private byte[] EncryptFileBytes(byte[] plainBytes)
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            using var memoryStream = new MemoryStream();

            // Write IV first (first 16 bytes)
            memoryStream.Write(aes.IV, 0, aes.IV.Length);

            // Write encrypted data
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();
            }

            return memoryStream.ToArray();
        }

        private string GenerateDummyIdContent(bool isFront, string idType)
        {
            if (isFront)
            {
                return $@"===== {idType.ToUpper()} (FRONT) =====
REPUBLIC OF THE PHILIPPINES
{idType}

Name: [DUMMY DATA - ENCRYPTED]
ID No: {_random.Next(100000000, 999999999)}
Date of Birth: {DateTime.Now.AddYears(-_random.Next(18, 70)).ToString("yyyy-MM-dd")}
Sex: {(_random.Next(0, 2) == 0 ? "M" : "F")}
Address: Davao City, Philippines

Valid Until: {DateTime.Now.AddYears(5).ToString("yyyy-MM-dd")}
=============================";
            }
            else
            {
                return $@"===== {idType.ToUpper()} (BACK) =====
REPUBLIC OF THE PHILIPPINES

Emergency Contact Information
Contact: 09{_random.Next(100000000, 999999999)}

Signature: [SIGNATURE]
Thumbprint: [THUMBPRINT]

Issued: {DateTime.Now.AddDays(-_random.Next(1, 1000)).ToString("yyyy-MM-dd")}
=============================";
            }
        }

        private string GenerateDummyPrescription()
        {
            return $@"MEDICAL PRESCRIPTION
Date: {DateTime.Now.ToString("yyyy-MM-dd")}

Patient: [DUMMY DATA - ENCRYPTED]
Doctor: Dr. {_lastNames[_random.Next(_lastNames.Length)]}

Rx:
1. {new[] { "Amoxicillin 500mg", "Paracetamol 500mg", "Ibuprofen 400mg", "Cetirizine 10mg" }[_random.Next(4)]} - {_random.Next(1, 3)}x daily for {_random.Next(3, 14)} days
2. {new[] { "Vitamin C", "Multivitamins", "Zinc supplements" }[_random.Next(3)]} - 1x daily

Follow-up: {DateTime.Now.AddDays(7).ToString("yyyy-MM-dd")}

Doctor's Signature: ___________
License No: {_random.Next(100000, 999999)}";
        }

        private string GenerateDummyDeathCertificate()
        {
            return $@"CERTIFICATE OF DEATH
REPUBLIC OF THE PHILIPPINES
PHILIPPINE STATISTICS AUTHORITY

Registry No: {_random.Next(2024000000, 2024999999)}
Date of Death: {DateTime.Now.AddDays(-_random.Next(1, 30)).ToString("yyyy-MM-dd")}
Place of Death: Davao City

Deceased: [DUMMY DATA - ENCRYPTED]
Age: {_random.Next(50, 95)}
Sex: {(_random.Next(0, 2) == 0 ? "Male" : "Female")}

Cause of Death: {new[] { "Cardiac Arrest", "Respiratory Failure", "Natural Causes", "Complications from illness" }[_random.Next(4)]}

Attended by: Dr. {_lastNames[_random.Next(_lastNames.Length)]}
Date Issued: {DateTime.Now.ToString("yyyy-MM-dd")}

Local Civil Registrar
Davao City";
        }

        private string GenerateDummyMedicalCertificate()
        {
            return $@"MEDICAL CERTIFICATE

This is to certify that:
Patient: [DUMMY DATA - ENCRYPTED]
Age: {_random.Next(18, 80)}
Sex: {(_random.Next(0, 2) == 0 ? "Male" : "Female")}

Has been examined and found to be:
Diagnosis: {new[] { "Hypertension", "Diabetes Mellitus", "Acute Gastroenteritis", "Upper Respiratory Tract Infection", "Urinary Tract Infection" }[_random.Next(5)]}

Recommendation: {new[] { "Needs medication", "Requires laboratory tests", "Advised for rest", "Follow-up consultation needed" }[_random.Next(4)]}

Date of Examination: {DateTime.Now.AddDays(-_random.Next(1, 7)).ToString("yyyy-MM-dd")}

Examining Physician: Dr. {_lastNames[_random.Next(_lastNames.Length)]}
License No: {_random.Next(100000, 999999)}
Signature: ___________

Date Issued: {DateTime.Now.ToString("yyyy-MM-dd")}";
        }
    }
}
