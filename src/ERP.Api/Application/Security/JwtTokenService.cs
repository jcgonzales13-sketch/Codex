using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ERP.Api.Application.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Api.Application.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateAccessToken(SessaoAutenticacaoResponse sessao)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, sessao.Usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, sessao.Usuario.Email),
            new Claim("empresa_id", sessao.Usuario.EmpresaId.ToString()),
            new Claim("session_token", sessao.Token),
            new Claim(ClaimTypes.Name, sessao.Usuario.Nome)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: sessao.ExpiresAt.UtcDateTime < DateTime.UtcNow
                ? DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes)
                : sessao.ExpiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public DateTimeOffset GetRefreshTokenExpiration() => DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=')
            + Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
    }

    public string ExtractSessionToken(string accessToken)
    {
        var principal = new JwtSecurityTokenHandler().ValidateToken(accessToken, CreateValidationParameters(), out _);
        var sessionToken = principal.FindFirst("session_token")?.Value;
        if (string.IsNullOrWhiteSpace(sessionToken))
        {
            throw new UnauthorizedAccessException("Token JWT invalido.");
        }

        return sessionToken;
    }

    private TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }
}
