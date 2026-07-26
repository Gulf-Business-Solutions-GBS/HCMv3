using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;

namespace HCMv3.TimeManagement.Services;

/// <inheritdoc cref="IRosterService"/>
/// <remarks>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.Services.RosterService</c> (a static-methods-only
/// class operating on an XPO <c>Session</c>). This is the canonical multi-week roster/duty resolver.
///
/// RELATIONSHIP TO <c>TimeDataService</c>: <c>HCMv3.TimeManagement.Services.TimeDataService</c> (an earlier
/// wave) already contains an inline, private <c>ResolveRosterDutyPeriodId</c> helper that resolves ONLY the
/// winning <see cref="DutyPeriod"/> id for a date (via <see cref="RosterGroupDetail"/>/<see cref="RosterGroupSchedule"/>),
/// explicitly documented there as a simplified stand-in because the Roster subsystem "was never ported".
/// This class supersedes that simplification for the specific <c>GetRosterDutyPeriod</c>/<c>GetRosterDuty</c>
/// surface: it is the full, faithful priority-chain + multi-week rotation port. <c>TimeDataService</c> itself
/// is NOT refactored to call into this class (that would be a materially larger change than this task's
/// scope -- <c>TimeDataService</c>'s own duty-resolution call sites feed a different, simpler "which classic
/// DutyPeriod applies" question used purely for holiday/weekly-off/rest-day date-list calculations, not a
/// Duty-per-day roster rotation); left as a follow-up note for a future wave to consider consolidating.
///
/// PRIORITY ORDER (faithful to legacy <c>GetRosterDuty</c>):
/// <list type="number">
/// <item><description><see cref="EmployeeSubstituteDuty"/> for the exact date -- wins outright.</description></item>
/// <item><description><see cref="EmployeeDutyPeriod"/> covering the date.</description></item>
/// <item><description><see cref="RosterGroupDetail"/> (employee's roster group for the date) +
/// <see cref="RosterGroupSchedule"/> (that group's duty period template for the date).</description></item>
/// </list>
///
/// StartPoint / multi-week rotation, both paths: legacy's <c>GetRosterDuty</c> computes the day's Duty via a
/// multi-week rotation formula (<see cref="DutyPeriodDetail"/> Week/Day1Duty..Day7Duty + a "StartPoint"
/// offset + a "StartDate" anchor) for BOTH the EmployeeDutyPeriod and RosterGroupSchedule resolution paths.
/// This port now carries the equivalent <see cref="EmployeeDutyPeriod.StartPoint"/> column, so both paths
/// apply the same rotation formula, faithful to legacy:
/// <list type="bullet">
/// <item><description>When the winning source is <see cref="EmployeeDutyPeriod"/> and its
/// <see cref="EmployeeDutyPeriod.StartPoint"/> is greater than 0, the full multi-week rotation formula is
/// used (anchored at <see cref="EmployeeDutyPeriod.StartDate"/>). When <c>StartPoint</c> is 0 or unset (no
/// rotation configured for this assignment), this port falls back to resolving Duty via the classic
/// day-of-week FK on <see cref="DutyPeriod"/> (Sunday..SaturdayDuty) -- the same fallback <c>TimeDataService</c>
/// already uses for its non-Roster path.</description></item>
/// <item><description>When the winning source is <see cref="RosterGroupSchedule"/> (which always carries
/// <see cref="RosterGroupSchedule.StartPoint"/>), the full multi-week rotation formula is used, exactly as
/// legacy -- this is the genuine "Roster" (<c>EnableRosterDuty</c>) scenario the legacy class exists for.</description></item>
/// </list>
///
/// <see cref="UnitOfWorkAttribute"/> at the class level, matching <c>SysSettingsAccessor</c>/<c>TimeDataService</c>
/// (this domain service is independently callable, opening its own unit of work when not already inside one).
/// </remarks>
[UnitOfWork]
public class RosterService : HCMv3DomainServiceBase, IRosterService
{
    private readonly IRepository<DutyPeriod> _dutyPeriodRepository;
    private readonly IRepository<DutyPeriodDetail> _dutyPeriodDetailRepository;
    private readonly IRepository<Duty> _dutyRepository;
    private readonly IRepository<EmployeeDutyPeriod> _employeeDutyPeriodRepository;
    private readonly IRepository<EmployeeSubstituteDuty> _employeeSubstituteDutyRepository;
    private readonly IRepository<RosterGroupDetail> _rosterGroupDetailRepository;
    private readonly IRepository<RosterGroupSchedule> _rosterGroupScheduleRepository;

