using BudgetApp.Api.Modules.Portfolio.Models;
using FluentValidation;

namespace BudgetApp.Api.Modules.Portfolio.Validators;

public class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotEmpty().Must(AssetTypes.IsValid)
            .WithMessage($"Type must be one of: {string.Join(", ", AssetTypes.All)}");
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.PurchasePrice).GreaterThan(0);
        RuleFor(x => x.PurchaseDate).NotEmpty().LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1));
    }
}

public class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).NotEmpty().Must(AssetTypes.IsValid)
            .WithMessage($"Type must be one of: {string.Join(", ", AssetTypes.All)}");
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class AddPriceRequestValidator : AbstractValidator<AddPriceRequest>
{
    public AddPriceRequestValidator()
    {
        RuleFor(x => x.Value).GreaterThan(0);
        RuleFor(x => x.Date).NotEmpty();
    }
}
