using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeLoansAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeLoanDto>> GetAll(GetAllEmployeeLoansInput input);

    Task<GetEmployeeLoanForEditOutput> GetEmployeeLoanForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeLoanDto input);

    Task Delete(EntityDto input);
}
