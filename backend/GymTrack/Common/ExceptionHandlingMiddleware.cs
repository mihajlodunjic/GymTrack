using GymTrack.Common.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace GymTrack.Common;

public sealed class ExceptionHandlingMiddleware
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
        catch (AppException exception)
        {
            await ApiErrorResponseFactory.WriteAsync(context, exception.StatusCode, exception.Message, exception.Errors);
        }
        catch (ValidationException exception)
        {
            await ApiErrorResponseFactory.WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                string.IsNullOrWhiteSpace(exception.Message) ? "Validation failed." : exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request.");

            await ApiErrorResponseFactory.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }
}
