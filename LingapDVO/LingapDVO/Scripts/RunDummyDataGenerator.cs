using LingapDVO.Models;
using LingapDVO.Scripts;
using LingapDVO.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LingapDVO.Scripts
{

    /// <summary>
    /// Console application to run the dummy data generator
    /// This will populate the database with 250 realistic Filipino users
    /// with complete workflows including applications and notifications
    /// </summary>
    public class RunDummyDataGenerator
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("  LINGAPDVO DUMMY DATA GENERATOR");
            Console.WriteLine("  Generating 250 Realistic Filipino User Data");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine();

            try
            {
                // Build configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                // Get connection string
                string? connectionString = configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ ERROR: Connection string not found in appsettings.json");
                    Console.ResetColor();
                    return;
                }

                // Setup DbContext
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer(connectionString);

                using (var context = new ApplicationDbContext(optionsBuilder.Options))
                {
                    // Test connection
                    Console.WriteLine("⏳ Testing database connection...");
                    bool canConnect = await context.Database.CanConnectAsync();

                    if (!canConnect)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ ERROR: Cannot connect to database. Please check your connection string.");
                        Console.ResetColor();
                        return;
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✓ Database connection successful!");
                    Console.ResetColor();
                    Console.WriteLine();

                    // Confirm before proceeding
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠ WARNING: This will add 250 users with multiple applications to your database.");
                    Console.WriteLine("⚠ Estimated records to be created:");
                    Console.WriteLine("   - 250 User Accounts (RegisterAcc)");
                    Console.WriteLine("   - 250 Verified Accounts (Verifyaccount)");
                    Console.WriteLine("   - ~375-500 Applications (Hospital, Other, Funeral)");
                    Console.WriteLine("   - ~1500-2000 Notifications");
                    Console.WriteLine();
                    Console.ResetColor();

                    Console.Write("Do you want to continue? (yes/no): ");
                    string? response = Console.ReadLine();

                    if (response?.ToLower() != "yes")
                    {
                        Console.WriteLine();
                        Console.WriteLine("Operation cancelled by user.");
                        return;
                    }

                    Console.WriteLine();
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine("  STARTING DATA GENERATION");
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine();

                    var startTime = DateTime.Now;

                    // Create generator and run
                    var generator = new DummyDataGenerator(context, configuration);
                    await generator.GenerateAllDummyDataAsync();

                    var endTime = DateTime.Now;
                    var duration = endTime - startTime;

                    Console.WriteLine();
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  ✓ GENERATION COMPLETED SUCCESSFULLY!");
                    Console.ResetColor();
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    Console.WriteLine();
                    Console.WriteLine($"Time taken: {duration.TotalMinutes:F2} minutes");
                    Console.WriteLine();

                    // Display summary statistics
                    await DisplaySummaryStatisticsAsync(context);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("  ❌ ERROR OCCURRED");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine();
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Stack Trace:");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task DisplaySummaryStatisticsAsync(ApplicationDbContext context)
        {
            Console.WriteLine("📊 DATABASE SUMMARY STATISTICS:");
            Console.WriteLine("────────────────────────────────────────────────────────────");

            int totalUsers = await context.RegisterAcc.CountAsync();
            int totalVerified = await context.Verifyaccount.CountAsync();
            int totalHospital = await context.HospitalAssistance.CountAsync();
            int totalOther = await context.OtherAssistance.CountAsync();
            int totalFuneral = await context.FuneralAssistance.CountAsync();
            int totalNotifications = await context.Notifications.CountAsync();

            Console.WriteLine($"  Total Users:                 {totalUsers}");
            Console.WriteLine($"  Verified Accounts:           {totalVerified}");
            Console.WriteLine($"  Hospital Applications:       {totalHospital}");
            Console.WriteLine($"  Other Applications:          {totalOther}");
            Console.WriteLine($"  Funeral Applications:        {totalFuneral}");
            Console.WriteLine($"  Total Applications:          {totalHospital + totalOther + totalFuneral}");
            Console.WriteLine($"  Total Notifications:         {totalNotifications}");
            Console.WriteLine();

            // Status breakdown for Hospital Assistance
            var hospitalApproved = await context.HospitalAssistance.CountAsync(h => h.Status2 == "Approve");
            var hospitalDisapproved = await context.HospitalAssistance.CountAsync(h => h.Status2 == "Disapprove");
            var hospitalRetake = await context.HospitalAssistance.CountAsync(h => h.Status2 == "Retake");
            var hospitalClaimed = await context.HospitalAssistance.CountAsync(h => h.Status3 == "Claimed");

            Console.WriteLine("  HOSPITAL ASSISTANCE BREAKDOWN:");
            Console.WriteLine($"    ✓ Approved:                {hospitalApproved}");
            Console.WriteLine($"    ✗ Disapproved:             {hospitalDisapproved}");
            Console.WriteLine($"    ⟳ Retake:                  {hospitalRetake}");
            Console.WriteLine($"    ✔ Claimed:                 {hospitalClaimed}");
            Console.WriteLine();

            // Status breakdown for Other Assistance
            var otherApproved = await context.OtherAssistance.CountAsync(o => o.Status2 == "Approve");
            var otherDisapproved = await context.OtherAssistance.CountAsync(o => o.Status2 == "Disapprove");
            var otherRetake = await context.OtherAssistance.CountAsync(o => o.Status2 == "Retake");
            var otherClaimed = await context.OtherAssistance.CountAsync(o => o.Status3 == "Claimed");

            Console.WriteLine("  OTHER ASSISTANCE BREAKDOWN:");
            Console.WriteLine($"    ✓ Approved:                {otherApproved}");
            Console.WriteLine($"    ✗ Disapproved:             {otherDisapproved}");
            Console.WriteLine($"    ⟳ Retake:                  {otherRetake}");
            Console.WriteLine($"    ✔ Claimed:                 {otherClaimed}");
            Console.WriteLine();

            // Status breakdown for Funeral Assistance
            var funeralApproved = await context.FuneralAssistance.CountAsync(f => f.Status2 == "Approve");
            var funeralDisapproved = await context.FuneralAssistance.CountAsync(f => f.Status2 == "Disapprove");
            var funeralRetake = await context.FuneralAssistance.CountAsync(f => f.Status2 == "Retake");
            var funeralClaimed = await context.FuneralAssistance.CountAsync(f => f.Status3 == "Claimed");

            Console.WriteLine("  FUNERAL ASSISTANCE BREAKDOWN:");
            Console.WriteLine($"    ✓ Approved:                {funeralApproved}");
            Console.WriteLine($"    ✗ Disapproved:             {funeralDisapproved}");
            Console.WriteLine($"    ⟳ Retake:                  {funeralRetake}");
            Console.WriteLine($"    ✔ Claimed:                 {funeralClaimed}");
            Console.WriteLine();

            // Notification statistics
            var notifSubmitted = await context.Notifications.CountAsync(n => n.Type == "application_submitted");
            var notifProcessing = await context.Notifications.CountAsync(n => n.Type == "application_processing");
            var notifApproved = await context.Notifications.CountAsync(n => n.Type == "application_approved");
            var notifDisapproved = await context.Notifications.CountAsync(n => n.Type == "application_disapproved");
            var notifRetake = await context.Notifications.CountAsync(n => n.Type == "application_retake");
            var notifClaimed = await context.Notifications.CountAsync(n => n.Type == "application_claimed");

            Console.WriteLine("  NOTIFICATION TYPE BREAKDOWN:");
            Console.WriteLine($"    Submitted:                 {notifSubmitted}");
            Console.WriteLine($"    Processing:                {notifProcessing}");
            Console.WriteLine($"    Approved:                  {notifApproved}");
            Console.WriteLine($"    Disapproved:               {notifDisapproved}");
            Console.WriteLine($"    Retake:                    {notifRetake}");
            Console.WriteLine($"    Claimed:                   {notifClaimed}");
            Console.WriteLine();

            Console.WriteLine("────────────────────────────────────────────────────────────");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  ℹ All documents and IDs are encrypted with AES-256");
            Console.WriteLine("  ℹ Data follows complete workflow: Submit → Process → Result → Claim");
            Console.WriteLine("  ℹ Includes realistic Filipino names, addresses, and phone numbers");
            Console.ResetColor();
            Console.WriteLine("────────────────────────────────────────────────────────────");
        }
    }
}
