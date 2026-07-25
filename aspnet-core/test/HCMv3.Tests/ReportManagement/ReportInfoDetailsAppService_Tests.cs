using System.Threading.Tasks;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class ReportInfoDetailsAppService_Tests : ReportManagementAppServiceTestBase
{
    private ReportInfoDetailsAppService CreateAppService()
    {
        return CreateService(() => new ReportInfoDetailsAppService(FakeRepository.For<ReportInfoDetail>(Context)));
    }

    private int CreateReportInfo(string reportName)
    {
        var reportType = new ReportType { TenantId = AbpSession.TenantId ?? 1, NameEn = "T" };
        Context.ReportTypes.Add(reportType);
        var domainType = new DomainType { TenantId = AbpSession.TenantId ?? 1, NameEn = "D" };
        Context.DomainTypes.Add(domainType);
        Context.SaveChanges();

        var entity = new ReportInfo
        {
            TenantId = AbpSession.TenantId ?? 1,
            ReportName = reportName,
            ReportTypeId = reportType.Id,
            DomainTypeId = domainType.Id
        };
        Context.ReportInfos.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ReportInfoDetail()
    {
        var reportInfoId = CreateReportInfo("Employee Listing");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditReportInfoDetailDto
        {
            ReportInfoId = reportInfoId,
            EntityName = "Employee",
            PropertyName = "FirstNameEn",
            PropertyDisplayName = "First Name"
        });

        var result = await service.GetAll(new GetAllReportInfoDetailsInput { ReportInfoId = reportInfoId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].PropertyName.ShouldBe("FirstNameEn");
    }
}
