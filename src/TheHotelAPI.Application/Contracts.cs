using TheHotelAPI.Domain;

namespace TheHotelAPI.Application;

public sealed record MoneyDto(decimal Amount, string Currency);
public sealed record GeoLocationDto(double Latitude, double Longitude);
public sealed record UpsertHotelRequest(string Name, MoneyDto PricePerNight, GeoLocationDto Location);
public sealed record HotelResponse(Guid Id, string Name, MoneyDto PricePerNight, GeoLocationDto Location);
public sealed record SearchHotelsRequest(
    string Prompt,
    GeoLocationDto? CurrentLocation = null,
    int Page = 1,
    int PageSize = 20,
    string? City = null);
public sealed record SearchCriteria(GeoLocation CurrentLocation, decimal Budget, string Currency);
public sealed record HotelSearchItem(Guid Id, string Name, MoneyDto PricePerNight, double DistanceKm, double Score);
public sealed record SearchCriteriaResponse(GeoLocationDto CurrentLocation, decimal Budget, string Currency);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record HotelSearchResponse(IReadOnlyList<HotelSearchItem> Items, int Page, int PageSize, int TotalCount, SearchCriteriaResponse Criteria);
