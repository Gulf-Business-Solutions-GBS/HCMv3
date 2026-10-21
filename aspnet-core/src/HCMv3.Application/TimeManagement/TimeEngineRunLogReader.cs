using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Timing;
using HCMv3.EngineRuns;
using HCMv3.TimeManagement.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// The day-level log of one Time Engine run, read from <c>TimeLog</c> -- legacy's log grid
/// (<c>uscTimeEngine.SetLogGridDataSource</c>, one row per employee per day with its status and message) and the
/// file legacy exported from it.
///
/// <para><b>Which rows are the run's.</b> <c>TimeLog</c> has no run id; legacy did not need one because it
/// truncated the table before every run. Here a run's rows are those of the employees the run processed
/// (<see cref="EngineRunEmployee"/>) stamped between the run's start and its end -- the same "rows since the run
/// started" scoping the engine uses for its own summary. A payroll run evaluating the same employee's time inside
/// that window would add its rows too; two runs of one employee at once is what the per-area guard prevents.</para>
///
/// <para>Kept apart from <see cref="EngineRunProgressReader"/> on purpose: that reader serves the 5-second poll and
/// must never touch the engine's tables. This one is read on demand (a dialog, an export), after the employee's
/// batch has been saved.</para>
/// </summary>
public class TimeEngineRunLogReader : ITransientDependency
{
    private readonly IRepository<EngineRun, long> _engineRunRepository;
    private readonly IRepository<EngineRunEmployee, long> _engineRunEmployeeRepository;
    private readonly IRepository<TimeLog> _timeLogRepository;

    public TimeEngineRunLogReader(
        IRepository<EngineRun, long> engineRunRepository,
        IRepository<EngineRunEmployee, long> engineRunEmployeeRepository,
        IRepository<TimeLog> timeLogRepository)
    {
        _engineRunRepository = engineRunRepository;
        _engineRunEmployeeRepository = engineRunEmployeeRepository;
        _timeLogRepository = timeLogRepository;
    }

    /// <param name="runId">A Time Engine run; any other engine's run returns nothing.</param>
    /// <param name="employeeId">One employee's days only, or null for every employee of the run.</param>
    /// <param name="onlyFailed">true = failed days, false = successful days, null = all (legacy All/Successful/Failed).</param>
    /// <param name="filter">Text search on employee code, name and message.</param>
    [UnitOfWork]
    public virtual async Task<List<TimeEngineRunDayDto>> GetDaysAsync(long runId, int? employeeId, bool? onlyFailed, string filter)
    {
        var run = await _engineRunRepository.GetAll()
            .Where(r => r.Id == runId && r.EngineType == EngineType.TimeEngine)
            .Select(r => new { r.StartedAt, r.CompletedAt })
            .FirstOrDefaultAsync();

        if (run == null)
        {
            return new List<TimeEngineRunDayDto>();
        }

        var windowStart = run.StartedAt;
        var windowEnd = run.CompletedAt ?? Clock.Now;
        var text = filter.IsNullOrWhiteSpace() ? null : filter.Trim();

        var query =
            from log in _timeLogRepository.GetAll()
            join employee in _engineRunEmployeeRepository.GetAll().Where(e => e.EngineRunId == runId)
                on log.EmployeeId equals employee.EmployeeId
            where log.RunTimeStamp >= windowStart && log.RunTimeStamp <= windowEnd
            select new
            {
                log.Id,
                log.EmployeeId,
                employee.EmployeeCode,
                employee.EmployeeName,
                log.DayDate,
                log.Status,
                log.Message,
                log.RunTimeStamp
            };

        var rows = await query
            .WhereIf(employeeId.HasValue, r => r.EmployeeId == employeeId)
            .WhereIf(onlyFailed == true, r => r.Status != 0)
            .WhereIf(onlyFailed == false, r => r.Status == 0)
            .WhereIf(text != null,
                r => r.EmployeeCode.Contains(text) || r.EmployeeName.Contains(text) || r.Message.Contains(text))
            .OrderBy(r => r.EmployeeCode)
            .ThenBy(r => r.DayDate)
            .ThenBy(r => r.Id)
            .ToListAsync();

        return rows.Select(r => new TimeEngineRunDayDto
        {
            EmployeeId = r.EmployeeId,
            EmployeeCode = r.EmployeeCode,
            EmployeeName = r.EmployeeName,
            DayDate = r.DayDate,
            // Legacy TimeLog.Status: 0 = successful, 1 = failed.
            Succeeded = r.Status == 0,
            Message = r.Message,
            RunTimeStamp = r.RunTimeStamp
        }).ToList();
    }
}
