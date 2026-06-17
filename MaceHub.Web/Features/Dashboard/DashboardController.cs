using Microsoft.AspNetCore.Mvc;

namespace MaceHub.Web.Features.Dashboard;

public class DashboardController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();
}
