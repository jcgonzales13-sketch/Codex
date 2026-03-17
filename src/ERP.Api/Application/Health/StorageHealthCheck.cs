using ERP.Api.Application.Storage;
using Microsoft.Data.SqlClient;
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

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = options.Value.ConnectionString?.Trim();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("ConnectionString do storage SqlServer nao configurada."));
            }

            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                using var command = new SqlCommand("SELECT 1", connection);
                _ = command.ExecuteScalar();
                return Task.FromResult(HealthCheckResult.Healthy("Storage SqlServer conectado e respondendo."));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Falha ao validar storage SqlServer.", ex));
            }
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
