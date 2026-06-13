namespace OrderManagement.Api.v2026_11_12.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.AntiCorruptionLayer;
using OrderManagement.Api.v2026_11_12.Models;
using OrderManagement.Domain;
using Trellis;
using Trellis.EntityFrameworkCore;

/// <summary>
/// Order endpoints.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    /// <summary>Constructor.</summary>
    public OrdersController(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    /// <summary>Create a draft order.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.OrdersCreate) is { } forbidden)
            return forbidden;

        var lines = request.LineItems?.Select(li => (li.ProductId, li.Quantity)).ToList() ?? [];
        var lineValidation = Order.ValidateLines(lines);
        if (lineValidation.IsFailure)
            return ApiSupport.Problem(this, lineValidation.Error!);

        if (!await _db.Customers.AnyAsync(customer => customer.Id == request.CustomerId, cancellationToken))
            return ApiSupport.Problem(this, new Error.NotFound(ResourceRef.For("Customer", request.CustomerId)) { Detail = "Customer not found." });

        var products = await LoadProducts(lines.Select(line => line.ProductId), cancellationToken);
        if (products.Count != lines.Select(line => line.ProductId).Distinct().Count())
            return ApiSupport.Problem(this, new Error.NotFound(ResourceRef.For("Product")) { Detail = "Product not found." });

        var result = Order.TryCreate(
            request.CustomerId,
            actor.Id,
            lines.Select(line => (products[line.ProductId], line.Quantity)).ToList(),
            _timeProvider);
        if (result.IsFailure)
            return ApiSupport.Problem(this, result.Error!);

        if (!result.TryGetValue(out var order))
            return ApiSupport.Problem(this, result.Error!);

        _db.Orders.Add(order);
        var save = await _db.SaveChangesResultUnitAsync(cancellationToken);
        if (save.IsFailure)
            return ApiSupport.Problem(this, save.Error!);

        return Created($"/api/orders/{order.Id}?api-version=2026-11-12", OrderResponse.From(order));
    }

    /// <summary>Add a line item.</summary>
    [HttpPost("{id:guid}/line-items")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> AddLineItem(Guid id, OrderLineItemRequest request, CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.OrdersCreate) is { } forbidden)
            return forbidden;

        var order = await FindOrder(id, cancellationToken);
        if (order is null)
            return NotFoundOrder(id);

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        if (product is null)
            return ApiSupport.Problem(this, new Error.NotFound(ResourceRef.For("Product", request.ProductId)) { Detail = "Product not found." });

        var result = order.AddLineItem(product, request.Quantity);
        if (result.IsFailure)
            return ApiSupport.Problem(this, result.Error!);

        var save = await _db.SaveChangesResultUnitAsync(cancellationToken);
        if (save.IsFailure)
            return ApiSupport.Problem(this, save.Error!);
        return OrderResponse.From(order);
    }

    /// <summary>Remove a line item.</summary>
    [HttpDelete("{id:guid}/line-items/{lineItemId:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> RemoveLineItem(Guid id, Guid lineItemId, CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.OrdersCreate) is { } forbidden)
            return forbidden;

        var order = await FindOrder(id, cancellationToken);
        if (order is null)
            return NotFoundOrder(id);

        var result = order.RemoveLineItem(lineItemId);
        if (result.IsFailure)
            return ApiSupport.Problem(this, result.Error!);

        var save = await _db.SaveChangesResultUnitAsync(cancellationToken);
        if (save.IsFailure)
            return ApiSupport.Problem(this, save.Error!);
        return OrderResponse.From(order);
    }

    /// <summary>Submit an order.</summary>
    [HttpPost("{id:guid}/submission")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Submit(Guid id, CancellationToken cancellationToken) =>
        Transition(id, Permissions.OrdersSubmit, (order, products) => order.Submit(products, _timeProvider), cancellationToken);

    /// <summary>Approve an order.</summary>
    [HttpPost("{id:guid}/approval")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Approve(Guid id, CancellationToken cancellationToken) =>
        Transition(id, Permissions.OrdersApprove, (order, _) => order.Approve(_timeProvider), cancellationToken);

    /// <summary>Ship an order.</summary>
    [HttpPost("{id:guid}/shipment")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Ship(Guid id, CancellationToken cancellationToken) =>
        Transition(id, Permissions.OrdersShip, (order, _) => order.Ship(_timeProvider), cancellationToken);

    /// <summary>Deliver an order.</summary>
    [HttpPost("{id:guid}/delivery")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<OrderResponse>> Deliver(Guid id, CancellationToken cancellationToken) =>
        Transition(id, Permissions.OrdersDeliver, (order, _) => order.Deliver(_timeProvider), cancellationToken);

    /// <summary>Cancel an order.</summary>
    [HttpPost("{id:guid}/cancellation")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.OrdersCancel) is { } forbidden)
            return forbidden;

        var order = await FindOrder(id, cancellationToken);
        if (order is null)
            return NotFoundOrder(id);
        if (order.CreatedByActorId != actor.Id && !actor.Has(Permissions.OrdersReadAll))
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Forbidden", detail: "Only the creator or an administrator can cancel this order.");

        var products = await LoadProducts(order.LineItems.Select(line => line.ProductId), cancellationToken);
        var result = order.Cancel(products, _timeProvider);
        if (result.IsFailure)
            return ApiSupport.Problem(this, result.Error!);

        var save = await _db.SaveChangesResultUnitAsync(cancellationToken);
        if (save.IsFailure)
            return ApiSupport.Problem(this, save.Error!);
        return OrderResponse.From(order);
    }

    /// <summary>Get an order by id.</summary>
    [HttpGet("{id:guid}", Name = "Orders_GetById")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.OrdersRead) is { } forbidden)
            return forbidden;

        var order = await FindOrder(id, cancellationToken);
        return order is null ? NotFoundOrder(id) : OrderResponse.From(order);
    }

    /// <summary>List overdue orders.</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> Overdue(CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, Permissions.OrdersReadAll) is { } forbidden)
            return forbidden;

        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-7);
        var orders = await _db.Orders
            .Include(order => order.LineItems)
            .Where(order => order.Status == OrderStatus.Submitted && order.SubmittedAt < cutoff)
            .OrderBy(order => order.SubmittedAt)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderResponse.From).ToList();
    }

    private async Task<ActionResult<OrderResponse>> Transition(
        Guid id,
        string permission,
        Func<Order, IReadOnlyDictionary<Guid, Product>, Result<Order>> transition,
        CancellationToken cancellationToken)
    {
        var actor = ApiSupport.GetActor(Request);
        if (ApiSupport.ForbidUnless(this, actor, permission) is { } forbidden)
            return forbidden;

        var order = await FindOrder(id, cancellationToken);
        if (order is null)
            return NotFoundOrder(id);

        var products = await LoadProducts(order.LineItems.Select(line => line.ProductId), cancellationToken);
        var result = transition(order, products);
        if (result.IsFailure)
            return ApiSupport.Problem(this, result.Error!);

        var save = await _db.SaveChangesResultUnitAsync(cancellationToken);
        if (save.IsFailure)
            return ApiSupport.Problem(this, save.Error!);
        return OrderResponse.From(order);
    }

    private async Task<Order?> FindOrder(Guid id, CancellationToken cancellationToken) =>
        await _db.Orders
            .Include(order => order.LineItems)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    private async Task<Dictionary<Guid, Product>> LoadProducts(IEnumerable<Guid> productIds, CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().ToList();
        return await _db.Products
            .Where(product => ids.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);
    }

    private ActionResult NotFoundOrder(Guid id) =>
        ApiSupport.Problem(this, new Error.NotFound(ResourceRef.For("Order", id)) { Detail = "Order not found." });
}
