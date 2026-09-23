using Asisya.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Asisya.Api.Errors;

/// <summary>
/// Maps application exceptions to RFC 7807 Problem Details. Unexpected errors are logged
/// and returned as a generic 500 so internals never leak to clients.
/// </summary>
public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            BusinessException ex => (StatusCodes.Status400BadRequest, "Business rule violation", ex.Message),
            NotFoundException ex => (StatusCodes.Status404NotFound, "Resource not found", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}
