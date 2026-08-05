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
        var criteria = parser.Parse(request.Prompt);
        var hotels = await repository.GetAllAsync(cancellationToken);

        var ranked = hotels
            // Budget and currency are hard constraints; ranking is applied only to eligible hotels.
            .Where(h => string.Equals(h.PricePerNight.Currency, criteria.Currency, StringComparison.OrdinalIgnoreCase))
            .Where(h => h.PricePerNight.Amount <= criteria.MaximumBudget)
            .Select(h =>
            {
                var distance = h.Location.DistanceTo(criteria.Location);
                // Both components are normalized before equal weighting so units do not dominate the score.
                var priceScore = (double)(h.PricePerNight.Amount / criteria.MaximumBudget);
                var distanceScore = Math.Min(distance / DistanceReferenceKm, 1);
                return new HotelSearchItem(h.Id, h.Name,
                    new(h.PricePerNight.Amount, h.PricePerNight.Currency),
                    Math.Round(distance, 2), Math.Round((priceScore + distanceScore) / 2, 4));
            })
            // Explicit tie-breakers guarantee deterministic output and stable pagination.
            .OrderBy(h => h.Score).ThenBy(h => h.PricePerNight.Amount).ThenBy(h => h.DistanceKm).ThenBy(h => h.Id)
            .ToArray();

        var items = ranked.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToArray();
        return new(items, request.Page, request.PageSize, ranked.Length,
            new(new(criteria.Location.Latitude, criteria.Location.Longitude), criteria.MaximumBudget, criteria.Currency));
    }
}
