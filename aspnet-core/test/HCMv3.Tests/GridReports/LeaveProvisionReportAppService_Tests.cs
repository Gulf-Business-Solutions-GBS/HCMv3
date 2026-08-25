using System;
using System.Threading.Tasks;
using HCMv3.GridReports.QuotaProvision;
using HCMv3.GridReports.QuotaProvision.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class LeaveProvisionReportAppService_Tests : AppTestBase
{
    private readonly ILeaveProvisionReportAppService _appService;

    public LeaveProvisionReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILeaveProvisionReportAppService>();
    }

    [Fact]
    public async Task Should_Return_Empty_When_No_Salary_Register_Exists_For_The_Period()
    {
        var result = await _appService.GetLeaveProvisionReport(new GetLeaveProvisionReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 1, 31),
            MaxResultCount = 100
        });

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_Without_Throwing_When_No_Data_Matches()
    {
        var file = await _appService.GetLeaveProvisionReportToExcel(new GetLeaveProvisionReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 1, 31),
            MaxResultCount = 100
        });

        file.FileName.ShouldBe("LeaveProvision.xlsx");
    }
}
