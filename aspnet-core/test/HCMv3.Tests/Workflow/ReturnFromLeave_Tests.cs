using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers returning from leave -- which is deliberately NOT a transaction type of its own.
///
/// <para>
/// Legacy had a <c>ReturnFromLeaveRequest</c> business object, its own table and its own approval route. Two
/// of its three outcomes turn out to be an amendment to the leave (stamp the return; pull the end date in and
/// stamp the return), and the third -- coming back late -- is an ordinary application for the extra days. So
/// there is no entity here, only three more fields on the amendment payload, which is JSON on the request and
/// therefore not even a schema change.
/// </para>
///
/// <para>
/// The test that matters most in this file is the rejected one. A return is a change to a leave that is
/// already approved and already in payroll's view; declining it must leave that leave exactly as it was,
/// dates, return stamp and all. Getting that wrong would mean an employee whose early return was refused
/// nonetheless loses the days.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class ReturnFromLeave_Tests : AppTestBase
{
    private readonly IMyLeaveAppService _myLeaveAppService;
    private readonly IWorkflowApprovalService _approvalService;

    public ReturnFromLeave_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _myLeaveAppService = Resolve<IMyLeaveAppService>();
        _approvalService = Resolve<IWorkflowApprovalService>();
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

        return employee.Id;
    }

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    private int CreateAbsenceType(bool allowToReturnEarly = true, string nameEn = "Annual")
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

    private void CreateQuota(int employeeId, int absenceTypeId, double accrued)
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
                QuotaUsed = 0
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
                Code = "LEAVE-RET",
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

    [Fact]
    public async Task Returning_On_Time_Should_Stamp_The_Return_Without_Moving_Any_Dates()
    {
        // Nothing changed about the leave -- the employee took exactly what was granted. All that is being
        // recorded is that they are back.
        var meId = CreateCurrentUserEmployee("RET100");
        var approverId = EnableLeaveWorkflowWithApprover("RET100APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var before = LeaveRow(leaveId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = before.EndDate.AddDays(1)
        });

        result.IsEarlyReturn.ShouldBeFalse();
        result.DaysReturned.ShouldBe(0);

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "welcome back");

        var after = LeaveRow(leaveId);
        after.IsReturned.ShouldBeTrue();
        after.ReturnDate.Date.ShouldBe(before.EndDate.Date.AddDays(1));

        after.StartDate.ShouldBe(before.StartDate);
        after.EndDate.ShouldBe(before.EndDate);
        after.TotalLeaveDays.ShouldBe(before.TotalLeaveDays);
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Returning_Early_Should_Shorten_The_Leave_And_Keep_The_Original_End_Date()
    {
        // InitialEndDate is the point: after the change, the row's EndDate is the day they actually came back
        // to, and the only remaining record of what was granted is InitialEndDate.
        var meId = CreateCurrentUserEmployee("RET110");
        var approverId = EnableLeaveWorkflowWithApprover("RET110APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var originalEnd = start.AddDays(4);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        // Back two days early: last day of leave becomes start+2.
        var rejoiningDate = start.AddDays(3);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = rejoiningDate,
            Reason = "Work came up"
        });

        result.IsEarlyReturn.ShouldBeTrue();
        result.NewEndDate.ShouldBe(start.AddDays(2).Date);
        result.DaysReturned.ShouldBe(2);

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "fine");

        var after = LeaveRow(leaveId);
        after.EndDate.Date.ShouldBe(start.AddDays(2).Date);
        after.InitialEndDate.Date.ShouldBe(originalEnd.Date);
        after.ReturnDate.Date.ShouldBe(rejoiningDate.Date);
        after.IsReturned.ShouldBeTrue();
        after.TotalLeaveDays.ShouldBe(3);
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Returning_Early_Should_Give_The_Unused_Days_Back_To_The_Quota()
    {
        var meId = CreateCurrentUserEmployee("RET115");
        var approverId = EnableLeaveWorkflowWithApprover("RET115APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "fine");

        var quota = UsingDbContext(context => context.Set<EmployeeAbsenceQuota>()
            .IgnoreQueryFilters()
            .Single(q => q.EmployeeId == meId && q.AbsenceTypeId == typeId));

        // Five were taken out on approval of the leave; three of them were actually used.
        quota.QuotaUsed.ShouldBe(3);
    }

    [Fact]
    public async Task A_Rejected_Return_Should_Leave_The_Leave_Completely_Untouched()
    {
        // THE test. A refused early return must not cost the employee the days: the leave keeps its dates, is
        // still approved, and carries no return stamp at all -- as though the return had never been asked for.
        var meId = CreateCurrentUserEmployee("RET120");
        var approverId = EnableLeaveWorkflowWithApprover("RET120APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var before = LeaveRow(leaveId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3),
            Reason = "Back early"
        });

        await _approvalService.RejectAsync(result.WorkflowRequestId, approverId, "Finish your leave");

        var after = LeaveRow(leaveId);
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        after.StartDate.ShouldBe(before.StartDate);
        after.EndDate.ShouldBe(before.EndDate);
        after.TotalLeaveDays.ShouldBe(before.TotalLeaveDays);

        // The three fields the return would have written stay exactly as they were.
        after.IsReturned.ShouldBeFalse();
        after.ReturnDate.ShouldBe(before.ReturnDate);
        after.InitialEndDate.ShouldBe(before.InitialEndDate);
    }

    [Fact]
    public async Task A_Rejected_Return_Should_Not_Disturb_The_Quota_Either()
    {
        var meId = CreateCurrentUserEmployee("RET125");
        var approverId = EnableLeaveWorkflowWithApprover("RET125APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3)
        });

        await _approvalService.RejectAsync(result.WorkflowRequestId, approverId, "Finish your leave");

        var quota = UsingDbContext(context => context.Set<EmployeeAbsenceQuota>()
            .IgnoreQueryFilters()
            .Single(q => q.EmployeeId == meId && q.AbsenceTypeId == typeId));

        quota.QuotaUsed.ShouldBe(5);
    }

    [Fact]
    public async Task An_Early_Return_Should_Be_Refused_For_A_Leave_Type_That_Must_Be_Taken_In_Full()
    {
        // Maternity is the usual example: the entitlement is not a pot of days the employee may hand back.
        var meId = CreateCurrentUserEmployee("RET130");
        var approverId = EnableLeaveWorkflowWithApprover("RET130APP");
        var typeId = CreateAbsenceType(allowToReturnEarly: false, nameEn: "Maternity");
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(3)
            }));

        exception.Message.ShouldContain("Maternity");

        // And nothing was left half-raised against the leave.
        LeaveRow(leaveId).IsReturned.ShouldBeFalse();
    }

    [Fact]
    public async Task An_On_Time_Return_Should_Still_Be_Allowed_For_A_Type_That_Forbids_Returning_Early()
    {
        // The rule is about cutting the leave short, not about recording that the employee came back.
        var meId = CreateCurrentUserEmployee("RET140");
        var approverId = EnableLeaveWorkflowWithApprover("RET140APP");
        var typeId = CreateAbsenceType(allowToReturnEarly: false, nameEn: "Maternity");
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(5)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        LeaveRow(leaveId).IsReturned.ShouldBeTrue();
    }

    [Fact]
    public async Task Returning_Late_Should_Be_Refused_And_Say_To_Apply_For_The_Extra_Days()
    {
        // Coming back late is MORE absence than anybody approved. Absorbing it here would spend quota on the
        // employee's own authority, so it is refused and pointed at the path that asks an approver.
        var meId = CreateCurrentUserEmployee("RET150");
        var approverId = EnableLeaveWorkflowWithApprover("RET150APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var before = LeaveRow(leaveId);

        // Approved to start+4, back on start+8: three extra days of absence (start+5, +6, +7).
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(8)
            }));

        exception.Message.ShouldContain("apply for those days");
        exception.Message.ShouldContain("3 extra day(s)");

        // Refused means refused: no leave was quietly stretched to cover the gap.
        var after = LeaveRow(leaveId);
        after.EndDate.ShouldBe(before.EndDate);
        after.IsReturned.ShouldBeFalse();
    }

    [Fact]
    public async Task A_Rejoining_Date_On_Or_Before_The_Leave_Start_Should_Be_Refused()
    {
        // That is not a return, it is a leave that never happened -- cancel it instead.
        var meId = CreateCurrentUserEmployee("RET160");
        var approverId = EnableLeaveWorkflowWithApprover("RET160APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start
            }));
    }

    [Fact]
    public async Task A_Second_Return_Against_The_Same_Leave_Should_Be_Refused()
    {
        var meId = CreateCurrentUserEmployee("RET170");
        var approverId = EnableLeaveWorkflowWithApprover("RET170APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var result = await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3)
        });

        await _approvalService.ApproveAsync(result.WorkflowRequestId, approverId, "ok");

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
            {
                EmployeeAbsenceId = leaveId,
                RejoiningDate = start.AddDays(2)
            }));
    }

    [Fact]
    public async Task Raising_A_Return_Should_Not_Change_The_Leave_Until_It_Is_Approved()
    {
        // Same promise every amendment makes, restated for the return path: payroll must see what it saw
        // before, right up to the moment somebody agrees.
        var meId = CreateCurrentUserEmployee("RET180");
        var approverId = EnableLeaveWorkflowWithApprover("RET180APP");
        var typeId = CreateAbsenceType();
        CreateQuota(meId, typeId, accrued: 30);

        var start = DateTime.Today.AddDays(30);
        var leaveId = await CreateApprovedLeaveAsync(typeId, start, days: 5, approverId);

        var before = LeaveRow(leaveId);

        await _myLeaveAppService.ReturnFromMyLeave(new ReturnFromLeaveInput
        {
            EmployeeAbsenceId = leaveId,
            RejoiningDate = start.AddDays(3)
        });

        var after = LeaveRow(leaveId);
        after.EndDate.ShouldBe(before.EndDate);
        after.IsReturned.ShouldBeFalse();
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }
}
