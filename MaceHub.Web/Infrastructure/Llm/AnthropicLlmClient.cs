using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace MaceHub.Web.Infrastructure.Llm;

/// <summary>
/// The only file permitted to reference an <c>Anthropic.SDK</c> type; the SDK stays
/// behind <see cref="ILlmClient"/> so a provider swap touches this class alone.
/// </summary>
public sealed class AnthropicLlmClient : ILlmClient
{
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly Lazy<AnthropicClient> _client;

    public AnthropicLlmClient(IConfiguration configuration)
    {
        _apiKey = configuration["ANTHROPIC_API_KEY"];
        _model = configuration["Llm:Model"] ?? "claude-opus-4-8";

        // Lazy so the app boots without a key; the client is built on first use.
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
