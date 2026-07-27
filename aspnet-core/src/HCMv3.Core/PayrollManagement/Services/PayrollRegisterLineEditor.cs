using System.Threading.Tasks;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// The "find the line for this wage type, create it if missing, set its Amount, add it back" idiom that the
/// legacy calculated-pay branches repeat verbatim ~20 times (e.g. legacy
/// <c>HCM10.PayrollEngine/EmployeeCalculatedPayService.cs</c> lines 540-557 for wage code 5, 583-600 for 7,
/// 626-643 for 9, 668-685 for 11, 712-729 for 13, 756-773 for 23, 800-817 for 25, 981-998 for 33,
/// 1025-1042 for 35).
///
/// <para><b>Why this exists rather than a bare <see cref="IPayrollRegisterStore.SaveLineAsync"/> call.</b>
/// Legacy only ever assigns <c>Amount</c> in those branches -- an EXISTING line keeps whatever <c>Rate</c>
/// and <c>Number</c> a preceding wage rule put there. <see cref="PayrollRegisterLine"/> carries all three
/// fields and <c>SaveLineAsync</c> writes all three, so a naive call would silently zero Rate/Number on an
/// existing row. This helper reads the current line first and carries them forward.</para>
/// </summary>
public static class PayrollRegisterLineEditor
{
    /// <summary>
    /// Legacy's <c>salaryRegisterDetail.Amount = amount;</c> form. Rate/Number of an existing line are
    /// preserved; a newly created line gets 0/0 (legacy leaves its commented-out
    /// <c>//salaryRegisterDetail.Rate = 0; //salaryRegisterDetail.Number = 0;</c> initialisers, and
    /// <c>SalaryRegisterDetail</c>'s doubles default to 0).
    /// </summary>
    public static async Task<PayrollRegisterLine> SetAmountAsync(
        IPayrollRegisterStore store,
        PayrollRegisterRef register,
        WageType wageType,
        double amount,
        bool deleteWhenAmountIsZero = true)
    {
        var existing = await store.GetLineByWageTypeIdAsync(register, wageType.Id);

        var line = new PayrollRegisterLine
        {
            Id = existing?.Id ?? 0,
            WageTypeId = wageType.Id,
            WageCode = wageType.Code,
            Rate = existing?.Rate ?? 0,
            Number = existing?.Number ?? 0,
            Amount = amount
        };

        return await store.SaveLineAsync(register, line, deleteWhenAmountIsZero);
    }

    /// <summary>
    /// Legacy's <c>salaryRegisterDetail.Amount += amount;</c> form -- used only by the Loan Recovery
    /// Deduction branch (legacy line 96), which accumulates across several <c>LoanType</c>s that share one
    /// wage type.
    /// </summary>
    public static async Task<PayrollRegisterLine> AddAmountAsync(
        IPayrollRegisterStore store,
        PayrollRegisterRef register,
        WageType wageType,
        double amount,
        bool deleteWhenAmountIsZero = true)
    {
        var existing = await store.GetLineByWageTypeIdAsync(register, wageType.Id);

        var line = new PayrollRegisterLine
        {
            Id = existing?.Id ?? 0,
            WageTypeId = wageType.Id,
            WageCode = wageType.Code,
            Rate = existing?.Rate ?? 0,
            Number = existing?.Number ?? 0,
            Amount = (existing?.Amount ?? 0) + amount
        };

        return await store.SaveLineAsync(register, line, deleteWhenAmountIsZero);
    }

    /// <summary>
    /// Legacy's <c>Rate = amount; Amount = amount;</c> form, used by the three average wage types 27/29/31
    /// (legacy lines 851-855, 897-901, 943-947).
    /// </summary>
    public static async Task<PayrollRegisterLine> SetRateAndAmountAsync(
        IPayrollRegisterStore store,
        PayrollRegisterRef register,
        WageType wageType,
        double rate,
        double amount,
        bool deleteWhenAmountIsZero = true)
    {
        var existing = await store.GetLineByWageTypeIdAsync(register, wageType.Id);

        var line = new PayrollRegisterLine
        {
            Id = existing?.Id ?? 0,
            WageTypeId = wageType.Id,
            WageCode = wageType.Code,
            Rate = rate,
            Number = existing?.Number ?? 0,
            Amount = amount
        };

        return await store.SaveLineAsync(register, line, deleteWhenAmountIsZero);
    }
}
