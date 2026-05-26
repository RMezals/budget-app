namespace BudgetApp.Api.Modules.Dashboard;

public record AnalyseRequest(string Provider = AiProviders.Ollama, List<string>? Goals = null, string? ApiKey = null);
public record AdvisorResult(string Provider, string Tips);
public record ErrorResult(string Error);
