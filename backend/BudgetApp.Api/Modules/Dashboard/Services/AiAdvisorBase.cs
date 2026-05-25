namespace BudgetApp.Api.Modules.Dashboard.Services;

public abstract class AiAdvisorBase : IAiAdvisor
{
    public abstract Task<string> AnalyseAsync(string financialSummary, string userGoals, string? apiKey = null);

    protected static string BuildPrompt(string financialSummary, string userGoals) =>
        $"You are a personal finance advisor. The user wants to {userGoals}. " +
        $"Based on this financial summary, provide exactly 3 concise, actionable tips.\n\n" +
        $"Format your response as a simple numbered list:\n" +
        $"1. First tip\n" +
        $"2. Second tip\n" +
        $"3. Third tip\n\n" +
        $"Do NOT use headers (##) or special formatting. Just use plain numbered points.\n\n" +
        $"Financial summary:\n{financialSummary}";
}
