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
            // htmx ignores non-2xx responses by default, so returning BadRequest here would
            // make an invalid submission silently no-op. Instead, reply 200 but use response
            // headers to redirect the swap to the form's error region (HX-Retarget) and
            // replace its contents (HX-Reswap). No HX-Trigger is sent, so the form is NOT
            // reset — the user keeps their input to correct it.
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

        // Signal a successful add so the form (and only on success) clears itself and any
        // lingering error message — see hx-on:example-item-added in Index.cshtml.
        Response.Headers["HX-Trigger"] = "example-item-added";
        return PartialView("_ExampleRow", item);
    }
}
