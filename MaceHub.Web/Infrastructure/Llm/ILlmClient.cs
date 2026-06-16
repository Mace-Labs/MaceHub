namespace MaceHub.Web.Infrastructure.Llm;

/// <summary>
/// The owned LLM abstraction. Slices, controllers and view models depend on THIS
/// interface, never on a provider SDK directly. This is the one deliberate seam in
/// the project: it keeps the provider's blast radius to a single implementation, so
/// swapping providers later means writing one new class — not touching any slice.
///
/// Keep it to the handful of methods features actually need (a completion call now;
/// a streaming call when a feature genuinely streams to the browser). Do NOT grow it
/// into a multi-provider configurable abstraction until a second provider exists.
/// </summary>
public interface ILlmClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
