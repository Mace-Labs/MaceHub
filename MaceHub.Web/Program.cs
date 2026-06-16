using Hangfire;
using Hangfire.PostgreSql;
using MaceHub.Web.Infrastructure;
using MaceHub.Web.Infrastructure.Llm;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Single source of truth for the DB connection (see §10): a full connection string
// supplied as ConnectionStrings__Default (.env → env var → config). No host/port
// assembly in app code.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is not configured. Set ConnectionStrings__Default (see .env.example).");

// MVC (vertical-slice). Views are resolved by the namespace-driven expander — this is
// the one place framework view-location defaults are overridden.
builder.Services.AddControllersWithViews();
builder.Services.Configure<RazorViewEngineOptions>(o =>
    o.ViewLocationExpanders.Add(new FeatureViewLocationExpander()));

// EF Core + Postgres (plain — no pgvector day one).
builder.Services.AddDbContext<MaceHubDbContext>(o => o.UseNpgsql(connectionString));

// LLM seam: slices depend on ILlmClient, never on the provider SDK directly.
builder.Services.AddSingleton<ILlmClient, AnthropicLlmClient>();

// Hangfire DI registration (storage points at the same Postgres instance). Registering
// here is fine; what matters is the server doesn't start processing — and the storage
// isn't first touched — before EF migrations have run (see ordering below).
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

var app = builder.Build();

// --- Startup ordering: EF migrations MUST complete before Hangfire storage is touched
// and the server starts processing jobs (Hangfire bootstraps its OWN schema, and both
// hit the same DB). Migrate() runs here, before any request pipeline / dashboard. ---
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
        // Halt startup rather than half-applying / running against an un-migrated DB.
        logger.LogCritical(ex, "Database migration failed — halting startup.");
        throw;
    }
}

app.UseStaticFiles();
app.UseRouting();

// Hangfire dashboard. NO auth: intentional — home network, single user, http only
// (matches the project's accepted threat model). The explicit allow-all filter is
// required because Hangfire's default only permits requests local to the server
// process, which 401s any request reaching the container from another host. This MUST
// be locked down before the box is ever exposed beyond the LAN.
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new AllowAllDashboardAuthorizationFilter()],
});

app.MapControllers();

// One trivial recurring job proves the wiring. The future scheduled pg_dump backup
// job will live here too (deferred — see CLAUDE.md / SCAFFOLD.md).
RecurringJob.AddOrUpdate(
    "heartbeat",
    () => Console.WriteLine($"[heartbeat] Mace Hub alive at {DateTime.UtcNow:u}"),
    Cron.Minutely);

app.Run();

// Exposed so an integration-test host could reference the entry point if ever needed.
public partial class Program;
