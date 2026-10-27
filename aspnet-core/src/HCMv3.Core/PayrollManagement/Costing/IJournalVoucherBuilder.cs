using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PayrollManagement.Costing;

/// <summary>
/// THE single cost-resolution / journal-voucher assembly seam.
///
/// <para><b>Why one service for two screens.</b> Legacy shipped this logic TWICE, as a private
/// <c>ExportTable(...)</c> method copy-pasted into each screen:
/// <c>HCM10.Web/UserControls/PayrollManagement/uscCostingFile.ascx.cs</c> lines 1760-2174 and
/// <c>uscPostJV.ascx.cs</c> lines 1052-1343. The two copies build the SAME 24-column ledger table from the
/// SAME cost register, with the SAME cost-element-slot-to-GL-dimension mapping, the SAME
/// Earning/Provision-debit vs Deduction-credit sign rule, the SAME provision offset-line generation and the
/// SAME "Net Pay" balancing line. <c>uscPostJV</c>'s copy is simply the older, un-maintained snapshot -- it
/// lacks the four features that were later added to <c>uscCostingFile</c>'s copy only. This port factors the
/// logic ONCE, taking <c>uscCostingFile</c> (the newer, live copy) as the reference implementation, and both
/// app services call in here.</para>
///
/// <para><b>The four <c>uscCostingFile</c>-only behaviours, and why they are the correct baseline.</b></para>
/// <list type="number">
/// <item><description><b>Per-line rounding.</b> <c>uscCostingFile</c> rounds each line's Debit/Credit to
/// 3 dp at projection time (lines 1904-1905); <c>uscPostJV</c> did not round at all (lines 1184-1185).
/// Adopting the rounding version is required, because the D365 payload serialises the raw
/// <c>double</c> and an unrounded value emits full float noise into the ledger.</description></item>
/// <item><description><b>The provision wage-type override list.</b> Codes 1066/1067 are re-labelled
/// "Provision" regardless of their configured <c>BillingType</c> (line 1916), which both routes them to the
/// provision JV and makes them generate an offset line. <c>uscPostJV</c> keyed only off
/// <c>BillingType == Provision</c> (line 1253).</description></item>
/// <item><description><b><c>MergeOffsetAccount</c> accumulation.</b> <c>uscCostingFile</c> honours
/// <c>EmployeeWageAccount.MergeOffsetAccount</c> by collapsing many offset lines into a single accumulating
/// GL row (lines 2027-2111); <c>uscPostJV</c> always emitted one offset line per source line.</description></item>
/// <item><description><b>Net Pay excludes provisions.</b> <c>uscCostingFile</c> excludes the 1066/1067
/// provision codes from the Net Pay total (line 2121); <c>uscPostJV</c> summed all Earning+Deduction lines
/// (line 1294). The exclusion is the correct one: provision lines already self-balance via their own offset
/// line, so including them would double-count and unbalance the JV.</description></item>
/// </list>
///
/// <para><b>Balancing.</b> Every provision line is paired with an offset line carrying the swapped
/// Debit/Credit (legacy lines 2051-2052), and the residual Earning-minus-Deduction total is discharged onto
/// a single Net Pay CREDIT line against the payroll area's FI bank account (lines 2118-2163). Legacy asserts
/// balance only in the "Validate JV" button path, and does so after rounding both totals to WHOLE UNITS
/// (lines 1719-1722) -- not to <c>decimalPlaces</c>. That tolerance is preserved verbatim in
/// <see cref="JournalVoucherResult.IsBalanced"/>; it is a legacy behaviour, not an endorsement.</para>
/// </summary>
public interface IJournalVoucherBuilder : IDomainService
{
    /// <summary>
    /// Builds the full JV line set for a payroll area / period / run type. Direct replacement for legacy
    /// <c>uscCostingFile.ExportTable(objectSpace, payrollRunType, empCode, payInDate, payrollArea,
    /// PostingDate, Sponser)</c>.
    ///
    /// <para>The legacy <c>Sponser</c> parameter is NOT carried over: it is dead in both copies -- the caller
    /// always passes <c>""</c> (<c>uscCostingFile.ascx.cs</c> lines 265, 1708) and the method body never
    /// reads it.</para>
    /// </summary>
    Task<JournalVoucherResult> BuildAsync(CostingRunInput input);
}
