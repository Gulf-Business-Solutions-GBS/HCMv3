using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization.Users;
using Abp.UI;
using HCMv3.AiAgent;
using HCMv3.AiAgent.Dto;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.MultiTenancy;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// One tenant can never reach another's data through the assistant -- not its conversations, not its drafts,
/// and not its employees, pay or leave through any tool, even for a user in the other tenant who holds EVERY
/// assistant and pay permission and knows the exact codes and ids.
///
/// <para>
/// Tenant A is the default tenant, holding employee 12001 with basic pay, a pending leave request, a conversation
/// and an unconfirmed pay-change card. Tenant B is a second tenant whose administrator tries every route to them.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class AiAgentTenantIsolation_Tests : AiAgentTestBase
{
    private static readonly string[] EveryAssistantAndPayPermission =
    {
        AppPermissions.Ess, AppPermissions.Ess_AiAgent, AppPermissions.Ess_MyLeave, AppPermissions.Ess_MyTeam,
        AppPermissions.Pages_PersonnelAdministration_Employees,
        AppPermissions.Pages_PayrollManagement_WageTypes,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Create,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPays_Edit,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Create,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Edit,
        AppPermissions.Pages_PersonnelAdministration_EmployeeBasicPayDetails_Delete,
        AppPermissions.Pages_PersonnelAdministration_EmployeePayments,
        AppPermissions.Pages_PersonnelAdministration_EmployeePayments_Create
    };

    private readonly int _tenantA;
    private readonly int _employeeA;
    private readonly int _pendingLeaveA;
    private readonly SendAiMessageOutput _conversationA;

    private int _tenantB;
    private long _adminB;

    public AiAgentTenantIsolation_Tests()
    {
        // ---- tenant A: the default tenant, logged in as its admin by the base class
        _tenantA = TenantId;
        _employeeA = CreateEmployee("12001", AbpSession.UserId);

        var kwd = UsingDbContext(context => context.Set<Currency>().Add(new Currency
        {
            TenantId = _tenantA, Code = "KWD", NameEn = "Kuwaiti Dinar", NameAr = "دينار كويتي", DecimalCount = 3
        }).Entity);

        var carAllowance = UsingDbContext(context => context.Set<WageType>().Add(new WageType
        {
            TenantId = _tenantA, Code = "1029", NameEn = "Car Allowance", NameAr = "بدل سيارة",
            BillingType = BillingType.Earning, IncludeInBasicPay = true, Status = true
        }).Entity);

        UsingDbContext(context => context.Set<WageType>().Add(new WageType
        {
            TenantId = _tenantA, Code = "2014", NameEn = "Traffic Fines Ded", NameAr = "خصم مخالفات مرورية",
            BillingType = BillingType.Deduction, Status = true
        }));

        var basicPay = UsingDbContext(context => context.Set<EmployeeBasicPay>().Add(new EmployeeBasicPay
        {
            TenantId = _tenantA, EmployeeId = _employeeA, StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(9999, 12, 31), Amount = 800, PayType = PayType.Regular, PayInterval = PayInterval.Monthly
        }).Entity);

        UsingDbContext(context => context.Set<EmployeeBasicPayDetail>().Add(new EmployeeBasicPayDetail
        {
            TenantId = _tenantA, EmployeeBasicPayId = basicPay.Id, WageTypeId = carAllowance.Id, Amount = 800, CurrencyId = kwd.Id
        }));

        var annual = CreateAbsenceType("Annual Leave", "إجازة سنوية");
        _pendingLeaveA = CreateLeave(_employeeA, annual, DateTime.Today.AddDays(20), 3, "tenant A secret leave", RequestStatus.Open);

        // Tenant A's admin has a conversation holding a pay figure and an unconfirmed pay-change card.
        Model.CallsTool("prepare_basic_pay_change",
                @"{""employeeCode"":""12001"",""effectiveDate"":""2026-10-01"",""changes"":[{""wageType"":""Car Allowance"",""amount"":50,""change"":""increase_by""}]}")
            .RepliesWith("Tenant A: car allowance 800 -> 850, please confirm.");
        _conversationA = Service.SendMessage(new SendAiMessageInput { Message = "tenant A private question" }).GetAwaiter().GetResult();
        _conversationA.AssistantMessage.PendingAction.ShouldNotBeNull();
    }

    /// <summary>Creates tenant B and an administrator there holding every permission the assistant can use, then logs in as them.</summary>
    private void LogInAsTenantBAdministrator()
    {
        var tenant = UsingDbContext(null, context =>
        {
            var created = context.Tenants.Add(new Tenant("tenantb", "Tenant B") { IsActive = true }).Entity;
            context.SaveChanges();
            return created;
        });
        _tenantB = tenant.Id;

        _adminB = UsingDbContext(_tenantB, context =>
        {
            var user = new User
            {
                TenantId = _tenantB,
                UserName = "adminb",
                Name = "Admin",
                Surname = "B",
                EmailAddress = "adminb@tenantb.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
            };
            user.SetNormalizedNames();
            context.Users.Add(user);
            context.SaveChanges();

            foreach (var permission in EveryAssistantAndPayPermission)
            {
                context.UserPermissions.Add(new UserPermissionSetting { TenantId = _tenantB, UserId = user.Id, Name = permission, IsGranted = true });
            }

            context.SaveChanges();
            return user.Id;
        });

        AbpSession.TenantId = _tenantB;
        AbpSession.UserId = _adminB;
    }

    private string ToolResult(int requestIndex) => Model.Requests[requestIndex].Messages.Last().Content;

    // ------------------------------------------------------------------ conversations and cards

    [Fact]
    public async Task Tenant_B_Should_Not_See_Open_Delete_Or_Continue_Tenant_A_Conversations()
    {
        LogInAsTenantBAdministrator();
        var modelCallsBefore = Model.Requests.Count;

        (await Service.GetMyConversations()).Items.ShouldBeEmpty();

        await Should.ThrowAsync<UserFriendlyException>(() => Service.GetConversation(new EntityDto(_conversationA.ConversationId)));
        await Should.ThrowAsync<UserFriendlyException>(() => Service.DeleteConversation(new EntityDto(_conversationA.ConversationId)));
        await Should.ThrowAsync<UserFriendlyException>(() =>
            Service.SendMessage(new SendAiMessageInput { ConversationId = _conversationA.ConversationId, Message = "what did tenant A ask?" }));

        // Tenant A's conversation was never handed to the model on tenant B's behalf.
        Model.Requests.Count.ShouldBe(modelCallsBefore);
    }

    [Fact]
    public async Task Tenant_B_Should_Not_Confirm_Or_Cancel_Tenant_A_Card()
    {
        LogInAsTenantBAdministrator();
        var card = new EntityDto(_conversationA.AssistantMessage.PendingAction.Id);

        await Should.ThrowAsync<UserFriendlyException>(() => Service.ConfirmAction(card));
        await Should.ThrowAsync<UserFriendlyException>(() => Service.CancelAction(card));

        UsingDbContext(null, context =>
        {
            context.Set<AiPendingAction>().IgnoreQueryFilters().Single(a => a.Id == card.Id).Status.ShouldBe(AiPendingActionStatus.Pending);
            context.Set<EmployeeBasicPay>().IgnoreQueryFilters().Count(b => b.EmployeeId == _employeeA && !b.IsDeleted).ShouldBe(1);
        });
    }

    // ------------------------------------------------------------------ tools

    [Fact]
    public async Task Tenant_B_Pay_Tools_Should_Not_Find_Tenant_A_Employees_Wage_Types_Or_Pay()
    {
        LogInAsTenantBAdministrator();
        var start = Model.Requests.Count;

        Model.CallsTool("find_employee", @"{""query"":""12001""}")
            .CallsTool("get_employee_basic_pay", @"{""employeeCode"":""12001""}")
            .CallsTool("find_wage_types", @"{""usage"":""basic_pay"",""query"":""Car""}")
            .RepliesWith("nothing");
        await Service.SendMessage(new SendAiMessageInput { Message = "show me employee 12001" });

        ToolResult(start + 1).ShouldContain(@"""count"":0");
        ToolResult(start + 1).ShouldNotContain("12001 T");
        ToolResult(start + 2).ShouldContain("employee_not_found");
        ToolResult(start + 2).ShouldNotContain("800");
        ToolResult(start + 3).ShouldContain(@"""count"":0");
        ToolResult(start + 3).ShouldNotContain("Car Allowance");

        var next = Model.Requests.Count;

        Model.CallsTool("prepare_basic_pay_change",
                @"{""employeeCode"":""12001"",""effectiveDate"":""2026-10-01"",""changes"":[{""wageType"":""Car Allowance"",""amount"":50,""change"":""increase_by""}]}")
            .CallsTool("prepare_one_time_payment",
                @"{""employeeCode"":""12001"",""wageType"":""Traffic Fines Ded"",""amount"":50,""paymentDate"":""2026-10-15""}")
            .RepliesWith("nothing");
        var output = await Service.SendMessage(new SendAiMessageInput { Message = "add 50 car allowance and a traffic fine for 12001" });

        ToolResult(next + 1).ShouldContain("No employee has the code '12001'");
        ToolResult(next + 2).ShouldContain("No employee has the code '12001'");
        output.AssistantMessage.PendingAction.ShouldBeNull();

        // Nothing was drafted against, or written to, tenant A.
        UsingDbContext(null, context =>
        {
            context.Set<AiPendingAction>().IgnoreQueryFilters().Count(a => a.TenantId == _tenantB).ShouldBe(0);
            context.Set<EmployeePayment>().IgnoreQueryFilters().Count(p => p.EmployeeId == _employeeA).ShouldBe(0);
        });
    }

    [Fact]
    public async Task Tenant_B_Should_Not_Reach_Tenant_A_Leave_Even_By_Its_Exact_Id()
    {
        LogInAsTenantBAdministrator();
        CreateEmployee("B001", _adminB); // tenant B's admin is an employee of tenant B
        var start = Model.Requests.Count;

        Model.CallsTool("get_my_leave_requests", @"{""includeHistory"":true}")
            .CallsTool("prepare_leave_cancellation", $@"{{""leaveRequestId"":{_pendingLeaveA}}}")
            .RepliesWith("nothing");
        var output = await Service.SendMessage(new SendAiMessageInput { Message = "cancel leave " + _pendingLeaveA });

        ToolResult(start + 1).ShouldNotContain("tenant A secret leave");
        ToolResult(start + 2).ShouldContain("not one of the user's leave requests");
        output.AssistantMessage.PendingAction.ShouldBeNull();

        UsingDbContext(null, context =>
            context.Set<EmployeeAbsence>().IgnoreQueryFilters().Single(a => a.Id == _pendingLeaveA).IsDeleted.ShouldBeFalse());
    }

    [Fact]
    public async Task Everything_Tenant_B_Creates_Should_Belong_To_Tenant_B()
    {
        LogInAsTenantBAdministrator();
        Model.RepliesWith("hello from tenant B");

        var output = await Service.SendMessage(new SendAiMessageInput { Message = "hello" });

        UsingDbContext(null, context =>
        {
            context.Set<AiConversation>().IgnoreQueryFilters().Single(c => c.Id == output.ConversationId).TenantId.ShouldBe(_tenantB);
            context.Set<AiMessage>().IgnoreQueryFilters().Where(m => m.ConversationId == output.ConversationId)
                .ToList().ShouldAllBe(m => m.TenantId == _tenantB);
        });

        // And tenant A's admin, back in tenant A, does not see it.
        AbpSession.TenantId = _tenantA;
        AbpSession.UserId = GetUserByUserName("admin").Id;
        (await Service.GetMyConversations()).Items.Select(c => c.Id).ShouldNotContain(output.ConversationId);
    }
}
