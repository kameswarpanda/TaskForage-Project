using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TaskForge.API.Middleware;

/// <summary>
/// Middleware for structured request/response logging using Serilog.
/// Logs method, path, status code, and elapsed time for every request.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "[{RequestId}] → {Method} {Path}{Query}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);

        await _next(context);

        stopwatch.Stop();
        _logger.LogInformation(
            "[{RequestId}] ← {StatusCode} in {Elapsed}ms",
            requestId,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}

/// <summary>
/// Global exception handling middleware.
/// Catches all unhandled exceptions and returns standardized ProblemDetails responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            InvalidOperationException => (HttpStatusCode.Conflict, exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning("Handled exception: {Type} - {Message}", exception.GetType().Name, exception.Message);
        }

        var problemDetails = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title = statusCode.ToString(),
            status = (int)statusCode,
            detail = message,
            traceId = context.TraceIdentifier
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
