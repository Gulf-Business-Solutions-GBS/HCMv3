using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PATimelineExceptionsAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PATimelineExceptionsAppService CreateAppService()
    {
        return CreateService(() => new PATimelineExceptionsAppService(FakeRepository.For<PATimelineException>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Exception()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPATimelineExceptionDto
        {
            PAPeriodId = periodId,
            EmployeeId = employeeId,
            ActivityType = ActivityType.Plan,
            ExtensionDate = new DateTime(2026, 3, 1)
        });

        var result = await service.GetAll(new GetAllPATimelineExceptionsInput { PAPeriodId = periodId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].ActivityType.ShouldBe(ActivityType.Plan);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Period_Employee_ActivityType()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPATimelineExceptionDto
        {
            PAPeriodId = periodId,
            EmployeeId = employeeId,
            ActivityType = ActivityType.Plan,
            ExtensionDate = new DateTime(2026, 3, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPATimelineExceptionDto
            {
                PAPeriodId = periodId,
                EmployeeId = employeeId,
                ActivityType = ActivityType.Plan,
                ExtensionDate = new DateTime(2026, 4, 1)
            }));
    }

    [Fact]
    public async Task Should_Delete_Exception()
    {
        var periodId = CreatePAPeriod("2026");
        var employeeId = CreateEmployee();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPATimelineExceptionDto
        {
            PAPeriodId = periodId,
            EmployeeId = employeeId,
            ActivityType = ActivityType.Plan,
            ExtensionDate = new DateTime(2026, 3, 1)
        });

        var all = await service.GetAll(new GetAllPATimelineExceptionsInput { PAPeriodId = periodId });
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPATimelineExceptionsInput { PAPeriodId = periodId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
