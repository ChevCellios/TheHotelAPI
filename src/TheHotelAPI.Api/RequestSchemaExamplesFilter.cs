using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TheHotelAPI.Application;

namespace TheHotelAPI.Api;

public sealed class RequestSchemaExamplesFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(UpsertHotelRequest))
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("Split Central Hotel"),
                ["pricePerNight"] = new OpenApiObject
                {
                    ["amount"] = new OpenApiDouble(95),
                    ["currency"] = new OpenApiString("EUR")
                },
                ["city"] = new OpenApiString("Split")
            };
            return;
        }

        if (context.Type != typeof(SearchHotelsRequest)) return;

        schema.Example = new OpenApiObject
        {
            ["prompt"] = new OpenApiString("Tražim hotel do 150 EUR"),
            ["originCity"] = new OpenApiString("Zagreb"),
            ["destinationCity"] = new OpenApiString("Split")
        };
    }
}
