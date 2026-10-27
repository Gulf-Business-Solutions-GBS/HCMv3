using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface IIncidentClaimsAppService : IApplicationService
{
    Task<PagedResultDto<IncidentClaimDto>> GetAll(GetAllIncidentClaimsInput input);

    Task<GetIncidentClaimForEditOutput> GetIncidentClaimForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditIncidentClaimDto input);

    Task Delete(EntityDto input);
}
