using BudgetApp.Api.Modules.Auth.Models;
using FluentValidation;

namespace BudgetApp.Api.Modules.Auth;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    private static readonly string[] SupportedCurrencies = ["USD", "EUR", "GBP", "JPY", "CAD", "AUD"];

    public UpdateProfileRequestValidator()
    {
        When(x => x.Email is not null, () =>
            RuleFor(x => x.Email!)
                .EmailAddress()
                .WithMessage("Invalid email address."));

        When(x => x.Currency is not null, () =>
            RuleFor(x => x.Currency!)
                .Must(c => SupportedCurrencies.Contains(c))
                .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}."));

        When(x => x.DisplayName is not null, () =>
            RuleFor(x => x.DisplayName!)
                .NotEmpty()
                .WithMessage("Display name cannot be empty.")
                .MaximumLength(100)
                .WithMessage("Display name cannot exceed 100 characters."));
    }
}
