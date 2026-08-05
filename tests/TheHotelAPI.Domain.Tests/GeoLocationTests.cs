using TheHotelAPI.Domain;

namespace TheHotelAPI.Domain.Tests;

public sealed class GeoLocationTests
{
    [Fact]
    public void DistanceTo_ReturnsExpectedDistanceBetweenZagrebAndSplit()
    {
        var zagreb = new GeoLocation(45.8150, 15.9819);
        var split = new GeoLocation(43.5081, 16.4402);
        Assert.InRange(zagreb.DistanceTo(split), 257, 260);
    }

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void Constructor_RejectsInvalidCoordinates(double latitude, double longitude)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new GeoLocation(latitude, longitude));
}
