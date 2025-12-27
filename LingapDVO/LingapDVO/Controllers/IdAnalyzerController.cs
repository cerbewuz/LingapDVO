using Microsoft.AspNetCore.Mvc;
using LingapDVO.Services;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LingapDVO.Controllers
{
    /// <summary>
    /// API Controller for ID Analyzer v2 operations
    /// Base URL: https://api2.idanalyzer.com
    ///
    /// Endpoints:
    /// - POST /api/IdAnalyzer/saveSelfie - Save selfie image to disk
    /// - POST /api/IdAnalyzer/scan - Standard ID Scan
    /// - POST /api/IdAnalyzer/face - Face Verification
    /// - GET /api/IdAnalyzer/health - Health Check
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class IdAnalyzerController : ControllerBase
    {
        private readonly IVerificationService _verificationService;
        private readonly ILogger<IdAnalyzerController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly AesEncryptionHelper _encryptionHelper;
        private readonly LingapDVO.Services.ApplicationDbContext _context;

        public IdAnalyzerController(
            IVerificationService verificationService,
            ILogger<IdAnalyzerController> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            LingapDVO.Services.ApplicationDbContext context)
        {
            _verificationService = verificationService;
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
            _encryptionHelper = new AesEncryptionHelper(configuration);
            _context = context;
        }

        // ═══════════════════════════════════════════════════════════════════════════
        //                     AES-256 ENCRYPTION HELPER CLASS
        //          Secure AES-256 Implementation using Configuration
        //          Encrypts data before sending to ID Analyzer API
        // ═══════════════════════════════════════════════════════════════════════════
        private class AesEncryptionHelper
        {
            private readonly byte[] _aesKey;

            public AesEncryptionHelper(IConfiguration configuration)
            {
                string keyHex = configuration["Security:AesEncryption:Key"]
                    ?? throw new InvalidOperationException("AES encryption key not found in configuration");

                keyHex = keyHex.Trim().Replace(" ", "").Replace("-", "").Replace(":", "");

                if (string.IsNullOrWhiteSpace(keyHex))
                    throw new InvalidOperationException("AES encryption key is empty");

                _aesKey = SafeConvertHexStringToByteArray(keyHex);

                if (_aesKey.Length != 32)
                    throw new InvalidOperationException($"AES key must be 32 bytes (256 bits). Current: {_aesKey.Length} bytes");
            }

            private static byte[] SafeConvertHexStringToByteArray(string hex)
            {
                if (string.IsNullOrWhiteSpace(hex))
                    throw new ArgumentException("Hex string cannot be null or empty");

                hex = hex.Trim().Replace(" ", "").Replace("-", "").Replace(":", "");

                if (hex.Length % 2 != 0)
                    hex = "0" + hex;

                if (!System.Text.RegularExpressions.Regex.IsMatch(hex, @"^[0-9A-Fa-f]+$"))
                    throw new ArgumentException("Hex string contains invalid characters");

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
        }

        /// <summary>
        /// Save selfie image to /wwwroot/UsersImg directory
        /// This endpoint stores the selfie before sending to ID Analyzer API
        /// </summary>
        [HttpPost("saveSelfie")]
        public async Task<IActionResult> SaveSelfie([FromBody] SaveSelfieRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ImageData))
                {
                    return BadRequest(new { success = false, error = "Image data is required" });
                }

                // Remove data URL prefix if present
                var base64Data = RemoveDataUrlPrefix(request.ImageData);

                // Generate unique filename
                var fileName = $"selfie_{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                var usersImgPath = Path.Combine(_environment.WebRootPath, "UsersImg");

                // Ensure directory exists
                if (!Directory.Exists(usersImgPath))
                {
                    Directory.CreateDirectory(usersImgPath);
                    _logger.LogInformation("Created UsersImg directory: {Path}", usersImgPath);
                }

                var filePath = Path.Combine(usersImgPath, fileName);

                // Convert base64 to bytes and save
                var imageBytes = Convert.FromBase64String(base64Data);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                _logger.LogInformation("✅ Selfie saved successfully: {FileName} ({Size} bytes)", fileName, imageBytes.Length);

                return Ok(new
                {
                    success = true,
                    fileName = fileName,
                    filePath = $"/UsersImg/{fileName}",
                    fileSize = imageBytes.Length,
                    message = "Selfie saved successfully"
                });
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 format");
                return BadRequest(new { success = false, error = "Invalid image format" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving selfie");
                return StatusCode(500, new { success = false, error = "Failed to save selfie" });
            }
        }

        /// <summary>
        /// Save Valid ID image (front or back) as RAW FILE to /wwwroot/Validimg directory
        /// CRITICAL: This endpoint accepts FormData to preserve 100% of original file data including EXIF
        /// </summary>
        [HttpPost("saveValidIdFile")]
        public async Task<IActionResult> SaveValidIdFile([FromForm] IFormFile imageFile, [FromForm] bool isBack = false)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    return BadRequest(new { success = false, error = "Image file is required" });
                }

                // Generate unique filename with side indicator
                var sidePrefix = isBack ? "back" : "front";
                var extension = Path.GetExtension(imageFile.FileName).ToLower();
                if (string.IsNullOrEmpty(extension) || !new[] { ".jpg", ".jpeg", ".png" }.Contains(extension))
                {
                    extension = ".jpg";
                }
                var fileName = $"{sidePrefix}_id_{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                var validImgPath = Path.Combine(_environment.WebRootPath, "Validimg");

                // Ensure directory exists
                if (!Directory.Exists(validImgPath))
                {
                    Directory.CreateDirectory(validImgPath);
                    _logger.LogInformation("Created Validimg directory: {Path}", validImgPath);
                }

                var filePath = Path.Combine(validImgPath, fileName);

                // CRITICAL: Write raw bytes directly from IFormFile stream
                // This preserves 100% of the original file data including ALL EXIF metadata
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                var fileInfo = new FileInfo(filePath);
                _logger.LogInformation("✅ {Side} ID saved successfully (RAW FILE): {FileName} ({Size} bytes)",
                    isBack ? "BACK" : "FRONT", fileName, fileInfo.Length);
                _logger.LogInformation("   📁 Path: {Path}", filePath);
                _logger.LogInformation("   🔐 EXIF Preserved: 100% - Raw bytes from uploaded file");
                _logger.LogInformation("   📄 Original Name: {OriginalName}", imageFile.FileName);

                return Ok(new
                {
                    success = true,
                    fileName = fileName,
                    filePath = $"/Validimg/{fileName}",
                    fileSize = fileInfo.Length,
                    isBack = isBack,
                    originalFileName = imageFile.FileName,
                    message = $"{(isBack ? "Back" : "Front")} ID saved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving {Side} ID file", isBack ? "back" : "front");
                return StatusCode(500, new { success = false, error = "Failed to save ID image" });
            }
        }

        /// <summary>
        /// Save Valid ID image (front or back) to /wwwroot/Validimg directory
        /// This endpoint stores ID images before sending to ID Analyzer API
        /// CRITICAL: Saves raw bytes WITHOUT any re-encoding to preserve EXIF metadata
        /// </summary>
        [HttpPost("saveValidId")]
        public async Task<IActionResult> SaveValidId([FromBody] SaveValidIdRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ImageData))
                {
                    return BadRequest(new { success = false, error = "Image data is required" });
                }

                // Remove data URL prefix if present
                var base64Data = RemoveDataUrlPrefix(request.ImageData);

                // Generate unique filename with side indicator
                var sidePrefix = request.IsBack ? "back" : "front";
                var fileName = $"{sidePrefix}_id_{Guid.NewGuid():N}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                var validImgPath = Path.Combine(_environment.WebRootPath, "Validimg");

                // Ensure directory exists
                if (!Directory.Exists(validImgPath))
                {
                    Directory.CreateDirectory(validImgPath);
                    _logger.LogInformation("Created Validimg directory: {Path}", validImgPath);
                }

                var filePath = Path.Combine(validImgPath, fileName);

                // Convert base64 to bytes and save
                // CRITICAL: This preserves the EXACT original bytes from the uploaded file
                var imageBytes = Convert.FromBase64String(base64Data);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                _logger.LogInformation("✅ {Side} ID saved successfully: {FileName} ({Size} bytes)",
                    request.IsBack ? "BACK" : "FRONT", fileName, imageBytes.Length);
                _logger.LogInformation("   📁 Path: {Path}", filePath);
                _logger.LogInformation("   🔐 EXIF Preserved: Original bytes written to disk");

                return Ok(new
                {
                    success = true,
                    fileName = fileName,
                    filePath = $"/Validimg/{fileName}",
                    fileSize = imageBytes.Length,
                    isBack = request.IsBack,
                    message = $"{(request.IsBack ? "Back" : "Front")} ID saved successfully"
                });
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 format for {Side} ID", request.IsBack ? "back" : "front");
                return BadRequest(new { success = false, error = "Invalid image format" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving {Side} ID", request.IsBack ? "back" : "front");
                return StatusCode(500, new { success = false, error = "Failed to save ID image" });
            }
        }

        /// <summary>
        /// Standard ID Scan - Scans ID document, extracts data, and optionally matches face
        /// NEW FLOW:
        /// 1. Receive files directly from frontend
        /// 2. Apply AES-256 encryption
        /// 3. Send encrypted data to ID Analyzer API
        /// 4. ONLY if decision = "accept" → Save original files to storage (Validimg/UsersImg)
        /// This ensures only verified files are stored permanently
        /// API Reference: https://developer.idanalyzer.com/reference/post-scan
        /// </summary>
        [HttpPost("scan")]
        public async Task<IActionResult> ScanId([FromForm] IFormFile? documentFile, [FromForm] IFormFile? backFile, [FromForm] string? selfieFileName, [FromForm] int? userId)
        {
            try
            {
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");
                _logger.LogInformation("📥 SCAN REQUEST RECEIVED");
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");
                _logger.LogInformation("   Front ID File: {FrontId}", documentFile?.FileName ?? "NULL");
                _logger.LogInformation("   Back ID File: {BackId}", backFile?.FileName ?? "NULL");
                _logger.LogInformation("   Selfie Filename: {Selfie}", selfieFileName ?? "NULL");
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");

                // ═══════════════════════════════════════════════════════════════
                // STEP 1: Read uploaded files and convert to base64
                // CRITICAL: Preserve 100% of original file data including EXIF
                // ═══════════════════════════════════════════════════════════════
                string? documentBase64 = null;
                byte[]? frontIdBytes = null;

                if (documentFile != null && documentFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await documentFile.CopyToAsync(memoryStream);
                        frontIdBytes = memoryStream.ToArray();
                        documentBase64 = Convert.ToBase64String(frontIdBytes);
                    }

                    _logger.LogInformation("✅ Front ID received:");
                    _logger.LogInformation("   - Filename: {FileName}", documentFile.FileName);
                    _logger.LogInformation("   - Size: {Size} bytes ({KB} KB)", frontIdBytes.Length, Math.Round(frontIdBytes.Length / 1024.0, 2));
                    _logger.LogInformation("   - 🔐 Original bytes: PRESERVED from upload");
                    _logger.LogInformation("   - 📝 EXIF metadata: INTACT");
                }
                else
                {
                    return BadRequest(new { success = false, error = "Front ID file is required" });
                }

                // Read Back ID (REQUIRED)
                string? backBase64 = null;
                byte[]? backIdBytes = null;

                if (backFile != null && backFile.Length > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await backFile.CopyToAsync(memoryStream);
                        backIdBytes = memoryStream.ToArray();
                        backBase64 = Convert.ToBase64String(backIdBytes);
                    }

                    _logger.LogInformation("✅ Back ID received:");
                    _logger.LogInformation("   - Filename: {FileName}", backFile.FileName);
                    _logger.LogInformation("   - Size: {Size} bytes ({KB} KB)", backIdBytes.Length, Math.Round(backIdBytes.Length / 1024.0, 2));
                    _logger.LogInformation("   - 🔐 Original bytes: PRESERVED from upload");
                    _logger.LogInformation("   - 📝 EXIF metadata: INTACT");
                }
                else
                {
                    return BadRequest(new { success = false, error = "Back ID file is required" });
                }

                // Read Selfie from UsersImg folder (if filename provided)
                string? faceBase64 = null;
                byte[]? selfieBytes = null;

                if (!string.IsNullOrEmpty(selfieFileName))
                {
                    var usersImgPath = Path.Combine(_environment.WebRootPath, "UsersImg");
                    var selfiePath = Path.Combine(usersImgPath, selfieFileName);

                    if (System.IO.File.Exists(selfiePath))
                    {
                        selfieBytes = await System.IO.File.ReadAllBytesAsync(selfiePath);
                        faceBase64 = Convert.ToBase64String(selfieBytes);

                        _logger.LogInformation("✅ Selfie loaded from disk:");
                        _logger.LogInformation("   - File: {FileName}", selfieFileName);
                        _logger.LogInformation("   - Size: {Size} bytes ({KB} KB)", selfieBytes.Length, Math.Round(selfieBytes.Length / 1024.0, 2));
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Selfie file not found: {Path}", selfiePath);
                    }
                }

                // ═══════════════════════════════════════════════════════════════
                // STEP 2: Send UNENCRYPTED data to ID Analyzer API
                // IMPORTANT: ID Analyzer needs actual image data, not encrypted data
                // The API must be able to analyze the actual images
                // ═══════════════════════════════════════════════════════════════
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogInformation("📤 Sending UNENCRYPTED data to ID Analyzer API:");
                _logger.LogInformation("   - Document: {HasDoc} ({DocLen} chars)", !string.IsNullOrEmpty(documentBase64), documentBase64?.Length ?? 0);
                _logger.LogInformation("   - Face: {HasFace} ({FaceLen} chars)", !string.IsNullOrEmpty(faceBase64), faceBase64?.Length ?? 0);
                _logger.LogInformation("   - Back: {HasBack} ({BackLen} chars)", !string.IsNullOrEmpty(backBase64), backBase64?.Length ?? 0);
                _logger.LogInformation("   - ⚠️ NOTE: Images sent as-is (unencrypted) for API analysis");
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var result = await _verificationService.ScanIdAsync(documentBase64!, faceBase64, backBase64);

                // Concatenate Address 1 and Address 2 into full address (needed for database storage)
                var fullAddress = string.Join(", ", new[] { result.Address, result.Address2 }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                // ═══════════════════════════════════════════════════════════════
                // STEP 4: ONLY if decision = "accept" → Save files and data to storage
                // This ensures only verified/accepted files and data are stored permanently
                // ═══════════════════════════════════════════════════════════════
                // REFACTORED: Do NOT automatically save to database
                // The scan endpoint now ONLY returns extracted data for user review
                // Database save happens later when user clicks "Verify Account" button
                _logger.LogInformation("✅ API Decision: {Decision} - Data extracted successfully", result.Decision ?? "UNKNOWN");
                _logger.LogInformation("   ⚠️ NOTE: Files and data NOT saved yet - waiting for user confirmation");
                _logger.LogInformation("   → User must review data and click 'Verify Account' to complete verification");

                if (!result.Success)
                {
                    _logger.LogWarning("ID scan failed: {Error}", result.ErrorMessage);
                    return Ok(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }

                return Ok(new
                {
                    success = true,
                    decision = result.Decision,
                    transactionId = result.TransactionId,
                    profileId = result.ProfileId,
                    reviewScore = result.ReviewScore,
                    rejectScore = result.RejectScore,
                    warnings = result.Warnings,
                    missingFields = result.MissingFields,
                    data = new
                    {
                        // Personal Info (CRITICAL for name validation)
                        firstName = result.FirstName,
                        middleName = result.MiddleName,
                        lastName = result.LastName,
                        suffix = result.Suffix,
                        fullName = result.FullName,
                        sex = result.Sex,
                        civilStatus = result.CivilStatus,  // From back ID (National ID only)
                        dateOfBirth = result.DateOfBirth,
                        age = result.Age,
                        dobDay = result.DobDay,
                        dobMonth = result.DobMonth,
                        dobYear = result.DobYear,

                        // Nationality
                        nationality = result.Nationality,
                        nationalityIso2 = result.NationalityIso2,
                        nationalityIso3 = result.NationalityIso3,

                        // Document Info
                        documentNumber = result.DocumentNumber,
                        documentType = result.DocumentType,
                        documentName = result.DocumentName,
                        documentSide = result.DocumentSide,
                        issueDate = result.IssueDate,
                        expiryDate = result.ExpiryDate,
                        internalId = result.InternalId,
                        backSideId = result.BackSideId,
                        reverseId = result.ReverseId,

                        // Address (CRITICAL for Davao City validation)
                        address1 = result.Address,
                        address2 = result.Address2,
                        address = fullAddress,
                        city = result.City,
                        state = result.State,
                        postalCode = result.PostalCode,
                        country = result.Country,
                        countryIso2 = result.CountryIso2,
                        countryIso3 = result.CountryIso3,

                        // Verification
                        verificationPassed = result.VerificationPassed,
                        faceMatch = result.FaceMatch,
                        faceSimilarity = result.FaceSimilarity,
                        faceConfidence = result.FaceConfidence,
                        faceSimilarityPercentage = result.SimilarityPercentage
                    },
                    validation = new
                    {
                        // Davao City Residence Validation
                        isDavaoCityResident = result.IsDavaoCityResident,
                        residenceValidationMessage = result.ResidenceValidationMessage,

                        // Name Validation
                        nameMatchesRegistration = result.NameMatchesRegistration,
                        nameValidationMessage = result.NameValidationMessage
                    },
                    outputImage = new
                    {
                        front = result.FrontImageHash,
                        face = result.FaceImageHash
                    },
                    metadata = new
                    {
                        createdAt = result.CreatedAt,
                        updatedAt = result.UpdatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ID scan request");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        /// <summary>
        /// Face Verification - Compares selfie with reference image (from ID)
        /// API Reference: https://developer.idanalyzer.com/reference/post-face-2
        /// </summary>
        [HttpPost("face")]
        public async Task<IActionResult> VerifyFace([FromBody] IdAnalyzerFaceRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Face) || string.IsNullOrEmpty(request.Reference))
                {
                    return BadRequest(new { success = false, error = "Both face and reference images are required" });
                }

                var faceBase64 = RemoveDataUrlPrefix(request.Face);
                var referenceBase64 = RemoveDataUrlPrefix(request.Reference);

                _logger.LogInformation("Processing face verification request");

                var result = await _verificationService.VerifyFaceAsync(faceBase64, referenceBase64);

                if (!result.Success)
                {
                    _logger.LogWarning("Face verification failed: {Error}", result.ErrorMessage);
                    return Ok(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }

                return Ok(new
                {
                    success = true,
                    isMatch = result.FaceMatch,
                    similarity = result.FaceSimilarity,
                    similarityPercentage = result.SimilarityPercentage,
                    confidence = result.FaceConfidence,
                    decision = result.Decision
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing face verification request");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                service = "ID Analyzer API v2",
                baseUrl = "https://api2.idanalyzer.com"
            });
        }

        /// <summary>
        /// Get verification status for the current user
        /// Used for polling to check if admin has updated decision from "review" to "accept"/"reject"
        /// </summary>
        [HttpGet("verificationStatus")]
        public async Task<IActionResult> GetVerificationStatus([FromQuery] int userId)
        {
            try
            {
                // Get verification record from database
                var dbContext = HttpContext.RequestServices.GetService<LingapDVO.Services.ApplicationDbContext>();
                if (dbContext == null)
                {
                    return StatusCode(500, new { success = false, error = "Database context not available" });
                }

                var verification = await dbContext.Verifyaccount
                    .Where(v => v.UserId == userId)
                    .OrderByDescending(v => v.Id)
                    .FirstOrDefaultAsync();

                if (verification == null)
                {
                    return NotFound(new { success = false, error = "No verification found for user" });
                }

                return Ok(new
                {
                    success = true,
                    decision = verification.decision ?? "pending"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification status for user {UserId}", userId);
                return StatusCode(500, new { success = false, error = "Failed to get verification status" });
            }
        }

        private string RemoveDataUrlPrefix(string dataUrl)
        {
            if (string.IsNullOrEmpty(dataUrl))
                return dataUrl;

            if (dataUrl.StartsWith("data:"))
            {
                var commaIndex = dataUrl.IndexOf(',');
                if (commaIndex >= 0)
                    return dataUrl.Substring(commaIndex + 1);
            }

            return dataUrl;
        }
    }

    /// <summary>
    /// Request model for saving selfie
    /// </summary>
    public class SaveSelfieRequest
    {
        public string ImageData { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for saving Valid ID images (front/back)
    /// </summary>
    public class SaveValidIdRequest
    {
        public string ImageData { get; set; } = string.Empty;
        public bool IsBack { get; set; } = false;
    }

    /// <summary>
    /// Request model for ID scan (IdAnalyzer)
    /// Updated to support stored selfie files
    /// </summary>
    public class IdAnalyzerScanRequest
    {
        public string DocumentImage { get; set; } = string.Empty;
        public string? FaceImage { get; set; }          // Deprecated: Use SelfieFileName instead
        public string? SelfieFileName { get; set; }     // NEW: Filename of stored selfie in /wwwroot/UsersImg
        public string? BackImage { get; set; }
    }

    /// <summary>
    /// Request model for face verification (IdAnalyzer)
    /// </summary>
    public class IdAnalyzerFaceRequest
    {
        public string Face { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for scan endpoint with file-based approach
    /// Uses filenames instead of base64 data to preserve original files
    /// </summary>
    public class ScanRequest
    {
        public string? FrontIdFileName { get; set; }
        public string? BackIdFileName { get; set; }
        public string? SelfieFileName { get; set; }
    }
}
