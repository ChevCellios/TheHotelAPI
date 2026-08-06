using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheHotelAPI.Application;

namespace TheHotelAPI.Api.Tests;

public sealed class HotelApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestApiKey = "integration-test-only-key";
    private readonly HttpClient _client;
    public HotelApiTests(WebApplicationFactory<Program> factory) => _client = factory
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiKey"] = TestApiKey
                }));
            builder.ConfigureLogging(logging => logging.ClearProviders());
        })
        .CreateClient();

    [Fact]
    public async Task CrudAndSearch_EndToEnd()
    {
        var request = new UpsertHotelRequest("Split Central", new(90, "EUR"), "Split");
        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/hotels") { Content = JsonContent.Create(request) };
        create.Headers.Add("X-Api-Key", TestApiKey);
        var createResponse = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdHotel = await createResponse.Content.ReadFromJsonAsync<HotelResponse>();
        Assert.NotNull(createdHotel);
        Assert.Equal(43.5081, createdHotel.Location.Latitude);
        Assert.Equal(16.4402, createdHotel.Location.Longitude);

        var searchResponse = await _client.PostAsJsonAsync("/api/v1/hotel-searches",
            new SearchHotelsRequest("Hotel under 100 EUR", OriginCity: "Zagreb", DestinationCity: "Split"));
        searchResponse.EnsureSuccessStatusCode();
        var result = await searchResponse.Content.ReadFromJsonAsync<HotelSearchResponse>();
        Assert.NotNull(result);
        var hotel = Assert.Single(result.Items, hotel => hotel.Name == "Split Central");
        Assert.Equal(259.06, hotel.DistanceKm);
        Assert.Equal("Split", result.Criteria.City);
        Assert.Equal(45.8150, result.Criteria.CurrentLocation.Latitude);
    }

    [Fact]
    public async Task Create_WithoutApiKey_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/hotels", new UpsertHotelRequest("Hotel", new(90, "EUR"), "Split"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Search_WithInvalidPrompt_ReturnsUnprocessableEntityProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/hotel-searches", new SearchHotelsRequest("no structured criteria"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_WithInvalidCity_ReturnsUnprocessableEntity()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/hotels")
        {
            Content = JsonContent.Create(new UpsertHotelRequest("Hotel", new(90, "EUR"), new string('a', 101)))
        };
        request.Headers.Add("X-Api-Key", TestApiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task LiveHealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_DocumentsWriteSecurityAndSearchExampleAccurately()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/swagger/v1/swagger.json"));
        var root = document.RootElement;
        var paths = root.GetProperty("paths");

        Assert.True(paths.GetProperty("/api/v1/hotels").GetProperty("post").TryGetProperty("security", out _));
        Assert.False(paths.GetProperty("/api/v1/hotel-searches").GetProperty("post").TryGetProperty("security", out _));

        var example = root.GetProperty("components").GetProperty("schemas")
            .GetProperty("SearchHotelsRequest").GetProperty("example");
        Assert.Equal("Tražim hotel do 150 EUR", example.GetProperty("prompt").GetString());
        Assert.Equal("Zagreb", example.GetProperty("originCity").GetString());
        Assert.Equal("Split", example.GetProperty("destinationCity").GetString());
        Assert.False(example.TryGetProperty("currentLocation", out _));
        Assert.False(example.TryGetProperty("page", out _));
        Assert.False(example.TryGetProperty("pageSize", out _));

        var hotelExample = root.GetProperty("components").GetProperty("schemas")
            .GetProperty("UpsertHotelRequest").GetProperty("example");
        Assert.Equal("Split", hotelExample.GetProperty("city").GetString());
        Assert.False(hotelExample.TryGetProperty("location", out _));
    }
}
