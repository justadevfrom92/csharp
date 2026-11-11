using RazorPagesContacts.Stores;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages(); // Razor Pages instead of AddControllersWithViews()
builder.Services.AddSingleton<ContactStore>();

var app = builder.Build();
app.UseStatusCodePages();
app.UseRouting();
app.MapRazorPages();          // every .cshtml file under Pages/ with "@page" becomes a URL
app.MapGet("/", () => Results.Redirect("/Contacts"));
app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
