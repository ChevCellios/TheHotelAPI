using TheHotelAPI.Domain;

namespace TheHotelAPI.Application;

/// <summary>
/// Persistence boundary for hotel data. Infrastructure implementations may use memory,
/// a relational database, or another store without changing application services.
/// </summary>
public interface IHotelRepository
{
    Task<Hotel?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Hotel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> AddAsync(Hotel hotel, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Hotel hotel, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Converts a free-text user prompt into validated, structured search criteria.</summary>
public interface ISearchPromptParser
{
    Task<SearchCriteria> ParseAsync(string prompt, string? originCity = null, string? destinationCity = null, GeoLocation? explicitCurrentLocation = null, CancellationToken cancellationToken = default);
}

/// <summary>Resolves a city name to geographic coordinates.</summary>
public interface IGeocodingService
{
    Task<GeoLocation?> FindAsync(string city, CancellationToken cancellationToken = default);
}

/// <summary>Indicates that required search criteria could not be extracted from a prompt.</summary>
public sealed class SearchPromptException(string message) : Exception(message);

/// <summary>Indicates that a supplied city cannot be converted into a hotel location.</summary>
public sealed class LocationResolutionException(string message) : Exception(message);
