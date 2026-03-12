namespace ERP.Api.Application.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "CodexERP";
    public string Audience { get; init; } = "CodexERP.Clients";
    public string SigningKey { get; init; } = "CHANGE_ME_DEVELOPMENT_ONLY_SIGNING_KEY_123456789";
    public int AccessTokenExpirationMinutes { get; init; } = 60;
    public int RefreshTokenExpirationDays { get; init; } = 30;
}
