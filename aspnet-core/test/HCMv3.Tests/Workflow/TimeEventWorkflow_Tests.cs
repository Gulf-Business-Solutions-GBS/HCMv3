using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers the missing-punch request -- the port of legacy <c>SkipPunchRequest</c> -- across its handler and
/// its self-service service.
///
/// <para>
/// The single most important thing asserted here is that a requested punch is invisible until it is granted.
/// A <see cref="TimeEvent"/> is not an ordinary record: the Time Engine loads punches for a date range and
/// recomputes attendance from them, and attendance is what payroll pays. If an un-approved punch reached
/// that query, an employee could give themselves an arrival at the start of the shift by filling in a form,
/// and nothing would look wrong anywhere -- the day would simply come out complete.
/// </para>
///
/// <para>
/// The second is the duplicate check, and specifically that it sees PENDING punches. The filter that makes
/// the first guarantee work is the same filter that would hide an in-flight request from the duplicate
/// check, so the two properties pull against each other. Both are asserted, and the pending case explicitly.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEventWorkflow_Tests : AppTestBase
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IMyPunchAppService _punches;

    public TimeEventWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
        _punches = Resolve<IMyPunchAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- seeding

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

    /// <summary>
    /// A punch type. The code is what decides In vs Out -- "1" is the type the Time Engine resolves as
    /// clock-in (<see cref="TransTypeCodes.ClockIn"/>) -- so the tests use the same codes the engine does
    /// rather than relying on the name.
    /// </summary>
    private int CreateTransType(string code, string nameEn)
    {
        TransType transType = null;
        UsingDbContext(context =>
        {
            transType = context.Set<TransType>().Add(new TransType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = nameEn,
                NameAr = nameEn
            }).Entity;
        });

        return transType.Id;
    }

    private int CreateDuty(TimeSpan startTime, TimeSpan endTime)
    {
        Duty duty = null;
        UsingDbContext(context =>
        {
            duty = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Day shift",
                NameAr = "Day shift",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                StartTime = startTime,
                EndTime = endTime
            }).Entity;
        });

        return duty.Id;
    }

    /// <summary>Same duty on every day of the week, so a test never has to care which weekday its date falls on.</summary>
    private void GiveEmployeeADuty(int employeeId, int dutyId)
    {
        DutyPeriod dutyPeriod = null;
        UsingDbContext(context =>
        {
            dutyPeriod = context.Set<DutyPeriod>().Add(new DutyPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Standard week",
                NameAr = "Standard week",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                SundayDutyId = dutyId,
                MondayDutyId = dutyId,
                TuesdayDutyId = dutyId,
                WednesdayDutyId = dutyId,
                ThursdayDutyId = dutyId,
                FridayDutyId = dutyId,
                SaturdayDutyId = dutyId
            }).Entity;
        });

        UsingDbContext(context =>
        {
            context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DutyPeriodId = dutyPeriod.Id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31)
            });
        });
    }

    /// <summary>Plants a punch directly, outside any unit of work, so any workflow status can be seeded.</summary>
    private int CreatePunch(int employeeId, int transTypeId, DateTime transDate, DateTime transTime, RequestStatus? status)
    {
        TimeEvent timeEvent = null;
        UsingDbContext(context =>
        {
            timeEvent = context.Set<TimeEvent>().Add(new TimeEvent
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TransTypeId = transTypeId,
                TransDate = transDate,
                TransTime = transTime,
                TransSource = TransSource.ESS,
                WorkflowStatus = status
            }).Entity;
        });

        return timeEvent.Id;
    }

    private WorkflowTransactionContext CreateContext(int employeeId, int? targetId)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = TimeEventWorkflowTransactionHandler.TimeEventRequestType,
                TargetEntityId = targetId,
                RequestNumber = "PCH-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = RequestStatus.Draft
            }).Entity;
        });

        return new WorkflowTransactionContext(request, employeeId);
    }

    /// <summary>Turns workflow on for the missing-punch type, pointing it at a one-step chain.</summary>
    private void EnablePunchWorkflow(string approverCode)
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
                Code = "PCH-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
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
                .First(t => t.Code == TimeEventWorkflowTransactionHandler.TimeEventRequestType);
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });
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

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    // ---------------------------------------------------------------- the catalog

    [Fact]
    public void The_Missing_Punch_Type_Should_Be_Registered_And_Dormant()
    {
        // Registered so it can be turned on; dormant so that deploying this does not start routing punches
        // through approval for tenants who never asked for it.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>()
            .Single(t => t.Code == TimeEventWorkflowTransactionHandler.TimeEventRequestType));

        type.IsWorkflowEnabled.ShouldBeFalse();
        type.IsEssEnabled.ShouldBeFalse();

        // Routed on the punch type, so a missing arrival can need a different approver from a departure.
        type.SubTypeSource.ShouldBe("TransType");
    }

    // ---------------------------------------------------------------- the duplicate check

    [Theory]
    [InlineData(null)]                      // a real punch off a device, or entered by HR
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Closed)]
    [InlineData(RequestStatus.Open)]        // the one the ApprovedOnly filter would hide
    public async Task The_Same_Punch_Should_Not_Be_Requested_Twice_For_A_Day(RequestStatus? existingStatus)
    {
        var handler = Resolve<TimeEventWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("PCH1" + existingStatus);
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        GiveEmployeeADuty(employeeId, CreateDuty(new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)));

        var day = new DateTime(2026, 3, 12);
        CreatePunch(employeeId, clockIn, day, day.AddHours(8), existingStatus);

        var second = CreatePunch(employeeId, clockIn, day, default, RequestStatus.Draft);
        var context = CreateContext(employeeId, second);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Theory]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Draft)]
    public async Task An_Abandoned_Punch_Request_Should_Not_Block_A_New_One(RequestStatus? existingStatus)
    {
        // Neither a refused nor an unsubmitted request is a punch, so neither is a duplicate of one.
        var handler = Resolve<TimeEventWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("PCH2" + existingStatus);
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        GiveEmployeeADuty(employeeId, CreateDuty(new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)));

        var day = new DateTime(2026, 3, 12);
        CreatePunch(employeeId, clockIn, day, day.AddHours(8), existingStatus);

        var second = CreatePunch(employeeId, clockIn, day, default, RequestStatus.Draft);
        var context = CreateContext(employeeId, second);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task An_Out_Punch_Should_Not_Be_Blocked_By_An_In_Punch_On_The_Same_Day()
    {
        // The whole point of a missing-punch request is usually that the other half of the day IS recorded.
        var handler = Resolve<TimeEventWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("PCH3");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        var clockOut = CreateTransType(TransTypeCodes.ClockOut, "Clock Out");
        GiveEmployeeADuty(employeeId, CreateDuty(new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)));

        var day = new DateTime(2026, 3, 12);
        CreatePunch(employeeId, clockIn, day, day.AddHours(8), null);

        var missingOut = CreatePunch(employeeId, clockOut, day, default, RequestStatus.Draft);
        var context = CreateContext(employeeId, missingOut);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task A_Punch_Should_Be_Refused_When_No_Duty_Can_Be_Resolved_For_The_Day()
    {
        // No duty period means the employee was not due to work at all, so there is no shift for the punch
        // to belong to and nothing to derive a time from. Legacy only discovered this at approval time.
        var handler = Resolve<TimeEventWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("PCH4");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");

        var punchId = CreatePunch(employeeId, clockIn, new DateTime(2026, 3, 12), default, RequestStatus.Draft);
        var context = CreateContext(employeeId, punchId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    // ---------------------------------------------------------------- deriving the time

    [Fact]
    public async Task Approving_An_In_Punch_With_No_Time_Should_Stamp_The_Start_Of_The_Duty()
    {
        var handler = Resolve<TimeEventWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("PCH5");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        GiveEmployeeADuty(employeeId, CreateDuty(new TimeSpan(8, 30, 0), new TimeSpan(17, 30, 0)));

        var day = new DateTime(2026, 3, 12);
        var punchId = CreatePunch(employeeId, clockIn, day, default, RequestStatus.Open);
        var context = CreateContext(employeeId, punchId);

        await InUnitOfWorkAsync(() => handler.OnApprovedAsync(context));

        var stored = UsingDbContext(c => c.Set<TimeEvent>().IgnoreQueryFilters().Single(e => e.Id == punchId));

        stored.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        stored.TransTime.ShouldBe(day.Add(new TimeSpan(8, 30, 0)));

        // System, not ESS: this time was worked out from the roster, not observed. An auditor asking why the
        // punch is exactly on the shift boundary is answered by this column.
        stored.TransSource.ShouldBe(TransSource.System);
    }

    [Fact]
    public async Task Approving_An_Out_Punch_With_No_Time_Should_Stamp_The_End_Of_The_Duty()
    {
        // In vs Out is decided by the TransType's code -- "2" is what the Time Engine resolves as clock-out
        // -- not by the punch's name or by which end of the day the request was raised on.
        var handler = Resolve<TimeEventWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("PCH6");
        var clockOut = CreateTransType(TransTypeCodes.ClockOut, "Clock Out");
        GiveEmployeeADuty(employeeId, CreateDuty(new TimeSpan(8, 30, 0), new TimeSpan(17, 30, 0)));

        var day = new DateTime(2026, 3, 12);
        var punchId = CreatePunch(employeeId, clockOut, day, default, RequestStatus.Open);
        var context = CreateContext(employeeId, punchId);

        await InUnitOfWorkAsync(() => handler.OnApprovedAsync(context));

        var stored = UsingDbContext(c => c.Set<TimeEvent>().IgnoreQueryFilters().Single(e => e.Id == punchId));

        stored.TransTime.ShouldBe(day.Add(new TimeSpan(17, 30, 0)));
    }

    [Fact]
    public async Task Approving_A_Punch_The_Employee_Timed_Should_Leave_Their_Time_Alone()
    {
        // If they said 08:47, they said 08:47. Overwriting it with the shift start would quietly turn a
        // late arrival into a punctual one.
        var handler = Resolve<TimeEventWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("PCH7");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        GiveEmployeeADuty(employeeId, CreateDuty(new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)));

        var day = new DateTime(2026, 3, 12);
        var stated = day.Add(new TimeSpan(8, 47, 0));
        var punchId = CreatePunch(employeeId, clockIn, day, stated, RequestStatus.Open);
        var context = CreateContext(employeeId, punchId);

        await InUnitOfWorkAsync(() => handler.OnApprovedAsync(context));

        var stored = UsingDbContext(c => c.Set<TimeEvent>().IgnoreQueryFilters().Single(e => e.Id == punchId));

        stored.TransTime.ShouldBe(stated);
        stored.TransSource.ShouldBe(TransSource.ESS);
    }

    // ---------------------------------------------------------------- the summary

    [Fact]
    public async Task The_Summary_Should_Say_Which_Punch_Is_Missing_And_When()
    {
        // The approver inbox lists every request type in one query, so this string is all an approver sees
        // before opening it.
        var handler = Resolve<TimeEventWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("PCH8");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");

        var punchId = CreatePunch(employeeId, clockIn, new DateTime(2026, 3, 12), default, RequestStatus.Draft);
        var context = CreateContext(employeeId, punchId);

        var summary = await InUnitOfWorkAsync(() => handler.BuildSummaryAsync(context));

        summary.SummaryEn.ShouldBe("Missing In-punch, 12 Mar 2026");
        summary.SummaryAr.ShouldNotBeNullOrWhiteSpace();
    }

    // ---------------------------------------------------------------- self service

    [Fact]
    public async Task The_Service_Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked to an employee, so this is the ordinary state of the system, not
        // an edge case -- and a punch attributed to nobody would be worse than an error.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _punches.GetMyPunches(ListInput()));

        await Assert.ThrowsAsync<UserFriendlyException>(() => _punches.RequestMissingPunch(
            new RequestMissingPunchInput
            {
                TransTypeId = CreateTransType(TransTypeCodes.ClockIn, "Clock In"),
                TransDate = DateTime.Today.AddDays(-1)
            }));
    }

    [Fact]
    public async Task A_Requested_Punch_Should_Be_Invisible_To_The_Time_Engine_Until_It_Is_Granted()
    {
        // THE test. The engine loads punches through IRepository<TimeEvent> inside a unit of work with no
        // filter opt-out, so if ApprovedOnly does not bite here, a form submission becomes an attendance
        // day and payroll pays it.
        var meId = CreateCurrentUserEmployee("PCH9");
        EnablePunchWorkflow("PCH9APP");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        GiveEmployeeADuty(meId, CreateDuty(new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)));

        var result = await _punches.RequestMissingPunch(new RequestMissingPunchInput
        {
            TransTypeId = clockIn,
            TransDate = new DateTime(2026, 3, 12),
            TransTime = new DateTime(2026, 3, 12, 8, 5, 0)
        });

        result.SubmittedForApproval.ShouldBeTrue();

        var repository = Resolve<IRepository<TimeEvent>>();

        using (_unitOfWorkManager.Begin())
        {
            var visibleToTheEngine = repository.GetAll().Any(e => e.Id == result.TransactionId);
            visibleToTheEngine.ShouldBeFalse("A punch nobody has approved was visible to the Time Engine.");
        }

        // The employee still sees their own request, which is the whole reason the row lives in the real
        // table instead of a shadow one.
        var mine = await _punches.GetMyPunches(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(result.TransactionId);
    }

    [Fact]
    public async Task A_Requested_Punch_Should_Be_Attributed_To_The_Signed_In_Employee()
    {
        // There is no employee field on the input at all: a self-service form must not be able to punch for
        // somebody else.
        var meId = CreateCurrentUserEmployee("PCH10");
        CreateEmployee("PCH10OTHER");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        GiveEmployeeADuty(meId, CreateDuty(new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)));

        var result = await _punches.RequestMissingPunch(new RequestMissingPunchInput
        {
            TransTypeId = clockIn,
            TransDate = new DateTime(2026, 3, 12),
            TransTime = new DateTime(2026, 3, 12, 8, 5, 0)
        });

        var row = UsingDbContext(c => c.Set<TimeEvent>().IgnoreQueryFilters().Single(e => e.Id == result.TransactionId));

        row.EmployeeId.ShouldBe(meId);
        row.TransSource.ShouldBe(TransSource.ESS);
    }

    [Fact]
    public async Task A_Punch_Cannot_Be_Requested_For_A_Day_That_Has_Not_Happened()
    {
        var meId = CreateCurrentUserEmployee("PCH11");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        GiveEmployeeADuty(meId, CreateDuty(new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)));

        await Assert.ThrowsAsync<UserFriendlyException>(() => _punches.RequestMissingPunch(
            new RequestMissingPunchInput
            {
                TransTypeId = clockIn,
                TransDate = DateTime.Today.AddDays(1)
            }));
    }

    [Fact]
    public async Task With_Workflow_Off_A_Punch_With_No_Time_Should_Still_Get_One_From_The_Duty()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install: the row is
        // effective at once and no handler ever runs against it. Without this the blank time would be saved
        // as midnight and the engine would read a five-hour-early arrival.
        var meId = CreateCurrentUserEmployee("PCH12");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        GiveEmployeeADuty(meId, CreateDuty(new TimeSpan(9, 15, 0), new TimeSpan(18, 15, 0)));

        var day = new DateTime(2026, 3, 12);

        var result = await _punches.RequestMissingPunch(new RequestMissingPunchInput
        {
            TransTypeId = clockIn,
            TransDate = day
        });

        result.SubmittedForApproval.ShouldBeFalse();

        var row = UsingDbContext(c => c.Set<TimeEvent>().IgnoreQueryFilters().Single(e => e.Id == result.TransactionId));

        row.TransTime.ShouldBe(day.Add(new TimeSpan(9, 15, 0)));
        row.WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task An_Employee_Should_Be_Able_To_Withdraw_A_Punch_Request()
    {
        var meId = CreateCurrentUserEmployee("PCH13");
        EnablePunchWorkflow("PCH13APP");
        var clockIn = CreateTransType(TransTypeCodes.ClockIn, "Clock In");
        GiveEmployeeADuty(meId, CreateDuty(new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)));

        var result = await _punches.RequestMissingPunch(new RequestMissingPunchInput
        {
            TransTypeId = clockIn,
            TransDate = new DateTime(2026, 3, 12),
            TransTime = new DateTime(2026, 3, 12, 8, 5, 0)
        });

        await _punches.CancelMyPunch(new Abp.Application.Services.Dto.EntityDto(result.TransactionId));

        var row = UsingDbContext(c => c.Set<TimeEvent>().IgnoreQueryFilters()
            .Single(e => e.Id == result.TransactionId));

        row.IsDeleted.ShouldBeTrue();
    }
}
