using FluentValidation;

namespace BudgetApp.Api.Modules.Dashboard.Validators;

public class AnalyseRequestValidator : AbstractValidator<AdvisorController.AnalyseRequest>
{
    private const int MaxGoalsCount = 10;
    private const int MaxGoalLength = 100;

    public AnalyseRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .WithMessage("Provider is required")
            .Must(AiProviders.IsValid)
            .WithMessage($"Provider must be one of: {string.Join(", ", AiProviders.All)}");

        RuleFor(x => x.Goals)
            .Must(g => g == null || g.Count <= MaxGoalsCount)
            .WithMessage($"Maximum {MaxGoalsCount} goals allowed");

        RuleForEach(x => x.Goals)
            .MaximumLength(MaxGoalLength)
            .WithMessage($"Each goal must not exceed {MaxGoalLength} characters");

        When(x => x.Provider == AiProviders.Claude, () =>
        {
            RuleFor(x => x.ApiKey)
                .NotEmpty()
                .WithMessage("API key is required for Claude provider");
        });
    }
}
