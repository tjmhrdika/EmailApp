using EmailApp.Components;
using EmailApp.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "CIP Station Alarm Notification";
});

if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5146");
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys")))
    .SetApplicationName("EmailApp");

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages(context =>
{
    var httpContext = context.HttpContext;
    var requestPath = httpContext.Request.Path;

    if (httpContext.Response.StatusCode == StatusCodes.Status401Unauthorized &&
        !requestPath.StartsWithSegments("/api") &&
        !requestPath.StartsWithSegments("/login") &&
        !httpContext.Response.HasStarted)
    {
        var returnUrl = Uri.EscapeDataString($"{httpContext.Request.PathBase}{httpContext.Request.Path}{httpContext.Request.QueryString}");
        httpContext.Response.Redirect($"/login?returnUrl={returnUrl}");
    }

    return Task.CompletedTask;
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

if (!WindowsServiceHelpers.IsWindowsService())
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://localhost:5146",
                UseShellExecute = true
            });
        }
        catch
        {
            // The server is still usable even if Windows cannot open a browser.
        }
    });
}

app.Run();
