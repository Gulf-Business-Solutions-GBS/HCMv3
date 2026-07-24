using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAMidYearReviewsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAMidYearReviewsAppService CreateAppService()
    {
        return CreateService(() => new PAMidYearReviewsAppService(FakeRepository.For<PAMidYearReview>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_MidYearReview()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAMidYearReviewDto
        {
            PAFormId = paFormId,
            KpiInfoId = kpiInfoId,
            Weight = 50,
            Target = "T"
        });

        var result = await service.GetAll(new GetAllPAMidYearReviewsInput { PAFormId = paFormId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Weight.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_KpiInfo_For_PAForm()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAMidYearReviewDto { PAFormId = paFormId, KpiInfoId = kpiInfoId, Weight = 30 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAMidYearReviewDto { PAFormId = paFormId, KpiInfoId = kpiInfoId, Weight = 20 }));
    }

    [Fact]
    public async Task Should_Delete_MidYearReview()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAMidYearReviewDto { PAFormId = paFormId, KpiInfoId = kpiInfoId, Weight = 30 });

        var all = await service.GetAll(new GetAllPAMidYearReviewsInput { PAFormId = paFormId });
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPAMidYearReviewsInput { PAFormId = paFormId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
