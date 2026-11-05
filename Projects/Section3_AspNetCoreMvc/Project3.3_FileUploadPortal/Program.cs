var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseStaticFiles(); // serves wwwroot, so saved files can be shown at /uploads/...
app.UseRouting();
app.MapControllerRoute("default", "{controller=Upload}/{action=Index}/{id?}");
app.Run();
