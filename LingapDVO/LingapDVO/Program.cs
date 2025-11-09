using LingapDVO.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ?? Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})

.AddCookie()

.AddFacebook(options =>
{
    options.AppId = "818350247528005";
    options.AppSecret = "b56a1f8ab40396f09efc99ecaabcff1f";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.SaveTokens = true;

    // Add this line to match your controller action
    options.CallbackPath = new PathString("/signin-facebook");

    options.Scope.Add("public_profile");
    options.Fields.Add("picture.type(large)");
    options.Fields.Add("id");
})

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

// MVC
builder.Services.AddControllersWithViews();

// ?? Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// ?? Session - Configurable inactivity timeout with warning
builder.Services.AddSession(options =>
{
    // Read timeout from configuration (defaults to 10 if not set)
    var sessionTimeout = builder.Configuration.GetValue<int>("Session:IdleTimeoutMinutes", 10);
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeout);

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = builder.Configuration.GetValue<string>("Session:CookieName", ".LingapDVO.Session");

    // ? Ensure cookies work for external redirect
    options.Cookie.SameSite = SameSiteMode.Lax; // Or None if using HTTPS everywhere
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Use Always in production
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ISmsService, SmsService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMultiChannelNotificationService, MultiChannelNotificationService>();

// SignalR for real-time notifications
builder.Services.AddSignalR();

var app = builder.Build();

// ?? Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

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
    pattern: "Accountverification",
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
    name: "route-facebook-login",
    pattern: "FacebookLogin",
    defaults: new { controller = "Login", action = "FacebookLogin" });

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
    pattern: "Homepage",
    defaults: new { controller = "Dashboard", action = "Homepage" });

app.MapControllerRoute(
    name: "route-userprofile",
    pattern: "Userprofile",
    defaults: new { controller = "Dashboard", action = "Userprofile" });

app.MapControllerRoute(
    name: "route-uploads",
    pattern: "Uploads",
    defaults: new { controller = "Dashboard", action = "Eligibilitychecking" });

app.MapControllerRoute(
    name: "route-maps",
    pattern: "Maps",
    defaults: new { controller = "Dashboard", action = "Maps" });

app.MapControllerRoute(
    name: "route-listofpartners",
    pattern: "Listofpartners",
    defaults: new { controller = "Dashboard", action = "Listofpartners" });

app.MapControllerRoute(
    name: "route-history",
    pattern: "History",
    defaults: new { controller = "Dashboard", action = "History" });

app.MapControllerRoute(
    name: "route-eligibilitychecking",
    pattern: "Eligibilitychecking",
    defaults: new { controller = "Dashboard", action = "Eligibilitychecking" });

// Dashboard Forms Routes
app.MapControllerRoute(
    name: "route-hospitalbill",
    pattern: "FillupformHospitalBill",
    defaults: new { controller = "Dashboard", action = "FillupformHospitalBill" });

app.MapControllerRoute(
    name: "route-hospitalbill-edit",
    pattern: "FillupformHospitalBilledit/{id?}",
    defaults: new { controller = "Dashboard", action = "FillupformHospitalBilledit" });

app.MapControllerRoute(
    name: "route-hospitalbill-delete",
    pattern: "FillupformHospitalBilldelete/{id?}",
    defaults: new { controller = "Dashboard", action = "FillupformHospitalBilldelete" });

app.MapControllerRoute(
    name: "route-hospitalbill-view",
    pattern: "Fillupformhospitalbillview/{id?}",
    defaults: new { controller = "Dashboard", action = "Fillupformhospitalbillview" });

app.MapControllerRoute(
    name: "route-medicallab",
    pattern: "Medicalandlabform",
    defaults: new { controller = "Dashboard", action = "Medicalandlabform" });

app.MapControllerRoute(
    name: "route-medicallab-edit",
    pattern: "Medicalandlabformedit/{id?}",
    defaults: new { controller = "Dashboard", action = "Medicalandlabformedit" });

app.MapControllerRoute(
    name: "route-medicallab-delete",
    pattern: "Medicalandlabformedelete/{id?}",
    defaults: new { controller = "Dashboard", action = "Medicalandlabformedelete" });

app.MapControllerRoute(
    name: "route-medicallab-view",
    pattern: "Medicalandlabformview/{id?}",
    defaults: new { controller = "Dashboard", action = "Medicalandlabformview" });

