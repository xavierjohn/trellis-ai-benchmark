using Domain.Common;
using Domain.Products;

namespace Domain.Orders;

public class Order
{
    private readonly List<LineItem> _lineItems = new();

    public Guid OrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CreatedByActorId { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public IReadOnlyList<LineItem> LineItems => _lineItems.AsReadOnly();

    private Order()
    {
    }

    public static Result<Order> Create(Guid customerId, string createdByActorId, TimeProvider timeProvider)
    {
        if (customerId == Guid.Empty)
        {
            return Result<Order>.Failure("CustomerId is required.");
        }

        if (string.IsNullOrWhiteSpace(createdByActorId))
        {
            return Result<Order>.Failure("CreatedByActorId is required.");
        }

        return Result<Order>.Success(new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerId = customerId,
            CreatedByActorId = createdByActorId,
            Status = OrderStatus.Draft,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        });
    }

    public Result AddLineItem(Guid productId, string productName, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
        {
            return Result.Failure("Can only add line items to Draft orders.", "unprocessable");
        }

        if (quantity < 1 || quantity > 999)
        {
            return Result.Failure("Quantity must be between 1 and 999.", "validation");
        }

        if (unitPrice <= 0)
        {
            return Result.Failure("UnitPrice must be greater than 0.", "validation");
        }

        var existing = _lineItems.FirstOrDefault(li => li.ProductId == productId);
        if (existing != null)
        {
            return Result.Failure("Product already exists in this order. Update the quantity instead.", "unprocessable");
        }

        _lineItems.Add(LineItem.Create(productId, productName, quantity, unitPrice));
        return Result.Success();
    }

    public Result RemoveLineItem(Guid lineItemId)
    {
        if (Status != OrderStatus.Draft)
        {
            return Result.Failure("Can only remove line items from Draft orders.", "unprocessable");
        }

        var item = _lineItems.FirstOrDefault(li => li.LineItemId == lineItemId);
        if (item == null)
        {
            return Result.NotFound("LineItem not found.");
        }

        if (_lineItems.Count == 1)
        {
            return Result.Failure("Cannot remove the last line item from an order.", "unprocessable");
        }

        _lineItems.Remove(item);
        return Result.Success();
    }

    public Result Submit(IEnumerable<Product> products, TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Draft)
        {
            return Result.Failure($"Cannot submit an order in {Status} status.", "unprocessable");
        }

        if (!_lineItems.Any())
        {
            return Result.Failure("Order must have at least one line item.", "unprocessable");
        }

        var productList = products.ToList();
        var reserved = new List<(Product Product, int Quantity)>();

        foreach (var item in _lineItems)
        {
            var product = productList.FirstOrDefault(p => p.ProductId == item.ProductId);
            if (product == null)
            {
                ReleaseReservedStock(reserved);
                return Result.NotFound($"Product {item.ProductId} not found.");
            }

            var reserveResult = product.ReserveStock(item.Quantity);
            if (!reserveResult.IsSuccess)
            {
                ReleaseReservedStock(reserved);
                return reserveResult;
            }

            reserved.Add((product, item.Quantity));
        }

        Status = OrderStatus.Submitted;
        SubmittedAt = timeProvider.GetUtcNow().UtcDateTime;
        return Result.Success();
    }

    public Result Approve()
    {
        if (Status != OrderStatus.Submitted)
        {
            return Result.Failure($"Cannot approve an order in {Status} status.", "unprocessable");
        }

        Status = OrderStatus.Approved;
        return Result.Success();
    }

    public Result Ship(TimeProvider timeProvider)
    {
        if (Status != OrderStatus.Approved)
        {
            return Result.Failure($"Cannot ship an order in {Status} status.", "unprocessable");
        }

        Status = OrderStatus.Shipped;
        ShippedAt = timeProvider.GetUtcNow().UtcDateTime;
        return Result.Success();
    }

    public Result Deliver()
    {
        if (Status != OrderStatus.Shipped)
        {
            return Result.Failure($"Cannot deliver an order in {Status} status.", "unprocessable");
        }

        Status = OrderStatus.Delivered;
        return Result.Success();
    }

    public Result Cancel(IEnumerable<Product> products)
    {
        if (Status is OrderStatus.Delivered or OrderStatus.Cancelled or OrderStatus.Shipped)
        {
            return Result.Failure($"Cannot cancel an order in {Status} status.", "unprocessable");
        }

        if (Status is OrderStatus.Submitted or OrderStatus.Approved)
        {
            var productList = products.ToList();
            foreach (var item in _lineItems)
            {
                var product = productList.FirstOrDefault(p => p.ProductId == item.ProductId);
                product?.ReleaseStock(item.Quantity);
            }
        }

        Status = OrderStatus.Cancelled;
        return Result.Success();
    }

    private static void ReleaseReservedStock(IEnumerable<(Product Product, int Quantity)> reserved)
    {
        foreach (var entry in reserved)
        {
            entry.Product.ReleaseStock(entry.Quantity);
        }
    }
}
