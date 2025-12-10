using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LingapDVO.Services
{
    /// <summary>
    /// ID Analyzer API v2 Service for ID verification, OCR, and face comparison
    /// Base URL: https://api2.idanalyzer.com
    /// API Documentation: https://developer.idanalyzer.com/docs/
    /// 
    /// Endpoints:
    /// - POST /scan - Standard ID Scan (https://developer.idanalyzer.com/reference/post-scan)
    /// - POST /face - Face Verification (https://developer.idanalyzer.com/reference/post-face-2)
    /// - GET /myaccount - Account Info (https://developer.idanalyzer.com/reference/get-myaccount-2)
    /// </summary>
    public interface IIdAnalyzerService
    {
        Task<IdAnalyzerResult> ScanIdAsync(string documentImageBase64, string? faceImageBase64 = null, string? backImageBase64 = null);
        Task<FaceCompareResult> VerifyFaceAsync(string faceImageBase64, string referenceImageBase64);
        Task<AccountInfoResult> GetAccountInfoAsync();
        Task<IdAnalyzerResult> GetTransactionAsync(string transactionId);
    }

    public class IdAnalyzerService : IIdAnalyzerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdAnalyzerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        
        // Base URL for ID Analyzer API v2
        private const string BASE_URL = "https://api2.idanalyzer.com";

        public IdAnalyzerService(HttpClient httpClient, ILogger<IdAnalyzerService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiKey = configuration["IDAnalyzerSettings:ApiKey"] ?? throw new InvalidOperationException("ID Analyzer API key not configured");
            
            // Set API key header for authentication
            // Reference: https://developer.idanalyzer.com/docs/authentication-type
            _httpClient.DefaultRequestHeaders.Remove("X-Api-Key");
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _apiKey);
        }

        /// <summary>
        /// Standard ID Scan - Scans ID document and extracts data
        /// Endpoint: POST https://api2.idanalyzer.com/scan
        /// Reference: https://developer.idanalyzer.com/reference/post-scan
        /// </summary>
        public async Task<IdAnalyzerResult> ScanIdAsync(string documentImageBase64, string? faceImageBase64 = null, string? backImageBase64 = null)
        {
            try
            {
                _logger.LogInformation("Starting ID Analyzer Standard ID Scan...");

                var profileId = _configuration["IDAnalyzerSettings:Profile"] ?? "security_medium";
                
                // Build request body per API spec
                var requestBody = new Dictionary<string, object>
                {
                    ["document"] = CleanBase64(documentImageBase64),
                    ["profile"] = profileId
                };

                // Add face image for face matching (compares selfie with ID photo)
                if (!string.IsNullOrEmpty(faceImageBase64))
                {
                    requestBody["face"] = CleanBase64(faceImageBase64);
                }

                // Add back of ID if provided
                if (!string.IsNullOrEmpty(backImageBase64))
                {
                    requestBody["documentBack"] = CleanBase64(backImageBase64);
                }

                // Optional: Add profile overrides from config
                var profileOverride = BuildProfileOverride();
                if (profileOverride.Count > 0)
                {
                    requestBody["profileOverride"] = profileOverride;
                }

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("Calling POST {Url}/scan", BASE_URL);
                var response = await _httpClient.PostAsync($"{BASE_URL}/scan", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                // Log full response for debugging
                _logger.LogInformation("=== ID ANALYZER API RESPONSE START ===");
                _logger.LogInformation("HTTP Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Response Length: {Length} characters", responseJson.Length);
                _logger.LogInformation("Response Body: {Response}", responseJson);
                _logger.LogInformation("=== ID ANALYZER API RESPONSE END ===");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ID Analyzer API error: HTTP {StatusCode}", response.StatusCode);
                }

                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                
                // Check for API errors
                if (result.TryGetProperty("error", out var errorElement))
                {
                    var errorCode = errorElement.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : "UNKNOWN";
                    var errorMsg = errorElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    
                    _logger.LogWarning("ID Analyzer error: {Code} - {Message}", errorCode, errorMsg);
                    return new IdAnalyzerResult
                    {
                        Success = false,
                        ErrorCodeString = errorCode,
                        ErrorMessage = errorMsg
                    };
                }

                // Parse successful result
                var idResult = ParseScanResult(result);
                idResult.Success = true;
                idResult.RawJson = responseJson;

                _logger.LogInformation("ID Analyzer scan completed. DocName: {DocName}, DocType: {DocType}, Decision: {Decision}, TransactionId: {TransactionId}", 
                    idResult.DocumentName ?? "(not extracted)", 
                    idResult.DocumentType ?? "(not extracted)", 
                    idResult.Decision,
                    idResult.TransactionId);
                
                return idResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ID Analyzer scan API");
                return new IdAnalyzerResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Face Verification - Compares two face images
        /// Endpoint: POST https://api2.idanalyzer.com/face
        /// Reference: https://developer.idanalyzer.com/reference/post-face-2
        /// </summary>
        public async Task<FaceCompareResult> VerifyFaceAsync(string faceImageBase64, string referenceImageBase64)
        {
            try
            {
                _logger.LogInformation("Starting ID Analyzer Face Verification...");

                var profileId = _configuration["IDAnalyzerSettings:Profile"] ?? "security_medium";

                // Build request body per API spec
                // face: The face image to verify (selfie)
                // reference: The reference face image (from ID document)
                var requestBody = new Dictionary<string, object>
                {
                    ["face"] = CleanBase64(faceImageBase64),
                    ["reference"] = CleanBase64(referenceImageBase64),
                    ["profile"] = profileId
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("Calling POST {Url}/face", BASE_URL);
                var response = await _httpClient.PostAsync($"{BASE_URL}/face", content);
                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Face verification response: {Response}", responseJson);

                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // Check for errors
                if (result.TryGetProperty("error", out var errorElement))
                {
                    var errorMsg = errorElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    var errorCode = errorElement.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : "UNKNOWN";
                    _logger.LogWarning("Face verification error: {Code} - {Message}", errorCode, errorMsg);
                    return new FaceCompareResult
                    {
                        Success = false,
                        ErrorMessage = errorMsg
                    };
                }

                // Parse face verification result
                return ParseFaceResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ID Analyzer face verification API");
                return new FaceCompareResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get Account Information - Validates API key and gets account details
        /// Endpoint: GET https://api2.idanalyzer.com/myaccount
        /// Reference: https://developer.idanalyzer.com/reference/get-myaccount-2
        /// </summary>
        public async Task<AccountInfoResult> GetAccountInfoAsync()
        {
            try
            {
                _logger.LogInformation("Getting ID Analyzer account information...");

                var response = await _httpClient.GetAsync($"{BASE_URL}/myaccount");
                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Account info response: {Response}", responseJson);

                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (result.TryGetProperty("error", out var errorElement))
                {
                    var errorMsg = errorElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    return new AccountInfoResult
                    {
                        Success = false,
                        ErrorMessage = errorMsg
                    };
                }

                return new AccountInfoResult
                {
                    Success = true,
                    AccountId = result.TryGetProperty("accountId", out var accId) ? accId.GetString() : null,
                    Email = result.TryGetProperty("email", out var email) ? email.GetString() : null,
                    Credits = result.TryGetProperty("credits", out var credits) ? credits.GetInt32() : 0,
                    Plan = result.TryGetProperty("plan", out var plan) ? plan.GetString() : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ID Analyzer account info");
                return new AccountInfoResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get Transaction Data - Retrieves processed ID data from a previous scan
        /// Endpoint: GET https://api2.idanalyzer.com/transaction/{transactionId}
        /// Reference: https://developer.idanalyzer.com/reference/get-transaction-transactionid
        /// 
        /// This retrieves the extracted data from an ID that was previously processed,
        /// including all OCR data fields needed for account verification.
        /// </summary>
        public async Task<IdAnalyzerResult> GetTransactionAsync(string transactionId)
        {
            try
            {
                if (string.IsNullOrEmpty(transactionId))
                {
                    return new IdAnalyzerResult
                    {
                        Success = false,
                        ErrorMessage = "Transaction ID is required"
                    };
                }

                _logger.LogInformation("Getting transaction data for ID: {TransactionId}", transactionId);

                var response = await _httpClient.GetAsync($"{BASE_URL}/transaction/{transactionId}");
                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Transaction response: {Response}", responseJson);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get transaction: HTTP {StatusCode}", response.StatusCode);
                    return new IdAnalyzerResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP error: {response.StatusCode}"
                    };
                }

                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

                // Check for API errors
                if (result.TryGetProperty("error", out var errorElement))
                {
                    var errorCode = errorElement.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : "UNKNOWN";
                    var errorMsg = errorElement.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "Unknown error";
                    
                    _logger.LogWarning("Transaction error: {Code} - {Message}", errorCode, errorMsg);
                    return new IdAnalyzerResult
                    {
                        Success = false,
                        ErrorCodeString = errorCode,
                        ErrorMessage = errorMsg
                    };
                }

                // Parse the transaction result - same structure as scan result
                var idResult = ParseScanResult(result);
                idResult.Success = true;
                idResult.RawJson = responseJson;
                idResult.TransactionId = transactionId;

                _logger.LogInformation("Transaction data retrieved. Document: {DocType}, Name: {Name}", 
                    idResult.DocumentName, idResult.FullName ?? $"{idResult.FirstName} {idResult.LastName}");

                return idResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction data: {TransactionId}", transactionId);
                return new IdAnalyzerResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Build profile override settings from configuration
        /// </summary>
        private Dictionary<string, object> BuildProfileOverride()
        {
            var profile = new Dictionary<string, object>();
            
            // Output settings
            if (_configuration.GetValue<bool>("IDAnalyzerSettings:OutputImage", false))
                profile["outputImage"] = true;
            
            if (_configuration.GetValue<bool>("IDAnalyzerSettings:SaveTransaction", false))
                profile["saveFile"] = true;
            
            // Processing settings
            if (_configuration.GetValue<bool>("IDAnalyzerSettings:OrientationCorrection", true))
                profile["orientationCorrection"] = true;
            
            if (_configuration.GetValue<bool>("IDAnalyzerSettings:InferFullName", true))
                profile["inferFullName"] = true;
            
            if (_configuration.GetValue<bool>("IDAnalyzerSettings:SplitFirstName", true))
                profile["splitFirstName"] = true;
            
            // Document restrictions
            var docCountries = _configuration["IDAnalyzerSettings:DocumentCountries"];
            if (!string.IsNullOrEmpty(docCountries))
                profile["restrictCountry"] = docCountries;
            
            var docTypes = _configuration["IDAnalyzerSettings:DocumentTypes"];
            if (!string.IsNullOrEmpty(docTypes))
                profile["restrictType"] = docTypes;

            return profile;
        }

        /// <summary>
        /// Parse scan result from API response
        /// Based on ID Analyzer API v2 references:
        /// - POST /scan: https://developer.idanalyzer.com/reference/post-scan
        /// - GET /transaction/{id}: https://developer.idanalyzer.com/reference/get-transaction-transactionid
        /// 
        /// Response structure:
        /// {
        ///   "decision": "accept|review|reject",
        ///   "transactionId": "...",
        ///   "data": {
        ///     "firstName": { "value": "JOHN", "confidence": 0.95 },
        ///     "middleName": { "value": "DOE", "confidence": 0.90 },
        ///     "lastName": { "value": "SMITH", "confidence": 0.95 },
        ///     "fullName": { "value": "JOHN DOE SMITH", "confidence": 0.93 },
        ///     "dob": { "value": "1990-01-15", "confidence": 0.98 },
        ///     "sex": { "value": "M", "confidence": 0.99 },
        ///     "documentNumber": { "value": "N01-12-345678", "confidence": 0.97 },
        ///     "documentType": { "value": "I", "confidence": 0.99 },
        ///     "documentName": { "value": "National ID", "confidence": 0.99 },
        ///     "address1": { "value": "123 Main Street", "confidence": 0.85 },
        ///     "address2": { "value": "Davao City", "confidence": 0.85 },
        ///     ...
        ///   },
        ///   "face": { "isIdentical": true, "similarity": 0.92, "confidence": 0.95 },
        ///   "warning": [ { "code": "MINOR_AGE", "description": "..." } ]
        /// }
        /// </summary>
        private IdAnalyzerResult ParseScanResult(JsonElement result)
        {
            var idResult = new IdAnalyzerResult();

            // Log the raw response for debugging
            _logger.LogDebug("Parsing ID Analyzer response: {Response}", result.ToString());

            // Parse decision (accept, review, reject)
            if (result.TryGetProperty("decision", out var decisionEl))
            {
                idResult.Decision = decisionEl.GetString();
                idResult.VerificationPassed = idResult.Decision == "accept";
            }

            // Parse transaction ID first
            if (result.TryGetProperty("transactionId", out var transIdEl))
                idResult.TransactionId = transIdEl.GetString();

            // Parse extracted data fields from ID Analyzer API v2
            // The data object contains fields with { "value": "...", "confidence": ... } structure
            JsonElement dataResult = default;
            bool hasData = false;

            if (result.TryGetProperty("data", out dataResult))
            {
                hasData = true;
                _logger.LogDebug("Found 'data' property in response");
            }
            else if (result.TryGetProperty("result", out var resultProp) && resultProp.TryGetProperty("data", out dataResult))
            {
                hasData = true;
                _logger.LogDebug("Found 'result.data' property in response");
            }

            if (hasData && dataResult.ValueKind == JsonValueKind.Object)
            {
                // Log all available properties in the data object for debugging
                var availableFields = new List<string>();
                foreach (var prop in dataResult.EnumerateObject())
                {
                    var fieldType = prop.Value.ValueKind.ToString();
                    var fieldInfo = prop.Name;

                    // Add more details about the field
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var arrayLen = prop.Value.GetArrayLength();
                        fieldInfo += $" (Array[{arrayLen}])";
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        fieldInfo += " (Object)";
                    }

                    availableFields.Add(fieldInfo);
                }
                _logger.LogInformation("ID Analyzer data fields available: [{Fields}]", string.Join(", ", availableFields));

                // Log a sample of the data structure for debugging
                _logger.LogDebug("Sample data structure: {DataSample}", dataResult.ToString());

                // Personal Information - ID Analyzer field names
                // Fields have structure: { "value": "...", "confidence": 0.95 }
                idResult.FirstName = GetDataFieldValue(dataResult, "firstName");       // First Name
                idResult.MiddleName = GetDataFieldValue(dataResult, "middleName");     // Middle Name  
                idResult.LastName = GetDataFieldValue(dataResult, "lastName");         // Last Name
                idResult.Suffix = GetDataFieldValue(dataResult, "suffix");             // Suffix (optional)
                idResult.FullName = GetDataFieldValue(dataResult, "fullName");         // Full Name (combined)
                idResult.Sex = GetDataFieldValue(dataResult, "sex");                   // Sex/Gender (M/F)
                idResult.DateOfBirth = GetDataFieldValue(dataResult, "dob");           // Date of Birth (YYYY-MM-DD)
                idResult.Nationality = GetDataFieldValue(dataResult, "nationality");   // Nationality

                // Document Information - ID Analyzer field names
                idResult.DocumentNumber = GetDataFieldValue(dataResult, "documentNumber");  // Document Number (ID Number)
                idResult.DocumentType = GetDataFieldValue(dataResult, "documentType");      // Document Type code (D=Driver, I=ID, P=Passport)
                idResult.DocumentName = GetDataFieldValue(dataResult, "documentName");      // Document Name (human readable)
                idResult.IssueDate = GetDataFieldValue(dataResult, "issued");               // Issue Date
                idResult.ExpiryDate = GetDataFieldValue(dataResult, "expiry");              // Expiry Date

                _logger.LogInformation("📄 Document Type extraction - documentType: '{DocType}', documentName: '{DocName}'",
                    idResult.DocumentType ?? "(null)", idResult.DocumentName ?? "(null)");

                // Try alternative field names for document info
                if (string.IsNullOrEmpty(idResult.DocumentType))
                {
                    idResult.DocumentType = GetDataFieldValue(dataResult, "docType");
                    if (!string.IsNullOrEmpty(idResult.DocumentType))
                        _logger.LogInformation("✓ Found documentType using alternative field name 'docType': '{DocType}'", idResult.DocumentType);
                }

                if (string.IsNullOrEmpty(idResult.DocumentName))
                {
                    idResult.DocumentName = GetDataFieldValue(dataResult, "docName");
                    if (!string.IsNullOrEmpty(idResult.DocumentName))
                        _logger.LogInformation("✓ Found documentName using alternative field name 'docName': '{DocName}'", idResult.DocumentName);

                    if (string.IsNullOrEmpty(idResult.DocumentName))
                    {
                        idResult.DocumentName = GetDataFieldValue(dataResult, "document");
                        if (!string.IsNullOrEmpty(idResult.DocumentName))
                            _logger.LogInformation("✓ Found documentName using alternative field name 'document': '{DocName}'", idResult.DocumentName);
                    }
                }

                if (string.IsNullOrEmpty(idResult.DocumentNumber))
                {
                    idResult.DocumentNumber = GetDataFieldValue(dataResult, "docNumber");
                    if (!string.IsNullOrEmpty(idResult.DocumentNumber))
                        _logger.LogInformation("✓ Found documentNumber using alternative field name 'docNumber': '{DocNum}'", idResult.DocumentNumber);

                    if (string.IsNullOrEmpty(idResult.DocumentNumber))
                    {
                        idResult.DocumentNumber = GetDataFieldValue(dataResult, "idNumber");
                        if (!string.IsNullOrEmpty(idResult.DocumentNumber))
                            _logger.LogInformation("✓ Found documentNumber using alternative field name 'idNumber': '{DocNum}'", idResult.DocumentNumber);
                    }
                }

                // Final check - if still empty, log warning
                if (string.IsNullOrEmpty(idResult.DocumentType) && string.IsNullOrEmpty(idResult.DocumentName))
                {
                    _logger.LogWarning("⚠️ WARNING: Both documentType and documentName are NULL/empty after extraction. ID type detection may fail!");
                }

                // Address Information - ID Analyzer field names
                idResult.Address = GetDataFieldValue(dataResult, "address1");               // Address Line 1
                idResult.Address2 = GetDataFieldValue(dataResult, "address2");              // Address Line 2
                idResult.City = GetDataFieldValue(dataResult, "addressCity");               // City
                idResult.State = GetDataFieldValue(dataResult, "addressState");             // State/Province/Region
                idResult.PostalCode = GetDataFieldValue(dataResult, "addressPostcode");     // Postal/ZIP Code
                idResult.Country = GetDataFieldValue(dataResult, "issueCountryFull");       // Country (full name)

                // Fallback to alternative field names if primary not found
                if (string.IsNullOrEmpty(idResult.Address))
                    idResult.Address = GetDataFieldValue(dataResult, "address");
                if (string.IsNullOrEmpty(idResult.City))
                    idResult.City = GetDataFieldValue(dataResult, "city");
                if (string.IsNullOrEmpty(idResult.State))
                    idResult.State = GetDataFieldValue(dataResult, "state");
                if (string.IsNullOrEmpty(idResult.PostalCode))
                    idResult.PostalCode = GetDataFieldValue(dataResult, "postcode");
                if (string.IsNullOrEmpty(idResult.Country))
                    idResult.Country = GetDataFieldValue(dataResult, "country");
                    
                // Log extracted data for debugging
                _logger.LogInformation("ID Analyzer extracted - Name: {FirstName} {MiddleName} {LastName}, DOB: {DOB}, DocType: {DocType}, DocName: {DocName}, DocNum: {DocNum}, Address: {Address}",
                    idResult.FirstName ?? "(null)", idResult.MiddleName ?? "(null)", idResult.LastName ?? "(null)", 
                    idResult.DateOfBirth ?? "(null)", idResult.DocumentType ?? "(null)", idResult.DocumentName ?? "(null)", 
                    idResult.DocumentNumber ?? "(null)", idResult.Address ?? "(null)");
            }
            else
            {
                _logger.LogWarning("No data found in ID Analyzer response or data is not an object");
            }

            // Parse face matching results (if face image was provided in scan)
            // Reference: https://developer.idanalyzer.com/reference/post-face-2
            if (result.TryGetProperty("face", out var faceResult))
            {
                idResult.FaceMatch = faceResult.TryGetProperty("isIdentical", out var identEl) && identEl.GetBoolean();
                idResult.FaceSimilarity = faceResult.TryGetProperty("similarity", out var simEl) ? simEl.GetDouble() : 0;
                idResult.FaceConfidence = faceResult.TryGetProperty("confidence", out var confEl) ? confEl.GetDouble() : 0;
                
                _logger.LogInformation("Face match in scan: IsIdentical={IsIdentical}, Similarity={Similarity}", 
                    idResult.FaceMatch, idResult.FaceSimilarity);
            }

            // Parse warnings array
            if (result.TryGetProperty("warning", out var warningEl) && warningEl.ValueKind == JsonValueKind.Array)
            {
                idResult.Warnings = new List<string>();
                foreach (var warning in warningEl.EnumerateArray())
                {
                    if (warning.TryGetProperty("code", out var warnCode))
                        idResult.Warnings.Add(warnCode.GetString() ?? "UNKNOWN");
                    else if (warning.ValueKind == JsonValueKind.String)
                        idResult.Warnings.Add(warning.GetString() ?? "UNKNOWN");
                }
                
                if (idResult.Warnings.Count > 0)
                    _logger.LogInformation("ID Analyzer warnings: [{Warnings}]", string.Join(", ", idResult.Warnings));
            }

            return idResult;
        }

        /// <summary>
        /// Extract value from data field object
        /// ID Analyzer API v2 returns data fields in multiple formats:
        /// 1. Array of objects: [{ "value": "...", "confidence": 0.95, "source": "visual" }]
        /// 2. Single object: { "value": "...", "confidence": 0.95 }
        /// 3. Direct value: "string" or number
        /// </summary>
        private string? GetDataFieldValue(JsonElement dataElement, string fieldName)
        {
            if (!dataElement.TryGetProperty(fieldName, out var fieldElement))
                return null;

            _logger.LogDebug("Extracting field '{FieldName}', Type: {Type}", fieldName, fieldElement.ValueKind);

            // If the field is an ARRAY (most common in API v2)
            if (fieldElement.ValueKind == JsonValueKind.Array)
            {
                var arrayLength = fieldElement.GetArrayLength();
                _logger.LogDebug("Field '{FieldName}' is array with {Length} elements", fieldName, arrayLength);

                if (arrayLength > 0)
                {
                    var firstElement = fieldElement[0];

                    // Get value from first element in array
                    if (firstElement.ValueKind == JsonValueKind.Object && firstElement.TryGetProperty("value", out var valueEl))
                    {
                        var extractedValue = valueEl.ValueKind == JsonValueKind.String
                            ? valueEl.GetString()
                            : valueEl.ToString();

                        _logger.LogDebug("Extracted '{Value}' from array field '{FieldName}'", extractedValue, fieldName);
                        return extractedValue;
                    }

                    // If array element is direct string
                    if (firstElement.ValueKind == JsonValueKind.String)
                        return firstElement.GetString();
                }
                return null;
            }

            // If the field is an object with "value" property (API v2 alternative format)
            if (fieldElement.ValueKind == JsonValueKind.Object)
            {
                if (fieldElement.TryGetProperty("value", out var valueEl))
                {
                    if (valueEl.ValueKind == JsonValueKind.String)
                        return valueEl.GetString();
                    if (valueEl.ValueKind == JsonValueKind.Number)
                        return valueEl.ToString();
                }
                return null;
            }

            // If the field is a direct string value
            if (fieldElement.ValueKind == JsonValueKind.String)
                return fieldElement.GetString();

            // If the field is a number (for document numbers, etc.)
            if (fieldElement.ValueKind == JsonValueKind.Number)
                return fieldElement.ToString();

            _logger.LogWarning("Could not extract value from field '{FieldName}' with type {Type}", fieldName, fieldElement.ValueKind);
            return null;
        }

        /// <summary>
        /// Parse face verification result from API response
        /// Reference: https://developer.idanalyzer.com/reference/post-face-2
        /// 
        /// Response structure:
        /// {
        ///   "decision": "accept|review|reject",
        ///   "face": {
        ///     "isIdentical": true|false,
        ///     "similarity": 0.0-1.0,
        ///     "confidence": 0.0-1.0
        ///   },
        ///   "transactionId": "...",
        ///   "warning": [...]
        /// }
        /// </summary>
        private FaceCompareResult ParseFaceResult(JsonElement result)
        {
            double similarity = 0;
            bool isMatch = false;
            double confidence = 0;
            string decision = "unknown";

            // Parse decision first
            if (result.TryGetProperty("decision", out var decisionEl))
            {
                decision = decisionEl.GetString() ?? "unknown";
            }

            // Parse face comparison data from "face" object
            if (result.TryGetProperty("face", out var faceResult))
            {
                similarity = faceResult.TryGetProperty("similarity", out var simEl) ? simEl.GetDouble() : 0;
                isMatch = faceResult.TryGetProperty("isIdentical", out var identEl) && identEl.GetBoolean();
                confidence = faceResult.TryGetProperty("confidence", out var confEl) ? confEl.GetDouble() : similarity;
                
                _logger.LogInformation("Face verification parsed - Similarity: {Similarity}, IsIdentical: {IsIdentical}, Confidence: {Confidence}", 
                    similarity, isMatch, confidence);
            }
            else
            {
                _logger.LogWarning("No 'face' object found in face verification response");
            }

            // Decision takes precedence for determining match
            if (decision == "accept")
            {
                isMatch = true;
            }
            else if (decision == "reject")
            {
                isMatch = false;
            }
            else
            {
                // Apply custom threshold if decision not definitive
                var threshold = _configuration.GetValue<double>("IDAnalyzerSettings:FaceMatchThreshold", 0.5);
                if (similarity >= threshold)
                {
                    isMatch = true;
                }
            }

            _logger.LogInformation("Face verification result: Similarity={Similarity}%, Match={Match}, Decision={Decision}", 
                Math.Round(similarity * 100, 1), isMatch, decision);

            return new FaceCompareResult
            {
                Success = true,
                IsMatch = isMatch,
                Similarity = similarity,
                Confidence = confidence,
                SimilarityPercentage = (int)Math.Round(similarity * 100),
                Decision = decision
            };
        }

        private string CleanBase64(string base64)
        {
            // Remove data URI prefix if present (e.g., "data:image/jpeg;base64,")
            if (base64.Contains(","))
            {
                base64 = base64.Split(',')[1];
            }
            return base64.Trim();
        }
    }

    /// <summary>
    /// Result from ID Analyzer scan
    /// </summary>
    public class IdAnalyzerResult
    {
        public bool Success { get; set; }
        public string? ErrorCodeString { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawJson { get; set; }
        public string? TransactionId { get; set; }
        public string? Decision { get; set; }
        public List<string>? Warnings { get; set; }

        // Personal Information
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Suffix { get; set; }
        public string? FullName { get; set; }
        public string? Sex { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Nationality { get; set; }

        // Document Information
        public string? DocumentNumber { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentName { get; set; }
        public string? IssueDate { get; set; }
        public string? ExpiryDate { get; set; }

        // Address
        public string? Address { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // Verification Results
        public bool VerificationPassed { get; set; }

        // Face Matching Results (when face provided in scan)
        public bool FaceMatch { get; set; }
        public double FaceSimilarity { get; set; }
        public double FaceConfidence { get; set; }
    }

    /// <summary>
    /// Result from face verification
    /// </summary>
    public class FaceCompareResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsMatch { get; set; }
        public double Similarity { get; set; }
        public double Confidence { get; set; }
        public int SimilarityPercentage { get; set; }
        public string? Decision { get; set; }
    }

    /// <summary>
    /// Result from account info request
    /// </summary>
    public class AccountInfoResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AccountId { get; set; }
        public string? Email { get; set; }
        public int Credits { get; set; }
        public string? Plan { get; set; }
    }
}
