using TheHotelAPI.Application;
using TheHotelAPI.Domain;
using TheHotelAPI.Infrastructure;

namespace TheHotelAPI.Application.Tests;

public sealed class HotelSearchServiceTests
{
    [Fact]
    public async Task Search_FiltersByBudgetAndRanksCheaperCloserHotelFirst()
    {
        var repository = new InMemoryHotelRepository();
        var nearCheap = new Hotel(Guid.NewGuid(), "Near and cheap", new Money(80, "EUR"), new GeoLocation(43.51, 16.44));
        var farExpensive = new Hotel(Guid.NewGuid(), "Far and expensive", new Money(140, "EUR"), new GeoLocation(43.9, 17.0));
        var overBudget = new Hotel(Guid.NewGuid(), "Over budget", new Money(200, "EUR"), new GeoLocation(43.51, 16.44));
        await repository.AddAsync(nearCheap);
        await repository.AddAsync(farExpensive);
        await repository.AddAsync(overBudget);
        var service = new HotelSearchService(repository, new DeterministicSearchPromptParser());

        var result = await service.SearchAsync(new("hotel in Split under 150 EUR"));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(nearCheap.Id, result.Items[0].Id);
        Assert.DoesNotContain(result.Items, item => item.Id == overBudget.Id);
    }

    [Fact]
    public async Task Search_RejectsUnknownLocation()
    {
        var service = new HotelSearchService(new InMemoryHotelRepository(), new DeterministicSearchPromptParser());
        await Assert.ThrowsAsync<SearchPromptException>(() => service.SearchAsync(new("hotel in London under 150 EUR")));
    }
}
