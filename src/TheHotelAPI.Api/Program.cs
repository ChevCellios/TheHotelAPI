using Microsoft.OpenApi.Models;
using TheHotelAPI.Api;
using TheHotelAPI.Application;
using TheHotelAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<HotelService>();
builder.Services.AddScoped<HotelSearchService>();
builder.Services.AddSingleton<IHotelRepository, InMemoryHotelRepository>();
builder.Services.AddScoped<ISearchPromptParser, DeterministicSearchPromptParser>();
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("TheHotelAPI/1.0");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "The Hotel API", Version = "v1" });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey, Name = "X-Api-Key", In = ParameterLocation.Header });
});

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();

public partial class Program;
