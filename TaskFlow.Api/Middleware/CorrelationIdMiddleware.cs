namespace TaskFlow.Api.Middleware;

/// <summary>
/// Middleware that establishes a correlation ID for every incoming request.
///
/// Behaviour:
///   - If the request carries an X-Correlation-ID header, that value is used,
///     allowing callers to trace a request end-to-end across service boundaries.
///   - Otherwise, ASP.NET Core's built-in TraceIdentifier is used as a fallback.
///
/// The correlation ID is:
///   - Pushed into the log scope so every ILogger call in the pipeline
///     automatically includes CorrelationId without additional instrumentation.
///   - Echoed back in the X-Correlation-ID response header so callers
///     can correlate their own logs with server-side entries.
///
/// Registration order: must be registered before ExceptionHandlingMiddleware
/// so that even error responses carry a correlation ID in both logs and headers.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prefer an incoming correlation ID from the caller.
        // Fall back to the ASP.NET Core TraceIdentifier, which is unique per request.
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                            ?? context.TraceIdentifier;

        // Echo the correlation ID back to the caller in the response.
        // OnStarting defers this until response writing begins, ensuring headers are writable.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Push the correlation ID into the log scope for the duration of this request.
        // Every ILogger call downstream will automatically include CorrelationId
        // in structured logs without any additional instrumentation.
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
