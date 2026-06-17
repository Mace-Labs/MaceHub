using Hangfire;
using Hangfire.PostgreSql;
using MaceHub.Web.Infrastructure;
using MaceHub.Web.Infrastructure.Llm;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is not configured. Set ConnectionStrings__Default (see .env.example).");

builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(o =>
    o.ViewLocationExpanders.Add(new FeatureViewLocationExpander()));

builder.Services.AddDbContext<MaceHubDbContext>(o => o.UseNpgsql(connectionString));

builder.Services.AddSingleton<ILlmClient, AnthropicLlmClient>();

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

var app = builder.Build();

// Migrations must run before Hangfire touches the same database (it bootstraps its own
// schema), so this happens before the dashboard and server are wired up below.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<MaceHubDbContext>();
        db.Database.Migrate();
        logger.LogInformation("EF Core migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed — halting startup.");
        throw;
    }
}

app.UseStaticFiles();
app.UseRouting();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AllowAllDashboardAuthorizationFilter()],
});

app.MapControllers();

// Use the DI IRecurringJobManager, not the static RecurringJob facade: the static API
// reads the global JobStorage.Current, which is only set as a side effect of the storage
// being resolved elsewhere, making it sensitive to registration order.
app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate(
    "heartbeat",
    () => Console.WriteLine($"[heartbeat] Mace Hub alive at {DateTime.UtcNow:u}"),
    Cron.Minutely);

app.Run();

// Public so a test host (WebApplicationFactory) can reference the entry point.
public partial class Program;
