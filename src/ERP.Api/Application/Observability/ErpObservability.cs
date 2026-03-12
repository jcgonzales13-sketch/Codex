using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ERP.Api.Application.Observability;

public static class ErpObservability
{
    public const string ServiceName = "Codex.ERP.Api";

    public static readonly ActivitySource ActivitySource = new(ServiceName);
    public static readonly Meter Meter = new(ServiceName, "1.0.0");
    public static readonly Counter<long> HttpRequests = Meter.CreateCounter<long>("erp_http_requests_total");
    public static readonly Counter<long> HttpFailures = Meter.CreateCounter<long>("erp_http_failures_total");
    public static readonly Histogram<double> HttpRequestDurationMs = Meter.CreateHistogram<double>("erp_http_request_duration_ms");
    public static readonly Counter<long> DomainOperations = Meter.CreateCounter<long>("erp_domain_operations_total");
    public static readonly Counter<long> Exceptions = Meter.CreateCounter<long>("erp_exceptions_total");
}
