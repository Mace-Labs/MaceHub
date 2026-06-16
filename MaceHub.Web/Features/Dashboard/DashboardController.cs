using Microsoft.AspNetCore.Mvc;

namespace MaceHub.Web.Features.Dashboard;

// The hub's front door. Over time this lists/links to the real feature slices.
// For the scaffold it links to the disposable Example slice and the Hangfire dashboard.
public class DashboardController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();
}
