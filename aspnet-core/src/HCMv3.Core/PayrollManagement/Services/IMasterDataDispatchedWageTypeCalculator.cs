namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// A wage-type rule that is NOT selected by wage CODE, but by a master table holding a reference to the wage
/// type. This is the second of legacy's two dispatch mechanisms, and until wave P9 the port could not express
/// it at all.
///
/// <para><b>The legacy mechanism, precisely.</b> Legacy builds <c>wtCalculatedWageList</c> -- the set of wage
/// types that go to <c>EmployeeCalculatedPayService</c> -- from FIVE sources
/// (<c>HCM10.PayrollEngine/Program.cs</c> lines 2698-2702):</para>
/// <list type="number">
/// <item><description>line 2698: every <c>DisciplinaryType.WageType.Oid</c>;</description></item>
/// <item><description>line 2699: every <c>TicketType.WageType.Oid</c>;</description></item>
/// <item><description>line 2700: every <c>LoanType.WageType.Oid</c>;</description></item>
/// <item><description>line 2701: every <c>TravelType.WageType.Oid</c>;</description></item>
/// <item><description>line 2702: the wage types whose CODE is in the hard-coded <c>wtcalList</c>
/// (lines 2683-2696) -- the only source <see cref="IWageTypeCalculator.WageCode"/> models.</description></item>
/// </list>
///
/// <para>Inside <c>EmployeeCalculatedPayService.Main</c> the first four are then re-matched by master-table
/// lookup rather than by code -- e.g. Loan Recovery Deduction at legacy line 73:
/// <c>Program.LoanTypeList.Where(r =&gt; r.WageType.Oid == currWageType.Oid).ToList()</c> -- and only the
/// fifth is matched by <c>currWageType.Code == "...."</c>.</para>
///
/// <para><b>This is not a theoretical distinction.</b> Wage code 2001 Loan Recovery Deduction is explicitly
/// COMMENTED OUT of <c>wtcalList</c> (legacy <c>Program.cs</c> line 2691:
/// <c>wtcalList.AddRange(new List&lt;string&gt;(new[] { "1006" }));//, "2001" }));</c>), so a code-only port
/// posts nothing for it, no matter how faithful the arithmetic is.</para>
///
/// <para><b>Multiple rules may claim one wage type, and all of them run.</b> Legacy's four master-table
/// regions are sequential <c>if</c>/<c>foreach</c> blocks inside one method body, NOT an <c>else if</c>
/// chain -- a wage type referenced by both a <c>TicketType</c> and a <c>LoanType</c> would legitimately go
/// through both. <see cref="WageTypeCalculatorResolver"/> therefore runs every claimant, in contrast to the
/// by-code path where duplicate claims are an error.</para>
///
/// <para><b>Why it extends <see cref="IWageTypeCalculator"/> instead of standing alone.</b> Registration is
/// silent when it fails (see <see cref="IWageTypeCalculator"/>'s remarks and
/// <c>WageTypeCalculatorRegistration_Tests</c>): ABP/Castle's <c>DefaultInterfaces</c> strategy matches an
/// interface to a class BY NAME, so a <c>...WageTypeCalculator</c> class registers under
/// <c>IWageTypeCalculator</c> and would NOT necessarily register under a differently-named standalone
/// interface. Inheriting keeps these rules inside the one enumerable that is already proven to be populated,
/// and the resolver filters with <c>OfType&lt;IMasterDataDispatchedWageTypeCalculator&gt;()</c>.</para>
///
/// <para><b><see cref="IWageTypeCalculator.WageCode"/> must be <c>null</c></b> on implementations of this
/// interface: they are never reached by code. <see cref="WageTypeCalculatorResolver.Resolve"/> never matches
/// a null code, and the registration test's wage-code-uniqueness assertion skips them.</para>
/// </summary>
public interface IMasterDataDispatchedWageTypeCalculator : IWageTypeCalculator
{
    /// <summary>
    /// Does this rule's master table reference <paramref name="wageTypeId"/>? Legacy's
    /// <c>Where(r =&gt; r.WageType.Oid == currWageType.Oid).Any()</c> membership test, hoisted so the engine
    /// can answer "is this wage type calculated at all?" (legacy's <c>wtCalculatedWageList.Contains</c> at
    /// <c>Program.cs</c> line 2574) without running anything.
    /// </summary>
    bool ClaimsWageType(int wageTypeId);
}
