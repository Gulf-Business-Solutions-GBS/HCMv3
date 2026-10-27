using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class SystemNotificationsAppService_Tests : SystemSecurityAppServiceTestBase
{
    private SystemNotificationsAppService CreateAppService()
    {
        return CreateService(() => new SystemNotificationsAppService(FakeRepository.For<SystemNotification>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SystemNotification()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSystemNotificationDto
        {
            NotificationName = "Contract Expiry",
            NotificationConfigId = configId,
            ReminderDays = 30,
            IsActive = true
        });

        var result = await service.GetAll(new GetAllSystemNotificationsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NotificationName.ShouldBe("Contract Expiry");
        result.Items[0].ReminderDays.ShouldBe(30);
    }

    [Fact]
    public async Task Should_Update_SystemNotification()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSystemNotificationDto { NotificationName = "N1", NotificationConfigId = configId, ReminderDays = 5 });
        var created = await service.GetAll(new GetAllSystemNotificationsInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditSystemNotificationDto { Id = id, NotificationName = "N1", NotificationConfigId = configId, ReminderDays = 15 });

        var edited = await service.GetSystemNotificationForEdit(new EntityDto(id));
        edited.SystemNotification.ReminderDays.ShouldBe(15);
    }

    [Fact]
    public async Task Should_Delete_SystemNotification()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSystemNotificationDto { NotificationName = "N1", NotificationConfigId = configId });
        var created = await service.GetAll(new GetAllSystemNotificationsInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllSystemNotificationsInput());
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Filter_By_Name()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSystemNotificationDto { NotificationName = "Alpha", NotificationConfigId = configId });
        await service.CreateOrEdit(new CreateOrEditSystemNotificationDto { NotificationName = "Beta", NotificationConfigId = configId });

        var result = await service.GetAll(new GetAllSystemNotificationsInput { Filter = "Alpha" });
        result.TotalCount.ShouldBe(1);
        result.Items[0].NotificationName.ShouldBe("Alpha");
    }
}
