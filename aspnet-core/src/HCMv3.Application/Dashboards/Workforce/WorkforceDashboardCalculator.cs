using System;
using System.Collections.Generic;
using System.Linq;
using HCMv3.Dashboards.Dto;
using HCMv3.Dashboards.Workforce.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Dashboards.Workforce;

/// <summary>
/// One employee as the Workforce Dashboard needs them. <see cref="LastDayInService"/> comes only from a
/// recorded, unrevoked separation (user decision 2026-09-30: separations are the one source of exits).
/// </summary>
public record WorkforceEmployee(
    int Id,
    string Code,
    string NameEn,
    string NameAr,
    Gender Gender,
    DateTime BirthDate,
    int NationalityId,
    int? QualificationId,
    int? JobTitleId,
    long? OrganizationUnitId,
    DateTime JoiningDate,
    bool IsActive,
    DateTime? LastDayInService,
    SeparationType? SeparationType);

public record WorkforceLookup(int Id, string NameEn, string NameAr);

/// <summary>
/// Every number on the Workforce Dashboard, computed in memory from employees the app service has already
/// scoped. Kept free of repositories so the rules can be tested directly.
///
/// <para><b>Who counts as employed on a day.</b> Joined on or before it, and either not separated yet (their last
/// day in service is that day or later) or, with no separation on file, still active. An inactive employee with
/// no separation record has no known exit date, so they are left out of every count rather than guessed at.</para>
///
/// <para><b>Turnover</b> = leavers in the period ÷ average headcount × 100, where the average is the headcount on
/// the first day and on the last day of the period. The standard HR formula; legacy never computed it correctly
/// (its "resigned" counted expired contract rows and the division was integer).</para>
///
/// <para><b>Age and service</b> are exact completed years on the as-of date. Legacy used birth year only.</para>
/// </summary>
public class WorkforceDashboardCalculator
{
    /// <summary>Bars shown on a "top N" chart before the rest are rolled into "Others".</summary>
    public const int TopCount = 10;

    private readonly DateTime _from;
    private readonly DateTime _asOf;
    private readonly DashboardOrgTree _tree;
    private readonly IReadOnlyDictionary<int, WorkforceLookup> _nationalities;
    private readonly IReadOnlyDictionary<int, WorkforceLookup> _qualifications;
    private readonly IReadOnlyDictionary<int, WorkforceLookup> _jobTitles;

    public WorkforceDashboardCalculator(
        DateTime from,
        DateTime asOf,
        DashboardOrgTree tree,
        IEnumerable<WorkforceLookup> nationalities,
        IEnumerable<WorkforceLookup> qualifications,
        IEnumerable<WorkforceLookup> jobTitles)
    {
        _from = from.Date;
        _asOf = asOf.Date;
        _tree = tree;
        _nationalities = nationalities.ToDictionary(n => n.Id);
        _qualifications = qualifications.ToDictionary(q => q.Id);
        _jobTitles = jobTitles.ToDictionary(j => j.Id);
    }

    // ---------------------------------------------------------------- rules

    public static bool IsEmployedOn(WorkforceEmployee e, DateTime day)
    {
        if (e.JoiningDate.Date > day.Date)
        {
            return false;
        }

        return e.LastDayInService.HasValue ? e.LastDayInService.Value.Date >= day.Date : e.IsActive;
    }

    public bool JoinedInPeriod(WorkforceEmployee e) => e.JoiningDate.Date >= _from && e.JoiningDate.Date <= _asOf;

    public bool LeftInPeriod(WorkforceEmployee e) =>
        e.LastDayInService.HasValue && e.LastDayInService.Value.Date >= _from && e.LastDayInService.Value.Date <= _asOf;

    /// <summary>Completed years between two dates (a birthday or anniversary later in the year has not counted yet).</summary>
    public static int CompletedYears(DateTime from, DateTime on)
    {
        var years = on.Year - from.Year;
        if (on.Month < from.Month || (on.Month == from.Month && on.Day < from.Day))
        {
            years--;
        }

        return Math.Max(0, years);
    }

