using Microsoft.AspNetCore.Mvc;
using TheHotelAPI.Application;

namespace TheHotelAPI.Api;

/// <summary>Maps expected validation failures to consistent RFC 7807 Problem Details responses.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception) when (exception is ArgumentException or SearchPromptException)
        {
            logger.LogWarning("Request validation failed: {Message}", exception.Message);
            var status = exception is SearchPromptException ? 422 : 400;
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = "Request validation failed", Detail = exception.Message });
        }
    }
}
