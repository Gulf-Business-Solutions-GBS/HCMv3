using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class EmployeesNotificationLogsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private EmployeesNotificationLogsAppService CreateAppService()
    {
        return CreateService(() => new EmployeesNotificationLogsAppService(FakeRepository.For<EmployeesNotificationLog>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeesNotificationLog()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var taskEventId = CreateTaskEvent();
        var emailNotificationLogId = CreateEmailNotificationLog();

        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationLogDto
        {
            EmployeeId = employeeId,
            TaskEventId = taskEventId,
            EmailNotificationLogId = emailNotificationLogId,
            NotificationType = "PunchIn",
            Date = new DateTime(2026, 5, 1),
            DismissTime = new DateTime(2026, 5, 1, 9, 0, 0)
        });

        var result = await service.GetAll(new GetAllEmployeesNotificationLogsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NotificationType.ShouldBe("PunchIn");
        result.Items[0].EmailNotificationLogId.ShouldBe(emailNotificationLogId);
    }

    [Fact]
    public async Task Should_Filter_By_EmployeeId()
    {
        var service = CreateAppService();
        var employee1 = CreateEmployee();
        var employee2 = CreateEmployee();

        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationLogDto { EmployeeId = employee1, Date = DateTime.Now });
        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationLogDto { EmployeeId = employee2, Date = DateTime.Now });

        var result = await service.GetAll(new GetAllEmployeesNotificationLogsInput { EmployeeId = employee2 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee2);
    }

    [Fact]
    public async Task Should_Delete_EmployeesNotificationLog()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationLogDto { Date = DateTime.Now });
        var created = await service.GetAll(new GetAllEmployeesNotificationLogsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllEmployeesNotificationLogsInput());
        result.TotalCount.ShouldBe(0);
    }
}
