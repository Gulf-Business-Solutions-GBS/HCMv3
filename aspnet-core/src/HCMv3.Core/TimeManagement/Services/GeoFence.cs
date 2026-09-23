using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace HCMv3.TimeManagement.Services;

/// <summary>A point on the Earth in decimal degrees.</summary>
public readonly record struct GeoPoint(double Latitude, double Longitude);

/// <summary>
/// The site boundary drawn around a work location: a closed polygon that says exactly where a self-service
/// punch counts as "at work", in place of the plain radius kept on <c>WorkLocation.GeoRange</c>.
///
/// <para>
/// Legacy stored <c>WorkLocation.GeoFenceCoordinates</c> as free text for the mobile app to DRAW on a map
/// and never read it back -- no parser, no format, nothing that judged a punch against it. So there is no
/// legacy behaviour to match here, only a legacy column to give a meaning. The format below is that
/// meaning, and the server is what enforces it.
/// </para>
///
/// <para>
/// <b>The format.</b> Latitude and longitude in decimal degrees, latitude first, one pair per point,
/// points separated by a semicolon (a newline or a <c>|</c> also work, because people paste lists):
/// </para>
/// <code>
/// 29.3399,48.0820; 29.3399,48.0856; 29.3377,48.0856; 29.3377,48.0820
/// </code>
/// <para>
/// That example is a block of Salmiya, Kuwait, roughly 240 m by 350 m. Latitude first is deliberate: it is
/// the order every map app shows a pin in, and the order <c>WorkLocation.Latitude</c>/<c>Longitude</c>
/// already sit in on the same form, so an administrator copying a corner out of Google Maps types what they
/// see. At least three points are needed; the ring closes itself, so repeating the first point at the end
/// is allowed but not required, and 2,000 characters hold about seventy corners.
/// </para>
/// <para>
/// <b>GeoJSON is also accepted</b>, because that is what a "draw a shape" tool exports, and retyping its
/// numbers by hand is how corners get transposed. Text starting with <c>{</c> or <c>[</c> is read as GeoJSON,
/// where -- unlike everywhere else -- the pairs are <c>[longitude, latitude]</c>. Only the outer ring is
/// used: a hole punched in a site boundary would mean an employee standing in a courtyard is "not at work",
/// which is not a rule anyone has asked for.
/// </para>
/// <para>
/// <b>Unreadable text is not a refusal.</b> <see cref="TryParse"/> returns <c>false</c> and the caller falls
/// back to the radius. A typo in a master-data field must not be able to stop a whole site clocking in.
/// </para>
/// </summary>
public static class GeoFence
{
    /// <summary>Least corners a boundary can have.</summary>
    public const int MinimumPoints = 3;

    /// <summary>
    /// A point exactly on the boundary counts as inside. Coordinates arrive from a phone with far more
    /// decimals than this, so the tolerance only decides the knife-edge case, which should fall the way that
    /// lets somebody standing on the site's own fence line clock in.
    /// </summary>
    private const double OnEdgeToleranceDegrees = 1e-9;

    private const double MetresPerDegreeLatitude = 111_320d;

    /// <summary>
    /// Reads <paramref name="text"/> into a boundary. <c>false</c> -- with <paramref name="polygon"/> empty
    /// -- when the field is blank, malformed, or names fewer than <see cref="MinimumPoints"/> points.
    /// </summary>
    public static bool TryParse(string text, out IReadOnlyList<GeoPoint> polygon)
    {
        polygon = Array.Empty<GeoPoint>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        var points = trimmed[0] is '{' or '['
            ? ReadGeoJson(trimmed)
            : ReadPairList(trimmed);

        if (points == null)
        {
            return false;
        }

        // A ring written closed ("...; 29.3399,48.0820") describes the same shape as an open one, and the
        // algorithms below close it themselves; keeping the repeat would add a zero-length edge.
        if (points.Count > 1 && Same(points[0], points[^1]))
        {
            points.RemoveAt(points.Count - 1);
        }

        if (points.Count < MinimumPoints)
        {
            return false;
        }

        polygon = points;

        return true;
    }

