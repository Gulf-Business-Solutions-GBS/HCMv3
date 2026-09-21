using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.TimeManagement.Services;

/// <summary>One of today's punches, reduced to what the button rules read: its type code and time of day.</summary>
public readonly record struct GeoPunchEvent(string TransTypeCode, TimeSpan Time);

/// <summary>
/// The shape of today's duty as the button rules see it. <see cref="BreakStart"/>/<see cref="BreakEnd"/> are
/// set only when the duty has a break shift, which switches the rules to their split-shift branch.
/// </summary>
public sealed class GeoPunchDutyShape
{
    public TimeSpan DutyStart { get; init; }

    public TimeSpan DutyEnd { get; init; }

    public TimeSpan? BreakStart { get; init; }

    public TimeSpan? BreakEnd { get; init; }

    public bool IsBreakShift => BreakStart.HasValue && BreakEnd.HasValue;
}

/// <summary>Which of the four punch buttons the employee may press right now.</summary>
public sealed class GeoPunchButtons
{
    public bool CanClockIn { get; init; }

    public bool CanClockOut { get; init; }

    public bool CanOvertimeIn { get; init; }

    public bool CanOvertimeOut { get; init; }

    /// <summary>Whether <paramref name="transTypeCode"/> is one of the buttons currently enabled.</summary>
    public bool Allows(string transTypeCode) => transTypeCode switch
    {
        TransTypeCodes.ClockIn => CanClockIn,
        TransTypeCodes.ClockOut => CanClockOut,
        TransTypeCodes.OvertimeIn => CanOvertimeIn,
        TransTypeCodes.OvertimeOut => CanOvertimeOut,
        _ => false
    };
}

/// <summary>
/// The rules behind the self-service geo punch screen (legacy <c>HCM10.ESS</c> <c>GeoLocationController</c>),
/// kept free of any data access so each one can be asserted on its own.
///
/// <para>
/// Legacy held the button logic as <c>ViewBag</c> flags named for the DISABLED state (<c>PunchedIn</c> means
/// "the Clock In button is greyed out"). The port keeps those names inside
/// <see cref="ResolveButtons"/> so it can be read line for line against the legacy <c>Index</c> action, and
/// inverts them only on the way out.
/// </para>
///
/// <para>
/// One deliberate change: legacy walked the overtime punches ordered by <c>TransDate</c>, which is the same
/// for every punch of the day, so the "last" overtime punch was whichever the database returned last. They
/// are ordered by time here, which is what that loop meant.
/// </para>
/// </summary>
public static class GeoPunchRules
{
    /// <summary>Mean Earth radius in kilometres, as legacy <c>Distance</c> used.</summary>
    public const double EarthRadiusKm = 6371;

