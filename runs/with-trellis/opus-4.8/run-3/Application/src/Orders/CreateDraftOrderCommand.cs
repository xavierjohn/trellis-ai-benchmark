namespace OrderManagement.Application.Orders;

using Mediator;
using OrderManagement.Domain;
using Trellis.Authorization;

/// <summary>Creates a draft order for a customer with one or more line items.</summary>
public sealed record CreateDraftOrderCommand(
    CustomerId CustomerId,
    IReadOnlyList<OrderLineInput> Lines) : ICommand<Result<Order>>, IAuthorize
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredPermissions { get; } = [Permissions.OrdersCreate];
}

/// <summary>Handler for <see cref="CreateDraftOrderCommand"/>.</summary>
public sealed class CreateDraftOrderCommandHandler : ICommandHandler<CreateDraftOrderCommand, Result<Order>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IActorProvider _actorProvider;

    /// <summary>Creates the handler.</summary>
    public CreateDraftOrderCommandHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IActorProvider actorProvider)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _actorProvider = actorProvider;
    }

    /// <inheritdoc />
    public async ValueTask<Result<Order>> Handle(CreateDraftOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
            return Result.Fail<Order>(
                Error.InvalidInput.ForField("lineItems", "required", "An order must have at least one line item."));

        var actor = (await _actorProvider.GetCurrentActorAsync(cancellationToken))
            .GetValueOrThrow("Actor must be present; the IAuthorize pipeline guarantees this.");

        var customer = await _customerRepository.FindByIdAsync(command.CustomerId, cancellationToken);
        if (!customer.HasValue)
            return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Customer>(command.CustomerId))
            {
                Detail = $"Customer {command.CustomerId.Value} not found.",
            });

        var distinctIds = command.Lines.Select(l => l.ProductId).Distinct().ToArray();
        var products = (await _productRepository.FindManyByIdsAsync(distinctIds, cancellationToken))
            .ToDictionary(p => p.Id);

        var drafts = new List<DraftLineItem>(command.Lines.Count);
        foreach (var line in command.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                return Result.Fail<Order>(new Error.NotFound(ResourceRef.For<Product>(line.ProductId))
                {
                    Detail = $"Product {line.ProductId.Value} not found.",
                });

            drafts.Add(new DraftLineItem(product.Id, product.Name, line.Quantity, product.UnitPrice));
        }

        return Order.CreateDraft(command.CustomerId, actor.Id.Value, drafts)
            .Tap(_orderRepository.Add);
    }
}
