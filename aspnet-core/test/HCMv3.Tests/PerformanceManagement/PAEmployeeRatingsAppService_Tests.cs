using System.Threading.Tasks;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PAEmployeeRatingsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _settings =
        NSubstitute.Substitute.For<HCMv3.SystemSettings.Services.ISysSettingsAccessor>();

    private PAEmployeeRatingsAppService CreateAppService(
        bool marksInReview = false,
        HCMv3.SystemSettings.MarksOn marksOn = HCMv3.SystemSettings.MarksOn.Ratings)
    {
        NSubstitute.SubstituteExtensions.Returns(_settings.GetAppraisalSettingsAsync(),
            Task.FromResult(new HCMv3.SystemSettings.ManageAppraisalSettings
            {
                TenantId = AbpSession.TenantId ?? 1,
                EnableMarksInPAReview = marksInReview,
                MarksOn = marksOn
            }));

        return CreateService(() => new PAEmployeeRatingsAppService(
            FakeRepository.For<PAEmployeeRating>(Context),
            FakeRepository.For<PAEmployeeReviewer>(Context),
            FakeRepository.For<PAEmployee>(Context),
            FakeRepository.For<PARating>(Context),
            _settings));
    }

    /// <summary>
    /// Legacy "Marks enabled, MarksOn = Ratings": score = sum((weight / maxRatingCode) * marks) scaled back by
    /// maxRatingCode / 100. One KPI weighted 100 with 80 marks against ratings coded 1..4 scores 80.
    /// </summary>
    [Fact]
    public async Task With_Marks_On_Ratings_Should_Score_Weighted_Marks_Against_The_Highest_Rating_Code()
    {
        var (paEmployeeId, reviewerId, detailId) = CreateAppraisalSetup();
        CreatePARating("1");
        CreatePARating("2");
        CreatePARating("3");
        CreatePARating("4");
        var service = CreateAppService(marksInReview: true, marksOn: HCMv3.SystemSettings.MarksOn.Ratings);

        await service.CreateOrEdit(new CreateOrEditPAEmployeeRatingDto
        {
            PAEmployeeId = paEmployeeId,
            PAEmployeeReviewerId = reviewerId,
            PAEmployeeDetailId = detailId,
            Marks = 80
        });

        var reviewer = await Context.PAEmployeeReviewers.FindAsync(reviewerId);
        reviewer.TotalMarks.ShouldBe(80);
        reviewer.AverageScore.ShouldBe(80);
    }

    /// <summary>Legacy "Marks enabled, MarksOn = Weight": only the total marks are kept.</summary>
    [Fact]
    public async Task With_Marks_On_Weight_Should_Sum_The_Marks_Only()
    {
        var (paEmployeeId, reviewerId, detailId) = CreateAppraisalSetup();
        var service = CreateAppService(marksInReview: true, marksOn: HCMv3.SystemSettings.MarksOn.Weight);

        await service.CreateOrEdit(new CreateOrEditPAEmployeeRatingDto
        {
            PAEmployeeId = paEmployeeId,
            PAEmployeeReviewerId = reviewerId,
            PAEmployeeDetailId = detailId,
            Marks = 65
        });

        var reviewer = await Context.PAEmployeeReviewers.FindAsync(reviewerId);
        reviewer.TotalMarks.ShouldBe(65);
        reviewer.AverageScore.ShouldBe(0);
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
