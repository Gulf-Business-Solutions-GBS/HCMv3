using System;
using System.Threading;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.HealthChecks;

public class HCMv3DbContextUsersHealthCheck : IHealthCheck
{
    private readonly IDbContextProvider<HCMv3DbContext> _dbContextProvider;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public HCMv3DbContextUsersHealthCheck(
        IDbContextProvider<HCMv3DbContext> dbContextProvider,
        IUnitOfWorkManager unitOfWorkManager
        )
    {
        _dbContextProvider = dbContextProvider;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            using (var uow = _unitOfWorkManager.Begin())
            {
                // Switching to host is necessary for single tenant mode.
                using (_unitOfWorkManager.Current.SetTenantId(null))
                {
                    var dbContext = await _dbContextProvider.GetDbContextAsync();
                    if (!await dbContext.Database.CanConnectAsync(cancellationToken))
                    {
                        return HealthCheckResult.Unhealthy(
                            "HCMv3DbContext could not connect to database"
                        );
                    }

                    var user = await dbContext.Users.AnyAsync(cancellationToken);
                    await uow.CompleteAsync();

                    if (user)
                    {
                        return HealthCheckResult.Healthy("HCMv3DbContext connected to database and checked whether user added");
                    }

                    return HealthCheckResult.Unhealthy("HCMv3DbContext connected to database but there is no user.");

                }
            }
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy("HCMv3DbContext could not connect to database.", e);
        }
    }
}
