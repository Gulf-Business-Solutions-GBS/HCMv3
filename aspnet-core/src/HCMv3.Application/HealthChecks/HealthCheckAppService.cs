using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HCMv3.Authorization;
using HCMv3.HealthChecks.Dto;

namespace HCMv3.HealthChecks;

[AbpAuthorize(AppPermissions.Pages_Administration_Host_HealthCheck)]
public class HealthCheckAppService : HCMv3AppServiceBase, IHealthCheckAppService
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckAppService(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task<ListResultDto<HealthCheckItemDto>> GetHealthChecks()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var healthCheckItems = report.Entries.Select(entry => new HealthCheckItemDto
        {
            Name = entry.Key,
            Status = entry.Value.Status.ToString(),
            Description = entry.Value.Description,
            Duration = entry.Value.Duration
        }).ToList();

        return new ListResultDto<HealthCheckItemDto>(healthCheckItems);
    }
}
