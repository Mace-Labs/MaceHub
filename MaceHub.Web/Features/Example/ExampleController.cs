using MaceHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaceHub.Web.Features.Example;

// Controller-as-handler: the action IS the handler. It uses the DbContext directly —
// no repository/service layer (see CLAUDE.md). This disposable slice proves the wiring
// end to end (full page render + ONE htmx fragment swap + a nested sub-feature link).
public class ExampleController(MaceHubDbContext db) : Controller
{
    // Full page load.
    [HttpGet("/example")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await db.ExampleItems
            .OrderByDescending(i => i.Id)
            .ToListAsync(ct);

        return View(new ExampleIndexViewModel { Items = items });
    }

    // htmx hx-post: inserts an item (data-annotation validation) and returns the new
    // row partial, which htmx swaps into the list. No full-page reload.
    [HttpPost("/example")]
    public async Task<IActionResult> Add(AddExampleItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var item = new ExampleItem
        {
            Name = request.Name,
            Url = request.Url,
            CreatedUtc = DateTime.UtcNow,
        };

        db.ExampleItems.Add(item);
        await db.SaveChangesAsync(ct);

        return PartialView("_ExampleRow", item);
    }
}
