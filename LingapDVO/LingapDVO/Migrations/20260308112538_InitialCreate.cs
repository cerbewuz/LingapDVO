using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adminaccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fullname = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adminaccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Office = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ServiceAvailed = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Contact = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Sex = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TypeOfClient = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssistanceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssistanceId = table.Column<int>(type: "int", nullable: true),
                    Q1_CCKnowledge = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Q2_CCVisibility = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Q3_CCHelpfulness = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    R1_ServiceSatisfaction = table.Column<int>(type: "int", nullable: true),
                    R2_TimeSpent = table.Column<int>(type: "int", nullable: true),
                    R3_ProcessFollowed = table.Column<int>(type: "int", nullable: true),
                    R4_ProcessSimplicity = table.Column<int>(type: "int", nullable: true),
                    R5_InformationAccess = table.Column<int>(type: "int", nullable: true),
                    R6_FairPayment = table.Column<int>(type: "int", nullable: true),
                    R7_Fairness = table.Column<int>(type: "int", nullable: true),
                    R8_EmployeeCourtesy = table.Column<int>(type: "int", nullable: true),
                    Commendation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Suggestion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Request = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Complaint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissionAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmissionToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasValidToken = table.Column<bool>(type: "bit", nullable: false),
                    SuspiciousActivity = table.Column<bool>(type: "bit", nullable: false),
                    SuspiciousReasons = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FormDataHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedFormId = table.Column<int>(type: "int", nullable: true),
                    IsDuplicate = table.Column<bool>(type: "bit", nullable: false),
                    DuplicateDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissionAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormSubmissionTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FormType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedFormId = table.Column<int>(type: "int", nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormSubmissionTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RecipientType = table.Column<int>(type: "int", nullable: false),
                    ApplicationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: true),
                    ApplicantName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status3 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProcessStage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetakeIteration = table.Column<int>(type: "int", nullable: true),
                    IsRetake = table.Column<bool>(type: "bit", nullable: false),
                    IsPermanent = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentViaInApp = table.Column<bool>(type: "bit", nullable: false),
                    SentViaEmail = table.Column<bool>(type: "bit", nullable: false),
                    SentViaSms = table.Column<bool>(type: "bit", nullable: false),
                    NotificationIdentifier = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Register",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fullname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phonenumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Dateofbirth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImageFilename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SecurityQuestions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Securityanswer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Register", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RegistrationToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasValidToken = table.Column<bool>(type: "bit", nullable: false),
                    SuspiciousActivity = table.Column<bool>(type: "bit", nullable: false),
                    SuspiciousReasons = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegisteredUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedByEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Superadminaccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fullname = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Superadminaccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Profilepicture = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreferEmailNotification = table.Column<bool>(type: "bit", nullable: false),
                    PreferSmsNotification = table.Column<bool>(type: "bit", nullable: false),
                    PreferInAppNotification = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuneralAssistance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Lastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Firstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Middlename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlkLotStreet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubVill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Brgy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhilHealth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhilHealthNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Dateofbirth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RLastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RFirstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RMiddlename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RSuffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RBlkLotStreet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RSubVill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RBrgy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Typeassistance = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ForCMOPERSONNEL = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeceasedPersonName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    RelationshipToDeceased = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DateOfDeath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TimeOfDeath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CauseOfDeath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FuneralHomeName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FuneralHomeAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BurialCremationDate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BurialCremationTime = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BurialCremationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Validfrontimage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValidBackimage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoctorPrescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeathCertificate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Processby = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RetakeReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetakeRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRetakeApplication = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuneralAssistance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuneralAssistance_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HospitalAssistance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Lastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Firstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Middlename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlkLotStreet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubVill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Brgy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhilHealth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhilHealthNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Dateofbirth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RLastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RFirstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RMiddlename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RSuffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RBlkLotStreet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RSubVill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RBrgy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelationshipPatient = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Typeassistance = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ForCMOPERSONNEL = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HospitalFacilityName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HospitalFacilityAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DiagnosisMedicalCondition = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HospitalBillCost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AdmissionDate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DischargeDate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WardRoomType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Validfrontimage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValidBackimage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoctorPrescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeathCertificate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Processby = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RetakeReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetakeRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRetakeApplication = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalAssistance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HospitalAssistance_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OtherAssistance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Lastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Firstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Middlename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlkLotStreet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubVill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Brgy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhilHealth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhilHealthNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Dateofbirth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RLastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RFirstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RMiddlename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RSuffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RBlkLotStreet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RSubVill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RBrgy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelationshipPatient = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Typeassistance = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ForCMOPERSONNEL = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MedicineName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MedicineQuantity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MedicineCost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrescribingDoctor = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DoctorContactDetail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LaboratoryCenterName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LaboratoryCenterAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TestName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TestCost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TestOtherInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TherapyFacilityName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TherapyFacilityAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TherapyFacilityContact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TherapyType = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EquipmentName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EquipmentBrand = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EquipmentCategory = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EquipmentQuantity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EquipmentCost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Validfrontimage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValidBackimage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoctorPrescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeathCertificate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MedCertificate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Processby = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RetakeReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetakeRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRetakeApplication = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherAssistance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtherAssistance_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VerifiedAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IDtype = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IDnumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FrontID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BackID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Firstname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Middlename = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Lastname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Dateofbirth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlkLotStreet = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SubVill = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Barangay = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    District = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Phonenumber = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CivilStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    userfacepicture = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    decision = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerifiedAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerifiedAccount_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adminaccount_Fullname",
                table: "Adminaccount",
                column: "Fullname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Adminaccount_Username",
                table: "Adminaccount",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissionAuditLog_UserId_AttemptedAt",
                table: "FormSubmissionAuditLogs",
                columns: new[] { "UserId", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissionToken_UserId_FormType_CreatedAt",
                table: "FormSubmissionTokens",
                columns: new[] { "UserId", "FormType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FuneralAssistance_Status_Status2_CreatedAt",
                table: "FuneralAssistance",
                columns: new[] { "Status", "Status2", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FuneralAssistance_Status3_CreatedAt",
                table: "FuneralAssistance",
                columns: new[] { "Status3", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FuneralAssistance_UserId_CreatedAt",
                table: "FuneralAssistance",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalAssistance_Status_Status2_CreatedAt",
                table: "HospitalAssistance",
                columns: new[] { "Status", "Status2", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalAssistance_Status3_CreatedAt",
                table: "HospitalAssistance",
                columns: new[] { "Status3", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalAssistance_UserId_CreatedAt",
                table: "HospitalAssistance",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ApplicationType_ApplicationId_CreatedAt",
                table: "Notifications",
                columns: new[] { "ApplicationType", "ApplicationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_NotificationIdentifier",
                table: "Notifications",
                column: "NotificationIdentifier");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientType_IsRead_CreatedAt_IsArchived",
                table: "Notifications",
                columns: new[] { "RecipientType", "IsRead", "CreatedAt", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type_ProcessStage_DisplayOrder",
                table: "Notifications",
                columns: new[] { "Type", "ProcessStage", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt_IsArchived",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtherAssistance_Status_Status2_CreatedAt",
                table: "OtherAssistance",
                columns: new[] { "Status", "Status2", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtherAssistance_Status3_CreatedAt",
                table: "OtherAssistance",
                columns: new[] { "Status3", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtherAssistance_UserId_CreatedAt",
                table: "OtherAssistance",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Register_Email",
                table: "Register",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Register_Fullname",
                table: "Register",
                column: "Fullname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Register_Phonenumber",
                table: "Register",
                column: "Phonenumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Register_Username",
                table: "Register",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Superadminaccount_Fullname",
                table: "Superadminaccount",
                column: "Fullname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Superadminaccount_Username",
                table: "Superadminaccount",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_Email",
                table: "UserAccount",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccount_Username",
                table: "UserAccount",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerifiedAccount_UserId",
                table: "VerifiedAccount",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adminaccount");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "FormSubmissionAuditLogs");

            migrationBuilder.DropTable(
                name: "FormSubmissionTokens");

            migrationBuilder.DropTable(
                name: "FuneralAssistance");

            migrationBuilder.DropTable(
                name: "HospitalAssistance");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OtherAssistance");

            migrationBuilder.DropTable(
                name: "Register");

            migrationBuilder.DropTable(
                name: "RegistrationAuditLogs");

            migrationBuilder.DropTable(
                name: "RegistrationTokens");

            migrationBuilder.DropTable(
                name: "Superadminaccount");

            migrationBuilder.DropTable(
                name: "VerifiedAccount");

            migrationBuilder.DropTable(
                name: "UserAccount");
        }
    }
}
