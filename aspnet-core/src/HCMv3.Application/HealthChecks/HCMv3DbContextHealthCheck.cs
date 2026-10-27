using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.HealthChecks;

public class HCMv3DbContextHealthCheck : IHealthCheck
{
    private readonly DatabaseCheckHelper _checkHelper;

    public HCMv3DbContextHealthCheck(DatabaseCheckHelper checkHelper)
    {
        _checkHelper = checkHelper;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (_checkHelper.Exist("db"))
        {
            return Task.FromResult(HealthCheckResult.Healthy("HCMv3DbContext connected to database."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("HCMv3DbContext could not connect to database"));
    }
}
