using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeOnHoldsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeOnHoldDto>> GetAll(GetAllEmployeeOnHoldsInput input);

    Task<GetEmployeeOnHoldForEditOutput> GetEmployeeOnHoldForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeOnHoldDto input);

    Task Delete(EntityDto input);
}
