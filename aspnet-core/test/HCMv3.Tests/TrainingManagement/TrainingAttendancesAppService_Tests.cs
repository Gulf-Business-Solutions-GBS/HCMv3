using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TrainingManagement;
using HCMv3.TrainingManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TrainingManagement;

// ReSharper disable once InconsistentNaming
public class TrainingAttendancesAppService_Tests : TrainingManagementAppServiceTestBase
{
    private TrainingAttendancesAppService CreateAppService()
    {
        return CreateService(() => new TrainingAttendancesAppService(
            FakeRepository.For<TrainingAttendance>(Context),
            FakeRepository.For<TrainingSchedule>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TrainingAttendance()
    {
        var employeeId = CreateEmployee();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTrainingAttendanceDto
        {
            TrainingScheduleId = scheduleId,
            EmployeeId = employeeId,
            DayDate = new DateTime(2026, 1, 5),
            Attendance = TrainingAttendanceStatus.Present
        });

        var result = await service.GetAll(new GetAllTrainingAttendancesInput { TrainingScheduleId = scheduleId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Attendance.ShouldBe(TrainingAttendanceStatus.Present);
    }

    [Fact]
    public async Task Should_Throw_When_DayDate_Outside_Schedule_Range()
    {
        var employeeId = CreateEmployee();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTrainingAttendanceDto
            {
                TrainingScheduleId = scheduleId,
                EmployeeId = employeeId,
                DayDate = new DateTime(2026, 1, 20),
                Attendance = TrainingAttendanceStatus.Present
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Employee_Schedule_DayDate()
    {
        var employeeId = CreateEmployee();
        var scheduleId = CreateTrainingSchedule(new DateTime(2026, 1, 1), new DateTime(2026, 1, 10));
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTrainingAttendanceDto
        {
            TrainingScheduleId = scheduleId,
            EmployeeId = employeeId,
            DayDate = new DateTime(2026, 1, 5),
            Attendance = TrainingAttendanceStatus.Present
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditTrainingAttendanceDto
            {
                TrainingScheduleId = scheduleId,
                EmployeeId = employeeId,
                DayDate = new DateTime(2026, 1, 5),
                Attendance = TrainingAttendanceStatus.Absent
            }));
    }
}
