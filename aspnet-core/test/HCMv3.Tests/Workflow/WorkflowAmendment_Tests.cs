using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers amendments: changing a transaction that has already been approved.
///
/// <para>
/// This is the one gap the single-table design does not close by itself. Everywhere else, keeping the
/// request and the transaction in one row is precisely what stops them diverging. But an approved row is
/// live, and editing it directly bypasses approval completely -- payroll simply sees different numbers the
/// next morning with no record that anybody decided anything.
/// </para>
///
/// <para>
/// The rule that makes amendments safe, and the thing most of these tests exist to protect, is that an
/// amendment must not touch the row it targets until it is approved. Refusing an edit must never destroy the
/// thing being edited: if declining a change marked the leave Rejected, an employee would lose leave they
/// were still entitled to because a manager said no to moving it.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowAmendment_Tests : AppTestBase
{
    private readonly IMyLeaveAppService _myLeaveAppService;
    private readonly IWorkflowApprovalService _approvalService;
    private readonly IWorkflowAmendmentService _amendmentService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public WorkflowAmendment_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _myLeaveAppService = Resolve<IMyLeaveAppService>();
        _approvalService = Resolve<IWorkflowApprovalService>();
        _amendmentService = Resolve<IWorkflowAmendmentService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

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

        // ESS counts leave days against the employee's week, so every test employee needs one.
        UsingDbContext(context => HCMv3.Tests.Workflow.EssLeaveTestDuty.AssignAllWorkingWeek(context, AbpSession.TenantId ?? 1, employee.Id));

        return employee.Id;
    }

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    /// <summary>
    /// An ordinary leave type. <c>AllowToReturnEarly</c> is set explicitly rather than left at its
    /// <c>false</c> default, because the handler now refuses any amendment that pulls a leave's end date in
    /// for a type that must be taken in full -- and most of the amendments in this file do exactly that while
    /// testing something else entirely (balance, snapshot, summary). The types that forbid it are the special
    /// case, covered on its own in <c>ReturnFromLeave_Tests</c>.
    /// </summary>
    private int CreateAbsenceType(string nameEn = "Annual", bool allowToReturnEarly = true)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn,
                AllowToReturnEarly = allowToReturnEarly
            }).Entity;
        });

        return absenceType.Id;
    }

    private void CreateQuota(int employeeId, int absenceTypeId, double accrued, double used = 0)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAbsenceQuota>().Add(new EmployeeAbsenceQuota
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                EntitlementFrom = DateTime.Today.AddYears(-1),
                EntitlementTo = DateTime.Today.AddYears(2),
                DeductionFrom = DateTime.Today.AddYears(-1),
                DeductionTo = DateTime.Today.AddYears(2),
                QuotaAccrued = accrued,
                QuotaUsed = used
            });
        });
    }

    /// <summary>A one-step leave chain with a named approver, and the catalog row switched on.</summary>
    private int EnableLeaveWorkflowWithApprover(string approverCode)
    {
        var approverId = CreateEmployee(approverCode);

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "LEAVE-STD",
                NameEn = "Standard Leave",
                NameAr = "Standard Leave",
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

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = approverId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAbsence");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    /// <summary>Applies for leave and takes it all the way to approved, which is the state amendments need.</summary>
    private async Task<int> CreateApprovedLeaveAsync(int absenceTypeId, DateTime start, int days, int approverId)
    {
        var result = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = absenceTypeId,
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            Comments = "original"
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId.Value, approverId, "ok");

        return result.EmployeeAbsenceId;
    }

    private EmployeeAbsence LeaveRow(int leaveId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>()
            .IgnoreQueryFilters()
            .Single(a => a.Id == leaveId));
    }

    private WorkflowRequest RequestRow(int requestId)
    {
        return UsingDbContext(context => context.Set<WorkflowRequest>().Single(r => r.Id == requestId));
    }

    private static AmendMyLeaveInput Amend(int leaveId, int absenceTypeId, DateTime start, int days, string reason = "Plans changed")
    {
        return new AmendMyLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            AbsenceTypeId = absenceTypeId,
            StartDate = start,
            EndDate = start.AddDays(days - 1),
            Reason = reason
        };
    }

    [Fact]
    public async Task Raising_An_Amendment_Should_Leave_The_Approved_Leave_Completely_Untouched()
    {
        // The core promise. Until an approver agrees, payroll and quota must see exactly what they saw
        // before the change was asked for.
        var meId = CreateCurrentUserEmployee("AMD100");
        var approverId = EnableLeaveWorkflowWithApprover("AMD100APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var before = LeaveRow(leaveId);

        await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start.AddDays(7), days: 3));

        var after = LeaveRow(leaveId);
        after.StartDate.ShouldBe(before.StartDate);
        after.EndDate.ShouldBe(before.EndDate);
        after.TotalLeaveDays.ShouldBe(before.TotalLeaveDays);
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task An_Amendment_Should_Carry_Both_The_Proposal_And_The_Snapshot()
    {
        var meId = CreateCurrentUserEmployee("AMD110");
        var approverId = EnableLeaveWorkflowWithApprover("AMD110APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start, days: 3));

        var request = RequestRow(requestId);
        request.IsAmendment.ShouldBeTrue();
        request.TargetEntityId.ShouldBe(leaveId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);

        var proposed = JsonSerializer.Deserialize<LeaveAmendment>(request.AmendmentJson);
        proposed.TotalLeaveDays.ShouldBe(3);

        // The approver needs the before as well as the after -- "5 days becomes 3" is a different decision
        // from "3 days".
        var snapshot = JsonSerializer.Deserialize<LeaveAmendment>(request.SnapshotJson);
        snapshot.TotalLeaveDays.ShouldBe(5);
    }

    [Fact]
    public async Task An_Amendment_Summary_Should_Read_As_A_Change()
    {
        var meId = CreateCurrentUserEmployee("AMD120");
        var approverId = EnableLeaveWorkflowWithApprover("AMD120APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start, days: 3));

        var request = RequestRow(requestId);
        request.SummaryEn.ShouldStartWith("Change:");
        request.SummaryEn.ShouldContain("5 day(s)");
        request.SummaryEn.ShouldContain("3 day(s)");
    }

    [Fact]
    public async Task Approving_An_Amendment_Should_Apply_It_And_Keep_The_Leave_Approved()
    {
        var meId = CreateCurrentUserEmployee("AMD130");
        var approverId = EnableLeaveWorkflowWithApprover("AMD130APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var newStart = start.AddDays(7);
        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, newStart, days: 3));

        await _approvalService.ApproveAsync(requestId, approverId, "fine");

        var leave = LeaveRow(leaveId);
        leave.StartDate.Date.ShouldBe(newStart.Date);
        leave.TotalLeaveDays.ShouldBe(3);

        // Still approved. It never stopped being approved.
        leave.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Rejecting_An_Amendment_Should_Not_Destroy_The_Leave_It_Was_Changing()
    {
        // THE test this whole file exists for. Marking the row Rejected here -- which is what every
        // non-amendment rejection does -- would take away leave the employee is still entitled to, as a side
        // effect of a manager declining to move it.
        var meId = CreateCurrentUserEmployee("AMD140");
        var approverId = EnableLeaveWorkflowWithApprover("AMD140APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start.AddDays(7), days: 3));

        await _approvalService.RejectAsync(requestId, approverId, "Cover already arranged for those dates");

        var leave = LeaveRow(leaveId);
        leave.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        leave.StartDate.Date.ShouldBe(start.Date);
        leave.TotalLeaveDays.ShouldBe(5);
    }

    [Fact]
    public async Task Sending_An_Amendment_Back_Should_Not_Disturb_The_Leave_Either()
    {
        var meId = CreateCurrentUserEmployee("AMD150");
        var approverId = EnableLeaveWorkflowWithApprover("AMD150APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start.AddDays(7), days: 3));

        await _approvalService.SendBackAsync(requestId, approverId, "Say which days exactly");

        var leave = LeaveRow(leaveId);
        leave.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        leave.StartDate.Date.ShouldBe(start.Date);
    }

    [Fact]
    public async Task A_Leave_With_A_Pending_Amendment_Should_Stay_Visible_To_The_Rest_Of_The_System()
    {
        // Restated from the other side, through the filter itself rather than the row: an amendment in
        // flight must not take an approved leave out of payroll's view.
        var meId = CreateCurrentUserEmployee("AMD160");
        var approverId = EnableLeaveWorkflowWithApprover("AMD160APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start.AddDays(7), days: 3));

        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeAbsence>>();

        using (_unitOfWorkManager.Begin())
        {
            // ApprovedOnly is ON here, exactly as payroll would see it.
            var visible = repository.GetAll().Any(a => a.Id == leaveId);
            visible.ShouldBeTrue("An amendment in flight hid an approved leave from the rest of the system.");
        }
    }

    [Fact]
    public async Task Only_One_Amendment_At_A_Time_Should_Be_Allowed_Against_A_Row()
    {
        // Two in flight would apply in approval order rather than the order raised, and the second would
        // overwrite the first without its approver having seen the values they were agreeing to.
        var meId = CreateCurrentUserEmployee("AMD170");
        var approverId = EnableLeaveWorkflowWithApprover("AMD170APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start.AddDays(7), days: 3));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start.AddDays(14), days: 2)));
    }

    [Fact]
    public async Task An_Amendment_Should_Be_Possible_Again_Once_The_First_Is_Decided()
    {
        var meId = CreateCurrentUserEmployee("AMD180");
        var approverId = EnableLeaveWorkflowWithApprover("AMD180APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var firstId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start.AddDays(7), days: 3));
        await _approvalService.RejectAsync(firstId, approverId, "no");

        var secondId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start.AddDays(14), days: 2));

        secondId.ShouldNotBe(firstId);
    }

    [Fact]
    public async Task A_Leave_That_Is_Not_Yet_Approved_Should_Not_Be_Amendable()
    {
        // Nothing has been agreed yet, so there is nothing to amend -- withdraw and re-apply, which costs
        // nobody an approval round-trip.
        var meId = CreateCurrentUserEmployee("AMD190");
        EnableLeaveWorkflowWithApprover("AMD190APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var result = await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = start,
            EndDate = start.AddDays(4),
            Comments = "original"
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.AmendMyLeave(Amend(result.EmployeeAbsenceId, typeId, start.AddDays(7), days: 3)));
    }

    [Fact]
    public async Task Amending_Someone_Elses_Leave_Should_Be_Refused()
    {
        var meId = CreateCurrentUserEmployee("AMD200");
        var approverId = EnableLeaveWorkflowWithApprover("AMD200APP");
        var otherId = CreateEmployee("AMD200OTHER");
        var typeId = CreateAbsenceType();
        CreateQuota(otherId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);

        // Built directly, because ApplyForLeave would attribute it to the signed-in employee.
        EmployeeAbsence theirLeave = null;
        UsingDbContext(context =>
        {
            theirLeave = context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                AbsenceTypeId = typeId,
                StartDate = start,
                EndDate = start.AddDays(4),
                TotalLeaveDays = 5,
                WorkflowStatus = RequestStatus.Approved
            }).Entity;
        });

        var theirLeaveId = theirLeave.Id;

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _myLeaveAppService.AmendMyLeave(Amend(theirLeaveId, typeId, start, days: 3)));
    }

    [Fact]
    public async Task Shortening_An_Approved_Leave_Should_Not_Be_Refused_For_Balance()
    {
        // Approving the leave puts its days into QuotaUsed, so the whole entitlement is consumed. Without
        // crediting those days back, an amendment that ASKS FOR LESS would be refused for insufficient
        // balance -- the most confusing rejection this system could produce.
        var meId = CreateCurrentUserEmployee("AMD210");
        var approverId = EnableLeaveWorkflowWithApprover("AMD210APP");
        var typeId = CreateAbsenceType();

        // Whole entitlement consumed by the leave about to be amended: 5 accrued, 5 used, 0 remaining.
        CreateQuota(meId, typeId, accrued: 5);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start, days: 3));

        await _approvalService.ApproveAsync(requestId, approverId, "ok");

        LeaveRow(leaveId).TotalLeaveDays.ShouldBe(3);
    }

    [Fact]
    public async Task An_Amendment_Beyond_The_Balance_Should_Still_Be_Refused()
    {
        // Same set-up as above, so the credit-back is exercised rather than bypassed: 5 accrued, all 5 used
        // by this very leave once approved. Crediting its own days back gives 5 available -- eight is still
        // three too many.
        var meId = CreateCurrentUserEmployee("AMD220");
        var approverId = EnableLeaveWorkflowWithApprover("AMD220APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 5);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start, days: 8)));
    }

    [Fact]
    public async Task Extending_An_Approved_Leave_Within_The_Balance_Should_Be_Allowed()
    {
        // The other side of the credit-back. 10 accrued, 5 used by this leave once approved, leaving 5 free;
        // asking for 8 means 3 more than the current 5, which fits.
        var meId = CreateCurrentUserEmployee("AMD225");
        var approverId = EnableLeaveWorkflowWithApprover("AMD225APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 10);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start, days: 8));
        await _approvalService.ApproveAsync(requestId, approverId, "ok");

        LeaveRow(leaveId).TotalLeaveDays.ShouldBe(8);
    }

    [Fact]
    public async Task An_Amendment_Overlapping_Other_Leave_Should_Be_Refused()
    {
        var meId = CreateCurrentUserEmployee("AMD230");
        var approverId = EnableLeaveWorkflowWithApprover("AMD230APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var first = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, first, days: 3, approverId);

        var second = DateTime.Today.AddDays(60);
        await CreateApprovedLeaveAsync(typeId, second, days: 3, approverId);

        // Moving the first onto the second is a collision the employee cannot see coming, so it is caught
        // when raised rather than when approved.
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, second, days: 3)));
    }

    [Fact]
    public async Task My_Leaves_Should_Report_Which_Rows_Can_Be_Changed()
    {
        var meId = CreateCurrentUserEmployee("AMD240");
        var approverId = EnableLeaveWorkflowWithApprover("AMD240APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 3, approverId);

        var listed = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput
        {
            IncludeHistory = true,
            MaxResultCount = 50,
            SkipCount = 0
        });

        var row = listed.Items.Single(l => l.Id == leaveId);
        row.CanAmend.ShouldBeTrue();
        row.HasPendingAmendment.ShouldBeFalse();
    }

    [Fact]
    public async Task My_Leaves_Should_Flag_A_Change_That_Is_Already_In_Flight()
    {
        // Without this the screen looks identical before and after asking for a change -- an amendment
        // deliberately leaves the row alone -- and the employee submits it again.
        var meId = CreateCurrentUserEmployee("AMD250");
        var approverId = EnableLeaveWorkflowWithApprover("AMD250APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 3, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start.AddDays(7), days: 2));

        var listed = await _myLeaveAppService.GetMyLeaves(new GetMyLeavesInput
        {
            IncludeHistory = true,
            MaxResultCount = 50,
            SkipCount = 0
        });

        var row = listed.Items.Single(l => l.Id == leaveId);
        row.HasPendingAmendment.ShouldBeTrue();
        row.PendingAmendmentRequestId.ShouldBe(requestId);
        row.CanAmend.ShouldBeFalse();
    }

    [Fact]
    public async Task A_Type_Whose_Handler_Does_Not_Support_Amendment_Should_Refuse_One()
    {
        // Assets have no ApplyAmendmentAsync, so an amendment is refused when RAISED. Accepting it and
        // doing nothing on approval would be worse: employee and approver would both believe the change
        // took effect.
        var meId = CreateCurrentUserEmployee("AMD260");

        using (var uow = _unitOfWorkManager.Begin())
        {
            (await _amendmentService.IsAmendmentEnabledAsync("EmployeeAsset")).ShouldBeFalse();

            await Assert.ThrowsAsync<UserFriendlyException>(() =>
                _amendmentService.CreateAndSubmitAsync("EmployeeAsset", 1, meId, "{}", meId));

            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Amendment_Should_Be_Off_While_Workflow_Is_Off_For_The_Type()
    {
        // Nothing to route the change through, and the ordinary edit screen is the right place to change it.
        // The catalog seeds every type dormant, so this is the state on a fresh install.
        CreateCurrentUserEmployee("AMD270");

        using (var uow = _unitOfWorkManager.Begin())
        {
            (await _amendmentService.IsAmendmentEnabledAsync("EmployeeAbsence")).ShouldBeFalse();

            await uow.CompleteAsync();
        }
    }

    private double QuotaUsedOf(int employeeId, int absenceTypeId) =>
        UsingDbContext(c => c.Set<EmployeeAbsenceQuota>()
            .Single(q => q.EmployeeId == employeeId && q.AbsenceTypeId == absenceTypeId).QuotaUsed);

    [Fact]
    public async Task Approving_Leave_Should_Take_The_Days_Out_Of_The_Quota()
    {
        // The moment leave stops being a claim on the balance and becomes a deduction from it.
        var meId = CreateCurrentUserEmployee("QTA100");
        var approverId = EnableLeaveWorkflowWithApprover("QTA100APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        QuotaUsedOf(meId, typeId).ShouldBe(0);

        await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 5, approverId);

        QuotaUsedOf(meId, typeId).ShouldBe(5);
    }

    [Fact]
    public async Task Leave_Awaiting_Approval_Should_Not_Touch_The_Quota()
    {
        // Only bookable moves while a request is pending. Deducting on submit would take days from somebody
        // whose request is then rejected.
        var meId = CreateCurrentUserEmployee("QTA110");
        EnableLeaveWorkflowWithApprover("QTA110APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        await _myLeaveAppService.ApplyForLeave(new ApplyForLeaveInput
        {
            AbsenceTypeId = typeId,
            StartDate = DateTime.Today.AddDays(30),
            EndDate = DateTime.Today.AddDays(34),
            Comments = "pending"
        });

        QuotaUsedOf(meId, typeId).ShouldBe(0);
    }

    [Fact]
    public async Task Shortening_An_Approved_Leave_Should_Give_The_Difference_Back()
    {
        var meId = CreateCurrentUserEmployee("QTA120");
        var approverId = EnableLeaveWorkflowWithApprover("QTA120APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        QuotaUsedOf(meId, typeId).ShouldBe(5);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start, days: 3));
        await _approvalService.ApproveAsync(requestId, approverId, "ok");

        // Three, not eight. The original five are released before the new figure is taken.
        QuotaUsedOf(meId, typeId).ShouldBe(3);
    }

    [Fact]
    public async Task Extending_An_Approved_Leave_Should_Take_The_Difference()
    {
        var meId = CreateCurrentUserEmployee("QTA130");
        var approverId = EnableLeaveWorkflowWithApprover("QTA130APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 3, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start, days: 7));
        await _approvalService.ApproveAsync(requestId, approverId, "ok");

        QuotaUsedOf(meId, typeId).ShouldBe(7);
    }

    [Fact]
    public async Task Changing_The_Absence_Type_Should_Move_The_Days_Between_Quotas()
    {
        // A single delta cannot express this, which is why the amendment releases and re-consumes rather
        // than adding a difference: the days leave one quota row and arrive in another.
        var meId = CreateCurrentUserEmployee("QTA140");
        var approverId = EnableLeaveWorkflowWithApprover("QTA140APP");
        var annualId = CreateAbsenceType("Annual");
        var sickId = CreateAbsenceType("Sick");
        CreateQuota(meId, annualId, accrued: 30);
        CreateQuota(meId, sickId, accrued: 15);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(annualId, start, days: 4, approverId);

        QuotaUsedOf(meId, annualId).ShouldBe(4);
        QuotaUsedOf(meId, sickId).ShouldBe(0);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, sickId, start, days: 4));
        await _approvalService.ApproveAsync(requestId, approverId, "ok");

        QuotaUsedOf(meId, annualId).ShouldBe(0);
        QuotaUsedOf(meId, sickId).ShouldBe(4);
    }

    [Fact]
    public async Task A_Rejected_Amendment_Should_Leave_The_Quota_As_It_Was()
    {
        // The same rule as the leave row itself: declining a change must not alter what was already granted.
        var meId = CreateCurrentUserEmployee("QTA150");
        var approverId = EnableLeaveWorkflowWithApprover("QTA150APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var requestId = await _myLeaveAppService.AmendMyLeave(Amend(leaveId, typeId, start, days: 2));
        await _approvalService.RejectAsync(requestId, approverId, "no");

        QuotaUsedOf(meId, typeId).ShouldBe(5);
    }

    [Fact]
    public async Task Approving_Leave_With_No_Quota_Row_Should_Not_Fail()
    {
        // Unpaid and compassionate types are deliberately not quota-controlled. Inventing a row to deduct
        // from would create an entitlement HR never granted.
        var meId = CreateCurrentUserEmployee("QTA160");
        var approverId = EnableLeaveWorkflowWithApprover("QTA160APP");
        var typeId = CreateAbsenceType("Unpaid");

        var leaveId = await CreateApprovedLeaveAsync(typeId, DateTime.Today.AddDays(30), days: 3, approverId);

        LeaveRow(leaveId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }
}
