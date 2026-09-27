using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers recording a workflow-enabled transaction WITHOUT sending it for approval.
///
/// <para>
/// Two gates gate a bypass, and both live on the server rather than in a screen: the transaction type must
/// permit it at all, and the user must hold <c>RecordWithoutApproval</c>. The type gate is what these tests
/// exercise. The permission gate is not unit-tested here for an honest reason -- the harness signs in as the
/// tenant administrator, who holds every permission, so a test asserting "denied" could only pass by
/// substituting the checker and would then be testing the substitute.
/// </para>
///
/// <para>
/// <c>EmployeeDelegation</c> is the subject because it is the smallest workflow-enabled entity in the
/// product: two employees and two dates. Nothing here is about delegation.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowDirectEntry_Tests : AppTestBase
{
    private readonly IAdminTransactionSubmitter _submitter;
    private readonly IRepository<EmployeeDelegation> _delegations;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public WorkflowDirectEntry_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _submitter = Resolve<IAdminTransactionSubmitter>();
        _delegations = Resolve<IRepository<EmployeeDelegation>>();
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
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                JoiningDate = new DateTime(2020, 1, 1),
                Status = EmployeeStatus.Active,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>Routes delegations through a one-step chain, and says whether the type may be bypassed.</summary>
    private void ConfigureType(bool workflowEnabled, bool allowDirectEntry)
    {
        var approverId = CreateEmployee("DE" + ShortCode().Substring(0, 5));

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
                Code = "DEL-STD-" + ShortCode().Substring(0, 4),
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
            type.IsWorkflowEnabled = workflowEnabled;
            type.IsActive = true;
            type.AllowDirectEntry = allowDirectEntry;
            type.DefaultRequestMasterId = master.Id;
        });
    }

    private EmployeeDelegation NewDelegation(int employeeId, int delegateToId) => new()
    {
        TenantId = TenantId,
        EmployeeId = employeeId,
        DelegateToId = delegateToId,
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddDays(30)
    };

    /// <summary>
    /// Calls the submitter inside a unit of work, because it is a domain service rather than an application
    /// service: nothing wraps it here the way ABP's interceptor wraps an app service in production, and the
    /// request-number generator needs an ambient context to read the counter from.
    /// </summary>
    private async Task<int> RecordAsync(int employeeId, int delegateToId, bool recordDirectly)
    {
        using var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { IsTransactional = true });

        var id = await _submitter.CreateAndSubmitAsync(
            _delegations,
            NewDelegation(employeeId, delegateToId),
            "EmployeeDelegation",
            "DEL",
            recordDirectly: recordDirectly);

        await uow.CompleteAsync();

        return id;
    }

    // ---------------------------------------------------------------- the type gate

    /// <summary>
    /// The first gate. A type that must always be approved cannot be talked out of it by the caller, however
    /// senior -- and it fails loudly, because a bypass that quietly routed instead would leave somebody
    /// believing a record was effective when it was sitting in an inbox.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_A_Bypass_On_A_Type_That_Does_Not_Permit_One()
    {
        ConfigureType(workflowEnabled: true, allowDirectEntry: false);
        var subjectId = CreateEmployee("DIR100SUBJ");
        var coverId = CreateEmployee("DIR100COVER");

        await Should.ThrowAsync<UserFriendlyException>(() => RecordAsync(subjectId, coverId, recordDirectly: true));
    }

    [Fact]
    public async Task Should_Record_Directly_When_The_Type_Permits_It()
    {
        ConfigureType(workflowEnabled: true, allowDirectEntry: true);
        var subjectId = CreateEmployee("DIR200SUBJ");
        var coverId = CreateEmployee("DIR200COVER");

        var id = await RecordAsync(subjectId, coverId, recordDirectly: true);

        var row = UsingDbContext(context => context.Set<EmployeeDelegation>().Single(d => d.Id == id));

        // Closed rather than Approved: final and visible, but nobody decided anything.
        row.WorkflowStatus.ShouldBe(RequestStatus.Closed);
        row.WorkflowRequestId.ShouldNotBeNull();
    }

    /// <summary>Nothing is left for anybody to act on -- that is the whole point of the bypass.</summary>
    [Fact]
    public async Task A_Directly_Recorded_Row_Should_Reach_No_Approver()
    {
        ConfigureType(workflowEnabled: true, allowDirectEntry: true);
        var subjectId = CreateEmployee("DIR300SUBJ");
        var coverId = CreateEmployee("DIR300COVER");

        var id = await RecordAsync(subjectId, coverId, recordDirectly: true);

        var requestId = UsingDbContext(context =>
            context.Set<EmployeeDelegation>().Single(d => d.Id == id).WorkflowRequestId.Value);

        UsingDbContext(context =>
        {
            context.Set<WorkFlowApprover>().Count(a => a.WorkflowRequestId == requestId).ShouldBe(0);
            context.Set<WorkflowRequest>().Single(r => r.Id == requestId).RequestStatus.ShouldBe(RequestStatus.Closed);
        });
    }

    /// <summary>
    /// The audit. A transaction with no workflow trace at all is indistinguishable from an approved one, so
    /// who skipped approval has to survive -- as its own action, not as a fake approval.
    /// </summary>
    [Fact]
    public async Task A_Directly_Recorded_Row_Should_Say_Who_Recorded_It()
    {
        ConfigureType(workflowEnabled: true, allowDirectEntry: true);
        var subjectId = CreateEmployee("DIR400SUBJ");
        var coverId = CreateEmployee("DIR400COVER");

        var id = await RecordAsync(subjectId, coverId, recordDirectly: true);

        var requestId = UsingDbContext(context =>
            context.Set<EmployeeDelegation>().Single(d => d.Id == id).WorkflowRequestId.Value);

        UsingDbContext(context =>
        {
            var log = context.Set<RequestLog>().Single(l => l.WorkflowRequestId == requestId);

            log.ActionType.ShouldBe(RequestActionType.RecordedDirectly);
            log.ActionById.ShouldNotBeNull();
        });
    }

    // ---------------------------------------------------------------- the ordinary paths still work

    /// <summary>The regression guard: not asking for a bypass must still route exactly as before.</summary>
    [Fact]
    public async Task Not_Asking_For_A_Bypass_Should_Still_Route_For_Approval()
    {
        ConfigureType(workflowEnabled: true, allowDirectEntry: true);
        var subjectId = CreateEmployee("DIR500SUBJ");
        var coverId = CreateEmployee("DIR500COVER");

        var id = await RecordAsync(subjectId, coverId, recordDirectly: false);

        var row = UsingDbContext(context => context.Set<EmployeeDelegation>().Single(d => d.Id == id));

        row.WorkflowStatus.ShouldBe(RequestStatus.Open);
    }

    /// <summary>
    /// With workflow off every save is already direct, so asking to bypass asks for what is happening
    /// anyway. Ignored rather than refused -- refusing would make the flag a trap for a caller that cannot
    /// know how an administrator has configured the type.
    /// </summary>
    [Fact]
    public async Task Asking_For_A_Bypass_On_An_Unrouted_Type_Should_Simply_Record_It()
    {
        ConfigureType(workflowEnabled: false, allowDirectEntry: false);
        var subjectId = CreateEmployee("DIR600SUBJ");
        var coverId = CreateEmployee("DIR600COVER");

        var id = await RecordAsync(subjectId, coverId, recordDirectly: true);

        var row = UsingDbContext(context => context.Set<EmployeeDelegation>().Single(d => d.Id == id));

        row.WorkflowStatus.ShouldBeNull();
        row.WorkflowRequestId.ShouldBeNull();
    }
}
