namespace MaceHub.Web.Infrastructure.Llm;

/// <summary>
/// The owned LLM seam. Slices depend on this, never on a provider SDK directly.
/// </summary>
public interface ILlmClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
