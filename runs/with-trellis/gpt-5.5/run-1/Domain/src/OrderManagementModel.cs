namespace OrderManagement.Domain;

using System.Text.RegularExpressions;
using Trellis;

/// <summary>
/// Order lifecycle statuses.
/// </summary>
public enum OrderStatus
{
    /// <summary>Draft order.</summary>
    Draft,
    /// <summary>Submitted order.</summary>
    Submitted,
    /// <summary>Approved order.</summary>
    Approved,
    /// <summary>Shipped order.</summary>
    Shipped,
    /// <summary>Delivered order.</summary>
    Delivered,
    /// <summary>Cancelled order.</summary>
    Cancelled,
}

/// <summary>
/// Customer aggregate.
/// </summary>
public sealed class Customer
{
    private static readonly Regex EmailRegex = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new("^[0-9+() .-]{7,32}$", RegexOptions.Compiled);

    /// <summary>Customer identifier.</summary>
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    /// <summary>First name.</summary>
    public string FirstName { get; private set; } = string.Empty;
    /// <summary>Last name.</summary>
    public string LastName { get; private set; } = string.Empty;
    /// <summary>Email address.</summary>
    public string Email { get; private set; } = string.Empty;
    /// <summary>Optional phone number.</summary>
    public string? PhoneNumber { get; private set; }
    /// <summary>Street.</summary>
    public string Street { get; private set; } = string.Empty;
    /// <summary>City.</summary>
    public string City { get; private set; } = string.Empty;
    /// <summary>State.</summary>
    public string State { get; private set; } = string.Empty;
    /// <summary>Postal code.</summary>
    public string PostalCode { get; private set; } = string.Empty;
    /// <summary>Country.</summary>
    public string Country { get; private set; } = string.Empty;

    private Customer()
    {
    }

    /// <summary>Create a validated customer.</summary>
    public static Result<Customer> TryCreate(
        string? firstName,
        string? lastName,
        string? email,
        string? phoneNumber,
        ShippingAddressInput? address)
    {
        var validation = ValidateRequired(firstName, "firstName", 100)
            .Combine(ValidateRequired(lastName, "lastName", 100))
            .Combine(ValidateEmail(email))
            .Combine(ValidateOptionalPhone(phoneNumber))
            .Combine(ValidateAddress(address));

        return validation.Map((first, last, validEmail, phone, validAddress) => new Customer
        {
            FirstName = first,
            LastName = last,
            Email = validEmail,
            PhoneNumber = phone,
            Street = validAddress.Street,
            City = validAddress.City,
            State = validAddress.State,
            PostalCode = validAddress.PostalCode,
            Country = validAddress.Country,
        });
    }

    private static Result<string> ValidateRequired(string? value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result.Fail<string>(Error.InvalidInput.ForField(field, "required", $"{field} is required."));
        return trimmed.Length > maxLength
            ? Result.Fail<string>(Error.InvalidInput.ForField(field, "too_long", $"{field} is too long."))
            : Result.Ok(trimmed);
    }

    private static Result<string> ValidateEmail(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) || !EmailRegex.IsMatch(trimmed)
            ? Result.Fail<string>(Error.InvalidInput.ForField("email", "invalid", "Email must be valid."))
            : Result.Ok(trimmed);
    }

    private static Result<string?> ValidateOptionalPhone(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed)
            ? Result.Ok<string?>(null)
            : PhoneRegex.IsMatch(trimmed)
                ? Result.Ok<string?>(trimmed)
                : Result.Fail<string?>(Error.InvalidInput.ForField("phoneNumber", "invalid", "Phone number must be valid."));
    }

    private static Result<ShippingAddressInput> ValidateAddress(ShippingAddressInput? address)
    {
        if (address is null)
            return Result.Fail<ShippingAddressInput>(Error.InvalidInput.ForField("shippingAddress", "required", "Shipping address is required."));

        return ValidateRequired(address.Street, "shippingAddress.street", 200)
            .Combine(ValidateRequired(address.City, "shippingAddress.city", 100))
            .Combine(ValidateRequired(address.State, "shippingAddress.state", 100))
            .Combine(ValidateRequired(address.PostalCode, "shippingAddress.postalCode", 32))
            .Combine(ValidateRequired(address.Country, "shippingAddress.country", 100))
            .Map((street, city, state, postalCode, country) => new ShippingAddressInput(street, city, state, postalCode, country));
    }
}

