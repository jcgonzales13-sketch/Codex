using System.Diagnostics;
using ERP.Api.Application.Observability;

namespace ERP.Api.Application.Logging;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger,
    InMemoryObservabilityCollector observabilityCollector)
{
    public async Task Invoke(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        using var activity = ErpObservability.ActivitySource.StartActivity("http.request", ActivityKind.Server);
        activity?.SetTag("http.method", context.Request.Method);
        activity?.SetTag("http.route", context.Request.Path.Value);
        activity?.SetTag("correlation.id", correlationId);

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
            ["Method"] = context.Request.Method,
            ["Path"] = context.Request.Path.Value
        });

        var startedAt = Stopwatch.GetTimestamp();

        logger.LogInformation(
            "HTTP request started {Method} {Path}",
            context.Request.Method,
            context.Request.Path.Value);

        try
        {
            await next(context);

            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            logger.LogInformation(
                "HTTP request finished {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                Math.Round(elapsedMs, 2));
            activity?.SetTag("http.status_code", context.Response.StatusCode);
            ErpObservability.HttpRequests.Add(1);
            ErpObservability.HttpRequestDurationMs.Record(elapsedMs);
            observabilityCollector.RecordHttpRequest(context.Request.Method, context.Request.Path.Value ?? string.Empty, context.Response.StatusCode, elapsedMs, false);
        }
        catch
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            logger.LogError(
                "HTTP request failed {Method} {Path} after {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path.Value,
                Math.Round(elapsedMs, 2));
            activity?.SetStatus(ActivityStatusCode.Error);
            ErpObservability.HttpRequests.Add(1);
            ErpObservability.HttpFailures.Add(1);
            ErpObservability.HttpRequestDurationMs.Record(elapsedMs);
            observabilityCollector.RecordHttpRequest(context.Request.Method, context.Request.Path.Value ?? string.Empty, StatusCodes.Status500InternalServerError, elapsedMs, true);
            throw;
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-Id", out var values))
        {
            var candidate = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