    /// <summary>Null when the birth date is missing or impossible (before 1900, or not before the as-of date).</summary>
    public int? AgeOn(WorkforceEmployee e) =>
        e.BirthDate.Year < 1900 || e.BirthDate.Date >= _asOf ? null : CompletedYears(e.BirthDate, _asOf);

    public int ServiceYearsOn(WorkforceEmployee e) => CompletedYears(e.JoiningDate, _asOf);

    public WorkforceAgeBand AgeBandOf(WorkforceEmployee e) => AgeOn(e) switch
    {
        null => WorkforceAgeBand.Unknown,
        < 21 => WorkforceAgeBand.Under21,
        <= 30 => WorkforceAgeBand.From21To30,
        <= 40 => WorkforceAgeBand.From31To40,
        <= 50 => WorkforceAgeBand.From41To50,
        _ => WorkforceAgeBand.Above50
    };

    public WorkforceServiceBand ServiceBandOf(WorkforceEmployee e) => ServiceYearsOn(e) switch
    {
        <= 4 => WorkforceServiceBand.From0To4,
        <= 10 => WorkforceServiceBand.From5To10,
        <= 15 => WorkforceServiceBand.From11To15,
        <= 19 => WorkforceServiceBand.From16To19,
        _ => WorkforceServiceBand.From20
    };

    /// <summary>The clicked-bar filters. Age and service are judged on the as-of date.</summary>
    public IEnumerable<WorkforceEmployee> ApplyAttributeFilters(IEnumerable<WorkforceEmployee> employees, WorkforceDashboardInput input)
    {
        return employees.Where(e =>
            (!input.Gender.HasValue || e.Gender == input.Gender.Value)
            && (!input.NationalityId.HasValue || e.NationalityId == input.NationalityId.Value)
            && (!input.QualificationId.HasValue || (e.QualificationId ?? 0) == input.QualificationId.Value)
            && (!input.JobTitleId.HasValue || (e.JobTitleId ?? 0) == input.JobTitleId.Value)
            && (!input.AgeBand.HasValue || AgeBandOf(e) == input.AgeBand.Value)
            && (!input.ServiceBand.HasValue || ServiceBandOf(e) == input.ServiceBand.Value));
    }

    // ---------------------------------------------------------------- figures

    public WorkforceKpisDto Kpis(IReadOnlyCollection<WorkforceEmployee> population)
    {
        var headcount = population.Where(e => IsEmployedOn(e, _asOf)).ToList();
        var leavers = population.Where(LeftInPeriod).ToList();
        var ages = headcount.Select(AgeOn).Where(a => a.HasValue).Select(a => (double)a.Value).ToList();
        var startHeadcount = population.Count(e => IsEmployedOn(e, _from));
        var averageHeadcount = (startHeadcount + headcount.Count) / 2.0;

        return new WorkforceKpisDto
        {
            Headcount = headcount.Count,
            Male = headcount.Count(e => e.Gender == Gender.Male),
            Female = headcount.Count(e => e.Gender == Gender.Female),
            GenderNotSpecified = headcount.Count(e => e.Gender == Gender.NotSpecified),
            AverageAge = ages.Count == 0 ? null : Round(ages.Average()),
            AverageServiceYears = headcount.Count == 0 ? null : Round(headcount.Average(e => (double)ServiceYearsOn(e))),
            Nationalities = headcount.Select(e => e.NationalityId).Distinct().Count(),
            Joiners = population.Count(JoinedInPeriod),
            Leavers = leavers.Count,
            Resignations = leavers.Count(e => e.SeparationType == SeparationType.Resignation),
            Terminations = leavers.Count(e => e.SeparationType == SeparationType.Termination),
            AverageHeadcount = Round(averageHeadcount),
            TurnoverPercent = Turnover(leavers.Count, averageHeadcount)
        };
    }

