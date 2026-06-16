using MaceHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaceHub.Web.Features.Example.Details;

// Sub-feature in a nested namespace (...Features.Example.Details). It's a plain
// controller — no areas, no per-controller view config. The FeatureViewLocationExpander
// derives the view path "Example/Details" from this namespace, so Index.cshtml resolves
// from Features/Example/Details/ automatically. Proves one-level nesting works.
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
