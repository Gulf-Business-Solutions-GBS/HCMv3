using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// The two step fields restored for the absence-violation job: <c>HoldTillPunchFound</c> and the step's
/// <c>NotificationConfig</c>. They must survive create, edit, the edit form and the list.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AbsenceViolationRuleDetailsAppService_NotificationFields_Tests : PayrollManagementAppServiceTestBase
{
    private AbsenceViolationRuleDetailsAppService CreateAppService()
    {
        return CreateService(() => new AbsenceViolationRuleDetailsAppService(FakeRepository.For<AbsenceViolationRuleDetail>(Context)));
    }

    private int CreateRule()
    {
        var rule = new AbsenceViolationRule
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "AVRN",
            NameEn = "AVRN",
            NameAr = "AVRN"
        };
        Context.AbsenceViolationRules.Add(rule);
        Context.SaveChanges();
        return rule.Id;
    }

    private int CreateNotificationConfig(string name)
    {
        var config = new NotificationConfig
        {
            TenantId = AbpSession.TenantId ?? 1,
            Name = name
        };
        Context.Set<NotificationConfig>().Add(config);
        Context.SaveChanges();
        return config.Id;
    }

    [Fact]
    public async Task Should_Save_And_List_HoldTillPunchFound_And_NotificationConfig()
    {
        var ruleId = CreateRule();
        var configId = CreateNotificationConfig("Tell the manager");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleDetailDto
        {
            AbsenceViolationRuleId = ruleId,
            Step = 1,
            DayInterval = 2,
            Action = AbsenceViolationAction.Notification,
            HoldTillPunchFound = true,
            NotificationConfigId = configId
        });

        var item = (await service.GetAll(new GetAllAbsenceViolationRuleDetailsInput())).Items.Single();
        item.HoldTillPunchFound.ShouldBeTrue();
        item.NotificationConfigId.ShouldBe(configId);
        item.NotificationConfigName.ShouldBe("Tell the manager");

        var forEdit = await service.GetAbsenceViolationRuleDetailForEdit(new EntityDto(item.Id));
        forEdit.AbsenceViolationRuleDetail.HoldTillPunchFound.ShouldBeTrue();
        forEdit.AbsenceViolationRuleDetail.NotificationConfigId.ShouldBe(configId);
    }

    [Fact]
    public async Task Should_Clear_HoldTillPunchFound_And_NotificationConfig_On_Edit()
    {
        var ruleId = CreateRule();
        var configId = CreateNotificationConfig("Tell HR");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleDetailDto
        {
            AbsenceViolationRuleId = ruleId,
            Step = 1,
            HoldTillPunchFound = true,
            NotificationConfigId = configId
        });

        var id = (await service.GetAll(new GetAllAbsenceViolationRuleDetailsInput())).Items.Single().Id;

        await service.CreateOrEdit(new CreateOrEditAbsenceViolationRuleDetailDto
        {
            Id = id,
            AbsenceViolationRuleId = ruleId,
            Step = 1,
            HoldTillPunchFound = false,
            NotificationConfigId = null
        });

        var forEdit = await service.GetAbsenceViolationRuleDetailForEdit(new EntityDto(id));
        forEdit.AbsenceViolationRuleDetail.HoldTillPunchFound.ShouldBeFalse();
        forEdit.AbsenceViolationRuleDetail.NotificationConfigId.ShouldBeNull();
    }
}
