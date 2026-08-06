using TheHotelAPI.Application;
using TheHotelAPI.Domain;
using TheHotelAPI.Infrastructure;

namespace TheHotelAPI.Application.Tests;

public sealed class HotelSearchServiceTests
{
    [Fact]
    public async Task Search_ReturnsAllHotelsAndRanksCheaperCloserHotelFirst()
    {
        var repository = new InMemoryHotelRepository();
        var nearCheap = new Hotel(Guid.NewGuid(), "Near and cheap", new Money(80, "EUR"), new GeoLocation(43.51, 16.44));
        var farExpensive = new Hotel(Guid.NewGuid(), "Far and expensive", new Money(140, "EUR"), new GeoLocation(43.9, 17.0));
        var overBudget = new Hotel(Guid.NewGuid(), "Over budget", new Money(200, "EUR"), new GeoLocation(43.51, 16.44));
        await repository.AddAsync(nearCheap);
        await repository.AddAsync(farExpensive);
        await repository.AddAsync(overBudget);
        var service = new HotelSearchService(repository, CreateParser());

        var result = await service.SearchAsync(new("hotel in Split under 150 EUR"));

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(nearCheap.Id, result.Items[0].Id);
        Assert.Equal(farExpensive.Id, result.Items[^1].Id);
        Assert.Contains(result.Items, item => item.Id == overBudget.Id);
        Assert.Equal(0.21, result.Items[0].DistanceKm);
    }

    [Fact]
    public async Task Search_RejectsUnknownLocation()
    {
        var service = new HotelSearchService(new InMemoryHotelRepository(), CreateParser());
        await Assert.ThrowsAsync<SearchPromptException>(() => service.SearchAsync(new("hotel in London under 150 EUR")));
    }

    [Fact]
    public async Task Search_UsesExplicitCoordinatesWhenPromptHasNoCity()
    {
        var repository = new InMemoryHotelRepository();
        await repository.AddAsync(new Hotel(Guid.NewGuid(), "Nearby", new Money(100, "EUR"), new GeoLocation(45.81, 15.98)));
        var service = new HotelSearchService(repository, CreateParser());

        var result = await service.SearchAsync(new(
            "hotel under 150 EUR",
            new GeoLocationDto(45.8150, 15.9819)));

        Assert.Equal(45.8150, result.Criteria.CurrentLocation.Latitude);
        Assert.InRange(result.Items[0].DistanceKm, 0.55, 0.58);
    }

    [Fact]
    public async Task Search_ResolvesCityFieldWithoutCoordinates()
    {
        var service = new HotelSearchService(new InMemoryHotelRepository(), CreateParser());
        var result = await service.SearchAsync(new("hotel under 150 EUR", City: "Zagreb"));
        Assert.Equal(45.8150, result.Criteria.CurrentLocation.Latitude);
        Assert.Equal(15.9819, result.Criteria.CurrentLocation.Longitude);
    }

    private static DeterministicSearchPromptParser CreateParser() => new(new FakeGeocodingService());

    private sealed class FakeGeocodingService : IGeocodingService
    {
        public Task<GeoLocation?> FindAsync(string city, CancellationToken cancellationToken = default)
        {
            GeoLocation? location = city.ToLowerInvariant() switch
            {
                "split" => new(43.5081, 16.4402),
                "zagreb" => new(45.8150, 15.9819),
                _ => null
            };
            return Task.FromResult(location);
        }
    }
}
