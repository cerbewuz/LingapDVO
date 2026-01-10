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

        // public DbSet<Useraccount> Useraccount { get; set; } // Legacy model - commented out

        public DbSet<UserAccount> UserAccount { get; set; }

        public DbSet<VerifiedAccount> VerifiedAccount { get; set; }

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

        // ═══════════════════════════════════════════════════════════════
        // 🔔 UNIFIED NOTIFICATIONS SYSTEM
        // Comprehensive notification tracking with built-in read status
        // Synchronized across Homepage and Application Tracking pages
        // RecipientType: 1 = User notifications, 2 = Admin notifications
        // ═══════════════════════════════════════════════════════════════
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ═══════════════════════════════════════════════════════════════
            // 🔔 UNIFIED NOTIFICATIONS INDEXES
            // ═══════════════════════════════════════════════════════════════

            // Primary query: Get user notifications ordered by date
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.CreatedAt, n.IsArchived })
                .HasDatabaseName("IX_Notifications_UserId_CreatedAt_IsArchived");

            // Query admin notifications (RecipientType = 2)
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.RecipientType, n.IsRead, n.CreatedAt, n.IsArchived })
                .HasDatabaseName("IX_Notifications_RecipientType_IsRead_CreatedAt_IsArchived");

            // Query unread notifications
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
                .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");

            // Query by application
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.ApplicationType, n.ApplicationId, n.CreatedAt })
                .HasDatabaseName("IX_Notifications_ApplicationType_ApplicationId_CreatedAt");

            // Query by notification identifier (for deduplication)
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.NotificationIdentifier)
                .HasDatabaseName("IX_Notifications_NotificationIdentifier");

            // Query by type and stage (for timeline rendering)
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.Type, n.ProcessStage, n.DisplayOrder })
                .HasDatabaseName("IX_Notifications_Type_ProcessStage_DisplayOrder");

            // ═══════════════════════════════════════════════════════════════
            // 🚀 PERFORMANCE INDEXES - Optimize frequently queried columns
            // ═══════════════════════════════════════════════════════════════

            // HospitalAssistance indexes
            modelBuilder.Entity<HospitalAssistance>()
                .HasIndex(h => new { h.UserId, h.CreatedAt })
                .HasDatabaseName("IX_HospitalAssistance_UserId_CreatedAt");

            modelBuilder.Entity<HospitalAssistance>()
                .HasIndex(h => new { h.Status, h.Status2, h.CreatedAt })
                .HasDatabaseName("IX_HospitalAssistance_Status_Status2_CreatedAt");

            modelBuilder.Entity<HospitalAssistance>()
                .HasIndex(h => new { h.Status3, h.CreatedAt })
                .HasDatabaseName("IX_HospitalAssistance_Status3_CreatedAt");

            // OtherAssistance indexes
            modelBuilder.Entity<OtherAssistance>()
                .HasIndex(o => new { o.UserId, o.CreatedAt })
                .HasDatabaseName("IX_OtherAssistance_UserId_CreatedAt");

            modelBuilder.Entity<OtherAssistance>()
                .HasIndex(o => new { o.Status, o.Status2, o.CreatedAt })
                .HasDatabaseName("IX_OtherAssistance_Status_Status2_CreatedAt");

            modelBuilder.Entity<OtherAssistance>()
                .HasIndex(o => new { o.Status3, o.CreatedAt })
                .HasDatabaseName("IX_OtherAssistance_Status3_CreatedAt");

            // FuneralAssistance indexes
            modelBuilder.Entity<FuneralAssistance>()
                .HasIndex(f => new { f.UserId, f.CreatedAt })
                .HasDatabaseName("IX_FuneralAssistance_UserId_CreatedAt");

            modelBuilder.Entity<FuneralAssistance>()
                .HasIndex(f => new { f.Status, f.Status2, f.CreatedAt })
                .HasDatabaseName("IX_FuneralAssistance_Status_Status2_CreatedAt");

            modelBuilder.Entity<FuneralAssistance>()
                .HasIndex(f => new { f.Status3, f.CreatedAt })
                .HasDatabaseName("IX_FuneralAssistance_Status3_CreatedAt");

            // FormSubmissionToken index for faster token lookups
            modelBuilder.Entity<FormSubmissionToken>()
                .HasIndex(t => new { t.UserId, t.FormType, t.CreatedAt })
                .HasDatabaseName("IX_FormSubmissionToken_UserId_FormType_CreatedAt");

            // FormSubmissionAuditLog index for audit queries
            modelBuilder.Entity<FormSubmissionAuditLog>()
                .HasIndex(a => new { a.UserId, a.AttemptedAt })
                .HasDatabaseName("IX_FormSubmissionAuditLog_UserId_AttemptedAt");
        }
    }
}
