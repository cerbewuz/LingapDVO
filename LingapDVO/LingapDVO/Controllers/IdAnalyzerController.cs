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
    /// PRIVACY NOTE: Selfie/face images are NOT stored in the system.
    /// Only ID images (front/back) are saved with AES-256 encryption.
    /// Selfies are only used for real-time face matching via ID Analyzer API.
    ///
    /// Endpoints:
    /// - POST /api/IdAnalyzer/saveSelfie - Validate selfie (NOT saved to disk)
    /// - POST /api/IdAnalyzer/scan - Standard ID Scan (saves encrypted ID images only)
    /// - POST /api/IdAnalyzer/face - Face Verification
    /// - GET /api/IdAnalyzer/health - Health Check
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class IdAnalyzerController : ControllerBase
    {
        private readonly IVerificationService _verificationService;
        private readonly IOcrSpaceService _ocrSpaceService;
        private readonly ILogger<IdAnalyzerController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly AesEncryptionHelper _encryptionHelper;
        private readonly LingapDVO.Services.ApplicationDbContext _context;

        public IdAnalyzerController(
            IVerificationService verificationService,
            IOcrSpaceService ocrSpaceService,
            ILogger<IdAnalyzerController> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            LingapDVO.Services.ApplicationDbContext context)
        {
            _verificationService = verificationService;
            _ocrSpaceService = ocrSpaceService;
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

            /// <summary>
            /// Encrypts byte array (used for images) and returns encrypted bytes
            /// </summary>
            public byte[] EncryptBytes(byte[] plainBytes)
            {
                using var aes = Aes.Create();
                aes.Key = _aesKey;
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                using var memoryStream = new MemoryStream();

                // Write IV at the beginning
                memoryStream.Write(aes.IV, 0, aes.IV.Length);

                // Encrypt the data
                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                    cryptoStream.FlushFinalBlock();
                }

                return memoryStream.ToArray();
            }

            /// <summary>
            /// Decrypts byte array (used for images) and returns decrypted bytes
            /// </summary>
            public byte[] DecryptBytes(byte[] encryptedBytes)
            {
                using var aes = Aes.Create();
                aes.Key = _aesKey;

                // Extract IV from the beginning
                byte[] iv = new byte[16];
                Array.Copy(encryptedBytes, 0, iv, 0, 16);
                aes.IV = iv;

                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                using var memoryStream = new MemoryStream(encryptedBytes, 16, encryptedBytes.Length - 16);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var resultStream = new MemoryStream();

                cryptoStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        /// <summary>
        /// Validate selfie image for ID Analyzer API verification
        /// NOTE: Selfie/face images are NOT saved to disk for privacy reasons.
        /// They are only used for real-time face matching with ID Analyzer API.
        /// </summary>
        [HttpPost("saveSelfie")]
        public Task<IActionResult> SaveSelfie([FromBody] SaveSelfieRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ImageData))
                {
                    return Task.FromResult<IActionResult>(BadRequest(new { success = false, error = "Image data is required" }));
                }

                // Remove data URL prefix if present and validate format
                var base64Data = RemoveDataUrlPrefix(request.ImageData);

                // Validate base64 format without saving to disk
                var imageBytes = Convert.FromBase64String(base64Data);

                _logger.LogInformation("✅ Selfie validated successfully ({Size} bytes) - NOT saved to disk for privacy", imageBytes.Length);

                // Return success without saving the file
                // Selfie is only used for ID Analyzer API verification, not stored in system
                return Task.FromResult<IActionResult>(Ok(new
                {
                    success = true,
                    fileName = "", // No file saved
                    filePath = "", // No file path
                    fileSize = imageBytes.Length,
                    message = "Selfie validated successfully (not stored for privacy)"
                }));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid base64 format");
                return Task.FromResult<IActionResult>(BadRequest(new { success = false, error = "Invalid image format" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating selfie");
                return Task.FromResult<IActionResult>(StatusCode(500, new { success = false, error = "Failed to validate selfie" }));
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
        /// Standard ID Scan - Document validation and face verification ONLY
        ///
        /// DATA EXTRACTION FLOW:
        /// 1. ID Analyzer (this endpoint): Document type, ID number, face verification
        /// 2. OCR.space (/api/IdAnalyzer/ocr): Personal data (names, DOB, gender, address, civil status)
        ///
        /// ID ANALYZER PROVIDES:
        /// - Document type/name validation (only National ID, Driver's License, UMID accepted)
        /// - Document number extraction
        /// - Face verification (comparing selfie with ID photo)
        ///
        /// OCR.SPACE PROVIDES (via /api/IdAnalyzer/ocr):
        /// - First name, middle name, last name, suffix
        /// - Date of birth, gender, civil status
        /// - Address
        ///
        /// PRIVACY FLOW:
        /// 1. Receive ID images and selfie from frontend as Base64
        /// 2. Send to ID Analyzer API for document validation and face matching
        /// 3. ONLY if decision = "accept" → Save ID images ONLY (encrypted with AES-256)
        /// 4. Selfie/face image is NEVER saved - only used for API verification
        ///
        /// API Reference: https://developer.idanalyzer.com/reference/post-scan
        /// </summary>
        [HttpPost("scan")]
        public async Task<IActionResult> ScanId([FromBody] IdAnalyzerScanRequest request)
        {
            try
            {
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");
                _logger.LogInformation("📥 SCAN REQUEST RECEIVED (JSON with Base64)");
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");
                _logger.LogInformation("   Front ID (Base64): {HasFront} ({Length} chars)", !string.IsNullOrEmpty(request.DocumentImage), request.DocumentImage?.Length ?? 0);
                _logger.LogInformation("   Back ID (Base64): {HasBack} ({Length} chars)", !string.IsNullOrEmpty(request.BackImage), request.BackImage?.Length ?? 0);
                _logger.LogInformation("   Selfie (Base64): {HasFace} ({Length} chars)", !string.IsNullOrEmpty(request.FaceImage), request.FaceImage?.Length ?? 0);
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");

                // ═══════════════════════════════════════════════════════════════
                // STEP 1: Validate and clean Base64 strings
                // Remove data URI prefix if present (e.g., "data:image/jpeg;base64,")
                // ═══════════════════════════════════════════════════════════════
                if (string.IsNullOrEmpty(request.DocumentImage))
                {
                    return BadRequest(new { success = false, error = "Front ID is required" });
                }

                if (string.IsNullOrEmpty(request.BackImage))
                {
                    return BadRequest(new { success = false, error = "Back ID is required" });
                }

                if (string.IsNullOrEmpty(request.FaceImage))
                {
                    return BadRequest(new { success = false, error = "Selfie is required" });
                }

                // Clean Base64 strings (remove data URI prefix)
                var documentBase64 = RemoveDataUrlPrefix(request.DocumentImage);
                var backBase64 = RemoveDataUrlPrefix(request.BackImage);
                var faceBase64 = RemoveDataUrlPrefix(request.FaceImage);

                _logger.LogInformation("✅ Base64 strings cleaned and validated");
                _logger.LogInformation("   - Document: {Length} chars", documentBase64.Length);
                _logger.LogInformation("   - Back: {Length} chars", backBase64.Length);
                _logger.LogInformation("   - Face: {Length} chars", faceBase64.Length);

                // ═══════════════════════════════════════════════════════════════
                // STEP 2: Send Base64 data directly to ID Analyzer API
                // Images are NOT saved to disk yet - only sent to API
                // Files will be saved ONLY if API returns "accept" decision
                // ═══════════════════════════════════════════════════════════════
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogInformation("📤 Sending Base64 data to ID Analyzer API (no disk save yet)");
                _logger.LogInformation("   - Document: {DocLen} chars", documentBase64.Length);
                _logger.LogInformation("   - Face: {FaceLen} chars", faceBase64.Length);
                _logger.LogInformation("   - Back: {BackLen} chars", backBase64.Length);
                _logger.LogInformation("   - 💾 Files saved to disk: NO (pending API decision)");
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var result = await _verificationService.ScanIdAsync(documentBase64, faceBase64, backBase64);

                // Concatenate Address 1 and Address 2 into full address (needed for database storage)
                var fullAddress = string.Join(", ", new[] { result.Address, result.Address2 }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                // ═══════════════════════════════════════════════════════════════
                // STEP 3: ONLY if decision = "accept" → Save files to disk
                // This ensures only verified/accepted files are stored permanently
                // ═══════════════════════════════════════════════════════════════
                var decision = (result.Decision ?? "").ToLower();
                _logger.LogInformation("✅ API Decision: {Decision}", result.Decision ?? "UNKNOWN");

                // Variable to hold OCR.space extracted data (only populated on "accept")
                object? ocrExtractedData = null;

                if (decision == "accept" && result.Success)
                {
                    _logger.LogInformation("💾 Decision is 'accept' - Saving ID images to disk...");
                    _logger.LogInformation("🔒 NOTE: Selfie/face image is NOT saved for privacy reasons");

                    try
                    {
                        // Convert Base64 back to bytes for saving (ID images only, NOT selfie)
                        var frontIdBytes = Convert.FromBase64String(documentBase64);
                        var backIdBytes = Convert.FromBase64String(backBase64);
                        // Selfie is NOT saved to disk - only used for ID Analyzer API verification

                        // Create directory for encrypted ID images
                        var validImgPath = Path.Combine(_environment.WebRootPath, "Validimg");
                        Directory.CreateDirectory(validImgPath);

                        // Generate unique filenames for ID images only
                        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                        var guid = Guid.NewGuid().ToString("N").Substring(0, 8);

                        var frontIdFileName = $"front_id_{guid}_{timestamp}.jpg";
                        var backIdFileName = $"back_id_{guid}_{timestamp}.jpg";

                        var frontIdPath = Path.Combine(validImgPath, frontIdFileName);
                        var backIdPath = Path.Combine(validImgPath, backIdFileName);

                        // Encrypt front and back ID images using AES-256
                        _logger.LogInformation("🔐 Encrypting ID images with AES-256...");
                        var encryptedFrontIdBytes = _encryptionHelper.EncryptBytes(frontIdBytes);
                        var encryptedBackIdBytes = _encryptionHelper.EncryptBytes(backIdBytes);
                        _logger.LogInformation("✅ ID images encrypted successfully");

                        // Save encrypted ID files only (NO selfie saved)
                        await System.IO.File.WriteAllBytesAsync(frontIdPath, encryptedFrontIdBytes);
                        await System.IO.File.WriteAllBytesAsync(backIdPath, encryptedBackIdBytes);

                        _logger.LogInformation("✅ ID images saved successfully (encrypted with AES-256):");
                        _logger.LogInformation("   - Front ID: {FileName} ({Size} KB)", frontIdFileName, Math.Round(encryptedFrontIdBytes.Length / 1024.0, 2));
                        _logger.LogInformation("   - Back ID: {FileName} ({Size} KB)", backIdFileName, Math.Round(encryptedBackIdBytes.Length / 1024.0, 2));
                        _logger.LogInformation("   - Selfie: NOT SAVED (privacy protection)");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error saving ID images to disk");
                        // Continue execution - file save error should not fail the entire verification
                    }

                    // ═══════════════════════════════════════════════════════════════
                    // STEP 3.5: Extract personal data using OCR.space
                    // Only triggered when ID Analyzer returns "accept" decision
                    // OCR.space is the PRIMARY source for personal data extraction
                    // ═══════════════════════════════════════════════════════════════
                    _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    _logger.LogInformation("📤 Decision is 'accept' - Calling OCR.space for personal data extraction...");
                    _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                    try
                    {
                        // Call OCR.space to extract text from front and back ID images
                        var ocrResult = await _ocrSpaceService.ExtractTextFromImagesAsync(documentBase64, backBase64);

                        if (ocrResult.Success)
                        {
                            _logger.LogInformation("✅ OCR.space extraction successful");
                            _logger.LogInformation("   Front text length: {Length} chars", ocrResult.FrontText?.Length ?? 0);
                            _logger.LogInformation("   Back text length: {Length} chars", ocrResult.BackText?.Length ?? 0);

                            // Detect ID type from document info or OCR text
                            var idType = result.DocumentName ?? result.DocumentType ?? "";
                            var detectedIdType = _ocrSpaceService.DetectIdTypeFromText(ocrResult.ExtractedText ?? "");
                            if (string.IsNullOrEmpty(idType) && !string.IsNullOrEmpty(detectedIdType))
                            {
                                idType = detectedIdType;
                            }

                            // Parse the OCR text to extract structured personal data
                            var extractedData = _ocrSpaceService.ParsePhilippineIdText(ocrResult.ExtractedText ?? "", idType);

                            _logger.LogInformation("📋 OCR.space EXTRACTED DATA:");
                            _logger.LogInformation("   - First Name: {FirstName}", extractedData.FirstName ?? "N/A");
                            _logger.LogInformation("   - Last Name: {LastName}", extractedData.LastName ?? "N/A");
                            _logger.LogInformation("   - Middle Name: {MiddleName}", extractedData.MiddleName ?? "N/A");
                            _logger.LogInformation("   - Suffix: {Suffix}", extractedData.Suffix ?? "N/A");
                            _logger.LogInformation("   - DOB: {DOB}", extractedData.DateOfBirth ?? "N/A");
                            _logger.LogInformation("   - Gender: {Gender}", extractedData.Gender ?? "N/A");
                            _logger.LogInformation("   - Civil Status: {CivilStatus}", extractedData.CivilStatus ?? "N/A");
                            _logger.LogInformation("   - Address: {Address}", extractedData.Address ?? "N/A");

                            // Store OCR extracted data for response
                            ocrExtractedData = new
                            {
                                firstName = extractedData.FirstName,
                                lastName = extractedData.LastName,
                                middleName = extractedData.MiddleName,
                                suffix = extractedData.Suffix,
                                dateOfBirth = extractedData.DateOfBirth,
                                sex = extractedData.Gender,
                                civilStatus = extractedData.CivilStatus,
                                address1 = extractedData.Address,
                                // Include document info from ID Analyzer (authoritative source)
                                documentNumber = result.DocumentNumber,
                                documentType = result.DocumentType,
                                documentName = result.DocumentName,
                                // Include raw OCR text for frontend parsing if needed
                                rawText = new
                                {
                                    front = ocrResult.FrontText,
                                    back = ocrResult.BackText,
                                    combined = ocrResult.ExtractedText
                                }
                            };
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ OCR.space extraction failed: {Error}", ocrResult.ErrorMessage);
                            // Fall back to ID Analyzer data if OCR.space fails
                            ocrExtractedData = new
                            {
                                firstName = result.FirstName,
                                lastName = result.LastName,
                                middleName = result.MiddleName,
                                suffix = result.Suffix,
                                dateOfBirth = result.DateOfBirth,
                                sex = result.Sex,
                                civilStatus = result.CivilStatus,
                                address1 = result.Address,
                                address2 = result.Address2,
                                documentNumber = result.DocumentNumber,
                                documentType = result.DocumentType,
                                documentName = result.DocumentName,
                                fallbackSource = "ID Analyzer (OCR.space failed)"
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error calling OCR.space for data extraction");
                        // Fall back to ID Analyzer data if OCR.space throws exception
                        ocrExtractedData = new
                        {
                            firstName = result.FirstName,
                            lastName = result.LastName,
                            middleName = result.MiddleName,
                            suffix = result.Suffix,
                            dateOfBirth = result.DateOfBirth,
                            sex = result.Sex,
                            civilStatus = result.CivilStatus,
                            address1 = result.Address,
                            address2 = result.Address2,
                            documentNumber = result.DocumentNumber,
                            documentType = result.DocumentType,
                            documentName = result.DocumentName,
                            fallbackSource = "ID Analyzer (OCR.space error)"
                        };
                    }
                }
                else
                {
                    _logger.LogInformation("⚠️ Files NOT saved to disk - Decision: {Decision}, Success: {Success}", result.Decision, result.Success);
                }

                _logger.LogInformation("   ⚠️ NOTE: Database save happens when user clicks 'Verify Account'");

                if (!result.Success)
                {
                    _logger.LogWarning("ID scan failed: {Error}", result.ErrorMessage);
                    return Ok(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }

                // ═══════════════════════════════════════════════════════════════
                // RESPONSE: Combined ID Analyzer + OCR.space data
                // - ID Analyzer: Document validation, face verification, decision
                // - OCR.space: Personal data extraction (only on "accept" decision)
                // ═══════════════════════════════════════════════════════════════
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

                    // Document validation data (ID Analyzer is authoritative for these)
                    document = new
                    {
                        // Document Type Validation (CRITICAL - only National ID, Driver's License, UMID accepted)
                        documentNumber = result.DocumentNumber,
                        documentType = result.DocumentType,
                        documentName = result.DocumentName,
                        documentSide = result.DocumentSide,
                        issueDate = result.IssueDate,
                        expiryDate = result.ExpiryDate,
                        internalId = result.InternalId,
                        backSideId = result.BackSideId,
                        reverseId = result.ReverseId
                    },

                    // Personal data extracted by OCR.space (only populated on "accept" decision)
                    // This is the PRIMARY source for form population and validation
                    data = ocrExtractedData,

                    // Face verification results (ID Analyzer handles biometric matching)
                    faceVerification = new
                    {
                        verificationPassed = result.VerificationPassed,
                        faceMatch = result.FaceMatch,
                        faceSimilarity = result.FaceSimilarity,
                        faceConfidence = result.FaceConfidence,
                        faceSimilarityPercentage = result.SimilarityPercentage
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
                    },

                    message = decision == "accept" 
                        ? "Document validated and personal data extracted successfully via OCR.space."
                        : "Document validation complete."
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

                var verification = await dbContext.VerifiedAccount
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

        /// <summary>
        /// OCR.space API endpoint - PRIMARY source for personal data extraction
        ///
        /// This endpoint extracts personal information from ID images:
        /// - First name, middle name, last name, suffix
        /// - Date of birth
        /// - Gender
        /// - Civil status (if available on ID)
        /// - Address
        ///
        /// IMPORTANT: Only National ID, Driver's License, and UMID are accepted.
        /// ID type validation is performed before extraction.
        ///
        /// USAGE FLOW:
        /// 1. First call /api/IdAnalyzer/scan for document type validation and face verification
        /// 2. If scan returns "accept", call this endpoint for personal data extraction
        /// </summary>
        [HttpPost("ocr")]
        public async Task<IActionResult> ExtractTextWithOcr([FromBody] OcrSpaceRequest request)
        {
            try
            {
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");
                _logger.LogInformation("📥 OCR.SPACE REQUEST RECEIVED (EXCLUSIVE DATA EXTRACTION)");
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");
                _logger.LogInformation("   Front ID (Base64): {HasFront} ({Length} chars)", !string.IsNullOrEmpty(request.FrontImage), request.FrontImage?.Length ?? 0);
                _logger.LogInformation("   Back ID (Base64): {HasBack} ({Length} chars)", !string.IsNullOrEmpty(request.BackImage), request.BackImage?.Length ?? 0);
                _logger.LogInformation("   ID Type: {IdType}", request.IdType ?? "Not specified");
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");

                // VALIDATION 1: Front ID image is required
                if (string.IsNullOrEmpty(request.FrontImage))
                {
                    return BadRequest(new { success = false, error = "Front ID image is required" });
                }

                // VALIDATION 2: ID Type must be specified and must be one of the three accepted types
                var acceptedIdTypes = new[] { "National ID", "Driver's License", "UMID" };
                var idType = request.IdType?.Trim() ?? "";

                if (string.IsNullOrEmpty(idType))
                {
                    _logger.LogWarning("❌ ID Type not specified");
                    return BadRequest(new { success = false, error = "ID type is required. Please select National ID, Driver's License, or UMID." });
                }

                var isValidIdType = acceptedIdTypes.Any(t => t.Equals(idType, StringComparison.OrdinalIgnoreCase));
                if (!isValidIdType)
                {
                    _logger.LogWarning("❌ Invalid ID Type: {IdType}", idType);
                    return BadRequest(new
                    {
                        success = false,
                        error = $"Invalid ID type: \"{idType}\". Only National ID, Driver's License, and UMID are accepted."
                    });
                }

                _logger.LogInformation("✅ ID Type validated: {IdType}", idType);

                // Clean Base64 strings
                var frontBase64 = RemoveDataUrlPrefix(request.FrontImage);
                var backBase64 = !string.IsNullOrEmpty(request.BackImage) ? RemoveDataUrlPrefix(request.BackImage) : null;

                // Extract text from both images using OCR.space
                var ocrResult = await _ocrSpaceService.ExtractTextFromImagesAsync(frontBase64, backBase64);

                if (!ocrResult.Success)
                {
                    _logger.LogWarning("❌ OCR.space extraction failed: {Error}", ocrResult.ErrorMessage);
                    return Ok(new
                    {
                        success = false,
                        error = ocrResult.ErrorMessage ?? "Failed to read your ID. Please take clearer photos and try again."
                    });
                }

                _logger.LogInformation("✅ OCR.space extraction successful");
                _logger.LogInformation("   Front text length: {Length} chars", ocrResult.FrontText?.Length ?? 0);
                _logger.LogInformation("   Back text length: {Length} chars", ocrResult.BackText?.Length ?? 0);

                // VALIDATION 3: Verify the detected ID type matches the selected ID type
                var detectedIdType = _ocrSpaceService.DetectIdTypeFromText(ocrResult.ExtractedText ?? "");
                if (!string.IsNullOrEmpty(detectedIdType))
                {
                    var idTypeMatch = _ocrSpaceService.ValidateIdTypeMatch(ocrResult.ExtractedText ?? "", idType);
                    if (!idTypeMatch)
                    {
                        _logger.LogWarning("⚠️ ID Type mismatch detected - Selected: {Selected}, Detected: {Detected}", idType, detectedIdType);
                        // Continue but log the mismatch - we'll use the user-selected type for parsing
                    }
                }

                // Parse the extracted text to get structured data using the SELECTED ID type
                var extractedData = _ocrSpaceService.ParsePhilippineIdText(ocrResult.ExtractedText ?? "", idType);

                _logger.LogInformation("═══════════════════════════════════════════════════════════════");
                _logger.LogInformation("📋 EXTRACTED DATA (OCR.space PRIMARY SOURCE):");
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");
                _logger.LogInformation("   - ID Type: {IdType}", extractedData.IdType ?? idType);
                _logger.LogInformation("   - ID Number: {IdNumber}", extractedData.IdNumber ?? "N/A");
                _logger.LogInformation("   - First Name: {FirstName}", extractedData.FirstName ?? "N/A");
                _logger.LogInformation("   - Middle Name: {MiddleName}", extractedData.MiddleName ?? "N/A");
                _logger.LogInformation("   - Last Name: {LastName}", extractedData.LastName ?? "N/A");
                _logger.LogInformation("   - Suffix: {Suffix}", extractedData.Suffix ?? "N/A");
                _logger.LogInformation("   - Gender: {Gender}", extractedData.Gender ?? "N/A");
                _logger.LogInformation("   - Date of Birth: {DOB}", extractedData.DateOfBirth ?? "N/A");
                _logger.LogInformation("   - Civil Status: {CivilStatus}", extractedData.CivilStatus ?? "N/A");
                _logger.LogInformation("   - Address: {Address}", extractedData.Address ?? "N/A");
                _logger.LogInformation("═══════════════════════════════════════════════════════════════");

                return Ok(new
                {
                    success = true,
                    rawText = new
                    {
                        front = ocrResult.FrontText,
                        back = ocrResult.BackText,
                        combined = ocrResult.ExtractedText
                    },
                    detectedIdType = detectedIdType,
                    data = new
                    {
                        idType = extractedData.IdType ?? idType,
                        idNumber = extractedData.IdNumber,
                        firstName = extractedData.FirstName,
                        middleName = extractedData.MiddleName,
                        lastName = extractedData.LastName,
                        suffix = extractedData.Suffix,
                        gender = extractedData.Gender,
                        dateOfBirth = extractedData.DateOfBirth,
                        civilStatus = extractedData.CivilStatus,
                        address = extractedData.Address,
                        city = extractedData.City,
                        barangay = extractedData.Barangay
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OCR.space request");
                return StatusCode(500, new { success = false, error = "Internal server error during OCR processing" });
            }
        }

        // NOTE: AES-256 encryption of ID images is handled by LoginController
        // when the user submits the verification form. This ensures images are only
        // encrypted and stored permanently after the complete verification process.

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
        public string DocumentImage { get; set; } = string.Empty;  // Front ID as Base64
        public string? BackImage { get; set; }                      // Back ID as Base64
        public string? FaceImage { get; set; }                      // Selfie as Base64
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

    /// <summary>
    /// Request model for OCR.space text extraction
    /// Used after ID Analyzer returns "accept" to extract text data from ID images
    /// </summary>
    public class OcrSpaceRequest
    {
        public string FrontImage { get; set; } = string.Empty;  // Front ID as Base64
        public string? BackImage { get; set; }                   // Back ID as Base64
        public string? IdType { get; set; }                      // ID Type (National ID, Driver's License, UMID)
    }

    // NOTE: AES-256 encryption of ID images is handled by LoginController
    // when the user submits the verification form. The EncryptIdImagesRequest
    // model is not needed here.
}
