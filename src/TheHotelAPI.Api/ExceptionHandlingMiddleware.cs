using Microsoft.AspNetCore.Mvc;
using TheHotelAPI.Application;

namespace TheHotelAPI.Api;

/// <summary>Maps expected validation failures to consistent RFC 7807 Problem Details responses.</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (exception is ArgumentException or SearchPromptException or LocationResolutionException)
        {
            logger.LogWarning("Request validation failed: {Message}", exception.Message);
            var status = exception is SearchPromptException or LocationResolutionException ? 422 : 400;
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails { Status = status, Title = "Request validation failed", Detail = exception.Message },
                options: null,
                contentType: "application/problem+json",
                cancellationToken: context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An unhandled error occurred while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted) throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            }, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
