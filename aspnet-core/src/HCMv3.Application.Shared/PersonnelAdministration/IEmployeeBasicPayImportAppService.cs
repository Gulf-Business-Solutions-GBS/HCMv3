using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Basic-pay (salary) Excel import, ported from legacy <c>ImportExcelController</c> (the <c>EmployeeBasicPay</c>
/// branch), including <c>ManagePayrollSettings.CarryAllowanceOnSalaryImport</c>.
/// </summary>
public interface IEmployeeBasicPayImportAppService : IApplicationService
{
    /// <summary>
    /// Imports every row of the workbook, each in its own transaction, so a failed row rolls back only itself.
    /// Runs inline and returns the counts and error lines.
    /// </summary>
    Task<ImportEmployeeBasicPaysOutput> ImportEmployeeBasicPays(ImportEmployeeBasicPaysInput input);
}
