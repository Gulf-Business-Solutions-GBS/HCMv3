using Microsoft.Extensions.DependencyInjection;
using HCMv3.HealthChecks;

namespace HCMv3.Web.HealthCheck;

public static class AbpZeroHealthCheck
{
    public static IHealthChecksBuilder AddAbpZeroHealthCheck(this IServiceCollection services)
    {
        var builder = services.AddHealthChecks();
        builder.AddCheck<HCMv3DbContextHealthCheck>("Database Connection");
        builder.AddCheck<HCMv3DbContextUsersHealthCheck>("Database Connection with user check");
        builder.AddCheck<CacheHealthCheck>("Cache");

        // add your custom health checks here
        // builder.AddCheck<MyCustomHealthCheck>("my health check");

        return builder;
    }
}

