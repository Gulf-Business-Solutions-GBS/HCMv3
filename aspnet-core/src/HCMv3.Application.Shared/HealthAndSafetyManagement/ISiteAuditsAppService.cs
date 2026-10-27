using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface ISiteAuditsAppService : IApplicationService
{
    Task<PagedResultDto<SiteAuditDto>> GetAll(GetAllSiteAuditsInput input);

    Task<GetSiteAuditForEditOutput> GetSiteAuditForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditSiteAuditDto input);

    Task Delete(EntityDto input);
}
