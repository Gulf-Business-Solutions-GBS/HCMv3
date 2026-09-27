using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers the rule that work which has declared itself non-interactive never routes for approval,
/// whatever the catalog says.
///
/// <para>
/// The case that matters is not hypothetical. The go-live employee load writes bank details through a
/// routed application service, so before this rule existed, ticking "Workflow Enabled" on Employee Bank
/// would have turned a five-hundred-employee opening load into five hundred approval requests for data
/// that was migrated rather than requested -- and nobody would have connected the two settings until it
/// happened, at go-live, which is the worst possible moment to find out.
/// </para>
///
/// <para>
/// <c>EmployeeDelegation</c> stands in for any routed transaction, being the smallest one in the product.
/// Nothing here is about delegation.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class NonInteractiveScope_Tests : AppTestBase
{
    private readonly IAdminTransactionSubmitter _submitter;
    private readonly IRepository<EmployeeDelegation> _delegations;
    private readonly IRepository<WorkflowRequest> _requests;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public NonInteractiveScope_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _submitter = Resolve<IAdminTransactionSubmitter>();
        _delegations = Resolve<IRepository<EmployeeDelegation>>();
        _requests = Resolve<IRepository<WorkflowRequest>>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string ShortCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private int CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = ShortCode(),
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
                FirstNameAr = "T",
                LastNameAr = "T",
                FullNameEn = code + " T",
                FullNameAr = "T T",
                JoiningDate = new DateTime(2020, 1, 1),
                Status = EmployeeStatus.Active,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>Routes delegations through a one-step chain, so there is something real to bypass.</summary>
    private void EnableRouting()
    {
        var approverId = CreateEmployee("NI" + ShortCode().Substring(0, 5));

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = ShortCode().Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "NI-STD-" + ShortCode().Substring(0, 4),
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
                ApprovalRequire = ApprovalRequire.Single
            });

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = approverId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeDelegation");
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });
    }

    private async Task<int> RecordAsync(int employeeId, int delegateToId)
    {
        using var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });

        var id = await _submitter.CreateAndSubmitAsync(
            _delegations,
            new EmployeeDelegation
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DelegateToId = delegateToId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30)
            },
            "EmployeeDelegation",
            "DEL");

        await uow.CompleteAsync();

        return id;
    }

    // ------------------------------------------------------------------ the rule

    /// <summary>
    /// The whole point. Same routed type, same call, same configuration -- the only difference is that the
    /// caller has declared itself an import, and no approval request exists afterwards.
    /// </summary>
    [Fact]
    public async Task Should_Not_Route_Inside_A_Non_Interactive_Scope()
    {
        EnableRouting();
        var subjectId = CreateEmployee("NIS100SUBJ");
        var coverId = CreateEmployee("NIS100COVER");

        int id;
        using (NonInteractiveScope.Begin("a test import"))
        {
            id = await RecordAsync(subjectId, coverId);
        }

        var row = UsingDbContext(context => context.Set<EmployeeDelegation>().Single(d => d.Id == id));

        // Null, not Draft and not Closed: an imported row has always been immediately effective, and it
        // stays exactly that. Draft would hide it from every ordinary query.
        row.WorkflowStatus.ShouldBeNull();
        row.WorkflowRequestId.ShouldBeNull();

        var raised = UsingDbContext(context =>
            context.Set<WorkflowRequest>().Count(r => r.TargetEntityId == id && r.RequestType == "EmployeeDelegation"));

        raised.ShouldBe(0);
    }

    /// <summary>
    /// The control. Without the scope the identical call routes, which is what proves the test above is
    /// measuring the scope rather than a catalog that was never switched on.
    /// </summary>
    [Fact]
    public async Task Should_Still_Route_Outside_The_Scope()
    {
        EnableRouting();
        var subjectId = CreateEmployee("NIS200SUBJ");
        var coverId = CreateEmployee("NIS200COVER");

        var id = await RecordAsync(subjectId, coverId);

        var raised = UsingDbContext(context =>
            context.Set<WorkflowRequest>().Count(r => r.TargetEntityId == id && r.RequestType == "EmployeeDelegation"));

        raised.ShouldBe(1);
    }

    /// <summary>
    /// A load is one scope around many rows, so it has to survive more than the first one.
    /// </summary>
    [Fact]
    public async Task Should_Not_Route_Any_Row_Of_A_Multi_Row_Load()
    {
        EnableRouting();
        var coverId = CreateEmployee("NIS300COVER");

        using (NonInteractiveScope.Begin("a test import"))
        {
            for (var i = 0; i < 3; i++)
            {
                await RecordAsync(CreateEmployee("NIS30" + i + "SUBJ"), coverId);
            }
        }

        var raised = UsingDbContext(context =>
            context.Set<WorkflowRequest>().Count(r => r.RequestType == "EmployeeDelegation"));

        raised.ShouldBe(0);
    }

    // ------------------------------------------------------------------ the scope itself

    [Fact]
    public void Should_Not_Be_Active_By_Default()
    {
        NonInteractiveScope.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Should_Clear_Itself_When_The_Scope_Ends()
    {
        using (NonInteractiveScope.Begin("a test import"))
        {
            NonInteractiveScope.IsActive.ShouldBeTrue();
        }

        NonInteractiveScope.IsActive.ShouldBeFalse();
    }

    /// <summary>
    /// An importer calling another importer must not unmark itself when the inner one finishes -- that
    /// would leave the remaining rows of the outer load routing, which is the exact failure this whole
    /// mechanism exists to prevent, made harder to spot by only affecting part of a run.
    /// </summary>
    [Fact]
    public void Should_Stay_Active_Until_The_Outermost_Scope_Ends()
    {
        using (NonInteractiveScope.Begin("outer load"))
        {
            using (NonInteractiveScope.Begin("inner load"))
            {
                NonInteractiveScope.IsActive.ShouldBeTrue();
            }

            NonInteractiveScope.IsActive.ShouldBeTrue();
            NonInteractiveScope.Reason.ShouldBe("outer load");
        }

        NonInteractiveScope.IsActive.ShouldBeFalse();
    }

    /// <summary>
    /// The second chokepoint. Nothing creates a request inside a scope any more, so this only fires if a
    /// future path builds one some other way and tries to send it -- which is precisely when a loud
    /// failure is worth having.
    /// </summary>
    [Fact]
    public void Should_Refuse_To_Send_A_Request_From_Inside_A_Scope()
    {
        using (NonInteractiveScope.Begin("a test import"))
        {
            Should.Throw<InvalidOperationException>(() => NonInteractiveScope.ThrowIfActive());
        }
    }
}
