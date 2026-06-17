using MaceHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaceHub.Web.Features.Example.Details;

public class DetailsController(MaceHubDbContext db) : Controller
{
    [HttpGet("/example/{id:int}")]
    public async Task<IActionResult> Index(int id, CancellationToken ct)
    {
        var item = await db.ExampleItems.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null)
        {
            return NotFound();
        }

        return View(item);
    }
}
