using TheHotelAPI.Domain;

namespace TheHotelAPI.Application;

/// <summary>Coordinates hotel CRUD use cases independently of the HTTP and storage layers.</summary>
public sealed class HotelService(IHotelRepository repository)
{
    public async Task<HotelResponse> CreateAsync(UpsertHotelRequest request, CancellationToken cancellationToken = default)
    {
        var hotel = Map(Guid.NewGuid(), request);
        await repository.AddAsync(hotel, cancellationToken);
        return Map(hotel);
    }

    public async Task<HotelResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => MapOrNull(await repository.GetAsync(id, cancellationToken));

    public async Task<PagedResult<HotelResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        var hotels = await repository.GetAllAsync(cancellationToken);
        // The id tie-breaker makes pagination stable when multiple hotels have the same name.
        var ordered = hotels.OrderBy(h => h.Name).ThenBy(h => h.Id).ToArray();
        return new(ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(Map).ToArray(), page, pageSize, ordered.Length);
    }

    public async Task<HotelResponse?> UpdateAsync(Guid id, UpsertHotelRequest request, CancellationToken cancellationToken = default)
    {
        var hotel = Map(id, request);
        return await repository.UpdateAsync(hotel, cancellationToken) ? Map(hotel) : null;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => repository.DeleteAsync(id, cancellationToken);

    internal static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        if (pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
    }

    private static Hotel Map(Guid id, UpsertHotelRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.PricePerNight);
        ArgumentNullException.ThrowIfNull(request.Location);
        return new(id, request.Name, new(request.PricePerNight.Amount, request.PricePerNight.Currency),
            new(request.Location.Latitude, request.Location.Longitude));
    }

    private static HotelResponse? MapOrNull(Hotel? hotel) => hotel is null ? null : Map(hotel);
    private static HotelResponse Map(Hotel hotel) => new(hotel.Id, hotel.Name,
        new(hotel.PricePerNight.Amount, hotel.PricePerNight.Currency),
        new(hotel.Location.Latitude, hotel.Location.Longitude));
}
