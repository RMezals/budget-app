namespace BudgetApp.Api.Modules.Dashboard.Services;

/// <summary>
/// Factory implementation for creating AI advisor instances
/// </summary>
public class AiAdvisorFactory(IServiceProvider serviceProvider) : IAiAdvisorFactory
{
    public IAiAdvisor GetAdvisor(string provider)
    {
        if (!AiProviders.IsValid(provider))
        {
            throw new ArgumentException($"Invalid provider '{provider}'. Valid providers are: {string.Join(", ", AiProviders.All)}", nameof(provider));
        }

        return serviceProvider.GetRequiredKeyedService<IAiAdvisor>(provider);
    }
}
