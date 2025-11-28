using LingapDVO.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ?? Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})

.AddCookie()

.AddGoogle(options =>
{
    options.ClientId = "233826016495-mdmj8b8v2314khtbb1tp4h2bu46abljh.apps.googleusercontent.com";
    options.ClientSecret = "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";

    // ? Keep default callback path (ASP.NET handles this automatically)
    // DO NOT set options.CallbackPath - let it use default: /signin-google

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.SaveTokens = true; // Important: save tokens for later use

    options.Scope.Add("email");
    options.Scope.Add("profile");

    // ? Redirect to your custom action AFTER successful authentication
    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
    {
        OnTicketReceived = context =>
        {
            // After Google auth succeeds, redirect to your custom handler
            context.ReturnUri = "/Auth/GoogleCallback";
            return Task.CompletedTask;
        },

        OnRedirectToAuthorizationEndpoint = context =>
        {
            var redirectUri = context.RedirectUri;
            if (!redirectUri.Contains("prompt="))
            {
                redirectUri += (redirectUri.Contains("?") ? "&" : "?") + "prompt=consent";
            }
            context.Response.Redirect(redirectUri);
            return Task.CompletedTask;
        }
    };
});

// Form Submission Security Service
builder.Services.AddScoped<FormSubmissionSecurityService>();

// Session Configuration Service
builder.Services.AddSingleton<ISessionConfigurationService, SessionConfigurationService>();

// DateTime Service - Philippine Timezone (Asia/Manila, UTC+8)
builder.Services.AddSingleton<IDateTimeService, DateTimeService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

// ⚡ PERFORMANCE: Memory caching for frequently accessed data
builder.Services.AddMemoryCache();

// ⚡ PERFORMANCE: Response compression (gzip/brotli) to reduce bandwidth
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();

    // In Development: exclude HTML to allow Browser Link script injection
    if (builder.Environment.IsDevelopment())
    {
        options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes
            .Where(mime => !mime.Equals("text/html", StringComparison.OrdinalIgnoreCase));
    }
});

// MVC
builder.Services.AddControllersWithViews();

// ?? Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// ?? Session - Configurable inactivity timeout with warning
// Session timeout is centrally configured in appsettings.json under Security:Session
builder.Services.AddSession(options =>
{
    // Read timeout from configuration (defaults to 12 minutes if not set)
    var sessionTimeout = builder.Configuration.GetValue<int>("Security:Session:IdleTimeoutMinutes", 12);
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeout);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = builder.Configuration.GetValue<string>("Security:Session:CookieName", ".LingapDVO.Session");

    // ? Ensure cookies work for external redirect
    options.Cookie.SameSite = SameSiteMode.Lax; // Or None if using HTTPS everywhere
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Use Always in production
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ISmsService, SmsService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMultiChannelNotificationService, MultiChannelNotificationService>();
builder.Services.AddScoped<IAdminNotificationService, AdminNotificationService>();

// Priority Tracking Service
builder.Services.AddScoped<PriorityTrackingService>();

// Priority Background Service (periodic checks)
builder.Services.AddHostedService<PriorityBackgroundService>();

// SignalR for real-time notifications
builder.Services.AddSignalR();

var app = builder.Build();

// ?? Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ⚡ PERFORMANCE: Enable response compression middleware
app.UseResponseCompression();

// 🔒 SECURITY: Add security headers
app.Use(async (context, next) =>
{
    // Content Security Policy
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://code.jquery.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://cdn.tailwindcss.com https://www.google.com https://www.gstatic.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://cdn.tailwindcss.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net data:; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' https://www.facebook.com https://www.google.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://api.ocr.space; " +
        "frame-src 'self' https://www.google.com https://www.gstatic.com; " +
        "frame-ancestors 'self'; " +
        "base-uri 'self'; " +
        "form-action 'self'");

    // Prevent clickjacking
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");

    // Prevent MIME type sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // Enable XSS filter
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

    // Referrer policy
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Permissions policy
    context.Response.Headers.Append("Permissions-Policy",
        "geolocation=(), microphone=(), camera=(), payment=()");

    await next();
});

app.UseHttpsRedirection();

// ⚡ PERFORMANCE: Configure static files with caching
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static assets for 1 year (for versioned files like CSS, JS, images)
        const int durationInSeconds = 60 * 60 * 24 * 365; // 1 year
        ctx.Context.Response.Headers.Append("Cache-Control", $"public,max-age={durationInSeconds}");
    }
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ===== CLEAN URL ROUTES (Controller names hidden) =====

// Login Controller Routes
app.MapControllerRoute(
    name: "route-login",
    pattern: "Login",
    defaults: new { controller = "Login", action = "Login" });

app.MapControllerRoute(
    name: "route-register",
    pattern: "Register",
    defaults: new { controller = "Login", action = "Register" });

app.MapControllerRoute(
    name: "route-accountverification",
    pattern: "Verifyaccount",
    defaults: new { controller = "Login", action = "Accountverification" });

app.MapControllerRoute(
    name: "route-registeredit",
    pattern: "Registeredit",
    defaults: new { controller = "Login", action = "Registeredit" });

