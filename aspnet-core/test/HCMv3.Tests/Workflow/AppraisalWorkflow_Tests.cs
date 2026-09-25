using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Localization;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using HCMv3.PerformanceManagement.Workflow;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers performance appraisals (<see cref="PAEmployee"/>) through the workflow engine.
///
/// <para>
/// The second transaction type that is routed but NOT self-service. An appraisal is opened FOR an employee
/// -- by HR for a cycle, or by a manager for their team -- so it is submitted from the admin service and
/// its catalog row carries no ESS route. The employee's own contribution is their self-rating, which lands
/// on <c>PAEmployeeReviewer</c>/<c>PAEmployeeRating</c> rather than on the row that routes.
/// </para>
///
/// <para>
/// Routing is worth it because a signed-off appraisal is what a proposed increment and a proposed job title
/// are argued from. Until it is approved the row must stay invisible to everything that reads appraisals,
/// which is what the ApprovedOnly test below is really checking.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class AppraisalWorkflow_Tests : AppTestBase
{
    private readonly IPAEmployeesAppService _appraisals;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public AppraisalWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appraisals = Resolve<IPAEmployeesAppService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
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

    private int CreateCycle(string nameEn = "2026 Annual")
    {
        PACycle cycle = null;
        UsingDbContext(context =>
        {
            cycle = context.Set<PACycle>().Add(new PACycle
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = "دورة"
            }).Entity;
        });

        return cycle.Id;
    }

    private int CreateFormType(string nameEn = "360 Review")
    {
        FormType formType = null;
        UsingDbContext(context =>
        {
            formType = context.Set<FormType>().Add(new FormType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = "نموذج",
                MinReviewerLimit = 1,
                MaxReviewerLimit = 5
            }).Entity;
        });

        return formType.Id;
    }

    /// <summary>Turns appraisals on for approval with a real approver holding the step's role.</summary>
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
                Code = "APR-STD",
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
                .First(t => t.Code == AppraisalWorkflowTransactionHandler.AppraisalRequestType);
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static CreateOrEditPAEmployeeDto AppraisalFor(int employeeId, int cycleId, int formTypeId)
    {
        return new CreateOrEditPAEmployeeDto
        {
            EmployeeId = employeeId,
            PACycleId = cycleId,
            FormTypeId = formTypeId,
            ProposedIncrement = "5%",
            ProposedJobTitle = "Senior Analyst"
        };
    }

    private PAEmployee AppraisalRow(int employeeId) =>
        UsingDbContext(c => c.Set<PAEmployee>().IgnoreQueryFilters().First(a => a.EmployeeId == employeeId));

    private WorkflowRequest AppraisalRequest() =>
        UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.RequestType == AppraisalWorkflowTransactionHandler.AppraisalRequestType));

    [Fact]
    public async Task Should_Record_Immediately_While_The_Type_Is_Not_Routed()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install -- and the
        // behaviour HR has today. An appraisal is simply recorded and is effective at once.
        var employeeId = CreateEmployee("APR100");

        await _appraisals.CreateOrEdit(AppraisalFor(employeeId, CreateCycle(), CreateFormType()));

        AppraisalRow(employeeId).WorkflowStatus.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>()
            .Count(r => r.RequestType == AppraisalWorkflowTransactionHandler.AppraisalRequestType))
            .ShouldBe(0);
    }

    [Fact]
    public async Task Should_Route_For_Approval_Once_The_Type_Is_Enabled()
    {
        var employeeId = CreateEmployee("APR110");
        EnableWorkflow("APR110APP");
        var formTypeId = CreateFormType();

        await _appraisals.CreateOrEdit(AppraisalFor(employeeId, CreateCycle(), formTypeId));

        var request = AppraisalRequest();
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        // A number of its own, and nothing but digits: the per-type prefix went when the counter became
        // tenant-wide, so "which type is this" is no longer something the number answers.
        request.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        request.RequestNumber.ShouldNotContain("-");
        int.Parse(request.RequestNumber).ShouldBeGreaterThan(0);

        // About the appraisee, not about whoever in HR opened the appraisal -- the approver inbox and the
        // employee's own request list both have to show it against them.
        request.EmployeeId.ShouldBe(employeeId);

        // Routed on FORM TYPE, as legacy did. A 360 form is signed off by a different set of people from a
        // probation form; the cycle is only a period, so it would be the wrong axis to route on.
        request.RequestSubTypeId.ShouldBe(formTypeId);
    }

    [Fact]
    public async Task An_Unapproved_Appraisal_Should_Be_Invisible_To_The_Rest_Of_The_System()
    {
        // The whole point of the one-row design: the pending appraisal lives in the real table, so it must
        // be the ApprovedOnly filter that keeps everything else from reading it. An unapproved score must
        // not be able to justify an increment.
        var employeeId = CreateEmployee("APR120");
        EnableWorkflow("APR120APP");

        await _appraisals.CreateOrEdit(AppraisalFor(employeeId, CreateCycle(), CreateFormType()));

        var repository = Resolve<Abp.Domain.Repositories.IRepository<PAEmployee>>();

        using (_unitOfWorkManager.Begin())
        {
            repository.GetAll().Any(a => a.EmployeeId == employeeId)
                .ShouldBeFalse("An unapproved appraisal was visible to the rest of the system.");
        }
    }

    [Fact]
    public async Task Approving_Should_Make_The_Appraisal_Real()
    {
        var employeeId = CreateEmployee("APR130");
        var approverId = EnableWorkflow("APR130APP");

        await _appraisals.CreateOrEdit(AppraisalFor(employeeId, CreateCycle(), CreateFormType()));

        var requestId = AppraisalRequest().Id;

        await Resolve<IWorkflowApprovalService>().ApproveAsync(requestId, approverId, "Agreed");

        AppraisalRow(employeeId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task The_Summary_Should_Name_The_Cycle_And_The_Form()
    {
        // An approver seeing only an employee's name cannot tell this year's annual review from a probation
        // form, and cannot see the figure they are agreeing to.
        var employeeId = CreateEmployee("APR140");
        EnableWorkflow("APR140APP");

        await _appraisals.CreateOrEdit(AppraisalFor(employeeId, CreateCycle("2026 Annual"), CreateFormType("360 Review")));

        var request = AppraisalRequest();

        request.SummaryEn.ShouldStartWith("2026 Annual");
        request.SummaryEn.ShouldContain("360 Review");
        request.SummaryEn.ShouldContain("score");
        request.SummaryAr.ShouldContain("دورة");
    }

    [Fact]
    public async Task An_Appraisal_With_No_Cycle_Should_Not_Be_Submittable()
    {
        // Without a cycle nobody can say which period is being signed off, so the same appraisal could be
        // counted against the wrong year -- or twice. Legacy refused this at submit for the same reason
        // ("Period Cannot be blank").
        var appraisal = new PAEmployee { Id = 1, TenantId = TenantId, EmployeeId = 7, PACycleId = 0, FormTypeId = 3 };

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            HandlerOver(appraisal).ValidateBeforeSubmitAsync(ContextFor(appraisal)));
    }

    [Fact]
    public async Task An_Appraisal_With_No_Form_Type_Should_Not_Be_Submittable()
    {
        // No form type means no KPI set and no reviewer limits -- there is nothing for the reviewers to
        // fill in, and nothing for the engine to route on.
        var appraisal = new PAEmployee { Id = 1, TenantId = TenantId, EmployeeId = 7, PACycleId = 3, FormTypeId = 0 };

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            HandlerOver(appraisal).ValidateBeforeSubmitAsync(ContextFor(appraisal)));
    }

    [Fact]
    public async Task A_Complete_Appraisal_Should_Pass_Validation()
    {
        var appraisal = new PAEmployee { Id = 1, TenantId = TenantId, EmployeeId = 7, PACycleId = 3, FormTypeId = 4 };

        await HandlerOver(appraisal).ValidateBeforeSubmitAsync(ContextFor(appraisal));
    }

    [Fact]
    public void Appraisals_Should_Not_Be_Offered_As_Self_Service()
    {
        // The appraisal row is opened for an employee rather than by them, so it carries no ESS route and
        // never appears on the self-service tile list -- the same arrangement as a personnel action.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>()
            .First(t => t.Code == AppraisalWorkflowTransactionHandler.AppraisalRequestType));

        type.EssRoute.ShouldBeNull();
        type.EssPermissionName.ShouldBeNull();
        type.IsEssEnabled.ShouldBeFalse();
        type.SubTypeSource.ShouldBe("FormType");
    }

    /// <summary>
    /// Builds the handler over plain in-memory rows rather than over the store.
    ///
    /// <para>
    /// Needed because the two values this validation refuses -- an unset cycle and an unset form type -- are
    /// non-nullable FKs, so a row carrying them cannot be inserted at all: SQLite rejects it on the foreign
    /// key before the handler is ever reached. The check is still worth having and still worth testing: it
    /// is the guard for rows that arrive by import or by a caller that skips the app service, and the
    /// message it produces is what an approver would otherwise never see an explanation for.
    /// </para>
    /// </summary>
    private AppraisalWorkflowTransactionHandler HandlerOver(params PAEmployee[] appraisals)
    {
        var activeUnitOfWork = Substitute.For<IActiveUnitOfWork>();
        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Current.Returns(activeUnitOfWork);

        var handler = new AppraisalWorkflowTransactionHandler(
            ListRepository(appraisals),
            ListRepository<PACycle>(),
            ListRepository<FormType>(),
            unitOfWorkManager);

        handler.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();

        return handler;
    }

    private static IRepository<TEntity> ListRepository<TEntity>(params TEntity[] rows)
        where TEntity : class, IEntity<int>
    {
        var repository = Substitute.For<IRepository<TEntity>>();
        repository.GetAll().Returns(_ => rows.AsQueryable());

        return repository;
    }

    private WorkflowTransactionContext ContextFor(PAEmployee appraisal)
    {
        return new WorkflowTransactionContext(
            new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = appraisal.EmployeeId,
                RequestType = AppraisalWorkflowTransactionHandler.AppraisalRequestType,
                TargetEntityId = appraisal.Id,
                RequestNumber = "APR-TEST",
                RequestStatus = RequestStatus.Draft
            },
            appraisal.EmployeeId);
    }
}
