using TheHotelAPI.Domain;

namespace TheHotelAPI.Application;

/// <summary>Filters, scores, orders, and paginates hotels for structured search criteria.</summary>
public sealed class HotelSearchService(IHotelRepository repository, ISearchPromptParser parser)
{
    // Distances at or above this value receive the maximum distance penalty.
    private const double DistanceReferenceKm = 100;

    public async Task<HotelSearchResponse> SearchAsync(SearchHotelsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        HotelService.ValidatePaging(request.Page, request.PageSize);
        // The origin is used for distance; the explicit destination (or prompt fallback) filters hotel results.
        var criteria = await parser.ParseAsync(request.Prompt, request.OriginCity, request.DestinationCity, cancellationToken: cancellationToken);
        var hotels = await repository.GetAllAsync(cancellationToken);

        var ranked = hotels
            .Where(h => string.Equals(h.City, criteria.City, StringComparison.OrdinalIgnoreCase))
            // The assignment asks for all CRUD-managed hotels. Budget affects score but is not a filter.
            .Select(h =>
            {
                var distance = h.Location.DistanceTo(criteria.CurrentLocation);
                // Both components are normalized before equal weighting so units do not dominate the score.
                // A hotel above budget receives a score greater than 1 and remains in the result list.
                var priceScore = (double)(h.PricePerNight.Amount / criteria.Budget);
                var distanceScore = Math.Min(distance / DistanceReferenceKm, 1);
                return new HotelSearchItem(h.Id, h.Name,
                    new(h.PricePerNight.Amount, h.PricePerNight.Currency),
                    h.City,
                    Math.Round(distance, 2), Math.Round((priceScore + distanceScore) / 2, 4));
            })
            // Explicit tie-breakers guarantee deterministic output and stable pagination.
            .OrderBy(h => h.Score).ThenBy(h => h.PricePerNight.Amount).ThenBy(h => h.DistanceKm).ThenBy(h => h.Id)
            .ToArray();

        var items = ranked.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToArray();
        return new(items, request.Page, request.PageSize, ranked.Length,
            new(criteria.City, new(criteria.CurrentLocation.Latitude, criteria.CurrentLocation.Longitude), criteria.Budget, criteria.Currency));
    }
}