    public RosterService(
        IRepository<DutyPeriod> dutyPeriodRepository,
        IRepository<DutyPeriodDetail> dutyPeriodDetailRepository,
        IRepository<Duty> dutyRepository,
        IRepository<EmployeeDutyPeriod> employeeDutyPeriodRepository,
        IRepository<EmployeeSubstituteDuty> employeeSubstituteDutyRepository,
        IRepository<RosterGroupDetail> rosterGroupDetailRepository,
        IRepository<RosterGroupSchedule> rosterGroupScheduleRepository)
    {
        _dutyPeriodRepository = dutyPeriodRepository;
        _dutyPeriodDetailRepository = dutyPeriodDetailRepository;
        _dutyRepository = dutyRepository;
        _employeeDutyPeriodRepository = employeeDutyPeriodRepository;
        _employeeSubstituteDutyRepository = employeeSubstituteDutyRepository;
        _rosterGroupDetailRepository = rosterGroupDetailRepository;
        _rosterGroupScheduleRepository = rosterGroupScheduleRepository;
    }

    // NOTE: HCMv3.Core has no reference to EntityFrameworkCore (per architecture rules), so the usual
    // Microsoft.EntityFrameworkCore async IQueryable extensions are not available here -- same documented
    // constraint as SysSettingsAccessor/AccrualService/TimeDataService. Plain synchronous System.Linq over
    // IRepository<T>.GetAll() is used instead, with results wrapped in Task.FromResult for interface
    // consistency.

    public Task<DutyPeriod> GetRosterDutyPeriodAsync(int employeeId, DateTime dayDate)
    {
        var dutyPeriodId = ResolveDutyPeriodId(employeeId, dayDate);
        if (dutyPeriodId == null)
        {
            return Task.FromResult<DutyPeriod>(null);
        }

        var dutyPeriod = _dutyPeriodRepository.GetAll().FirstOrDefault(dp => dp.Id == dutyPeriodId.Value);
        return Task.FromResult(dutyPeriod);
    }

    public Task<Duty> GetRosterDutyAsync(int employeeId, DateTime dayDate, List<EmployeeSubstituteDuty> empSubstituteDutyList = null, bool throwException = true)
    {
        // Priority 1: EmployeeSubstituteDuty for the exact date wins outright (cache-list-aware, same as legacy).
        var substituteDuty = empSubstituteDutyList == null
            ? _employeeSubstituteDutyRepository.GetAll()
                .Where(r => r.EmployeeId == employeeId && r.SubstituteDate == dayDate)
                .Select(r => r.Duty)
                .FirstOrDefault()
            : empSubstituteDutyList.Where(r => r.SubstituteDate == dayDate).Select(r => r.Duty).FirstOrDefault();

        if (substituteDuty != null)
        {
            return Task.FromResult(substituteDuty);
        }

        // Priority 2: EmployeeDutyPeriod covering the date -- see class remarks on the rotation formula.
        var empDutyPeriod = _employeeDutyPeriodRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= dayDate && r.EndDate >= dayDate)
            .Select(r => new { r.DutyPeriodId, r.StartDate, r.StartPoint })
            .FirstOrDefault();

        if (empDutyPeriod != null)
        {
            if (empDutyPeriod.StartPoint > 0)
            {
                var rotationDuty = ResolveRotationDuty(empDutyPeriod.DutyPeriodId, empDutyPeriod.StartDate, empDutyPeriod.StartPoint, dayDate, throwException);
                return Task.FromResult(rotationDuty);
            }

            return Task.FromResult(GetDayOfWeekDuty(empDutyPeriod.DutyPeriodId, dayDate.DayOfWeek));
        }

