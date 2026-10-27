using System.Linq;
using System.Threading.Tasks;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSettings;

/// <summary>The presentation-only settings, read by any signed-in user in one call.</summary>
// ReSharper disable once InconsistentNaming
public class TenantUiSettingsAppService_Tests : AppTestBase
{
    private readonly ITenantUiSettingsAppService _appService;

    public TenantUiSettingsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ITenantUiSettingsAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    [Fact]
    public async Task Should_Reflect_The_Org_Payroll_Report_General_And_Time_Settings()
    {
        UsingDbContext(context =>
        {
            var org = context.Set<ManageOrgSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                      ?? context.Set<ManageOrgSettings>().Add(new ManageOrgSettings { TenantId = TenantId }).Entity;
            org.EnablePostingGroup = true;
            org.EnableOrgChart3Levels = true;

            var payroll = context.Set<ManagePayrollSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                          ?? context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings { TenantId = TenantId }).Entity;
            payroll.ShowBillingTypeInPaymentScreen = true;
            payroll.AllowSimulationRun = true;

            var report = context.Set<ManageReportSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                         ?? context.Set<ManageReportSettings>().Add(new ManageReportSettings { TenantId = TenantId }).Entity;
            report.DecimalInReports = 2;

            var general = context.Set<ManageGeneralSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                          ?? context.Set<ManageGeneralSettings>().Add(new ManageGeneralSettings { TenantId = TenantId }).Entity;
            general.DisableAutoEmpcode = true;

            var time = context.Set<ManageTimeSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                       ?? context.Set<ManageTimeSettings>().Add(new ManageTimeSettings { TenantId = TenantId }).Entity;
            time.EnableIsTravelling = true;
        });

        var settings = await _appService.GetTenantUiSettings();

        settings.EnablePostingGroup.ShouldBeTrue();
        settings.EnableBudgetingGroup.ShouldBeFalse();
        settings.EnableOrgChart3Levels.ShouldBeTrue();
        settings.ShowBillingTypeInPaymentScreen.ShouldBeTrue();
        settings.AllowSimulationRun.ShouldBeTrue();
        settings.DecimalInReports.ShouldBe(2);
        settings.DisableAutoEmpcode.ShouldBeTrue();
        settings.EnableIsTravelling.ShouldBeTrue();
    }

    [Fact]
    public async Task Decimals_Should_Default_To_Three_When_Unset()
    {
        UsingDbContext(context =>
        {
            var report = context.Set<ManageReportSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                         ?? context.Set<ManageReportSettings>().Add(new ManageReportSettings { TenantId = TenantId }).Entity;
            report.DecimalInReports = 0;
        });

        (await _appService.GetTenantUiSettings()).DecimalInReports.ShouldBe(3);
    }
}