    /// <summary>One point per month of the period, each month clipped to the period.</summary>
    public List<WorkforceTrendPointDto> Trend(IReadOnlyCollection<WorkforceEmployee> population)
    {
        var points = new List<WorkforceTrendPointDto>();

        for (var month = new DateTime(_from.Year, _from.Month, 1); month <= _asOf; month = month.AddMonths(1))
        {
            var start = month < _from ? _from : month;
            var monthEnd = month.AddMonths(1).AddDays(-1);
            var end = monthEnd > _asOf ? _asOf : monthEnd;

            var headcountAtStart = population.Count(e => IsEmployedOn(e, start));
            var headcountAtEnd = population.Count(e => IsEmployedOn(e, end));
            var leavers = population.Count(e => e.LastDayInService.HasValue
                                                && e.LastDayInService.Value.Date >= start
                                                && e.LastDayInService.Value.Date <= end);

            points.Add(new WorkforceTrendPointDto
            {
                MonthStart = month,
                Headcount = headcountAtEnd,
                Joiners = population.Count(e => e.JoiningDate.Date >= start && e.JoiningDate.Date <= end),
                Leavers = leavers,
                TurnoverPercent = Turnover(leavers, (headcountAtStart + headcountAtEnd) / 2.0)
            });
        }

        return points;
    }

    /// <summary>Every band in order, empty ones included so the axis never jumps; Unknown only when someone is in it.</summary>
    public List<WorkforceBandCountDto> AgeBands(IReadOnlyCollection<WorkforceEmployee> headcount)
    {
        var counts = headcount.GroupBy(AgeBandOf).ToDictionary(g => g.Key, g => g.Count());

        return Enum.GetValues<WorkforceAgeBand>()
            .Where(b => b != WorkforceAgeBand.Unknown || counts.ContainsKey(b))
            .Select(b => new WorkforceBandCountDto { Band = (int)b, Count = counts.GetValueOrDefault(b) })
            .ToList();
    }

    public List<WorkforceBandCountDto> ServiceBands(IReadOnlyCollection<WorkforceEmployee> headcount)
    {
        var counts = headcount.GroupBy(ServiceBandOf).ToDictionary(g => g.Key, g => g.Count());

        return Enum.GetValues<WorkforceServiceBand>()
            .Select(b => new WorkforceBandCountDto { Band = (int)b, Count = counts.GetValueOrDefault(b) })
            .ToList();
    }

    public static List<WorkforceGenderCountDto> Genders(IReadOnlyCollection<WorkforceEmployee> headcount) =>
        headcount.GroupBy(e => e.Gender)
            .OrderBy(g => g.Key)
            .Select(g => new WorkforceGenderCountDto { Gender = g.Key, Count = g.Count() })
            .ToList();

    public List<WorkforceNamedCountDto> Nationalities(IReadOnlyCollection<WorkforceEmployee> headcount) =>
        TopN(headcount, e => e.NationalityId, _nationalities);

    public List<WorkforceNamedCountDto> Qualifications(IReadOnlyCollection<WorkforceEmployee> headcount) =>
        TopN(headcount, e => e.QualificationId ?? 0, _qualifications);

    public List<WorkforceNamedCountDto> JobTitles(IReadOnlyCollection<WorkforceEmployee> headcount) =>
        TopN(headcount, e => e.JobTitleId ?? 0, _jobTitles);

    /// <summary>
    /// The biggest <see cref="TopCount"/> groups, then everybody else as one "Others" bar. Id 0 ("not recorded")
    /// is a group like any other, so missing data stays visible instead of disappearing.
    /// </summary>
    private static List<WorkforceNamedCountDto> TopN(
        IReadOnlyCollection<WorkforceEmployee> headcount,
        Func<WorkforceEmployee, int> key,
        IReadOnlyDictionary<int, WorkforceLookup> names)
    {
        var groups = headcount.GroupBy(key)
            .Select(g =>
            {
                names.TryGetValue(g.Key, out var name);
                return new WorkforceNamedCountDto { Id = g.Key, NameEn = name?.NameEn, NameAr = name?.NameAr, Count = g.Count() };
            })
            .OrderByDescending(g => g.Count)
            .ThenBy(g => g.NameEn)
            .ToList();

        if (groups.Count <= TopCount + 1)
        {
            return groups;
        }

        var top = groups.Take(TopCount).ToList();
        top.Add(new WorkforceNamedCountDto { IsOthers = true, Count = groups.Skip(TopCount).Sum(g => g.Count) });
        return top;
    }

