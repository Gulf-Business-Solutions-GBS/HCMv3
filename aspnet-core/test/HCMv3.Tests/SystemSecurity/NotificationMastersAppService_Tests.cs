using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class NotificationMastersAppService_Tests : SystemSecurityAppServiceTestBase
{
    private NotificationMastersAppService CreateAppService()
    {
        return CreateService(() => new NotificationMastersAppService(FakeRepository.For<NotificationMaster>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_NotificationMaster()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto
        {
            NotificationType = "AbsenceRequest",
            NotificationConfigId = configId,
            IsEnabled = true
        });

        var result = await service.GetAll(new GetAllNotificationMastersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NotificationType.ShouldBe("AbsenceRequest");
        result.Items[0].IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_NotificationType_Duplicate()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = "AbsenceRequest", NotificationConfigId = configId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = "AbsenceRequest", NotificationConfigId = configId }));
    }

    [Fact]
    public async Task Should_Update_NotificationMaster()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = "AbsenceRequest", NotificationConfigId = configId });
        var created = await service.GetAll(new GetAllNotificationMastersInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { Id = id, NotificationType = "AbsenceRequest", NotificationConfigId = configId, IsEnabled = true });

        var edited = await service.GetNotificationMasterForEdit(new EntityDto(id));
        edited.NotificationMaster.IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Delete_NotificationMaster()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = "AbsenceRequest", NotificationConfigId = configId });
        var created = await service.GetAll(new GetAllNotificationMastersInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllNotificationMastersInput());
        result.TotalCount.ShouldBe(0);
    }
}
