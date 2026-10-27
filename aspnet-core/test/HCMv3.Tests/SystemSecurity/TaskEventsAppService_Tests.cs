using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class TaskEventsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private TaskEventsAppService CreateAppService()
    {
        return CreateService(() => new TaskEventsAppService(FakeRepository.For<TaskEvent>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TaskEvent()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();

        await service.CreateOrEdit(new CreateOrEditTaskEventDto
        {
            RefEmployeeId = employeeId,
            Subject = "Renew Contract",
            DueDate = new DateTime(2026, 3, 1),
            StartDate = new DateTime(2026, 2, 1),
            Status = TaskEventStatus.NotStarted
        });

        var result = await service.GetAll(new GetAllTaskEventsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Subject.ShouldBe("Renew Contract");
        result.Items[0].RefEmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task Should_Filter_By_RefEmployeeId()
    {
        var service = CreateAppService();
        var employee1 = CreateEmployee();
        var employee2 = CreateEmployee();

        await service.CreateOrEdit(new CreateOrEditTaskEventDto { RefEmployeeId = employee1, DueDate = DateTime.Now, StartDate = DateTime.Now });
        await service.CreateOrEdit(new CreateOrEditTaskEventDto { RefEmployeeId = employee2, DueDate = DateTime.Now, StartDate = DateTime.Now });

        var result = await service.GetAll(new GetAllTaskEventsInput { RefEmployeeId = employee2 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].RefEmployeeId.ShouldBe(employee2);
    }

    [Fact]
    public async Task Should_Delete_TaskEvent()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditTaskEventDto { DueDate = DateTime.Now, StartDate = DateTime.Now });
        var created = await service.GetAll(new GetAllTaskEventsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllTaskEventsInput());
        result.TotalCount.ShouldBe(0);
    }
}
