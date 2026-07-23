using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PayrollManagement.Dto;

namespace HCMv3.PayrollManagement;

public interface IOrgUnitWageAccGroupsAppService : IApplicationService
{
    Task<PagedResultDto<OrgUnitWageAccGroupDto>> GetAll(GetAllOrgUnitWageAccGroupsInput input);

    Task<GetOrgUnitWageAccGroupForEditOutput> GetOrgUnitWageAccGroupForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditOrgUnitWageAccGroupDto input);

    Task Delete(EntityDto input);
}
