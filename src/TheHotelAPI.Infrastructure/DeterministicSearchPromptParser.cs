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
    public async Task<SearchCriteria> ParseAsync(string prompt, string? city = null, GeoLocation? explicitCurrentLocation = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt)) throw new SearchPromptException("Prompt is required.");
        if (prompt.Length > 500) throw new SearchPromptException("Prompt cannot exceed 500 characters.");
        var currentLocation = explicitCurrentLocation;
        if (currentLocation is null)
        {
            city = string.IsNullOrWhiteSpace(city) ? ExtractCity(prompt) : city.Trim();
            if (string.IsNullOrWhiteSpace(city))
                throw new SearchPromptException("City could not be extracted. Provide the city field or use a prompt such as 'hotel in Split under 150 EUR'.");
            currentLocation = await geocoder.FindAsync(city, cancellationToken)
                ?? throw new SearchPromptException($"City '{city}' could not be found.");
        }
        var match = BudgetRegex().Match(prompt);
        if (!match.Success || !decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var budget) || budget <= 0)
            throw new SearchPromptException("Budget could not be extracted. Example: 'hotel in Split under 150 EUR'.");
        return new(currentLocation, decimal.Round(budget, 2), "EUR");
    }

    private static string? ExtractCity(string prompt)
    {
        var match = CityRegex().Match(prompt);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    // Source generation compiles the expression at build time and avoids per-request regex setup.
    [GeneratedRegex(@"(?<!\d)(\d+(?:[\.,]\d{1,2})?)\s*(?:EUR|\u20AC|eura?)", RegexOptions.IgnoreCase)]
    private static partial Regex BudgetRegex();

    [GeneratedRegex(@"(?:\bin\s+|\bu\s+|\bnear\s+|\bblizu\s+)([\p{L}][\p{L}\s.'-]*?)(?=\s+(?:under|ispod|do|for|za)\s+\d|[,.;]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex CityRegex();
}
