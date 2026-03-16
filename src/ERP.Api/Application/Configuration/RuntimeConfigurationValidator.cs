using ERP.Api.Application.Security;
using ERP.Api.Application.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ERP.Api.Application.Configuration;

internal static class RuntimeConfigurationValidator
{
    internal static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var storage = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        ValidateStorage(storage);
        ValidateJwt(jwt, environment);
        ValidateProductionConstraints(storage, environment);
    }

    private static void ValidateStorage(StorageOptions storage)
    {
        if (string.Equals(storage.Provider, "SqlServer", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(storage.ConnectionString))
        {
            throw new InvalidOperationException("Storage:ConnectionString e obrigatoria quando Storage:Provider=SqlServer.");
        }

        if (string.Equals(storage.Provider, "JsonFile", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(storage.FilePath))
        {
            throw new InvalidOperationException("Storage:FilePath e obrigatoria quando Storage:Provider=JsonFile.");
        }
    }

    private static void ValidateJwt(JwtOptions jwt, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            throw new InvalidOperationException("Jwt:SigningKey e obrigatoria.");
        }

        if (environment.IsProduction() && jwt.SigningKey.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Jwt:SigningKey deve ser configurada com um segredo real em producao.");
        }
    }

    private static void ValidateProductionConstraints(StorageOptions storage, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        if (!string.Equals(storage.Provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Em producao, Storage:Provider deve ser SqlServer.");
        }
    }
}
