using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface ISiteAuditDetailsAppService : IApplicationService
{
    Task<PagedResultDto<SiteAuditDetailDto>> GetAll(GetAllSiteAuditDetailsInput input);

    Task<GetSiteAuditDetailForEditOutput> GetSiteAuditDetailForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSiteAuditDetailDto input);

    Task Delete(EntityDto input);
}
