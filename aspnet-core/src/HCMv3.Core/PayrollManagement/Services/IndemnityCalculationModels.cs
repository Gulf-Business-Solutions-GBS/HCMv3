using System.Collections.Generic;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Result of <see cref="IIndemnityCalculationService.GetIndemnityPayAsync"/>.
///
/// <para>The legacy method (<c>EmployeeCalculatedPayService.GetIndemnityPay</c>, line 1716) was <c>void</c>
/// and mutated / created a <c>SalaryRegisterDetail</c> XPO object in-place under an implicit session.
/// This port persists the create/update through the injected repository and returns this summary so
/// callers/tests can observe what happened without re-querying (same approach already taken by
/// <see cref="ForfeitCalculationResult"/> in this folder).</para>
/// </summary>
public class IndemnityPayResult
{
    /// <summary>Legacy <c>salaryRegisterDetail.Rate</c> (= sum of Rate over all IncludeInIndemnity wage types).</summary>
    public double Rate { get; set; }

    /// <summary>Legacy <c>salaryRegisterDetail.Number</c> (= wage code "4005" Number, indemnity paid days).</summary>
    public double Number { get; set; }

    /// <summary>Legacy <c>salaryRegisterDetail.Amount</c>.</summary>
    public double Amount { get; set; }

    /// <summary>True when a new <c>SalaryRegisterDetail</c> row was inserted for the current wage type.</summary>
    public bool SalaryRegisterDetailCreated { get; set; }

    /// <summary>True when an existing <c>SalaryRegisterDetail</c> row was updated.</summary>
    public bool SalaryRegisterDetailUpdated { get; set; }

    /// <summary>
    /// Id of the created/updated <c>SalaryRegisterDetail</c>, or <c>null</c> when legacy wrote nothing
    /// (i.e. paid days == 0, or basis days == 0, or the computed amount == 0).
    /// </summary>
    public int? SalaryRegisterDetailId { get; set; }

    /// <summary>
    /// The indemnity slab list as it stands after the call. <c>GetIndemnityBifercateAmount</c> may lazily
    /// (re)build the legacy static <c>Program.IndemnitySlabList</c> for End-Of-Service off-cycle runs; since
    /// that static is parameterised in this port, the possibly-refreshed list is handed back here.
    /// </summary>
    public List<IndemnitySlab> IndemnitySlabs { get; set; }
}

/// <summary>
/// Result of <see cref="IIndemnityCalculationService.GetIndemnityProvisionAsync"/> and
/// <see cref="IIndemnityCalculationService.GetTotalIndemnityProvisionAsync"/>.
///
/// <para>Both legacy methods (<c>EmployeeCalculatedPayService.GetIndemnityProvision</c> line 2610 and
/// <c>GetTotalIndemnityProvision</c> line 2643) wrote <c>Rate</c>/<c>Number</c>/<c>Amount</c> onto the
/// <c>SalaryRegisterDetail</c> passed by reference AND returned a <c>double</c>. Those two legacy return
/// values are NOT the same thing, and one of them is a legacy bug that is preserved here verbatim -- see
/// <see cref="LegacyReturnValue"/>.</para>
/// </summary>
public class IndemnityProvisionResult
{
    /// <summary>Value written to <c>SalaryRegisterDetail.Rate</c>.</summary>
    public double Rate { get; set; }

    /// <summary>Value written to <c>SalaryRegisterDetail.Number</c>.</summary>
    public double Number { get; set; }

    /// <summary>Value written to <c>SalaryRegisterDetail.Amount</c>.</summary>
    public double Amount { get; set; }

