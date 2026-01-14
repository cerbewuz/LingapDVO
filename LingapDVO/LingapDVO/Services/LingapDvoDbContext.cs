using System;
using System.Collections.Generic;
using LingapDVO.TempVerify;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Services;

public partial class LingapDvoDbContext : DbContext
{
    public LingapDvoDbContext()
    {
    }

    public LingapDvoDbContext(DbContextOptions<LingapDvoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Adminaccount> Adminaccounts { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<FormSubmissionAuditLog> FormSubmissionAuditLogs { get; set; }

    public virtual DbSet<FormSubmissionToken> FormSubmissionTokens { get; set; }

    public virtual DbSet<FuneralAssistance> FuneralAssistances { get; set; }

    public virtual DbSet<HospitalAssistance> HospitalAssistances { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OtherAssistance> OtherAssistances { get; set; }

    public virtual DbSet<RegistrationAuditLog> RegistrationAuditLogs { get; set; }

    public virtual DbSet<RegistrationToken> RegistrationTokens { get; set; }

    public virtual DbSet<Superadminaccount> Superadminaccounts { get; set; }

    public virtual DbSet<VerifiedAccount> VerifiedAccounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Adminaccount>(entity =>
        {
            entity.ToTable("Adminaccount");

            entity.HasIndex(e => e.Fullname, "IX_Adminaccount_Fullname").IsUnique();

            entity.HasIndex(e => e.Username, "IX_Adminaccount_Username").IsUnique();

            entity.Property(e => e.Fullname).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.Property(e => e.AssistanceType).HasMaxLength(50);
            entity.Property(e => e.Commendation).HasMaxLength(1000);
            entity.Property(e => e.Complaint).HasMaxLength(1000);
            entity.Property(e => e.Contact).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Office).HasMaxLength(200);
            entity.Property(e => e.Q1Ccknowledge)
                .HasMaxLength(200)
                .HasColumnName("Q1_CCKnowledge");
            entity.Property(e => e.Q2Ccvisibility)
                .HasMaxLength(200)
                .HasColumnName("Q2_CCVisibility");
            entity.Property(e => e.Q3Cchelpfulness)
                .HasMaxLength(200)
                .HasColumnName("Q3_CCHelpfulness");
            entity.Property(e => e.R1ServiceSatisfaction).HasColumnName("R1_ServiceSatisfaction");
            entity.Property(e => e.R2TimeSpent).HasColumnName("R2_TimeSpent");
            entity.Property(e => e.R3ProcessFollowed).HasColumnName("R3_ProcessFollowed");
            entity.Property(e => e.R4ProcessSimplicity).HasColumnName("R4_ProcessSimplicity");
            entity.Property(e => e.R5InformationAccess).HasColumnName("R5_InformationAccess");
            entity.Property(e => e.R6FairPayment).HasColumnName("R6_FairPayment");
            entity.Property(e => e.R7Fairness).HasColumnName("R7_Fairness");
            entity.Property(e => e.R8EmployeeCourtesy).HasColumnName("R8_EmployeeCourtesy");
            entity.Property(e => e.Request).HasMaxLength(1000);
            entity.Property(e => e.ServiceAvailed).HasMaxLength(200);
            entity.Property(e => e.Sex).HasMaxLength(50);
            entity.Property(e => e.Signature).HasMaxLength(200);
            entity.Property(e => e.Suggestion).HasMaxLength(1000);
            entity.Property(e => e.TypeOfClient).HasMaxLength(100);
        });

        modelBuilder.Entity<FormSubmissionAuditLog>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.AttemptedAt }, "IX_FormSubmissionAuditLog_UserId_AttemptedAt");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.DuplicateDetails).HasMaxLength(1000);
            entity.Property(e => e.FormDataHash).HasMaxLength(500);
            entity.Property(e => e.FormType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(100);
            entity.Property(e => e.PatientName).HasMaxLength(200);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.RequestorName).HasMaxLength(200);
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.SubmissionToken).HasMaxLength(500);
            entity.Property(e => e.SuspiciousReasons).HasMaxLength(1000);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
        });

        modelBuilder.Entity<FormSubmissionToken>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.FormType, e.CreatedAt }, "IX_FormSubmissionToken_UserId_FormType_CreatedAt");

            entity.Property(e => e.FormType).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(100);
            entity.Property(e => e.Token).HasMaxLength(500);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
        });

        modelBuilder.Entity<FuneralAssistance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Funeralburialform");

            entity.ToTable("FuneralAssistance");

            entity.HasIndex(e => new { e.Status3, e.CreatedAt }, "IX_FuneralAssistance_Status3_CreatedAt");

            entity.HasIndex(e => new { e.Status, e.Status2, e.CreatedAt }, "IX_FuneralAssistance_Status_Status2_CreatedAt");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_FuneralAssistance_UserId_CreatedAt");

            entity.Property(e => e.Age).HasMaxLength(100);
            entity.Property(e => e.BlkLotStreet).HasMaxLength(100);
            entity.Property(e => e.Brgy).HasMaxLength(100);
            entity.Property(e => e.BurialCremationDate).HasMaxLength(200);
            entity.Property(e => e.BurialCremationTime).HasMaxLength(200);
            entity.Property(e => e.BurialCremationType).HasMaxLength(100);
            entity.Property(e => e.CauseOfDeath).HasMaxLength(500);
            entity.Property(e => e.ContactNo).HasMaxLength(100);
            entity.Property(e => e.DateOfDeath).HasMaxLength(200);
            entity.Property(e => e.Dateofbirth).HasMaxLength(100);
            entity.Property(e => e.DeathCertificate).HasMaxLength(100);
            entity.Property(e => e.DeceasedPersonName).HasMaxLength(300);
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.DoctorPrescription).HasMaxLength(100);
            entity.Property(e => e.Firstname).HasMaxLength(100);
            entity.Property(e => e.ForCmopersonnel)
                .HasMaxLength(100)
                .HasColumnName("ForCMOPERSONNEL");
            entity.Property(e => e.FuneralHomeAddress).HasMaxLength(500);
            entity.Property(e => e.FuneralHomeName).HasMaxLength(500);
            entity.Property(e => e.Lastname).HasMaxLength(100);
            entity.Property(e => e.Middlename).HasMaxLength(100);
            entity.Property(e => e.PhilHealth).HasMaxLength(100);
            entity.Property(e => e.PhilHealthNo).HasMaxLength(100);
            entity.Property(e => e.Processby).HasMaxLength(100);
            entity.Property(e => e.RblkLotStreet)
                .HasMaxLength(100)
                .HasColumnName("RBlkLotStreet");
            entity.Property(e => e.Rbrgy)
                .HasMaxLength(100)
                .HasColumnName("RBrgy");
            entity.Property(e => e.Rdistrict)
                .HasMaxLength(100)
                .HasColumnName("RDistrict");
            entity.Property(e => e.RelationshipToDeceased).HasMaxLength(200);
            entity.Property(e => e.Rfirstname)
                .HasMaxLength(100)
                .HasColumnName("RFirstname");
            entity.Property(e => e.Rlastname)
                .HasMaxLength(100)
                .HasColumnName("RLastname");
            entity.Property(e => e.Rmiddlename)
                .HasMaxLength(100)
                .HasColumnName("RMiddlename");
            entity.Property(e => e.RsubVill)
                .HasMaxLength(100)
                .HasColumnName("RSubVill");
            entity.Property(e => e.Rsuffix)
                .HasMaxLength(100)
                .HasColumnName("RSuffix");
            entity.Property(e => e.Sex).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(100);
            entity.Property(e => e.Status2)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Status3)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.SubVill).HasMaxLength(100);
            entity.Property(e => e.Suffix).HasMaxLength(100);
            entity.Property(e => e.TimeOfDeath).HasMaxLength(200);
            entity.Property(e => e.Typeassistance).HasMaxLength(100);
            entity.Property(e => e.ValidBackimage).HasMaxLength(100);
            entity.Property(e => e.Validfrontimage).HasMaxLength(100);
        });

        modelBuilder.Entity<HospitalAssistance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_FillupformHospitalBill");

            entity.ToTable("HospitalAssistance");

            entity.HasIndex(e => new { e.Status3, e.CreatedAt }, "IX_HospitalAssistance_Status3_CreatedAt");

            entity.HasIndex(e => new { e.Status, e.Status2, e.CreatedAt }, "IX_HospitalAssistance_Status_Status2_CreatedAt");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_HospitalAssistance_UserId_CreatedAt");

            entity.Property(e => e.AdmissionDate).HasMaxLength(200);
            entity.Property(e => e.Age)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.BlkLotStreet)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Brgy)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.ContactNo).HasMaxLength(100);
            entity.Property(e => e.Dateofbirth)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.DeathCertificate)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.DiagnosisMedicalCondition).HasMaxLength(1000);
            entity.Property(e => e.DischargeDate).HasMaxLength(200);
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.DoctorPrescription)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Firstname)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.ForCmopersonnel)
                .HasMaxLength(100)
                .HasColumnName("ForCMOPERSONNEL");
            entity.Property(e => e.HospitalBillCost).HasMaxLength(200);
            entity.Property(e => e.HospitalFacilityAddress).HasMaxLength(500);
            entity.Property(e => e.HospitalFacilityName).HasMaxLength(500);
            entity.Property(e => e.Lastname)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Middlename)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.PhilHealth)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.PhilHealthNo).HasMaxLength(100);
            entity.Property(e => e.Processby)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.RblkLotStreet)
                .HasMaxLength(100)
                .HasColumnName("RBlkLotStreet");
            entity.Property(e => e.Rbrgy)
                .HasMaxLength(100)
                .HasColumnName("RBrgy");
            entity.Property(e => e.Rdistrict)
                .HasMaxLength(100)
                .HasColumnName("RDistrict");
            entity.Property(e => e.RelationshipPatient).HasMaxLength(100);
            entity.Property(e => e.Rfirstname)
                .HasMaxLength(100)
                .HasColumnName("RFirstname");
            entity.Property(e => e.Rlastname)
                .HasMaxLength(100)
                .HasColumnName("RLastname");
            entity.Property(e => e.Rmiddlename)
                .HasMaxLength(100)
                .HasColumnName("RMiddlename");
            entity.Property(e => e.RsubVill)
                .HasMaxLength(100)
                .HasColumnName("RSubVill");
            entity.Property(e => e.Rsuffix)
                .HasMaxLength(100)
                .HasColumnName("RSuffix");
            entity.Property(e => e.Sex)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Status2)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Status3)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.SubVill)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Suffix)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Typeassistance)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.ValidBackimage)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Validfrontimage).HasMaxLength(100);
            entity.Property(e => e.WardRoomType).HasMaxLength(200);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.ApplicationType, e.ApplicationId, e.CreatedAt }, "IX_Notifications_ApplicationType_ApplicationId_CreatedAt");

            entity.HasIndex(e => e.NotificationIdentifier, "IX_Notifications_NotificationIdentifier");

            entity.HasIndex(e => new { e.RecipientType, e.IsRead, e.CreatedAt, e.IsArchived }, "IX_Notifications_RecipientType_IsRead_CreatedAt_IsArchived");

            entity.HasIndex(e => new { e.Type, e.ProcessStage, e.DisplayOrder }, "IX_Notifications_Type_ProcessStage_DisplayOrder");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt, e.IsArchived }, "IX_Notifications_UserId_CreatedAt_IsArchived");

            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt }, "IX_Notifications_UserId_IsRead_CreatedAt");

            entity.Property(e => e.ApplicantName).HasMaxLength(300);
            entity.Property(e => e.ApplicationType).HasMaxLength(50);
            entity.Property(e => e.Link).HasMaxLength(500);
            entity.Property(e => e.NotificationIdentifier).HasMaxLength(255);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.ProcessStage).HasMaxLength(50);
            entity.Property(e => e.ProcessedBy).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Status2).HasMaxLength(50);
            entity.Property(e => e.Status3).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<OtherAssistance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Medicalandlabform");

            entity.ToTable("OtherAssistance");

            entity.HasIndex(e => new { e.Status3, e.CreatedAt }, "IX_OtherAssistance_Status3_CreatedAt");

            entity.HasIndex(e => new { e.Status, e.Status2, e.CreatedAt }, "IX_OtherAssistance_Status_Status2_CreatedAt");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_OtherAssistance_UserId_CreatedAt");

            entity.Property(e => e.Age).HasMaxLength(100);
            entity.Property(e => e.BlkLotStreet).HasMaxLength(100);
            entity.Property(e => e.Brgy).HasMaxLength(100);
            entity.Property(e => e.ContactNo).HasMaxLength(100);
            entity.Property(e => e.Dateofbirth).HasMaxLength(100);
            entity.Property(e => e.DeathCertificate).HasMaxLength(100);
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.DoctorContactDetail).HasMaxLength(200);
            entity.Property(e => e.DoctorPrescription).HasMaxLength(100);
            entity.Property(e => e.EquipmentBrand).HasMaxLength(200);
            entity.Property(e => e.EquipmentCategory).HasMaxLength(200);
            entity.Property(e => e.EquipmentCost).HasMaxLength(200);
            entity.Property(e => e.EquipmentName).HasMaxLength(300);
            entity.Property(e => e.EquipmentQuantity).HasMaxLength(200);
            entity.Property(e => e.Firstname).HasMaxLength(100);
            entity.Property(e => e.ForCmopersonnel)
                .HasMaxLength(100)
                .HasColumnName("ForCMOPERSONNEL");
            entity.Property(e => e.LaboratoryCenterAddress).HasMaxLength(500);
            entity.Property(e => e.LaboratoryCenterName).HasMaxLength(500);
            entity.Property(e => e.Lastname).HasMaxLength(100);
            entity.Property(e => e.MedCertificate)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.MedicineCost).HasMaxLength(200);
            entity.Property(e => e.MedicineName).HasMaxLength(300);
            entity.Property(e => e.MedicineQuantity).HasMaxLength(200);
            entity.Property(e => e.Middlename).HasMaxLength(100);
            entity.Property(e => e.PhilHealth).HasMaxLength(100);
            entity.Property(e => e.PhilHealthNo).HasMaxLength(100);
            entity.Property(e => e.PrescribingDoctor).HasMaxLength(300);
            entity.Property(e => e.Processby).HasMaxLength(100);
            entity.Property(e => e.RblkLotStreet)
                .HasMaxLength(100)
                .HasColumnName("RBlkLotStreet");
            entity.Property(e => e.Rbrgy)
                .HasMaxLength(100)
                .HasColumnName("RBrgy");
            entity.Property(e => e.Rdistrict)
                .HasMaxLength(100)
                .HasColumnName("RDistrict");
            entity.Property(e => e.RelationshipPatient).HasMaxLength(100);
            entity.Property(e => e.Rfirstname)
                .HasMaxLength(100)
                .HasColumnName("RFirstname");
            entity.Property(e => e.Rlastname)
                .HasMaxLength(100)
                .HasColumnName("RLastname");
            entity.Property(e => e.Rmiddlename)
                .HasMaxLength(100)
                .HasColumnName("RMiddlename");
            entity.Property(e => e.RsubVill)
                .HasMaxLength(100)
                .HasColumnName("RSubVill");
            entity.Property(e => e.Rsuffix)
                .HasMaxLength(100)
                .HasColumnName("RSuffix");
            entity.Property(e => e.Sex).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(100);
            entity.Property(e => e.Status2)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Status3)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.SubVill).HasMaxLength(100);
            entity.Property(e => e.Suffix).HasMaxLength(100);
            entity.Property(e => e.TestCost).HasMaxLength(200);
            entity.Property(e => e.TestName).HasMaxLength(300);
            entity.Property(e => e.TestOtherInfo).HasMaxLength(500);
            entity.Property(e => e.TherapyFacilityAddress).HasMaxLength(500);
            entity.Property(e => e.TherapyFacilityContact).HasMaxLength(200);
            entity.Property(e => e.TherapyFacilityName).HasMaxLength(500);
            entity.Property(e => e.TherapyType).HasMaxLength(300);
            entity.Property(e => e.Typeassistance).HasMaxLength(100);
            entity.Property(e => e.ValidBackimage).HasMaxLength(100);
            entity.Property(e => e.Validfrontimage).HasMaxLength(100);
        });

        modelBuilder.Entity<RegistrationAuditLog>(entity =>
        {
            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IpAddress).HasMaxLength(100);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.RegistrationToken).HasMaxLength(500);
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.SuspiciousReasons).HasMaxLength(1000);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<RegistrationToken>(entity =>
        {
            entity.Property(e => e.IpAddress).HasMaxLength(100);
            entity.Property(e => e.Token).HasMaxLength(500);
            entity.Property(e => e.UsedByEmail).HasMaxLength(100);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
        });

        modelBuilder.Entity<Superadminaccount>(entity =>
        {
            entity.ToTable("Superadminaccount");

            entity.HasIndex(e => e.Fullname, "IX_Superadminaccount_Fullname").IsUnique();

            entity.HasIndex(e => e.Username, "IX_Superadminaccount_Username").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Fullname).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        modelBuilder.Entity<VerifiedAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_Verifyaccount");

            entity.ToTable("VerifiedAccount");

            entity.Property(e => e.BackId)
                .HasMaxLength(100)
                .HasColumnName("BackID");
            entity.Property(e => e.Barangay).HasMaxLength(100);
            entity.Property(e => e.BlkLotStreet).HasMaxLength(100);
            entity.Property(e => e.CivilStatus)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.Dateofbirth).HasMaxLength(100);
            entity.Property(e => e.Decision)
                .HasMaxLength(20)
                .HasColumnName("decision");
            entity.Property(e => e.Firstname).HasMaxLength(100);
            entity.Property(e => e.FrontId)
                .HasMaxLength(100)
                .HasColumnName("FrontID");
            entity.Property(e => e.Gender).HasMaxLength(100);
            entity.Property(e => e.Idnumber)
                .HasMaxLength(100)
                .HasColumnName("IDnumber");
            entity.Property(e => e.Idtype)
                .HasMaxLength(100)
                .HasColumnName("IDtype");
            entity.Property(e => e.Lastname).HasMaxLength(100);
            entity.Property(e => e.Middlename).HasMaxLength(100);
            entity.Property(e => e.Phonenumber)
                .HasMaxLength(100)
                .HasDefaultValue("");
            entity.Property(e => e.SubVill).HasMaxLength(100);
            entity.Property(e => e.Suffix).HasMaxLength(100);
            entity.Property(e => e.TransactionId).HasMaxLength(200);
            entity.Property(e => e.Userfacepicture)
                .HasMaxLength(100)
                .HasDefaultValue("")
                .HasColumnName("userfacepicture");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
