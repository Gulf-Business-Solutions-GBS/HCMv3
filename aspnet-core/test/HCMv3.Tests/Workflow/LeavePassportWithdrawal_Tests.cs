using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// A passport withdrawal raised automatically from a leave request (legacy <c>LeaveRequest.js</c>
/// <c>CreatePassWithdrawal</c> / <c>PassportWithdrawalSubmit</c> and the mobile
/// <c>CreatePassportWithdrawalFromLeave</c>): the decision on its own, and the whole way through the leave
/// flow.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeavePassportWithdrawal_Tests : EssLeaveWorkflowTestBase
{
    private const string PassportRequestType = "EmployeeDocumentMvmt";

    private readonly ILeavePassportWithdrawalService _service;

    public LeavePassportWithdrawal_Tests()
    {
        _service = Resolve<ILeavePassportWithdrawalService>();
    }

    private static DateTime Soon => DateTime.Today.AddDays(30);

    // ------------------------------------------------------------------ fixtures

    private void SetPassportSetting(bool required, int step)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageSelfServiceSettings>().FirstOrDefault(s => s.TenantId == TenantId);

            if (settings == null)
            {
                settings = context.Set<ManageSelfServiceSettings>()
                    .Add(new ManageSelfServiceSettings { TenantId = TenantId }).Entity;
            }

            settings.IsPassportWithdrawalRequired = required;
            settings.PassportWithdrawalSubmissionStep = step;
        });
    }

    /// <summary>The withdrawable type is the one coded "2", legacy's passport.</summary>
    private int EnsurePassportType()
    {
        return UsingDbContext(context =>
        {
            var existing = context.Set<DocumentType>().FirstOrDefault(t => t.Code == "2");
            if (existing != null)
            {
                return existing.Id;
            }

            var type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = "2",
                NameEn = "Passport",
                NameAr = "جواز سفر"
            }).Entity;

            context.SaveChanges();
            return type.Id;
        });
    }

    private int CreatePassport(int employeeId)
    {
        var typeId = EnsurePassportType();

        return UsingDbContext(context =>
        {
            var document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = typeId,
                DocumentNum = "P" + Guid.NewGuid().ToString("N").Substring(0, 6),
                IssueDate = DateTime.Today.AddYears(-2),
                ExpiryDate = DateTime.Today.AddYears(5)
            }).Entity;

            context.SaveChanges();
            return document.Id;
        });
    }

    /// <summary>
    /// A one-step passport chain and the catalog row switched on. With <paramref name="withApprover"/> false
    /// the step's role has nobody in it, so a submission cannot be routed and fails.
    /// </summary>
    private int EnablePassportWorkflow(string approverCode, bool withApprover = true, bool essEnabled = true, bool workflowEnabled = true)
    {
        var approverId = CreateEmployee(approverCode);

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Passport approver",
                NameAr = "Passport approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "PPW-LV",
                NameEn = "Passport",
                NameAr = "Passport",
                WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            if (withApprover)
            {
                context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
                {
                    TenantId = TenantId,
                    EmployeeId = approverId,
                    WorkflowRoleId = role.Id,
                    StartDate = DateTime.Today.AddYears(-1),
                    EndDate = DateTime.Today.AddYears(2)
                });
            }

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == PassportRequestType);
            type.IsWorkflowEnabled = workflowEnabled;
            type.IsEssEnabled = essEnabled;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private int AllDaysCountType(string nameEn = "Annual Leave") => CreateAbsenceType(t =>
    {
        t.IncludeWeekend = true;
        t.IncludePublicHoliday = true;
        t.IncludeRestDay = true;
    }, nameEn);

    private Task<ApplyForLeaveResultDto> ApplyAsync(int typeId, bool travelling = true, int days = 5, string comments = null)
    {
        return _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(days - 1),
            IsTravelling = travelling,
            Comments = comments
        });
    }

    /// <summary>Every movement of the employee as stored: pending, hidden or soft-deleted alike.</summary>
    private List<EmployeeDocumentMvmt> MovementsOf(int employeeId)
    {
        return UsingDbContext(context => context.Set<EmployeeDocumentMvmt>()
            .IgnoreQueryFilters()
            .Where(m => m.EmployeeId == employeeId)
            .OrderBy(m => m.Id)
            .ToList());
    }

    private WorkflowRequest RequestRow(int requestId)
    {
        return UsingDbContext(context => context.Set<WorkflowRequest>()
            .IgnoreQueryFilters()
            .Single(r => r.Id == requestId));
    }

    /// <summary>
    /// A travelling leave waiting at step 1 of a one-step chain, applied for while the setting was OFF so
    /// that nothing has been raised yet and the service can be asked directly.
    /// </summary>
    private async Task<(int meId, int leaveId, int leaveRequestId, int leaveApproverId, int typeId)> PendingLeaveAsync(
        string code, bool travelling = true, bool withPassport = true, string comments = null)
    {
        var meId = CreateCurrentUserEmployee(code);
        var leaveApproverId = EnableLeaveWorkflowWithApprover(code + "LA");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);

        if (withPassport)
        {
            CreatePassport(meId);
        }

        SetPassportSetting(required: false, step: 1);

        var applied = await ApplyAsync(typeId, travelling, comments: comments);

        return (meId, applied.EmployeeAbsenceId, applied.WorkflowRequestId.Value, leaveApproverId, typeId);
    }

    // ------------------------------------------------------------------ the step rule

    [Theory]
    [InlineData(0, WorkflowStepAction.Submitted, 0, true)]   // legacy: a new form is at step 0
    [InlineData(0, WorkflowStepAction.Submitted, 1, true)]
    [InlineData(1, WorkflowStepAction.Submitted, 1, true)]   // the chain's creator step
    [InlineData(1, WorkflowStepAction.Submitted, 0, false)]  // no creator step: step 1 is an approver's
    [InlineData(2, WorkflowStepAction.Submitted, 1, false)]
    [InlineData(2, WorkflowStepAction.Approved, 2, true)]
    [InlineData(2, WorkflowStepAction.Approved, 3, false)]
    [InlineData(2, WorkflowStepAction.Approved, 1, false)]
    [InlineData(0, WorkflowStepAction.Approved, 1, false)]
    public void Should_Match_Only_The_Configured_Step(int configured, WorkflowStepAction action, int step, bool expected)
    {
        LeavePassportWithdrawalService.IsConfiguredStep(configured, action, step).ShouldBe(expected);
    }

    [Fact]
    public async Task The_Trigger_Point_Should_Follow_The_Setting_The_Step_And_The_Kind_Of_Request()
    {
        var leave = await PendingLeaveAsync("LPW090");
        var request = RequestRow(leave.leaveRequestId);

        // Off.
        (await _service.IsTriggerPointAsync(request, WorkflowStepAction.Approved, 1)).ShouldBeFalse();

        SetPassportSetting(required: true, step: 1);

        (await _service.IsTriggerPointAsync(request, WorkflowStepAction.Approved, 1)).ShouldBeTrue();
        (await _service.IsTriggerPointAsync(request, WorkflowStepAction.Approved, 2)).ShouldBeFalse();
        (await _service.IsTriggerPointAsync(null, WorkflowStepAction.Approved, 1)).ShouldBeFalse();

        // A change to a leave is never one.
        request.IsAmendment = true;
        request.RequestAction = WorkflowRequestAction.Cancellation;
        (await _service.IsTriggerPointAsync(request, WorkflowStepAction.Approved, 1)).ShouldBeFalse();

        // Nor is another type's request.
        request.IsAmendment = false;
        request.RequestAction = WorkflowRequestAction.New;
        request.RequestType = PassportRequestType;
        (await _service.IsTriggerPointAsync(request, WorkflowStepAction.Approved, 1)).ShouldBeFalse();
    }

    // ------------------------------------------------------------------ the decision

    [Fact]
    public async Task Should_Raise_Nothing_When_The_Setting_Is_Off()
    {
        var leave = await PendingLeaveAsync("LPW100");
        EnablePassportWorkflow("LPW100PA");

        var decision = await _service.DecideAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);
        var raised = await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        decision.SkipReason.ShouldBe(LeavePassportWithdrawalSkipReason.SettingOff);
        raised.ShouldBeNull();
        MovementsOf(leave.meId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Raise_Nothing_At_Another_Step()
    {
        var leave = await PendingLeaveAsync("LPW110");
        EnablePassportWorkflow("LPW110PA");
        SetPassportSetting(required: true, step: 2);

        var decision = await _service.DecideAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);
        var raised = await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        decision.SkipReason.ShouldBe(LeavePassportWithdrawalSkipReason.NotTheConfiguredStep);
        raised.ShouldBeNull();
        MovementsOf(leave.meId).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(WorkflowRequestAction.Amendment)]
    [InlineData(WorkflowRequestAction.Cancellation)]
    [InlineData(WorkflowRequestAction.Return)]
    public async Task Should_Raise_Nothing_For_A_Change_To_A_Leave(WorkflowRequestAction requestAction)
    {
        var leave = await PendingLeaveAsync("LPW12" + (int)requestAction);
        EnablePassportWorkflow("LPW12PA" + (int)requestAction);
        SetPassportSetting(required: true, step: 1);

        // A request of that kind against the same leave, as the amendment service stores one.
        var changeRequestId = UsingDbContext(context =>
        {
            var request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = leave.meId,
                RequestType = "EmployeeAbsence",
                TargetEntityId = leave.leaveId,
                RequestSubTypeId = leave.typeId,
                RequestNumber = "CHG-" + (int)requestAction,
                RequestStatus = RequestStatus.Open,
                WorkflowStep = 1,
                IsAmendment = true,
                RequestAction = requestAction,
                AmendmentJson = "{}"
            }).Entity;

            context.SaveChanges();
            return request.Id;
        });

        var decision = await _service.DecideAsync(changeRequestId, WorkflowStepAction.Approved, 1);
        var raised = await _service.RaiseIfRequiredAsync(changeRequestId, WorkflowStepAction.Approved, 1);

        decision.SkipReason.ShouldBe(LeavePassportWithdrawalSkipReason.NotANewLeave);
        raised.ShouldBeNull();
        MovementsOf(leave.meId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Raise_Nothing_When_The_Employee_Is_Not_Travelling()
    {
        var leave = await PendingLeaveAsync("LPW130", travelling: false);
        EnablePassportWorkflow("LPW130PA");
        SetPassportSetting(required: true, step: 1);

        var decision = await _service.DecideAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        decision.SkipReason.ShouldBe(LeavePassportWithdrawalSkipReason.NotTravelling);
        (await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1)).ShouldBeNull();
        MovementsOf(leave.meId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Raise_Nothing_And_Not_Throw_When_There_Is_No_Passport_On_File()
    {
        var leave = await PendingLeaveAsync("LPW140", withPassport: false);
        EnsurePassportType();
        EnablePassportWorkflow("LPW140PA");
        SetPassportSetting(required: true, step: 1);

        var decision = await _service.DecideAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);
        var raised = await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        decision.SkipReason.ShouldBe(LeavePassportWithdrawalSkipReason.NoDocumentOnFile);
        raised.ShouldBeNull();
        MovementsOf(leave.meId).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Should_Raise_Nothing_When_Passport_Workflow_Or_Self_Service_Is_Off(bool workflowEnabled, bool essEnabled)
    {
        var leave = await PendingLeaveAsync("LPW15" + (workflowEnabled ? "W" : "E"));
        EnablePassportWorkflow("LPW15PA" + (workflowEnabled ? "W" : "E"), essEnabled: essEnabled, workflowEnabled: workflowEnabled);
        SetPassportSetting(required: true, step: 1);

        var decision = await _service.DecideAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        decision.SkipReason.ShouldBe(LeavePassportWithdrawalSkipReason.PassportWorkflowOff);
        (await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1)).ShouldBeNull();
        MovementsOf(leave.meId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Raise_One_Withdrawal_With_The_Legacy_Values_Into_The_Passport_Chain()
    {
        var leave = await PendingLeaveAsync("LPW160", comments: "Family visit");
        var passportApproverId = EnablePassportWorkflow("LPW160PA");
        SetPassportSetting(required: true, step: 1);

        var leaveNumber = RequestRow(leave.leaveRequestId).RequestNumber;

        var movementId = await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        movementId.ShouldNotBeNull();

        var movement = MovementsOf(leave.meId).ShouldHaveSingleItem();
        movement.Id.ShouldBe(movementId.Value);

        // Legacy: the day before the leave starts to the day after it ends, the leave type as the reason,
        // and no guarantor.
        movement.WithdrawDate.ShouldBe(Soon.AddDays(-1));
        movement.ReturnDate.ShouldBe(Soon.AddDays(5));
        movement.WithdrawalReason.ShouldBe("Annual Leave");
        movement.GuarantorId.ShouldBeNull();
        movement.ActualReturnDate.ShouldBe(default);

        // Linked to the leave, and says so.
        movement.SourceEmployeeAbsenceId.ShouldBe(leave.leaveId);
        movement.Comments.ShouldContain(leaveNumber);
        movement.Comments.ShouldContain("Family visit");

        // A normal request: its own number, Open, waiting on the PASSPORT chain's approver.
        movement.WorkflowStatus.ShouldBe(RequestStatus.Open);
        movement.WorkflowRequestId.ShouldNotBeNull();

        var request = RequestRow(movement.WorkflowRequestId.Value);
        request.RequestType.ShouldBe(PassportRequestType);
        request.EmployeeId.ShouldBe(leave.meId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.ApproverId.ShouldBe(passportApproverId);
        request.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        request.RequestNumber.ShouldNotBe(leaveNumber);
        request.SummaryEn.ShouldContain("Raised automatically from leave request " + leaveNumber);
        request.SummaryAr.ShouldContain(leaveNumber);
        request.SummaryAr.ShouldNotContain("Raised automatically");

        // Raised as the employee, and the log's first entry says where it came from.
        var created = UsingDbContext(context => context.Set<RequestLog>()
            .Single(l => l.WorkflowRequestId == request.Id && l.ActionType == RequestActionType.Created));
        created.ActionById.ShouldBe(leave.meId);
        created.RequestActionComment.ShouldContain(leaveNumber);

        // The leave itself is untouched by any of it.
        LeaveRow(leave.leaveId).WorkflowStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task Should_Raise_Only_One_Withdrawal_For_The_Same_Leave()
    {
        var leave = await PendingLeaveAsync("LPW170");
        EnablePassportWorkflow("LPW170PA");
        SetPassportSetting(required: true, step: 1);

        var first = await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);
        var decision = await _service.DecideAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);
        var second = await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        first.ShouldNotBeNull();
        decision.SkipReason.ShouldBe(LeavePassportWithdrawalSkipReason.AlreadyRaised);
        second.ShouldBeNull();
        MovementsOf(leave.meId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Not_Raise_Again_After_The_Employee_Withdrew_The_Automatic_Request()
    {
        var leave = await PendingLeaveAsync("LPW180");
        EnablePassportWorkflow("LPW180PA");
        SetPassportSetting(required: true, step: 1);

        var movementId = await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        await Resolve<IMyPassportWithdrawalAppService>().CancelMyPassportWithdrawal(new EntityDto(movementId.Value));

        var again = await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        again.ShouldBeNull();
        MovementsOf(leave.meId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Raise_For_The_Leave_Employee_When_They_Have_No_Login()
    {
        var leave = await PendingLeaveAsync("LPW190");
        EnablePassportWorkflow("LPW190PA");
        SetPassportSetting(required: true, step: 1);

        // Whoever triggers it now is not the employee: the signed-in user is no longer linked to anybody.
        UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == leave.meId).UserId = null);

        var movementId = await _service.RaiseIfRequiredAsync(leave.leaveRequestId, WorkflowStepAction.Approved, 1);

        movementId.ShouldNotBeNull();

        var movement = MovementsOf(leave.meId).ShouldHaveSingleItem();
        var request = RequestRow(movement.WorkflowRequestId.Value);

        request.EmployeeId.ShouldBe(leave.meId);
        UsingDbContext(context => context.Set<RequestLog>()
                .Single(l => l.WorkflowRequestId == request.Id && l.ActionType == RequestActionType.Created))
            .ActionById.ShouldBe(leave.meId);
    }

    // ------------------------------------------------------------------ through the leave flow

    [Fact]
    public async Task Approving_The_Leave_At_The_Configured_Step_Should_Raise_The_Passport_Request()
    {
        var meId = CreateCurrentUserEmployee("LPW200");
        var leaveApproverId = EnableLeaveWorkflowWithApprover("LPW200LA");
        var passportApproverId = EnablePassportWorkflow("LPW200PA");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);
        CreatePassport(meId);
        SetPassportSetting(required: true, step: 1);

        var applied = await ApplyAsync(typeId);

        // Step 1 is the approver's, so applying is not the moment.
        MovementsOf(meId).ShouldBeEmpty();

        await _approvalService.ApproveAsync(applied.WorkflowRequestId.Value, leaveApproverId, "ok");

        // The leave approval is exactly what it would have been.
        RequestRow(applied.WorkflowRequestId.Value).RequestStatus.ShouldBe(RequestStatus.Approved);
        LeaveRow(applied.EmployeeAbsenceId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(5);

        var movement = MovementsOf(meId).ShouldHaveSingleItem();
        movement.SourceEmployeeAbsenceId.ShouldBe(applied.EmployeeAbsenceId);
        movement.WithdrawDate.ShouldBe(Soon.AddDays(-1));
        movement.ReturnDate.ShouldBe(Soon.AddDays(5));
        movement.WorkflowStatus.ShouldBe(RequestStatus.Open);

        // In the employee's own Passport Withdrawals list, with a number of its own...
        var withdrawals = Resolve<IMyPassportWithdrawalAppService>();
        var listed = (await withdrawals.GetMyPassportWithdrawals(
                new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 }))
            .Items.ShouldHaveSingleItem();

        listed.Id.ShouldBe(movement.Id);
        listed.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        listed.RequestNumber.ShouldNotBe(RequestRow(applied.WorkflowRequestId.Value).RequestNumber);
        listed.WithdrawalReason.ShouldBe("Annual Leave");
        listed.GuarantorId.ShouldBeNull();

        // ...and decided like any other.
        await _approvalService.ApproveAsync(movement.WorkflowRequestId.Value, passportApproverId, "ok");

        MovementsOf(meId).Single().WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Submitting_The_Leave_Should_Raise_The_Passport_Request_When_The_Step_Is_Zero()
    {
        var meId = CreateCurrentUserEmployee("LPW210");
        var leaveApproverId = EnableLeaveWorkflowWithApprover("LPW210LA");
        EnablePassportWorkflow("LPW210PA");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);
        CreatePassport(meId);
        SetPassportSetting(required: true, step: 0);

        var applied = await ApplyAsync(typeId);

        applied.WorkflowRequestId.ShouldNotBeNull();
        RequestRow(applied.WorkflowRequestId.Value).RequestStatus.ShouldBe(RequestStatus.Open);
        MovementsOf(meId).ShouldHaveSingleItem().SourceEmployeeAbsenceId.ShouldBe(applied.EmployeeAbsenceId);

        // Sent back, sent again and approved: the same leave passes the same point twice more.
        await _approvalService.SendBackAsync(applied.WorkflowRequestId.Value, leaveApproverId, "Shorter please");

        await _myLeaveAppService.ResubmitMyLeave(new ResubmitMyLeaveInput
        {
            EmployeeAbsenceId = applied.EmployeeAbsenceId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(3),
            IsTravelling = true
        });

        await _approvalService.ApproveAsync(applied.WorkflowRequestId.Value, leaveApproverId, "ok");

        LeaveRow(applied.EmployeeAbsenceId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
        MovementsOf(meId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_Leave_Sent_Back_And_Approved_Again_At_The_Same_Step_Should_Still_Have_One_Passport_Request()
    {
        var meId = CreateCurrentUserEmployee("LPW220");
        var leaveApproverId = EnableLeaveWorkflowWithApprover("LPW220LA");
        EnablePassportWorkflow("LPW220PA");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);
        CreatePassport(meId);
        SetPassportSetting(required: true, step: 1);

        // A second approver on the same step, both required, so the first approval leaves the leave at
        // step 1 and the step can be approved, sent back, and approved again.
        var secondApproverId = CreateEmployee("LPW220LB");
        UsingDbContext(context =>
        {
            var detail = context.Set<RequestDetail>()
                .Single(d => d.RequestMaster.Code == "LEAVE-RET" && d.Step == 1);

            detail.ApprovalRequire = ApprovalRequire.All;

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = secondApproverId,
                WorkflowRoleId = detail.WorkflowRoleId.Value,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });
        });

        var applied = await ApplyAsync(typeId);
        var requestId = applied.WorkflowRequestId.Value;

        await _approvalService.ApproveAsync(requestId, leaveApproverId, "ok");

        // Raised on the first approval at the step, as legacy raised it on the approver's click.
        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Open);
        MovementsOf(meId).Count.ShouldBe(1);

        await _approvalService.SendBackAsync(requestId, secondApproverId, "Check dates");

        await _myLeaveAppService.ResubmitMyLeave(new ResubmitMyLeaveInput
        {
            EmployeeAbsenceId = applied.EmployeeAbsenceId,
            AbsenceTypeId = typeId,
            StartDate = Soon,
            EndDate = Soon.AddDays(4),
            IsTravelling = true
        });

        await _approvalService.ApproveAsync(requestId, leaveApproverId, "ok");
        await _approvalService.ApproveAsync(requestId, secondApproverId, "ok");

        LeaveRow(applied.EmployeeAbsenceId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
        MovementsOf(meId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_Failure_Raising_The_Passport_Request_Should_Not_Break_The_Leave_Approval()
    {
        var meId = CreateCurrentUserEmployee("LPW230");
        var leaveApproverId = EnableLeaveWorkflowWithApprover("LPW230LA");

        // Nobody holds the passport chain's role, so the passport request cannot be routed and its
        // submission throws.
        EnablePassportWorkflow("LPW230PA", withApprover: false);

        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);
        CreatePassport(meId);
        SetPassportSetting(required: true, step: 1);

        var applied = await ApplyAsync(typeId);

        // The approval goes through untouched although the raise behind it throws.
        await _approvalService.ApproveAsync(applied.WorkflowRequestId.Value, leaveApproverId, "ok");

        RequestRow(applied.WorkflowRequestId.Value).RequestStatus.ShouldBe(RequestStatus.Approved);
        LeaveRow(applied.EmployeeAbsenceId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
        QuotaRow(meId, typeId).QuotaUsed.ShouldBe(5);

        // Nothing was sent to anybody. (The half-made rows are rolled back with the raise's own
        // transaction in production; this harness runs units of work without transactions, so all that can
        // be asserted here is that no passport request ever reached the chain.)
        UsingDbContext(context => context.Set<WorkflowRequest>()
            .IgnoreQueryFilters()
            .Count(r => r.RequestType == PassportRequestType && r.RequestStatus != RequestStatus.Draft)).ShouldBe(0);
        MovementsOf(meId).ShouldAllBe(m => m.WorkflowStatus == RequestStatus.Draft);

        // Proof the raise really does fail in this set-up, rather than having been skipped. A fresh leave
        // request id is not needed: the failure is in routing, before anything about the leave is read again.
        UsingDbContext(context =>
        {
            foreach (var leftover in context.Set<EmployeeDocumentMvmt>().IgnoreQueryFilters().Where(m => m.EmployeeId == meId))
            {
                leftover.SourceEmployeeAbsenceId = null;
            }
        });

        await Should.ThrowAsync<Exception>(() =>
            _service.RaiseIfRequiredAsync(applied.WorkflowRequestId.Value, WorkflowStepAction.Approved, 1));
    }

    [Fact]
    public async Task Approving_A_Leave_The_Employee_Is_Not_Travelling_On_Should_Raise_Nothing()
    {
        var meId = CreateCurrentUserEmployee("LPW240");
        var leaveApproverId = EnableLeaveWorkflowWithApprover("LPW240LA");
        EnablePassportWorkflow("LPW240PA");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);
        CreatePassport(meId);
        SetPassportSetting(required: true, step: 1);

        var applied = await ApplyAsync(typeId, travelling: false);

        await _approvalService.ApproveAsync(applied.WorkflowRequestId.Value, leaveApproverId, "ok");

        LeaveRow(applied.EmployeeAbsenceId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
        MovementsOf(meId).ShouldBeEmpty();
    }

    [Fact]
    public async Task Approving_A_Leave_With_The_Setting_Off_Should_Raise_Nothing()
    {
        var meId = CreateCurrentUserEmployee("LPW250");
        var leaveApproverId = EnableLeaveWorkflowWithApprover("LPW250LA");
        EnablePassportWorkflow("LPW250PA");
        var typeId = AllDaysCountType();
        CreateQuota(meId, typeId, accrued: 30);
        CreatePassport(meId);
        SetPassportSetting(required: false, step: 1);

        var applied = await ApplyAsync(typeId);

        await _approvalService.ApproveAsync(applied.WorkflowRequestId.Value, leaveApproverId, "ok");

        LeaveRow(applied.EmployeeAbsenceId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
        MovementsOf(meId).ShouldBeEmpty();
    }
}
