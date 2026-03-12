using ERP.Api.Application.Contracts;

namespace ERP.Api.Application.Observability;

public sealed class InMemoryObservabilityCollector
{
    private readonly object _sync = new();
    private long _totalHttpRequests;
    private long _totalHttpFailures;
    private long _totalDomainOperations;
    private long _totalExceptions;
    private DateTimeOffset? _lastRequestAt;
    private DateTimeOffset? _lastDomainOperationAt;
    private readonly Dictionary<string, EndpointMetricSnapshot> _endpoints = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _operations = new(StringComparer.OrdinalIgnoreCase);

    public void RecordHttpRequest(string method, string path, int statusCode, double durationMs, bool failed)
    {
        lock (_sync)
        {
            _totalHttpRequests++;
            if (failed || statusCode >= 500)
            {
                _totalHttpFailures++;
            }

            _lastRequestAt = DateTimeOffset.UtcNow;

            var key = $"{method} {path}";
            if (!_endpoints.TryGetValue(key, out var current))
            {
                current = new EndpointMetricSnapshot(method, path, 0, 0, 0, 0);
            }

            var count = current.TotalRequests + 1;
            var failures = current.TotalFailures + ((failed || statusCode >= 500) ? 1 : 0);
            var average = ((current.AverageDurationMs * current.TotalRequests) + durationMs) / count;
            _endpoints[key] = current with
            {
                TotalRequests = count,
                TotalFailures = failures,
                LastStatusCode = statusCode,
                AverageDurationMs = Math.Round(average, 2)
            };
        }
    }

    public void RecordDomainOperation(string action)
    {
        lock (_sync)
        {
            _totalDomainOperations++;
            _lastDomainOperationAt = DateTimeOffset.UtcNow;
            _operations[action] = _operations.TryGetValue(action, out var total) ? total + 1 : 1;
        }
    }

    public void RecordException(string code)
    {
        lock (_sync)
        {
            _totalExceptions++;
            _operations[$"exception:{code}"] = _operations.TryGetValue($"exception:{code}", out var total) ? total + 1 : 1;
        }
    }

    public ObservabilityMetricsResponse Snapshot()
    {
        lock (_sync)
        {
            return new ObservabilityMetricsResponse(
                _totalHttpRequests,
                _totalHttpFailures,
                _totalDomainOperations,
                _totalExceptions,
                _lastRequestAt,
                _lastDomainOperationAt,
                _endpoints.Values.OrderByDescending(item => item.TotalRequests).ThenBy(item => item.Path).ToArray(),
                _operations.OrderByDescending(item => item.Value)
                    .Select(item => new OperationMetricSnapshot(item.Key, item.Value))
                    .ToArray());
        }
    }
}
