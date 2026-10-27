using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service leave plans (item 59, legacy <c>HCM10.Module/BusinessObjects/Workflow/Leave/LeavePlanner.cs</c>
/// and ESS <c>LeavePlannerController</c> / <c>WorkflowRepository</c> lines 517-1170).
///
/// <para>
/// What is protected here: the legacy validation (dates, end not before start); the stored planned days;
/// that a pending plan is hidden from everything but its owner; that NOTHING happens on approval beyond the
/// status (legacy's approval block was commented out); that overlapping plans are accepted, as legacy accepted
/// them; and that the team scheduler shows the employee and their direct reports only.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyLeavePlanAppService_Tests : AppTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IMyLeavePlanAppService _plans;

    public MyLeavePlanAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _plans = Resolve<IMyLeavePlanAppService>();
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
                Status = EmployeeStatus.Active,
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    /// <summary>
    /// The catalog row, found or created. The seeder is the shared catalog file, so this test does not assume
    /// the row has been added there yet -- it only needs one to exist.
    /// </summary>
    private void EnsureCatalogRow(bool workflowEnabled, int? requestMasterId = null)
    {
        UsingDbContext(context =>
        {
            var type = context.Set<WorkflowTransactionType>().IgnoreQueryFilters()
                .FirstOrDefault(t => t.TenantId == TenantId && t.Code == LeavePlanWorkflowTransactionHandler.LeavePlanRequestType);

            if (type == null)
            {
                type = context.Set<WorkflowTransactionType>().Add(new WorkflowTransactionType
                {
                    TenantId = TenantId,
                    Code = LeavePlanWorkflowTransactionHandler.LeavePlanRequestType,
                    NameEn = "Leave Plan",
                    NameAr = "خطة إجازة",
                    DisplayOrder = 15
                }).Entity;
            }

            type.IsActive = true;
            type.IsWorkflowEnabled = workflowEnabled;
            type.IsEssEnabled = true;
            type.DefaultRequestMasterId = requestMasterId;
        });
    }

    /// <summary>Turns workflow on for leave plans, with a one-step chain and a real approver. Returns the approver.</summary>
    private int EnableLeavePlanWorkflow(string approverCode)
    {
        var approverId = CreateEmployee(approverCode);
        var masterId = 0;

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
                Code = "LPL-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                NameEn = "Standard",
                NameAr = "Standard",
                WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();
            masterId = master.Id;

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
        });

        EnsureCatalogRow(true, masterId);

        return approverId;
    }

    private static RequestLeavePlanInput PlanInput(DateTime start, DateTime end) => new RequestLeavePlanInput
    {
        StartDate = start,
        EndDate = end,
        Comments = "Summer"
    };

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private LeavePlan Row(int id) =>
        UsingDbContext(c => c.Set<LeavePlan>().IgnoreQueryFilters().Single(p => p.Id == id));

    // ---------------------------------------------------------------- the legacy rules

    [Fact]
    public async Task An_End_Date_Before_The_Start_Should_Be_Refused()
    {
        // Legacy LeavePlanner.PerformValidations line 223, and ESS LeavePlannerController line 148.
        CreateCurrentUserEmployee("LPL100");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _plans.RequestLeavePlan(PlanInput(DateTime.Today.AddDays(20), DateTime.Today.AddDays(19))));
    }

    [Fact]
    public async Task A_Missing_Start_Date_Should_Be_Refused()
    {
        CreateCurrentUserEmployee("LPL105");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _plans.RequestLeavePlan(PlanInput(default, DateTime.Today.AddDays(19))));
    }

    [Fact]
    public async Task The_Planned_Days_Should_Be_Stored_As_Calendar_Days()
    {
        // Legacy PlannedDays: End - Start + 1, weekends included -- the "Planned Calendar Days" box.
        CreateCurrentUserEmployee("LPL110");

        var start = DateTime.Today.AddDays(30);
        var result = await _plans.RequestLeavePlan(PlanInput(start, start.AddDays(9)));

        Row(result.TransactionId).PlannedDays.ShouldBe(10);
    }

    [Fact]
    public async Task Overlapping_Plans_Should_Both_Be_Accepted()
    {
        // Legacy has no overlap rule for plans at all -- not against other plans, not against leave. The
        // planner exists to show such overlaps, not to prevent them.
        CreateCurrentUserEmployee("LPL120");
        EnableLeavePlanWorkflow("LPL120APP");

        var start = DateTime.Today.AddDays(40);

        var first = await _plans.RequestLeavePlan(PlanInput(start, start.AddDays(5)));
        var second = await _plans.RequestLeavePlan(PlanInput(start.AddDays(2), start.AddDays(8)));

        first.SubmittedForApproval.ShouldBeTrue();
        second.SubmittedForApproval.ShouldBeTrue();
    }

    // ---------------------------------------------------------------- workflow

    [Fact]
    public async Task With_Workflow_Off_The_Plan_Should_Be_Recorded_At_Once()
    {
        CreateCurrentUserEmployee("LPL200");
        EnsureCatalogRow(false);

        var start = DateTime.Today.AddDays(10);
        var result = await _plans.RequestLeavePlan(PlanInput(start, start.AddDays(2)));

        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();
        Row(result.TransactionId).WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task A_Submitted_Plan_Should_Be_Hidden_From_Everybody_But_Its_Owner()
    {
        CreateCurrentUserEmployee("LPL210");
        EnableLeavePlanWorkflow("LPL210APP");

        var start = DateTime.Today.AddDays(12);
        var result = await _plans.RequestLeavePlan(PlanInput(start, start.AddDays(3)));

        Row(result.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Open);

        var repository = Resolve<IRepository<LeavePlan>>();
        using (Resolve<IUnitOfWorkManager>().Begin())
        {
            repository.GetAll().Any(p => p.Id == result.TransactionId)
                .ShouldBeFalse("A pending leave plan was visible to the rest of the system.");
        }

        var mine = await _plans.GetMyLeavePlans(ListInput());
        var row = mine.Items.Single(i => i.Id == result.TransactionId);
        row.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        row.CanCancel.ShouldBeTrue();
    }

    [Fact]
    public async Task Approving_A_Plan_Should_Change_Nothing_But_Its_Status()
    {
        // Legacy's RequestStatus.Approved block in LeavePlanner.OnSaving is commented out. A plan is not a
        // leave: no absence is created and no quota moves.
        var meId = CreateCurrentUserEmployee("LPL220");
        var approverId = EnableLeavePlanWorkflow("LPL220APP");

        var start = DateTime.Today.AddDays(14);
        var result = await _plans.RequestLeavePlan(PlanInput(start, start.AddDays(4)));

        await Resolve<IWorkflowApprovalService>().ApproveAsync(result.WorkflowRequestId.Value, approverId, "Fine");

        Row(result.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Approved);

        UsingDbContext(c => c.Set<EmployeeAbsence>().IgnoreQueryFilters().Count(a => a.EmployeeId == meId))
            .ShouldBe(0, "Approving a leave plan booked leave.");
        UsingDbContext(c => c.Set<EmployeeAbsenceQuota>().IgnoreQueryFilters().Count(q => q.EmployeeId == meId))
            .ShouldBe(0, "Approving a leave plan touched quota.");
    }

    [Fact]
    public void The_Leave_Plan_Type_Should_Have_A_Handler()
    {
        // Castle binds handlers by the ...WorkflowTransactionHandler suffix; anything else would route with no
        // validation at all.
        Resolve<IWorkflowTransactionHandlerResolver>()
            .Resolve(LeavePlanWorkflowTransactionHandler.LeavePlanRequestType)
            .ShouldNotBeNull();
    }

    // ---------------------------------------------------------------- drafts

    [Fact]
    public async Task A_Draft_Should_Keep_Missing_Dates_And_Be_Submitted_With_Them_Checked()
    {
        CreateCurrentUserEmployee("LPL300");
        EnableLeavePlanWorkflow("LPL300APP");

        var draft = await _plans.SaveMyLeavePlanDraft(new SaveLeavePlanDraftInput { StartDate = DateTime.Today.AddDays(50) });

        draft.SavedAsDraft.ShouldBeTrue();
        Row(draft.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Draft);
        Row(draft.TransactionId).EndDate.ShouldBe(default(DateTime));

        // Refused at submit, where the rule first matters to somebody else.
        await Should.ThrowAsync<UserFriendlyException>(() => _plans.SubmitMyLeavePlanDraft(new EntityDto(draft.TransactionId)));

        await _plans.SaveMyLeavePlanDraft(new SaveLeavePlanDraftInput
        {
            Id = draft.TransactionId,
            StartDate = DateTime.Today.AddDays(50),
            EndDate = DateTime.Today.AddDays(52)
        });

        var submitted = await _plans.SubmitMyLeavePlanDraft(new EntityDto(draft.TransactionId));

        submitted.SubmittedForApproval.ShouldBeTrue();
        Row(draft.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Open);
        Row(draft.TransactionId).PlannedDays.ShouldBe(3);
    }

    [Fact]
    public async Task Withdrawing_A_Pending_Plan_Should_Close_Its_Request()
    {
        CreateCurrentUserEmployee("LPL310");
        EnableLeavePlanWorkflow("LPL310APP");

        var start = DateTime.Today.AddDays(16);
        var result = await _plans.RequestLeavePlan(PlanInput(start, start));

        await _plans.CancelMyLeavePlan(new EntityDto(result.TransactionId));

        Row(result.TransactionId).IsDeleted.ShouldBeTrue();
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value).RequestStatus)
            .ShouldBe(RequestStatus.Closed);
    }

    [Fact]
    public async Task Preview_Should_Give_The_Planned_Days_For_The_Dates_On_The_Form()
    {
        CreateCurrentUserEmployee("LPL320");

        var start = DateTime.Today.AddDays(60);
        var preview = await _plans.PreviewMyLeavePlan(new PreviewLeavePlanInput { StartDate = start, EndDate = start.AddDays(6) });

        preview.PlannedDays.ShouldBe(7);
    }

    // ---------------------------------------------------------------- the team scheduler

    [Fact]
    public async Task The_Team_Scheduler_Should_Show_Me_And_My_Direct_Reports_Only()
    {
        // Legacy WorkflowRepository.GetSchedulerData: the logged-in employee, and everybody whose
        // EmployeeManager row names them. Not a colleague, not the whole company.
        var meId = CreateCurrentUserEmployee("LPL400");
        var reportId = CreateEmployee("LPL400REPORT");
        var strangerId = CreateEmployee("LPL400STRANGER");

        var month = new DateTime(DateTime.Today.Year + 1, 3, 1);

        UsingDbContext(context =>
        {
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = TenantId,
                EmployeeId = reportId,
                ManagerId = meId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = InfiniteEndDate
            });

            foreach (var employeeId in new[] { meId, reportId, strangerId })
            {
                context.Set<LeavePlan>().Add(new LeavePlan
                {
                    TenantId = TenantId,
                    EmployeeId = employeeId,
                    StartDate = month.AddDays(3),
                    EndDate = month.AddDays(5),
                    PlannedDays = 3
                });
            }
        });

        var planner = await _plans.GetMyTeamLeavePlanner(new GetLeavePlannerInput { Month = month, IncludePlanned = true });

        var shown = planner.Groups.SelectMany(g => g.Rows).Select(r => r.EmployeeId).ToList();
        shown.ShouldContain(meId);
        shown.ShouldContain(reportId);
        shown.ShouldNotContain(strangerId);
    }

    [Fact]
    public async Task The_Team_Scheduler_Should_Not_Show_A_Draft()
    {
        // A draft is the employee's own unfinished work; their manager has not been told about it.
        var meId = CreateCurrentUserEmployee("LPL410");
        var month = new DateTime(DateTime.Today.Year + 1, 4, 1);

        UsingDbContext(context =>
        {
            context.Set<LeavePlan>().Add(new LeavePlan
            {
                TenantId = TenantId,
                EmployeeId = meId,
                StartDate = month.AddDays(1),
                EndDate = month.AddDays(2),
                WorkflowStatus = RequestStatus.Draft
            });
        });

        var planner = await _plans.GetMyTeamLeavePlanner(new GetLeavePlannerInput
        {
            Month = month,
            IncludePlanned = true,
            IncludePlannedPending = true
        });

        planner.Groups.SelectMany(g => g.Rows).ShouldBeEmpty();
    }
}
