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

    Task CreateOrEdit(CreateOrEditEmployeeLoanDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Employees to pick from when adding a loan from the Employee Loans list: at most
    /// <see cref="GetEmployeeLoanEmployeesInput.MaxResultCount"/> matches on code or name, ordered by code.
    /// </summary>
    Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeeLoanEmployeesInput input);
}
