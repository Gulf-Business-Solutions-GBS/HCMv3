using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HCMv3.TimeManagement;

namespace HCMv3.Ess.Dto;

/// <summary>
/// Everything the geo punch screen needs to draw itself: which buttons are open, where the employee may punch
/// from, what the tenant's settings ask of the screen, and today's punches. Port of the <c>ViewBag</c> that
/// legacy <c>GeoLocationController.Index</c> filled.
/// </summary>
public class MyGeoPunchContextDto
{
    public bool CanClockIn { get; set; }

    public bool CanClockOut { get; set; }

    public bool CanOvertimeIn { get; set; }

    public bool CanOvertimeOut { get; set; }

    /// <summary>The employee's current sub group pays overtime (legacy <c>IsOTAllowed</c>); when false the
    /// overtime buttons are not shown at all.</summary>
    public bool IsOvertimeAllowed { get; set; }

    /// <summary>False when no duty period covers today. Legacy refused such a punch silently; the screen
    /// says so up front instead.</summary>
    public bool HasDutyPeriodToday { get; set; }

    /// <summary>The work locations the employee may punch from right now. Empty means an open-location
    /// punch: no work location is chosen and no range applies (legacy <c>AllowOpenLocationPunch</c>).</summary>
    public List<MyGeoPunchWorkLocationDto> WorkLocations { get; set; } = new();

    /// <summary>Pre-selected work location: the employee's own, when the time policy restricts punching to it.</summary>
    public int? DefaultWorkLocationId { get; set; }

    public bool IsOpenLocation { get; set; }

    /// <summary>Tenant allows geo selfies AND the employee's time policy requires a face.</summary>
    public bool RequiresSelfie { get; set; }

    public bool AllowPunchOutsideRange { get; set; }

    public bool AutoLogoutAfterPunch { get; set; }

    /// <summary>Hide the latitude/longitude/distance panel.</summary>
    public bool HideLocationInfo { get; set; }

    /// <summary>Hide today's punch list.</summary>
    public bool HidePunchList { get; set; }

    public DateTime ServerTime { get; set; }

    public List<MyGeoPunchDto> TodaysPunches { get; set; } = new();
}

public class MyGeoPunchWorkLocationDto
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    /// <summary>Allowed radius in metres.</summary>
    public double GeoRange { get; set; }

    /// <summary>
    /// A site boundary is drawn for this location and the tenant has geo fencing on, so whether the punch
    /// counts as "at work" is decided by the shape and not by <see cref="GeoRange"/>. The screen says
    /// "site boundary" rather than showing a radius the punch is not judged against.
    /// </summary>
    public bool HasGeoFence { get; set; }

    /// <summary>The employee's own work location, as opposed to an extended one.</summary>
    public bool IsDefault { get; set; }
}

/// <summary>One of today's punches, as listed under the buttons (legacy <c>_TimeEvents</c> partial).</summary>
public class MyGeoPunchDto
{
    public int Id { get; set; }

    public string TransTypeCode { get; set; }

    public string TransTypeNameEn { get; set; }

    public string TransTypeNameAr { get; set; }

    public DateTime TransDate { get; set; }

    public DateTime TransTime { get; set; }

    public string WorkLocationNameEn { get; set; }

    public string WorkLocationNameAr { get; set; }

    public bool PunchInRange { get; set; }

    /// <summary>Metres beyond the allowed radius; zero when in range.</summary>
    public double RangeDifference { get; set; }

    public bool HasSelfie { get; set; }
}

/// <summary>Where the employee is standing, to be judged against a work location before they punch.</summary>
public class CheckMyGeoPunchLocationInput
{
    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    /// <summary>Required unless the punch is open-location.</summary>
    public int? WorkLocationId { get; set; }
}

public class MyGeoPunchLocationCheckDto
{
    /// <summary>
    /// Metres from the work location; zero for an open-location punch. When <see cref="UsesGeoFence"/> is
    /// true this is instead the distance to the nearest point of the site boundary, and zero means inside
    /// it -- a shape has no centre to measure from.
    /// </summary>
    public double DistanceMeters { get; set; }

    /// <summary>
    /// The work location's allowed radius in metres; null for an open-location punch and null when
    /// <see cref="UsesGeoFence"/> is true, because no radius is being applied.
    /// </summary>
    public double? RangeMeters { get; set; }

    /// <summary>A site boundary decided this verdict, rather than the radius.</summary>
    public bool UsesGeoFence { get; set; }

    public GeoPunchRangeVerdict Verdict { get; set; }
}

public class RecordMyGeoPunchInput
{
    /// <summary>
    /// Which button was pressed: <c>TransTypeCodes.ClockIn</c> ("1"), <c>ClockOut</c> ("2"),
    /// <c>OvertimeIn</c> ("3") or <c>OvertimeOut</c> ("4").
    /// </summary>
    [Required]
    [StringLength(10)]
    public string TransTypeCode { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    /// <summary>Required unless the punch is open-location.</summary>
    public int? WorkLocationId { get; set; }

    /// <summary>
    /// The selfie as a <c>data:image/jpeg;base64,...</c> (or png) URL. Required when
    /// <see cref="MyGeoPunchContextDto.RequiresSelfie"/> is true, ignored otherwise.
    /// </summary>
    [StringLength(MyGeoPunchConsts.MaxSelfieDataUrlLength)]
    public string Selfie { get; set; }
}

public class MyGeoPunchResultDto
{
    public int Id { get; set; }

    public bool PunchInRange { get; set; }

    public double DistanceMeters { get; set; }

    public double RangeDifference { get; set; }

    public bool AutoLogout { get; set; }
}

public class MyGeoPunchSelfieDto
{
    /// <summary>The stored selfie as a data URL, ready for an <c>img</c> tag; null when there is none.</summary>
    public string DataUrl { get; set; }
}

public static class MyGeoPunchConsts
{
    /// <summary>Largest decoded selfie accepted, in bytes. A 800x500 JPEG snapshot is well under 200 KB.</summary>
    public const int MaxSelfieBytes = 2 * 1024 * 1024;

    /// <summary>Base64 is 4/3 the size of the bytes, plus the data URL prefix.</summary>
    public const int MaxSelfieDataUrlLength = MaxSelfieBytes / 3 * 4 + 100;
}
