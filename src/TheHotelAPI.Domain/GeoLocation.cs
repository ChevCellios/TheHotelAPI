namespace TheHotelAPI.Domain;

/// <summary>Represents a validated point on Earth in decimal degrees.</summary>
public sealed record GeoLocation
{
    public double Latitude { get; }
    public double Longitude { get; }

    public GeoLocation(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be a finite value between -90 and 90.");
        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be a finite value between -180 and 180.");
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Calculates the great-circle distance to another point in kilometres.</summary>
    public double DistanceTo(GeoLocation other)
    {
        // Haversine is sufficiently accurate for hotel ranking and avoids an external GIS dependency.
        const double earthRadiusKm = 6371.0088;
        var latitudeDelta = DegreesToRadians(other.Latitude - Latitude);
        var longitudeDelta = DegreesToRadians(other.Longitude - Longitude);
        var firstLatitude = DegreesToRadians(Latitude);
        var secondLatitude = DegreesToRadians(other.Latitude);
        var haversine = Math.Pow(Math.Sin(latitudeDelta / 2), 2) +
                        Math.Cos(firstLatitude) * Math.Cos(secondLatitude) *
                        Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
