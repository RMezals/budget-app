using System.Text;
using System.Text.Json;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class OllamaAdvisor(IHttpClientFactory httpClientFactory, IConfiguration config) : IAiAdvisor
{
    public async Task<string> AnalyseAsync(string financialSummary, string userGoals)
    {
        var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model   = config["Ollama:Model"]   ?? "llama3.2";

        using var client = httpClientFactory.CreateClient();

        var payload = new
        {
            model,
            stream   = false,
            messages = new[]
            {
                new
                {
                    role    = "user",
                    content = $"You are a personal finance advisor. The user wants to {userGoals}. Based on this financial summary, provide 3 concise, actionable tips specifically focused on helping them achieve their goals:\n\n{financialSummary}"
                }
            }
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(
                $"{baseUrl}/api/chat",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        }
        catch (HttpRequestException)
        {
            return "Ollama is not running. Start it locally with: ollama serve";
        }

        response.EnsureSuccessStatusCode();

        var body   = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        return parsed.RootElement.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }
}