app.MapControllerRoute(
    name: "route-verify-otp",
    pattern: "VerifyOTP",
    defaults: new { controller = "Login", action = "VerifyOTP" });

app.MapControllerRoute(
    name: "route-google-login",
    pattern: "GoogleLogin",
    defaults: new { controller = "Login", action = "GoogleLogin" });

app.MapControllerRoute(
    name: "route-logout",
    pattern: "Logout",
    defaults: new { controller = "Login", action = "Logout" });

// Dashboard Controller Routes
app.MapControllerRoute(
    name: "route-landingpage",
    pattern: "",
    defaults: new { controller = "Dashboard", action = "Landingpage" });

app.MapControllerRoute(
    name: "route-homepage",
    pattern: "Home",
    defaults: new { controller = "Dashboard", action = "Homepage" });

app.MapControllerRoute(
    name: "route-userprofile",
    pattern: "Userprofile",
    defaults: new { controller = "Dashboard", action = "Userprofile" });

app.MapControllerRoute(
    name: "route-applicationtracking",
    pattern: "Applicationtracking",
    defaults: new { controller = "Dashboard", action = "Applicationtracking" });

app.MapControllerRoute(
    name: "route-nearby-offices",
    pattern: "Nearbyoffices",
    defaults: new { controller = "Dashboard", action = "Nearbyoffices" });

app.MapControllerRoute(
    name: "route-listofpartners",
    pattern: "Listofpartners",
    defaults: new { controller = "Dashboard", action = "Listofpartners" });

app.MapControllerRoute(
    name: "route-history",
    pattern: "History",
    defaults: new { controller = "Dashboard", action = "History" });

// Dashboard Forms Routes
app.MapControllerRoute(
    name: "route-hospitalbill",
    pattern: "HospitalAssistance",
    defaults: new { controller = "Dashboard", action = "HospitalAssistance" });

app.MapControllerRoute(
    name: "route-hospitalbill-edit",
    pattern: "HospitalAssistanceEdit/{id?}",
    defaults: new { controller = "Dashboard", action = "HospitalAssistanceedit" });

app.MapControllerRoute(
    name: "route-hospitalbill-delete",
    pattern: "HospitalAssistancedelete/{id?}",
    defaults: new { controller = "Dashboard", action = "HospitalAssistancedelete" });

app.MapControllerRoute(
    name: "route-hospitalbill-view",
    pattern: "HospitalAssistanceView/{id?}",
    defaults: new { controller = "Dashboard", action = "HospitalAssistanceView" });

app.MapControllerRoute(
    name: "route-medicallab",
    pattern: "OtherAssistance",
    defaults: new { controller = "Dashboard", action = "OtherAssistance" });

app.MapControllerRoute(
    name: "route-medicallab-edit",
    pattern: "OtherAssistanceEdit/{id?}",
    defaults: new { controller = "Dashboard", action = "OtherAssistanceedit" });

app.MapControllerRoute(
    name: "route-medicallab-delete",
    pattern: "OtherAssistanceedelete/{id?}",
    defaults: new { controller = "Dashboard", action = "OtherAssistanceedelete" });

app.MapControllerRoute(
    name: "route-medicallab-view",
    pattern: "OtherAssistanceView/{id?}",
    defaults: new { controller = "Dashboard", action = "OtherAssistanceview" });

app.MapControllerRoute(
    name: "route-FuneralAssistance",
    pattern: "FuneralAssistance",
    defaults: new { controller = "Dashboard", action = "FuneralAssistance" });

app.MapControllerRoute(
    name: "route-FuneralAssistance-edit",
    pattern: "FuneralAssistanceEdit/{id?}",
    defaults: new { controller = "Dashboard", action = "FuneralAssistanceedit" });

app.MapControllerRoute(
    name: "route-FuneralAssistance-delete",
    pattern: "FuneralAssistanceedelete/{id?}",
    defaults: new { controller = "Dashboard", action = "FuneralAssistanceedelete" });

app.MapControllerRoute(
    name: "route-FuneralAssistance-view",
    pattern: "FuneralAssistanceView/{id?}",
    defaults: new { controller = "Dashboard", action = "FuneralAssistanceview" });

app.MapControllerRoute(
    name: "feedback-form-view",
    pattern: "Feedback",
    defaults: new { controller = "Dashboard", action = "Feedback" });

// Adminuser Controller Routes
app.MapControllerRoute(
    name: "route-admin",
    pattern: "Admin",
    defaults: new { controller = "Adminuser", action = "Admin" });

app.MapControllerRoute(
    name: "route-analyticsdashboard",
    pattern: "Analyticsdashboard",
    defaults: new { controller = "Adminuser", action = "Analyticsdashboard" });

app.MapControllerRoute(
    name: "route-feedbackdashboard",
    pattern: "Feedbacksreport",
    defaults: new { controller = "Adminuser", action = "Feedbackanalytics" });

app.MapControllerRoute(
    name: "route-priorities",
    pattern: "Priorities",
    defaults: new { controller = "Adminuser", action = "Priorities" });

