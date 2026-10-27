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
public class EmpPerformanceHistoriesAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private EmpPerformanceHistoriesAppService CreateAppService()
    {
        return CreateService(() => new EmpPerformanceHistoriesAppService(FakeRepository.For<EmpPerformanceHistory>(Context)));
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
    public async Task Should_Create_And_Retrieve_EmpPerformanceHistory()
    {
        var employeeId = CreateEmployee();
        var ratingId = CreateRating();
        var nineBoxRatingId = CreateNineBoxRating();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmpPerformanceHistoryDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            Score = 4.5,
            RatingId = ratingId,
            Source = ScoreSource.Manual,
            NineBoxRatingId = nineBoxRatingId
        });

        var result = await service.GetAll(new GetAllEmpPerformanceHistoriesInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items.Single().Score.ShouldBe(4.5);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var employeeId = CreateEmployee();
        var ratingId = CreateRating();
        var nineBoxRatingId = CreateNineBoxRating();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmpPerformanceHistoryDto
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

        await service.CreateOrEdit(new CreateOrEditEmpPerformanceHistoryDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            RatingId = ratingId,
            NineBoxRatingId = nineBoxRatingId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmpPerformanceHistoryDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 12, 31),
                RatingId = ratingId,
                NineBoxRatingId = nineBoxRatingId
            }));
    }

    [Fact]
    public async Task Should_Delete_EmpPerformanceHistory()
    {
        var employeeId = CreateEmployee();
        var ratingId = CreateRating();
        var nineBoxRatingId = CreateNineBoxRating();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmpPerformanceHistoryDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            RatingId = ratingId,
            NineBoxRatingId = nineBoxRatingId
        });
        var all = await service.GetAll(new GetAllEmpPerformanceHistoriesInput { EmployeeId = employeeId });
        var id = all.Items.Single().Id;

        await service.Delete(new EntityDto(id));

        (await service.GetAll(new GetAllEmpPerformanceHistoriesInput { EmployeeId = employeeId })).TotalCount.ShouldBe(0);
    }
}
