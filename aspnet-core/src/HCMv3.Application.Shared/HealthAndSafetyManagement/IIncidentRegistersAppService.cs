using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface IIncidentRegistersAppService : IApplicationService
{
    Task<PagedResultDto<IncidentRegisterDto>> GetAll(GetAllIncidentRegistersInput input);

    Task<GetIncidentRegisterForEditOutput> GetIncidentRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditIncidentRegisterDto input);

    Task Delete(EntityDto input);
}
