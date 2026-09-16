using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Runtime.Validation;
using Abp.UI;
using Castle.MicroKernel.Registration;
using HCMv3.AiAgent;
using HCMv3.AiAgent.Chat;
using HCMv3.AiAgent.Dto;
using HCMv3.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// The conversation loop against a scripted model and the REAL self-service services underneath, so what is
/// tested is what the model is actually shown -- including that the permission and employee checks of the
/// services it stands in for still apply when it is the one asking.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AiAgentAppService_Tests : AppTestBase
{
    private readonly FakeAiChatClient _model = new();

    public AiAgentAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        LocalIocManager.IocContainer.Register(Component.For<IAiChatClient>().Instance(_model).IsDefault());
    }

    private IAiAgentAppService Service => Resolve<IAiAgentAppService>();

    private int TenantId => AbpSession.TenantId ?? 1;

    // ------------------------------------------------------------------ conversations

    [Fact]
    public async Task Should_Save_The_Question_And_The_Reply_In_A_New_Conversation()
    {
        _model.RepliesWith("Hello! How can I help?");

        var output = await Service.SendMessage(new SendAiMessageInput { Message = "  Hi there  " });

        output.ConversationId.ShouldBeGreaterThan(0);
        output.ConversationTitle.ShouldBe("Hi there");
        output.UserMessage.Content.ShouldBe("Hi there");
        output.AssistantMessage.Content.ShouldBe("Hello! How can I help?");
        output.AssistantMessage.Role.ShouldBe(AiMessageRole.Assistant);

        var conversations = await Service.GetMyConversations();
        conversations.Items.Count.ShouldBe(1);

        var conversation = await Service.GetConversation(new EntityDto(output.ConversationId));
        conversation.Messages.Select(m => m.Role).ShouldBe(new[] { AiMessageRole.User, AiMessageRole.Assistant });
    }

    [Fact]
    public async Task Should_Replay_The_Earlier_Conversation_To_The_Model()
    {
        _model.RepliesWith("First answer").RepliesWith("Second answer");

        var first = await Service.SendMessage(new SendAiMessageInput { Message = "First question" });
        await Service.SendMessage(new SendAiMessageInput { ConversationId = first.ConversationId, Message = "Second question" });

        var second = _model.Requests[1].Messages;
        second.Select(m => m.Role).ShouldBe(new[]
        {
            AiChatMessage.SystemRole, AiChatMessage.UserRole, AiChatMessage.AssistantRole, AiChatMessage.UserRole
        });
        second[1].Content.ShouldBe("First question");
        second[2].Content.ShouldBe("First answer");
        second[3].Content.ShouldBe("Second question");
    }

    [Fact]
    public async Task Should_Delete_A_Conversation_From_The_List()
    {
        _model.RepliesWith("ok");
        var output = await Service.SendMessage(new SendAiMessageInput { Message = "hello" });

        await Service.DeleteConversation(new EntityDto(output.ConversationId));

        (await Service.GetMyConversations()).Items.ShouldBeEmpty();
        await Should.ThrowAsync<UserFriendlyException>(() => Service.GetConversation(new EntityDto(output.ConversationId)));
    }

    [Fact]
    public async Task Should_Never_Show_A_Conversation_To_Another_User()
    {
        _model.RepliesWith("private answer");
        var output = await Service.SendMessage(new SendAiMessageInput { Message = "my private question" });

        AbpSession.UserId = CreateUser(AppPermissions.Ess, AppPermissions.Ess_AiAgent);

        (await Service.GetMyConversations()).Items.ShouldBeEmpty();
        await Should.ThrowAsync<UserFriendlyException>(() => Service.GetConversation(new EntityDto(output.ConversationId)));
        await Should.ThrowAsync<UserFriendlyException>(() => Service.DeleteConversation(new EntityDto(output.ConversationId)));

        // Nor may they write into it -- and the model must not be called at all.
        await Should.ThrowAsync<UserFriendlyException>(() =>
            Service.SendMessage(new SendAiMessageInput { ConversationId = output.ConversationId, Message = "what did they ask?" }));
        _model.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Refuse_A_User_Without_The_Assistant_Permission()
    {
        AbpSession.UserId = CreateUser(AppPermissions.Ess, AppPermissions.Ess_MyLeave);

        await Should.ThrowAsync<AbpAuthorizationException>(() => Service.SendMessage(new SendAiMessageInput { Message = "hi" }));
        await Should.ThrowAsync<AbpAuthorizationException>(() => Service.GetMyConversations());
        await Should.ThrowAsync<AbpAuthorizationException>(() => Service.GetStartupInfo());
    }

    [Fact]
    public async Task Should_Refuse_An_Empty_Message_Without_Calling_The_Model()
    {
        // Refused either by input validation ([Required] rejects whitespace) or by the service's own check --
        // which one fires first is ABP's business. What matters is that it is refused before the model.
        Exception refusal = null;
        try
        {
            await Service.SendMessage(new SendAiMessageInput { Message = "   " });
        }
        catch (Exception ex)
        {
            refusal = ex;
        }

        (refusal is AbpValidationException || refusal is UserFriendlyException).ShouldBeTrue(refusal?.GetType().Name ?? "nothing thrown");
        _model.Requests.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ tools

    [Fact]
    public async Task Should_Look_Up_The_Balance_And_Give_The_Result_To_The_Model()
    {
        var employeeId = CreateEmployee("AI001", AbpSession.UserId);
        var annual = CreateAbsenceType("Annual Leave", "إجازة سنوية");
        CreateQuota(employeeId, annual, accrued: 30, used: 5);

        _model.CallsTool("get_my_leave_balances").RepliesWith("You have 25 days of annual leave left.");

        var output = await Service.SendMessage(new SendAiMessageInput { Message = "How much leave do I have?" });

        output.AssistantMessage.Content.ShouldBe("You have 25 days of annual leave left.");

        _model.Requests.Count.ShouldBe(2);
        var toolResult = _model.Requests[1].Messages.Last();
        toolResult.Role.ShouldBe(AiChatMessage.ToolRole);
        toolResult.ToolName.ShouldBe("get_my_leave_balances");
        toolResult.Content.ShouldContain(@"""remaining"":25");
        toolResult.Content.ShouldContain("إجازة سنوية"); // unescaped, so the model can read it

        // The tool steps are kept for the audit trail but never shown in the conversation.
        var conversation = await Service.GetConversation(new EntityDto(output.ConversationId));
        conversation.Messages.Count.ShouldBe(2);

        UsingDbContext(context =>
        {
            var stored = context.Set<AiMessage>().Where(m => m.ConversationId == output.ConversationId).ToList();
            stored.Count.ShouldBe(4);
            stored.Single(m => m.Role == AiMessageRole.Tool).Content.ShouldContain(@"""bookable"":25");
            stored.Single(m => m.ToolCallsJson != null).ToolCallsJson.ShouldContain("get_my_leave_balances");
        });
    }

    [Fact]
    public async Task Should_Only_Show_The_Callers_Own_Leave_Requests()
    {
        var mine = CreateEmployee("AI002", AbpSession.UserId);
        var someoneElse = CreateEmployee("AI003");
        var annual = CreateAbsenceType("Annual Leave", "إجازة سنوية");
        CreateLeave(mine, annual, DateTime.Today.AddDays(10), 3, "my leave");
        CreateLeave(someoneElse, annual, DateTime.Today.AddDays(10), 5, "their leave");

        _model.CallsTool("get_my_leave_requests").RepliesWith("You have one upcoming leave.");

        await Service.SendMessage(new SendAiMessageInput { Message = "Show my leave requests" });

        var toolResult = _model.Requests[1].Messages.Last().Content;
        toolResult.ShouldContain("my leave");
        toolResult.ShouldNotContain("their leave");
        toolResult.ShouldContain(@"""status"":""Approved""");
    }

    [Fact]
    public async Task Should_Turn_A_Refusal_Into_An_Error_The_Model_Can_Explain()
    {
        // The admin is deliberately not linked to an employee, so the leave service refuses. That must reach
        // the model as data it can explain -- not end the conversation with an exception.
        _model.CallsTool("get_my_leave_balances").RepliesWith("Your login is not linked to an employee record.");

        var output = await Service.SendMessage(new SendAiMessageInput { Message = "my balance?" });

        output.AssistantMessage.Content.ShouldBe("Your login is not linked to an employee record.");
        _model.Requests[1].Messages.Last().Content.ShouldStartWith(@"{""error"":");
    }

    [Fact]
    public async Task Should_Not_Offer_Leave_Tools_To_A_User_Without_Leave_Access()
    {
        AbpSession.UserId = CreateUser(AppPermissions.Ess, AppPermissions.Ess_AiAgent);
        _model.RepliesWith("I can't see leave information.");

        await Service.SendMessage(new SendAiMessageInput { Message = "how much leave do I have" });

        _model.Requests[0].Tools.ShouldBeEmpty();
        _model.Requests[0].Messages[0].Content.ShouldContain("does not have access to leave information");
    }

    [Fact]
    public async Task Should_Offer_The_Leave_Tools_To_A_User_With_Leave_Access()
    {
        _model.RepliesWith("hi");

        await Service.SendMessage(new SendAiMessageInput { Message = "hi" });

        _model.Requests[0].Tools.Select(t => t.Name)
            .ShouldBe(new[] { "get_my_leave_balances", "get_my_leave_requests", "list_leave_types" }, ignoreOrder: true);
    }

    [Fact]
    public async Task Should_Stop_A_Model_That_Keeps_Calling_Tools()
    {
        for (var i = 0; i < AiAgentConsts.MaxModelCallsPerMessage; i++)
        {
            _model.CallsTool("list_leave_types");
        }

        var output = await Service.SendMessage(new SendAiMessageInput { Message = "loop forever" });

        _model.Requests.Count.ShouldBe(AiAgentConsts.MaxModelCallsPerMessage);
        _model.Requests.First().Tools.ShouldNotBeEmpty();
        // On the last call nothing is offered, so the model has to answer.
        _model.Requests.Last().Tools.ShouldBeEmpty();
        output.AssistantMessage.Content.ShouldNotBeNullOrWhiteSpace();
    }

    // ------------------------------------------------------------------ model failures

    [Fact]
    public async Task Should_Keep_The_Question_When_The_Model_Is_Down()
    {
        _model.Fails();

        await Should.ThrowAsync<UserFriendlyException>(() => Service.SendMessage(new SendAiMessageInput { Message = "anyone there?" }));

        var conversation = (await Service.GetMyConversations()).Items.ShouldHaveSingleItem();
        var messages = (await Service.GetConversation(new EntityDto(conversation.Id))).Messages;
        messages.ShouldHaveSingleItem().Content.ShouldBe("anyone there?");
    }

    [Fact]
    public async Task Should_Tell_The_User_When_The_Model_Timed_Out()
    {
        _model.Fails(isTimeout: true);

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => Service.SendMessage(new SendAiMessageInput { Message = "slow?" }));

        ex.Message.ShouldContain("too long");
    }

    // ------------------------------------------------------------------ startup

    [Fact]
    public async Task Should_Say_When_The_Login_Is_Not_Linked_To_An_Employee()
    {
        var info = await Service.GetStartupInfo();

        info.IsLinkedToEmployee.ShouldBeFalse();
        info.CanUseLeave.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Greet_A_Linked_Employee_By_Name()
    {
        CreateEmployee("AI004", AbpSession.UserId);

        var info = await Service.GetStartupInfo();

        info.IsLinkedToEmployee.ShouldBeTrue();
        info.FirstNameEn.ShouldBe("AI004");
    }

    [Theory]
    [InlineData("How much   annual\nleave do I have?", "How much annual leave do I have?")]
    [InlineData("I would like to apply for ten days of annual leave starting on the first of October please",
        "I would like to apply for ten days of annual leave starting…")]
    public void Should_Make_A_Readable_Title_From_The_First_Message(string message, string expected)
    {
        AiAgentAppService.MakeTitle(message).ShouldBe(expected);
    }

    // ------------------------------------------------------------------ data helpers

    private long CreateUser(params string[] grantedPermissions)
    {
        var userId = 0L;

        UsingDbContext(context =>
        {
            var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = new User
            {
                TenantId = TenantId,
                UserName = "ai" + unique,
                Name = "AI",
                Surname = "Tester",
                EmailAddress = "ai" + unique + "@testdomain.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
            };
            user.SetNormalizedNames();

            userId = context.Users.Add(user).Entity.Id;
            context.SaveChanges();
            userId = user.Id;

            foreach (var permission in grantedPermissions)
            {
                context.UserPermissions.Add(new UserPermissionSetting
                {
                    TenantId = TenantId,
                    UserId = userId,
                    Name = permission,
                    IsGranted = true
                });
            }
        });

        return userId;
    }

    private int CreateEmployee(string code, long? userId = null)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateAbsenceType(string nameEn, string nameAr)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameAr
            }).Entity;
        });

        return absenceType.Id;
    }

    private void CreateQuota(int employeeId, int absenceTypeId, double accrued, double used)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = DateTime.Today.AddYears(-1),
                EntitlementTo = DateTime.Today.AddYears(1),
                DeductionFrom = DateTime.Today.AddYears(-1),
                DeductionTo = DateTime.Today.AddYears(1),
                QuotaAccrued = accrued,
                QuotaUsed = used
            });
        });
    }

    private void CreateLeave(int employeeId, int absenceTypeId, DateTime start, int days, string comments)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                StartDate = start,
                EndDate = start.AddDays(days - 1),
                InitialEndDate = start.AddDays(days - 1),
                CalendarDays = days,
                TotalLeaveDays = days,
                Comments = comments
            });
        });
    }
}
