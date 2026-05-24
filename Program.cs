using EmailApp.Components;
using EmailApp.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "CIP Station Alarm Notification";
});

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

app.Run();
