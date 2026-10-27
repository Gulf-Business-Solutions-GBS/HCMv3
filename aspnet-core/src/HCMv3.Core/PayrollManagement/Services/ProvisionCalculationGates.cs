namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Wave P5. The two run-type gates the legacy PROVISION branches apply INSIDE their own
/// <c>else if (currWageType.Code == "300x")</c> arm, before doing any work.
///
/// <para>These are deliberately NOT part of <see cref="IWageTypeCalculatorResolver"/>'s four shared gates:
/// the resolver reproduces legacy's four ENGINE-level gates (<c>Allow_CalcWageType</c>,
/// <c>IsNotReturnFromLeave</c>, <c>IsOnHold</c>, <c>AllowToCalculate</c>), whereas these two are per-branch
/// business conditions that legacy wrote out by hand in each provision arm and that differ BETWEEN the
/// provision codes (3000/3003/3001/3004 use <see cref="AllowAdvanceLeaveProvision"/>; 3002/3005 and 1010 use
/// <see cref="AllowEndOfServiceProvision"/>). Folding them into the resolver would apply the wrong one to
/// half the codes.</para>
/// </summary>
internal static class ProvisionCalculationGates
{
    /// <summary>
    /// Legacy: <c>payrollRunType == Regular || (payrollRunType == OffCycle &amp;&amp;
    /// currWageType.IncludeInAdvancePay == true &amp;&amp; offCycleReasons == Program.reasonAdvanceLeave)</c>
    /// -- legacy <c>EmployeeCalculatedPayService.cs</c> lines 190-192 (3000), 224-226 (3003), 262-264 (3001),
    /// 300-302 (3004).
    /// </summary>
    public static bool AllowAdvanceLeaveProvision(PayrollCalculationContext context)
    {
        if (context.PayrollRunType == PayrollRunType.Regular)
        {
            return true;
        }

        return context.PayrollRunType == PayrollRunType.OffCycle
               && context.WageType != null
               && context.WageType.IncludeInAdvancePay
               && context.IsAdvanceLeave;
    }

    /// <summary>
    /// Legacy: <c>payrollRunType == Regular || (payrollRunType == OffCycle &amp;&amp; offCycleReasons ==
    /// Program.endOfServiceOffcycle)</c> -- legacy <c>EmployeeCalculatedPayService.cs</c> line 179 (1010),
    /// 334 (3002), 366 (3005). Note it does NOT test <c>IncludeInAdvancePay</c>.
    ///
    /// <para>Identical to the sibling wave's <see cref="PayrollRunGates.AllowEndOfServiceOnly"/>, so it
    /// delegates rather than duplicating the predicate. (Its <c>AllowIncludingAdvanceLeave</c> sibling is
    /// deliberately NOT reused by <see cref="AllowAdvanceLeaveProvision"/>: that one has a THIRD arm
    /// -- <c>|| (OffCycle &amp;&amp; endOfServiceOffcycle)</c> -- which the four provision branches above do
    /// not have. The two predicates look alike and are not interchangeable.)</para>
    /// </summary>
    public static bool AllowEndOfServiceProvision(PayrollCalculationContext context)
    {
        return PayrollRunGates.AllowEndOfServiceOnly(context);
    }
}
