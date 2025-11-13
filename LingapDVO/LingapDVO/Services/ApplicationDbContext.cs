using LingapDVO.Models;
using Microsoft.EntityFrameworkCore;

namespace LingapDVO.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Register> Register { get; set; }

        public DbSet<HospitalAssistance> HospitalAssistance { get; set; }

        public DbSet<OtherAssistance> OtherAssistance { get; set; }

        public DbSet<FuneralAssistance> FuneralAssistance { get; set; }

        public DbSet<Adminaccount> Adminaccount { get; set; }

        public DbSet<Superadminaccount> Superadminaccount { get; set; }

        public DbSet<Useraccount> Useraccount { get; set; }

        public DbSet<RegisterAcc> RegisterAcc { get; set; }

        public DbSet<Verifyaccount> Verifyaccount { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // 🔒 ANTI-MANIPULATION SECURITY TABLES
        // ═══════════════════════════════════════════════════════════════
        public DbSet<RegistrationToken> RegistrationTokens { get; set; }
        public DbSet<RegistrationAuditLog> RegistrationAuditLogs { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // 🔒 FORM SUBMISSION ANTI-DUPLICATION SECURITY
        // ═══════════════════════════════════════════════════════════════
        public DbSet<FormSubmissionToken> FormSubmissionTokens { get; set; }
        public DbSet<FormSubmissionAuditLog> FormSubmissionAuditLogs { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // 📝 CITIZEN FEEDBACK
        // ═══════════════════════════════════════════════════════════════
        public DbSet<Feedback> Feedbacks { get; set; }
    }
}