app.MapControllerRoute(
    name: "route-funeralburial",
    pattern: "Funeralburialform",
    defaults: new { controller = "Dashboard", action = "Funeralburialform" });

app.MapControllerRoute(
    name: "route-funeralburial-edit",
    pattern: "Funeralburialformedit/{id?}",
    defaults: new { controller = "Dashboard", action = "Funeralburialformedit" });

app.MapControllerRoute(
    name: "route-funeralburial-delete",
    pattern: "Funeralburialformedelete/{id?}",
    defaults: new { controller = "Dashboard", action = "Funeralburialformedelete" });

app.MapControllerRoute(
    name: "route-funeralburial-view",
    pattern: "Funeralburialformview/{id?}",
    defaults: new { controller = "Dashboard", action = "Funeralburialformview" });

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
    name: "route-priorities",
    pattern: "Priorities",
    defaults: new { controller = "Adminuser", action = "Priorities" });

// Admin Hospital Bill Status Routes
app.MapControllerRoute(
    name: "route-admin-hospitalbill-update",
    pattern: "FillupformHospitalBillUpdatestatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FillupformHospitalBillUpdatestatus" });

app.MapControllerRoute(
    name: "route-admin-hospitalbill-processing",
    pattern: "FillupformHospitalBillUpdateprocessingstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FillupformHospitalBillUpdateprocessingstatus" });

app.MapControllerRoute(
    name: "route-admin-hospitalbill-approved",
    pattern: "FillupformHospitalBillapprovedstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FillupformHospitalBillapprovedstatus" });

app.MapControllerRoute(
    name: "route-admin-hospitalbill-disapproved",
    pattern: "FillupformHospitalBillDisapprovedstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FillupformHospitalBillDisapprovedstatus" });

app.MapControllerRoute(
    name: "route-admin-hospitalbill-claimed",
    pattern: "FillupformHospitalBillUpdatestatuClaimeddocs/{id?}",
    defaults: new { controller = "Adminuser", action = "FillupformHospitalBillUpdatestatuClaimeddocs" });

// Admin Medical Lab Status Routes
app.MapControllerRoute(
    name: "route-admin-medicallab-status",
    pattern: "Medicalandlabformstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "Medicalandlabformstatus" });

app.MapControllerRoute(
    name: "route-admin-medicallab-processing",
    pattern: "MedicalandlabformUpdateprocessingstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "MedicalandlabformUpdateprocessingstatus" });

app.MapControllerRoute(
    name: "route-admin-medicallab-approved",
    pattern: "Medicalandlabformapprovedsstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "Medicalandlabformapprovedsstatus" });

app.MapControllerRoute(
    name: "route-admin-medicallab-disapproved",
    pattern: "MedicalandlabformDisapprovedstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "MedicalandlabformDisapprovedstatus" });

app.MapControllerRoute(
    name: "route-admin-medicallab-claimed",
    pattern: "MedicalandlabformstatusUpdateClaimeddocs/{id?}",
    defaults: new { controller = "Adminuser", action = "MedicalandlabformstatusUpdateClaimeddocs" });

// Admin Funeral Burial Status Routes
app.MapControllerRoute(
    name: "route-admin-funeralburial-status",
    pattern: "Funeralburialformstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "Funeralburialformstatus" });

app.MapControllerRoute(
    name: "route-admin-funeralburial-processing",
    pattern: "FuneralburialformUpdateprocessingstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FuneralburialformUpdateprocessingstatus" });

app.MapControllerRoute(
    name: "route-admin-funeralburial-approved",
    pattern: "Funeralburialapprovedstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "Funeralburialapprovedstatus" });

app.MapControllerRoute(
    name: "route-admin-funeralburial-disapproved",
    pattern: "FuneralburialDisapprovedstatus/{id?}",
    defaults: new { controller = "Adminuser", action = "FuneralburialDisapprovedstatus" });

app.MapControllerRoute(
    name: "route-admin-funeralburial-claimed",
    pattern: "FuneralburialapprovedstatusUpdateClaimeddocs/{id?}",
    defaults: new { controller = "Adminuser", action = "FuneralburialapprovedstatusUpdateClaimeddocs" });

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

// Default fallback route (lowest priority)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Landingpage}/{id?}")
    .WithStaticAssets();

// SignalR Hub endpoint
app.MapHub<LingapDVO.Hubs.NotificationHub>("/notificationHub");

app.Run();
