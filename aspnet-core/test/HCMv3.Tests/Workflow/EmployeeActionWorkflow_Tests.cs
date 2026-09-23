using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers personnel actions through the workflow engine -- the first transaction type that is routed for
/// approval but is NOT self-service.
///
/// <para>
/// HR raises a promotion or a transfer, never the employee it concerns, so submission happens in the admin
/// service rather than an ESS one and the catalog row carries no ESS route at all. What makes routing worth
/// it is what an EmployeeAction is the parent of: org assignments and contracts hang off it, so an
/// unapproved one moves somebody's job title, cost centre and pay in a single step.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeActionWorkflow_Tests : AppTestBase
{
    private readonly IEmployeeActionsAppService _employeeActions;

    public EmployeeActionWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _employeeActions = Resolve<IEmployeeActionsAppService>();
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

    private int CreatePersonalAction()
    {
        PersonalAction action = null;
        UsingDbContext(context =>
        {
            action = context.Set<PersonalAction>().Add(new PersonalAction
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Promotion",
                NameAr = "Promotion"
            }).Entity;
        });

        return action.Id;
    }

    private int CreateReason()
    {
        PersonalActionReason reason = null;
        UsingDbContext(context =>
        {
            reason = context.Set<PersonalActionReason>().Add(new PersonalActionReason
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Annual review",
                NameAr = "Annual review"
            }).Entity;
        });

        return reason.Id;
    }

    /// <summary>Turns personnel actions on for approval with a real approver holding the step's role.</summary>
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
                Code = "PA-STD",
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

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAction");
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private CreateOrEditEmployeeActionDto ActionFor(int employeeId, int personalActionId, int reasonId, DateTime? start = null)
    {
        return new CreateOrEditEmployeeActionDto
        {
            EmployeeId = employeeId,
            PersonalActionId = personalActionId,
            ActionReasonId = reasonId,
            StartDate = start ?? DateTime.Today.AddDays(30),
            Comments = "Annual promotion round"
        };
    }

    private EmployeeAction ActionRow(int employeeId) =>
        UsingDbContext(c => c.Set<EmployeeAction>().IgnoreQueryFilters().First(a => a.EmployeeId == employeeId));

    [Fact]
    public async Task Should_Record_Immediately_While_The_Type_Is_Not_Routed()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install -- and the
        // pre-workflow behaviour HR relies on today.
        var employeeId = CreateEmployee("PA100");

        await _employeeActions.CreateOrEdit(ActionFor(employeeId, CreatePersonalAction(), CreateReason()));

        ActionRow(employeeId).WorkflowStatus.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>().Count(r => r.RequestType == "EmployeeAction")).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Route_For_Approval_Once_The_Type_Is_Enabled()
    {
        var employeeId = CreateEmployee("PA110");
        EnableWorkflow("PA110APP");

        await _employeeActions.CreateOrEdit(ActionFor(employeeId, CreatePersonalAction(), CreateReason()));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "EmployeeAction"));
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.RequestNumber.ShouldStartWith("PA-");

        // The request is about the SUBJECT employee, not whoever in HR raised it. The approver inbox and
        // the employee's own request list both have to show it against them.
        request.EmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task An_Unapproved_Action_Should_Be_Invisible_To_The_Rest_Of_The_System()
    {
        // The reason this type is worth routing: org assignments and contracts hang off an EmployeeAction,
        // so one that nobody agreed to must not be buildable on.
        var employeeId = CreateEmployee("PA120");
        EnableWorkflow("PA120APP");

        await _employeeActions.CreateOrEdit(ActionFor(employeeId, CreatePersonalAction(), CreateReason()));

        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeAction>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            // ApprovedOnly is ON here, exactly as an org-assignment screen would see it.
            repository.GetAll().Any(a => a.EmployeeId == employeeId)
                .ShouldBeFalse("An unapproved personnel action was visible to the rest of the system.");
        }
    }

    [Fact]
    public async Task Approving_Should_Make_The_Action_Real()
    {
        var employeeId = CreateEmployee("PA130");
        var approverId = EnableWorkflow("PA130APP");

        await _employeeActions.CreateOrEdit(ActionFor(employeeId, CreatePersonalAction(), CreateReason()));

        var requestId = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "EmployeeAction").Id);

        await Resolve<HCMv3.Workflow.Services.IWorkflowApprovalService>()
            .ApproveAsync(requestId, approverId, "Agreed");

        ActionRow(employeeId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task The_Summary_Should_Say_What_Is_Being_Decided()
    {
        // "Promotion, Annual review" tells an approver what they are agreeing to; an employee name and a
        // date do not.
        var employeeId = CreateEmployee("PA140");
        EnableWorkflow("PA140APP");

        await _employeeActions.CreateOrEdit(ActionFor(employeeId, CreatePersonalAction(), CreateReason()));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.RequestType == "EmployeeAction"));

        request.SummaryEn.ShouldStartWith("Promotion");
        request.SummaryEn.ShouldContain("Annual review");
    }

    [Fact]
    public async Task A_Duplicate_Action_On_The_Same_Day_Should_Be_Refused()
    {
        var employeeId = CreateEmployee("PA150");
        EnableWorkflow("PA150APP");

        var personalActionId = CreatePersonalAction();
        var reasonId = CreateReason();
        var start = DateTime.Today.AddDays(30);

        await _employeeActions.CreateOrEdit(ActionFor(employeeId, personalActionId, reasonId, start));

        // Refused by the admin service's own duplicate check or by the handler's -- either is correct, and
        // the point is that a pending first action is visible to whichever one fires.
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _employeeActions.CreateOrEdit(ActionFor(employeeId, personalActionId, reasonId, start)));
    }

    [Fact]
    public void Personnel_Actions_Should_Not_Be_Offered_As_Self_Service()
    {
        // A promotion you can request for yourself is not an approval chain, it is a suggestion box. The
        // catalog row carries no ESS route, which is what keeps it off the landing page.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAction"));

        type.EssRoute.ShouldBeNull();
        type.EssPermissionName.ShouldBeNull();
        type.IsEssEnabled.ShouldBeFalse();
    }
}
