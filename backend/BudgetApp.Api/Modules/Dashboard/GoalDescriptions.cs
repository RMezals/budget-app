namespace BudgetApp.Api.Modules.Dashboard;

public static class GoalDescriptions
{
    public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>
    {
        ["save_more"]       = "save more money",
        ["reduce_expenses"] = "reduce expenses",
        ["invest"]          = "start investing",
        ["emergency_fund"]  = "build an emergency fund",
        ["pay_debt"]        = "pay off debt",
        ["budget_better"]   = "budget better"
    };
}
