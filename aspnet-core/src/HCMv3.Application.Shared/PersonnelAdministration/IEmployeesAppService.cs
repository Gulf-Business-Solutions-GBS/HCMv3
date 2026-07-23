using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDto>> GetAll(GetAllEmployeesInput input);

    Task<GetEmployeeForEditOutput> GetEmployeeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeDto input);

    Task Delete(EntityDto input);
}
