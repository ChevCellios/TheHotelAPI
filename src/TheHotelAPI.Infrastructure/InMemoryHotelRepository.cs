using System.Collections.Concurrent;
using TheHotelAPI.Application;
using TheHotelAPI.Domain;

namespace TheHotelAPI.Infrastructure;

/// <summary>
/// Thread-safe PoC repository whose lifetime is tied to the application process.
/// Data is intentionally lost when the API restarts.
/// </summary>
public sealed class InMemoryHotelRepository : IHotelRepository
{
    private readonly ConcurrentDictionary<Guid, Hotel> _hotels = new();
    public Task<Hotel?> GetAsync(Guid id, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); _hotels.TryGetValue(id, out var hotel); return Task.FromResult(hotel); }
    public Task<IReadOnlyCollection<Hotel>> GetAllAsync(CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); return Task.FromResult<IReadOnlyCollection<Hotel>>(_hotels.Values.ToArray()); }
    public Task<bool> AddAsync(Hotel hotel, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); return Task.FromResult(_hotels.TryAdd(hotel.Id, hotel)); }
    public Task<bool> UpdateAsync(Hotel hotel, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        // Compare-and-swap prevents a concurrent writer from being overwritten silently.
        while (_hotels.TryGetValue(hotel.Id, out var current)) if (_hotels.TryUpdate(hotel.Id, hotel, current)) return Task.FromResult(true);
        return Task.FromResult(false);
    }
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); return Task.FromResult(_hotels.TryRemove(id, out _)); }
}
