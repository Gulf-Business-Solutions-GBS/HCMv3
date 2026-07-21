using Microsoft.JSInterop;
using HCMv3.Maui.Core.Components;
using HCMv3.MultiTenancy.HostDashboard;
using HCMv3.MultiTenancy.HostDashboard.Dto;

namespace HCMv3.Maui.Pages.Dashboard;

public partial class HostDashboard : HCMv3MainLayoutPageComponentBase
{
    private readonly IHostDashboardAppService _appService;

    TopStatsData TopStatsData { get; set; }

    GetIncomeStatisticsDataOutput IncomeStatisticsData { get; set; }

    GetEditionTenantStatisticsOutput EditionTenantStatisticsData { get; set; }

    public HostDashboard()
    {
        _appService = Resolve<IHostDashboardAppService>();
    }

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("Dashboard"), L("DashboardHeaderInfo"));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadDataAsync();
            await ShowNewSubscriptionAmountAsync();
            await ShowIncomeStatisticsAsync();
            await ShowEditionTenantStatisticsAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        TopStatsData = await _appService.GetTopStatsData(new GetTopStatsInput
        {
            StartDate = DateTime.Now.AddDays(-14),
            EndDate = DateTime.Now
        });

        IncomeStatisticsData = await _appService.GetIncomeStatistics(new GetIncomeStatisticsDataInput
        {
            StartDate = DateTime.Now.AddMonths(-1),
            EndDate = DateTime.Now,
            IncomeStatisticsDateInterval = ChartDateInterval.Monthly
        });

        EditionTenantStatisticsData = await _appService.GetEditionTenantStatistics(new GetEditionTenantStatisticsInput
        {
            StartDate = DateTime.Now.AddDays(-14),
            EndDate = DateTime.Now
        });
    }

    private async Task ShowNewSubscriptionAmountAsync()
    {
        var labels = new[]
        {
            L("NewSubscriptionAmount"),
            L("NewTenants"),
            L("DashboardSampleStatistics") + " 1",
            L("DashboardSampleStatistics") + " 2"
        };

        var data = new[]
        {
            TopStatsData.NewSubscriptionAmount,
            TopStatsData.NewTenantsCount,
            TopStatsData.DashboardPlaceholder1,
            TopStatsData.DashboardPlaceholder2
        };

        var chartData = new
        {
            labels,
            fill = true,
            tension = 0.4,
            datasets = new[]
            {
                new
                {
                    backgroundColor = new[]
                    {
                        "rgb(255, 99, 132)", "rgb(54, 162, 235)", "rgb(255, 206, 86)", "rgb(75, 192, 192)"
                    },
                    borderColor = new[]
                    {
                        "rgb(255, 99, 132)", "rgb(54, 162, 235)", "rgb(255, 206, 86)", "rgb(75, 192, 192)"
                    },
                    data
                }
            }
        };


        var chartOptions = new
        {
            responsive = true,

            maintainAspectRatio = false,
            plugins = new
            {
                legend = new
                {
                    display = false
                }
            },
            indexAxis = "y"
        };

        await JS.InvokeVoidAsync("BlazorChartManagerService.showChart", "getTopStatsData", "bar", chartData,
            chartOptions);
    }

    private async Task ShowIncomeStatisticsAsync()
    {
        var labels = IncomeStatisticsData.IncomeStatistics.Select(stat => stat.Label).ToArray();
        var data = IncomeStatisticsData.IncomeStatistics.Select(stat => stat.Amount).ToArray();

        var chartData = new
        {
            labels,
            datasets = new[]
            {
                new
                {
                    label = "Income Statistics",
                    backgroundColor = new[]
                    {
                        "rgb(255, 99, 132)",
                        "rgb(54, 162, 235)",
                        "rgb(255, 206, 86)",
                        "rgb(75, 192, 192)",
                        "rgb(153, 102, 255)"
                    },
                    borderColor = new[]
                    {
                        "rgb(255, 99, 132)",
                        "rgb(54, 162, 235)",
                        "rgb(255, 206, 86)",
                        "rgb(75, 192, 192)",
                        "rgb(153, 102, 255)"
                    },
                    data
                }
            }
        };

        var chartOptions = new
        {
            responsive = true,
            maintainAspectRatio = false,
            plugins = new
            {
                legend = new
                {
                    display = false
                }
            },
            scales = new
            {
                y = new
                {
                    beginAtZero = true
                }
            }
        };

        await JS.InvokeVoidAsync("BlazorChartManagerService.showChart", "incomeStatistics", "bar", chartData,
            chartOptions);
    }

    private async Task ShowEditionTenantStatisticsAsync()
    {
        var labels = EditionTenantStatisticsData.EditionStatistics.Select(stat => stat.Label).ToArray();
        var data = EditionTenantStatisticsData.EditionStatistics.Select(stat => stat.Value).ToArray();

        var chartData = new
        {
            labels,
            datasets = new[]
            {
                new
                {
                    label = "Edition Tenant Statistics",
                    backgroundColor = new[]
                    {
                        "rgb(255, 99, 132)",
                        "rgb(54, 162, 235)"
                    },
                    borderColor = new[]
                    {
                        "rgb(255, 99, 132)",
                        "rgb(54, 162, 235)"
                    },
                    data
                }
            }
        };

        var chartOptions = new
        {
            responsive = true,
            maintainAspectRatio = false,
            plugins = new
            {
                legend = new
                {
                    display = false
                }
            },
            scales = new
            {
                y = new
                {
                    beginAtZero = true
                }
            }
        };

        await JS.InvokeVoidAsync("BlazorChartManagerService.showChart", "editionStatistics", "bar", chartData,
            chartOptions);
    }
}