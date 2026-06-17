using MaceHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaceHub.Web.Features.Example;

public class ExampleController(MaceHubDbContext db) : Controller
{
    [HttpGet("/example")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await db.ExampleItems
            .OrderByDescending(i => i.Id)
            .ToListAsync(ct);

        return View(new ExampleIndexViewModel { Items = items });
    }

    [HttpPost("/example")]
    public async Task<IActionResult> Add(AddExampleItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // htmx ignores non-2xx responses, so reply 200 and retarget the swap to the
            // error region instead of returning BadRequest (which would silently no-op).
            Response.Headers["HX-Retarget"] = "#example-form-error";
            Response.Headers["HX-Reswap"] = "innerHTML";
            return PartialView("_FormErrors", ModelState);
        }

        var item = new ExampleItem
        {
            Name = request.Name,
            Url = request.Url,
            CreatedUtc = DateTime.UtcNow,
        };

        db.ExampleItems.Add(item);
        await db.SaveChangesAsync(ct);

        // Fired only on success; Index.cshtml listens for it to reset the form.
        Response.Headers["HX-Trigger"] = "example-item-added";
        return PartialView("_ExampleRow", item);
    }
}