/// <summary>
/// Product aggregate.
/// </summary>
public sealed class Product
{
    private static readonly Regex SkuRegex = new("^[A-Z0-9]{3,20}$", RegexOptions.Compiled);

    /// <summary>Product identifier.</summary>
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    /// <summary>Product name.</summary>
    public string ProductName { get; private set; } = string.Empty;
    /// <summary>Unique SKU.</summary>
    public string Sku { get; private set; } = string.Empty;
    /// <summary>Unit price in USD.</summary>
    public decimal UnitPrice { get; private set; }
    /// <summary>Available stock quantity.</summary>
    public int StockQuantity { get; private set; }

    private Product()
    {
    }

    /// <summary>Create a validated product.</summary>
    public static Result<Product> TryCreate(string? productName, string? sku, decimal unitPrice) =>
        ValidateName(productName)
            .Combine(ValidateSku(sku))
            .Combine(ValidateUnitPrice(unitPrice))
            .Map((name, validSku, price) => new Product
            {
                ProductName = name,
                Sku = validSku,
                UnitPrice = price,
                StockQuantity = 0,
            });

    /// <summary>Add stock.</summary>
    public Result<Product> AddStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Fail<Product>(Error.InvalidInput.ForField("quantity", "positive", "Quantity must be positive."));

        StockQuantity += quantity;
        return Result.Ok(this);
    }

    /// <summary>Reserve stock.</summary>
    public Result<Product> ReserveStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Fail<Product>(Error.InvalidInput.ForField("quantity", "positive", "Quantity must be positive."));
        if (StockQuantity < quantity)
            return Result.Fail<Product>(Error.InvalidInput.ForRule("inventory.insufficient_stock", "Insufficient stock."));

        StockQuantity -= quantity;
        return Result.Ok(this);
    }

    /// <summary>Release stock.</summary>
    public void ReleaseStock(int quantity) => StockQuantity += quantity;

    private static Result<string> ValidateName(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return Result.Fail<string>(Error.InvalidInput.ForField("productName", "required", "Product name is required."));
        return trimmed.Length > 200
            ? Result.Fail<string>(Error.InvalidInput.ForField("productName", "too_long", "Product name is too long."))
            : Result.Ok(trimmed);
    }

    private static Result<string> ValidateSku(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) || !SkuRegex.IsMatch(trimmed)
            ? Result.Fail<string>(Error.InvalidInput.ForField("sku", "invalid", "SKU must be 3-20 uppercase letters or digits."))
            : Result.Ok(trimmed);
    }

    private static Result<decimal> ValidateUnitPrice(decimal value) =>
        value <= 0
            ? Result.Fail<decimal>(Error.InvalidInput.ForField("unitPrice", "positive", "Unit price must be greater than zero."))
            : Result.Ok(value);
}

/// <summary>
/// Order aggregate.
/// </summary>
public sealed class Order
{
    /// <summary>Order identifier.</summary>
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    /// <summary>Customer identifier.</summary>
    public Guid CustomerId { get; private set; }
    /// <summary>Creating actor identifier.</summary>
    public string CreatedByActorId { get; private set; } = string.Empty;
    /// <summary>Current status.</summary>
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>UTC submission timestamp.</summary>
    public DateTime? SubmittedAt { get; private set; }
    /// <summary>UTC shipment timestamp.</summary>
    public DateTime? ShippedAt { get; private set; }
    /// <summary>Line items.</summary>
    public List<OrderLineItem> LineItems { get; private set; } = [];
    /// <summary>Domain events captured for tests/integration.</summary>
    public List<object> DomainEvents { get; } = [];

    private Order()
    {
    }

