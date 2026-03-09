using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LingapDVO.Controllers
{

    /// <summary>
    /// Controller for running the data seeder
    /// IMPORTANT: This should only be run in development/testing environments
    /// </summary>
    [EnableRateLimiting("GlobalLimit")]
    public class DataSeederController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILogger<DataSeederController> _logger;
        private readonly IConfiguration _configuration;

        public DataSeederController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IDateTimeService dateTimeService,
            ILogger<DataSeederController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _dateTimeService = dateTimeService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Endpoint to trigger data seeding
        /// Access: /DataSeeder/Seed
        /// WARNING: Only use in development environment
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Seed()
        {
            // Safety check: Only allow in development
            if (!_environment.IsDevelopment())
            {
                return StatusCode(403, "Data seeding is only allowed in development environment");
            }

            try
            {
                _logger.LogInformation("Starting data seeding process...");

                string aesKey = _configuration["Security:AesEncryption:Key"] 
                    ?? throw new InvalidOperationException("AES encryption key not found in configuration");
                
                var seeder = new DataSeeder(_context, _environment, _dateTimeService, aesKey);
                await seeder.SeedDataAsync();

                _logger.LogInformation("Data seeding completed successfully");

                return Ok(new
                {
                    success = true,
                    message = "Successfully seeded 250 dummy applications with encrypted ID files",
                    details = new
                    {
                        totalApplications = 250,
                        formTypes = new[] { "HospitalAssistance", "FuneralAssistance", "OtherAssistance" },
                        idTypes = new[] { "phil-id", "driver-license", "umid" },
                        encryption = "AES256-CBC with PKCS7 padding",
                        filesCreated = "Encrypted ID front/back images, prescriptions, death certificates, medical certificates"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data seeding");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during data seeding",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Endpoint to clear seeded data
        /// Access: /DataSeeder/Clear
        /// WARNING: This will delete ALL applications data
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Clear()
        {
            // Safety check: Only allow in development
            if (!_environment.IsDevelopment())
            {
                return StatusCode(403, "Data clearing is only allowed in development environment");
            }

            try
            {
                _logger.LogWarning("Starting data clearing process...");

                // Remove all applications
                _context.HospitalAssistance.RemoveRange(_context.HospitalAssistance);
                _context.FuneralAssistance.RemoveRange(_context.FuneralAssistance);
                _context.OtherAssistance.RemoveRange(_context.OtherAssistance);
                await _context.SaveChangesAsync();

                // Clear uploaded files - using system's folder structure
                var storageFolders = new[]
                {
                    Path.Combine(_environment.WebRootPath, "Validimg"),
                    Path.Combine(_environment.WebRootPath, "HospitalAssistanceFileStorage"),
                    Path.Combine(_environment.WebRootPath, "FuneralAssistanceFileStorage"),
                    Path.Combine(_environment.WebRootPath, "OtherAssistanceFileStorage")
                };

                foreach (var dirPath in storageFolders)
                {
                    if (Directory.Exists(dirPath))
                    {
                        foreach (var file in Directory.GetFiles(dirPath, "*.enc"))
                        {
                            System.IO.File.Delete(file);
                        }
                    }
                }

                _logger.LogWarning("Data clearing completed");

                return Ok(new
                {
                    success = true,
                    message = "Successfully cleared all application data and encrypted files"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data clearing");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during data clearing",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get seeder statistics
        /// Access: /DataSeeder/Stats
        /// </summary>
        [HttpGet]
        public IActionResult Stats()
        {
            try
            {
                int hospitalCount = _context.HospitalAssistance.Count();
                int funeralCount = _context.FuneralAssistance.Count();
                int otherCount = _context.OtherAssistance.Count();
                int totalCount = hospitalCount + funeralCount + otherCount;

                return Ok(new
                {
                    success = true,
                    statistics = new
                    {
                        total = totalCount,
                        hospitalAssistance = hospitalCount,
                        funeralAssistance = funeralCount,
                        otherAssistance = otherCount,
                        environment = _environment.EnvironmentName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving statistics",
                    error = ex.Message
                });
            }
        }
    }
}