    /// <summary>
    /// Whether (<paramref name="latitude"/>, <paramref name="longitude"/>) is inside the boundary, by ray
    /// casting, with a point on an edge counted as inside. Longitude is the x axis and latitude the y: over a
    /// site a few hundred metres across, treating degrees as a flat grid is accurate to well under a metre,
    /// and the alternative is a spherical winding test nobody can check by reading it.
    /// </summary>
    public static bool Contains(IReadOnlyList<GeoPoint> polygon, double latitude, double longitude)
    {
        if (polygon == null || polygon.Count < MinimumPoints)
        {
            return false;
        }

        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = polygon[i];
            var b = polygon[j];

            if (IsOnSegment(latitude, longitude, a, b))
            {
                return true;
            }

            // The ray runs east from the point: an edge counts when it straddles the point's latitude and
            // crosses to the point's east. The half-open straddle test (one end strictly above) is what
            // stops a vertex exactly at the point's latitude being counted twice.
            var straddles = a.Latitude > latitude != b.Latitude > latitude;
            if (!straddles)
            {
                continue;
            }

            var crossingLongitude = a.Longitude +
                                    (latitude - a.Latitude) * (b.Longitude - a.Longitude) / (b.Latitude - a.Latitude);
            if (longitude < crossingLongitude)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// How far (<paramref name="latitude"/>, <paramref name="longitude"/>) is from the nearest point of the
    /// boundary, in metres. This is what "out of range" means for a shape: a circle can report the distance
    /// to its centre less its radius, a polygon has no such number, and the distance to the fence is the one
    /// an employee can act on ("you are 40 m outside the site").
    /// </summary>
    public static double DistanceToBoundaryMeters(IReadOnlyList<GeoPoint> polygon, double latitude, double longitude)
    {
        if (polygon == null || polygon.Count < MinimumPoints)
        {
            return 0;
        }

        // Flat local grid in metres, centred on the point, so the segment maths below is ordinary plane
        // geometry. Longitude degrees shrink towards the poles, hence the cosine.
        var metresPerDegreeLongitude = MetresPerDegreeLatitude * Math.Cos(latitude * Math.PI / 180);

        (double x, double y) ToGrid(GeoPoint p) =>
            ((p.Longitude - longitude) * metresPerDegreeLongitude, (p.Latitude - latitude) * MetresPerDegreeLatitude);

        var nearest = double.MaxValue;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = ToGrid(polygon[i]);
            var b = ToGrid(polygon[j]);
            nearest = Math.Min(nearest, DistanceToSegment(a, b));
        }

        return nearest;
    }

    // ---------------------------------------------------------------- parsing

    /// <summary>"29.3399,48.0820; 29.3399,48.0856; ..." -- and the same list broken over lines or pipes.</summary>
    private static List<GeoPoint> ReadPairList(string text)
    {
        var points = new List<GeoPoint>();

        foreach (var chunk in text.Split(new[] { ';', '\n', '\r', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrWhiteSpace(chunk))
            {
                continue;
            }

            var parts = chunk.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 ||
                !TryReadDegrees(parts[0], out var latitude) ||
                !TryReadDegrees(parts[1], out var longitude))
            {
                return null;
            }

            if (!IsPlausible(latitude, longitude))
            {
                return null;
            }

            points.Add(new GeoPoint(latitude, longitude));
        }

        return points;
    }

