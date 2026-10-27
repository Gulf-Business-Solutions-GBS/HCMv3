using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class EmployeesNotificationsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private EmployeesNotificationsAppService CreateAppService()
    {
        return CreateService(() => new EmployeesNotificationsAppService(FakeRepository.For<EmployeesNotification>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeesNotification()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var taskEventId = CreateTaskEvent();

        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationDto
        {
            EmployeeId = employeeId,
            TaskEventId = taskEventId,
            IsNotificationRead = false
        });

        var result = await service.GetAll(new GetAllEmployeesNotificationsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
        result.Items[0].TaskEventId.ShouldBe(taskEventId);
        result.Items[0].IsNotificationRead.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Filter_By_EmployeeId()
    {
        var service = CreateAppService();
        var employee1 = CreateEmployee();
        var employee2 = CreateEmployee();

        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationDto { EmployeeId = employee1 });
        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationDto { EmployeeId = employee2 });

        var result = await service.GetAll(new GetAllEmployeesNotificationsInput { EmployeeId = employee2 });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employee2);
    }

    [Fact]
    public async Task Should_Mark_As_Read_On_Update()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();

        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationDto { EmployeeId = employeeId, IsNotificationRead = false });
        var created = await service.GetAll(new GetAllEmployeesNotificationsInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationDto { Id = id, EmployeeId = employeeId, IsNotificationRead = true });

        var forEdit = await service.GetEmployeesNotificationForEdit(new EntityDto(id));
        forEdit.EmployeesNotification.IsNotificationRead.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Delete_EmployeesNotification()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();

        await service.CreateOrEdit(new CreateOrEditEmployeesNotificationDto { EmployeeId = employeeId });
        var created = await service.GetAll(new GetAllEmployeesNotificationsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllEmployeesNotificationsInput());
        result.TotalCount.ShouldBe(0);
    }
}
