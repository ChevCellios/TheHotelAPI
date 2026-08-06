using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace TheHotelAPI.Api;

/// <summary>
/// Protects hotel write operations with a simple PoC API key while keeping reads and searches public.
/// A production deployment should replace this mechanism with OAuth2/OIDC.
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var protectedOperation = context.Request.Path.StartsWithSegments("/api/v1/hotels") && !HttpMethods.IsGet(context.Request.Method);
        if (protectedOperation)
        {
            var expected = configuration["ApiKey"];
            var supplied = context.Request.Headers["X-Api-Key"].FirstOrDefault();
            if (!KeysMatch(expected, supplied))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = 401, Title = "A valid X-Api-Key header is required." });
                return;
            }
        }
        await next(context);
    }

    private static bool KeysMatch(string? expected, string? supplied)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(supplied)) return false;
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
