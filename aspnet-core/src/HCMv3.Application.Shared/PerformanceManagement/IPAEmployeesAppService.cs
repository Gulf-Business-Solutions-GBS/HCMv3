using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PerformanceManagement.Dto;

namespace HCMv3.PerformanceManagement;

public interface IPAEmployeesAppService : IApplicationService
{
    Task<PagedResultDto<PAEmployeeDto>> GetAll(GetAllPAEmployeesInput input);

    Task<GetPAEmployeeForEditOutput> GetPAEmployeeForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditPAEmployeeDto input);

    Task Delete(EntityDto input);
}
