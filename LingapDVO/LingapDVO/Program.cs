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

// Notifications
builder.Services.AddScoped<INotificationService, NotificationService>();
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

// ?? Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    // ? Ensure cookies work for external redirect
    options.Cookie.SameSite = SameSiteMode.Lax; // Or None if using HTTPS everywhere
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Use Always in production
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<SmsService>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Accountverification}/{id?}")
    .WithStaticAssets();


app.MapControllerRoute(
    name: "hashed-login",
    pattern: "Login",
    defaults: new { controller = "Login", action = "Login" });

app.MapControllerRoute(
    name: "hashed-login",
    pattern: "Register",
    defaults: new { controller = "Login", action = "Register" });

app.MapControllerRoute(
    name: "Homepage",
    pattern: "Homepage",
    defaults: new { controller = "Dashboard", action = "Homepage" });

app.MapControllerRoute(
    name: "Superadmin",
    pattern: "Superadmin",
    defaults: new { controller = "Superadmin", action = "Superadmin" });

app.MapControllerRoute(
    name: "Analyticsdashboard",
    pattern: "Analyticsdashboard",
    defaults: new { controller = "Adminuser", action = "Analyticsdashboard" });

app.MapControllerRoute(
    name: "Admin",
    pattern: "Admin",
    defaults: new { controller = "Adminuser", action = "Admin" });


app.MapControllerRoute(
    name: "Accountverification",
    pattern: "Accountverification",
    defaults: new { controller = "Login", action = "Accountverification" });

app.MapControllerRoute(
    name: "Uploads",
    pattern: "Uploads",
    defaults: new { controller = "Dashboard", action = "Uploads" });

app.MapControllerRoute(
    name: "Userprofile",
    pattern: "Userprofile",
    defaults: new { controller = "Dashboard", action = "Userprofile" });


app.MapControllerRoute(
    name: "FillupformHospitalBill",
    pattern: "FillupformHospitalBill",
    defaults: new { controller = "Dashboard", action = "FillupformHospitalBill" });


app.MapControllerRoute(
    name: "Medicalandlabform",
    pattern: "Medicalandlabform",
    defaults: new { controller = "Dashboard", action = "Medicalandlabform" });

app.MapControllerRoute(
    name: "Funeralburialform",
    pattern: "Funeralburialform",
    defaults: new { controller = "Dashboard", action = "Funeralburialform" });

app.MapControllerRoute(
    name: "Eligibilitychecking",
    pattern: "Eligibilitychecking",
    defaults: new { controller = "Dashboard", action = "Eligibilitychecking" });


app.MapControllerRoute(
    name: "Maps",
    pattern: "Maps",
    defaults: new { controller = "Dashboard", action = "Maps" });

app.MapControllerRoute(
    name: "Listofpartners",
    pattern: "Listofpartners",
    defaults: new { controller = "Dashboard", action = "Listofpartners" });

app.MapControllerRoute(
    name: "History",
    pattern: "history",
    defaults: new { controller = "Dashboard", action = "History" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");





app.Run();
