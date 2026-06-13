using System.Text.RegularExpressions;

namespace OrderManagement.Api;

public enum OrderStatus
{
    Draft,
    Submitted,
    Approved,
    Shipped,
    Delivered,
    Cancelled
}

public sealed class DomainException(string message) : Exception(message);

public sealed class ShippingAddress
{
    public string Street { get; private set; } = "";
    public string City { get; private set; } = "";
    public string State { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public string Country { get; private set; } = "";

    private ShippingAddress() { }

    public ShippingAddress(string street, string city, string state, string postalCode, string country)
    {
        Street = Require(street, "Street");
        City = Require(city, "City");
        State = Require(state, "State");
        PostalCode = Require(postalCode, "PostalCode");
        Country = Require(country, "Country");
    }

    private static string Require(string value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException($"{field} is required.") : value.Trim();
}

public sealed class Customer
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^\+?[0-9][0-9\s().-]{6,24}$", RegexOptions.Compiled);

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string? PhoneNumber { get; private set; }
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    private Customer() { }

    public Customer(string firstName, string lastName, string email, string? phoneNumber, ShippingAddress shippingAddress)
    {
        Id = Guid.NewGuid();
        FirstName = RequireLength(firstName, "FirstName", 100);
        LastName = RequireLength(lastName, "LastName", 100);
        Email = NormalizeEmail(email);
        PhoneNumber = NormalizePhone(phoneNumber);
        ShippingAddress = shippingAddress;
    }

    private static string RequireLength(string value, string field, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{field} is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > max) throw new DomainException($"{field} must be {max} characters or fewer.");
        return trimmed;
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email.Trim()))
            throw new DomainException("Email must be a valid email address.");
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizePhone(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
        var trimmed = phoneNumber.Trim();
        if (!PhoneRegex.IsMatch(trimmed)) throw new DomainException("PhoneNumber must be a valid phone number.");
        return trimmed;
    }
}

public sealed class Product
{
    private static readonly Regex SkuRegex = new(@"^[A-Z0-9]{3,20}$", RegexOptions.Compiled);

    public Guid Id { get; private set; }
    public string ProductName { get; private set; } = "";
    public string Sku { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    private Product() { }

    public Product(string productName, string sku, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        ProductName = RequireName(productName);
        Sku = RequireSku(sku);
        if (unitPrice <= 0) throw new DomainException("UnitPrice must be greater than zero.");
        UnitPrice = unitPrice;
        StockQuantity = 0;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        StockQuantity += quantity;
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        if (StockQuantity - quantity < 0) throw new DomainException($"Insufficient stock for SKU {Sku}.");
        StockQuantity -= quantity;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");
        StockQuantity += quantity;
    }

    private static string RequireName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("ProductName is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > 200) throw new DomainException("ProductName must be 200 characters or fewer.");
        return trimmed;
    }

    private static string RequireSku(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !SkuRegex.IsMatch(value.Trim()))
            throw new DomainException("SKU must be 3-20 uppercase letters or digits.");
        return value.Trim();
    }
}

public sealed class OrderLineItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = "";
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private OrderLineItem() { }

    public OrderLineItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId == Guid.Empty ? throw new DomainException("ProductId is required.") : productId;
        ProductName = string.IsNullOrWhiteSpace(productName) ? throw new DomainException("ProductName is required.") : productName.Trim();
        Quantity = ValidateQuantity(quantity);
        UnitPrice = unitPrice > 0 ? unitPrice : throw new DomainException("UnitPrice must be greater than zero.");
    }

    public static int ValidateQuantity(int quantity) =>
        quantity is < 1 or > 999 ? throw new DomainException("Quantity must be between 1 and 999.") : quantity;
}

public sealed class Order
{
    private readonly List<OrderLineItem> _lineItems = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CreatedByActorId { get; private set; } = "";
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public IReadOnlyCollection<OrderLineItem> LineItems => _lineItems;
    public decimal Total => _lineItems.Sum(i => i.UnitPrice * i.Quantity);

    private Order() { }

    public Order(Guid customerId, string createdByActorId, IEnumerable<OrderLineItem> lineItems, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId == Guid.Empty ? throw new DomainException("CustomerId is required.") : customerId;
        CreatedByActorId = string.IsNullOrWhiteSpace(createdByActorId) ? throw new DomainException("CreatedByActorId is required.") : createdByActorId;
        Status = OrderStatus.Draft;
        CreatedAt = createdAt;

        foreach (var item in lineItems) AddLineItem(item);
        if (_lineItems.Count == 0) throw new DomainException("An order must have at least one line item.");
    }

    public void AddLineItem(OrderLineItem item)
    {
        EnsureDraft();
        if (_lineItems.Any(i => i.ProductId == item.ProductId))
            throw new DomainException("The same product cannot appear in multiple line items.");
        _lineItems.Add(item);
    }

    public void RemoveLineItem(Guid lineItemId)
    {
        EnsureDraft();
        if (_lineItems.Count <= 1) throw new DomainException("Cannot remove the last line item from an order.");
        var item = _lineItems.SingleOrDefault(i => i.Id == lineItemId) ?? throw new KeyNotFoundException("Line item not found.");
        _lineItems.Remove(item);
    }

    public void Submit(IEnumerable<Product> products, DateTimeOffset now)
    {
        if (Status != OrderStatus.Draft) throw new DomainException($"Cannot submit an order from {Status} status.");
        if (_lineItems.Count == 0) throw new DomainException("An order must have at least one line item.");
        var byId = products.ToDictionary(p => p.Id);
        foreach (var item in _lineItems)
        {
            if (!byId.TryGetValue(item.ProductId, out var product)) throw new DomainException("Product not found.");
            product.ReserveStock(item.Quantity);
        }
        Status = OrderStatus.Submitted;
        SubmittedAt = now;
    }

    public void Approve(DateTimeOffset now)
    {
        if (Status != OrderStatus.Submitted) throw new DomainException($"Cannot approve an order from {Status} status.");
        Status = OrderStatus.Approved;
    }

    public void Ship(DateTimeOffset now)
    {
        if (Status != OrderStatus.Approved) throw new DomainException($"Cannot ship an order from {Status} status.");
        Status = OrderStatus.Shipped;
        ShippedAt = now;
    }

    public void Deliver(DateTimeOffset now)
    {
        if (Status != OrderStatus.Shipped) throw new DomainException($"Cannot deliver an order from {Status} status.");
        Status = OrderStatus.Delivered;
    }

    public void Cancel(IEnumerable<Product> products, DateTimeOffset now)
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new DomainException($"Cannot cancel an order from {Status} status.");

        if (Status is OrderStatus.Submitted or OrderStatus.Approved)
        {
            var byId = products.ToDictionary(p => p.Id);
            foreach (var item in _lineItems)
            {
                if (byId.TryGetValue(item.ProductId, out var product)) product.ReleaseStock(item.Quantity);
            }
        }

        Status = OrderStatus.Cancelled;
    }

    public bool IsOverdue(DateTimeOffset now) =>
        Status == OrderStatus.Submitted && SubmittedAt is not null && SubmittedAt.Value < now.AddDays(-7);

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft) throw new DomainException("Order must be in Draft status.");
    }
}
