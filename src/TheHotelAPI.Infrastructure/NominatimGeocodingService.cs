using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TheHotelAPI.Application;
using TheHotelAPI.Domain;

namespace TheHotelAPI.Infrastructure;

/// <summary>Uses a small offline cache first, then OpenStreetMap Nominatim for other cities.</summary>
public sealed class NominatimGeocodingService(HttpClient httpClient) : IGeocodingService
{
    private static readonly IReadOnlyDictionary<string, GeoLocation> KnownCities =
        new Dictionary<string, GeoLocation>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dubrovnik"] = new(42.6507, 18.0944), ["Split"] = new(43.5081, 16.4402),
            ["Zagreb"] = new(45.8150, 15.9819), ["Zadar"] = new(44.1194, 15.2314),
            ["Rijeka"] = new(45.3271, 14.4422), ["Pula"] = new(44.8666, 13.8496),
            ["Osijek"] = new(45.5550, 18.6955)
        };

    public async Task<GeoLocation?> FindAsync(string city, CancellationToken cancellationToken = default)
    {
        city = city.Trim();
        if (KnownCities.TryGetValue(city, out var known)) return known;

        var url = $"search?format=jsonv2&limit=1&featuretype=city&q={Uri.EscapeDataString(city)}";
        var results = await httpClient.GetFromJsonAsync<GeocodingResult[]>(url, cancellationToken);
        var result = results?.FirstOrDefault();
        if (result is null ||
            !double.TryParse(result.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
            !double.TryParse(result.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)) return null;
        return new GeoLocation(latitude, longitude);
    }

    private sealed record GeocodingResult(
        [property: JsonPropertyName("lat")] string Latitude,
        [property: JsonPropertyName("lon")] string Longitude);
}
