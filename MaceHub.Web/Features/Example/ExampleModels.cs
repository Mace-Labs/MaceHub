using System.ComponentModel.DataAnnotations;

namespace MaceHub.Web.Features.Example;

// View model for the full-page Example list.
public sealed class ExampleIndexViewModel
{
    public required IReadOnlyList<ExampleItem> Items { get; init; }
}

// Input for adding an item. Data-annotation validation only — the scaffold does not
// demonstrate FluentValidation (deferred to the first slice with non-trivial input).
public sealed class AddExampleItemRequest
{
    [Required]
    [StringLength(200)]
    public required string Name { get; init; }

    [Required]
    [StringLength(2048)]
    [Url]
    public required string Url { get; init; }
}
