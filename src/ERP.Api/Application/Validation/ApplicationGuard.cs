using ERP.BuildingBlocks;

namespace ERP.Api.Application.Validation;

public static class ApplicationGuard
{
    public static void AgainstEmptyGuid(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} e obrigatorio.");
        }
    }

    public static void AgainstNullOrWhiteSpace(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} e obrigatorio.");
        }
    }

    public static void AgainstNegative(decimal value, string fieldName)
    {
        if (value < 0)
        {
            throw new DomainException($"{fieldName} nao pode ser negativo.");
        }
    }

    public static void AgainstZeroOrNegative(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainException($"{fieldName} deve ser maior que zero.");
        }
    }

    public static void AgainstEmptyCollection<T>(IReadOnlyCollection<T> items, string fieldName)
    {
        if (items.Count == 0)
        {
            throw new DomainException($"{fieldName} deve possuir ao menos um item.");
        }
    }
}
