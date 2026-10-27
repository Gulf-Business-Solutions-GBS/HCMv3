using System.Threading.Tasks;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ReportManagement;

// ReSharper disable once InconsistentNaming
public class ReportDetailsAppService_Tests : ReportManagementAppServiceTestBase
{
    private ReportDetailsAppService CreateAppService()
    {
        return CreateService(() => new ReportDetailsAppService(FakeRepository.For<ReportDetail>(Context)));
    }

    private int CreateReportMaster(string nameEn)
    {
        var entity = new ReportMaster
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.ReportMasters.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ReportDetail()
    {
        var masterId = CreateReportMaster("Master1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditReportDetailDto
        {
            ReportMasterId = masterId,
            DrillDownLevel = 1
        });

        var result = await service.GetAll(new GetAllReportDetailsInput { ReportMasterId = masterId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].ReportMasterName.ShouldBe("Master1");
    }
}
