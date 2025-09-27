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

    // ? Important: set correct sign-in scheme
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // ? Remove CallbackPath override unless strictly required
     options.CallbackPath = "/signin-google";

    // Optional: force re-consent
    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
    {
        OnRedirectToAuthorizationEndpoint = context =>
        {
            var redirectUri = context.RedirectUri;
            if (!redirectUri.Contains("prompt="))
            {
                redirectUri += (redirectUri.Contains("?") ? "&" : "?") + "prompt=consent&access_type=offline";
            }

            context.Response.Redirect(redirectUri);
            return Task.CompletedTask;
        }
    };
});



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
    pattern: "{controller=Dashboard}/{action=Landingpage}/{id?}")
    .WithStaticAssets();

app.Run();
