namespace OrderManagement.Domain;

/// <summary>Quantity of a product in an order line item (1–999).</summary>
[Range(1, 999)]
public partial class Quantity : RequiredInt<Quantity> { }
