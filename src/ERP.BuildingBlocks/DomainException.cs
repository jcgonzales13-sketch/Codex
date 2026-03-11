namespace ERP.BuildingBlocks;

public sealed class DomainException(string message) : Exception(message);
