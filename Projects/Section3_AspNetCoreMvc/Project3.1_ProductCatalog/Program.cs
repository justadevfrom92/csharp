var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseStaticFiles();
app.UseRouting();

// Conventional routing: /{controller}/{action}/{id?}  ->  "/" goes to Products/Index.
app.MapControllerRoute("default", "{controller=Products}/{action=Index}/{id?}");

app.Run();

// Lets the Playwright tests in Section 4.3 start this app in-process.
public partial class Program
{
}
