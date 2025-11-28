using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using LingapDVO.Scripts;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using LingapDVO.Services;
using System.Threading.Tasks;

namespace LingapDVO.Controllers
{
    [ApiController]
    [Route("api/admin/dummydata")]
    public class DummyDataController : ControllerBase
    {
        private readonly ILogger<DummyDataController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public DummyDataController(ILogger<DummyDataController> logger, IWebHostEnvironment env, ApplicationDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _env = env;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("generate")]
        [HttpGet("generate")]
        public async Task<IActionResult> Generate()
        {
            if (!_env.IsDevelopment())
            {
                _logger.LogWarning("Attempt to access generate endpoint outside Development environment.");
                return StatusCode(403, new { success = false, message = "Endpoint allowed only in Development environment." });
            }

            try
            {
                _logger.LogInformation("Starting dummy data generation via API (Development only)...");
                var generator = new LingapDVO.Scripts.DummyDataGenerator(_context, _configuration);
                await generator.GenerateAllDummyDataAsync();

                // After generation, ensure no records remain Pending/Processing; convert them to completed statuses
                var rand = new Random();
                int updated = 0;

                var hospPending = await _context.HospitalAssistance.Where(h => h.Status == "Pending" || h.Status == "Processing").ToListAsync();
                var othPending = await _context.OtherAssistance.Where(o => o.Status == "Pending" || o.Status == "Processing").ToListAsync();
                var funPending = await _context.FuneralAssistance.Where(f => f.Status == "Pending" || f.Status == "Processing").ToListAsync();

                void ApplyRandomCompletion(dynamic item)
                {
                    int pick = rand.Next(100);
                    if (pick < 85)
                    {
                        // Approve (85%); within approved, ~59% will be claimed to get ~50% overall claimed
                        item.Status2 = "Approve";
                        item.Status3 = rand.Next(100) < 59 ? "Claimed" : "";
                        item.Status = item.Status3 == "Claimed" ? "Claimed" : "Completed";
                    }
                    else
                    {
                        // Disapprove (15%)
                        item.Status2 = "Disapprove";
                        item.Status3 = "";
                        item.Status = "Completed";
                    }
                }

                foreach (var r in hospPending) { ApplyRandomCompletion(r); updated++; }
                foreach (var r in othPending) { ApplyRandomCompletion(r); updated++; }
                foreach (var r in funPending) { ApplyRandomCompletion(r); updated++; }

                if (updated > 0)
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Dummy data generation completed");
                return Ok(new { success = true, message = "Dummy data generation completed (Development).", fixedCount = updated });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error generating dummy data");
                return StatusCode(500, new { success = false, message = "Error generating dummy data", error = ex.Message });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> Statistics()
        {
            var stats = new
            {
                totalUsers = await _context.RegisterAcc.CountAsync(),
                verifiedAccounts = await _context.Verifyaccount.CountAsync(),
                hospitalApplications = await _context.HospitalAssistance.CountAsync(),
                otherApplications = await _context.OtherAssistance.CountAsync(),
                funeralApplications = await _context.FuneralAssistance.CountAsync()
            };

            return Ok(new { success = true, statistics = stats });
        }

        [HttpPost("clear")]
        [HttpGet("clear")]
        public async Task<IActionResult> Clear(string confirmPassword = "")
        {
            if (!_env.IsDevelopment())
            {
                _logger.LogWarning("Attempt to clear data outside Development environment.");
                return StatusCode(403, new { success = false, message = "Endpoint allowed only in Development environment." });
            }

            if (confirmPassword != "DELETE_ALL_DUMMY_DATA_2025")
            {
                return BadRequest(new { success = false, message = "Invalid confirmation password. Provide confirmPassword=DELETE_ALL_DUMMY_DATA_2025" });
            }

            try
            {
                _logger.LogWarning("Clearing dummy data (Development only)...");
                _context.Notifications.RemoveRange(_context.Notifications);
                _context.HospitalAssistance.RemoveRange(_context.HospitalAssistance);
                _context.OtherAssistance.RemoveRange(_context.OtherAssistance);
                _context.FuneralAssistance.RemoveRange(_context.FuneralAssistance);
                _context.Verifyaccount.RemoveRange(_context.Verifyaccount);
                _context.RegisterAcc.RemoveRange(_context.RegisterAcc);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Cleared dummy data (Development)." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error clearing dummy data");
                return StatusCode(500, new { success = false, message = "Error clearing data", error = ex.Message });
            }
        }
    }
}
