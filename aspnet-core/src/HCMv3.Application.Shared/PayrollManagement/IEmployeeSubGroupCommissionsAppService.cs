using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IEmployeeSubGroupCommissionsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeSubGroupCommissionDto>> GetAll(GetAllEmployeeSubGroupCommissionsInput input);

    Task<GetEmployeeSubGroupCommissionForEditOutput> GetEmployeeSubGroupCommissionForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeSubGroupCommissionDto input);

    Task Delete(EntityDto input);
}
