using System;
using System.Linq;
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

    private void SetReportSettings(bool showPeriod, int monthLimit)
    {
        UsingDbContext(context =>
        {
            var tenantId = AbpSession.TenantId ?? 1;
            var settings = context.Set<HCMv3.SystemSettings.ManageReportSettings>().FirstOrDefault(s => s.TenantId == tenantId)
                           ?? context.Set<HCMv3.SystemSettings.ManageReportSettings>()
                               .Add(new HCMv3.SystemSettings.ManageReportSettings { TenantId = tenantId }).Entity;
            settings.ShowPeriodInLeaveProvision = showPeriod;
            settings.PeriodMonthLimitInLeaveProvision = monthLimit;
        });
    }

    /// <summary>Legacy uscRptLeaveProvision lines 450-456: "Allowed months : N" when the span is too long.</summary>
    [Fact]
    public async Task Should_Refuse_A_Span_Beyond_The_Month_Limit_When_The_Period_Is_Shown()
    {
        SetReportSettings(showPeriod: true, monthLimit: 2);

        var exception = await Assert.ThrowsAsync<Abp.UI.UserFriendlyException>(() => _appService.GetLeaveProvisionReport(
            new GetLeaveProvisionReportInput
            {
                FromDate = new DateTime(2024, 1, 1),
                ToDate = new DateTime(2024, 3, 31),
                MaxResultCount = 100
            }));

        exception.Message.ShouldContain("2");
    }

    /// <summary>With the period hidden (legacy hides the TO fields), a long span is simply the FROM month.</summary>
    [Fact]
    public async Task Should_Ignore_The_To_Date_When_The_Period_Is_Not_Shown()
    {
        SetReportSettings(showPeriod: false, monthLimit: 1);

        var result = await _appService.GetLeaveProvisionReport(new GetLeaveProvisionReportInput
        {
            FromDate = new DateTime(2024, 1, 1),
            ToDate = new DateTime(2024, 12, 31),
            MaxResultCount = 100
        });

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