    /// <summary>
    /// Great-circle (haversine) distance in metres between two coordinates. Legacy computed this in the
    /// browser round trip <c>GetLocationDistance</c> and trusted whatever figure came back with the punch; it
    /// is computed on the server here, so a client cannot claim to be in range.
    /// </summary>
    public static double DistanceMeters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        var lat1 = ToRadians(latitude1);
        var lat2 = ToRadians(latitude2);
        var dLat = lat2 - lat1;
        var dLon = ToRadians(longitude2) - ToRadians(longitude1);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dLon / 2), 2);

        return 2 * Math.Asin(Math.Sqrt(a)) * EarthRadiusKm * 1000;
    }

    /// <summary>
    /// Legacy <c>hideshowbutton</c> / <c>PerformPunchingEvent</c>: inside the radius (inclusive) is in range;
    /// outside it the punch goes through only when the tenant's self-service setting
    /// <c>AllowPunchOutsideRange</c> is on.
    /// </summary>
    public static GeoPunchRangeVerdict JudgeRange(double distanceMeters, double rangeMeters, bool allowOutsideRange)
    {
        if (distanceMeters <= rangeMeters)
        {
            return GeoPunchRangeVerdict.InRange;
        }

        return allowOutsideRange ? GeoPunchRangeVerdict.OutOfRangeAllowed : GeoPunchRangeVerdict.OutOfRangeRefused;
    }

    /// <summary>
    /// Which buttons are enabled given today's punches, today's duty and the current time of day. Port of the
    /// "Determine Punch/OT State" region of legacy <c>GeoLocationController.Index</c>.
    /// </summary>
    public static GeoPunchButtons ResolveButtons(
        IReadOnlyCollection<GeoPunchEvent> todaysPunches,
        GeoPunchDutyShape duty,
        TimeSpan now)
    {
        todaysPunches ??= Array.Empty<GeoPunchEvent>();

        // Named for the DISABLED state, as legacy's ViewBag flags were.
        bool punchedIn;
        bool punchedOut;
        bool otIn;
        bool otOut;

        if (duty != null && duty.IsBreakShift)
        {
            (punchedIn, punchedOut, otIn, otOut) = ResolveBreakShift(todaysPunches, duty, now);
        }
        else
        {
            (punchedIn, punchedOut, otIn, otOut) = ResolveRegular(todaysPunches);
        }

        return new GeoPunchButtons
        {
            CanClockIn = !punchedIn,
            CanClockOut = !punchedOut,
            CanOvertimeIn = !otIn,
            CanOvertimeOut = !otOut
        };
    }

    private static (bool punchedIn, bool punchedOut, bool otIn, bool otOut) ResolveRegular(
        IReadOnlyCollection<GeoPunchEvent> punches)
    {
        var punchedIn = punches.Any(p => Is(p, TransTypeCodes.ClockIn));
        var punchedOut = punches.Any(p => Is(p, TransTypeCodes.ClockOut));

        // Nothing yet today: only Clock In is offered.
        if (!punchedIn && !punchedOut)
        {
            punchedOut = true;
        }

        var (otIn, otOut) = LastOvertimeState(punches);

        if (punchedIn && punchedOut && !otIn)
        {
            otIn = false;
            otOut = true;
        }

        if (punchedIn && punchedOut && otIn)
        {
            otIn = true;
            otOut = false;
        }

        // Mid-shift (exactly one of In/Out still open): overtime is not offered at all.
        if (!punchedIn && punchedOut || punchedIn && !punchedOut)
        {
            otIn = true;
            otOut = true;
        }

        return (punchedIn, punchedOut, otIn, otOut);
    }

    private static (bool punchedIn, bool punchedOut, bool otIn, bool otOut) ResolveBreakShift(
        IReadOnlyCollection<GeoPunchEvent> punches,
        GeoPunchDutyShape duty,
        TimeSpan now)
    {
        var punchedIn = false;
        var punchedOut = false;
        var firstClockIn = false;
        var firstClockOut = false;
        var secondClockIn = false;
        var secondClockOut = false;

        // Overtime is closed on a split shift until both halves are done and the duty has ended.
        var otIn = true;
        var otOut = true;

        // Legacy's tolerances, kept exactly: the first half runs from an hour before duty start until two
        // hours after the break begins; the second half starts an hour before the break ends.
        var breakStart = duty.BreakStart!.Value + TimeSpan.FromHours(2);
        var breakEnd = duty.BreakEnd!.Value - TimeSpan.FromHours(1);
        var dutyStart = duty.DutyStart - TimeSpan.FromHours(1);
        var dutyEnd = duty.DutyEnd;

        var firstHalf = punches.Where(p => p.Time >= dutyStart && p.Time <= breakStart).ToList();

        if (firstHalf.Any(p => Is(p, TransTypeCodes.ClockIn)))
        {
            punchedIn = true;
            firstClockIn = true;
        }

        if (firstHalf.Any(p => Is(p, TransTypeCodes.ClockOut)))
        {
            punchedOut = true;
            firstClockOut = true;
        }

        if (!punchedIn && !punchedOut && now <= breakStart)
        {
            punchedOut = true;
            punchedIn = false;
        }
        else if (punchedIn && !punchedOut && now <= breakStart)
        {
            punchedOut = false;
        }
        else
        {
            punchedOut = true;
            punchedIn = true;
        }

        if (firstClockIn && firstClockOut && now >= breakEnd)
        {
            punchedIn = false;

            var secondHalf = punches.Where(p => p.Time >= breakEnd).ToList();

            if (secondHalf.Any(p => Is(p, TransTypeCodes.ClockIn)))
            {
                punchedIn = true;
                secondClockIn = true;
            }

            if (punchedIn)
            {
                punchedOut = false;

                if (secondHalf.Any(p => Is(p, TransTypeCodes.ClockOut)))
                {
                    secondClockOut = true;
                    punchedOut = true;
                }
            }

            if (!punchedIn && now > dutyEnd)
            {
                punchedIn = true;
                punchedOut = true;
            }
        }

        if (firstClockIn && firstClockOut && secondClockIn && secondClockOut && now > dutyEnd)
        {
            (otIn, otOut) = LastOvertimeState(punches);
        }

        return (punchedIn, punchedOut, otIn, otOut);
    }

    /// <summary>
    /// The overtime buttons implied by the last overtime punch: after OT In only OT Out is open, and
    /// otherwise (no OT punch, or last was OT Out) only OT In is open.
    /// </summary>
    private static (bool otIn, bool otOut) LastOvertimeState(IEnumerable<GeoPunchEvent> punches)
    {
        var otIn = false;
        var otOut = false;

        foreach (var punch in punches
                     .Where(p => Is(p, TransTypeCodes.OvertimeIn) || Is(p, TransTypeCodes.OvertimeOut))
                     .OrderBy(p => p.Time))
        {
            if (Is(punch, TransTypeCodes.OvertimeIn))
            {
                otIn = true;
                otOut = false;
            }
            else
            {
                otIn = false;
                otOut = true;
            }
        }

        if (!otIn && !otOut)
        {
            otOut = true;
        }

        return (otIn, otOut);
    }

    private static bool Is(GeoPunchEvent punch, string code) =>
        string.Equals(punch.TransTypeCode?.Trim(), code, StringComparison.OrdinalIgnoreCase);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
