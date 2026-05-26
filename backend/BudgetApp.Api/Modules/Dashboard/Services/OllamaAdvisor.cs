using System.Text;
using System.Text.Json;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class OllamaAdvisor(IHttpClientFactory httpClientFactory, IConfiguration config) : AiAdvisorBase
{
    private readonly string _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
    private readonly string _model = config["Ollama:Model"] ?? "llama3.2";

    public override async Task<string> AnalyseAsync(string financialSummary, string userGoals, string? apiKey = null)
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

        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Ollama request failed ({response.StatusCode}): {ex.Message}");
        }

        var body = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        return parsed.RootElement.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }

}
