using System.Collections.Generic;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The site boundary format and the maths that judges a punch against it. Legacy never read
/// <c>WorkLocation.GeoFenceCoordinates</c> back, so there is no legacy behaviour to match -- these tests are
/// the specification of what the field now means.
///
/// <para>
/// The coordinates throughout are a block of Salmiya, Kuwait, about 240 m north to south and 350 m east to
/// west. Real numbers rather than a unit square, because the bugs this class can have are in the parsing and
/// in the degrees-to-metres conversion, and both only show up at a real latitude.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class GeoFence_Tests
{
    /// <summary>The Salmiya block, corners clockwise from the north-west.</summary>
    private const string SalmiyaBlock = "29.3399,48.0820; 29.3399,48.0856; 29.3377,48.0856; 29.3377,48.0820";

    private const double InsideLatitude = 29.3388;
    private const double InsideLongitude = 48.0838;

    /// <summary>Parses and fails the test if the text was not readable, so each case asserts one thing.</summary>
    private static IReadOnlyList<GeoPoint> Parse(string text)
    {
        GeoFence.TryParse(text, out var polygon).ShouldBeTrue();

        return polygon;
    }

    // ---------------------------------------------------------------- parsing

    [Fact]
    public void The_Pair_List_Format_Should_Be_Read_Latitude_First()
    {
        var polygon = Parse(SalmiyaBlock);

        polygon.Count.ShouldBe(4);
        polygon[0].Latitude.ShouldBe(29.3399);
        polygon[0].Longitude.ShouldBe(48.0820);
    }

    [Theory]
    [InlineData("29.3399,48.0820\n29.3399,48.0856\n29.3377,48.0856\n29.3377,48.0820")]
    [InlineData("29.3399,48.0820|29.3399,48.0856|29.3377,48.0856|29.3377,48.0820")]
    [InlineData("  29.3399 , 48.0820 ;  29.3399 , 48.0856 ; 29.3377 , 48.0856 ; 29.3377 , 48.0820  ")]
    public void A_List_Broken_Over_Lines_Pipes_Or_Padded_With_Spaces_Should_Still_Read(string text)
    {
        Parse(text).Count.ShouldBe(4);
    }

    [Fact]
    public void A_Ring_Written_Closed_Should_Not_Keep_The_Repeated_Corner()
    {
        // Every map tool exports the first point again at the end; keeping it would add a zero-length edge.
        Parse(SalmiyaBlock + "; 29.3399,48.0820").Count.ShouldBe(4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("29.3399,48.0820; 29.3399,48.0856")]                  // two corners are not a shape
    [InlineData("29.3399,48.0820; not a number; 29.3377,48.0856")]
    [InlineData("29.3399; 48.0820; 29.3377")]                          // single numbers, not pairs
    [InlineData("0,0; 0,0.001; 0.001,0.001")]                          // the null island an empty form posts
    [InlineData("129.3399,48.0820; 29.3399,48.0856; 29.3377,48.0856")] // latitude out of range
    public void Text_That_Is_Not_A_Boundary_Should_Be_Refused_Rather_Than_Guessed_At(string text)
    {
        GeoFence.TryParse(text, out var polygon).ShouldBeFalse();
        polygon.ShouldBeEmpty();
    }

    [Fact]
    public void A_GeoJson_Polygon_Should_Be_Read_With_Longitude_First()
    {
        // What a "draw a shape on the map" tool exports. The standard puts longitude first, and getting
        // this backwards would place every Kuwait site in the Indian Ocean.
        const string geoJson = """
                               {"type":"Polygon","coordinates":[[
                                 [48.0820,29.3399],[48.0856,29.3399],[48.0856,29.3377],[48.0820,29.3377],[48.0820,29.3399]
                               ]]}
                               """;

        var polygon = Parse(geoJson);

        polygon.Count.ShouldBe(4);
        polygon[0].Latitude.ShouldBe(29.3399);
        polygon[0].Longitude.ShouldBe(48.0820);
        GeoFence.Contains(polygon, InsideLatitude, InsideLongitude).ShouldBeTrue();
    }

    [Fact]
    public void A_GeoJson_Feature_Should_Be_Read_Through_Its_Geometry()
    {
        const string feature = """
                               {"type":"Feature","properties":{"name":"Salmiya"},"geometry":{"type":"Polygon",
                               "coordinates":[[[48.0820,29.3399],[48.0856,29.3399],[48.0856,29.3377],[48.0820,29.3377]]]}}
                               """;

        Parse(feature).Count.ShouldBe(4);
    }

    [Fact]
    public void A_Hole_In_A_GeoJson_Polygon_Should_Be_Ignored()
    {
        // Only the outer ring is used. Reading the hole's corners as more outer corners would fold the
        // shape in on itself and refuse punches from the middle of the site.
        const string withHole = """
                                {"type":"Polygon","coordinates":[
                                  [[48.0820,29.3399],[48.0856,29.3399],[48.0856,29.3377],[48.0820,29.3377]],
                                  [[48.0835,29.3390],[48.0840,29.3390],[48.0840,29.3385],[48.0835,29.3385]]
                                ]}
                                """;

        var polygon = Parse(withHole);

        polygon.Count.ShouldBe(4);
        GeoFence.Contains(polygon, 29.3387, 48.0837).ShouldBeTrue();
    }

    [Fact]
    public void Broken_Json_Should_Be_Refused_Rather_Than_Throwing()
    {
        // A half-pasted shape reaches this from a master-data field; an exception here would take down
        // the punch screen for the whole site.
        GeoFence.TryParse("{\"type\":\"Polygon\",\"coordinates\":[[[48.08", out _).ShouldBeFalse();
        GeoFence.TryParse("{\"type\":\"Point\",\"coordinates\":[48.0820,29.3399]}", out _).ShouldBeFalse();
    }

    // ---------------------------------------------------------------- inside and outside

    [Fact]
    public void A_Point_In_The_Middle_Of_The_Block_Should_Be_Inside()
    {
        GeoFence.Contains(Parse(SalmiyaBlock), InsideLatitude, InsideLongitude).ShouldBeTrue();
    }

    [Theory]
    [InlineData(29.3388, 48.0810)] // west of the block, on the street
    [InlineData(29.3388, 48.0870)] // east
    [InlineData(29.3410, 48.0838)] // north
    [InlineData(29.3360, 48.0838)] // south
    public void A_Point_Off_The_Block_Should_Be_Outside(double latitude, double longitude)
    {
        GeoFence.Contains(Parse(SalmiyaBlock), latitude, longitude).ShouldBeFalse();
    }

    [Fact]
    public void A_Point_On_The_Boundary_Should_Count_As_Inside()
    {
        // Somebody standing on the site's own fence line must be able to clock in; the knife-edge case
        // should not fall the other way because of a rounding digit.
        GeoFence.Contains(Parse(SalmiyaBlock), 29.3399, 48.0838).ShouldBeTrue();
    }

    [Fact]
    public void A_Corner_Should_Count_As_Inside()
    {
        GeoFence.Contains(Parse(SalmiyaBlock), 29.3399, 48.0820).ShouldBeTrue();
    }

    [Fact]
    public void An_L_Shaped_Compound_Should_Exclude_The_Notch()
    {
        // The whole reason a shape beats a radius: a circle wide enough to cover the far wing also covers
        // the street in the notch.
        const string lShaped = "29.3399,48.0820; 29.3399,48.0856; 29.3388,48.0856; 29.3388,48.0838; " +
                               "29.3377,48.0838; 29.3377,48.0820";

        var polygon = Parse(lShaped);

        GeoFence.Contains(polygon, 29.3394, 48.0850).ShouldBeTrue();  // in the top wing
        GeoFence.Contains(polygon, 29.3382, 48.0828).ShouldBeTrue();  // in the bottom wing
        GeoFence.Contains(polygon, 29.3382, 48.0850).ShouldBeFalse(); // in the notch
    }

    // ---------------------------------------------------------------- distance to the boundary

    [Fact]
    public void Distance_From_Outside_Should_Be_Measured_To_The_Nearest_Edge()
    {
        // 48.0810 is 0.0010 degrees of longitude west of the block's west edge, which at this latitude is
        // 0.0010 * 111320 * cos(29.34) = about 97 m.
        var metres = GeoFence.DistanceToBoundaryMeters(Parse(SalmiyaBlock), 29.3388, 48.0810);

        metres.ShouldBeInRange(90, 105);
    }

    [Fact]
    public void Distance_Should_Be_Measured_To_A_Corner_When_That_Is_Nearest()
    {
        // Diagonally off the north-west corner: the answer must be the corner distance, not the distance
        // to either edge's infinite line, which is what an unclamped projection would give.
        var metres = GeoFence.DistanceToBoundaryMeters(Parse(SalmiyaBlock), 29.3408, 48.0810);

        // 0.0009 deg latitude = 100 m north, 0.0010 deg longitude = 97 m west.
        metres.ShouldBeInRange(130, 150);
    }

    [Fact]
    public void An_Unreadable_Boundary_Should_Report_Nothing_Rather_Than_Throw()
    {
        GeoFence.Contains(null, InsideLatitude, InsideLongitude).ShouldBeFalse();
        GeoFence.DistanceToBoundaryMeters(null, InsideLatitude, InsideLongitude).ShouldBe(0);
    }
}
