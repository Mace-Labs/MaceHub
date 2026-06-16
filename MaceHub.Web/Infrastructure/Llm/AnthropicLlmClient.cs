using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace MaceHub.Web.Infrastructure.Llm;

/// <summary>
/// The ONLY file permitted to reference an <c>Anthropic.SDK</c> type. It wraps the
/// community SDK behind <see cref="ILlmClient"/> so the provider can be swapped later
/// by writing one new implementation. If <c>Anthropic.SDK</c> ever goes stale it can be
/// replaced with a thin <c>HttpClient</c> wrapper here without touching any slice.
///
/// The scaffold deliberately makes NO live API calls — it only reads the key/model
/// from configuration and proves the client resolves from DI. The underlying SDK
/// client is created lazily on first use, so the app boots even when no key is set.
/// </summary>
public sealed class AnthropicLlmClient : ILlmClient
{
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly Lazy<AnthropicClient> _client;

    public AnthropicLlmClient(IConfiguration configuration)
    {
        // Single source: ANTHROPIC_API_KEY from .env → env var → config (see §10).
        _apiKey = configuration["ANTHROPIC_API_KEY"];

        // Model is pinned in configuration, never hardcoded here or in handlers.
        _model = configuration["Llm:Model"] ?? "claude-opus-4-8";

        _client = new Lazy<AnthropicClient>(() => new AnthropicClient(_apiKey));
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var parameters = new MessageParameters
        {
            Model = _model,
            MaxTokens = 1024,
            Stream = false,
            Messages = [new Message(RoleType.User, prompt)],
        };

        var response = await _client.Value.Messages.GetClaudeMessageAsync(parameters, cancellationToken);
        return response.Message?.ToString() ?? string.Empty;
    }
}
