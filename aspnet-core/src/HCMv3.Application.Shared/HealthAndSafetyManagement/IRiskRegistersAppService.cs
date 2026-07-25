using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthAndSafetyManagement.Dto;

namespace HCMv3.HealthAndSafetyManagement;

public interface IRiskRegistersAppService : IApplicationService
{
    Task<PagedResultDto<RiskRegisterDto>> GetAll(GetAllRiskRegistersInput input);

    Task<GetRiskRegisterForEditOutput> GetRiskRegisterForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditRiskRegisterDto input);

    Task Delete(EntityDto input);
}
