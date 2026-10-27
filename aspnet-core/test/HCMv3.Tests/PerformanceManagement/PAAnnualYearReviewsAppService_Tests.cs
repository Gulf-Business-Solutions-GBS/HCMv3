using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAAnnualYearReviewsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAAnnualYearReviewsAppService CreateAppService()
    {
        return CreateService(() => new PAAnnualYearReviewsAppService(
            FakeRepository.For<PAAnnualYearReview>(Context),
            FakeRepository.For<PAForm>(Context),
            FakeRepository.For<PARating>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_AnnualYearReview()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAAnnualYearReviewDto
        {
            PAFormId = paFormId,
            KpiInfoId = kpiInfoId,
            Weight = 50,
            Target = "T"
        });

        var result = await service.GetAll(new GetAllPAAnnualYearReviewsInput { PAFormId = paFormId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Weight.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Recompute_OverallScore_On_Create()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var ratingId = CreatePARating("4"); // Rating.Code = "4"
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAAnnualYearReviewDto
        {
            PAFormId = paFormId,
            KpiInfoId = kpiInfoId,
            Weight = 50,
            RatingId = ratingId
        });

        // OverallScore = ratingCode(4) * weight(50) / 100 = 2
        var paForm = Context.PAForms.Find(paFormId);
        paForm.OverallScore.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Recompute_OverallScore_On_Delete()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var ratingId = CreatePARating("4");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAAnnualYearReviewDto { PAFormId = paFormId, KpiInfoId = kpiInfoId, Weight = 50, RatingId = ratingId });

        var all = await service.GetAll(new GetAllPAAnnualYearReviewsInput { PAFormId = paFormId });
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var paForm = Context.PAForms.Find(paFormId);
        paForm.OverallScore.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_KpiInfo_For_PAForm()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var paFormId = CreatePAForm(periodId, employeeId);
        var kpiInfoId = CreateKpiInfo("K1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAAnnualYearReviewDto { PAFormId = paFormId, KpiInfoId = kpiInfoId, Weight = 30 });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPAAnnualYearReviewDto { PAFormId = paFormId, KpiInfoId = kpiInfoId, Weight = 20 }));
    }
}
