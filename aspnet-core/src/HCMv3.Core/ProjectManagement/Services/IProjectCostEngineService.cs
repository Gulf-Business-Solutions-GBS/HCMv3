using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.ProjectManagement.Services;

/// <summary>
/// The Project Cost Engine: splits an already-calculated <c>SalaryRegister</c> across the projects an
/// employee worked on in the period, writing one <c>ProjectRegisterDetail</c> row per
/// (Project, WageType).
///
/// <para><b>Ported from</b> legacy <c>HCM10.Web.UserControls.ProjectManagement.uscProjectCosting</c>
/// (<c>Main</c> lines 167-308, <c>CreateProjectRegister</c> lines 309-671, <c>RunProjectCalculation</c>
/// lines 672-1287, <c>GetInitialVariables</c> lines 1308-1320, <c>SetRoundoff</c> lines 1385-1427,
/// <c>GetEmployeeList</c> lines 1747-1774).</para>
///
/// <para><b>NOT the same thing as</b> <c>HCMv3.Core/PayrollManagement/Costing</c>
/// (<c>ICostEngineService</c>/<c>IJournalVoucherBuilder</c>). That one splits payroll across GL accounts /
/// cost centres and builds a Journal Voucher for finance posting. This one splits payroll across
/// <c>ProjectManagement.Project</c> rows using each employee's <c>EmployeeProject</c> assignment and
/// <c>TimeRegister</c> day counts, and writes to <c>ProjectRegister</c>/<c>ProjectRegisterDetail</c>. They
/// share no entity and no calculation; neither supersedes the other.</para>
///
/// <para><b>DELIBERATE DEVIATIONS -- read before extending.</b></para>
/// <list type="number">
/// <item><description><b>XAF <c>IObjectSpace</c> per-employee lifecycle NOT ported</b> (legacy 361-369,
/// 623-630). ABP's unit of work owns this, same as <c>PayrollEngineService</c>.</description></item>
/// <item><description><b>XAF <c>AuditTrailService</c> plumbing NOT ported</b> (legacy 371-381, 1291-1304).
/// <see cref="ProjectCostEngineSettings.EnableAuditLogInProjectCostEngine"/> is carried but unused.</description></item>
/// <item><description><b><c>SysCreator</c>/<c>SysModifier</c> NOT ported</b> (legacy 552-566) -- superseded
/// by ABP audit columns.</description></item>
/// <item><description><b>Legacy's raw-SQL <c>SetRoundoff</c> (lines 1385-1427) is re-expressed in-memory.</b>
/// Legacy opened its own <c>SqlConnection</c> and ran an <c>UPDATE ... ROUND(...)</c> over
/// <c>ProjectRegisterDetail</c>. Raw SQL from a domain service is forbidden here, so the SAME digit count
/// (the employee's payroll-area currency <c>DecimalCount</c>, via <c>IPayrollRoundingService</c>
/// .<c>GetCurrencyDecimalRoundingAsync</c>, which is the port of legacy's identical
/// <c>CurrentCompany.PayrollArea.Currency.DecimalCount</c> lookup + default) is applied to
/// <c>Number</c> and <c>Amount</c> in memory, at the same point in the flow (after the period's register
/// is committed, legacy line 575). Legacy swallowed every exception here; so does this port.</description></item>
/// <item><description><b>Legacy's run-level failure <c>ProjectLog</c> row IS written</b> (legacy 280-293)
/// since wave P8 made <c>ProjectLog.EmployeeId</c> nullable, matching the sibling <c>PayrollLog</c>. One
/// deviation remains: legacy SWALLOWED the exception after logging; this port logs the row and then
/// RE-THROWS, so the background job / app service still see the failure. The log write itself is
/// swallowed so it can never mask the original error.</description></item>
/// <item><description><b>No EF Core in <c>HCMv3.Core</c></b> -- every query is synchronous LINQ over
/// <c>IRepository&lt;T&gt;.GetAll()</c>; async signatures kept for interface consistency.</description></item>
/// <item><description><b><c>ProjectCostingMethod.ActualHours</c> does nothing</b> -- legacy's branch
/// (lines 1276-1279) is an empty block. Preserved as an empty branch, not an exception.</description></item>
/// </list>
/// </summary>
public interface IProjectCostEngineService : IDomainService
{
    /// <summary>
    /// Runs the engine. Legacy <c>Main</c> + <c>CreateProjectRegister</c>. Per-employee failures are
    /// isolated into failed <c>ProjectLog</c> rows exactly as legacy does; only pre-flight failures throw.
    /// </summary>
    Task<ProjectCostEngineRunResult> RunAsync(ProjectCostEngineRunInput input);
}
