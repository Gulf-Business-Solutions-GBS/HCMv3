using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using HCMv3.TimeManagement.Services;
using HCMv3.TimeManagement.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers the shift override -- the port of legacy <c>RosterPlannerRequest</c> -- across its handler, its
/// admin service and the roster read path.
///
/// <para>
/// The single most important thing asserted here is that a PENDING override changes nobody's roster. An
/// <see cref="EmployeeSubstituteDuty"/> is not an ordinary record: it wins outright over the employee's duty
/// period and their roster group, so an un-approved one that leaked into
/// <see cref="IRosterService.GetRosterDutyAsync"/> would silently rewrite what the employee was due to work
/// that day -- and with it their late, early, overtime and absence figures, which payroll pays.
/// </para>
///
/// <para>
/// The second is the duplicate check seeing PENDING overrides. The filter that makes the first guarantee
/// work is the same filter that would hide an in-flight override from the duplicate check, so the two pull
/// against each other; both are asserted, and the pending case explicitly.
/// </para>
///
/// <para>
/// The third is removal. There is no delete request and no pending-removal state in the engine: taking a
/// shift away is an amendment to an off-duty Duty, approved like any other change, which is proved end to
/// end here.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class SubstituteDutyWorkflow_Tests : AppTestBase
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IRosterService _rosterService;
    private readonly IWorkflowAmendmentService _amendmentService;
    private readonly IWorkflowApprovalService _approvalService;
    private readonly IEmployeeSubstituteDutiesAppService _substituteDuties;

    public SubstituteDutyWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
        _rosterService = Resolve<IRosterService>();
        _amendmentService = Resolve<IWorkflowAmendmentService>();
        _approvalService = Resolve<IWorkflowApprovalService>();
        _substituteDuties = Resolve<IEmployeeSubstituteDutiesAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static readonly DateTime TheDay = new DateTime(2026, 3, 14);

    // ---------------------------------------------------------------- seeding

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

    private int CreateDuty(string nameEn, string nameAr = null, bool isWeeklyOff = false, bool isRestDay = false)
    {
        Duty duty = null;
        UsingDbContext(context =>
        {
            duty = context.Set<Duty>().Add(new Duty
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameAr ?? nameEn,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2030, 12, 31),
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                IsWeeklyOff = isWeeklyOff,
                IsRestDay = isRestDay
            }).Entity;
        });

        return duty.Id;
    }

    /// <summary>Same duty every day of the week, so the roster resolves without caring which weekday it is.</summary>
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

    /// <summary>Plants an override directly, outside any unit of work, so any workflow status can be seeded.</summary>
    private int CreateOverride(int employeeId, int dutyId, DateTime date, RequestStatus? status)
    {
        EmployeeSubstituteDuty entity = null;
        UsingDbContext(context =>
        {
            entity = context.Set<EmployeeSubstituteDuty>().Add(new EmployeeSubstituteDuty
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DutyId = dutyId,
                SubstituteDate = date,
                WorkflowStatus = status
            }).Entity;
        });

        return entity.Id;
    }

    private WorkflowTransactionContext CreateContext(
        int employeeId,
        int? targetId,
        SubstituteDutyAmendment amendment = null)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = SubstituteDutyWorkflowTransactionHandler.SubstituteDutyRequestType,
                TargetEntityId = targetId,
                RequestNumber = "SFT-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = RequestStatus.Draft,
                IsAmendment = amendment != null,
                AmendmentJson = amendment == null ? null : JsonSerializer.Serialize(amendment)
            }).Entity;
        });

        return new WorkflowTransactionContext(request, employeeId);
    }

    /// <summary>Turns workflow on for the shift-override type, pointing it at a one-step chain.</summary>
    private int EnableOverrideWorkflow(string approverCode)
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
                Code = "SFT-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
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
                .First(t => t.Code == SubstituteDutyWorkflowTransactionHandler.SubstituteDutyRequestType);
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
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

    private EmployeeSubstituteDuty OverrideRow(int id) =>
        UsingDbContext(c => c.Set<EmployeeSubstituteDuty>().IgnoreQueryFilters().Single(e => e.Id == id));

    private WorkflowRequest RequestRow(int id) =>
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == id));

    private WorkflowRequest RequestForOverride(int overrideId) =>
        UsingDbContext(c => c.Set<WorkflowRequest>()
            .Where(r => r.TargetEntityId == overrideId && !r.IsAmendment)
            .OrderByDescending(r => r.Id)
            .First());

    /// <summary>The duty the roster says the employee works that day, read exactly as every caller reads it.</summary>
    private async Task<Duty> RosterDutyAsync(int employeeId, DateTime day) =>
        await InUnitOfWorkAsync(() => _rosterService.GetRosterDutyAsync(employeeId, day, throwException: false));

    // ---------------------------------------------------------------- the catalog

    [Fact]
    public void The_Shift_Override_Type_Should_Be_Registered_Dormant_And_Not_Self_Service()
    {
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>()
            .Single(t => t.Code == SubstituteDutyWorkflowTransactionHandler.SubstituteDutyRequestType));

        // Registered so it can be turned on; dormant so that deploying this does not start routing shift
        // planning through approval for tenants who never asked for it.
        type.IsWorkflowEnabled.ShouldBeFalse();

        // NOT self-service, and this is the assertion that says so. A manager plans who works which shift;
        // an employee does not raise a request about somebody else's roster. Same shape as EmployeeAction.
        type.IsEssEnabled.ShouldBeFalse();
        type.EssRoute.ShouldBeNull();
        type.EssPermissionName.ShouldBeNull();

        // Routed on the Duty, so moving somebody onto nights can need a longer chain than swapping one day
        // shift for another.
        type.SubTypeSource.ShouldBe("Duty");
    }

    // ---------------------------------------------------------------- the duplicate check

    [Theory]
    [InlineData(null)]                      // entered directly by a planner, before the type was routed
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Closed)]
    [InlineData(RequestStatus.Open)]        // the one the ApprovedOnly filter would hide
    public async Task A_Second_Override_Should_Be_Refused_For_The_Same_Employee_And_Date(RequestStatus? existingStatus)
    {
        // The Open case is the hole being closed. The app service's own check runs under ApprovedOnly, so
        // before this handler existed an in-flight override was invisible to it and the same date could be
        // planned over and over -- with two of them approved, which Duty the employee works comes down to
        // whichever row the roster query returns first.
        var handler = Resolve<SubstituteDutyWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("SFT1" + existingStatus);
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");

        CreateOverride(employeeId, dayDuty, TheDay, existingStatus);

        var second = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Draft);
        var context = CreateContext(employeeId, second);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Theory]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Draft)]
    public async Task An_Abandoned_Override_Should_Not_Block_A_New_One(RequestStatus? existingStatus)
    {
        // Neither a refused nor an unsubmitted request is a shift, so neither is a duplicate of one.
        var handler = Resolve<SubstituteDutyWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("SFT2" + existingStatus);
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");

        CreateOverride(employeeId, dayDuty, TheDay, existingStatus);

        var second = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Draft);
        var context = CreateContext(employeeId, second);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task Another_Employees_Override_Should_Not_Block_The_Same_Date()
    {
        var handler = Resolve<SubstituteDutyWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("SFT3");
        var otherId = CreateEmployee("SFT3OTHER");
        var nightDuty = CreateDuty("Night Duty");

        CreateOverride(otherId, nightDuty, TheDay, RequestStatus.Approved);

        var mine = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Draft);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(CreateContext(employeeId, mine)));
    }

    [Fact]
    public async Task An_Amendment_Should_Be_Checked_Against_The_Date_It_Proposes()
    {
        // The row still holds the OLD date while the amendment is in flight, so validating the row would
        // check a date nobody is asking for -- and would pass an amendment that collides with an override
        // that already exists on the day it is moving to.
        var handler = Resolve<SubstituteDutyWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("SFT4");
        var nightDuty = CreateDuty("Night Duty");

        var liveId = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Approved);
        CreateOverride(employeeId, nightDuty, TheDay.AddDays(1), RequestStatus.Approved);

        var context = CreateContext(employeeId, liveId, new SubstituteDutyAmendment
        {
            DutyId = nightDuty,
            SubstituteDate = TheDay.AddDays(1)
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    // ---------------------------------------------------------------- the roster read path

    [Fact]
    public async Task A_Pending_Override_Should_Not_Change_The_Calculated_Roster()
    {
        // THE test. RosterService reads this table through an ordinary IRepository<EmployeeSubstituteDuty>
        // with no filter opt-out, and a substitute duty wins outright over the employee's duty period. If
        // ApprovedOnly did not bite here, filling in a form would change what the employee was due to work
        // -- and therefore their late, early, overtime and absence figures for the day.
        var employeeId = CreateEmployee("SFT10");
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");
        GiveEmployeeADuty(employeeId, dayDuty);

        CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Open);

        var duty = await RosterDutyAsync(employeeId, TheDay);

        duty.ShouldNotBeNull();
        duty.Id.ShouldBe(dayDuty, "An override nobody has approved changed the calculated roster.");
    }

    [Fact]
    public async Task An_Approved_Override_Should_Change_The_Calculated_Roster()
    {
        // The other half: the filter must hide the pending one without hiding the granted one, or approval
        // would buy nothing at all.
        var employeeId = CreateEmployee("SFT11");
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");
        GiveEmployeeADuty(employeeId, dayDuty);

        CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Approved);

        var duty = await RosterDutyAsync(employeeId, TheDay);

        duty.Id.ShouldBe(nightDuty);
    }

    [Fact]
    public async Task An_Override_Entered_Before_Workflow_Existed_Should_Still_Change_The_Roster()
    {
        // A null status means "never routed", and every row that exists today has one. Backward
        // compatibility is not a nicety here: it is what stops this change rewriting historic rosters.
        var employeeId = CreateEmployee("SFT12");
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");
        GiveEmployeeADuty(employeeId, dayDuty);

        CreateOverride(employeeId, nightDuty, TheDay, null);

        var duty = await RosterDutyAsync(employeeId, TheDay);

        duty.Id.ShouldBe(nightDuty);
    }

    // ---------------------------------------------------------------- the admin service

    [Fact]
    public async Task Planning_A_Shift_Should_Route_It_And_Keep_It_Off_The_Roster_Until_Approved()
    {
        var employeeId = CreateEmployee("SFT20");
        var approverId = EnableOverrideWorkflow("SFT20APP");
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");
        GiveEmployeeADuty(employeeId, dayDuty);

        await _substituteDuties.CreateOrEdit(new CreateOrEditEmployeeSubstituteDutyDto
        {
            EmployeeId = employeeId,
            DutyId = nightDuty,
            SubstituteDate = TheDay
        });

        var row = UsingDbContext(c => c.Set<EmployeeSubstituteDuty>().IgnoreQueryFilters()
            .Single(e => e.EmployeeId == employeeId));

        row.WorkflowStatus.ShouldBe(RequestStatus.Open);
        (await RosterDutyAsync(employeeId, TheDay)).Id.ShouldBe(dayDuty);

        // The request is raised against the EMPLOYEE the shift belongs to, not against the planner who typed
        // it -- that is the whole difference between the admin path and self-service.
        var request = RequestForOverride(row.Id);
        request.EmployeeId.ShouldBe(employeeId);
        // A number of its own, and nothing but digits: the per-type prefix went when the counter became
        // tenant-wide, so "which type is this" is no longer something the number answers.
        request.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        request.RequestNumber.ShouldNotContain("-");
        int.Parse(request.RequestNumber).ShouldBeGreaterThan(0);

        await _approvalService.ApproveAsync(request.Id, approverId, "cover agreed");

        OverrideRow(row.Id).WorkflowStatus.ShouldBe(RequestStatus.Approved);
        (await RosterDutyAsync(employeeId, TheDay)).Id.ShouldBe(nightDuty);
    }

    [Fact]
    public async Task Planning_A_Shift_Twice_For_A_Day_Should_Be_Refused_Even_While_The_First_Is_Pending()
    {
        // Through the service this time, not the handler in isolation: with the type routed, the first
        // override sits Open and invisible, which is exactly the state that used to let a second one
        // through.
        var employeeId = CreateEmployee("SFT21");
        EnableOverrideWorkflow("SFT21APP");
        var nightDuty = CreateDuty("Night Duty");
        GiveEmployeeADuty(employeeId, CreateDuty("Day Duty"));

        await _substituteDuties.CreateOrEdit(new CreateOrEditEmployeeSubstituteDutyDto
        {
            EmployeeId = employeeId,
            DutyId = nightDuty,
            SubstituteDate = TheDay
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => _substituteDuties.CreateOrEdit(
            new CreateOrEditEmployeeSubstituteDutyDto
            {
                EmployeeId = employeeId,
                DutyId = nightDuty,
                SubstituteDate = TheDay
            }));
    }

    [Fact]
    public async Task A_Planner_Should_Be_Able_To_Open_An_Override_That_Is_Still_Awaiting_Approval()
    {
        // The same filter that keeps a pending override off the roster would keep it out of the edit screen
        // too. A planner has to be able to open the thing they just raised, so the admin service opts out
        // explicitly -- one of the few places that legitimately needs to see in-flight rows.
        var employeeId = CreateEmployee("SFT22");
        EnableOverrideWorkflow("SFT22APP");
        var nightDuty = CreateDuty("Night Duty");
        GiveEmployeeADuty(employeeId, CreateDuty("Day Duty"));

        await _substituteDuties.CreateOrEdit(new CreateOrEditEmployeeSubstituteDutyDto
        {
            EmployeeId = employeeId,
            DutyId = nightDuty,
            SubstituteDate = TheDay
        });

        var row = UsingDbContext(c => c.Set<EmployeeSubstituteDuty>().IgnoreQueryFilters()
            .Single(e => e.EmployeeId == employeeId));

        var forEdit = await _substituteDuties.GetEmployeeSubstituteDutyForEdit(
            new Abp.Application.Services.Dto.EntityDto(row.Id));

        forEdit.EmployeeSubstituteDuty.DutyId.ShouldBe(nightDuty);
        forEdit.EmployeeSubstituteDuty.SubstituteDate.Date.ShouldBe(TheDay);
    }

    // ---------------------------------------------------------------- amendments

    [Fact]
    public async Task An_Amendment_Should_Not_Touch_The_Override_Until_It_Is_Approved()
    {
        var employeeId = CreateEmployee("SFT30");
        var approverId = EnableOverrideWorkflow("SFT30APP");
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");
        GiveEmployeeADuty(employeeId, dayDuty);

        var overrideId = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Approved);
        var otherDuty = CreateDuty("Evening Duty");

        await InUnitOfWorkAsync(() => _amendmentService.CreateAndSubmitAsync(
            SubstituteDutyWorkflowTransactionHandler.SubstituteDutyRequestType,
            overrideId,
            employeeId,
            JsonSerializer.Serialize(new SubstituteDutyAmendment { DutyId = otherDuty, SubstituteDate = TheDay }),
            approverId,
            otherDuty));

        var row = OverrideRow(overrideId);
        row.DutyId.ShouldBe(nightDuty, "The live override changed before anybody approved the change.");
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);

        // And the roster still says the shift that was actually granted.
        (await RosterDutyAsync(employeeId, TheDay)).Id.ShouldBe(nightDuty);
    }

    [Fact]
    public async Task Approving_An_Amendment_Should_Apply_It_And_Keep_The_Override_Approved()
    {
        var employeeId = CreateEmployee("SFT31");
        var approverId = EnableOverrideWorkflow("SFT31APP");
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");
        var eveningDuty = CreateDuty("Evening Duty");
        GiveEmployeeADuty(employeeId, dayDuty);

        var overrideId = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Approved);

        var requestId = await InUnitOfWorkAsync(() => _amendmentService.CreateAndSubmitAsync(
            SubstituteDutyWorkflowTransactionHandler.SubstituteDutyRequestType,
            overrideId,
            employeeId,
            JsonSerializer.Serialize(new SubstituteDutyAmendment { DutyId = eveningDuty, SubstituteDate = TheDay }),
            approverId,
            eveningDuty));

        await _approvalService.ApproveAsync(requestId, approverId, "fine");

        var row = OverrideRow(overrideId);
        row.DutyId.ShouldBe(eveningDuty);

        // Still approved. It never stopped being approved -- the shift must not vanish from the roster
        // while somebody thinks about a change to it.
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        (await RosterDutyAsync(employeeId, TheDay)).Id.ShouldBe(eveningDuty);
    }

    [Fact]
    public async Task Rejecting_An_Amendment_Should_Leave_The_Live_Override_Completely_Untouched()
    {
        // Refusing an edit must never destroy the thing being edited: marking the row Rejected here -- which
        // is what every non-amendment rejection does -- would take away a shift the employee is still
        // rostered for, as a side effect of a manager declining to move it.
        var employeeId = CreateEmployee("SFT32");
        var approverId = EnableOverrideWorkflow("SFT32APP");
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");
        var eveningDuty = CreateDuty("Evening Duty");
        GiveEmployeeADuty(employeeId, dayDuty);

        var overrideId = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Approved);

        var requestId = await InUnitOfWorkAsync(() => _amendmentService.CreateAndSubmitAsync(
            SubstituteDutyWorkflowTransactionHandler.SubstituteDutyRequestType,
            overrideId,
            employeeId,
            JsonSerializer.Serialize(new SubstituteDutyAmendment
            {
                DutyId = eveningDuty,
                SubstituteDate = TheDay.AddDays(3)
            }),
            approverId,
            eveningDuty));

        await _approvalService.RejectAsync(requestId, approverId, "cover already arranged");

        var row = OverrideRow(overrideId);
        row.DutyId.ShouldBe(nightDuty);
        row.SubstituteDate.Date.ShouldBe(TheDay);
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);

        // The roster is the observable version of the same guarantee.
        (await RosterDutyAsync(employeeId, TheDay)).Id.ShouldBe(nightDuty);
    }

    [Fact]
    public async Task Amending_To_An_Off_Duty_Should_Be_How_A_Shift_Is_Removed()
    {
        // The product decision, end to end. Removing an override does not delete the row: the manager amends
        // it to an off-duty Duty and that goes through approval like any other change. Two things are
        // asserted -- the employee is no longer rostered for the night shift, and the record of somebody
        // deciding to take it away still exists, which a delete would have erased.
        var employeeId = CreateEmployee("SFT33");
        var approverId = EnableOverrideWorkflow("SFT33APP");
        var dayDuty = CreateDuty("Day Duty");
        var nightDuty = CreateDuty("Night Duty");
        var offDuty = CreateDuty("Weekly Off", "راحة أسبوعية", isWeeklyOff: true);
        GiveEmployeeADuty(employeeId, dayDuty);

        var overrideId = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Approved);

        var requestId = await InUnitOfWorkAsync(() => _amendmentService.CreateAndSubmitAsync(
            SubstituteDutyWorkflowTransactionHandler.SubstituteDutyRequestType,
            overrideId,
            employeeId,
            JsonSerializer.Serialize(new SubstituteDutyAmendment { DutyId = offDuty, SubstituteDate = TheDay }),
            approverId,
            offDuty));

        // The approver is asked a question they can answer without opening anything.
        var request = RequestRow(requestId);
        request.SummaryEn.ShouldBe("Change: Night Duty to Weekly Off (off duty), 14 Mar 2026");

        await _approvalService.ApproveAsync(requestId, approverId, "shift withdrawn");

        var row = OverrideRow(overrideId);
        row.IsDeleted.ShouldBeFalse("Removal is an amendment; the row must survive it.");
        row.DutyId.ShouldBe(offDuty);
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);

        (await RosterDutyAsync(employeeId, TheDay)).Id.ShouldBe(offDuty);

        // And the decision is on file: who asked, who agreed, and what it was before.
        var snapshot = JsonSerializer.Deserialize<SubstituteDutyAmendment>(RequestRow(requestId).SnapshotJson);
        snapshot.DutyId.ShouldBe(nightDuty);
    }

    // ---------------------------------------------------------------- the inbox label

    [Fact]
    public async Task A_New_Override_Should_Read_As_A_Duty_And_A_Date()
    {
        var handler = Resolve<SubstituteDutyWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("SFT40");
        var nightDuty = CreateDuty("Night Duty", "وردية ليلية");

        var overrideId = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Draft);
        var context = CreateContext(employeeId, overrideId);

        var summary = await InUnitOfWorkAsync(() => handler.BuildSummaryAsync(context));

        summary.SummaryEn.ShouldBe("Night Duty, 14 Mar 2026");
        summary.SummaryAr.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task An_Amendment_Summary_Should_Read_As_A_Change()
    {
        // "Night Duty, 14 Mar 2026" is a different decision from "Day Duty becomes Night Duty", and an
        // approver shown only the latter half is deciding on the wrong question.
        var handler = Resolve<SubstituteDutyWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("SFT41");
        var dayDuty = CreateDuty("Day Duty", "وردية صباحية");
        var nightDuty = CreateDuty("Night Duty", "وردية ليلية");

        var overrideId = CreateOverride(employeeId, dayDuty, TheDay, RequestStatus.Approved);
        var context = CreateContext(employeeId, overrideId, new SubstituteDutyAmendment
        {
            DutyId = nightDuty,
            SubstituteDate = TheDay
        });

        var summary = await InUnitOfWorkAsync(() => handler.BuildSummaryAsync(context));

        summary.SummaryEn.ShouldBe("Change: Day Duty to Night Duty, 14 Mar 2026");
    }

    [Fact]
    public async Task An_Amendment_That_Moves_The_Day_Should_Show_Both_Dates()
    {
        var handler = Resolve<SubstituteDutyWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("SFT42");
        var nightDuty = CreateDuty("Night Duty", "وردية ليلية");

        var overrideId = CreateOverride(employeeId, nightDuty, TheDay, RequestStatus.Approved);
        var context = CreateContext(employeeId, overrideId, new SubstituteDutyAmendment
        {
            DutyId = nightDuty,
            SubstituteDate = TheDay.AddDays(1)
        });

        var summary = await InUnitOfWorkAsync(() => handler.BuildSummaryAsync(context));

        summary.SummaryEn.ShouldBe("Change: Night Duty, 14 Mar 2026 to Night Duty, 15 Mar 2026");
    }
}
