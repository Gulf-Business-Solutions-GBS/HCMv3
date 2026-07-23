using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IWageTypeSecurityRolesAppService : IApplicationService
{
    Task<PagedResultDto<WageTypeSecurityRoleDto>> GetAll(GetAllWageTypeSecurityRolesInput input);

    Task<GetWageTypeSecurityRoleForEditOutput> GetWageTypeSecurityRoleForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditWageTypeSecurityRoleDto input);

    Task Delete(EntityDto input);
}
