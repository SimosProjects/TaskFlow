using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Middleware;

/// <summary>
/// Global exception handling middleware.
/// 
/// Centralizes error handling and ensures consistent RFC 7807
/// ProblemDetails responses across the API.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the next middleware and maps known exceptions
    /// to appropriate HTTP ProblemDetails responses.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogInformation(ex, "Validation failure: {Message}", ex.Message);

            await WriteProblemDetailsAsync(
                context,
                HttpStatusCode.BadRequest,
                title: "Invalid request",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");

            await WriteProblemDetailsAsync(
                context,
                HttpStatusCode.InternalServerError,
                title: "Server error",
                detail: "An unexpected error occurred.");
        }
    }

    /// <summary>
    /// Writes a standardized RFC 7807 ProblemDetails response.
    /// </summary>
    private static Task WriteProblemDetailsAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        return context.Response.WriteAsJsonAsync(problem);
    }
}