    /// <summary>
    /// The exact value the legacy method returned to its caller.
    ///
    /// <para><b>Legacy quirk preserved verbatim:</b> <c>GetIndemnityProvision</c> declares
    /// <c>double amount = 0;</c>, never assigns to that local (it assigns to
    /// <c>salaryRegisterDetail.Amount</c> instead) and then returns it -- so it ALWAYS returns 0,
    /// regardless of the amount actually posted. <c>GetTotalIndemnityProvision</c> does assign
    /// <c>amount = salaryRegisterDetail.Amount;</c> and therefore returns the real posted amount (and 0 on
    /// the else-branch). Both legacy call sites (<c>EmployeeCalculatedPayService</c> lines 210 and 244)
    /// discard the return value, so the quirk is harmless -- but it is reproduced rather than "fixed" so
    /// that any future caller sees exactly what legacy produced. Use <see cref="Amount"/> for the real
    /// posted amount.</para>
    /// </summary>
    public double LegacyReturnValue { get; set; }

    /// <summary>See <see cref="IndemnityPayResult.IndemnitySlabs"/>.</summary>
    public List<IndemnitySlab> IndemnitySlabs { get; set; }
}

/// <summary>
/// Result of <see cref="IIndemnityCalculationService.GetIndemnityBifurcateAmountAsync"/> (legacy
/// <c>EmployeeCalculatedPayService.GetIndemnityBifercateAmount</c>, line 2677).
/// </summary>
public class IndemnityBifurcateResult
{
    /// <summary>The bifurcated (slab-wise) indemnity amount.</summary>
    public double Amount { get; set; }

    /// <summary>
    /// The slab list after the call. Legacy mutated the static <c>Program.IndemnitySlabList</c> in place
    /// (via <c>Program.GetSlabWiseIndemnityTable</c>) when it was null/empty on an End-Of-Service off-cycle
    /// run; that static is parameterised here, so the refreshed list is returned instead.
    /// </summary>
    public List<IndemnitySlab> IndemnitySlabs { get; set; }
}

/// <summary>
/// Result of <see cref="IIndemnityCalculationService.GetIndemnityProvisionDayWiseEntryAsync"/> (legacy
/// <c>EmployeeCalculatedPayService.GetIndemnityProvisionDayWiseEntry</c>, line 2743, which was <c>void</c>).
/// </summary>
public class IndemnityProvisionDayWiseResult
{
    /// <summary>Number of pre-existing <c>SalaryRegisterDaysDetail</c> rows deleted at the start of the call.</summary>
    public int DeletedDaysDetailCount { get; set; }

    /// <summary>Number of <c>SalaryRegisterDaysDetail</c> rows created by the call.</summary>
    public int CreatedDaysDetailCount { get; set; }

    /// <summary>
    /// The provision-change (previous-month difference) amount returned by
    /// <see cref="IIndemnityCalculationService.GetProvisionChangeIndemnityAsync"/> and added to the wage
    /// code "3000" line. 0 when the method returned early (no increment this month / no earlier basic pay)
    /// or when there was no difference.
    /// </summary>
    public double ProvisionChangeAmount { get; set; }

    /// <summary>Final <c>Amount</c> written to the wage code "3000" <c>SalaryRegisterDetail</c>.</summary>
    public double PostedAmount { get; set; }

    /// <summary>Id of the wage code "3000" <c>SalaryRegisterDetail</c> row touched, if any.</summary>
    public int? SalaryRegisterDetailId { get; set; }
}

/// <summary>
/// Result of <see cref="IIndemnityCalculationService.UpdateEmployeeIndemnityQuotaAsync"/> (legacy
/// <c>Program.UpdateEmployeeIndemnityQuota</c>, line 2774, which was <c>void</c>).
/// </summary>
public class EmployeeIndemnityQuotaUpdateResult
{
    /// <summary>False when the employee has no <c>EmployeeGratuityQuota</c> row (legacy returned early).</summary>
    public bool Updated { get; set; }

    /// <summary>Id of the updated <c>EmployeeGratuityQuota</c> row, when <see cref="Updated"/> is true.</summary>
    public int? EmployeeGratuityQuotaId { get; set; }

    /// <summary>Value written to <c>QuotaAccrued</c> (wage code "4025" Number, <c>Math.Round(x, 3)</c>).</summary>
    public double QuotaAccrued { get; set; }

    /// <summary>Value written to <c>QuotaUsed</c> (wage code "4005" Number, <c>Math.Round(x, 3)</c>).</summary>
    public double QuotaUsed { get; set; }
}
