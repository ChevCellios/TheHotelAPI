using TheHotelAPI.Domain;

namespace TheHotelAPI.Domain.Tests;

public sealed class HotelTests
{
    [Fact]
    public void Constructor_TrimsNameAndNormalizesCurrency()
    {
        var hotel = new Hotel(Guid.NewGuid(), "  Adriatic  ", new Money(99.999m, "eur"), new GeoLocation(43, 16));
        Assert.Equal("Adriatic", hotel.Name);
        Assert.Equal(100m, hotel.PricePerNight.Amount);
        Assert.Equal("EUR", hotel.PricePerNight.Currency);
    }

    [Fact]
    public void Money_RejectsNonPositiveAmount()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Money(0, "EUR"));

    [Fact]
    public void Money_RejectsUnsupportedCurrency()
        => Assert.Throws<ArgumentException>(() => new Money(100, "USD"));
}
