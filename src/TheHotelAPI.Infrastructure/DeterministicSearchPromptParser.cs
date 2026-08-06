using System.Globalization;
using System.Text.RegularExpressions;
using TheHotelAPI.Application;
using TheHotelAPI.Domain;

namespace TheHotelAPI.Infrastructure;

/// <summary>
/// Extracts a supported Croatian city and an EUR budget without requiring an external
/// geocoder or AI service. It can be replaced through <see cref="ISearchPromptParser"/>.
/// </summary>
public sealed partial class DeterministicSearchPromptParser(IGeocodingService geocoder) : ISearchPromptParser
{
    public async Task<SearchCriteria> ParseAsync(string prompt, string? originCity = null, string? destinationCity = null, GeoLocation? explicitCurrentLocation = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt)) throw new SearchPromptException("Prompt is required.");
        if (prompt.Length > 500) throw new SearchPromptException("Prompt cannot exceed 500 characters.");
        destinationCity = string.IsNullOrWhiteSpace(destinationCity) ? ExtractCity(prompt) : destinationCity.Trim();
        if (string.IsNullOrWhiteSpace(destinationCity))
            throw new SearchPromptException("Destination city could not be extracted. Use a prompt such as 'hotel in Split under 150 EUR'.");
        if (destinationCity.Length > 100)
            throw new SearchPromptException("City cannot exceed 100 characters.");

        // originCity is the traveller's starting point; the city in the prompt is the destination.
        originCity = string.IsNullOrWhiteSpace(originCity) ? destinationCity : originCity.Trim();
        if (originCity.Length > 100)
            throw new SearchPromptException("City cannot exceed 100 characters.");
        var currentLocation = explicitCurrentLocation ?? await geocoder.FindAsync(originCity, cancellationToken)
            ?? throw new SearchPromptException($"City '{originCity}' could not be found.");

        // Validate the destination independently, even when a different origin was supplied.
        _ = await geocoder.FindAsync(destinationCity, cancellationToken)
            ?? throw new SearchPromptException($"City '{destinationCity}' could not be found.");
        var match = BudgetRegex().Match(prompt);
        if (!match.Success || !decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var budget) || budget <= 0)
            throw new SearchPromptException("Budget could not be extracted. Example: 'hotel in Split under 150 EUR'.");
        return new(destinationCity, currentLocation, decimal.Round(budget, 2), "EUR");
    }

    private static string? ExtractCity(string prompt)
    {
        var match = CityRegex().Match(prompt);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    // Source generation compiles the expression at build time and avoids per-request regex setup.
    [GeneratedRegex(@"(?<!\d)(\d+(?:[\.,]\d{1,2})?)\s*(?:EUR|\u20AC|eura?)", RegexOptions.IgnoreCase)]
    private static partial Regex BudgetRegex();

    [GeneratedRegex(@"(?:\bin\s+|\bu\s+(?:gradu\s+)?|\bnear\s+|\bblizu\s+)([\p{L}][\p{L}\s.'-]*?)(?=\s+(?:under|ispod|do|for|za)\s+\d|[,.;]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex CityRegex();
}
