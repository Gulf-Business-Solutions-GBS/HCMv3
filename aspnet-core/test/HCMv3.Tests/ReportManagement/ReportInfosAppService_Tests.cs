using System.Threading.Tasks;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class ReportInfosAppService_Tests : ReportManagementAppServiceTestBase
{
    private ReportInfosAppService CreateAppService()
    {
        return CreateService(() => new ReportInfosAppService(FakeRepository.For<ReportInfo>(Context)));
    }

    private int CreateReportType(string nameEn)
    {
        var entity = new ReportType { TenantId = AbpSession.TenantId ?? 1, NameEn = nameEn };
        Context.ReportTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateDomainType(string nameEn)
    {
        var entity = new DomainType { TenantId = AbpSession.TenantId ?? 1, NameEn = nameEn };
        Context.DomainTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ReportInfo()
    {
        var reportTypeId = CreateReportType("Ad-Hoc");
        var domainTypeId = CreateDomainType("Employee");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditReportInfoDto
        {
            ReportName = "Employee Listing",
            ReportTypeId = reportTypeId,
            DomainTypeId = domainTypeId,
            IsActive = true
        });

        var result = await service.GetAll(new GetAllReportInfosInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].ReportTypeName.ShouldBe("Ad-Hoc");
        result.Items[0].DomainTypeName.ShouldBe("Employee");
    }
}
