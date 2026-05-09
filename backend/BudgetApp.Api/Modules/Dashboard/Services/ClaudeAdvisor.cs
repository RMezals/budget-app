using System.Text;
using System.Text.Json;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class ClaudeAdvisor(IHttpClientFactory httpClientFactory, IConfiguration config) : IAiAdvisor
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiKeyHeader = "x-api-key";
    private const string VersionHeader = "anthropic-version";
    private const string ApiVersion = "2023-06-01";
    private const string DefaultModel = "claude-haiku-4-5-20251001";
    private const int DefaultMaxTokens = 1024;

    public async Task<string> AnalyseAsync(string financialSummary, string userGoals)
    {
        var apiKey = config["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Claude is not configured. Add Anthropic:ApiKey to appsettings.");

        var model = config["Anthropic:Model"] ?? DefaultModel;
        var maxTokens = int.TryParse(config["Anthropic:MaxTokens"], out var t) ? t : DefaultMaxTokens;

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyHeader, apiKey);
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

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        return parsed.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
    }

    private static string BuildPrompt(string financialSummary, string userGoals) =>
        $"You are a personal finance advisor. The user wants to {userGoals}. " +
        $"Based on this financial summary, provide 3 concise, actionable tips specifically focused on helping them achieve their goals:\n\n{financialSummary}";
}
