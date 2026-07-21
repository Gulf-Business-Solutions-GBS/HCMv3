using Microsoft.JSInterop;
using HCMv3.Maui.Core.Components;
using HCMv3.Tenants.Dashboard;

namespace HCMv3.Maui.Pages.Dashboard;

public partial class TenantDashboard : HCMv3MainLayoutPageComponentBase
{
    private int[] TotalProfitData { get; } = DashboardRandomDataGenerator.GetRandomArray(5, 10, 50);

    private int[] DailySalesData { get; } = DashboardRandomDataGenerator.GetRandomArray(4, 10, 50);

    private int[] ProfitShareData { get; } = DashboardRandomDataGenerator.GetRandomPercentageArray(3);

    protected override async Task OnInitializedAsync()
    {
        await SetPageHeader(L("Dashboard"), L("DashboardHeaderInfo"));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await ShowTotalProfitAsync();
            await ShowDailySalesChartAsync();
            await ShowProfitShareAsync();
        }
    }

    private async Task ShowTotalProfitAsync()
    {
        var chartData = new
        {
            labels = new[] { "Q1", "Q2", "Q3", "Q4", "Q5" },
            datasets = new[]
            {
                new
                {
                    label = "Quarterly Growth",
                    backgroundColor = "rgba(75, 192, 192, 0.2)",
                    borderColor = "rgb(75, 192, 192)",
                    fill = true,
                    tension = 0.4,
                    pointBackgroundColor = new[]
                    {
                        "rgb(255, 99, 132)", "rgb(54, 162, 235)", "rgb(255, 206, 86)", "rgb(75, 192, 192)",
                        "rgb(153, 102, 255)"
                    },
                    pointBorderColor = new[]
                    {
                        "rgb(255, 99, 132)", "rgb(54, 162, 235)", "rgb(255, 206, 86)", "rgb(75, 192, 192)",
                        "rgb(153, 102, 255)"
                    },
                    data = TotalProfitData
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
                    display = true
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

        await JS.InvokeVoidAsync("BlazorChartManagerService.showChart", "totalProfit", "line", chartData, chartOptions);
    }

    private async Task ShowDailySalesChartAsync()
    {
        var chartData = new
        {
            labels = new[] { "January", "February", "March", "April" },
            datasets = new[]
            {
                new
                {
                    label = "Daily Sales",
                    backgroundColor = new[]
                    {
                        "rgb(255, 99, 132)", "rgb(54, 162, 235)", "rgb(255, 206, 86)", "rgb(75, 192, 192)"
                    },
                    borderColor = new[]
                    {
                        "rgb(255, 99, 132)", "rgb(54, 162, 235)", "rgb(255, 206, 86)", "rgb(75, 192, 192)"
                    },
                    data = DailySalesData
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
            }
        };

        await JS.InvokeVoidAsync("BlazorChartManagerService.showChart", "dailySales", "bar", chartData, chartOptions);
    }

    private async Task ShowProfitShareAsync()
    {
        var chartData = new
        {
            labels = new[] { "Product Sales", "Online Courses", "Custom Development" },
            datasets = new[]
            {
                new
                {
                    label = "Profit Share",
                    backgroundColor = new[]
                    {
                        "rgb(255, 99, 132)",
                        "rgb(54, 162, 235)",
                        "rgb(255, 206, 86)"
                    },
                    borderColor = new[]
                    {
                        "rgba(255, 255, 255, 1)",
                        "rgba(255, 255, 255, 1)",
                        "rgba(255, 255, 255, 1)"
                    },
                    borderWidth = 2,
                    data = ProfitShareData
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
                    display = true,
                    position = "right"
                }
            }
        };

        await JS.InvokeVoidAsync("BlazorChartManagerService.showChart", "profitShare", "doughnut", chartData,
            chartOptions);
    }
}