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
        var nearCheap = new Hotel(Guid.NewGuid(), "Near and cheap", new Money(80, "EUR"), "Split", new GeoLocation(43.51, 16.44));
        var farExpensive = new Hotel(Guid.NewGuid(), "Far and expensive", new Money(140, "EUR"), "Split", new GeoLocation(43.9, 17.0));
        var overBudget = new Hotel(Guid.NewGuid(), "Over budget", new Money(200, "EUR"), "Split", new GeoLocation(43.51, 16.44));
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
    public async Task Search_ReturnsOnlyHotelsFromDestinationCity()
    {
        var repository = new InMemoryHotelRepository();
        await repository.AddAsync(new Hotel(Guid.NewGuid(), "Split hotel", new Money(90, "EUR"), "Split", new GeoLocation(43.5081, 16.4402)));
        await repository.AddAsync(new Hotel(Guid.NewGuid(), "Zagreb hotel", new Money(80, "EUR"), "Zagreb", new GeoLocation(45.8150, 15.9819)));
        var service = new HotelSearchService(repository, CreateParser());

        var result = await service.SearchAsync(new("hotel under 150 EUR", OriginCity: "Zagreb", DestinationCity: "Split"));

        var hotel = Assert.Single(result.Items);
        Assert.Equal("Split hotel", hotel.Name);
        Assert.Equal("Split", result.Criteria.City);
    }

    [Fact]
    public async Task Search_RejectsUnknownLocation()
    {
        var service = new HotelSearchService(new InMemoryHotelRepository(), CreateParser());
        await Assert.ThrowsAsync<SearchPromptException>(() => service.SearchAsync(new("hotel in London under 150 EUR")));
    }

    [Fact]
    public async Task Search_ResolvesOriginCityWithoutClientCoordinates()
    {
        var service = new HotelSearchService(new InMemoryHotelRepository(), CreateParser());
        var result = await service.SearchAsync(new("hotel under 150 EUR", OriginCity: "Zagreb", DestinationCity: "Split"));
        Assert.Equal(45.8150, result.Criteria.CurrentLocation.Latitude);
        Assert.Equal(15.9819, result.Criteria.CurrentLocation.Longitude);
    }

    [Fact]
    public async Task Search_RejectsExcessivelyLongCityBeforeCallingGeocoder()
    {
        var service = new HotelSearchService(new InMemoryHotelRepository(), CreateParser());

        var exception = await Assert.ThrowsAsync<SearchPromptException>(() =>
            service.SearchAsync(new("hotel under 150 EUR", OriginCity: new string('a', 101), DestinationCity: "Split")));

        Assert.Equal("City cannot exceed 100 characters.", exception.Message);
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
