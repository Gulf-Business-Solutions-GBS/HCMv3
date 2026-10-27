using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.GatePassManagement.Dto;

namespace HCMv3.GatePassManagement;

public interface IEmployeePoolCarsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeePoolCarDto>> GetAll(GetAllEmployeePoolCarsInput input);

    Task<GetEmployeePoolCarForEditOutput> GetEmployeePoolCarForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeePoolCarDto input);

    Task Delete(EntityDto input);
}
