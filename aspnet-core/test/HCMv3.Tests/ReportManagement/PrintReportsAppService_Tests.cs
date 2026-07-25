using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class PrintReportsAppService_Tests : ReportManagementAppServiceTestBase
{
    private PrintReportsAppService CreateAppService()
    {
        return CreateService(() => new PrintReportsAppService(FakeRepository.For<PrintReport>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PrintReport()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPrintReportDto
        {
            NameEn = "Payroll Summary",
            NameAr = "ملخص الرواتب",
            ReportName = "PayrollSummary.rpt",
            IsActive = true,
            ReportCategory = ReportCategory.Default,
            ReportCreatedOn = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllPrintReportsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].FullReportName.ShouldBe("Payroll Summary - ملخص الرواتب");
    }

    [Fact]
    public async Task Should_Throw_When_Names_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPrintReportDto { NameEn = "Report A", NameAr = "A" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPrintReportDto { NameEn = "Report A", NameAr = "B" }));
    }
}