    /// <summary>Create a draft order.</summary>
    public static Result<Order> TryCreate(Guid customerId, string actorId, IReadOnlyList<(Product Product, int Quantity)> lines, TimeProvider timeProvider)
    {
        if (customerId == Guid.Empty)
            return Result.Fail<Order>(Error.InvalidInput.ForField("customerId", "required", "Customer id is required."));
        if (string.IsNullOrWhiteSpace(actorId))
            return Result.Fail<Order>(Error.InvalidInput.ForField("actorId", "required", "Actor id is required."));
        var lineValidation = ValidateLines(lines.Select(l => (l.Product.Id, l.Quantity)).ToList());
        if (lineValidation.IsFailure)
            return Result.Fail<Order>(lineValidation.Error!);

        var order = new Order
        {
            CustomerId = customerId,
            CreatedByActorId = actorId,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            Status = OrderStatus.Draft,
        };

        foreach (var (product, quantity) in lines)
            order.LineItems.Add(OrderLineItem.Create(product, quantity));

        return Result.Ok(order);
    }

    /// <summary>Add a line item to a draft order.</summary>
    public Result<Order> AddLineItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.not_draft", "Order must be in Draft status."));
        var quantityResult = ValidateQuantity(quantity, "quantity");
        if (quantityResult.IsFailure)
            return Result.Fail<Order>(quantityResult.Error!);
        if (LineItems.Any(li => li.ProductId == product.Id))
            return Result.Fail<Order>(Error.InvalidInput.ForField("productId", "duplicate", "Product already exists in the order."));

        LineItems.Add(OrderLineItem.Create(product, quantity));
        return Result.Ok(this);
    }

    /// <summary>Remove a line item from a draft order.</summary>
    public Result<Order> RemoveLineItem(Guid lineItemId)
    {
        if (Status != OrderStatus.Draft)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.not_draft", "Order must be in Draft status."));
        if (LineItems.Count <= 1)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.last_line_item", "Cannot remove the last line item."));
        var item = LineItems.FirstOrDefault(li => li.Id == lineItemId);
        if (item is null)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For("OrderLineItem", lineItemId)) { Detail = "Line item not found." });

        LineItems.Remove(item);
        return Result.Ok(this);
    }

    /// <summary>Submit and reserve stock.</summary>
    public Result<Order> Submit(IReadOnlyDictionary<Guid, Product> products, TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Draft)
            return InvalidTransition("submit");
        if (LineItems.Count == 0)
            return Result.Fail<Order>(Error.InvalidInput.ForRule("order.line_items_required", "Order must have at least one line item."));

        foreach (var line in LineItems)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                return Result.Fail<Order>(new Error.NotFound(ResourceRef.For("Product", line.ProductId)) { Detail = "Product not found." });
            if (product.StockQuantity < line.Quantity)
                return Result.Fail<Order>(Error.InvalidInput.ForRule("inventory.insufficient_stock", "Insufficient stock."));
        }

        foreach (var line in LineItems)
        {
            var reserve = products[line.ProductId].ReserveStock(line.Quantity);
            if (reserve.IsFailure)
                return Result.Fail<Order>(reserve.Error!);
        }

        SubmittedAt = timeProvider.GetUtcNow().UtcDateTime;
        Status = OrderStatus.Submitted;
        DomainEvents.Add(new OrderSubmittedEvent(Id, CustomerId, Total, SubmittedAt.Value));
        return Result.Ok(this);
    }

    /// <summary>Approve the order.</summary>
    public Result<Order> Approve(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Submitted)
            return InvalidTransition("approve");
        Status = OrderStatus.Approved;
        DomainEvents.Add(new OrderApprovedEvent(Id, timeProvider.GetUtcNow().UtcDateTime));
        return Result.Ok(this);
    }

    /// <summary>Ship the order.</summary>
    public Result<Order> Ship(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Approved)
            return InvalidTransition("ship");
        ShippedAt = timeProvider.GetUtcNow().UtcDateTime;
        Status = OrderStatus.Shipped;
        DomainEvents.Add(new OrderShippedEvent(Id, CustomerId, ShippedAt.Value));
        return Result.Ok(this);
    }

    /// <summary>Deliver the order.</summary>
    public Result<Order> Deliver(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Shipped)
            return InvalidTransition("deliver");
        Status = OrderStatus.Delivered;
        DomainEvents.Add(new OrderDeliveredEvent(Id, timeProvider.GetUtcNow().UtcDateTime));
        return Result.Ok(this);
    }

    /// <summary>Cancel and release stock when necessary.</summary>
    public Result<Order> Cancel(IReadOnlyDictionary<Guid, Product> products, TimeProvider timeProvider)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
            return InvalidTransition("cancel");

        var from = Status;
        if (Status is OrderStatus.Submitted or OrderStatus.Approved)
        {
            foreach (var line in LineItems)
            {
                if (!products.TryGetValue(line.ProductId, out var product))
                    return Result.Fail<Order>(new Error.NotFound(ResourceRef.For("Product", line.ProductId)) { Detail = "Product not found." });
                product.ReleaseStock(line.Quantity);
            }
        }

        Status = OrderStatus.Cancelled;
        DomainEvents.Add(new OrderCancelledEvent(Id, from.ToString(), timeProvider.GetUtcNow().UtcDateTime));
        return Result.Ok(this);
    }

    /// <summary>Order total.</summary>
    public decimal Total => LineItems.Sum(item => item.UnitPrice * item.Quantity);

    /// <summary>Validate order line input.</summary>
    public static Result<Unit> ValidateLines(IReadOnlyList<(Guid ProductId, int Quantity)> lines)
    {
        if (lines.Count == 0)
            return Result.Fail(Error.InvalidInput.ForField("lineItems", "required", "At least one line item is required."));
        if (lines.Select(l => l.ProductId).Distinct().Count() != lines.Count)
            return Result.Fail(Error.InvalidInput.ForField("lineItems", "duplicate_product", "The same product cannot appear more than once."));

        foreach (var line in lines)
        {
            var quantity = ValidateQuantity(line.Quantity, "quantity");
            if (quantity.IsFailure)
                return Result.Fail(quantity.Error!);
        }

        return Result.Ok();
    }

    private static Result<int> ValidateQuantity(int quantity, string field) =>
        quantity is < 1 or > 999
            ? Result.Fail<int>(Error.InvalidInput.ForField(field, "out_of_range", "Quantity must be between 1 and 999."))
            : Result.Ok(quantity);

    private Result<Order> InvalidTransition(string trigger) =>
        Result.Fail<Order>(Error.InvalidInput.ForRule("state.machine.invalid.transition", $"Cannot {trigger} an order from {Status} status."));
}

