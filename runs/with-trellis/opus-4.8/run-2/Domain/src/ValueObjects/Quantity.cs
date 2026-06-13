namespace OrderManagement.Domain;

/// <summary>Quantity of a product in a single order line item. 1–999.</summary>
[Range(1, 999)]
public sealed partial class Quantity : RequiredInt<Quantity>;

/// <summary>A positive quantity of stock to add to a product.</summary>
[Range(1, int.MaxValue)]
public sealed partial class StockAddition : RequiredInt<StockAddition>;
