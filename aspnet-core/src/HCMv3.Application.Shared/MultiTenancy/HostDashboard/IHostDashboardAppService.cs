using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.MultiTenancy.HostDashboard.Dto;

namespace HCMv3.MultiTenancy.HostDashboard;

public interface IHostDashboardAppService : IApplicationService
{
    Task<TopStatsData> GetTopStatsData(GetTopStatsInput input);

    Task<GetRecentTenantsOutput> GetRecentTenantsData();

    Task<GetExpiringTenantsOutput> GetSubscriptionExpiringTenantsData();

    Task<GetIncomeStatisticsDataOutput> GetIncomeStatistics(GetIncomeStatisticsDataInput input);

    Task<GetEditionTenantStatisticsOutput> GetEditionTenantStatistics(GetEditionTenantStatisticsInput input);
}