// Admin Hospital Status Routes
app.MapControllerRoute(
    name: "route-admin-hospitalbill-pending",
    pattern: "HospitalAssistancePendingStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "HospitalAssistancePendingStatus" });

app.MapControllerRoute(
    name: "route-admin-hospitalbill-processing",
    pattern: "HospitalAssistanceProcessingStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "HospitalAssistanceProcessingStatus" });

app.MapControllerRoute(
    name: "route-admin-hospitalbill-approved",
    pattern: "HospitalAssistanceApproveStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "HospitalAssistanceApproveStatus" });

app.MapControllerRoute(
    name: "route-admin-hospitalbill-disapproved",
    pattern: "HospitalAssistanceDisapproveStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "HospitalAssistanceDisapproveStatus" });

app.MapControllerRoute(
    name: "route-admin-hospitalbill-claimed",
    pattern: "HospitalAssistanceClaimStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "HospitalAssistanceClaimStatus" });

// Admin Medical Lab Status Routes
app.MapControllerRoute(
    name: "route-admin-medicallab-pending",
    pattern: "OtherAssistancePendingStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "OtherAssistancePendingStatus" });

app.MapControllerRoute(
    name: "route-admin-medicallab-processing",
    pattern: "OtherAssistanceProcessingStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "OtherAssistanceProcessingStatus" });

app.MapControllerRoute(
    name: "route-admin-medicallab-approved",
    pattern: "OtherAssistanceApproveStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "OtherAssistanceApproveStatus" });

app.MapControllerRoute(
    name: "route-admin-medicallab-disapproved",
    pattern: "OtherAssistanceDisapproveStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "OtherAssistanceDisapproveStatus" });

app.MapControllerRoute(
    name: "route-admin-medicallab-claimed",
    pattern: "OtherAssistanceClaimStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "OtherAssistanceClaimStatus" });

// Admin Funeral Burial Status Routes
app.MapControllerRoute(
    name: "route-admin-FuneralAssistance-pending",
    pattern: "FuneralAssistancePendingStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FuneralAssistancePendingStatus" });

app.MapControllerRoute(
    name: "route-admin-FuneralAssistance-processing",
    pattern: "FuneralAssistanceProcessingStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FuneralAssistanceProcessingStatus" });

app.MapControllerRoute(
    name: "route-admin-FuneralAssistance-approved",
    pattern: "FuneralAssistanceApproveStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FuneralAssistanceApproveStatus" });

app.MapControllerRoute(
    name: "route-admin-FuneralAssistance-disapproved",
    pattern: "FuneralAssistanceDisapproveStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FuneralAssistanceDisapproveStatus" });

app.MapControllerRoute(
    name: "route-admin-FuneralAssistance-claimed",
    pattern: "FuneralAssistanceClaimStatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FuneralAssistanceClaimStatus" });

// Superadmin Controller Routes
app.MapControllerRoute(
    name: "route-superadmin",
    pattern: "Superadmin",
    defaults: new { controller = "Superadmin", action = "Superadmin" });

app.MapControllerRoute(
    name: "route-superadmin-choice",
    pattern: "Choice",
    defaults: new { controller = "Superadmin", action = "Choice" });

app.MapControllerRoute(
    name: "route-superadmin-changepass",
    pattern: "Superadminchangepass",
    defaults: new { controller = "Superadmin", action = "Superadminchangepass" });

app.MapControllerRoute(
    name: "route-superadmin-createaccount",
    pattern: "Admincreateaccount",
    defaults: new { controller = "Superadmin", action = "Admincreateaccount" });

app.MapControllerRoute(
    name: "route-superadmin-users",
    pattern: "Users",
    defaults: new { controller = "Superadmin", action = "Users" });

app.MapControllerRoute(
    name: "route-superadmin-removeuser",
    pattern: "RemoveUser",
    defaults: new { controller = "Superadmin", action = "RemoveUser" });

app.MapControllerRoute(
    name: "route-superadmin-removeadmin",
    pattern: "RemoveAdminacc",
    defaults: new { controller = "Superadmin", action = "RemoveAdminacc" });

// Dummy data API routes (handled by a disabled controller that returns informative 403 responses)
app.MapControllerRoute(
    name: "route-api-dummydata-generate",
    pattern: "api/admin/dummydata/generate",
    defaults: new { controller = "DummyData", action = "Generate" });

app.MapControllerRoute(
    name: "route-api-dummydata-statistics",
    pattern: "api/admin/dummydata/statistics",
    defaults: new { controller = "DummyData", action = "Statistics" });

app.MapControllerRoute(
    name: "route-api-dummydata-clear",
    pattern: "api/admin/dummydata/clear",
    defaults: new { controller = "DummyData", action = "Clear" });

// Default fallback route (lowest priority)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Landingpage}/{id?}")
    .WithStaticAssets();

// SignalR Hub endpoint
app.MapHub<LingapDVO.Hubs.NotificationHub>("/notificationHub");

app.Run();
