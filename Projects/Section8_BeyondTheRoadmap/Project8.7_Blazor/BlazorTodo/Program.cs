using BlazorTodo.Components;

var builder = WebApplication.CreateBuilder(args);

// Blazor components, with INTERACTIVE SERVER rendering (C# event handlers run on the server).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.MapStaticAssets(); // serves wwwroot AND the framework files like _framework/blazor.web.js
app.UseAntiforgery(); // Blazor forms are protected against CSRF
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.Run();

// Lets tests reference the app's assembly.
public partial class Program
{
}
