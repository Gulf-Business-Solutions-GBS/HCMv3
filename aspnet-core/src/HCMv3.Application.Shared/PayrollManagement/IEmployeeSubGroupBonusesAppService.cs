using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupBonusesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupBonusDto>> GetAll(GetAllEmployeeSubGroupBonusesInput input);

    Task<GetEmployeeSubGroupBonusForEditOutput> GetEmployeeSubGroupBonusForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupBonusDto input);

    Task Delete(EntityDto input);
}