    /// <summary>
    /// The outer ring of a GeoJSON Polygon, a Feature wrapping one, or a bare array of pairs -- in
    /// <c>[longitude, latitude]</c> order, as the standard requires. Only the outer ring is used: a hole
    /// punched in a site boundary would mean an employee standing in a courtyard is "not at work", which is
    /// not a rule anyone has asked for.
    /// </summary>
    private static List<GeoPoint> ReadGeoJson(string text)
    {
        JsonElement ring;
        try
        {
            using var document = JsonDocument.Parse(text);
            var coordinates = FindCoordinates(document.RootElement);
            if (coordinates is not { ValueKind: JsonValueKind.Array })
            {
                return null;
            }

            // Descend to the level whose members are pairs of numbers: a Polygon nests coordinates ->
            // ring -> point, a bare list only point, and a hole is a second ring at the level above.
            var level = coordinates.Value;
            while (level.GetArrayLength() > 0 && level[0].ValueKind == JsonValueKind.Array &&
                   level[0].GetArrayLength() > 0 && level[0][0].ValueKind == JsonValueKind.Array)
            {
                level = level[0];
            }

            ring = level.Clone();
        }
        catch (JsonException)
        {
            return null;
        }

        var points = new List<GeoPoint>();
        foreach (var pair in ring.EnumerateArray())
        {
            if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() < 2 ||
                pair[0].ValueKind != JsonValueKind.Number || pair[1].ValueKind != JsonValueKind.Number)
            {
                return null;
            }

            var longitude = pair[0].GetDouble();
            var latitude = pair[1].GetDouble();
            if (!IsPlausible(latitude, longitude))
            {
                return null;
            }

            points.Add(new GeoPoint(latitude, longitude));
        }

        return points;
    }

    /// <summary>The <c>coordinates</c> member, through a Feature's <c>geometry</c>, or the array itself.</summary>
    private static JsonElement? FindCoordinates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return element;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (element.TryGetProperty("coordinates", out var coordinates))
        {
            return coordinates;
        }

        if (element.TryGetProperty("geometry", out var geometry))
        {
            return FindCoordinates(geometry);
        }

        // A FeatureCollection: the first feature is the boundary. A field holding several shapes has no
        // agreed meaning, and picking one is better than refusing the whole site's punches.
        if (element.TryGetProperty("features", out var features) &&
            features.ValueKind == JsonValueKind.Array && features.GetArrayLength() > 0)
        {
            return FindCoordinates(features[0]);
        }

        return null;
    }

    private static bool TryReadDegrees(string text, out double value) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    /// <summary>
    /// Real degrees, and not the 0,0 that an unfilled form posts -- a boundary through the Gulf of Guinea is
    /// always a mistake, and accepting it would refuse every punch at the site.
    /// </summary>
    private static bool IsPlausible(double latitude, double longitude) =>
        latitude is >= -90 and <= 90 &&
        longitude is >= -180 and <= 180 &&
        (latitude != 0 || longitude != 0);

    // ---------------------------------------------------------------- geometry

    private static bool Same(GeoPoint a, GeoPoint b) =>
        Math.Abs(a.Latitude - b.Latitude) < OnEdgeToleranceDegrees &&
        Math.Abs(a.Longitude - b.Longitude) < OnEdgeToleranceDegrees;

    private static bool IsOnSegment(double latitude, double longitude, GeoPoint a, GeoPoint b)
    {
        var cross = (b.Longitude - a.Longitude) * (latitude - a.Latitude) -
                    (b.Latitude - a.Latitude) * (longitude - a.Longitude);
        if (Math.Abs(cross) > OnEdgeToleranceDegrees)
        {
            return false;
        }

        return longitude >= Math.Min(a.Longitude, b.Longitude) - OnEdgeToleranceDegrees &&
               longitude <= Math.Max(a.Longitude, b.Longitude) + OnEdgeToleranceDegrees &&
               latitude >= Math.Min(a.Latitude, b.Latitude) - OnEdgeToleranceDegrees &&
               latitude <= Math.Max(a.Latitude, b.Latitude) + OnEdgeToleranceDegrees;
    }

    /// <summary>Distance from the grid origin (the punch) to the segment a-b, both already in metres.</summary>
    private static double DistanceToSegment((double x, double y) a, (double x, double y) b)
    {
        var dx = b.x - a.x;
        var dy = b.y - a.y;
        var lengthSquared = dx * dx + dy * dy;

        if (lengthSquared <= double.Epsilon)
        {
            return Math.Sqrt(a.x * a.x + a.y * a.y);
        }

        // Where the foot of the perpendicular falls along the segment, clamped to its ends.
        var t = Math.Clamp(-(a.x * dx + a.y * dy) / lengthSquared, 0, 1);
        var nearestX = a.x + t * dx;
        var nearestY = a.y + t * dy;

        return Math.Sqrt(nearestX * nearestX + nearestY * nearestY);
    }
}
