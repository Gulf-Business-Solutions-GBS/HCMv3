using System.Threading.Tasks;

namespace HCMv3.GeneralMasterInfo.Services;

/// <summary>
/// Currency-resolution helpers (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.CurrencyService</c>).
/// </summary>
public interface ICurrencyService
{
    /// <summary>Resolves the tenant's KWD currency row, or <c>null</c> if not configured.</summary>
    Task<Currency> GetDefaultCurrencyAsync();

    /// <summary>
    /// Rounds <paramref name="value"/> to the decimal-place count of the given employee's current payroll
    /// area's currency (ported from legacy <c>CurrencyService.Rounding</c>). See <see cref="CurrencyService"/>
    /// remarks for the "current payroll area" adaptation. Returns <paramref name="value"/> unrounded if no
    /// resolvable payroll area/currency is found (matching legacy fallback behavior).
    /// </summary>
    Task<double> RoundAsync(int employeeId, double value);

    /// <summary>
    /// Resolves an employee's currency via their latest <c>EmployeeBasicPay</c> record's first
    /// <c>EmployeeBasicPayDetail</c> row (ported from legacy <c>CurrencyService.GetCurrency</c>). Returns
    /// <c>null</c> if the employee has no basic pay records.
    /// </summary>
    Task<Currency> GetEmployeeCurrencyAsync(int employeeId);
}
