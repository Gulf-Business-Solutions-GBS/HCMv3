using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.AiAgent;
using HCMv3.AiAgent.Actions;
using HCMv3.AiAgent.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// The assistant's only write path: it may DRAFT, and only the user's Confirm submits. The central assertion,
/// repeated in several forms below, is that no leave row exists until Confirm is pressed.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AiAgentPendingActions_Tests : AiAgentTestBase
{
    private int _employeeId;
    private int _annualLeaveId;

    private void GivenAnEmployeeWithAnnualLeave(double accrued = 30)
    {
        _employeeId = CreateEmployee("AIP01", AbpSession.UserId);
        _annualLeaveId = CreateAbsenceType("Annual Leave", "إجازة سنوية");
        CreateQuota(_employeeId, _annualLeaveId, accrued);
    }

    private static string Date(int daysFromToday) => DateTime.Today.AddDays(daysFromToday).ToString("yyyy-MM-dd");

    private async Task<SendAiMessageOutput> AskForLeave(string argumentsJson, string message = "I want leave")
    {
        Model.CallsTool("prepare_leave_request", argumentsJson).RepliesWith("Please confirm below.");
        return await Service.SendMessage(new SendAiMessageInput { Message = message });
    }

    /// <summary>
    /// Every live leave row, pending included. Filters bypassed on purpose: the ApprovedOnly filter hides pending
    /// rows from ordinary queries, which would make "nothing was submitted" pass even when something was.
    /// </summary>
    private int LeaveRowCount() =>
        UsingDbContext(context => context.Set<EmployeeAbsence>().IgnoreQueryFilters()
            .Count(a => a.EmployeeId == _employeeId && !a.IsDeleted));

    private string LastToolResult() => Model.Requests.Last().Messages.Last().Content;

    // ------------------------------------------------------------------ asking for what is missing

    [Fact]
    public async Task Should_Ask_Which_Leave_Type_When_The_User_Did_Not_Say()
    {
        GivenAnEmployeeWithAnnualLeave();

        var output = await AskForLeave($@"{{""startDate"":""{Date(30)}"",""days"":10}}", "I want to create leave for 10 days");

        var result = LastToolResult();
        result.ShouldContain("needs_more_information");
        result.ShouldContain("which type of leave");
        result.ShouldContain("إجازة سنوية"); // the choices are offered

        output.AssistantMessage.PendingAction.ShouldBeNull();
        UsingDbContext(context => context.Set<AiPendingAction>().Count().ShouldBe(0));
    }

    [Fact]
    public async Task Should_Ask_For_The_Dates_When_Only_The_Type_Is_Known()
    {
        GivenAnEmployeeWithAnnualLeave();

        await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId}}}");

        var result = LastToolResult();
        result.ShouldContain("the first day of leave");
        result.ShouldContain("the last day of leave or how many days");
    }

    [Fact]
    public async Task Should_Ask_When_The_Last_Day_And_The_Number_Of_Days_Disagree()
    {
        GivenAnEmployeeWithAnnualLeave();

        await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""endDate"":""{Date(31)}"",""days"":10}}");

        LastToolResult().ShouldContain("Ask the user which is right");
        UsingDbContext(context => context.Set<AiPendingAction>().Count().ShouldBe(0));
    }

    // ------------------------------------------------------------------ drafting

    [Fact]
    public async Task Should_Draft_The_Request_Without_Submitting_It()
    {
        GivenAnEmployeeWithAnnualLeave();

        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");

        LastToolResult().ShouldContain("draft_ready");
        LastToolResult().ShouldContain($@"""endDate"":""{Date(39)}""");
        LastToolResult().ShouldContain(@"""bookableAfterThis"":20");

        var card = output.AssistantMessage.PendingAction;
        card.ShouldNotBeNull();
        card.Status.ShouldBe(AiPendingActionStatus.Pending);
        card.ActionType.ShouldBe(ApplyForLeaveAiPendingActionHandler.Type);
        card.Title.ShouldBe("Apply for leave");
        card.Details.Single(d => d.Label == "Days").Value.ShouldBe("10");
        card.Details.Single(d => d.Label == "Leave type").Value.ShouldBe("Annual Leave");

        // THE point: nothing has been applied for.
        LeaveRowCount().ShouldBe(0);

        // And the card comes back with the conversation.
        var conversation = await Service.GetConversation(new EntityDto(output.ConversationId));
        conversation.Messages.Last().PendingAction.Id.ShouldBe(card.Id);
    }

    [Fact]
    public async Task Should_Not_Draft_More_Days_Than_Are_Bookable()
    {
        GivenAnEmployeeWithAnnualLeave(accrued: 5);

        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");

        LastToolResult().ShouldContain("not_possible");
        LastToolResult().ShouldContain("at most 5 day(s)");
        output.AssistantMessage.PendingAction.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Draft_Leave_That_Overlaps_A_Pending_Request()
    {
        GivenAnEmployeeWithAnnualLeave();
        CreateLeave(_employeeId, _annualLeaveId, DateTime.Today.AddDays(35), 3, status: RequestStatus.Open);

        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");

        LastToolResult().ShouldContain("overlaps");
        LastToolResult().ShouldContain("Pending approval");
        output.AssistantMessage.PendingAction.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Allow_Leave_Over_A_Rejected_Request()
    {
        GivenAnEmployeeWithAnnualLeave();
        CreateLeave(_employeeId, _annualLeaveId, DateTime.Today.AddDays(35), 3, status: RequestStatus.Rejected);

        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");

        output.AssistantMessage.PendingAction.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_Newer_Draft_Should_Replace_The_Older_One()
    {
        GivenAnEmployeeWithAnnualLeave();

        var first = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");

        Model.CallsTool("prepare_leave_request", $@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":5}}")
            .RepliesWith("Updated, please confirm.");
        var second = await Service.SendMessage(new SendAiMessageInput { ConversationId = first.ConversationId, Message = "make it 5 days" });

        UsingDbContext(context =>
        {
            context.Set<AiPendingAction>().Single(a => a.Id == first.AssistantMessage.PendingAction.Id).Status.ShouldBe(AiPendingActionStatus.Expired);
            context.Set<AiPendingAction>().Single(a => a.Id == second.AssistantMessage.PendingAction.Id).Status.ShouldBe(AiPendingActionStatus.Pending);
        });

        // The abandoned 10-day card can no longer submit.
        await Should.ThrowAsync<UserFriendlyException>(() => Service.ConfirmAction(new EntityDto(first.AssistantMessage.PendingAction.Id)));
        LeaveRowCount().ShouldBe(0);
    }

    // ------------------------------------------------------------------ confirming

    [Fact]
    public async Task Should_Apply_For_The_Leave_Only_When_The_User_Confirms()
    {
        GivenAnEmployeeWithAnnualLeave();
        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10,""comments"":""Family trip""}}");

        var result = await Service.ConfirmAction(new EntityDto(output.AssistantMessage.PendingAction.Id));

        result.Action.Status.ShouldBe(AiPendingActionStatus.Completed);
        result.Message.Role.ShouldBe(AiMessageRole.Assistant);
        result.Message.Content.ShouldStartWith("Done.");

        UsingDbContext(context =>
        {
            var leave = context.Set<EmployeeAbsence>().Single(a => a.EmployeeId == _employeeId);
            leave.AbsenceTypeId.ShouldBe(_annualLeaveId);
            leave.StartDate.ShouldBe(DateTime.Today.AddDays(30));
            leave.EndDate.ShouldBe(DateTime.Today.AddDays(39));
            leave.TotalLeaveDays.ShouldBe(10);
            leave.Comments.ShouldBe("Family trip");
        });

        // The outcome is part of the conversation, so a follow-up question is answered knowing it.
        var conversation = await Service.GetConversation(new EntityDto(output.ConversationId));
        conversation.Messages.Last().Content.ShouldBe(result.Message.Content);
    }

    [Fact]
    public async Task Confirming_Twice_Should_Submit_Once()
    {
        GivenAnEmployeeWithAnnualLeave();
        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");
        var id = new EntityDto(output.AssistantMessage.PendingAction.Id);

        await Service.ConfirmAction(id);
        await Should.ThrowAsync<UserFriendlyException>(() => Service.ConfirmAction(id));

        LeaveRowCount().ShouldBe(1);
    }

    [Fact]
    public async Task Another_User_Should_Not_Be_Able_To_Confirm_Or_Cancel_It()
    {
        GivenAnEmployeeWithAnnualLeave();
        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");
        var id = new EntityDto(output.AssistantMessage.PendingAction.Id);

        AbpSession.UserId = CreateUser(HCMv3.Authorization.AppPermissions.Ess, HCMv3.Authorization.AppPermissions.Ess_AiAgent,
            HCMv3.Authorization.AppPermissions.Ess_MyLeave);

        await Should.ThrowAsync<UserFriendlyException>(() => Service.ConfirmAction(id));
        await Should.ThrowAsync<UserFriendlyException>(() => Service.CancelAction(id));

        LeaveRowCount().ShouldBe(0);
        UsingDbContext(context => context.Set<AiPendingAction>().Single(a => a.Id == id.Id).Status.ShouldBe(AiPendingActionStatus.Pending));
    }

    [Fact]
    public async Task A_Refusal_On_Confirm_Should_Be_Recorded_On_The_Card_Not_Thrown()
    {
        GivenAnEmployeeWithAnnualLeave();
        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");

        // Between drafting and confirming, the leave type is removed -- the leave service must refuse, and the
        // card must end Failed rather than stuck in Processing.
        UsingDbContext(context =>
        {
            context.Set<AbsenceType>().Single(t => t.Id == _annualLeaveId).IsDeleted = true;
        });

        var result = await Service.ConfirmAction(new EntityDto(output.AssistantMessage.PendingAction.Id));

        result.Action.Status.ShouldBe(AiPendingActionStatus.Failed);
        result.Action.ResultMessage.ShouldStartWith("It could not be submitted:");
        LeaveRowCount().ShouldBe(0);
    }

    [Fact]
    public async Task A_Draft_Left_Too_Long_Should_Expire_Instead_Of_Submitting()
    {
        GivenAnEmployeeWithAnnualLeave();
        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");
        var id = output.AssistantMessage.PendingAction.Id;

        UsingDbContext(context =>
        {
            context.Set<AiPendingAction>().Single(a => a.Id == id).CreationTime =
                DateTime.Now.AddHours(-AiAgentConsts.PendingActionLifetimeHours - 1);
        });

        var result = await Service.ConfirmAction(new EntityDto(id));

        result.Action.Status.ShouldBe(AiPendingActionStatus.Expired);
        LeaveRowCount().ShouldBe(0);
    }

    [Fact]
    public async Task Cancelling_The_Card_Should_Submit_Nothing()
    {
        GivenAnEmployeeWithAnnualLeave();
        var output = await AskForLeave($@"{{""leaveTypeId"":{_annualLeaveId},""startDate"":""{Date(30)}"",""days"":10}}");
        var id = new EntityDto(output.AssistantMessage.PendingAction.Id);

        var result = await Service.CancelAction(id);

        result.Action.Status.ShouldBe(AiPendingActionStatus.Cancelled);
        result.Message.Content.ShouldBe("Cancelled. Nothing was submitted.");
        await Should.ThrowAsync<UserFriendlyException>(() => Service.ConfirmAction(id));
        LeaveRowCount().ShouldBe(0);
    }

    // ------------------------------------------------------------------ withdrawing leave

    [Fact]
    public async Task Should_Withdraw_A_Pending_Request_Only_When_The_User_Confirms()
    {
        GivenAnEmployeeWithAnnualLeave();
        var leaveId = CreateLeave(_employeeId, _annualLeaveId, DateTime.Today.AddDays(20), 2, status: RequestStatus.Open);

        Model.CallsTool("prepare_leave_cancellation", $@"{{""leaveRequestId"":{leaveId}}}").RepliesWith("Confirm below to withdraw it.");
        var output = await Service.SendMessage(new SendAiMessageInput { Message = "cancel my leave" });

        output.AssistantMessage.PendingAction.ActionType.ShouldBe(CancelLeaveAiPendingActionHandler.Type);
        LeaveRowCount().ShouldBe(1);

        var result = await Service.ConfirmAction(new EntityDto(output.AssistantMessage.PendingAction.Id));

        result.Action.Status.ShouldBe(AiPendingActionStatus.Completed);
        LeaveRowCount().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Draft_Withdrawing_Approved_Leave()
    {
        GivenAnEmployeeWithAnnualLeave();
        var leaveId = CreateLeave(_employeeId, _annualLeaveId, DateTime.Today.AddDays(20), 2, status: RequestStatus.Approved);

        Model.CallsTool("prepare_leave_cancellation", $@"{{""leaveRequestId"":{leaveId}}}").RepliesWith("That one is already approved.");
        var output = await Service.SendMessage(new SendAiMessageInput { Message = "cancel my leave" });

        LastToolResult().ShouldContain("can no longer be withdrawn");
        output.AssistantMessage.PendingAction.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Draft_Withdrawing_Someone_Elses_Leave()
    {
        GivenAnEmployeeWithAnnualLeave();
        var colleague = CreateEmployee("AIP02");
        var theirLeave = CreateLeave(colleague, _annualLeaveId, DateTime.Today.AddDays(20), 2, status: RequestStatus.Open);

        Model.CallsTool("prepare_leave_cancellation", $@"{{""leaveRequestId"":{theirLeave}}}").RepliesWith("I can't find that.");
        var output = await Service.SendMessage(new SendAiMessageInput { Message = "cancel leave " + theirLeave });

        LastToolResult().ShouldContain("not one of the user's leave requests");
        output.AssistantMessage.PendingAction.ShouldBeNull();
    }

    // ------------------------------------------------------------------ wiring

    [Fact]
    public void Every_Action_Handler_Class_Should_Be_Registered()
    {
        var handlerTypes = typeof(IAiPendingActionHandler).Assembly.GetTypes()
            .Where(t => typeof(IAiPendingActionHandler).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .ToList();

        Resolve<AiPendingActionHandlerResolver>().All.Select(h => h.GetType()).ShouldBe(handlerTypes, ignoreOrder: true);
    }
}
