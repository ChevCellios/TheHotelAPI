using System.Globalization;
using System.Text.RegularExpressions;
using TheHotelAPI.Application;
using TheHotelAPI.Domain;

namespace TheHotelAPI.Infrastructure;

/// <summary>
/// Extracts a supported Croatian city and an EUR budget without requiring an external
/// geocoder or AI service. It can be replaced through <see cref="ISearchPromptParser"/>.
/// </summary>
public sealed partial class DeterministicSearchPromptParser : ISearchPromptParser
{
    private static readonly IReadOnlyDictionary<string, GeoLocation> Locations = new Dictionary<string, GeoLocation>(StringComparer.OrdinalIgnoreCase)
    {
        ["dubrovnik"] = new(42.6507, 18.0944), ["split"] = new(43.5081, 16.4402),
        ["zagreb"] = new(45.8150, 15.9819), ["zadar"] = new(44.1194, 15.2314),
        ["rijeka"] = new(45.3271, 14.4422), ["pula"] = new(44.8666, 13.8496),
        ["osijek"] = new(45.5550, 18.6955)
    };

    public SearchCriteria Parse(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) throw new SearchPromptException("Prompt is required.");
        if (prompt.Length > 500) throw new SearchPromptException("Prompt cannot exceed 500 characters.");
        // A fixed catalogue keeps this PoC deterministic, offline, and straightforward to test.
        var location = Locations.FirstOrDefault(item => prompt.Contains(item.Key, StringComparison.OrdinalIgnoreCase));
        if (location.Key is null) throw new SearchPromptException($"Location could not be extracted. Supported cities: {string.Join(", ", Locations.Keys)}.");
        var match = BudgetRegex().Match(prompt);
        if (!match.Success || !decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var budget) || budget <= 0)
            throw new SearchPromptException("Budget could not be extracted. Example: 'hotel in Split under 150 EUR'.");
        return new(location.Value, decimal.Round(budget, 2), "EUR");
    }

    // Source generation compiles the expression at build time and avoids per-request regex setup.
    [GeneratedRegex(@"(?<!\d)(\d+(?:[\.,]\d{1,2})?)\s*(?:EUR|€|eura?)", RegexOptions.IgnoreCase)]
    private static partial Regex BudgetRegex();
}
