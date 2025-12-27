using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.TempModels;

public partial class LingapDvoDbContext : DbContext
{
    public LingapDvoDbContext()
    {
    }

    public LingapDvoDbContext(DbContextOptions<LingapDvoDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<HospitalAssistance> HospitalAssistances { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
