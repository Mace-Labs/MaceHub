using Hangfire.Dashboard;

namespace MaceHub.Web.Infrastructure;

/// <summary>
/// Allows ALL requests to the Hangfire dashboard. This is intentional and matches the
/// project's accepted threat model: single-user, home-LAN only, http, no authentication
/// (see CLAUDE.md — Security posture). Hangfire's built-in default only permits requests
/// that are local to the server process, which returns 401 for any request reaching the
/// container from another host (e.g. a browser on the LAN). This filter removes that
/// restriction.
///
/// MUST be replaced with a real authorization check before the box is ever exposed
/// beyond the local network.
/// </summary>
public sealed class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}
