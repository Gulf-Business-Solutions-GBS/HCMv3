using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeActionsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeActionDto>> GetAll(GetAllEmployeeActionsInput input);

    Task<GetEmployeeActionForEditOutput> GetEmployeeActionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeActionDto input);

    Task Delete(EntityDto input);
}
