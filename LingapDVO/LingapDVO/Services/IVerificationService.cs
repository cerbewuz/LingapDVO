using System.Collections.Generic;
using System.Threading.Tasks;

namespace LingapDVO.Services
{
    public interface IVerificationService
    {
        Task<VerificationResult> ScanIdAsync(string documentImageBase64, string? faceImageBase64 = null, string? backImageBase64 = null);
        Task<VerificationResult> VerifyFaceAsync(string faceImageBase64, string referenceImageBase64);
    }

    public class VerificationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCodeString { get; set; }
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
        public string? CivilStatus { get; set; }  // From back ID (National ID)
        public string? DateOfBirth { get; set; }
        public int? Age { get; set; }
        public string? DobDay { get; set; }
        public string? DobMonth { get; set; }
        public string? DobYear { get; set; }
        public string? Nationality { get; set; }
        public string? NationalityIso2 { get; set; }
        public string? NationalityIso3 { get; set; }

        // Document Information
        public string? DocumentNumber { get; set; }
        public string? DocumentType { get; set; }
        public string? DocumentName { get; set; }
        public string? DocumentSide { get; set; }
        public string? IssueDate { get; set; }
        public string? ExpiryDate { get; set; }
        public string? InternalId { get; set; }

        // Address Information (CRITICAL for Davao City validation)
        public string? Address { get; set; }
        public string? Address2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? CountryIso2 { get; set; }
        public string? CountryIso3 { get; set; }

        // Davao City Residence Validation
        public bool IsDavaoCityResident { get; set; }
        public string? ResidenceValidationMessage { get; set; }

        // Name Validation (for comparing registered name vs ID name)
        public bool NameMatchesRegistration { get; set; }
        public string? NameValidationMessage { get; set; }

        // Face Verification
        public bool VerificationPassed { get; set; }
        public bool FaceMatch { get; set; }
        public double FaceSimilarity { get; set; }
        public double FaceConfidence { get; set; }
        public string? FaceImageHash { get; set; }

        // Face verification specific properties
        public bool IsMatch => FaceMatch;
        public double Similarity => FaceSimilarity;
        public int SimilarityPercentage => (int)Math.Round(FaceSimilarity); // FaceSimilarity is already stored as percentage (0-100)
        public double Confidence => FaceConfidence;

        // Additional ID Analyzer v2 fields
        public string? ProfileId { get; set; }
        public int? ReviewScore { get; set; }
        public int? RejectScore { get; set; }
        public string? BackSideId { get; set; }
        public string? ReverseId { get; set; }
        public List<string>? MissingFields { get; set; }
        public string? FrontImageHash { get; set; }
        public long? CreatedAt { get; set; }
        public long? UpdatedAt { get; set; }

        public bool? AmlPassed { get; set; }
        public double? AuthenticationScore { get; set; }
    }
}
