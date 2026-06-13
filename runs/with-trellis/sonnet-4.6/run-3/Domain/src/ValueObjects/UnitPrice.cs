namespace OrderManagement.Domain;

public partial class UnitPrice : RequiredDecimal<UnitPrice>
{
    static partial void ValidateAdditional(decimal value, string fieldName, ref string? errorMessage)
    {
        if (value <= 0)
            errorMessage = "Unit price must be greater than zero.";
    }
}
