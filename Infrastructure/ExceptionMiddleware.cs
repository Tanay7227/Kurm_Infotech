using System.Text.Json;
using UserManagementAPI.Application;

namespace UserManagementAPI.Infrastructure;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ApiException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = exception.Message }));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request error");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
        }
    }
}
