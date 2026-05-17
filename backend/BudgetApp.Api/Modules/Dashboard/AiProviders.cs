namespace BudgetApp.Api.Modules.Dashboard;

/// <summary>
/// Constants for AI provider identifiers
/// </summary>
public static class AiProviders
{
    public const string Claude = "claude";
    public const string Ollama = "ollama";

    /// <summary>
    /// Gets all valid provider names
    /// </summary>
    public static readonly string[] All = { Claude, Ollama };

    /// <summary>
    /// Checks if a provider name is valid
    /// </summary>
    public static bool IsValid(string provider) => All.Contains(provider);
}
