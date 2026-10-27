using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The per-row half of the basic-pay (salary) Excel import, ported from legacy
/// <c>ImportExcelController</c> (the <c>EmployeeBasicPay</c> branch). Reading the workbook, the row loop, the
/// unit of work per row and the result belong to the caller; this does what one row does to the data.
/// </summary>
public interface IEmployeeBasicPayImportService : IDomainService
{
    /// <summary>
    /// Validates <paramref name="row"/> and, when it is valid, writes it:
    /// <list type="number">
    /// <item><description>finds the employee's basic pay starting on the effective date, or creates one (EndDate
    /// 9999-12-31, PayType and PayInterval from the row). An existing record keeps its own PayType and
    /// PayInterval;</description></item>
    /// <item><description>if that record already has the wage type, stops and reports
    /// <see cref="EmployeeBasicPayImportRowStatus.AlreadyPresent"/> -- nothing is changed;</description></item>
    /// <item><description>otherwise adds the detail line and sets the header amount to the total of its lines;</description></item>
    /// <item><description>when <paramref name="carryAllowanceOnSalaryImport"/> is on and the wage type's Code is
    /// <c>"1000"</c>, copies every other line of the latest earlier basic pay the record does not already have;</description></item>
    /// <item><description>re-chains the EndDate of all the employee's basic pays.</description></item>
    /// </list>
    /// Throws when the data refuses the change (a blank or zero amount, a payroll lock, a retro rule). Run each
    /// row in its own unit of work so a throw rolls back only that row.
    /// </summary>
    /// <param name="tenantId">Stamped on every record created.</param>
    /// <param name="row">The raw values of the row.</param>
    /// <param name="carryAllowanceOnSalaryImport"><c>ManagePayrollSettings.CarryAllowanceOnSalaryImport</c>.</param>
    Task<EmployeeBasicPayImportRowResult> ImportRowAsync(int tenantId, EmployeeBasicPayImportRow row, bool carryAllowanceOnSalaryImport);
}
