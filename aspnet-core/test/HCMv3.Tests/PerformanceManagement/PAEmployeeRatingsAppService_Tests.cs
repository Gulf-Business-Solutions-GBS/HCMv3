using System.Threading.Tasks;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAEmployeeRatingsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PAEmployeeRatingsAppService CreateAppService()
    {
        return CreateService(() => new PAEmployeeRatingsAppService(
            FakeRepository.For<PAEmployeeRating>(Context),
            FakeRepository.For<PAEmployeeReviewer>(Context),
            FakeRepository.For<PAEmployee>(Context),
            FakeRepository.For<PARating>(Context)));
    }

    private (int paEmployeeId, int reviewerRecordId, int detailId) CreateAppraisalSetup()
    {
        var employeeId = CreateEmployee();
        var reviewerEmployeeId = CreateEmployee("R", "V");
        var cycleId = CreatePACycle("C1");
        var formTypeId = CreateFormType();
        var paEmployeeId = CreatePAEmployee(employeeId, cycleId, formTypeId);
        var kpiInfoId = CreateKpiInfo("K1");

        var reviewer = new PAEmployeeReviewer
        {
            TenantId = AbpSession.TenantId ?? 1,
            PAEmployeeId = paEmployeeId,
            EmployeeId = reviewerEmployeeId
        };
        Context.PAEmployeeReviewers.Add(reviewer);

        var detail = new PAEmployeeDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            PAEmployeeId = paEmployeeId,
            KpiInfoId = kpiInfoId,
            Weight = 100
        };
        Context.PAEmployeeDetails.Add(detail);
        Context.SaveChanges();

        return (paEmployeeId, reviewer.Id, detail.Id);
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Rating()
    {
        var (paEmployeeId, reviewerId, detailId) = CreateAppraisalSetup();
        var ratingId = CreatePARating("5");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeRatingDto
        {
            PAEmployeeId = paEmployeeId,
            PAEmployeeReviewerId = reviewerId,
            PAEmployeeDetailId = detailId,
            RatingId = ratingId,
            Marks = 90
        });

        var result = await service.GetAll(new GetAllPAEmployeeRatingsInput { PAEmployeeId = paEmployeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].RatingId.ShouldBe(ratingId);
    }

    [Fact]
    public async Task Should_Update_Reviewer_And_PAEmployee_AverageScore_On_Create()
    {
        var (paEmployeeId, reviewerId, detailId) = CreateAppraisalSetup();
        var ratingId = CreatePARating("4"); // Code "4" parses to rating code 4
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeRatingDto
        {
            PAEmployeeId = paEmployeeId,
            PAEmployeeReviewerId = reviewerId,
            PAEmployeeDetailId = detailId,
            RatingId = ratingId
        });

        var reviewer = Context.PAEmployeeReviewers.Find(reviewerId);
        reviewer.AverageScore.ShouldBe(4);
        reviewer.AverageRatingId.ShouldBe(ratingId);

        var paEmployee = Context.PAEmployees.Find(paEmployeeId);
        paEmployee.OverallAverageScore.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Reset_AverageScore_When_Rating_Deleted()
    {
        var (paEmployeeId, reviewerId, detailId) = CreateAppraisalSetup();
        var ratingId = CreatePARating("4");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPAEmployeeRatingDto
        {
            PAEmployeeId = paEmployeeId,
            PAEmployeeReviewerId = reviewerId,
            PAEmployeeDetailId = detailId,
            RatingId = ratingId
        });

        var all = await service.GetAll(new GetAllPAEmployeeRatingsInput { PAEmployeeId = paEmployeeId });
        var ratingRecordId = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(ratingRecordId));

        var reviewer = Context.PAEmployeeReviewers.Find(reviewerId);
        reviewer.AverageScore.ShouldBe(0);
        reviewer.AverageRatingId.ShouldBeNull();
    }
}
