using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TheHotelAPI.Api;

/// <summary>Documents API-key security only on hotel write operations.</summary>
public sealed class ApiKeySecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;
        var path = context.ApiDescription.RelativePath;
        if (string.Equals(method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase) ||
            path is null ||
            !path.StartsWith("api/v1/hotels", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                }] = Array.Empty<string>()
            }
        ];
    }
}
