using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeNonPayrollTransAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeNonPayrollTransDto>> GetAll(GetAllEmployeeNonPayrollTransInput input);

    Task<GetEmployeeNonPayrollTransForEditOutput> GetEmployeeNonPayrollTransForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeNonPayrollTransDto input);

    Task Delete(EntityDto input);
}
