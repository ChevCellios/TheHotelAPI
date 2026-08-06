using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using TheHotelAPI.Application;

namespace TheHotelAPI.Api.Tests;

public sealed class HotelApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public HotelApiTests(WebApplicationFactory<Program> factory) => _client = factory
        .WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.ClearProviders()))
        .CreateClient();

    [Fact]
    public async Task CrudAndSearch_EndToEnd()
    {
        var request = new UpsertHotelRequest("Split Central", new(90, "EUR"), new(43.5081, 16.4402));
        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/hotels") { Content = JsonContent.Create(request) };
        create.Headers.Add("X-Api-Key", "development-only-key");
        var createResponse = await _client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var searchResponse = await _client.PostAsJsonAsync("/api/v1/hotel-searches",
            new SearchHotelsRequest("Hotel under 100 EUR", City: "Split"));
        searchResponse.EnsureSuccessStatusCode();
        var result = await searchResponse.Content.ReadFromJsonAsync<HotelSearchResponse>();
        Assert.NotNull(result);
        var hotel = Assert.Single(result.Items, hotel => hotel.Name == "Split Central");
        Assert.Equal(0, hotel.DistanceKm);
        Assert.Equal(43.5081, result.Criteria.CurrentLocation.Latitude);
    }

    [Fact]
    public async Task Create_WithoutApiKey_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/hotels", new UpsertHotelRequest("Hotel", new(90, "EUR"), new(43, 16)));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LiveHealthCheck_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health/live");
        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
