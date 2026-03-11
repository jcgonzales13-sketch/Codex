using ERP.Api.Application.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ERP.Api.Application.Health;

public sealed class StorageHealthCheck(IOptions<StorageOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var provider = options.Value.Provider?.Trim() ?? "InMemory";

        if (string.Equals(provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(HealthCheckResult.Healthy("Storage em memoria ativo."));
        }

        if (!string.Equals(provider, "JsonFile", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Provider de storage nao suportado: {provider}."));
        }

        var filePath = options.Value.FilePath?.Trim();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("FilePath do storage JsonFile nao configurado."));
        }

        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Diretorio do storage JsonFile invalido."));
            }

            Directory.CreateDirectory(directory);
            return Task.FromResult(HealthCheckResult.Healthy($"Storage JsonFile apontando para {fullPath}."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Falha ao validar storage JsonFile.", ex));
        }
    }
}
