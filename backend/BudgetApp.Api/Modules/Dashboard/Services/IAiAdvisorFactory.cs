namespace BudgetApp.Api.Modules.Dashboard.Services;

/// <summary>
/// Factory for creating AI advisor instances based on provider name
/// </summary>
public interface IAiAdvisorFactory
{
    /// <summary>
    /// Gets an AI advisor instance for the specified provider
    /// </summary>
    /// <param name="provider">The provider name (e.g., "claude", "ollama")</param>
    /// <returns>An IAiAdvisor implementation</returns>
    /// <exception cref="ArgumentException">Thrown when provider is invalid</exception>
    IAiAdvisor GetAdvisor(string provider);
}
