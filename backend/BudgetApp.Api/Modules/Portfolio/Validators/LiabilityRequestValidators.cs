using FluentValidation;

namespace BudgetApp.Api.Modules.Portfolio.Validators;

public static class LiabilityTypes
{
    public static readonly IReadOnlyList<string> All =
    [
        "Mortgage", "CarLoan", "StudentLoan", "CreditCard", "PersonalLoan", "Other"
    ];

    public static bool IsValid(string type) =>
        All.Contains(type, StringComparer.OrdinalIgnoreCase);
}

public class CreateLiabilityRequestValidator : AbstractValidator<CreateLiabilityRequest>
{
    public CreateLiabilityRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotEmpty().Must(LiabilityTypes.IsValid)
            .WithMessage($"Type must be one of: {string.Join(", ", LiabilityTypes.All)}");
        RuleFor(x => x.InitialAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Date).NotEmpty();
    }
}

public class UpdateLiabilityRequestValidator : AbstractValidator<UpdateLiabilityRequest>
{
    public UpdateLiabilityRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotEmpty().Must(LiabilityTypes.IsValid)
            .WithMessage($"Type must be one of: {string.Join(", ", LiabilityTypes.All)}");
    }
}

public class AddAmountRequestValidator : AbstractValidator<AddAmountRequest>
{
    public AddAmountRequestValidator()
    {
        RuleFor(x => x.Value).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Date).NotEmpty();
    }
}