/// <summary>
/// Order line item entity.
/// </summary>
public sealed class OrderLineItem
{
    /// <summary>Line item identifier.</summary>
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    /// <summary>Product identifier.</summary>
    public Guid ProductId { get; private set; }
    /// <summary>Product name snapshot.</summary>
    public string ProductName { get; private set; } = string.Empty;
    /// <summary>Quantity.</summary>
    public int Quantity { get; private set; }
    /// <summary>Unit price snapshot.</summary>
    public decimal UnitPrice { get; private set; }

    private OrderLineItem()
    {
    }

    internal static OrderLineItem Create(Product product, int quantity) => new()
    {
        ProductId = product.Id,
        ProductName = product.ProductName,
        UnitPrice = product.UnitPrice,
        Quantity = quantity,
    };
}

/// <summary>Shipping address input value.</summary>
public sealed record ShippingAddressInput(string Street, string City, string State, string PostalCode, string Country);

/// <summary>Order submitted event.</summary>
public sealed record OrderSubmittedEvent(Guid OrderId, Guid CustomerId, decimal OrderTotal, DateTime SubmittedAt);
/// <summary>Order approved event.</summary>
public sealed record OrderApprovedEvent(Guid OrderId, DateTime ApprovedAt);
/// <summary>Order shipped event.</summary>
public sealed record OrderShippedEvent(Guid OrderId, Guid CustomerId, DateTime ShippedAt);
/// <summary>Order delivered event.</summary>
public sealed record OrderDeliveredEvent(Guid OrderId, DateTime DeliveredAt);
/// <summary>Order cancelled event.</summary>
public sealed record OrderCancelledEvent(Guid OrderId, string CancelledFromStatus, DateTime CancelledAt);

/// <summary>
/// Overdue order predicate.
/// </summary>
public static class OverdueOrderSpecification
{
    /// <summary>Returns true when an order has been submitted for more than seven days without approval.</summary>
    public static bool IsSatisfiedBy(Order order, DateTime asOfUtc) =>
        order.Status == OrderStatus.Submitted &&
        order.SubmittedAt is { } submittedAt &&
        submittedAt < asOfUtc.AddDays(-7);
}
