using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // CSRF: every POST/PUT/DELETE must carry a valid anti-forgery token. No action can forget it.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

// Security headers on every response.
app.Use(async (context, next) =>
{
    // XSS safety net: only run scripts/styles from our own site; no inline <script>.
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";   // don't guess file types
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Guestbook}/{action=Index}/{id?}");
app.Run();
