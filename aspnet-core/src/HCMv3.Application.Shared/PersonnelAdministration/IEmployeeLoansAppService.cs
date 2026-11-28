using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeLoansAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeLoanDto>> GetAll(GetAllEmployeeLoansInput input);

    Task<GetEmployeeLoanForEditOutput> GetEmployeeLoanForEdit(EntityDto input);

    /// <summary>
    /// Saves the loan and returns its id, so a loan just added can go on to have instalments, recoveries and
    /// exemptions recorded under it without closing the dialog.
    /// </summary>
    Task<int> CreateOrEdit(CreateOrEditEmployeeLoanDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Employees to pick from when adding a loan from the Employee Loans list: every employee, optionally
    /// narrowed by code or name, ordered by code.
    /// </summary>
    Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeeLoanEmployeesInput input);
}
