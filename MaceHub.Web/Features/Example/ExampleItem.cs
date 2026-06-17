namespace MaceHub.Web.Features.Example;

public class ExampleItem
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Url { get; init; }

    public DateTime CreatedUtc { get; init; }
}
