using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LingapDVO.Services
{
    public class IdAnalyzerService : IVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<IdAnalyzerService> _logger;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _profileId;

        public IdAnalyzerService(HttpClient httpClient, ILogger<IdAnalyzerService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = configuration["IdAnalyzerSettings:BaseUrl"] ?? "https://api2.idanalyzer.com/scan";
            _apiKey = configuration["IdAnalyzerSettings:ApiKey"] ?? throw new InvalidOperationException("ID Analyzer API key not configured");
            _profileId = configuration["IdAnalyzerSettings:ProfileId"] ?? throw new InvalidOperationException("ID Analyzer Profile ID not configured");

            _logger.LogInformation("🔧 IdAnalyzerService initialized - BaseUrl: {BaseUrl}, ProfileId: {ProfileId}", _baseUrl, _profileId);
        }

        public async Task<VerificationResult> ScanIdAsync(string documentImageBase64, string? faceImageBase64 = null, string? backImageBase64 = null)
        {
            try
            {
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogInformation("🚀 Starting ID Analyzer v2 API Scan");
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                // Clean base64 strings
                var cleanDocumentImage = CleanBase64(documentImageBase64);
                var cleanFaceImage = !string.IsNullOrEmpty(faceImageBase64) ? CleanBase64(faceImageBase64) : null;
                var cleanBackImage = !string.IsNullOrEmpty(backImageBase64) ? CleanBase64(backImageBase64) : null;

                // Validate required document image
                if (string.IsNullOrEmpty(cleanDocumentImage))
                {
                    _logger.LogError("❌ Document image is required but was null or empty");
                    return new VerificationResult
                    {
                        Success = false,
                        ErrorMessage = "Document image is required"
                    };
                }

                _logger.LogInformation("📦 Payload Info:");
                _logger.LogInformation("   - Profile ID: {ProfileId}", _profileId);
                _logger.LogInformation("   - Document Image: {Length} chars", cleanDocumentImage.Length);
                _logger.LogInformation("   - Face Image: {Length} chars", cleanFaceImage?.Length ?? 0);
                _logger.LogInformation("   - Back Image: {Length} chars", cleanBackImage?.Length ?? 0);

                // Build payload according to ID Analyzer v2 API documentation
                var payload = new Dictionary<string, object>
                {
                    { "profile", _profileId! },
                    { "document", cleanDocumentImage }
                };

                if (!string.IsNullOrEmpty(cleanBackImage))
                {
                    payload.Add("documentBack", cleanBackImage);
                }

                if (!string.IsNullOrEmpty(cleanFaceImage))
                {
                    payload.Add("face", cleanFaceImage);
                }

                var json = JsonSerializer.Serialize(payload);

                _logger.LogInformation("📤 Request:");
                _logger.LogInformation("   - URL: {BaseUrl}", _baseUrl);
                _logger.LogInformation("   - Method: POST");
                _logger.LogInformation("   - Headers: X-API-KEY (length: {Length}), Content-Type: application/json", _apiKey?.Length ?? 0);
                _logger.LogInformation("   - Payload size: {Size} bytes", json.Length);

                var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl);
                request.Headers.Add("X-API-KEY", _apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("⏳ Sending request to ID Analyzer API...");
                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogInformation("📥 Response Received:");
                _logger.LogInformation("   - Status Code: {StatusCode} ({StatusCodeNum})", response.StatusCode, (int)response.StatusCode);
                _logger.LogInformation("   - Response Length: {Length} chars", responseJson?.Length ?? 0);
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogInformation("📄 FULL API RESPONSE:");
                _logger.LogInformation("{ResponseJson}", responseJson);
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ ID Analyzer API returned non-success status code");
                    return new VerificationResult
                    {
                        Success = false,
                        ErrorMessage = $"API Error: Status {response.StatusCode}. Response: {responseJson}"
                    };
                }

                // Validate response JSON
                if (string.IsNullOrEmpty(responseJson))
                {
                    _logger.LogError("❌ ID Analyzer API returned empty response");
                    return new VerificationResult
                    {
                        Success = false,
                        ErrorMessage = "API returned empty response"
                    };
                }

                // Parse JSON response
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Log all top-level properties
                _logger.LogInformation("🔍 Top-level properties in response:");
                var topLevelProps = new List<string>();
                foreach (var prop in root.EnumerateObject())
                {
                    topLevelProps.Add($"{prop.Name} ({prop.Value.ValueKind})");
                    _logger.LogInformation("   - {PropName}: {ValueKind}", prop.Name, prop.Value.ValueKind);
                }

                // Check for error field FIRST (most important)
                if (root.TryGetProperty("error", out var errorProp))
                {
                    var errorMsg = "Unknown error from ID Analyzer";

                    if (errorProp.ValueKind == JsonValueKind.Object && errorProp.TryGetProperty("message", out var errMsg))
                    {
                        errorMsg = errMsg.GetString() ?? errorMsg;
                    }
                    else if (errorProp.ValueKind == JsonValueKind.String)
                    {
                        errorMsg = errorProp.GetString() ?? errorMsg;
                    }
                    else if (errorProp.ValueKind == JsonValueKind.Object)
                    {
                        // Log full error object
                        errorMsg = errorProp.ToString();
                    }

                    _logger.LogError("❌ ID Analyzer returned error: {ErrorMessage}", errorMsg);
                    return new VerificationResult { Success = false, ErrorMessage = errorMsg };
                }

                // FIXED: Check for success field (optional - not all responses have it)
                // ID Analyzer v2 API may not include "success" field when scan is successful
                // Presence of "data" object indicates success
                bool apiSuccess = true;  // Default to true if no error
                if (root.TryGetProperty("success", out var successProp))
                {
                    apiSuccess = successProp.GetBoolean();
                    _logger.LogInformation("📋 API Success field: {Success}", apiSuccess);

                    // Only fail if explicitly set to false
                    if (!apiSuccess)
                    {
                        _logger.LogError("❌ API returned success=false");
                        return new VerificationResult { Success = false, ErrorMessage = "ID Analyzer API returned success=false. Check logs for details." };
                    }
                }
                else
                {
                    _logger.LogInformation("📋 No 'success' field in response - assuming success (no error present)");
                }

                // Initialize result
                var result = new VerificationResult
                {
                    Success = true,
                    TransactionId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() :
                                    root.TryGetProperty("transactionId", out var transId) ? transId.GetString() :
                                    root.TryGetProperty("reference", out var refId) ? refId.GetString() :
                                    Guid.NewGuid().ToString(),
                    Decision = root.TryGetProperty("decision", out var decisionProp) ? decisionProp.GetString() : "processed"
                };

                _logger.LogInformation("🆔 Transaction ID: {TransactionId}", result.TransactionId);

                // Extract top-level fields
                if (root.TryGetProperty("profileId", out var profileIdProp))
                    result.ProfileId = profileIdProp.GetString();

                if (root.TryGetProperty("reviewScore", out var reviewScoreProp) && reviewScoreProp.ValueKind == JsonValueKind.Number)
                    result.ReviewScore = reviewScoreProp.GetInt32();

                if (root.TryGetProperty("rejectScore", out var rejectScoreProp) && rejectScoreProp.ValueKind == JsonValueKind.Number)
                    result.RejectScore = rejectScoreProp.GetInt32();

                if (root.TryGetProperty("createdAt", out var createdAtProp) && createdAtProp.ValueKind == JsonValueKind.Number)
                    result.CreatedAt = createdAtProp.GetInt64();

                if (root.TryGetProperty("updatedAt", out var updatedAtProp) && updatedAtProp.ValueKind == JsonValueKind.Number)
                    result.UpdatedAt = updatedAtProp.GetInt64();

                // Extract missingFields array
                if (root.TryGetProperty("missingFields", out var missingFieldsProp) && missingFieldsProp.ValueKind == JsonValueKind.Array)
                {
                    result.MissingFields = new List<string>();
                    foreach (var field in missingFieldsProp.EnumerateArray())
                    {
                        if (field.ValueKind == JsonValueKind.String)
                            result.MissingFields.Add(field.GetString() ?? "");
                    }
                }

                // Extract outputImage hashes
                if (root.TryGetProperty("outputImage", out var outputImageObj))
                {
                    if (outputImageObj.TryGetProperty("front", out var frontProp))
                        result.FrontImageHash = frontProp.GetString();

                    if (outputImageObj.TryGetProperty("face", out var faceProp))
                        result.FaceImageHash = faceProp.GetString();
                }

                // ID Analyzer v2 uses "data" object for extracted information
                if (root.TryGetProperty("data", out var dataObj))
                {
                    _logger.LogInformation("✅ Found 'data' property in response");
                    _logger.LogInformation("📋 Data properties:");

                    foreach (var prop in dataObj.EnumerateObject())
                    {
                        _logger.LogInformation("   - {PropName}: {ValueKind}", prop.Name, prop.Value.ValueKind);
                    }

                    // Extract fields from data object
                    // ID Analyzer v2 API exact field names based on actual response structure

                    // Personal Information
                    result.FirstName = ExtractFieldValue(dataObj, "firstName");
                    result.LastName = ExtractFieldValue(dataObj, "lastName");
                    result.MiddleName = ExtractFieldValue(dataObj, "middleName");
                    result.FullName = ExtractFieldValue(dataObj, "fullName");
                    result.Suffix = ExtractFieldValue(dataObj, "suffix");

                    // Date of Birth - ID Analyzer provides dob, dobDay, dobMonth, dobYear
                    result.DateOfBirth = ExtractFieldValue(dataObj, "dob");
                    result.DobDay = ExtractFieldValue(dataObj, "dobDay");
                    result.DobMonth = ExtractFieldValue(dataObj, "dobMonth");
                    result.DobYear = ExtractFieldValue(dataObj, "dobYear");

                    // Age
                    var ageStr = ExtractFieldValue(dataObj, "age");
                    if (!string.IsNullOrEmpty(ageStr) && int.TryParse(ageStr, out var age))
                        result.Age = age;

                    // Gender/Sex
                    result.Sex = ExtractFieldValue(dataObj, "sex", "gender");

                    // Nationality - ID Analyzer provides nationalityFull, nationalityIso2, nationalityIso3
                    result.Nationality = ExtractFieldValue(dataObj, "nationalityFull", "nationality");
                    result.NationalityIso2 = ExtractFieldValue(dataObj, "nationalityIso2");
                    result.NationalityIso3 = ExtractFieldValue(dataObj, "nationalityIso3");

                    // Country - ID Analyzer provides countryFull, countryIso2, countryIso3
                    result.Country = ExtractFieldValue(dataObj, "countryFull", "country");
                    result.CountryIso2 = ExtractFieldValue(dataObj, "countryIso2");
                    result.CountryIso3 = ExtractFieldValue(dataObj, "countryIso3");

                    // Document Information
                    result.DocumentNumber = ExtractFieldValue(dataObj, "documentNumber");
                    result.DocumentType = ExtractFieldValue(dataObj, "documentType");
                    result.DocumentName = ExtractFieldValue(dataObj, "documentName");
                    result.DocumentSide = ExtractFieldValue(dataObj, "documentSide");
                    result.InternalId = ExtractFieldValue(dataObj, "internalId");
                    result.BackSideId = ExtractFieldValue(dataObj, "backSideId");
                    result.ReverseId = ExtractFieldValue(dataObj, "reverseId");

                    // Address Information - CRITICAL for Davao City validation
                    result.Address = ExtractFieldValue(dataObj, "address1");
                    result.Address2 = ExtractFieldValue(dataObj, "address2");
                    result.City = ExtractFieldValue(dataObj, "city");
                    result.State = ExtractFieldValue(dataObj, "state", "province");
                    result.PostalCode = ExtractFieldValue(dataObj, "postalCode", "postcode");

                    // Dates
                    result.IssueDate = ExtractFieldValue(dataObj, "issueDate");
                    result.ExpiryDate = ExtractFieldValue(dataObj, "expiryDate", "expiry");

                    // ===== FACE VERIFICATION RESULTS (from /scan endpoint with face parameter) =====
                    // When face parameter is included in /scan request, results are in data object
                    if (!string.IsNullOrEmpty(faceImageBase64))
                    {
                        _logger.LogInformation("👤 Extracting face verification results from data object");

                        // ID Analyzer v2 returns face matching in data object with these possible field names:
                        result.FaceMatch = ExtractBoolValue(dataObj, "faceMatch", "isIdentical", "match", "verified");
                        result.FaceSimilarity = ExtractDoubleValue(dataObj, "faceSimilarity", "similarity", "faceScore");
                        result.FaceConfidence = ExtractDoubleValue(dataObj, "faceConfidence", "confidence", "faceConfidenceScore");

                        // If similarity is 0-1 range, convert to percentage
                        if (result.FaceSimilarity > 0 && result.FaceSimilarity <= 1)
                        {
                            result.FaceSimilarity = result.FaceSimilarity * 100;
                        }
                        if (result.FaceConfidence > 0 && result.FaceConfidence <= 1)
                        {
                            result.FaceConfidence = result.FaceConfidence * 100;
                        }

                        // Note: SimilarityPercentage is a computed property based on FaceSimilarity

                        _logger.LogInformation("   - Face Match: {FaceMatch}", result.FaceMatch);
                        _logger.LogInformation("   - Face Similarity: {Similarity}%", result.FaceSimilarity);
                        _logger.LogInformation("   - Face Confidence: {Confidence}%", result.FaceConfidence);
                    }

                    // ===== DAVAO CITY RESIDENCE VALIDATION (CRITICAL) =====
                    // Check if the address contains "Davao City" or "City of Davao"
                    var addressText = $"{result.Address} {result.Address2} {result.City}".ToLower();

                    bool isDavaoCityResident = addressText.Contains("davao city") ||
                                               addressText.Contains("city of davao") ||
                                               addressText.Contains("davao, city") ||
                                               (result.City != null && result.City.ToLower().Contains("davao"));

                    result.IsDavaoCityResident = isDavaoCityResident;

                    if (isDavaoCityResident)
                    {
                        result.ResidenceValidationMessage = "✅ Davao City residence confirmed";
                        _logger.LogInformation("✅ DAVAO CITY RESIDENT CONFIRMED");
                    }
                    else
                    {
                        result.ResidenceValidationMessage = "⚠️ Address does not indicate Davao City residence. Please verify manually.";
                        _logger.LogWarning("⚠️ DAVAO CITY RESIDENCE NOT CONFIRMED - Address: {Address}, City: {City}",
                            result.Address ?? "N/A", result.City ?? "N/A");
                    }

                    _logger.LogInformation("📋 Extracted Data:");
                    _logger.LogInformation("   - First Name: {FirstName}", result.FirstName ?? "N/A");
                    _logger.LogInformation("   - Last Name: {LastName}", result.LastName ?? "N/A");
                    _logger.LogInformation("   - Middle Name: {MiddleName}", result.MiddleName ?? "N/A");
                    _logger.LogInformation("   - Full Name: {FullName}", result.FullName ?? "N/A");
                    _logger.LogInformation("   - Suffix: {Suffix}", result.Suffix ?? "N/A");
                    _logger.LogInformation("   - DOB: {DOB}", result.DateOfBirth ?? "N/A");
                    _logger.LogInformation("   - Age: {Age}", result.Age?.ToString() ?? "N/A");
                    _logger.LogInformation("   - Sex: {Sex}", result.Sex ?? "N/A");
                    _logger.LogInformation("   - Document Number: {DocNum}", result.DocumentNumber ?? "N/A");
                    _logger.LogInformation("   - Document Type: {DocType}", result.DocumentType ?? "N/A");
                    _logger.LogInformation("   - Document Name: {DocName}", result.DocumentName ?? "N/A");
                    _logger.LogInformation("   - Address 1: {Address}", result.Address ?? "N/A");
                    _logger.LogInformation("   - Address 2: {Address2}", result.Address2 ?? "N/A");
                    _logger.LogInformation("   - City: {City}", result.City ?? "N/A");
                    _logger.LogInformation("   - State: {State}", result.State ?? "N/A");
                    _logger.LogInformation("   - Postal Code: {PostalCode}", result.PostalCode ?? "N/A");
                    _logger.LogInformation("   - Country: {Country}", result.Country ?? "N/A");
                    _logger.LogInformation("   - Davao City Resident: {IsDavaoCityResident}", result.IsDavaoCityResident ? "YES" : "NO");
                }
                else
                {
                    _logger.LogWarning("⚠️ No 'data' property found in response");
                }

                // Check for AML results
                if (root.TryGetProperty("aml", out var amlObj))
                {
                    _logger.LogInformation("⚖️ Found 'aml' property in response");
                    // ID Analyzer v2 AML structure usually has a 'passed' boolean or similar status
                    // Assuming simple check for now based on common response
                    // If 'aml' exists, we check if any matches found. 
                    // Often it has a 'found' count or 'matches' array.
                    // Let's assume 'passed' property if available, otherwise check matches.
                    
                    // Note: Specific AML response structure depends on API version. 
                    // We will log it and try to determine status.
                    
                    // For now, let's check if 'passed' exists
                    if (amlObj.ValueKind == JsonValueKind.Object) 
                    {
                         // Some versions return { "passed": true/false }
                         if (amlObj.TryGetProperty("passed", out var amlPassed))
                         {
                             result.AmlPassed = amlPassed.GetBoolean();
                         }
                         // Or check failure status
                         else if (amlObj.TryGetProperty("status", out var amlStatus))
                         {
                             result.AmlPassed = amlStatus.GetString() != "found";
                         }
                    }
                    _logger.LogInformation("   - AML Passed: {AmlPassed}", result.AmlPassed);
                }

                // Check for Authentication results
                if (root.TryGetProperty("authentication", out var authObj))
                {
                    _logger.LogInformation("🛡️ Found 'authentication' property in response");
                    result.AuthenticationScore = ExtractDoubleValue(authObj, "score");
                    _logger.LogInformation("   - Auth Score: {Score}", result.AuthenticationScore);
                }

                // Check for warnings
                if (root.TryGetProperty("warning", out var warningArray) && warningArray.ValueKind == JsonValueKind.Array)
                {
                    var warnings = new List<string>();
                    foreach (var warning in warningArray.EnumerateArray())
                    {
                        if (warning.TryGetProperty("description", out var desc))
                        {
                            warnings.Add(desc.GetString() ?? "Unknown warning");
                        }
                    }
                    result.Warnings = warnings;
                    _logger.LogInformation("⚠️ Warnings: {Count}", warnings.Count);
                    foreach (var warn in warnings)
                    {
                        _logger.LogInformation("   - {Warning}", warn);
                    }
                }

                // Decision is already extracted at initialization (line 170)
                _logger.LogInformation("🎯 API Decision: {Decision}", result.Decision);

                // Determine verification passed
                bool ocrPassed = !string.IsNullOrEmpty(result.DocumentNumber) && !string.IsNullOrEmpty(result.LastName);

                if (!string.IsNullOrEmpty(faceImageBase64))
                {
                    result.VerificationPassed = ocrPassed && result.FaceMatch;
                    _logger.LogInformation("✅ Verification (with face): OCR={OCR}, Face={Face}, Overall={Overall}",
                        ocrPassed, result.FaceMatch, result.VerificationPassed);
                }
                else
                {
                    result.VerificationPassed = ocrPassed;
                    _logger.LogInformation("✅ Verification (without face): OCR={OCR}, Overall={Overall}",
                        ocrPassed, result.VerificationPassed);
                }

                if (string.IsNullOrEmpty(result.Decision) || result.Decision == "processed")
                {
                    result.Decision = result.VerificationPassed ? "approved" : "review";
                }

                // ===== NAME VALIDATION PREPARATION =====
                // Prepare name fields for validation by controller/UI
                // The name validation will be done by comparing with registered user data
                // This flag will be set by the controller after comparing names
                result.NameMatchesRegistration = false; // Default to false, controller will validate
                result.NameValidationMessage = "Name validation pending - will be verified against registration data";

                _logger.LogInformation("📝 Name fields prepared for validation:");
                _logger.LogInformation("   - ID First Name: {FirstName}", result.FirstName ?? "N/A");
                _logger.LogInformation("   - ID Middle Name: {MiddleName}", result.MiddleName ?? "N/A");
                _logger.LogInformation("   - ID Last Name: {LastName}", result.LastName ?? "N/A");
                _logger.LogInformation("   - ID Suffix: {Suffix}", result.Suffix ?? "N/A");

                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                _logger.LogInformation("✅ ID Analyzer Scan Complete - Success: {Success}, Decision: {Decision}", result.Success, result.Decision);
                _logger.LogInformation("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ EXCEPTION in ID Analyzer ScanIdAsync");
                _logger.LogError("   Exception Type: {ExType}", ex.GetType().Name);
                _logger.LogError("   Exception Message: {Message}", ex.Message);
                _logger.LogError("   Stack Trace: {StackTrace}", ex.StackTrace);

                return new VerificationResult
                {
                    Success = false,
                    ErrorMessage = $"Exception: {ex.Message}"
                };
            }
        }

        public async Task<VerificationResult> VerifyFaceAsync(string faceImageBase64, string referenceImageBase64)
        {
            try
            {
                _logger.LogInformation("Starting ID Analyzer Face Verification...");

                var payload = new Dictionary<string, object>
                {
                    { "profile", _profileId },
                    { "reference", CleanBase64(referenceImageBase64) },
                    { "face", CleanBase64(faceImageBase64) }
                };

                var json = JsonSerializer.Serialize(payload);

                var faceApiUrl = "https://api2.idanalyzer.com/face";

                var request = new HttpRequestMessage(HttpMethod.Post, faceApiUrl);
                request.Headers.Add("X-API-KEY", _apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Face API Response Status: {StatusCode}", response.StatusCode);
                _logger.LogInformation("Face API Response: {Response}", responseJson);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ID Analyzer Face Verification failed: {StatusCode} {Error}", response.StatusCode, responseJson);
                    return new VerificationResult { Success = false, ErrorMessage = $"Face API Error: Status {response.StatusCode}" };
                }

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Check for success
                bool apiSuccess = root.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                bool hasError = root.TryGetProperty("error", out var errorProp);

                if (!apiSuccess || hasError)
                {
                    var errorMsg = "Unknown error from ID Analyzer";
                    if (hasError)
                    {
                        if (errorProp.ValueKind == JsonValueKind.Object && errorProp.TryGetProperty("message", out var errMsg))
                        {
                            errorMsg = errMsg.GetString() ?? errorMsg;
                        }
                        else if (errorProp.ValueKind == JsonValueKind.String)
                        {
                            errorMsg = errorProp.GetString() ?? errorMsg;
                        }
                    }

                    _logger.LogError("Face API error: {ErrorMessage}", errorMsg);
                    return new VerificationResult { Success = false, ErrorMessage = errorMsg };
                }

                var result = new VerificationResult
                {
                    Success = true,
                    TransactionId = root.TryGetProperty("transactionId", out var transId) ? transId.GetString() :
                                    root.TryGetProperty("reference", out var refId) ? refId.GetString() :
                                    Guid.NewGuid().ToString(),
                    Decision = "processed"
                };

                // Extract face match result
                result.FaceMatch = ExtractBoolValue(root, "match", "isIdentical", "verified");
                result.FaceConfidence = ExtractDoubleValue(root, "score", "confidence", "similarity");
                result.FaceSimilarity = result.FaceConfidence;
                result.VerificationPassed = result.FaceMatch;

                result.Decision = result.VerificationPassed ? "approved" : "declined";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ID Analyzer VerifyFaceAsync");
                return new VerificationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        private string CleanBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return "";

            // Remove data URI prefix if present
            if (base64.Contains(","))
            {
                base64 = base64.Split(',')[1];
            }

            return base64.Trim();
        }

        private string? ExtractFieldValue(JsonElement element, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (element.TryGetProperty(fieldName, out var prop))
                {
                    _logger.LogDebug("Found field '{FieldName}' with ValueKind: {ValueKind}", fieldName, prop.ValueKind);

                    // ID Analyzer v2 returns fields as ARRAYS with objects containing value and confidence
                    // Example: "firstName": [{"value": "Juan", "confidence": 0.95}]
                    if (prop.ValueKind == JsonValueKind.Array && prop.GetArrayLength() > 0)
                    {
                        var firstItem = prop[0];

                        // Check if array contains objects with "value" property
                        if (firstItem.ValueKind == JsonValueKind.Object && firstItem.TryGetProperty("value", out var valueProp))
                        {
                            var extractedValue = valueProp.GetString();
                            _logger.LogDebug("Extracted from array[0].value: '{FieldName}' = '{Value}'", fieldName, extractedValue);
                            return extractedValue;
                        }
                        // Fallback: array contains simple strings
                        else if (firstItem.ValueKind == JsonValueKind.String)
                        {
                            var extractedValue = firstItem.GetString();
                            _logger.LogDebug("Extracted from array[0]: '{FieldName}' = '{Value}'", fieldName, extractedValue);
                            return extractedValue;
                        }
                    }
                    // Fallback: Handle object with "value" property (non-array format)
                    else if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("value", out var valueProp))
                    {
                        var extractedValue = valueProp.GetString();
                        _logger.LogDebug("Extracted from object.value: '{FieldName}' = '{Value}'", fieldName, extractedValue);
                        return extractedValue;
                    }
                    // Fallback: Handle simple string value
                    else if (prop.ValueKind == JsonValueKind.String)
                    {
                        var extractedValue = prop.GetString();
                        _logger.LogDebug("Extracted simple string: '{FieldName}' = '{Value}'", fieldName, extractedValue);
                        return extractedValue;
                    }
                    else
                    {
                        _logger.LogWarning("Field '{FieldName}' has unexpected format: {ValueKind}. Value: {PropValue}",
                            fieldName, prop.ValueKind, prop.ToString());
                    }
                }
            }
            return null;
        }

        private bool ExtractBoolValue(JsonElement element, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (element.TryGetProperty(fieldName, out var prop))
                {
                    // Handle array format: [{"value": true, "confidence": 0.95}]
                    if (prop.ValueKind == JsonValueKind.Array && prop.GetArrayLength() > 0)
                    {
                        var firstItem = prop[0];
                        if (firstItem.ValueKind == JsonValueKind.Object && firstItem.TryGetProperty("value", out var valueProp))
                        {
                            return valueProp.GetBoolean();
                        }
                        else if (firstItem.ValueKind == JsonValueKind.True || firstItem.ValueKind == JsonValueKind.False)
                        {
                            return firstItem.GetBoolean();
                        }
                    }
                    // Handle object format: {"value": true, "confidence": 0.95}
                    else if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("value", out var valueProp))
                    {
                        return valueProp.GetBoolean();
                    }
                    // Handle direct boolean
                    else if (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False)
                    {
                        return prop.GetBoolean();
                    }
                }
            }
            return false;
        }

        private double ExtractDoubleValue(JsonElement element, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (element.TryGetProperty(fieldName, out var prop))
                {
                    // Handle array format: [{"value": 0.95, "confidence": 0.98}]
                    if (prop.ValueKind == JsonValueKind.Array && prop.GetArrayLength() > 0)
                    {
                        var firstItem = prop[0];
                        if (firstItem.ValueKind == JsonValueKind.Object && firstItem.TryGetProperty("value", out var valueProp))
                        {
                            return valueProp.GetDouble();
                        }
                        else if (firstItem.ValueKind == JsonValueKind.Number)
                        {
                            return firstItem.GetDouble();
                        }
                    }
                    // Handle object format: {"value": 0.95, "confidence": 0.98}
                    else if (prop.ValueKind == JsonValueKind.Object && prop.TryGetProperty("value", out var valueProp))
                    {
                        return valueProp.GetDouble();
                    }
                    // Handle direct number
                    else if (prop.ValueKind == JsonValueKind.Number)
                    {
                        return prop.GetDouble();
                    }
                }
            }
            return 0;
        }
    }
}
