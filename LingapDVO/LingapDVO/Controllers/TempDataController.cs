using LingapDVO.Services;
using Microsoft.AspNetCore.Mvc;

namespace LingapDVO.Controllers
{
    /// <summary>
    /// TEMPORARY CONTROLLER - DELETE AFTER USE
    /// Populates database with 200 realistic records
    /// </summary>
    public class TempDataController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TempDataController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Navigate to: /TempData/Populate
        /// RUN ONCE ONLY!
        /// </summary>
        public IActionResult Populate()
        {
            try
            {
                var populator = new TempDataPopulator(_context);
                populator.PopulateData();

                return Content(@"
                    <html>
                    <head>
                        <title>Data Population Complete</title>
                        <style>
                            body {
                                font-family: 'Segoe UI', sans-serif;
                                background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                                min-height: 100vh;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                margin: 0;
                            }
                            .container {
                                background: white;
                                padding: 3rem;
                                border-radius: 20px;
                                box-shadow: 0 20px 60px rgba(0,0,0,0.3);
                                max-width: 700px;
                                text-align: center;
                            }
                            h1 {
                                color: #43a047;
                                font-size: 2.5rem;
                                margin-bottom: 1rem;
                            }
                            .success-icon {
                                font-size: 5rem;
                                color: #43a047;
                                margin-bottom: 1rem;
                            }
                            .stats {
                                background: #f5f7fa;
                                padding: 2rem;
                                border-radius: 12px;
                                margin: 2rem 0;
                                text-align: left;
                            }
                            .stat-item {
                                padding: 0.75rem 0;
                                border-bottom: 1px solid #e0e0e0;
                                display: flex;
                                justify-content: space-between;
                            }
                            .stat-item:last-child {
                                border-bottom: none;
                            }
                            .warning {
                                background: #fff3cd;
                                border-left: 4px solid #ffc107;
                                padding: 1.5rem;
                                border-radius: 8px;
                                margin-top: 2rem;
                            }
                            .warning h3 {
                                color: #856404;
                                margin-top: 0;
                            }
                            .code {
                                background: #f8f9fa;
                                padding: 0.5rem 1rem;
                                border-radius: 4px;
                                font-family: 'Courier New', monospace;
                                font-size: 0.9rem;
                                margin: 1rem 0;
                            }
                            .btn {
                                display: inline-block;
                                padding: 12px 30px;
                                background: #667eea;
                                color: white;
                                text-decoration: none;
                                border-radius: 8px;
                                margin: 1rem 0.5rem;
                                font-weight: 600;
                            }
                            .btn:hover {
                                background: #5568d3;
                            }
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='success-icon'>✓</div>
                            <h1>Success!</h1>
                            <p style='font-size: 1.2rem; color: #666;'>200 realistic dummy records have been created!</p>

                            <div class='stats'>
                                <div class='stat-item'>
                                    <strong>👥 Users Created:</strong>
                                    <span>50 verified accounts</span>
                                </div>
                                <div class='stat-item'>
                                    <strong>🏥 Hospital Applications:</strong>
                                    <span>80 records</span>
                                </div>
                                <div class='stat-item'>
                                    <strong>💊 Medical Applications:</strong>
                                    <span>70 records</span>
                                </div>
                                <div class='stat-item'>
                                    <strong>⚰️ Funeral Applications:</strong>
                                    <span>50 records</span>
                                </div>
                                <div class='stat-item'>
                                    <strong>📊 Total Applications:</strong>
                                    <span>200 records</span>
                                </div>
                                <div class='stat-item'>
                                    <strong>💬 Feedback Entries:</strong>
                                    <span>80 responses</span>
                                </div>
                            </div>

                            <div style='margin: 2rem 0;'>
                                <a href='/Admin' class='btn'>View Application Dashboard</a>
                                <a href='/Adminuser/Analyticsdashboard' class='btn'>View Analytics</a>
                            </div>

                            <div class='warning'>
                                <h3>⚠️ IMPORTANT - DELETE TEMPORARY FILES NOW!</h3>
                                <p>The data has been created successfully. Now you must delete these files:</p>
                                <div class='code'>
                                    Services/TempDataPopulator.cs<br/>
                                    Controllers/TempDataController.cs
                                </div>
                                <p style='margin-top: 1rem;'><strong>Steps:</strong></p>
                                <ol style='text-align: left; margin-left: 2rem;'>
                                    <li>Close this browser tab</li>
                                    <li>Stop your application (Ctrl+C in terminal)</li>
                                    <li>Delete both files listed above</li>
                                    <li>Restart your application</li>
                                    <li>Data will remain in database permanently</li>
                                </ol>
                            </div>

                            <div style='margin-top: 2rem; padding: 1rem; background: #d1ecf1; border-radius: 8px; border-left: 4px solid #0c5460;'>
                                <strong>ℹ️ What Was Created:</strong>
                                <ul style='text-align: left; margin-top: 0.5rem;'>
                                    <li>✅ 50 real user accounts with hashed passwords</li>
                                    <li>✅ All security tokens and audit logs</li>
                                    <li>✅ 200 applications across all types</li>
                                    <li>✅ Realistic status distribution</li>
                                    <li>✅ 80 feedback responses</li>
                                    <li>✅ Data visible in ALL admin pages</li>
                                    <li>✅ Applications spread across 6 months</li>
                                    <li>✅ Priority applications included</li>
                                </ul>
                            </div>
                        </div>
                    </body>
                    </html>
                ", "text/html");
            }
            catch (Exception ex)
            {
                return Content($@"
                    <html>
                    <head>
                        <title>Error</title>
                        <style>
                            body {{
                                font-family: 'Segoe UI', sans-serif;
                                background: linear-gradient(135deg, #e53935 0%, #c62828 100%);
                                min-height: 100vh;
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                margin: 0;
                            }}
                            .container {{
                                background: white;
                                padding: 3rem;
                                border-radius: 20px;
                                box-shadow: 0 20px 60px rgba(0,0,0,0.3);
                                max-width: 700px;
                            }}
                            h1 {{ color: #e53935; }}
                            .error {{ background: #ffebee; padding: 1rem; border-radius: 8px; margin: 1rem 0; font-family: monospace; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h1>❌ Error</h1>
                            <p>An error occurred while populating data:</p>
                            <div class='error'>
                                <strong>Error:</strong> {ex.Message}<br/>
                                <strong>Stack:</strong> {ex.StackTrace}
                            </div>
                            <p><strong>Common fixes:</strong></p>
                            <ul>
                                <li>Make sure BCrypt.Net-Next package is installed</li>
                                <li>Ensure database connection is working</li>
                                <li>Check that migrations are up to date</li>
                            </ul>
                        </div>
                    </body>
                    </html>
                ", "text/html");
            }
        }
    }
}
