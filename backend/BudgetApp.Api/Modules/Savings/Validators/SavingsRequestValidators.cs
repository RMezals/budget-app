using FluentValidation;

namespace BudgetApp.Api.Modules.Savings.Validators;

public class CreateGoalRequestValidator : AbstractValidator<CreateGoalRequest>
{
    public CreateGoalRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0);

        RuleFor(x => x.Deadline)
            .NotEmpty()
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

public class AddContributionRequestValidator : AbstractValidator<AddContributionRequest>
{
    public AddContributionRequestValidator()
    {
        RuleFor(x => x.Amount)
            .NotEqual(0m);

        RuleFor(x => x.Date)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1));

        RuleFor(x => x.Reason)
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Note)
            .MaximumLength(500);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => x.Amount < 0)
            .WithMessage("Reason is required for withdrawals.");
    }
}
