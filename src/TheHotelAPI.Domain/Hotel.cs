namespace TheHotelAPI.Domain;

/// <summary>
/// Aggregate root containing the hotel data required for management and search.
/// </summary>
public sealed record Hotel
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public Money PricePerNight { get; init; }
    public GeoLocation Location { get; init; }

    public Hotel(Guid id, string name, Money pricePerNight, GeoLocation location)
    {
        if (id == Guid.Empty) throw new ArgumentException("Hotel id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Hotel name is required.", nameof(name));
        if (name.Trim().Length > 200) throw new ArgumentException("Hotel name cannot exceed 200 characters.", nameof(name));
        Id = id;
        Name = name.Trim();
        PricePerNight = pricePerNight ?? throw new ArgumentNullException(nameof(pricePerNight));
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }
}
