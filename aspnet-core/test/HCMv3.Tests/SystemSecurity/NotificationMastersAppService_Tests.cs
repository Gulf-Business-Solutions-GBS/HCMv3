using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.HcmNotifications;
using HCMv3.HcmNotifications.Rules;
using HCMv3.SystemSecurity;
using HCMv3.SystemSecurity.Dto;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SystemSecurity;

// ReSharper disable once InconsistentNaming
public class NotificationMastersAppService_Tests : SystemSecurityAppServiceTestBase
{
    private const string LeaveType = NotificationRuleSourceCodes.Leave;

    private readonly INotificationRuleSourceRegistry _registry = Substitute.For<INotificationRuleSourceRegistry>();
    private readonly INotificationCriterionEvaluator _criterionEvaluator = Substitute.For<INotificationCriterionEvaluator>();
    private readonly INotificationRuleSource _leaveSource = Substitute.For<INotificationRuleSource>();

    public NotificationMastersAppService_Tests()
    {
        _leaveSource.Code.Returns(LeaveType);
        _leaveSource.DisplayNameKey.Returns("NotificationRuleSource_Leave");
        _leaveSource.SubTypeDisplayNameKey.Returns("AbsenceType");
        _leaveSource.GetSubTypesAsync().Returns(new List<NotificationRuleSubTypeItem>
        {
            new() { Id = 7, Name = "Annual" }
        });

        _registry.Find(Arg.Any<string>()).Returns(call =>
            string.Equals(call.Arg<string>(), LeaveType, StringComparison.OrdinalIgnoreCase) ? _leaveSource : null);

        _criterionEvaluator.Validate(Arg.Any<INotificationRuleSource>(), Arg.Any<string>()).Returns(Array.Empty<string>());
    }

    private NotificationMastersAppService CreateAppService()
    {
        return CreateService(() => new NotificationMastersAppService(
            FakeRepository.For<NotificationMaster>(Context),
            _registry,
            _criterionEvaluator));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_NotificationMaster()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto
        {
            NotificationType = LeaveType,
            NotificationSubTypeId = 7,
            NotificationConfigId = configId,
            IsEnabled = true
        });

        var result = await service.GetAll(new GetAllNotificationMastersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NotificationType.ShouldBe(LeaveType);
        result.Items[0].IsSupportedType.ShouldBeTrue();
        result.Items[0].NotificationSubTypeId.ShouldBe(7);
        result.Items[0].NotificationSubTypeName.ShouldBe("Annual");
        result.Items[0].IsEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Store_The_Sources_Own_Code_When_Typed_In_Another_Case()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = "leave", NotificationConfigId = configId });

        var result = await service.GetAll(new GetAllNotificationMastersInput());
        result.Items[0].NotificationType.ShouldBe(LeaveType);
    }

    [Fact]
    public async Task Should_Throw_When_The_Rule_Is_An_Exact_Duplicate()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = LeaveType, NotificationConfigId = configId });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = LeaveType, NotificationConfigId = configId }));
    }

    [Fact]
    public async Task Should_Allow_Two_Rules_On_The_Same_Type_With_Different_Criteria()
    {
        // Legacy refused this, because both rules would have shared the record's one NotificationSent flag and
        // the second could never fire. With the per-rule sent log it does fire, so it is allowed.
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = LeaveType, NotificationConfigId = configId, Criterion = "{\"kind\":\"a\"}" });
        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = LeaveType, NotificationConfigId = configId, Criterion = "{\"kind\":\"b\"}" });

        var result = await service.GetAll(new GetAllNotificationMastersInput());
        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Refuse_A_Type_That_Is_Not_A_Supported_Source()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = "AbsenceRequest", NotificationConfigId = configId }));
    }

    [Fact]
    public async Task Should_Refuse_A_Sub_Type_The_Source_Does_Not_Offer()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = LeaveType, NotificationSubTypeId = 999, NotificationConfigId = configId }));
    }

    [Fact]
    public async Task Should_Refuse_A_Criterion_The_Engine_Rejects()
    {
        var configId = CreateNotificationConfig();
        _criterionEvaluator.Validate(_leaveSource, "bad").Returns(new[] { "'Leave.Nope' does not exist." });
        var service = CreateAppService();

        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = LeaveType, NotificationConfigId = configId, Criterion = "bad" }));

        exception.Details.ShouldContain("Leave.Nope");
    }

    [Fact]
    public async Task Should_List_An_Old_Free_Text_Rule_As_Unsupported()
    {
        // A rule saved before the rule engine, with legacy's free-text type, still lists -- flagged, so the
        // administrator can see which ones to re-enter.
        var configId = CreateNotificationConfig();
        Context.NotificationMasters.Add(new NotificationMaster
        {
            TenantId = AbpSession.TenantId ?? 1,
            NotificationType = "AbsenceRequest",
            NotificationConfigId = configId
        });
        Context.SaveChanges();

        var result = await CreateAppService().GetAll(new GetAllNotificationMastersInput());

        result.Items[0].IsSupportedType.ShouldBeFalse();
        result.Items[0].NotificationTypeName.ShouldBe("AbsenceRequest");
    }

    [Fact]
    public async Task Should_Update_NotificationMaster()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = LeaveType, NotificationConfigId = configId });
        var created = await service.GetAll(new GetAllNotificationMastersInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { Id = id, NotificationType = LeaveType, NotificationSubTypeId = 7, NotificationConfigId = configId, IsEnabled = true });

        var edited = await service.GetNotificationMasterForEdit(new EntityDto(id));
        edited.NotificationMaster.IsEnabled.ShouldBeTrue();
        edited.NotificationMaster.NotificationSubTypeId.ShouldBe(7);
    }

    [Fact]
    public async Task Should_Delete_NotificationMaster()
    {
        var configId = CreateNotificationConfig();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditNotificationMasterDto { NotificationType = LeaveType, NotificationConfigId = configId });
        var created = await service.GetAll(new GetAllNotificationMastersInput());
        var id = created.Items[0].Id;

        await service.Delete(new EntityDto(id));

        var result = await service.GetAll(new GetAllNotificationMastersInput());
        result.TotalCount.ShouldBe(0);
    }
}
