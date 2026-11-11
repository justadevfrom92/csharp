using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Secure by default: EVERY page needs a login unless it says [AllowAnonymous].
    AuthorizationPolicy loggedInUsersOnly = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(loggedInUsersOnly));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;                         // active users stay logged in
        options.Cookie.Name = ".Roadmap.Auth";
        options.Cookie.HttpOnly = true;                           // JavaScript can't read it
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication(); // who are you? (reads the cookie)
app.UseAuthorization();  // are you allowed? (checks [Authorize])
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
