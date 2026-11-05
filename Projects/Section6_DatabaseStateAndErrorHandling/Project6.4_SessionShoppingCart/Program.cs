var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Session data is kept on the SERVER (here: in memory). The browser only gets an encrypted session id cookie.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);          // cart is forgotten after 20 idle minutes
    options.Cookie.Name = ".Roadmap.Cart";
    options.Cookie.HttpOnly = true;                           // JavaScript can't read it (protects against XSS stealing it)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // only sent over HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;               // not sent on cross-site POSTs (helps against CSRF)
    options.Cookie.IsEssential = true;                        // the cart needs it to work at all
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseSession(); // must be before the controllers so they can use HttpContext.Session
app.MapControllerRoute("default", "{controller=Cart}/{action=Index}/{id?}");
app.Run();
