using Abp.Application.Services;
using HCMv3.Tenants.Dashboard.Dto;

namespace HCMv3.Tenants.Dashboard;

public interface ITenantDashboardAppService : IApplicationService
{
    GetMemberActivityOutput GetMemberActivity();

    GetDashboardDataOutput GetDashboardData(GetDashboardDataInput input);

    GetDailySalesOutput GetDailySales();

    GetProfitShareOutput GetProfitShare();

    GetSalesSummaryOutput GetSalesSummary(GetSalesSummaryInput input);

    GetTopStatsOutput GetTopStats();

    GetRegionalStatsOutput GetRegionalStats();

    GetGeneralStatsOutput GetGeneralStats();
}

