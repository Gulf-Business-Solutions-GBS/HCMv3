using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupAssetAllowancesAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupAssetAllowanceDto>> GetAll(GetAllEmployeeSubGroupAssetAllowancesInput input);

    Task<GetEmployeeSubGroupAssetAllowanceForEditOutput> GetEmployeeSubGroupAssetAllowanceForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupAssetAllowanceDto input);

    Task Delete(EntityDto input);
}
