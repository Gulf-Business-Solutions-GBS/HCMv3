using System.Threading.Tasks;
using HCMv3.MultiTenancy.HostDashboard.Dto;

namespace HCMv3.MultiTenancy.HostDashboard;

public class ProxyHostDashboardAppService : ProxyAppServiceBase, IHostDashboardAppService
{
    public async Task<TopStatsData> GetTopStatsData(GetTopStatsInput input)
    {
        return await ApiClient.GetAsync<TopStatsData>(GetEndpoint(nameof(GetTopStatsData)), input);
    }

    public async Task<GetRecentTenantsOutput> GetRecentTenantsData()
    {
        return await ApiClient.GetAsync<GetRecentTenantsOutput>(GetEndpoint(nameof(GetRecentTenantsData)));
    }

    public async Task<GetExpiringTenantsOutput> GetSubscriptionExpiringTenantsData()
    {
        return await ApiClient.GetAsync<GetExpiringTenantsOutput>(GetEndpoint(nameof(GetSubscriptionExpiringTenantsData)));
    }

    public async Task<GetIncomeStatisticsDataOutput> GetIncomeStatistics(GetIncomeStatisticsDataInput input)
    {
        return await ApiClient.GetAsync<GetIncomeStatisticsDataOutput>(GetEndpoint(nameof(GetIncomeStatistics)), input);
    }

    public async Task<GetEditionTenantStatisticsOutput> GetEditionTenantStatistics(GetEditionTenantStatisticsInput input)
    {
        return await ApiClient.GetAsync<GetEditionTenantStatisticsOutput>(GetEndpoint(nameof(GetEditionTenantStatistics)), input);
    }
}