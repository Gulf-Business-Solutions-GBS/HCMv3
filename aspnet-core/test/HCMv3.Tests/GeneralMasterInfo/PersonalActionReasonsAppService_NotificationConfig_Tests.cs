using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>Legacy <c>PersonalActionReason.NotificationConfig</c> is stored and returned.</summary>
// ReSharper disable once InconsistentNaming
public class PersonalActionReasonsAppService_NotificationConfig_Tests : AppTestBase
{
    private readonly IPersonalActionReasonsAppService _service;

    public PersonalActionReasonsAppService_NotificationConfig_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _service = Resolve<IPersonalActionReasonsAppService>();
    }

    private int CreateNotificationConfig(string name)
    {
        return UsingDbContext(context =>
        {
            var config = context.Set<NotificationConfig>().Add(new NotificationConfig
            {
                TenantId = AbpSession.TenantId ?? 1,
                Name = name
            }).Entity;
            context.SaveChanges();
            return config.Id;
        });
    }

    [Fact]
    public async Task Should_Store_And_Return_The_Notification_Configuration()
    {
        var configId = CreateNotificationConfig("Promotion letter");

        await _service.CreateOrEdit(new CreateOrEditPersonalActionReasonDto
        {
            NameEn = "Annual review",
            NameAr = "مراجعة سنوية",
            NotificationConfigId = configId
        });

        var item = (await _service.GetAll(new GetAllPersonalActionReasonsInput())).Items.Single(r => r.NameEn == "Annual review");
        item.NotificationConfigId.ShouldBe(configId);
        item.NotificationConfigName.ShouldBe("Promotion letter");

        var forEdit = await _service.GetPersonalActionReasonForEdit(new EntityDto(item.Id));
        forEdit.PersonalActionReason.NotificationConfigId.ShouldBe(configId);
    }

    [Fact]
    public async Task Should_Clear_The_Notification_Configuration_On_Update()
    {
        var configId = CreateNotificationConfig("Promotion letter");
        await _service.CreateOrEdit(new CreateOrEditPersonalActionReasonDto
        {
            NameEn = "Annual review",
            NameAr = "مراجعة سنوية",
            NotificationConfigId = configId
        });
        var item = (await _service.GetAll(new GetAllPersonalActionReasonsInput())).Items.Single(r => r.NameEn == "Annual review");

        await _service.CreateOrEdit(new CreateOrEditPersonalActionReasonDto
        {
            Id = item.Id,
            Code = item.Code,
            NameEn = "Annual review",
            NameAr = "مراجعة سنوية",
            NotificationConfigId = null
        });

        UsingDbContext(context => context.Set<PersonalActionReason>().Single(r => r.Id == item.Id).NotificationConfigId)
            .ShouldBeNull();
    }
}
