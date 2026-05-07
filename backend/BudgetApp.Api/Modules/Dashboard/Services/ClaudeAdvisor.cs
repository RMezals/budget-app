using System.Text;
using System.Text.Json;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class ClaudeAdvisor(IHttpClientFactory httpClientFactory, IConfiguration config) : IAiAdvisor
{
    public async Task<string> AnalyseAsync(string financialSummary, string userGoals)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return "Claude is not configured. Add Anthropic:ApiKey to appsettings.";

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var payload = new
        {
            model      = "claude-haiku-4-5-20251001",
            max_tokens = 1024,
            messages   = new[]
            {
                new
                {
                    role    = "user",
                    content = $"You are a personal finance advisor. The user wants to {userGoals}. Based on this financial summary, provide 3 concise, actionable tips specifically focused on helping them achieve their goals:\n\n{financialSummary}"
                }
            }
        };

        var response = await client.PostAsync(
            "https://api.anthropic.com/v1/messages",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        var body   = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        return parsed.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
    }
}
