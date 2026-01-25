using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LingapDVO.Services
{
    /// <summary>
    /// Service for OCR.space API integration
    /// Used for extracting text data from Philippine government IDs (National ID, Driver's License, UMID)
    /// API Documentation: https://ocr.space/ocrapi
    /// </summary>
    public interface IOcrSpaceService
    {
        Task<OcrSpaceResult> ExtractTextFromImageAsync(string base64Image, string language = "eng");
        Task<OcrSpaceResult> ExtractTextFromImagesAsync(string frontImageBase64, string backImageBase64, string language = "eng");
        OcrExtractedData ParsePhilippineIdText(string ocrText, string idType);
        string DetectIdTypeFromText(string ocrText);
        bool ValidateIdTypeMatch(string ocrText, string expectedIdType);
    }

    public class OcrSpaceService : IOcrSpaceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OcrSpaceService> _logger;
        private readonly string _apiKey;
        private readonly string _apiUrl;

        public OcrSpaceService(HttpClient httpClient, ILogger<OcrSpaceService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["OcrSpaceSettings:ApiKey"] ?? throw new InvalidOperationException("OCR.space API key not configured");
            _apiUrl = configuration["OcrSpaceSettings:ApiUrl"] ?? "https://api.ocr.space/parse/image";

            _logger.LogInformation("OCR.space Service initialized - API URL: {ApiUrl}", _apiUrl);
        }

        /// <summary>
        /// Extract text from a single image using OCR.space API
        /// </summary>
        public async Task<OcrSpaceResult> ExtractTextFromImageAsync(string base64Image, string language = "eng")
        {
            try
            {
                _logger.LogInformation("Starting OCR.space text extraction...");

                // Clean base64 string (remove data URI prefix if present)
                var cleanBase64 = CleanBase64(base64Image);

                // Prepare form data for OCR.space API
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent($"data:image/jpeg;base64,{cleanBase64}"), "base64Image");
                formData.Add(new StringContent(_apiKey), "apikey");
                formData.Add(new StringContent(language), "language");
                formData.Add(new StringContent("2"), "OCREngine"); // Engine 2 is better for IDs
                formData.Add(new StringContent("true"), "isTable");
                formData.Add(new StringContent("true"), "detectOrientation");
                formData.Add(new StringContent("true"), "scale");

                // Send request
                var response = await _httpClient.PostAsync(_apiUrl, formData);
                var responseJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("OCR.space Response Status: {StatusCode}", response.StatusCode);
                _logger.LogDebug("OCR.space Response: {Response}", responseJson);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OCR.space API error: {StatusCode} - {Response}", response.StatusCode, responseJson);
                    return new OcrSpaceResult
                    {
                        Success = false,
                        ErrorMessage = $"API Error: {response.StatusCode}"
                    };
                }

                // Parse response
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Check for errors
                if (root.TryGetProperty("IsErroredOnProcessing", out var isErrored) && isErrored.GetBoolean())
                {
                    var errorMessage = "Unknown OCR error";
                    if (root.TryGetProperty("ErrorMessage", out var errMsg))
                    {
                        if (errMsg.ValueKind == JsonValueKind.Array && errMsg.GetArrayLength() > 0)
                        {
                            errorMessage = errMsg[0].GetString() ?? errorMessage;
                        }
                        else if (errMsg.ValueKind == JsonValueKind.String)
                        {
                            errorMessage = errMsg.GetString() ?? errorMessage;
                        }
                    }
                    _logger.LogError("OCR.space processing error: {Error}", errorMessage);
                    return new OcrSpaceResult { Success = false, ErrorMessage = errorMessage };
                }

                // Extract parsed text
                var extractedText = "";
                if (root.TryGetProperty("ParsedResults", out var parsedResults) && parsedResults.ValueKind == JsonValueKind.Array)
                {
                    foreach (var result in parsedResults.EnumerateArray())
                    {
                        if (result.TryGetProperty("ParsedText", out var parsedText))
                        {
                            extractedText += parsedText.GetString() + "\n";
                        }
                    }
                }

                _logger.LogInformation("OCR.space extracted {Length} characters of text", extractedText.Length);
                _logger.LogDebug("Extracted Text:\n{Text}", extractedText);

                return new OcrSpaceResult
                {
                    Success = true,
                    ExtractedText = extractedText.Trim(),
                    RawResponse = responseJson
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in OCR.space text extraction");
                return new OcrSpaceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Extract text from front and back images and combine results
        /// </summary>
        public async Task<OcrSpaceResult> ExtractTextFromImagesAsync(string frontImageBase64, string backImageBase64, string language = "eng")
        {
            try
            {
                _logger.LogInformation("Starting OCR.space extraction for front and back ID images...");

                // Extract from front image
                var frontResult = await ExtractTextFromImageAsync(frontImageBase64, language);
                if (!frontResult.Success)
                {
                    _logger.LogWarning("Front ID OCR failed: {Error}", frontResult.ErrorMessage);
                }

                // Extract from back image (if provided)
                OcrSpaceResult? backResult = null;
                if (!string.IsNullOrEmpty(backImageBase64))
                {
                    backResult = await ExtractTextFromImageAsync(backImageBase64, language);
                    if (!backResult.Success)
                    {
                        _logger.LogWarning("Back ID OCR failed: {Error}", backResult.ErrorMessage);
                    }
                }

                // Combine results
                var combinedText = "";
                if (frontResult.Success)
                {
                    combinedText += "=== FRONT ID ===\n" + frontResult.ExtractedText + "\n\n";
                }
                if (backResult?.Success == true)
                {
                    combinedText += "=== BACK ID ===\n" + backResult.ExtractedText + "\n";
                }

                return new OcrSpaceResult
                {
                    Success = frontResult.Success || (backResult?.Success ?? false),
                    ExtractedText = combinedText.Trim(),
                    FrontText = frontResult.ExtractedText,
                    BackText = backResult?.ExtractedText,
                    ErrorMessage = frontResult.Success ? null : frontResult.ErrorMessage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in combined OCR.space extraction");
                return new OcrSpaceResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Detect and validate ID type from OCR text
        /// Returns the detected ID type if it matches one of the accepted types
        /// </summary>
        public string DetectIdTypeFromText(string ocrText)
        {
            if (string.IsNullOrEmpty(ocrText)) return "";

            var upperText = ocrText.ToUpper();

            // National ID / PhilSys detection patterns
            var nationalIdPatterns = new[] {
                "PHILIPPINE IDENTIFICATION",
                "PHILSYS",
                "PHIL-SYS",
                "REPUBLIKA NG PILIPINAS",
                "PSN",
                "PHILID",
                "NATIONAL ID"
            };

            foreach (var pattern in nationalIdPatterns)
            {
                if (upperText.Contains(pattern))
                {
                    _logger.LogInformation("Detected ID Type: National ID (matched pattern: {Pattern})", pattern);
                    return "National ID";
                }
            }

            // Driver's License detection patterns
            var driversLicensePatterns = new[] {
                "DRIVER'S LICENSE",
                "DRIVER LICENSE",
                "DRIVERS LICENSE",
                "NON-PROFESSIONAL",
                "PROFESSIONAL DRIVER",
                "LAND TRANSPORTATION",
                "LTO"
            };

            foreach (var pattern in driversLicensePatterns)
            {
                if (upperText.Contains(pattern))
                {
                    _logger.LogInformation("Detected ID Type: Driver's License (matched pattern: {Pattern})", pattern);
                    return "Driver's License";
                }
            }

            // UMID detection patterns
            // Note: SSS ID is a separate ID type from UMID - UMID is the Unified Multi-Purpose ID card
            var umidPatterns = new[] {
                "UMID",
                "UNIFIED MULTI-PURPOSE",
                "UNIFIED MULTIPURPOSE"
            };

            foreach (var pattern in umidPatterns)
            {
                if (upperText.Contains(pattern))
                {
                    _logger.LogInformation("Detected ID Type: UMID (matched pattern: {Pattern})", pattern);
                    return "UMID";
                }
            }

            _logger.LogWarning("Could not detect ID type from OCR text");
            return "";
        }

        /// <summary>
        /// Validate that the OCR text appears to be from the expected ID type
        /// </summary>
        public bool ValidateIdTypeMatch(string ocrText, string expectedIdType)
        {
            var detectedType = DetectIdTypeFromText(ocrText);

            if (string.IsNullOrEmpty(detectedType))
            {
                // Could not detect ID type - allow it to proceed but log warning
                _logger.LogWarning("Could not validate ID type match - detection failed");
                return true; // Allow to proceed
            }

            var normalizedExpected = NormalizeIdType(expectedIdType).ToLower();
            var normalizedDetected = detectedType.ToLower();

            var isMatch = normalizedExpected == normalizedDetected;

            if (!isMatch)
            {
                _logger.LogWarning("ID type mismatch! Expected: {Expected}, Detected: {Detected}",
                    expectedIdType, detectedType);
            }
            else
            {
                _logger.LogInformation("ID type validated: {IdType}", detectedType);
            }

            return isMatch;
        }

        /// <summary>
        /// Parse OCR text to extract structured data for Philippine IDs
        /// Supports: National ID (PhilSys), Driver's License, UMID
        /// </summary>
        public OcrExtractedData ParsePhilippineIdText(string ocrText, string idType)
        {
            _logger.LogInformation("Parsing OCR text for ID type: {IdType}", idType);
            _logger.LogDebug("OCR Text to parse:\n{Text}", ocrText);

            var result = new OcrExtractedData();
            var normalizedIdType = NormalizeIdType(idType);

            // Try to detect ID type from OCR text if not provided
            if (string.IsNullOrEmpty(normalizedIdType) || normalizedIdType.ToLower() == "generic")
            {
                var detectedType = DetectIdTypeFromText(ocrText);
                if (!string.IsNullOrEmpty(detectedType))
                {
                    normalizedIdType = detectedType;
                    _logger.LogInformation("Using detected ID type: {IdType}", normalizedIdType);
                }
            }

            // Normalize text for parsing (preserve special characters like n with tilde)
            var text = ocrText ?? "";
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToList();

            _logger.LogInformation("Processing {LineCount} lines of OCR text", lines.Count);

            // Set ID Type
            result.IdType = normalizedIdType;

            // Extract based on ID type
            switch (normalizedIdType.ToLower())
            {
                case "national id":
                case "phil-id":
                case "philsys":
                    result = ParsePhilSysId(lines, text);
                    result.IdType = "National ID";
                    break;

                case "driver's license":
                case "driver-license":
                case "drivers license":
                    result = ParseDriversLicense(lines, text);
                    result.IdType = "Driver's License";
                    break;

                case "umid":
                    result = ParseUmid(lines, text);
                    result.IdType = "UMID";
                    break;

                default:
                    // Try generic parsing
                    result = ParseGenericId(lines, text);
                    result.IdType = idType;
                    break;
            }

            // Clean up extracted data
            CleanExtractedData(result);

            _logger.LogInformation("Extraction complete - ID#: {IdNumber}, Name: {FirstName} {MiddleName} {LastName}",
                result.IdNumber ?? "N/A",
                result.FirstName ?? "N/A",
                result.MiddleName ?? "N/A",
                result.LastName ?? "N/A");

            return result;
        }

        #region Philippine ID Parsers

        /// <summary>
        /// Parse Philippine National ID (PhilSys)
        /// Format: PSN-XXXX-XXXX-XXXX-XXXX
        /// </summary>
        private OcrExtractedData ParsePhilSysId(List<string> lines, string fullText)
        {
            var result = new OcrExtractedData();

            // Extract PhilSys Number (PSN) - Format: PSN-XXXX-XXXX-XXXX-XXXX or similar patterns
            var psnPatterns = new[]
            {
                @"(?:PSN[:\-\s]*)?(\d{4}[\-\s]\d{4}[\-\s]\d{4}[\-\s]\d{4})",
                @"(?:PSN[:\-\s]*)?([\d]{16})",
                @"(\d{4}\s*\d{4}\s*\d{4}\s*\d{4})"
            };

            foreach (var pattern in psnPatterns)
            {
                var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    result.IdNumber = CleanIdNumber(match.Groups[1].Value);
                    _logger.LogInformation("Found PhilSys Number: {PSN}", result.IdNumber);
                    break;
                }
            }

            // Extract names - PhilSys format: SURNAME, GIVEN NAME MIDDLE NAME
            ExtractNamesFromText(lines, fullText, result);

            // Extract Date of Birth
            ExtractDateOfBirth(lines, fullText, result);

            // Extract Sex/Gender
            ExtractGender(lines, fullText, result);

            // Extract Civil Status (commonly found on back of National ID)
            ExtractCivilStatus(lines, fullText, result);

            // Extract Address
            ExtractAddress(lines, fullText, result);

            return result;
        }

        /// <summary>
        /// Parse Philippine Driver's License
        /// Format: LETTER + 2 DIGITS + HYPHEN + 7 DIGITS (e.g., N01-12-345678)
        /// </summary>
        private OcrExtractedData ParseDriversLicense(List<string> lines, string fullText)
        {
            var result = new OcrExtractedData();

            // Extract License Number - Various formats
            var dlPatterns = new[]
            {
                @"(?:LICENSE\s*(?:NO\.?|NUMBER|#)?[:\-\s]*)?([A-Z]\d{2}[\-\s]?\d{2}[\-\s]?\d{6,7})",
                @"([A-Z]\d{2}[\-\s]\d{2}[\-\s]\d{6,7})",
                @"(?:LIC(?:ENSE)?\.?\s*(?:NO\.?)?[:\-\s]*)([A-Z0-9]{3}[\-\s]?\d{2}[\-\s]?\d{6,7})"
            };

            foreach (var pattern in dlPatterns)
            {
                var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    result.IdNumber = CleanIdNumber(match.Groups[1].Value);
                    _logger.LogInformation("Found Driver's License Number: {DLN}", result.IdNumber);
                    break;
                }
            }

            // Extract names
            ExtractNamesFromText(lines, fullText, result);

            // Extract Date of Birth
            ExtractDateOfBirth(lines, fullText, result);

            // Extract Sex/Gender
            ExtractGender(lines, fullText, result);

            // Extract Civil Status (if available)
            ExtractCivilStatus(lines, fullText, result);

            // Extract Address
            ExtractAddress(lines, fullText, result);

            return result;
        }

        /// <summary>
        /// Parse Philippine UMID (Unified Multi-Purpose ID)
        /// Format: XXXX-XXXXXXX-X (CRN format)
        /// </summary>
        private OcrExtractedData ParseUmid(List<string> lines, string fullText)
        {
            var result = new OcrExtractedData();

            // Extract UMID/CRN Number - Format: XXXX-XXXXXXX-X
            var umidPatterns = new[]
            {
                @"(?:CRN|UMID|ID\s*(?:NO\.?)?)[:\-\s]*(\d{4}[\-\s]?\d{7}[\-\s]?\d)",
                @"(\d{4}[\-\s]\d{7}[\-\s]\d)",
                @"(\d{4}\d{7}\d)"
            };

            foreach (var pattern in umidPatterns)
            {
                var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    result.IdNumber = CleanIdNumber(match.Groups[1].Value);
                    _logger.LogInformation("Found UMID Number: {UMID}", result.IdNumber);
                    break;
                }
            }

            // Extract names
            ExtractNamesFromText(lines, fullText, result);

            // Extract Date of Birth
            ExtractDateOfBirth(lines, fullText, result);

            // Extract Sex/Gender
            ExtractGender(lines, fullText, result);

            // Extract Civil Status (if available)
            ExtractCivilStatus(lines, fullText, result);

            // Extract Address
            ExtractAddress(lines, fullText, result);

            return result;
        }

        /// <summary>
        /// Generic ID parser for unrecognized ID types
        /// </summary>
        private OcrExtractedData ParseGenericId(List<string> lines, string fullText)
        {
            var result = new OcrExtractedData();

            // Try to find any ID-like number
            var idPatterns = new[]
            {
                @"(?:ID\s*(?:NO\.?|NUMBER|#)?[:\-\s]*)([A-Z0-9\-]{8,20})",
                @"(?:NO\.?[:\-\s]*)([A-Z0-9\-]{8,20})",
                @"(\d{4}[\-\s]\d{4}[\-\s]\d{4}[\-\s]\d{4})",
                @"(\d{4}[\-\s]\d{7}[\-\s]\d)"
            };

            foreach (var pattern in idPatterns)
            {
                var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    result.IdNumber = CleanIdNumber(match.Groups[1].Value);
                    break;
                }
            }

            ExtractNamesFromText(lines, fullText, result);
            ExtractDateOfBirth(lines, fullText, result);
            ExtractGender(lines, fullText, result);
            ExtractCivilStatus(lines, fullText, result);
            ExtractAddress(lines, fullText, result);

            return result;
        }

        #endregion

        #region Field Extraction Helpers

        /// <summary>
        /// Extract names (First, Middle, Last, Suffix) from OCR text
        /// Handles Filipino name formats and special characters like ñ
        /// </summary>
        private void ExtractNamesFromText(List<string> lines, string fullText, OcrExtractedData result)
        {
            // Common patterns for names in Philippine IDs
            // Format 1: SURNAME, GIVEN NAME MIDDLE NAME
            // Format 2: LAST NAME: XXX, FIRST NAME: XXX, MIDDLE NAME: XXX
            // Format 3: FULL NAME: SURNAME, GIVEN NAME MIDDLE NAME

            // Pattern for labeled names
            var labeledPatterns = new Dictionary<string, string[]>
            {
                { "LastName", new[] {
                    @"(?:LAST\s*NAME|SURNAME|APE?LLIDO)[:\-\s]*([A-Za-zÑñ\-\s']+)",
                    @"(?:HULING\s*PANGALAN)[:\-\s]*([A-Za-zÑñ\-\s']+)"
                }},
                { "FirstName", new[] {
                    @"(?:FIRST\s*NAME|GIVEN\s*NAME|PANGALAN)[:\-\s]*([A-Za-zÑñ\-\s']+)",
                    @"(?:UNANG\s*PANGALAN)[:\-\s]*([A-Za-zÑñ\-\s']+)"
                }},
                { "MiddleName", new[] {
                    @"(?:MIDDLE\s*NAME|GITNANG\s*PANGALAN)[:\-\s]*([A-Za-zÑñ\-\s']+)",
                    @"(?:M\.?\s*N\.?)[:\-\s]*([A-Za-zÑñ\-\s']+)"
                }},
                { "Suffix", new[] {
                    @"(?:SUFFIX|EXT(?:ENSION)?)[:\-\s]*(JR\.?|SR\.?|I{1,3}|IV|V|VI{0,3})",
                    @"\b(JR\.?|SR\.?|III?|IV|V|VI{0,3})\b"
                }}
            };

            // Try labeled patterns first
            foreach (var field in labeledPatterns)
            {
                foreach (var pattern in field.Value)
                {
                    var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var value = match.Groups[1].Value.Trim();
                        value = CleanName(value);

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            switch (field.Key)
                            {
                                case "LastName":
                                    if (string.IsNullOrEmpty(result.LastName)) result.LastName = value;
                                    break;
                                case "FirstName":
                                    if (string.IsNullOrEmpty(result.FirstName)) result.FirstName = value;
                                    break;
                                case "MiddleName":
                                    if (string.IsNullOrEmpty(result.MiddleName)) result.MiddleName = value;
                                    break;
                                case "Suffix":
                                    if (string.IsNullOrEmpty(result.Suffix)) result.Suffix = value.ToUpper();
                                    break;
                            }
                            _logger.LogDebug("Extracted {Field}: {Value}", field.Key, value);
                        }
                    }
                }
            }

            // If names not found, try format: SURNAME, GIVEN NAME MIDDLE NAME
            if (string.IsNullOrEmpty(result.LastName) || string.IsNullOrEmpty(result.FirstName))
            {
                // Look for "NAME:" or full name line
                var nameLinePattern = @"(?:FULL\s*NAME|NAME)[:\-\s]*([A-Za-zÑñ\-\s',]+)";
                var nameMatch = Regex.Match(fullText, nameLinePattern, RegexOptions.IgnoreCase);

                if (nameMatch.Success)
                {
                    ParseFullNameLine(nameMatch.Groups[1].Value, result);
                }
                else
                {
                    // Try to find name pattern: UPPERCASE WORDS, UPPERCASE WORDS
                    var fullNamePattern = @"([A-Z][A-Za-zÑñ\-']+(?:\s+[A-Z][A-Za-zÑñ\-']+)*),\s*([A-Z][A-Za-zÑñ\-'\s]+)";
                    var fullMatch = Regex.Match(fullText, fullNamePattern);
                    if (fullMatch.Success)
                    {
                        result.LastName = CleanName(fullMatch.Groups[1].Value);
                        var givenNames = fullMatch.Groups[2].Value.Trim();
                        ParseGivenNames(givenNames, result);
                    }
                }
            }
        }

        /// <summary>
        /// Parse a full name line in format: SURNAME, GIVEN NAME MIDDLE NAME
        /// </summary>
        private void ParseFullNameLine(string nameLine, OcrExtractedData result)
        {
            nameLine = nameLine.Trim();

            // Check for comma-separated format
            if (nameLine.Contains(","))
            {
                var parts = nameLine.Split(',', 2);
                result.LastName = CleanName(parts[0]);

                if (parts.Length > 1)
                {
                    ParseGivenNames(parts[1].Trim(), result);
                }
            }
            else
            {
                // Space-separated: assume first word is first name, last word is last name
                var words = nameLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length >= 2)
                {
                    result.FirstName = CleanName(words[0]);
                    result.LastName = CleanName(words[words.Length - 1]);

                    if (words.Length > 2)
                    {
                        result.MiddleName = CleanName(string.Join(" ", words.Skip(1).Take(words.Length - 2)));
                    }
                }
            }
        }

        /// <summary>
        /// Parse given names (first name, middle name, suffix)
        /// </summary>
        private void ParseGivenNames(string givenNames, OcrExtractedData result)
        {
            // Check for suffix
            var suffixes = new[] { "JR", "JR.", "SR", "SR.", "II", "III", "IV", "V", "VI" };
            foreach (var suffix in suffixes)
            {
                if (givenNames.ToUpper().EndsWith(" " + suffix) || givenNames.ToUpper().EndsWith(suffix))
                {
                    result.Suffix = suffix.Replace(".", "");
                    givenNames = Regex.Replace(givenNames, $@"\s*{Regex.Escape(suffix)}\s*$", "", RegexOptions.IgnoreCase).Trim();
                    break;
                }
            }

            var nameParts = givenNames.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length >= 1)
            {
                result.FirstName = CleanName(nameParts[0]);
            }
            if (nameParts.Length >= 2)
            {
                result.MiddleName = CleanName(string.Join(" ", nameParts.Skip(1)));
            }
        }

        /// <summary>
        /// Extract date of birth from OCR text
        /// </summary>
        private void ExtractDateOfBirth(List<string> lines, string fullText, OcrExtractedData result)
        {
            var dobPatterns = new[]
            {
                @"(?:DATE\s*OF\s*BIRTH|D\.?O\.?B\.?|BIRTH\s*DATE|KAPANGANAKAN|PETSA\s*NG\s*KAPANGANAKAN)[:\-\s]*(\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4})",
                @"(?:DATE\s*OF\s*BIRTH|D\.?O\.?B\.?|BIRTH\s*DATE)[:\-\s]*([A-Za-z]+\s+\d{1,2},?\s+\d{4})",
                @"(?:DATE\s*OF\s*BIRTH|D\.?O\.?B\.?|BIRTH\s*DATE)[:\-\s]*(\d{1,2}\s+[A-Za-z]+\s+\d{4})",
                @"(?:BORN)[:\-\s]*(\d{1,2}[\/\-\.]\d{1,2}[\/\-\.]\d{2,4})"
            };

            foreach (var pattern in dobPatterns)
            {
                var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    result.DateOfBirth = NormalizeDateFormat(match.Groups[1].Value);
                    _logger.LogDebug("Extracted DOB: {DOB}", result.DateOfBirth);
                    break;
                }
            }
        }

        /// <summary>
        /// Extract gender/sex from OCR text
        /// </summary>
        private void ExtractGender(List<string> lines, string fullText, OcrExtractedData result)
        {
            var genderPatterns = new[]
            {
                @"(?:SEX|GENDER|KASARIAN)[:\-\s]*(MALE|FEMALE|M|F|LALAKI|BABAE)",
                @"\b(MALE|FEMALE)\b",
                @"(?:SEX|GENDER)[:\-\s]*([MF])\b"
            };

            foreach (var pattern in genderPatterns)
            {
                var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var gender = match.Groups[1].Value.ToUpper();
                    result.Gender = NormalizeGender(gender);
                    _logger.LogDebug("Extracted Gender: {Gender}", result.Gender);
                    break;
                }
            }
        }

        /// <summary>
        /// Extract civil status from OCR text
        /// Common values: Single, Married, Widowed, Separated, Annulled
        /// </summary>
        private void ExtractCivilStatus(List<string> lines, string fullText, OcrExtractedData result)
        {
            var civilStatusPatterns = new[]
            {
                @"(?:CIVIL\s*STATUS|MARITAL\s*STATUS|STATUS|KALAGAYAN)[:\-\s]*(SINGLE|MARRIED|WIDOWED|WIDOW|SEPARATED|ANNULLED|DIVORCED)",
                @"\b(SINGLE|MARRIED|WIDOWED|WIDOW|SEPARATED|ANNULLED|DIVORCED)\b"
            };

            foreach (var pattern in civilStatusPatterns)
            {
                var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var status = match.Groups[1].Value.ToUpper().Trim();
                    result.CivilStatus = NormalizeCivilStatus(status);
                    _logger.LogDebug("Extracted Civil Status: {CivilStatus}", result.CivilStatus);
                    break;
                }
            }
        }

        /// <summary>
        /// Normalize civil status value
        /// </summary>
        private string NormalizeCivilStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "";

            var upper = status.ToUpper().Trim();

            return upper switch
            {
                "SINGLE" => "Single",
                "MARRIED" => "Married",
                "WIDOWED" or "WIDOW" => "Widowed",
                "SEPARATED" => "Separated",
                "ANNULLED" => "Annulled",
                "DIVORCED" => "Divorced",
                _ => status
            };
        }

        /// <summary>
        /// Extract address from OCR text
        /// </summary>
        private void ExtractAddress(List<string> lines, string fullText, OcrExtractedData result)
        {
            var addressPatterns = new[]
            {
                @"(?:ADDRESS|TIRAHAN|LUGAR\s*NG\s*TIRAHAN)[:\-\s]*(.+?)(?=\n[A-Z]+:|$)",
                @"(?:ADD(?:RESS)?\.?)[:\-\s]*(.+?)(?=\n|$)"
            };

            foreach (var pattern in addressPatterns)
            {
                var match = Regex.Match(fullText, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                {
                    var address = match.Groups[1].Value.Trim();
                    // Clean up multiple spaces and newlines
                    address = Regex.Replace(address, @"\s+", " ");
                    address = CleanAddress(address);

                    if (!string.IsNullOrWhiteSpace(address) && address.Length > 5)
                    {
                        result.Address = address;
                        _logger.LogDebug("Extracted Address: {Address}", result.Address);
                        break;
                    }
                }
            }

            // If no address found, look for Davao City mention (common requirement)
            if (string.IsNullOrEmpty(result.Address))
            {
                var davaoMatch = Regex.Match(fullText, @"(.+(?:DAVAO\s*CITY|CITY\s*OF\s*DAVAO).+)", RegexOptions.IgnoreCase);
                if (davaoMatch.Success)
                {
                    result.Address = CleanAddress(davaoMatch.Groups[1].Value);
                }
            }
        }

        #endregion

        #region Utility Methods

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

        private string CleanIdNumber(string idNumber)
        {
            if (string.IsNullOrEmpty(idNumber)) return "";

            // Remove extra spaces but keep hyphens
            return Regex.Replace(idNumber.Trim(), @"\s+", "-");
        }

        private string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            // Remove numbers and special characters except letters, spaces, hyphens, apostrophes, and ñ
            name = Regex.Replace(name, @"[^\p{L}\s\-']", "");

            // Remove extra spaces
            name = Regex.Replace(name.Trim(), @"\s+", " ");

            // Title case
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
        }

        private string CleanAddress(string address)
        {
            if (string.IsNullOrEmpty(address)) return "";

            // Remove obvious garbage patterns but preserve special characters
            address = Regex.Replace(address, @"[^\p{L}\p{N}\s\-,.\#']", " ");
            address = Regex.Replace(address.Trim(), @"\s+", " ");

            return address;
        }

        private string NormalizeIdType(string idType)
        {
            if (string.IsNullOrEmpty(idType)) return "";

            var normalized = idType.ToLower().Trim();

            if (normalized.Contains("national") || normalized.Contains("philsys") || normalized.Contains("phil-id"))
                return "National ID";
            if (normalized.Contains("driver") || normalized.Contains("license"))
                return "Driver's License";
            if (normalized.Contains("umid") || normalized.Contains("unified"))
                return "UMID";

            return idType;
        }

        private string NormalizeGender(string gender)
        {
            if (string.IsNullOrEmpty(gender)) return "";

            var upper = gender.ToUpper().Trim();

            if (upper == "M" || upper == "MALE" || upper == "LALAKI")
                return "Male";
            if (upper == "F" || upper == "FEMALE" || upper == "BABAE")
                return "Female";

            return gender;
        }

        private string NormalizeDateFormat(string date)
        {
            if (string.IsNullOrEmpty(date)) return "";

            // Try to parse and format as YYYY-MM-DD
            try
            {
                // Handle various formats
                var formats = new[]
                {
                    "MM/dd/yyyy", "M/d/yyyy", "dd/MM/yyyy", "d/M/yyyy",
                    "MM-dd-yyyy", "M-d-yyyy", "dd-MM-yyyy", "d-M-yyyy",
                    "MM.dd.yyyy", "M.d.yyyy", "dd.MM.yyyy", "d.M.yyyy",
                    "MMMM d, yyyy", "MMMM dd, yyyy", "MMM d, yyyy", "MMM dd, yyyy",
                    "d MMMM yyyy", "dd MMMM yyyy", "d MMM yyyy", "dd MMM yyyy"
                };

                if (DateTime.TryParseExact(date.Trim(), formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedDate))
                {
                    return parsedDate.ToString("yyyy-MM-dd");
                }

                // Try generic parse as last resort
                if (DateTime.TryParse(date, out parsedDate))
                {
                    return parsedDate.ToString("yyyy-MM-dd");
                }
            }
            catch
            {
                // Return original if parsing fails
            }

            return date.Trim();
        }

        private void CleanExtractedData(OcrExtractedData data)
        {
            // Remove garbage text patterns commonly found in OCR
            var garbagePatterns = new[]
            {
                @"^(REPUBLIKA|REPUBLIC|PILIPINAS|PHILIPPINES|DEPARTMENT|LAND|TRANSPORTATION)$",
                @"^\d+$",
                @"^[A-Z]{1,2}$"
            };

            void CleanField(ref string? field)
            {
                if (string.IsNullOrEmpty(field)) return;

                foreach (var pattern in garbagePatterns)
                {
                    if (Regex.IsMatch(field, pattern, RegexOptions.IgnoreCase))
                    {
                        field = null;
                        return;
                    }
                }
            }

            // Don't clean these as garbage - they are valid data
            // CleanField(ref data.FirstName);
            // CleanField(ref data.MiddleName);
            // CleanField(ref data.LastName);
        }

        #endregion
    }

    /// <summary>
    /// Result from OCR.space API call
    /// </summary>
    public class OcrSpaceResult
    {
        public bool Success { get; set; }
        public string? ExtractedText { get; set; }
        public string? FrontText { get; set; }
        public string? BackText { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawResponse { get; set; }
    }

    /// <summary>
    /// Structured data extracted from OCR text
    /// </summary>
    public class OcrExtractedData
    {
        public string? IdType { get; set; }
        public string? IdNumber { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Suffix { get; set; }
        public string? Gender { get; set; }
        public string? DateOfBirth { get; set; }
        public string? CivilStatus { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Barangay { get; set; }
        public double Confidence { get; set; }
    }
}
