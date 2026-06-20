namespace GymTrack.Common;

public static class ApiErrorResponseFactory
{
    public static ApiErrorResponse Create(
        int statusCode,
        string message,
        PathString path,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        return new ApiErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow,
            Path = path.HasValue ? path.Value! : "/"
        };
    }

    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(Create(statusCode, message, context.Request.Path, errors));
    }
}
