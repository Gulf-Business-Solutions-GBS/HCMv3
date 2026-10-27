using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// The Time Engine: builds one <see cref="TimeRegister"/> row per employee per day for a requested date range
/// (ported from the legacy standalone console project <c>HCM10.TimeEngine</c>, specifically
/// <c>Program.CreateTimeRegister</c>).
///
/// <para><b>Port shape.</b> The legacy <c>Main()</c>/<c>args[]</c>/connection-string/XAF-type-registration
/// plumbing is throwaway host wiring and is NOT ported; only the algorithm inside <c>CreateTimeRegister</c> is,
/// as an ABP domain service. Legacy's per-employee <c>IObjectSpace</c> lifecycle (create/commit/dispose per
/// employee, plus "commit every 500 modified objects") is likewise not ported -- ABP's unit of work owns
/// transaction scope here.</para>
///
/// <para><b>Wave split.</b> This is wave T1 (orchestration + contracts). The per-day field computation is
/// delegated to <see cref="ITimeRegisterFieldCalculator"/> (wave T2, from legacy <c>GeneralTimeService</c>) and
/// <see cref="IPunchResolutionStrategy"/> (wave T3, from legacy <c>ActualTimeService</c>/
/// <c>PlannedTimeService</c>/<c>FlexiTimeService</c>/<c>TimesheetService</c>).</para>
///
/// <para><b>Reused, not re-derived.</b> Holiday/weekend/rest-day resolution comes from
/// <see cref="ITimeDataService"/>, roster duty resolution from <see cref="IRosterService"/>, leave/ticket
/// accrual + forfeit from <see cref="HCMv3.PayrollManagement.Services.IAccrualService"/>, and all configuration
/// from <see cref="HCMv3.SystemSettings.Services.ISysSettingsAccessor"/>. See <see cref="TimeEngineService"/>
/// remarks for the exhaustive list of deviations from legacy.</para>
/// </summary>
public interface ITimeEngineService : IDomainService
{
    /// <summary>
    /// Ported from legacy <c>Program.CreateTimeRegister(objectSpace, empCode, timeStartDate, timeEndDate,
    /// payrollArea, IsRanFromPayrollEngine)</c>.
    ///
    /// Like legacy, this never throws for per-employee or per-day failures: those are swallowed and recorded as
    /// failed <see cref="TimeLog"/> rows, and the run continues. Only a missing
    /// <see cref="TimeEngineRunInput.EndDate"/> throws (legacy <c>Main</c>'s single up-front validation).
    /// </summary>
    Task<TimeEngineRunResult> CreateTimeRegisterAsync(TimeEngineRunInput input);
}
