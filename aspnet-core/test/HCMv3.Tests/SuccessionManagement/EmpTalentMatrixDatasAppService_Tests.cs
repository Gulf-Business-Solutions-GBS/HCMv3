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
public class EmpTalentMatrixDatasAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private EmpTalentMatrixDatasAppService CreateAppService()
    {
        return CreateService(() => new EmpTalentMatrixDatasAppService(FakeRepository.For<EmpTalentMatrixData>(Context)));
    }

    private int CreateNineBoxRating(string nameEn)
    {
        var entity = new NineBoxRating { TenantId = AbpSession.TenantId ?? 1, NameEn = nameEn, NameAr = nameEn };
        Context.NineBoxRatings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmpTalentMatrixData()
    {
        var employeeId = CreateEmployee();
        var performanceRatingId = CreateNineBoxRating("Performance");
        var potentialRatingId = CreateNineBoxRating("Potential");
        var overallRatingId = CreateNineBoxRating("Overall");
        var calibratedRatingId = CreateNineBoxRating("Calibrated");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmpTalentMatrixDataDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            PerformanceRatingId = performanceRatingId,
            PotentialRatingId = potentialRatingId,
            OverallRatingId = overallRatingId,
            CalibratedRatingId = calibratedRatingId
        });

        var result = await service.GetAll(new GetAllEmpTalentMatrixDatasInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items.Single().OverallRatingId.ShouldBe(overallRatingId);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var employeeId = CreateEmployee();
        var ratingId = CreateNineBoxRating("Rating");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmpTalentMatrixDataDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 1, 1),
                PerformanceRatingId = ratingId,
                PotentialRatingId = ratingId,
                OverallRatingId = ratingId,
                CalibratedRatingId = ratingId
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Period_For_Same_Employee()
    {
        var employeeId = CreateEmployee();
        var ratingId = CreateNineBoxRating("Rating");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmpTalentMatrixDataDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            PerformanceRatingId = ratingId,
            PotentialRatingId = ratingId,
            OverallRatingId = ratingId,
            CalibratedRatingId = ratingId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmpTalentMatrixDataDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 5, 1),
                EndDate = new DateTime(2026, 12, 31),
                PerformanceRatingId = ratingId,
                PotentialRatingId = ratingId,
                OverallRatingId = ratingId,
                CalibratedRatingId = ratingId
            }));
    }
}
