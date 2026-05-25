using System.Text;
using System.Text.Json;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class ClaudeAdvisor(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<ClaudeAdvisor> logger) : AiAdvisorBase
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiKeyHeader = "x-api-key";
    private const string VersionHeader = "anthropic-version";
    private const string ApiVersion = "2023-06-01";
    private const string DefaultModel = "claude-haiku-4-5-20251001";
    private const int DefaultMaxTokens = 1024;

    public override async Task<string> AnalyseAsync(string financialSummary, string userGoals, string? apiKey = null)
    {
        var key = apiKey ?? config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Claude API key is required. Please provide your API key.");
        }

        var model = config["Anthropic:Model"] ?? DefaultModel;
        var maxTokens = int.TryParse(config["Anthropic:MaxTokens"], out var t) ? t : DefaultMaxTokens;

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyHeader, key);
        client.DefaultRequestHeaders.Add(VersionHeader, ApiVersion);

        var payload = new
        {
            model,
            max_tokens = maxTokens,
            messages = new[] { new { role = "user", content = BuildPrompt(financialSummary, userGoals) } }
        };

        var response = await client.PostAsync(
            ApiUrl,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Claude API error. StatusCode: {StatusCode}, Response: {ErrorBody}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Claude API error ({response.StatusCode}): {errorBody}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        return parsed.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
    }

}