    /// <summary>Headcount, leavers and turnover one org level below <paramref name="selectedUnitId"/>.</summary>
    public List<WorkforceOrgUnitBucketDto> ByOrgUnit(IReadOnlyCollection<WorkforceEmployee> population, long? selectedUnitId)
    {
        var buckets = new Dictionary<(long? Id, bool Direct), List<WorkforceEmployee>>();

        foreach (var employee in population)
        {
            var bucket = _tree.BucketFor(employee.OrganizationUnitId, selectedUnitId);
            if (bucket == null)
            {
                continue;
            }

            if (!buckets.TryGetValue(bucket.Value, out var members))
            {
                buckets[bucket.Value] = members = new List<WorkforceEmployee>();
            }

            members.Add(employee);
        }

        return buckets
            .Select(b =>
            {
                var headcount = b.Value.Count(e => IsEmployedOn(e, _asOf));
                var leavers = b.Value.Count(LeftInPeriod);
                var average = (b.Value.Count(e => IsEmployedOn(e, _from)) + headcount) / 2.0;

                return new WorkforceOrgUnitBucketDto
                {
                    OrganizationUnitId = b.Key.Id,
                    IsDirectStaff = b.Key.Direct,
                    Name = b.Key.Id.HasValue ? _tree.NameOf(b.Key.Id.Value) : null,
                    HasChildren = !b.Key.Direct && b.Key.Id.HasValue && _tree.HasChildren(b.Key.Id.Value),
                    Headcount = headcount,
                    Leavers = leavers,
                    TurnoverPercent = Turnover(leavers, average)
                };
            })
            .Where(b => b.Headcount > 0 || b.Leavers > 0)
            .OrderByDescending(b => b.Headcount)
            .ThenBy(b => b.Name)
            .ToList();
    }

    public WorkforceEmployeeRowDto ToRow(WorkforceEmployee e)
    {
        _nationalities.TryGetValue(e.NationalityId, out var nationality);
        WorkforceLookup jobTitle = null;
        if (e.JobTitleId.HasValue)
        {
            _jobTitles.TryGetValue(e.JobTitleId.Value, out jobTitle);
        }

        return new WorkforceEmployeeRowDto
        {
            EmployeeId = e.Id,
            EmployeeCode = e.Code,
            EmployeeNameEn = e.NameEn,
            EmployeeNameAr = e.NameAr,
            OrganizationUnitName = e.OrganizationUnitId is long unitId ? _tree.NameOf(unitId) : null,
            JobTitleNameEn = jobTitle?.NameEn,
            JobTitleNameAr = jobTitle?.NameAr,
            NationalityNameEn = nationality?.NameEn,
            NationalityNameAr = nationality?.NameAr,
            Gender = e.Gender,
            Age = AgeOn(e),
            ServiceYears = ServiceYearsOn(e),
            JoiningDate = e.JoiningDate,
            LastDayInService = e.LastDayInService,
            SeparationType = e.SeparationType
        };
    }

    /// <summary>The employees behind a list: employed on the as-of date, joined, or left in the period.</summary>
    public IEnumerable<WorkforceEmployee> Select(IEnumerable<WorkforceEmployee> population, WorkforceEmployeeListKind kind) =>
        kind switch
        {
            WorkforceEmployeeListKind.Joiners => population.Where(JoinedInPeriod),
            WorkforceEmployeeListKind.Leavers => population.Where(LeftInPeriod),
            _ => population.Where(e => IsEmployedOn(e, _asOf))
        };

    private static double? Turnover(int leavers, double averageHeadcount) =>
        averageHeadcount <= 0 ? null : Round(leavers / averageHeadcount * 100);

    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
}
