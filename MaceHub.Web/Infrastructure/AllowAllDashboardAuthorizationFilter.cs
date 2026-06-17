using Hangfire.Dashboard;

namespace MaceHub.Web.Infrastructure;

/// <summary>
/// Allows all dashboard requests, overriding Hangfire's default that 401s any request
/// not local to the server process (so a LAN browser can reach it). Deliberate under the
/// home-LAN-only threat model; must become a real auth check before any wider exposure.
/// </summary>
public sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
