using FluentValidation;
using OrderManagement.Api.Models;

namespace OrderManagement.Api.Models;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();

        When(x => x.PhoneNumber != null, () =>
        {
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[\d\s\-\(\)\.]+$")
                .WithMessage("PhoneNumber is not a valid phone number format.");
        });

        RuleFor(x => x.ShippingAddress).NotNull()
            .WithMessage("ShippingAddress is required.");

        When(x => x.ShippingAddress != null, () =>
        {
            RuleFor(x => x.ShippingAddress.Street).NotEmpty();
            RuleFor(x => x.ShippingAddress.City).NotEmpty();
            RuleFor(x => x.ShippingAddress.State).NotEmpty();
            RuleFor(x => x.ShippingAddress.PostalCode).NotEmpty();
            RuleFor(x => x.ShippingAddress.Country).NotEmpty();
        });
    }
}

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty()
            .Length(3, 20)
            .Matches(@"^[A-Z0-9]+$")
            .WithMessage("SKU must be 3-20 characters of uppercase letters and digits only.");
        RuleFor(x => x.UnitPrice).GreaterThan(0)
            .WithMessage("UnitPrice must be greater than zero.");
    }
}

public class AddStockRequestValidator : AbstractValidator<AddStockRequest>
{
    public AddStockRequestValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0)
            .WithMessage("Quantity must be positive.");
    }
}

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.LineItems).NotEmpty()
            .WithMessage("At least one line item is required.");

        When(x => x.LineItems != null && x.LineItems.Count > 0, () =>
        {
            RuleForEach(x => x.LineItems).ChildRules(item =>
            {
                item.RuleFor(li => li.ProductId).NotEmpty();
                item.RuleFor(li => li.Quantity).InclusiveBetween(1, 999);
            });

            RuleFor(x => x.LineItems)
                .Must(items => items.Select(i => i.ProductId).Distinct().Count() == items.Count)
                .WithMessage("Duplicate productIds are not allowed in a single order.");
        });
    }
}

public class AddLineItemRequestValidator : AbstractValidator<AddLineItemRequest>
{
    public AddLineItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 999);
    }
}
