namespace ERP.Api.Application.Logging;

public static class LoggingExtensions
{
    public static IApplicationBuilder UseStructuredRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
