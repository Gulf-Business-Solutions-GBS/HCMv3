using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface IIncidentInvestigationsAppService : IApplicationService
{
    Task<PagedResultDto<IncidentInvestigationDto>> GetAll(GetAllIncidentInvestigationsInput input);

    Task<GetIncidentInvestigationForEditOutput> GetIncidentInvestigationForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditIncidentInvestigationDto input);

    Task Delete(EntityDto input);
}
