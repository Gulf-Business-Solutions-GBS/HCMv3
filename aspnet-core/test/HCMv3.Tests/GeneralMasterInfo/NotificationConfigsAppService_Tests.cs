using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GeneralMasterInfo;

/// <summary>
/// The notification configuration screen: its notification groups are edited inside it, as legacy did, and
/// legacy's rule that Send To All stands alone is enforced.
/// </summary>
// ReSharper disable once InconsistentNaming
public class NotificationConfigsAppService_Tests : AppTestBase
{
    private readonly INotificationConfigsAppService _service;

    public NotificationConfigsAppService_Tests()
    {
        _service = Resolve<INotificationConfigsAppService>();
    }

    private int CreateRole()
    {
        return UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = AbpSession.TenantId!.Value,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "HR " + Guid.NewGuid().ToString("N").Substring(0, 4),
                NameAr = "HR"
            }).Entity;
            context.SaveChanges();
            return role.Id;
        });
    }

    private int IdOf(string name) =>
        UsingDbContext(context => context.Set<NotificationConfig>().Single(c => c.Name == name).Id);

    [Fact]
    public async Task Should_Save_And_Reload_Groups_With_The_Configuration()
    {
        var roleId = CreateRole();

        await _service.CreateOrEdit(new CreateOrEditNotificationConfigDto
        {
            Name = "Notify HR",
            SendNotification = true,
            Groups =
            {
                new NotificationConfigGroupDto { WorkflowRoleId = roleId, WorkflowRoleGroup = WorkflowRoleGroup.Department }
            }
        });

        var loaded = (await _service.GetNotificationConfigForEdit(new EntityDto(IdOf("Notify HR")))).NotificationConfig;

        var group = loaded.Groups.ShouldHaveSingleItem();
        group.WorkflowRoleId.ShouldBe(roleId);
        group.WorkflowRoleGroup.ShouldBe(WorkflowRoleGroup.Department);
    }

    [Fact]
    public async Task Saving_Should_Replace_The_Groups_The_Configuration_Had()
    {
        var firstRole = CreateRole();
        var secondRole = CreateRole();

        await _service.CreateOrEdit(new CreateOrEditNotificationConfigDto
        {
            Name = "Notify Payroll",
            Groups = { new NotificationConfigGroupDto { WorkflowRoleId = firstRole } }
        });

        var edit = (await _service.GetNotificationConfigForEdit(new EntityDto(IdOf("Notify Payroll")))).NotificationConfig;
        edit.Groups.Clear();
        edit.Groups.Add(new NotificationConfigGroupDto { WorkflowRoleId = secondRole });
        await _service.CreateOrEdit(edit);

        var reloaded = (await _service.GetNotificationConfigForEdit(new EntityDto(IdOf("Notify Payroll")))).NotificationConfig;
        reloaded.Groups.ShouldHaveSingleItem().WorkflowRoleId.ShouldBe(secondRole);
    }

    [Fact]
    public async Task Should_Refuse_Send_To_All_With_Employee_Ticked()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateOrEdit(new CreateOrEditNotificationConfigDto
        {
            Name = "Everyone and employee",
            SendToAll = true,
            ToEmployee = true
        }));
    }

    [Fact]
    public async Task Should_Refuse_Send_To_All_With_A_Group()
    {
        var roleId = CreateRole();

        await Should.ThrowAsync<UserFriendlyException>(() => _service.CreateOrEdit(new CreateOrEditNotificationConfigDto
        {
            Name = "Everyone and HR",
            SendToAll = true,
            Groups = { new NotificationConfigGroupDto { WorkflowRoleId = roleId } }
        }));
    }

    [Fact]
    public async Task Should_Allow_Send_To_All_On_Its_Own()
    {
        await _service.CreateOrEdit(new CreateOrEditNotificationConfigDto { Name = "Everyone", SendToAll = true });

        IdOf("Everyone").ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Refuse_A_Blank_Name()
    {
        // Refused by the required-field check on the input before the service's own rule is reached.
        await Should.ThrowAsync<Abp.Runtime.Validation.AbpValidationException>(() =>
            _service.CreateOrEdit(new CreateOrEditNotificationConfigDto { Name = "   " }));
    }
}