        // Priority 3: RosterGroupDetail + RosterGroupSchedule -- genuine multi-week rotation.
        var rosterGroupId = _rosterGroupDetailRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= dayDate && r.EndDate >= dayDate)
            .Select(r => (int?)r.RosterGroupId)
            .FirstOrDefault();

        if (rosterGroupId == null)
        {
            return Task.FromResult<Duty>(null);
        }

        var rosterGroupSchedule = _rosterGroupScheduleRepository.GetAll()
            .Where(r => r.RosterGroupId == rosterGroupId.Value && r.StartDate <= dayDate && r.EndDate >= dayDate)
            .Select(r => new { r.DutyPeriodId, r.StartDate, r.StartPoint })
            .FirstOrDefault();

        if (rosterGroupSchedule == null || rosterGroupSchedule.StartPoint <= 0)
        {
            return Task.FromResult<Duty>(null);
        }

        var resolvedDuty = ResolveRotationDuty(rosterGroupSchedule.DutyPeriodId, rosterGroupSchedule.StartDate, rosterGroupSchedule.StartPoint, dayDate, throwException);
        return Task.FromResult(resolvedDuty);
    }

    private int? ResolveDutyPeriodId(int employeeId, DateTime dayDate)
    {
        var empDutyPeriodId = _employeeDutyPeriodRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= dayDate && r.EndDate >= dayDate)
            .Select(r => (int?)r.DutyPeriodId)
            .FirstOrDefault();

        if (empDutyPeriodId != null)
        {
            return empDutyPeriodId;
        }

        var rosterGroupId = _rosterGroupDetailRepository.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.StartDate <= dayDate && r.EndDate >= dayDate)
            .Select(r => (int?)r.RosterGroupId)
            .FirstOrDefault();

        if (rosterGroupId == null)
        {
            return null;
        }

        return _rosterGroupScheduleRepository.GetAll()
            .Where(r => r.RosterGroupId == rosterGroupId.Value && r.StartDate <= dayDate && r.EndDate >= dayDate)
            .Select(r => (int?)r.DutyPeriodId)
            .FirstOrDefault();
    }

    private Duty GetDayOfWeekDuty(int dutyPeriodId, DayOfWeek dayOfWeek)
    {
        var dutyPeriod = _dutyPeriodRepository.GetAll().FirstOrDefault(dp => dp.Id == dutyPeriodId);
        if (dutyPeriod == null)
        {
            return null;
        }

        var dutyId = dayOfWeek switch
        {
            DayOfWeek.Sunday => dutyPeriod.SundayDutyId,
            DayOfWeek.Monday => dutyPeriod.MondayDutyId,
            DayOfWeek.Tuesday => dutyPeriod.TuesdayDutyId,
            DayOfWeek.Wednesday => dutyPeriod.WednesdayDutyId,
            DayOfWeek.Thursday => dutyPeriod.ThursdayDutyId,
            DayOfWeek.Friday => dutyPeriod.FridayDutyId,
            _ => dutyPeriod.SaturdayDutyId
        };

        return dutyId == null ? null : _dutyRepository.GetAll().FirstOrDefault(d => d.Id == dutyId.Value);
    }

    /// <summary>
    /// Faithful port of legacy's multi-week rotation formula from <c>RosterService.GetRosterDuty</c>
    /// (the block starting with <c>int weekCount = rst_dutyPeriod.DutyPeriodDetail.Count();</c>). The
    /// legacy <c>GetMemberValue("Day" + pickDuty + "Duty")</c> reflection call is replaced with a type-safe
    /// switch over the ported entity's <c>Day1DutyId</c>..<c>Day7DutyId</c> properties (confirmed exactly 7
    /// day slots on <see cref="DutyPeriodDetail"/>).
    /// </summary>
    private Duty ResolveRotationDuty(int dutyPeriodId, DateTime rstStartDate, int rstStartPoint, DateTime dayDate, bool throwException)
    {
        var dutyPeriod = _dutyPeriodRepository.GetAll().FirstOrDefault(dp => dp.Id == dutyPeriodId)
            ?? throw new InvalidOperationException($"DutyPeriod Id '{dutyPeriodId}' not found.");

        var weekDetails = _dutyPeriodDetailRepository.GetAll().Where(r => r.DutyPeriodId == dutyPeriodId).ToList();
        var weekCount = weekDetails.Count;
        if (weekCount == 0)
        {
            throw new Exception($"DutyPeriod : '{dutyPeriod.NameEn}' has no week information filled. Please fill proper week detail.");
        }

        var dutyCycle = weekCount * 7;
        var daysDiff = (int)dayDate.Subtract(rstStartDate).TotalDays + 1;
        var dutyDay = daysDiff % dutyCycle;

        var nextCount = rstStartPoint + dutyDay - 1;
        double newDutyDay = dutyDay > 0
            ? (nextCount <= dutyCycle ? nextCount : nextCount - dutyCycle)
            : (nextCount != 0 ? nextCount : dutyCycle);

        var curWeek = (int)Math.Ceiling(newDutyDay / 7);
        var pickDuty = (int)(newDutyDay - ((curWeek - 1) * 7));

        var dutyPeriodDetail = weekDetails.FirstOrDefault(r => r.Week == curWeek);
        if (dutyPeriodDetail == null)
        {
            if (throwException)
            {
                throw new Exception($"DutyPeriod : '{dutyPeriod.NameEn}' has wrong week number assigned. Please fill proper Duty period detail for week : '{curWeek}'.");
            }

            return null;
        }

        var dutyId = pickDuty switch
        {
            1 => dutyPeriodDetail.Day1DutyId,
            2 => dutyPeriodDetail.Day2DutyId,
            3 => dutyPeriodDetail.Day3DutyId,
            4 => dutyPeriodDetail.Day4DutyId,
            5 => dutyPeriodDetail.Day5DutyId,
            6 => dutyPeriodDetail.Day6DutyId,
            7 => dutyPeriodDetail.Day7DutyId,
            _ => (int?)null
        };

        return dutyId == null ? null : _dutyRepository.GetAll().FirstOrDefault(d => d.Id == dutyId.Value);
    }
}
