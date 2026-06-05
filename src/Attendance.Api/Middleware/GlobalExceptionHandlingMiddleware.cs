using System.Diagnostics;
using Attendance.Application.Exceptions;
using Attendance.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Attendance.Api.Middleware;

/// <summary>
/// ASP.NET Core middleware that catches all unhandled exceptions thrown during request processing
/// and maps them to RFC 7807 <see cref="ProblemDetails"/> JSON responses.
/// This is the single error-serialisation point — no controller or service needs try/catch for
/// known domain exceptions.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="GlobalExceptionHandlingMiddleware"/>.
    /// </summary>
    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Processes the HTTP request, catching any exception and converting it to a problem response.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — do not write a response body.
            context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, title) = exception switch
        {
            ValidationException          => (StatusCodes.Status400BadRequest,            "Validation Failed"),
            DomainException              => (StatusCodes.Status400BadRequest,            "Business Rule Violation"),
            InvalidCredentialsException  => (StatusCodes.Status401Unauthorized,          "Invalid Credentials"),
            AuthenticationException      => (StatusCodes.Status401Unauthorized,          "Authentication Failed"),
            UnauthorizedAccessException  => (StatusCodes.Status403Forbidden,             "Access Denied"),
            EmployeeNotFoundException    => (StatusCodes.Status404NotFound,              "Employee Not Found"),
            ActiveShiftNotFoundException => (StatusCodes.Status409Conflict,              "No Active Shift"),
            ActiveShiftAlreadyExistsException
                                         => (StatusCodes.Status409Conflict,             "Shift Already Active"),
            TimeProviderException        => (StatusCodes.Status500InternalServerError,   "Time Service Unavailable"),
            _                            => (StatusCodes.Status500InternalServerError,   "An unexpected error occurred")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. TraceId={TraceId} Method={Method} Path={Path}",
                traceId, context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed. Status={Status} TraceId={TraceId} Method={Method} Path={Path}",
                statusCode, traceId, context.Request.Method, context.Request.Path);
        }

        // Collect FluentValidation error messages into the detail field.
        var detail = exception is ValidationException validationEx
            ? string.Join(" | ", validationEx.Errors.Select(e => $"[{e.PropertyName}] {e.ErrorMessage}"))
            : exception.Message;

        var problemDetails = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails, context.RequestAborted);
    }
}
