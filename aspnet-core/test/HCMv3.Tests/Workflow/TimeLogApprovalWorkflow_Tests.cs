using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using HCMv3.TimeManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Time log approval -- batch approval of calculated timesheet days (legacy <c>TimeLogRequest</c>).
///
/// <para>
/// <b>What these tests are actually for.</b> Legacy wrote the corrected working-day figures onto
/// <c>TimeRegister</c> at SUBMIT time (<c>HCM10.ESS/Repository/TimeLogRepository.cs</c> lines 301-340) and
/// left approval with nothing to do but flip a status column. An employee's corrected timesheet was therefore
/// live -- visible to payroll -- before anybody had approved it, and rejecting the request did not take it
/// back. The first test in this class asserts that defect is absent, and it is named so that the intent
/// survives anybody who later wonders why the proposed figures do not go straight onto the row.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeLogApprovalWorkflow_Tests : AppTestBase
{
    private readonly ITimeLogApprovalsAppService _timeLogApprovals;
    private readonly TimeLogApprovalWorkflowTransactionHandler _handler;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public TimeLogApprovalWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _timeLogApprovals = Resolve<ITimeLogApprovalsAppService>();
        _handler = Resolve<TimeLogApprovalWorkflowTransactionHandler>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    #region Fixtures

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
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>
    /// A day the engine has calculated as an absence with 60 deductible minutes -- exactly the kind of day
    /// that ends up on the approval grid.
    /// </summary>
    private int CreateCalculatedDay(int employeeId, DateTime dayDate)
    {
        TimeRegister row = null;
        UsingDbContext(context =>
        {
            row = context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DayDate = dayDate,
                Status = TimeRegisterStatus.Opened,

                PlannedWorkingMins = 480,
                ActualWorkingMins = 420,
                ActualAbsenceMins = 60,

                AbsentDay = 1,
                ActualWorkingDay = 0,
                DedLateInEarlyOutMins = 60,

                ApprovedAbsentDay = 1,
                ApprovedActualWorkingDay = 0,
                ApprovedDedLateInEarlyOutMins = 60
            }).Entity;
        });

        return row.Id;
    }

    /// <summary>Turns the type on for approval with a real approver holding the step's role.</summary>
    private int EnableWorkflow(string approverCode)
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
                Code = "TL-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                NameEn = "Standard",
                NameAr = "Standard",
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

            var type = context.Set<WorkflowTransactionType>()
                .First(t => t.Code == TimeLogApprovalWorkflowTransactionHandler.TimeLogApprovalRequestType);
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    /// <summary>Forgives the absence and accepts 15 deductible minutes instead of 60.</summary>
    private static SubmitTimeLogApprovalDto CorrectionFor(params int[] timeRegisterIds) =>
        new SubmitTimeLogApprovalDto
        {
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 30),
            Comments = "Badge reader was down",
            Days = timeRegisterIds.Select(id => new SubmitTimeLogApprovalDayDto
            {
                TimeRegisterId = id,
                ProposedAbsentDay = false,
                ProposedDedLateInEarlyOutMins = 15
            }).ToList()
        };

    private TimeRegister DayRow(int timeRegisterId) =>
        UsingDbContext(c => c.Set<TimeRegister>().IgnoreQueryFilters().Single(r => r.Id == timeRegisterId));

    private WorkflowRequest RequestFor(int employeeId) =>
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(
            r => r.RequestType == TimeLogApprovalWorkflowTransactionHandler.TimeLogApprovalRequestType
                 && r.EmployeeId == employeeId));

    private async Task InUnitOfWorkAsync(Func<Task> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            await action();
            await uow.CompleteAsync();
        }
    }

    #endregion

    #region The legacy defect

    [Fact]
    public async Task Legacy_Bug_Proposed_Values_Must_Not_Reach_The_TimeRegister_Before_Approval()
    {
        // THE test. Legacy wrote the corrected figures onto TimeRegister.Approved* at submit time, so a
        // corrected timesheet went live before anybody approved it. If this fails, that defect is back.
        var employeeId = CreateEmployee("TLA100");
        EnableWorkflow("TLA100APP");
        var dayId = CreateCalculatedDay(employeeId, new DateTime(2026, 6, 3));

        await _timeLogApprovals.SubmitTimeLogApproval(CorrectionFor(dayId));

        var day = DayRow(dayId);

        day.ApprovedAbsentDay.ShouldBe(1, "The proposed absent day reached the TimeRegister before approval.");
        day.ApprovedDedLateInEarlyOutMins.ShouldBe(60, "The proposed deduction reached the TimeRegister before approval.");
        day.ApprovedActualWorkingDay.ShouldBe(0, "The proposed working day reached the TimeRegister before approval.");
        day.Status.ShouldBe(TimeRegisterStatus.Opened, "The day was marked approved at submit time.");
        day.TimeLogApprovalId.ShouldBeNull("The day was linked to an approval that has not happened yet.");

        // The proposal is not lost -- it is simply somewhere it cannot do any harm.
        var detail = UsingDbContext(c => c.Set<TimeLogApprovalDetail>().Single(d => d.TimeRegisterId == dayId));
        detail.ProposedAbsentDay.ShouldBe(0);
        detail.ProposedDedLateInEarlyOutMins.ShouldBe(15);
    }

    [Fact]
    public async Task Rejecting_Should_Leave_The_Calculated_Day_Exactly_As_The_Engine_Left_It()
    {
        // The other half of the same point: in legacy the figures were already written, so a rejection could
        // not undo them.
        var employeeId = CreateEmployee("TLA110");
        var approverId = EnableWorkflow("TLA110APP");
        var dayId = CreateCalculatedDay(employeeId, new DateTime(2026, 6, 4));

        await _timeLogApprovals.SubmitTimeLogApproval(CorrectionFor(dayId));

        await Resolve<IWorkflowApprovalService>()
            .RejectAsync(RequestFor(employeeId).Id, approverId, "No evidence");

        var day = DayRow(dayId);
        day.ApprovedAbsentDay.ShouldBe(1);
        day.ApprovedDedLateInEarlyOutMins.ShouldBe(60);
        day.Status.ShouldBe(TimeRegisterStatus.Opened);
        day.TimeLogApprovalId.ShouldBeNull();
    }

    #endregion

    #region Approval

    [Fact]
    public async Task Approving_Should_Apply_The_Proposed_Values_To_The_TimeRegister()
    {
        var employeeId = CreateEmployee("TLA200");
        var approverId = EnableWorkflow("TLA200APP");
        var dayId = CreateCalculatedDay(employeeId, new DateTime(2026, 6, 5));

        await _timeLogApprovals.SubmitTimeLogApproval(CorrectionFor(dayId));

        var requestId = RequestFor(employeeId).Id;
        await Resolve<IWorkflowApprovalService>().ApproveAsync(requestId, approverId, "Agreed");

        var day = DayRow(dayId);
        day.ApprovedAbsentDay.ShouldBe(0);
        day.ApprovedDedLateInEarlyOutMins.ShouldBe(15);
        day.Status.ShouldBe(TimeRegisterStatus.Approved);

        // Stamped, so the engine can find and invalidate this approval if the day is recalculated.
        day.TimeLogApprovalId.ShouldNotBeNull();

        // The absence was forgiven, so the day is credited its FULL planned minutes: 480/480 = 1.
        day.ApprovedActualWorkingDay.ShouldBe(1);
    }

    [Fact]
    public async Task A_Batch_Spanning_Employees_Should_Become_One_Request_Per_Employee()
    {
        // IHasEmployeeId needs one non-null employee, and so does approver routing -- a batch crossing two
        // departments has no single correct approver. The grid may still span people; the submission splits.
        var firstEmployeeId = CreateEmployee("TLA210");
        var secondEmployeeId = CreateEmployee("TLA211");
        EnableWorkflow("TLA210APP");

        var firstDayId = CreateCalculatedDay(firstEmployeeId, new DateTime(2026, 6, 6));
        var secondDayId = CreateCalculatedDay(secondEmployeeId, new DateTime(2026, 6, 6));

        var result = await _timeLogApprovals.SubmitTimeLogApproval(CorrectionFor(firstDayId, secondDayId));

        result.TimeLogApprovalIds.Count.ShouldBe(2);
        result.DayCount.ShouldBe(2);

        RequestFor(firstEmployeeId).EmployeeId.ShouldBe(firstEmployeeId);
        RequestFor(secondEmployeeId).EmployeeId.ShouldBe(secondEmployeeId);
    }

    [Fact]
    public async Task The_Summary_Should_Say_Whose_Days_And_How_Many()
    {
        var employeeId = CreateEmployee("TLA220");
        EnableWorkflow("TLA220APP");

        var firstDayId = CreateCalculatedDay(employeeId, new DateTime(2026, 6, 7));
        var secondDayId = CreateCalculatedDay(employeeId, new DateTime(2026, 6, 8));

        await _timeLogApprovals.SubmitTimeLogApproval(CorrectionFor(firstDayId, secondDayId));

        var summary = RequestFor(employeeId).SummaryEn;

        summary.ShouldContain("TLA220 T");
        summary.ShouldContain("2 day(s)");
        summary.ShouldContain("01 Jun 2026");
    }

    #endregion

    #region Refusals

    [Fact]
    public async Task Should_Refuse_An_Empty_Detail_List()
    {
        // Legacy set a message string on the view model and submitted anyway, so an approver could be asked
        // to approve nothing.
        var employeeId = CreateEmployee("TLA300");
        EnableWorkflow("TLA300APP");
        var approvalId = CreateBareApproval(employeeId);

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => InUnitOfWorkAsync(() => _handler.ValidateBeforeSubmitAsync(ContextFor(employeeId, approvalId))));

        exception.Message.ShouldContain("at least one day");
    }

    [Fact]
    public async Task Should_Refuse_A_Day_That_Is_Already_In_Another_In_Flight_Batch()
    {
        // Two batches proposing different figures for the same day would both be approvable, and whichever
        // was approved second would silently overwrite the first.
        var employeeId = CreateEmployee("TLA310");
        EnableWorkflow("TLA310APP");
        var dayId = CreateCalculatedDay(employeeId, new DateTime(2026, 6, 9));

        await _timeLogApprovals.SubmitTimeLogApproval(CorrectionFor(dayId));

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _timeLogApprovals.SubmitTimeLogApproval(CorrectionFor(dayId)));

        exception.Message.ShouldContain("another request");
    }

    [Fact]
    public async Task Should_Refuse_A_Day_That_Is_Already_Approved()
    {
        // Re-approving a settled day is how an approved figure gets replaced without the history row the
        // engine's reopen path would have written. Correcting it is legitimate, but it has to go through
        // recalculation, which reopens the day.
        var employeeId = CreateEmployee("TLA320");
        EnableWorkflow("TLA320APP");
        var dayId = CreateCalculatedDay(employeeId, new DateTime(2026, 6, 10));

        UsingDbContext(c =>
        {
            var row = c.Set<TimeRegister>().Single(r => r.Id == dayId);
            row.Status = TimeRegisterStatus.Approved;
        });

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _timeLogApprovals.SubmitTimeLogApproval(CorrectionFor(dayId)));

        exception.Message.ShouldContain("already been approved");
    }

    private int CreateBareApproval(int employeeId)
    {
        TimeLogApproval approval = null;
        UsingDbContext(context =>
        {
            approval = context.Set<TimeLogApproval>().Add(new TimeLogApproval
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 6, 30),
                WorkflowStatus = RequestStatus.Draft
            }).Entity;
        });

        return approval.Id;
    }

    private WorkflowTransactionContext ContextFor(int employeeId, int approvalId)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = TimeLogApprovalWorkflowTransactionHandler.TimeLogApprovalRequestType,
                TargetEntityId = approvalId,
                RequestNumber = "TL-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = RequestStatus.Draft
            }).Entity;
        });

        return new WorkflowTransactionContext(request, employeeId);
    }

    #endregion

    #region The approved-working-day formula (legacy TimeLogRepository.cs lines 311-336)

    private static TimeRegister DayWith(
        double plannedMins,
        double actualMins,
        double actualAbsenceMins,
        double absentDay,
        double approvedAbsentDay,
        double dedMins,
        double approvedDedMins,
        double leaveDay = 0)
    {
        return new TimeRegister
        {
            PlannedWorkingMins = plannedMins,
            ActualWorkingMins = actualMins,
            ActualAbsenceMins = actualAbsenceMins,
            AbsentDay = absentDay,
            ApprovedAbsentDay = approvedAbsentDay,
            DedLateInEarlyOutMins = dedMins,
            ApprovedDedLateInEarlyOutMins = approvedDedMins,
            LeaveDay = leaveDay
        };
    }

    private static double Calculate(TimeRegister day, MonthlyToleranceType tolerance = MonthlyToleranceType.NotSelected) =>
        TimeLogApprovalWorkflowTransactionHandler.CalculateApprovedActualWorkingDay(day, tolerance);

    [Fact]
    public void Forgiving_An_Absence_Should_Credit_The_Full_Planned_Day()
    {
        // AbsentDay != ApprovedAbsentDay -> the day is credited PlannedWorkingMins, not what was actually
        // worked. Treating somebody as present means treating them as present for the whole shift.
        var day = DayWith(480, 0, 0, absentDay: 1, approvedAbsentDay: 0, dedMins: 0, approvedDedMins: 0);

        Calculate(day).ShouldBe(1);
    }

    [Fact]
    public void Reducing_The_Deduction_Should_Hand_The_Difference_Back_When_No_Monthly_Tolerance_Is_Set()
    {
        // 420 worked + (60 - 15) handed back = 465 of 480 = 0.969.
        var day = DayWith(480, 420, actualAbsenceMins: 60, absentDay: 0, approvedAbsentDay: 0, dedMins: 60, approvedDedMins: 15);

        Calculate(day, MonthlyToleranceType.NotSelected).ShouldBe(0.969);
    }

    [Theory]
    [InlineData(MonthlyToleranceType.CalcMonthlyToleranceOnActual)]
    [InlineData(MonthlyToleranceType.CalcMonthlyTolearnceOnDed)]
    public void Reducing_The_Deduction_Should_Hand_Nothing_Back_When_Monthly_Tolerance_Is_Set(MonthlyToleranceType tolerance)
    {
        // THE TOLERANCE BRANCH. Both other settings forgive minutes across the whole MONTH, so crediting them
        // here as well would forgive the same minutes twice. 420 of 480 = 0.875, unchanged.
        var day = DayWith(480, 420, actualAbsenceMins: 60, absentDay: 0, approvedAbsentDay: 0, dedMins: 60, approvedDedMins: 15);

        Calculate(day, tolerance).ShouldBe(0.875);
    }

    [Fact]
    public void Reducing_The_Deduction_On_A_Day_With_No_Absence_Minutes_Should_Hand_Nothing_Back()
    {
        // The ActualAbsenceMins != 0 guard: nothing was deducted for, so there is nothing to hand back.
        var day = DayWith(480, 420, actualAbsenceMins: 0, absentDay: 0, approvedAbsentDay: 0, dedMins: 60, approvedDedMins: 15);

        Calculate(day).ShouldBe(0.875);
    }

    [Fact]
    public void A_Day_Carrying_Leave_Should_Be_Worth_One_Minus_The_Leave()
    {
        // The leave branch wins outright -- the minutes ratio is not consulted at all.
        var day = DayWith(480, 240, actualAbsenceMins: 0, absentDay: 0, approvedAbsentDay: 0, dedMins: 0, approvedDedMins: 0, leaveDay: 0.5);

        Calculate(day).ShouldBe(0.5);
    }

    [Fact]
    public void The_Result_Should_Be_Clamped_To_One()
    {
        // Overtime must not turn one day into more than one working day for payroll.
        var day = DayWith(480, 600, actualAbsenceMins: 0, absentDay: 0, approvedAbsentDay: 0, dedMins: 0, approvedDedMins: 0);

        Calculate(day).ShouldBe(1);
    }

    [Fact]
    public void The_Result_Should_Never_Go_Negative()
    {
        // Handing back a NEGATIVE difference (approved deduction higher than calculated) can push the minutes
        // below zero; a negative working day means nothing to payroll.
        var day = DayWith(480, 10, actualAbsenceMins: 60, absentDay: 0, approvedAbsentDay: 0, dedMins: 0, approvedDedMins: 480);

        Calculate(day).ShouldBe(0);
    }

    [Fact]
    public void A_Day_With_No_Planned_Minutes_Should_Be_Worth_Nothing()
    {
        var day = DayWith(0, 420, actualAbsenceMins: 0, absentDay: 0, approvedAbsentDay: 0, dedMins: 0, approvedDedMins: 0);

        Calculate(day).ShouldBe(0);
    }

    #endregion

    #region The grid

    [Fact]
    public async Task The_Grid_Should_Return_The_Calculated_Days_For_One_Employee()
    {
        var employeeId = CreateEmployee("TLA400");
        var dayId = CreateCalculatedDay(employeeId, new DateTime(2026, 6, 11));
        CreateCalculatedDay(employeeId, new DateTime(2026, 7, 20));

        var rows = await _timeLogApprovals.GetTimeLogApprovalGrid(new GetTimeLogApprovalGridInput
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 30)
        });

        var row = rows.ShouldHaveSingleItem();
        row.Id.ShouldBe(dayId);
        row.AbsentDay.ShouldBeTrue();
        row.DedLateInEarlyOutMins.ShouldBe(60);
        row.IsLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task The_Grid_Should_Lock_A_Day_That_Is_Already_Awaiting_Approval()
    {
        // Telling somebody a day cannot go forward before they build a batch around it beats refusing the
        // whole batch at submit.
        var employeeId = CreateEmployee("TLA410");
        EnableWorkflow("TLA410APP");
        var dayId = CreateCalculatedDay(employeeId, new DateTime(2026, 6, 12));

        await _timeLogApprovals.SubmitTimeLogApproval(CorrectionFor(dayId));

        var rows = await _timeLogApprovals.GetTimeLogApprovalGrid(new GetTimeLogApprovalGridInput
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 30)
        });

        rows.ShouldHaveSingleItem().IsLocked.ShouldBeTrue();
    }

    [Fact]
    public async Task The_Grid_Should_Refuse_A_Reversed_Date_Range()
    {
        // A reversed range silently returns nothing, which reads as "this employee has no timesheet".
        var employeeId = CreateEmployee("TLA420");

        await Should.ThrowAsync<UserFriendlyException>(
            () => _timeLogApprovals.GetTimeLogApprovalGrid(new GetTimeLogApprovalGridInput
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 30),
                EndDate = new DateTime(2026, 6, 1)
            }));
    }

    #endregion

    #region Catalog

    [Fact]
    public void Time_Log_Approval_Should_Not_Be_Offered_As_Self_Service()
    {
        // A manager reviews the engine's output for their people; an employee does not approve their own
        // attendance. A route here with no ESS service behind it would be a dead tile.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>()
            .First(t => t.Code == TimeLogApprovalWorkflowTransactionHandler.TimeLogApprovalRequestType));

        type.EssRoute.ShouldBeNull();
        type.EssPermissionName.ShouldBeNull();
        type.IsEssEnabled.ShouldBeFalse();
    }

    [Fact]
    public void The_Handler_Should_Be_Resolvable_By_Its_Request_Type()
    {
        Resolve<IWorkflowTransactionHandlerResolver>()
            .Resolve(TimeLogApprovalWorkflowTransactionHandler.TimeLogApprovalRequestType)
            .ShouldBeOfType<TimeLogApprovalWorkflowTransactionHandler>();
    }

    #endregion
}
