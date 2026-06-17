using System.ComponentModel.DataAnnotations;

namespace MaceHub.Web.Features.Example;

public sealed class ExampleIndexViewModel
{
    public required IReadOnlyList<ExampleItem> Items { get; init; }
}

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
