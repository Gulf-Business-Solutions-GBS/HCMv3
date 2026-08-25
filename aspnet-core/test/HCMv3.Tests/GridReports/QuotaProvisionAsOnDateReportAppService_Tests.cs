using System;
using System.Threading.Tasks;
using HCMv3.GridReports.QuotaProvision;
using HCMv3.GridReports.QuotaProvision.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class QuotaProvisionAsOnDateReportAppService_Tests : AppTestBase
{
    private readonly IQuotaProvisionAsOnDateReportAppService _appService;

    public QuotaProvisionAsOnDateReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IQuotaProvisionAsOnDateReportAppService>();
    }

    [Fact]
    public async Task Should_Return_Empty_When_No_Leave_Type_Is_Selected()
    {
        var result = await _appService.GetQuotaProvisionAsOnDateReport(new GetQuotaProvisionAsOnDateReportInput
        {
            FromDate = new DateTime(2024, 6, 1),
            MaxResultCount = 100
        });

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Return_Empty_When_Leave_Type_Code_Does_Not_Exist()
    {
        var result = await _appService.GetQuotaProvisionAsOnDateReport(new GetQuotaProvisionAsOnDateReportInput
        {
            FromDate = new DateTime(2024, 6, 1),
            LeaveTypeCode = "DOES-NOT-EXIST",
            MaxResultCount = 100
        });

        result.Items.ShouldBeEmpty();
    }
}
