using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeManagersAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeManagerDto>> GetAll(GetAllEmployeeManagersInput input);

    Task<GetEmployeeManagerForEditOutput> GetEmployeeManagerForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeManagerDto input);

    Task Delete(EntityDto input);
}
