using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.UI;
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
/// Covers the leave handler -- the reference implementation of <see cref="IWorkflowTransactionHandler"/>.
///
/// Two things matter here. The status bookkeeping inherited from
/// <c>WorkflowTransactionHandlerBase</c> is what moves a leave row in and out of view for payroll and quota,
/// so each lifecycle hook is asserted against the row itself. And the overlap check has to see in-flight
/// rows, which the ApprovedOnly filter hides by default -- the case that would let an employee book the same
/// week three times.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LeaveWorkflowTransactionHandler_Tests : AppTestBase
{
    private readonly LeaveWorkflowTransactionHandler _handler;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LeaveWorkflowTransactionHandler_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _handler = Resolve<LeaveWorkflowTransactionHandler>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        // These tests describe the booking-quota mode (pending leave blocks only overlapping dates), which
        // legacy switched on with EnableBookingQuotaInLeaveRequest; see LeaveSettingsToggles_Tests for the
        // other mode.
        UsingDbContext(context =>
        {
            var settings = context.Set<HCMv3.SystemSettings.ManageTimeSettings>().FirstOrDefault()
                           ?? context.Set<HCMv3.SystemSettings.ManageTimeSettings>()
                               .Add(new HCMv3.SystemSettings.ManageTimeSettings { TenantId = AbpSession.TenantId ?? 1 }).Entity;
            settings.EnableBookingQuotaInLeaveRequest = true;
        });
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code)
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
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        // Leave days are counted against the employee's working week (legacy ActualLeaveDays).
        UsingDbContext(context => EssLeaveTestDuty.AssignAllWorkingWeek(context, TenantId, employee.Id));

        return employee.Id;
    }

    private int CreateAbsenceType(string nameEn = "Annual", string nameAr = "سنوي")
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameAr,

                // As the seeded leave types are: a bare AbsenceType is "once only, no backdating", and these
                // tests use fixed 2026 dates that are now in the past.
                CanApplyAgain = true,
                AllowDuringProbation = true,
                AllowBackDatedDays = 3650
            }).Entity;
        });

        return absenceType.Id;
    }

    private int CreateLeave(
        int employeeId,
        int absenceTypeId,
        DateTime start,
        DateTime end,
        RequestStatus? status = null,
        double totalDays = 0)
    {
        EmployeeAbsence leave = null;
        UsingDbContext(context =>
        {
            leave = context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                StartDate = start,
                EndDate = end,
                TotalLeaveDays = totalDays,
                WorkflowStatus = status
            }).Entity;
        });

        return leave.Id;
    }

    private WorkflowTransactionContext CreateContext(int employeeId, int targetLeaveId)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = LeaveWorkflowTransactionHandler.LeaveRequestType,
                TargetEntityId = targetLeaveId,
                RequestNumber = "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = RequestStatus.Draft
            }).Entity;
        });

        return new WorkflowTransactionContext(request, employeeId);
    }

    private async Task<T> InUnitOfWorkAsync<T>(Func<Task<T>> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var result = await action();
            await uow.CompleteAsync();

            return result;
        }
    }

    private async Task InUnitOfWorkAsync(Func<Task> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            await action();
            await uow.CompleteAsync();
        }
    }

    private RequestStatus? StatusOf(int leaveId)
    {
        return UsingDbContext(context => context.Set<EmployeeAbsence>()
            .IgnoreQueryFilters()
            .Single(a => a.Id == leaveId)
            .WorkflowStatus);
    }

    [Fact]
    public void Should_Declare_The_Leave_Request_Type()
    {
        _handler.RequestType.ShouldBe("EmployeeAbsence");
    }

    [Theory]
    [InlineData("Submitted", RequestStatus.Open)]
    [InlineData("Approved", RequestStatus.Approved)]
    [InlineData("Rejected", RequestStatus.Rejected)]
    [InlineData("SentBack", RequestStatus.Draft)]
    public async Task Should_Stamp_The_Leave_Row_With_The_Requests_Status(string hook, RequestStatus expected)
    {
        var employeeId = CreateEmployee("LWH" + hook);
        var leaveId = CreateLeave(employeeId, CreateAbsenceType(), new DateTime(2026, 3, 1), new DateTime(2026, 3, 5));
        var context = CreateContext(employeeId, leaveId);

        await InUnitOfWorkAsync(() => hook switch
        {
            "Submitted" => _handler.OnSubmittedAsync(context),
            "Approved" => _handler.OnApprovedAsync(context),
            "Rejected" => _handler.OnRejectedAsync(context),
            _ => _handler.OnSentBackAsync(context)
        });

        StatusOf(leaveId).ShouldBe(expected);
    }

    [Fact]
    public async Task Should_Link_The_Leave_Row_Back_To_Its_Request()
    {
        // This link is what puts a "Workflow History" button on the admin leave screen.
        var employeeId = CreateEmployee("LWHLINK");
        var leaveId = CreateLeave(employeeId, CreateAbsenceType(), new DateTime(2026, 3, 1), new DateTime(2026, 3, 5));
        var context = CreateContext(employeeId, leaveId);

        await InUnitOfWorkAsync(() => _handler.OnSubmittedAsync(context));

        var stored = UsingDbContext(context2 => context2.Set<EmployeeAbsence>()
            .IgnoreQueryFilters()
            .Single(a => a.Id == leaveId));

        stored.WorkflowRequestId.ShouldBe(context.Request.Id);
    }

    [Fact]
    public async Task Approving_Should_Make_The_Leave_Visible_To_Ordinary_Queries_Again()
    {
        // The whole point of the design, end to end: hidden while pending, visible once approved.
        var employeeId = CreateEmployee("LWHVISIBLE");
        var leaveId = CreateLeave(employeeId, CreateAbsenceType(), new DateTime(2026, 4, 1), new DateTime(2026, 4, 3));
        var context = CreateContext(employeeId, leaveId);

        await InUnitOfWorkAsync(() => _handler.OnSubmittedAsync(context));

        var visibleWhilePending = await InUnitOfWorkAsync(() =>
            Resolve<Abp.Domain.Repositories.IRepository<EmployeeAbsence>>().GetAll().AnyAsync(a => a.Id == leaveId));

        await InUnitOfWorkAsync(() => _handler.OnApprovedAsync(context));

        var visibleAfterApproval = await InUnitOfWorkAsync(() =>
            Resolve<Abp.Domain.Repositories.IRepository<EmployeeAbsence>>().GetAll().AnyAsync(a => a.Id == leaveId));

        visibleWhilePending.ShouldBeFalse();
        visibleAfterApproval.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Build_A_Summary_Naming_The_Leave_Type_Days_And_Dates()
    {
        var employeeId = CreateEmployee("LWHSUM");
        var absenceTypeId = CreateAbsenceType("Sick Leave", "إجازة مرضية");
        var leaveId = CreateLeave(employeeId, absenceTypeId,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 5), totalDays: 5);
        var context = CreateContext(employeeId, leaveId);

        var summary = await InUnitOfWorkAsync(() => _handler.BuildSummaryAsync(context));

        summary.ShouldNotBeNull();
        summary.SummaryEn.ShouldContain("Sick Leave");
        summary.SummaryEn.ShouldContain("5");
        summary.SummaryEn.ShouldContain("01 Jan 2026");
        summary.SummaryAr.ShouldContain("إجازة مرضية");
    }

    [Fact]
    public async Task Should_Reject_A_Leave_That_Overlaps_An_Approved_One()
    {
        var employeeId = CreateEmployee("LWHOVER1");
        var absenceTypeId = CreateAbsenceType();

        CreateLeave(employeeId, absenceTypeId, new DateTime(2026, 5, 1), new DateTime(2026, 5, 10), RequestStatus.Approved);
        var newLeaveId = CreateLeave(employeeId, absenceTypeId, new DateTime(2026, 5, 5), new DateTime(2026, 5, 8));

        var context = CreateContext(employeeId, newLeaveId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => _handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task Should_Reject_A_Leave_That_Overlaps_A_Still_Pending_One()
    {
        // The case that motivates disabling the filter for this check. A pending row is invisible to
        // ordinary queries, so without the opt-out an employee could book the same week repeatedly and
        // every submission would validate.
        var employeeId = CreateEmployee("LWHOVER2");
        var absenceTypeId = CreateAbsenceType();

        CreateLeave(employeeId, absenceTypeId, new DateTime(2026, 6, 1), new DateTime(2026, 6, 10), RequestStatus.Open);
        var newLeaveId = CreateLeave(employeeId, absenceTypeId, new DateTime(2026, 6, 5), new DateTime(2026, 6, 8));

        var context = CreateContext(employeeId, newLeaveId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => _handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task Should_Not_Be_Blocked_By_A_Rejected_Leave()
    {
        // A rejected row is not a claim on the employee's time.
        var employeeId = CreateEmployee("LWHOVER3");
        var absenceTypeId = CreateAbsenceType();

        CreateLeave(employeeId, absenceTypeId, new DateTime(2026, 7, 1), new DateTime(2026, 7, 10), RequestStatus.Rejected);
        var newLeaveId = CreateLeave(employeeId, absenceTypeId, new DateTime(2026, 7, 5), new DateTime(2026, 7, 8));

        var context = CreateContext(employeeId, newLeaveId);

        await InUnitOfWorkAsync(() => _handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task Should_Not_Be_Blocked_By_Another_Employees_Leave()
    {
        var absenceTypeId = CreateAbsenceType();
        var otherEmployeeId = CreateEmployee("LWHOTHER");
        var employeeId = CreateEmployee("LWHOVER4");

        CreateLeave(otherEmployeeId, absenceTypeId, new DateTime(2026, 8, 1), new DateTime(2026, 8, 10), RequestStatus.Approved);
        var newLeaveId = CreateLeave(employeeId, absenceTypeId, new DateTime(2026, 8, 5), new DateTime(2026, 8, 8));

        var context = CreateContext(employeeId, newLeaveId);

        await InUnitOfWorkAsync(() => _handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task Should_Allow_Adjacent_Non_Overlapping_Leave()
    {
        var employeeId = CreateEmployee("LWHADJ");
        var absenceTypeId = CreateAbsenceType();

        CreateLeave(employeeId, absenceTypeId, new DateTime(2026, 9, 1), new DateTime(2026, 9, 5), RequestStatus.Approved);
        var newLeaveId = CreateLeave(employeeId, absenceTypeId, new DateTime(2026, 9, 6), new DateTime(2026, 9, 10));

        var context = CreateContext(employeeId, newLeaveId);

        await InUnitOfWorkAsync(() => _handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task Should_Reject_An_End_Date_Before_The_Start_Date()
    {
        var employeeId = CreateEmployee("LWHDATES");
        var leaveId = CreateLeave(employeeId, CreateAbsenceType(), new DateTime(2026, 10, 10), new DateTime(2026, 10, 1));

        var context = CreateContext(employeeId, leaveId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => _handler.ValidateBeforeSubmitAsync(context)));
    }
}
