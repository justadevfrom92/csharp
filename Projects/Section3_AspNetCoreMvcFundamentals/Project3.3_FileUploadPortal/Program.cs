using Microsoft.AspNetCore.StaticFiles;
using MvcFileUploadPortal.Models;
using MvcFileUploadPortal.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IUploadedFileStorageService, SecureUploadedFileStorageService>();
builder.WebHost.ConfigureKestrel(kestrelOptions =>
    kestrelOptions.Limits.MaxRequestBodySize = UploadSizeLimits.MaximumMultipartRequestBodyInBytes);

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRequestLocalization("en-US");

// Only explicitly mapped MIME types are served; unknown extensions return 404 instead of being sniffed.
var uploadContentTypeProvider = new FileExtensionContentTypeProvider();
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = uploadContentTypeProvider,
    ServeUnknownFileTypes = false,
    OnPrepareResponse = staticFileContext =>
    {
        // Stops browsers from MIME-sniffing an uploaded file into executable HTML/script.
        staticFileContext.Context.Response.Headers.XContentTypeOptions = "nosniff";
        if (staticFileContext.Context.Request.Path.StartsWithSegments("/uploads/documents"))
        {
            // Documents always download rather than render inline in the site's origin.
            staticFileContext.Context.Response.Headers.ContentDisposition = "attachment";
        }
    }
});

app.UseRouting();
app.UseAntiforgery();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;
