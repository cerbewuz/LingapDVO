using Microsoft.AspNetCore.Mvc;
using LingapDVO.Services;
using System.Text.Json;

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

        public IdAnalyzerController(
            IVerificationService verificationService,
            ILogger<IdAnalyzerController> logger,
            IWebHostEnvironment environment)
        {
            _verificationService = verificationService;
            _logger = logger;
            _environment = environment;
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
        /// Standard ID Scan - Scans ID document, extracts data, and optionally matches face
        /// Now reads selfie from /wwwroot/UsersImg and converts to base64 before sending to API
        /// API Reference: https://developer.idanalyzer.com/reference/post-scan
        /// </summary>
        [HttpPost("scan")]
        public async Task<IActionResult> ScanId([FromBody] IdAnalyzerScanRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.DocumentImage))
                {
                    return BadRequest(new { success = false, error = "Document image is required" });
                }

                var documentBase64 = RemoveDataUrlPrefix(request.DocumentImage);
                var backBase64 = !string.IsNullOrEmpty(request.BackImage) ? RemoveDataUrlPrefix(request.BackImage) : null;

                // CHANGED: Read selfie from stored file instead of receiving base64 directly
                string? faceBase64 = null;
                string? selfieFilePath = null;

                if (!string.IsNullOrEmpty(request.SelfieFileName))
                {
                    _logger.LogInformation("📸 Reading selfie from file: {FileName}", request.SelfieFileName);

                    var usersImgPath = Path.Combine(_environment.WebRootPath, "UsersImg");
                    selfieFilePath = Path.Combine(usersImgPath, request.SelfieFileName);

                    if (System.IO.File.Exists(selfieFilePath))
                    {
                        // Read file and convert to base64
                        var imageBytes = await System.IO.File.ReadAllBytesAsync(selfieFilePath);
                        faceBase64 = Convert.ToBase64String(imageBytes);

                        _logger.LogInformation("✅ Selfie loaded from disk and converted to BASE64:");
                        _logger.LogInformation("   - File: {FileName}", request.SelfieFileName);
                        _logger.LogInformation("   - Original Size: {Size} bytes", imageBytes.Length);
                        _logger.LogInformation("   - Base64 Length: {Base64Length} chars", faceBase64.Length);
                        _logger.LogInformation("   - Base64 Preview: {Preview}...", faceBase64.Substring(0, Math.Min(50, faceBase64.Length)));
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Selfie file not found: {FilePath}", selfieFilePath);
                        return BadRequest(new { success = false, error = $"Selfie file not found: {request.SelfieFileName}" });
                    }
                }
                else if (!string.IsNullOrEmpty(request.FaceImage))
                {
                    // Fallback: Accept base64 directly (for backward compatibility)
                    faceBase64 = RemoveDataUrlPrefix(request.FaceImage);
                    _logger.LogInformation("📸 Using selfie from request body (base64 fallback)");
                    _logger.LogInformation("   - Base64 Length: {Base64Length} chars", faceBase64.Length);
                }

                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogInformation("📤 Sending to ID Analyzer API:");
                _logger.LogInformation("   - Document: {HasDoc} ({DocLen} chars)", !string.IsNullOrEmpty(documentBase64), documentBase64?.Length ?? 0);
                _logger.LogInformation("   - Face (BASE64): {HasFace} ({FaceLen} chars)", !string.IsNullOrEmpty(faceBase64), faceBase64?.Length ?? 0);
                _logger.LogInformation("   - Back: {HasBack} ({BackLen} chars)", !string.IsNullOrEmpty(backBase64), backBase64?.Length ?? 0);
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var result = await _verificationService.ScanIdAsync(documentBase64, faceBase64, backBase64);

                // Clean up selfie file after processing (optional)
                if (!string.IsNullOrEmpty(selfieFilePath) && System.IO.File.Exists(selfieFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(selfieFilePath);
                        _logger.LogInformation("🗑️ Deleted selfie file after processing: {FileName}", request.SelfieFileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete selfie file: {FileName}", request.SelfieFileName);
                    }
                }

                if (!result.Success)
                {
                    _logger.LogWarning("ID scan failed: {Error}", result.ErrorMessage);
                    return Ok(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }

                // Concatenate Address 1 and Address 2 into full address
                var fullAddress = string.Join(", ", new[] { result.Address, result.Address2 }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

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
}
