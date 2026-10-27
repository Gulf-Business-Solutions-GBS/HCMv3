using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.HealthChecks.Dto;

namespace HCMv3.HealthChecks;

public interface IHealthCheckAppService : IApplicationService
{
    Task<ListResultDto<HealthCheckItemDto>> GetHealthChecks();
}
