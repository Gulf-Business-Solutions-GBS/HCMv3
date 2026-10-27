using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SuccessionManagement;
using HCMv3.SuccessionManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SuccessionManagement;

// ReSharper disable once InconsistentNaming
public class EmpPotentialAssessmentsAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private EmpPotentialAssessmentsAppService CreateAppService()
    {
        return CreateService(() => new EmpPotentialAssessmentsAppService(FakeRepository.For<EmpPotentialAssessment>(Context)));
    }

    private int CreateRating()
    {
        var entity = new SuccessionRating { TenantId = AbpSession.TenantId ?? 1, NameEn = "Exceeds", NameAr = "يفوق" };
        Context.SuccessionRatings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateNineBoxRating()
    {
        var entity = new NineBoxRating { TenantId = AbpSession.TenantId ?? 1, NameEn = "Star", NameAr = "نجم" };
        Context.NineBoxRatings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmpPotentialAssessment()
    {
        var employeeId = CreateEmployee();
        var ratingId = CreateRating();
        var nineBoxRatingId = CreateNineBoxRating();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmpPotentialAssessmentDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            Score = 3.5,
            RatingId = ratingId,
            Source = ScoreSource.Live,
            NineBoxRatingId = nineBoxRatingId
        });

        var result = await service.GetAll(new GetAllEmpPotentialAssessmentsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items.Single().Source.ShouldBe(ScoreSource.Live);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var employeeId = CreateEmployee();
        var ratingId = CreateRating();
        var nineBoxRatingId = CreateNineBoxRating();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmpPotentialAssessmentDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 1, 1),
                RatingId = ratingId,
                NineBoxRatingId = nineBoxRatingId
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Period_For_Same_Employee()
    {
        var employeeId = CreateEmployee();
        var ratingId = CreateRating();
        var nineBoxRatingId = CreateNineBoxRating();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmpPotentialAssessmentDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            RatingId = ratingId,
            NineBoxRatingId = nineBoxRatingId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmpPotentialAssessmentDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 3, 1),
                EndDate = new DateTime(2026, 9, 30),
                RatingId = ratingId,
                NineBoxRatingId = nineBoxRatingId
            }));
    }
}
