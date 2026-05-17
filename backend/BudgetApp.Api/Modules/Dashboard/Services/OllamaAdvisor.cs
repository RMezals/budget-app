using System.Text;
using System.Text.Json;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class OllamaAdvisor(IHttpClientFactory httpClientFactory, IConfiguration config) : IAiAdvisor
{
    private readonly string _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
    private readonly string _model = config["Ollama:Model"] ?? "llama3.2";

    public async Task<string> AnalyseAsync(string financialSummary, string userGoals, string? apiKey = null)
    {
        using var client = httpClientFactory.CreateClient();

        var payload = new
        {
            model = _model,
            stream = false,
            messages = new[] { new { role = "user", content = BuildPrompt(financialSummary, userGoals) } }
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(
                $"{_baseUrl}/api/chat",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException("Ollama is not running. Start it locally with: ollama serve");
        }

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        return parsed.RootElement.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

    private static string BuildPrompt(string financialSummary, string userGoals) =>
        $"You are a personal finance advisor. The user wants to {userGoals}. " +
        $"Based on this financial summary, provide exactly 3 concise, actionable tips.\n\n" +
        $"Format your response as a simple numbered list:\n" +
        $"1. First tip\n" +
        $"2. Second tip\n" +
        $"3. Third tip\n\n" +
        $"Do NOT use headers (##) or special formatting. Just use plain numbered points.\n\n" +
        $"Financial summary:\n{financialSummary}";
}
