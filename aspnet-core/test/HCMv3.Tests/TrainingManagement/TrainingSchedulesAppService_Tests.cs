using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TrainingManagement;
using HCMv3.TrainingManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TrainingManagement;

// ReSharper disable once InconsistentNaming
public class TrainingSchedulesAppService_Tests : TrainingManagementAppServiceTestBase
{
    private TrainingSchedulesAppService CreateAppService()
    {
        return CreateService(() => new TrainingSchedulesAppService(FakeRepository.For<TrainingSchedule>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TrainingSchedule()
    {
        var courseId = CreateTrainingCourse();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTrainingScheduleDto
        {
            NameEn = "Batch 1",
            NameAr = "الدفعة 1",
            TrainingCourseId = courseId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 10)
        });

        var result = await service.GetAll(new GetAllTrainingSchedulesInput { TrainingCourseId = courseId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Batch 1");
        result.Items[0].TrainingCourseId.ShouldBe(courseId);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var courseId = CreateTrainingCourse();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTrainingScheduleDto
            {
                NameEn = "Batch 1",
                NameAr = "Batch 1",
                TrainingCourseId = courseId,
                StartDate = new DateTime(2026, 1, 10),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var courseId = CreateTrainingCourse();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTrainingScheduleDto
        {
            NameEn = "Batch 1",
            NameAr = "Batch 1",
            TrainingCourseId = courseId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 10)
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTrainingScheduleDto
            {
                NameEn = "Batch 1",
                NameAr = "Other",
                TrainingCourseId = courseId,
                StartDate = new DateTime(2026, 2, 1),
                EndDate = new DateTime(2026, 2, 10)
            }));
    }
}
