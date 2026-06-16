namespace MaceHub.Web.Features.Example;

/// <summary>
/// Disposable example entity — proves EF Core + Postgres wiring end to end.
/// Deleted along with the rest of the Example slice once the first real feature exists.
/// </summary>
public class ExampleItem
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Url { get; init; }

    public DateTime CreatedUtc { get; init; }
}
