using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The <b>TIME-TYPE CUMULATION PRE-PASS</b>: the port of legacy
/// <c>HCM10.PayrollEngine/Program.cs</c> region <c>&lt;&lt; All TimeTypes Balance Cumulation &gt;&gt;</c>
/// (lines 1956-2518), which runs ONCE per (employee, retro period) BEFORE the
/// <c>PayrollOrder</c> wage-type loop and writes the <c>Number</c> of every 4xxx "day count" wage type plus
/// its 5xxx arrear twin onto the salary register. Most downstream wage rules read those numbers back off the
/// register, so this must run first -- see <see cref="PayrollEngineService"/>.
///
/// <para><b>THE 5xxx CODES ARE NOT DUPLICATE CALCULATIONS.</b> The wave brief assumed the 5xxx codes were the
/// same computation over a retro/arrear period. They are not. <i>Every single one</i> of the 5xxx call sites
/// in legacy lines 1956-2518 calls the SAME single function --
/// <c>GetArrear(objectSpace, emp, payDate, payInDate, "4xxx")</c> (legacy line 5302) -- which reads the
/// already-posted <c>Number</c> of the 4xxx code off this run's OTHER retro registers and subtracts the
/// previously-paid figure for the same <c>ForPeriod</c>. So the port is: <b>31 distinct computations + ONE
/// arrear function invoked 22 times</b>, not 31 computations run twice.</para>
///
/// <para><b>PER-CODE RECONCILIATION</b> (verified against the legacy CALL SITES, not against doc comments --
/// see the "wrong XML docs" note below). "Delegates" = this service calls an existing ported service;
/// "New" = ported here because it exists nowhere else; "Dead" = the legacy call site is fully commented
/// out and is therefore NOT ported (a live port would ADD behaviour legacy does not have).</para>
/// <list type="table">
/// <listheader><term>Code</term><description>Legacy fn (line) -&gt; disposition</description></listheader>
/// <item><term>4000</term><description><c>GetAnnualLeaveCurrentMonthPaidDays</c> (3247) -&gt; <b>New</b></description></item>
/// <item><term>4001</term><description><c>GetAbsentUnpaidDays</c> (4406) -&gt; <b>New</b> (NOT the same as 4029; see below)</description></item>
/// <item><term>4002</term><description><c>GetSickLeavePaidDays</c> (5227) -&gt; <b>Delegates</b> to <see cref="IPayrollDayCountService.GetSickLeavePaidDaysAsync"/></description></item>
/// <item><term>4003</term><description><c>GetSickLeaveUnpaidDays</c> (5254) -&gt; <b>Delegates</b> to <see cref="IPayrollDayCountService.GetSickLeaveUnpaidDaysAsync"/></description></item>
/// <item><term>4004</term><description><c>GetIndemnityDays</c> (3760) -&gt; <b>Delegates</b> to <see cref="IIndemnityCalculationService.GetIndemnityDaysAsync"/></description></item>
/// <item><term>4005</term><description><c>GetIndemnityPaidDays</c> (3828) -&gt; <b>New</b> (12-line <c>EmployeeGratuity</c> sum; not on any existing interface)</description></item>
/// <item><term>4006</term><description><c>GetIndemnityBalanceDays</c> (3872) -&gt; <b>New</b></description></item>
/// <item><term>4007</term><description><c>GetPublicHolidayDays</c> (5073) -&gt; <b>Delegates</b> to <see cref="IPayrollDayCountService.GetPublicHolidayDaysAsync"/></description></item>
/// <item><term>4008</term><description><c>GetWeeklyOffDays</c> (5103) -&gt; <b>Delegates</b> to <see cref="IPayrollDayCountService.GetWeeklyOffDaysAsync"/></description></item>
/// <item><term>4009</term><description><c>GetRestDays</c> (5196) -&gt; <b>Delegates</b> to <see cref="IPayrollDayCountService.GetRestDaysAsync"/></description></item>
/// <item><term>4010</term><description><c>GetAnnualLeaveCurrentMonthAccrual</c> (3189) -&gt; <b>New</b>. Deliberately NOT delegated to <see cref="IAccrualService"/>: legacy sums the <c>TimeRegister.AnnualLeaveAccrualPerDay</c> column the time engine already wrote, whereas every <c>IAccrualService</c> accrual method recomputes entitlement from master data. Same evaluated-vs-master split <see cref="IPayrollDayCountService"/> documents at length.</description></item>
/// <item><term>4011</term><description><c>GetActiveDays</c> (4895) -&gt; <b>Delegates</b> to <see cref="IPayrollDayCountService.GetActiveDaysAsync"/></description></item>
/// <item><term>4012</term><description><c>GetAdvanceLeavePaidDays</c> -&gt; <b>Dead</b> (legacy call site 1977-1984 fully commented out)</description></item>
/// <item><term>4013</term><description><c>GetAdvanceTicketPaidDays</c> -&gt; <b>Dead</b> (legacy 2164-2170 fully commented out)</description></item>
/// <item><term>4014</term><description><c>GetPresentDays</c> (4555) -&gt; <b>Delegates</b> to <see cref="IPayrollDayCountService.GetPresentDaysAsync"/></description></item>
/// <item><term>4015</term><description><c>GetPaidDays</c> (5282) -&gt; <b>Delegates</b> to <see cref="IPayrollDayCountService.GetPaidDaysAsync"/></description></item>
/// <item><term>4016</term><description><c>GetExtraWorkingPaidDays</c> (4981) -&gt; <b>New</b></description></item>
/// <item><term>4017</term><description><c>GetLieuPaidDays</c> (5010) -&gt; <b>New</b></description></item>
/// <item><term>4019</term><description><c>GetAdvanceLeaveRecoveryDays</c> -&gt; <b>Dead</b> (legacy 1961-1967 fully commented out)</description></item>
/// <item><term>4020</term><description><c>GetInactiveDays</c> (4952) -&gt; <b>New</b></description></item>
/// <item><term>4021</term><description><c>GetAnnualLeaveOpeningBalance</c> (3125) -&gt; <b>New</b></description></item>
/// <item><term>4022</term><description><c>GetAnnualLeaveClosingBalance</c> (3312) -&gt; <b>New</b> (pure register roll-up of 4021/4010/4000/4030/4031 + their 5xxx twins)</description></item>
/// <item><term>4023</term><description><c>GetTotalActiveDays</c> (4923) -&gt; <b>New</b></description></item>
/// <item><term>4024</term><description><c>GetTotalAbsentUnpaidDays</c> (4461) -&gt; <b>New</b></description></item>
/// <item><term>4025</term><description><c>GetTotalIndemnityDays</c> (3391) -&gt; <b>Delegates</b> to <see cref="IAccrualService.GetTotalIndemnityDaysAsync"/></description></item>
/// <item><term>4026</term><description><c>GetTotalIndemnityPaidDays</c> (3844) -&gt; <b>New</b></description></item>
/// <item><term>4027</term><description><c>GetEmployeeTicketAccrual</c> (4370) -&gt; <b>New</b> (sums the <c>TimeRegister.TicketAccrualPerDay</c> column; <see cref="IAccrualService.GetAirTicketAccrualStartEndAsync"/> only resolves the accrual PERIOD and is a different question)</description></item>
/// <item><term>4028</term><description><c>GetEmployeeTicketTotalAccrual</c> (4323) -&gt; <b>New</b></description></item>
/// <item><term>4029</term><description><c>GetAbsentDays</c> (5042) -&gt; <b>Delegates</b> to <see cref="IPayrollDayCountService.GetAbsentDaysAsync"/></description></item>
/// <item><term>4030</term><description><c>GetAnnualLeaveCurrentMonthAdjustment</c> (3218) -&gt; <b>New</b></description></item>
/// <item><term>4031</term><description><c>GetAnnualLeaveForfeitedAccrual</c> (3283) -&gt; <b>New</b>. NOT delegated to <see cref="IAccrualService.CalculateMonthlyForfeitAsync"/>: that method CREATES/updates the <c>EmployeeAbsQuotaForfeit</c> rows; this one merely SUMS the rows already committed. Delegating would re-run forfeiture inside payroll.</description></item>
/// <item><term>5000-5031</term><description><c>GetArrear</c> (5302) -&gt; <b>New</b>, ONE function, invoked once per live 4xxx code. 5005/5006/5012/5013/5019/5025/5026/5031 are <b>Dead</b> (commented out in legacy).</description></item>
/// <item><term>--</term><description><c>UpdateEmployeeIndemnityQuota</c> (2774, runs at the very END of the region, line 2514) -&gt; <b>Delegates</b> to <see cref="IIndemnityCalculationService.UpdateEmployeeIndemnityQuotaAsync"/></description></item>
/// </list>
///
/// <para><b>WAGE-CODE DOC CORRECTIONS.</b> <see cref="IPayrollDayCountService"/>'s XML comments mislabelled
/// three methods; the labels have been corrected in that file as part of this wave, and the code mapping used
/// here comes from the legacy call sites:</para>
/// <list type="bullet">
/// <item><description><c>GetSickLeavePaidDaysAsync</c> was documented as 4003; legacy line 2051 writes it to
/// <b>4002</b>.</description></item>
/// <item><description><c>GetSickLeaveUnpaidDaysAsync</c> was documented as 4005; legacy line 2071 writes it
/// to <b>4003</b>.</description></item>
/// <item><description><c>GetAbsentDaysAsync</c> was documented as "4001/4002"; legacy line 2032 passes it the
/// wage code <b>"4029"</b> and writes it to <b>4029</b>. <b>4001 is a genuinely DIFFERENT legacy function</b>
/// (<c>GetAbsentUnpaidDays</c>, line 4406) which adds unpaid-leave days, unapproved-absence leave days and
/// unpaid-leave quota adjustments on top. Treating them as one calculation would have been a real payroll
/// bug.</description></item>
/// <item><description><c>GetPaidDaysAsync</c>'s comment describes 4002 as "Paid Leave Days" and 4000 as
/// "Public Holiday". Legacy line 5288-5290 sums 4014 + 4002 + 4000, which by the corrected mapping is
/// Present Days + Sick Leave PAID Days + Annual Leave Paid Days. The arithmetic was already right; only the
/// labels were wrong.</description></item>
/// </list>
/// </summary>
public interface ITimeTypeCumulationService : IDomainService
{
    /// <summary>
    /// Runs the whole pre-pass for one (employee, retro period), in legacy's exact statement order (lines
    /// 1961-2515). Order is load-bearing: 4015 reads 4014/4002/4000 back off the register, 4022 reads
    /// 4021/4010/4000/4030/4031, 4023 reads 4011, 4024 reads 4001, 4006 reads 4025/4026, 4026 reads 4005,
    /// 4028 reads 4027.
    /// </summary>
    Task RunAsync(PayrollCalculationContext context);
}

/// <summary>
/// The legacy <c>DayType</c> enum used ONLY by <c>Program.GetSalaryRegisterDaysList</c> (legacy line 5387) to
/// select which <see cref="TimeManagement.TimeRegister"/> column the per-month breakdown row sums. Named
/// distinctly (rather than <c>DayType</c>) because it is a private detail of that one legacy switch, not a
/// domain concept.
/// </summary>
public enum SalaryRegisterDayType
{
    PublicHoliday,
    WeeklyOff,
    RestDay,
    SickLeavePaidDay,
    SickLeaveUnpaidDay,
    AnnualLeaveDay,
    AttendanceDay,
    LieuPaidDays,
    AbsentDay
}
