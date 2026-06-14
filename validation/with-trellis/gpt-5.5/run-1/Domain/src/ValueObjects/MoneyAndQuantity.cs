namespace OrderManagement.Domain;

/// <summary>USD unit price.</summary>
[Range(0.01, 1_000_000)]
public sealed partial class UnitPrice : RequiredDecimal<UnitPrice>;

/// <summary>Available stock quantity.</summary>
[AllowZero]
[Range(0, int.MaxValue)]
public sealed partial class StockQuantity : RequiredInt<StockQuantity>;

/// <summary>Positive stock adjustment quantity.</summary>
[Range(1, int.MaxValue)]
public sealed partial class StockAdjustmentQuantity : RequiredInt<StockAdjustmentQuantity>;

/// <summary>Order line item quantity.</summary>
[Range(1, 999)]
public sealed partial class OrderQuantity : RequiredInt<OrderQuantity>;
