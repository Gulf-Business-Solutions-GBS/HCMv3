using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TrainingManagement;
using HCMv3.TrainingManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TrainingManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeTrainingsAppService_Tests : TrainingManagementAppServiceTestBase
{
    private EmployeeTrainingsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeTrainingsAppService(FakeRepository.For<EmployeeTraining>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeeTraining()
    {
        var employeeId = CreateEmployee();
        var courseId = CreateTrainingCourse();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), courseId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTrainingDto
        {
            EmployeeId = employeeId,
            TrainingCourseId = courseId,
            TrainingScheduleId = scheduleId,
            CompletionStatus = TrainingCompletionStatus.Pending,
            Score = 0
        });

        var result = await service.GetAll(new GetAllEmployeeTrainingsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TrainingScheduleId.ShouldBe(scheduleId);
        result.Items[0].CompletionStatus.ShouldBe(TrainingCompletionStatus.Pending);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Employee_Course_Schedule_Combination()
    {
        var employeeId = CreateEmployee();
        var courseId = CreateTrainingCourse();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10), courseId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTrainingDto
        {
            EmployeeId = employeeId,
            TrainingCourseId = courseId,
            TrainingScheduleId = scheduleId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeeTrainingDto
            {
                EmployeeId = employeeId,
                TrainingCourseId = courseId,
                TrainingScheduleId = scheduleId
            }));
    }
}
