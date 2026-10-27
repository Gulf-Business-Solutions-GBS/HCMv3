using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.ManpowerPlanningManagement.Dto;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers manpower plans through the workflow engine.
///
/// <para>
/// This type is different from the ones before it: it did not gain workflow, it had its own approval
/// REPLACED. <c>ManpowerPlanningsAppService</c> used to expose Approve/Reject writing a stored
/// <c>ManpowerPlanningStatus</c> column. Those are gone, the column is gone, and the status is now a
/// read-only projection of the engine's <c>WorkflowStatus</c> -- so the interesting assertions here are
/// about the two never being able to disagree.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class ManpowerPlanningWorkflow_Tests : AppTestBase
{
    private readonly IManpowerPlanningsAppService _plans;

    public ManpowerPlanningWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _plans = Resolve<IManpowerPlanningsAppService>();
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

    private long CreateOrganizationUnit(string displayName)
    {
        Abp.Organizations.OrganizationUnit unit = null;
        UsingDbContext(context =>
        {
            unit = context.Set<Abp.Organizations.OrganizationUnit>()
                .Add(new Abp.Organizations.OrganizationUnit(TenantId, displayName)
                {
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8)
                }).Entity;
        });

        return unit.Id;
    }

    private int CreatePeriod(string name)
    {
        ManpowerPlanningPeriod period = null;
        UsingDbContext(context =>
        {
            period = context.Set<ManpowerPlanningPeriod>().Add(new ManpowerPlanningPeriod
            {
                TenantId = TenantId,
                Name = name,
                Active = true,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            }).Entity;
        });

        return period.Id;
    }

    private int CreateJobTitle(string code)
    {
        JobTitle jobTitle = null;
        UsingDbContext(context =>
        {
            jobTitle = context.Set<JobTitle>().Add(new JobTitle
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });

        return jobTitle.Id;
    }

    /// <summary>Turns manpower plans on for approval with a real approver holding the step's role.</summary>
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
                Code = "MP-STD",
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

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "ManpowerPlanning");
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private CreateOrEditManpowerPlanningDto PlanFor(int employeeId, long organizationUnitId, int periodId, int? jobTitleId)
    {
        var dto = new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = organizationUnitId,
            PeriodId = periodId,
            RequestComments = "Headcount for next year"
        };

        if (jobTitleId.HasValue)
        {
            dto.Details = new List<CreateOrEditManpowerPlanningDetailDto>
            {
                new CreateOrEditManpowerPlanningDetailDto
                {
                    JobTitleId = jobTitleId.Value,
                    January = 1000,
                    February = 1000
                }
            };
        }

        return dto;
    }

    private ManpowerPlanning PlanRow(int periodId) =>
        UsingDbContext(c => c.Set<ManpowerPlanning>().IgnoreQueryFilters().First(m => m.PeriodId == periodId));

    [Fact]
    public async Task Should_Record_Immediately_While_The_Type_Is_Not_Routed()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install.
        var periodId = CreatePeriod("MP-2026-A");

        await _plans.CreateOrEdit(PlanFor(CreateEmployee("MP100"), CreateOrganizationUnit("Ops A"), periodId, null));

        var plan = PlanRow(periodId);
        plan.WorkflowStatus.ShouldBeNull();
        plan.Status.ShouldBe(ManpowerPlanningStatus.Draft);

        UsingDbContext(c => c.Set<WorkflowRequest>().Count(r => r.RequestType == "ManpowerPlanning")).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Route_For_Approval_Once_The_Type_Is_Enabled()
    {
        var periodId = CreatePeriod("MP-2026-B");
        EnableWorkflow("MP110APP");
        var employeeId = CreateEmployee("MP110");

        await _plans.CreateOrEdit(PlanFor(employeeId, CreateOrganizationUnit("Ops B"), periodId, CreateJobTitle("JT110")));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "ManpowerPlanning"));
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        // A number of its own, and nothing but digits: the per-type prefix went when the counter became
        // tenant-wide, so "which type is this" is no longer something the number answers.
        request.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        request.RequestNumber.ShouldNotContain("-");
        int.Parse(request.RequestNumber).ShouldBeGreaterThan(0);
        request.EmployeeId.ShouldBe(employeeId);

        // The derived status tracks the engine with nothing else writing it.
        PlanRow(periodId).Status.ShouldBe(ManpowerPlanningStatus.Submitted);
    }

    [Fact]
    public async Task The_Summary_Should_Say_Which_Department_Which_Period_And_How_Much()
    {
        var periodId = CreatePeriod("MP-2026-C");
        EnableWorkflow("MP120APP");

        await _plans.CreateOrEdit(PlanFor(CreateEmployee("MP120"), CreateOrganizationUnit("Finance"), periodId, CreateJobTitle("JT120")));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "ManpowerPlanning"));

        request.SummaryEn.ShouldContain("Finance");
        request.SummaryEn.ShouldContain("MP-2026-C");
        request.SummaryEn.ShouldContain("2,000.00");
    }

    [Fact]
    public async Task An_Unapproved_Plan_Should_Be_Invisible_To_The_Rest_Of_The_System()
    {
        // A plan nobody agreed to must not read as budgeted headcount anywhere outside its own screen.
        var periodId = CreatePeriod("MP-2026-D");
        EnableWorkflow("MP130APP");

        await _plans.CreateOrEdit(PlanFor(CreateEmployee("MP130"), CreateOrganizationUnit("Ops D"), periodId, CreateJobTitle("JT130")));

        var repository = Resolve<Abp.Domain.Repositories.IRepository<ManpowerPlanning>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            // ApprovedOnly is ON here, exactly as any ordinary query would see it.
            repository.GetAll().Any(m => m.PeriodId == periodId)
                .ShouldBeFalse("An unapproved manpower plan was visible to the rest of the system.");
        }
    }

    [Fact]
    public async Task Approving_Should_Move_The_Derived_Status_And_Create_The_Planned_Positions()
    {
        var periodId = CreatePeriod("MP-2026-E");
        var approverId = EnableWorkflow("MP140APP");
        var jobTitleId = CreateJobTitle("JT140");

        await _plans.CreateOrEdit(PlanFor(CreateEmployee("MP140"), CreateOrganizationUnit("Ops E"), periodId, jobTitleId));

        var requestId = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "ManpowerPlanning").Id);

        await Resolve<HCMv3.Workflow.Services.IWorkflowApprovalService>()
            .ApproveAsync(requestId, approverId, "Agreed");

        var plan = PlanRow(periodId);
        plan.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        plan.Status.ShouldBe(ManpowerPlanningStatus.Approved);

        // The legacy "Entry On Final Approval" side effect now hangs off the engine's approval, and only
        // off it -- there is no other code path left that mints these positions.
        var detail = UsingDbContext(c => c.Set<ManpowerPlanningDetail>().First(d => d.ManpowerPlanningId == plan.Id));
        detail.PositionId.ShouldNotBeNull();

        var position = UsingDbContext(c => c.Set<Position>().First(p => p.Id == detail.PositionId.Value));
        position.JobTitleId.ShouldBe(jobTitleId);
        position.MinSalary.ShouldBe(1000);
        position.Vacant.ShouldBeTrue();
        position.IsBudgeted.ShouldBeTrue();
        position.FTELimit.ShouldBe(1);
        position.StartDate.ShouldBe(new DateTime(2026, 1, 1));
    }

    [Fact]
    public async Task Rejecting_Should_Show_As_Rejected_And_Create_No_Positions()
    {
        var periodId = CreatePeriod("MP-2026-F");
        var approverId = EnableWorkflow("MP150APP");

        await _plans.CreateOrEdit(PlanFor(CreateEmployee("MP150"), CreateOrganizationUnit("Ops F"), periodId, CreateJobTitle("JT150")));

        var requestId = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "ManpowerPlanning").Id);

        await Resolve<HCMv3.Workflow.Services.IWorkflowApprovalService>()
            .RejectAsync(requestId, approverId, "Not this year");

        var plan = PlanRow(periodId);
        plan.Status.ShouldBe(ManpowerPlanningStatus.Rejected);

        UsingDbContext(c => c.Set<ManpowerPlanningDetail>()
            .Count(d => d.ManpowerPlanningId == plan.Id && d.PositionId != null)).ShouldBe(0);
    }

    [Fact]
    public async Task An_Empty_Plan_Should_Not_Be_Submittable()
    {
        // Approval is what creates the positions, so approving a plan with no lines agrees to nothing and
        // then silently creates nothing.
        var periodId = CreatePeriod("MP-2026-G");
        EnableWorkflow("MP160APP");

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _plans.CreateOrEdit(PlanFor(CreateEmployee("MP160"), CreateOrganizationUnit("Ops G"), periodId, null)));
    }

    [Fact]
    public void Manpower_Plans_Should_Not_Be_Offered_As_Self_Service()
    {
        // A manager plans a department's headcount; an employee does not request headcount about
        // themselves. No ESS route is what keeps it off the self-service landing page.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>().First(t => t.Code == "ManpowerPlanning"));

        type.EssRoute.ShouldBeNull();
        type.EssPermissionName.ShouldBeNull();
        type.IsEssEnabled.ShouldBeFalse();
    }
}
