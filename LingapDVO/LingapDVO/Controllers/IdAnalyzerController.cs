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
    /// - POST /api/IdAnalyzer/scan - Standard ID Scan
    /// - POST /api/IdAnalyzer/face - Face Verification
    /// - GET /api/IdAnalyzer/account - Get Account Info
    /// - GET /api/IdAnalyzer/transaction/{id} - Get Transaction Data
    /// - GET /api/IdAnalyzer/health - Health Check
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class IdAnalyzerController : ControllerBase
    {
        private readonly IIdAnalyzerService _idAnalyzerService;
        private readonly ILogger<IdAnalyzerController> _logger;

        public IdAnalyzerController(IIdAnalyzerService idAnalyzerService, ILogger<IdAnalyzerController> logger)
        {
            _idAnalyzerService = idAnalyzerService;
            _logger = logger;
        }

        /// <summary>
        /// Standard ID Scan - Scans ID document, extracts data, and optionally matches face
        /// API Reference: https://developer.idanalyzer.com/reference/post-scan
        /// </summary>
        [HttpPost("scan")]
        public async Task<IActionResult> ScanId([FromBody] ScanIdRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.DocumentImage))
                {
                    return BadRequest(new { success = false, error = "Document image is required" });
                }

                var documentBase64 = RemoveDataUrlPrefix(request.DocumentImage);
                var faceBase64 = !string.IsNullOrEmpty(request.FaceImage) ? RemoveDataUrlPrefix(request.FaceImage) : null;
                var backBase64 = !string.IsNullOrEmpty(request.BackImage) ? RemoveDataUrlPrefix(request.BackImage) : null;

                _logger.LogInformation("Processing ID scan. Face: {HasFace}, Back: {HasBack}",
                    !string.IsNullOrEmpty(faceBase64), !string.IsNullOrEmpty(backBase64));

                var result = await _idAnalyzerService.ScanIdAsync(documentBase64, faceBase64, backBase64);

                if (!result.Success)
                {
                    _logger.LogWarning("ID scan failed: {Error} ({Code})", result.ErrorMessage, result.ErrorCodeString);
                    return Ok(new
                    {
                        success = false,
                        error = result.ErrorMessage,
                        errorCode = result.ErrorCodeString
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
                    warnings = result.Warnings,
                    data = new
                    {
                        // Personal Info - matching ID Analyzer field names
                        firstName = result.FirstName,           // "First Name"
                        middleName = result.MiddleName,         // "Middle Name"
                        lastName = result.LastName,             // "Last Name"
                        suffix = result.Suffix,                 // "Suffix" (optional)
                        fullName = result.FullName,
                        sex = result.Sex,
                        dateOfBirth = result.DateOfBirth,       // "Date of Birth"
                        nationality = result.Nationality,
                        
                        // Document Info
                        documentNumber = result.DocumentNumber, // "Document Number" -> ID Number
                        documentType = result.DocumentType,
                        documentName = result.DocumentName,     // "Document Name" -> ID Type
                        issueDate = result.IssueDate,
                        expiryDate = result.ExpiryDate,
                        
                        // Address - concatenated from Address 1 + Address 2
                        address1 = result.Address,              // "Address 1"
                        address2 = result.Address2,             // "Address 2"
                        address = fullAddress,                  // Concatenated full address
                        city = result.City,
                        state = result.State,
                        postalCode = result.PostalCode,
                        country = result.Country,
                        
                        // Verification
                        verificationPassed = result.VerificationPassed,
                        faceMatch = result.FaceMatch,
                        faceSimilarity = result.FaceSimilarity,
                        faceConfidence = result.FaceConfidence,
                        faceSimilarityPercentage = (int)Math.Round(result.FaceSimilarity * 100)
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
        public async Task<IActionResult> VerifyFace([FromBody] FaceVerifyRequest request)
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

                var result = await _idAnalyzerService.VerifyFaceAsync(faceBase64, referenceBase64);

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
                    isMatch = result.IsMatch,
                    similarity = result.Similarity,
                    similarityPercentage = result.SimilarityPercentage,
                    confidence = result.Confidence,
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
        /// Legacy endpoint - redirects to /face
        /// </summary>
        [HttpPost("compare-faces")]
        public async Task<IActionResult> CompareFaces([FromBody] CompareFacesRequest request)
        {
            var faceRequest = new FaceVerifyRequest
            {
                Face = request.FacePhoto,
                Reference = request.ReferenceFace
            };
            return await VerifyFace(faceRequest);
        }

        /// <summary>
        /// Get Account Info - Validates API key and returns account details
        /// API Reference: https://developer.idanalyzer.com/reference/get-myaccount-2
        /// </summary>
        [HttpGet("account")]
        public async Task<IActionResult> GetAccountInfo()
        {
            try
            {
                var result = await _idAnalyzerService.GetAccountInfoAsync();

                if (!result.Success)
                {
                    return Ok(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }

                return Ok(new
                {
                    success = true,
                    accountId = result.AccountId,
                    email = result.Email,
                    credits = result.Credits,
                    plan = result.Plan
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account info");
                return StatusCode(500, new { success = false, error = "Internal server error" });
            }
        }

        /// <summary>
        /// Get Transaction Data - Retrieves processed ID data from a previous scan
        /// API Reference: https://developer.idanalyzer.com/reference/get-transaction-transactionid
        /// Endpoint: GET https://api2.idanalyzer.com/transaction/{transactionId}
        /// 
        /// Returns all extracted data fields from a previously scanned ID including:
        /// - Personal info (firstName, middleName, lastName, dob, sex)
        /// - Document info (documentNumber, documentType, documentName)
        /// - Address info (address1, address2, city, state, postcode, country)
        /// </summary>
        [HttpGet("transaction/{transactionId}")]
        public async Task<IActionResult> GetTransaction(string transactionId)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionId))
                {
                    return BadRequest(new { success = false, error = "Transaction ID is required" });
                }

                _logger.LogInformation("Getting transaction data: {TransactionId}", transactionId);

                var result = await _idAnalyzerService.GetTransactionAsync(transactionId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to get transaction: {Error} ({Code})", result.ErrorMessage, result.ErrorCodeString);
                    return Ok(new
                    {
                        success = false,
                        error = result.ErrorMessage,
                        errorCode = result.ErrorCodeString
                    });
                }

                // Return all extracted data fields for account verification
                return Ok(new
                {
                    success = true,
                    transactionId = result.TransactionId,
                    decision = result.Decision,
                    
                    // Personal Information
                    data = new
                    {
                        firstName = result.FirstName,
                        middleName = result.MiddleName,
                        lastName = result.LastName,
                        suffix = result.Suffix,
                        fullName = result.FullName,
                        dateOfBirth = result.DateOfBirth,
                        sex = result.Sex,
                        nationality = result.Nationality,
                        
                        // Document Information
                        documentNumber = result.DocumentNumber,
                        documentType = result.DocumentType,
                        documentName = result.DocumentName,
                        issueDate = result.IssueDate,
                        expiryDate = result.ExpiryDate,
                        
                        // Address Information (concatenate address1 + address2)
                        address = CombineAddress(result.Address, result.Address2),
                        address1 = result.Address,
                        address2 = result.Address2,
                        city = result.City,
                        state = result.State,
                        postalCode = result.PostalCode,
                        country = result.Country
                    },
                    
                    // Face matching results (if available)
                    face = new
                    {
                        isMatch = result.FaceMatch,
                        similarity = result.FaceSimilarity,
                        confidence = result.FaceConfidence
                    },
                    
                    // Verification status
                    verificationPassed = result.VerificationPassed,
                    warnings = result.Warnings
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction: {TransactionId}", transactionId);
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
        /// Debug endpoint - Test API connectivity and get account info
        /// </summary>
        [HttpGet("debug/test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                _logger.LogInformation("Testing ID Analyzer API connection...");

                var accountInfo = await _idAnalyzerService.GetAccountInfoAsync();

                return Ok(new
                {
                    success = accountInfo.Success,
                    message = accountInfo.Success
                        ? "API connection successful"
                        : $"API connection failed: {accountInfo.ErrorMessage}",
                    accountInfo = accountInfo.Success ? new
                    {
                        email = accountInfo.Email,
                        credits = accountInfo.Credits,
                        plan = accountInfo.Plan
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API connection");
                return Ok(new
                {
                    success = false,
                    message = $"Exception: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
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

        /// <summary>
        /// Combines address1 and address2 into a single address string
        /// </summary>
        private string? CombineAddress(string? address1, string? address2)
        {
            if (string.IsNullOrWhiteSpace(address1) && string.IsNullOrWhiteSpace(address2))
                return null;
            
            if (string.IsNullOrWhiteSpace(address2))
                return address1?.Trim();
            
            if (string.IsNullOrWhiteSpace(address1))
                return address2?.Trim();
            
            return $"{address1.Trim()}, {address2.Trim()}";
        }
    }

    /// <summary>
    /// Request model for ID scan
    /// </summary>
    public class ScanIdRequest
    {
        public string DocumentImage { get; set; } = string.Empty;
        public string? FaceImage { get; set; }
        public string? BackImage { get; set; }
    }

    /// <summary>
    /// Request model for face verification (new)
    /// </summary>
    public class FaceVerifyRequest
    {
        public string Face { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request model for face comparison (legacy)
    /// </summary>
    public class CompareFacesRequest
    {
        public string ReferenceFace { get; set; } = string.Empty;
        public string FacePhoto { get; set; } = string.Empty;
    }
}
