using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Auth;
using OrderManagement.Api.Domain;
using OrderManagement.Api.Infrastructure;
using OrderManagement.Api.Models;

namespace OrderManagement.Api.Services;

public class OrderService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateOrderRequest> _createValidator;
    private readonly IValidator<AddLineItemRequest> _lineItemValidator;

    public OrderService(
        AppDbContext db,
        TimeProvider timeProvider,
        IValidator<CreateOrderRequest> createValidator,
        IValidator<AddLineItemRequest> lineItemValidator)
    {
        _db = db;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _lineItemValidator = lineItemValidator;
    }

    public async Task<Order> CreateDraftOrder(Actor actor, CreateOrderRequest request)
    {
        actor.RequirePermission(PermissionConstants.OrdersCreate);

        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new DomainValidationException(validation.Errors.Select(e => e.ErrorMessage));

        var customer = await _db.Customers.FindAsync(request.CustomerId)
            ?? throw new NotFoundException($"Customer {request.CustomerId} not found.");

        var order = Order.Create(customer.Id, actor.Id, _timeProvider.GetUtcNow().UtcDateTime);
        _db.Orders.Add(order);

        foreach (var item in request.LineItems)
        {
            var product = await _db.Products.FindAsync(item.ProductId)
                ?? throw new NotFoundException($"Product {item.ProductId} not found.");

            var lineItem = LineItem.Create(order.Id, product.Id, product.ProductName, item.Quantity, product.UnitPrice);
            order.AddLineItem(lineItem);
        }

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> AddLineItem(Actor actor, Guid orderId, AddLineItemRequest request)
    {
        actor.RequirePermission(PermissionConstants.OrdersCreate);

        var validation = await _lineItemValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new DomainValidationException(validation.Errors.Select(e => e.ErrorMessage));

        var order = await _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        var product = await _db.Products.FindAsync(request.ProductId)
            ?? throw new NotFoundException($"Product {request.ProductId} not found.");

        var lineItem = LineItem.Create(order.Id, product.Id, product.ProductName, request.Quantity, product.UnitPrice);
        order.AddLineItem(lineItem);

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> RemoveLineItem(Actor actor, Guid orderId, Guid lineItemId)
    {
        actor.RequirePermission(PermissionConstants.OrdersCreate);

        var order = await _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        order.RemoveLineItem(lineItemId);

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> SubmitOrder(Actor actor, Guid orderId)
    {
        actor.RequirePermission(PermissionConstants.OrdersSubmit);

        var order = await _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        // Reserve stock for each line item
        var productIds = order.LineItems.Select(li => li.ProductId).ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

        foreach (var lineItem in order.LineItems)
        {
            var product = products.First(p => p.Id == lineItem.ProductId);
            product.ReserveStock(lineItem.Quantity);
        }

        order.Submit(_timeProvider);

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> ApproveOrder(Actor actor, Guid orderId)
    {
        actor.RequirePermission(PermissionConstants.OrdersApprove);

        var order = await _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        order.Approve(_timeProvider);

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> ShipOrder(Actor actor, Guid orderId)
    {
        actor.RequirePermission(PermissionConstants.OrdersShip);

        var order = await _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        order.Ship(_timeProvider);

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> DeliverOrder(Actor actor, Guid orderId)
    {
        actor.RequirePermission(PermissionConstants.OrdersDeliver);

        var order = await _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        order.Deliver(_timeProvider);

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> CancelOrder(Actor actor, Guid orderId)
    {
        actor.RequirePermission(PermissionConstants.OrdersCancel);

        var order = await _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        // Ownership check: must be creator or have orders:read-all
        if (order.CreatedByActorId != actor.Id && !actor.HasPermission(PermissionConstants.OrdersReadAll))
            throw new ForbiddenException(
                $"Actor '{actor.Id}' is not authorized to cancel order {orderId}. Only the order creator or an admin can cancel orders.");

        var releaseStock = order.RequiresStockRelease;

        order.Cancel(_timeProvider);

        if (releaseStock)
        {
            var productIds = order.LineItems.Select(li => li.ProductId).ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            foreach (var lineItem in order.LineItems)
            {
                var product = products.First(p => p.Id == lineItem.ProductId);
                product.ReleaseStock(lineItem.Quantity);
            }
        }

        await _db.SaveChangesAsync();
        return order;
    }

    public async Task<Order> GetOrderById(Actor actor, Guid orderId)
    {
        actor.RequirePermission(PermissionConstants.OrdersRead);

        var order = await _db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException($"Order {orderId} not found.");

        return order;
    }

    public async Task<List<Order>> GetOrdersByCustomer(Actor actor, Guid customerId)
    {
        actor.RequirePermission(PermissionConstants.OrdersReadAll);

        var customerExists = await _db.Customers.AnyAsync(c => c.Id == customerId);
        if (!customerExists)
            throw new NotFoundException($"Customer {customerId} not found.");

        return await _db.Orders
            .Include(o => o.LineItems)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<List<Order>> GetOverdueOrders(Actor actor)
    {
        actor.RequirePermission(PermissionConstants.OrdersReadAll);

        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);

        return await _db.Orders
            .Include(o => o.LineItems)
            .Where(o => o.Status == OrderStatus.Submitted && o.SubmittedAt < cutoff)
            .ToListAsync();
    }
}
