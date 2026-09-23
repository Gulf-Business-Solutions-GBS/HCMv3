using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
/// <summary>
/// The creator rule the chain's first step carries: WHO may raise a request, and FOR WHOM.
///
/// <para>
/// <c>RequestDetail.Person</c> has been stored and editable since the port and read by nothing, so an
/// administrator could set "employees may raise this only for themselves" and the engine would happily
/// accept one raised for anybody. These tests exist so that it stays read.
/// </para>
/// <para>
/// Run against the real container rather than the Sqlite/FakeRepository harness the other engine tests use,
/// because the rule is resolved through <c>IEmployeeScopeProvider</c>, which answers from the SESSION. A
/// faked session would be testing the mapping and nothing else -- and the case that matters most here is an
/// admin with no employee record at all, which only a real session can produce.
/// </para>
/// </summary>
public class WorkflowCreatorScope_Tests : AppTestBase
{
    private const string RequestTypeCode = "CreatorScopeTest";

    private readonly IWorkflowApprovalService _approval;

    public WorkflowCreatorScope_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _approval = Resolve<IWorkflowApprovalService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ------------------------------------------------------------------------------------ the rule

    [Fact]
    public async Task Themselves_Should_Let_An_Employee_Raise_Their_Own_Request()
    {
        var me = CreateEmployee("CS100ME", linkToCurrentUser: true);
        var approverId = CreateEmployee("CS100APP");

        var masterId = ConfigureChain(WorkflowPersonScope.Themselves, approverId);
        var requestId = CreateDraftRequest(subjectEmployeeId: me);

        await _approval.SubmitAsync(requestId, me);

        var request = RequestRow(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);

        // Step 2, not step 1: step 1 is the creator row and nobody approves it.
        request.WorkflowStep.ShouldBe(2);
        request.ApproverId.ShouldBe(approverId);
    }

    [Fact]
    public async Task Themselves_Should_Refuse_A_Request_Raised_For_Somebody_Else()
    {
        var me = CreateEmployee("CS110ME", linkToCurrentUser: true);
        var colleagueId = CreateEmployee("CS110OTHER");
        var approverId = CreateEmployee("CS110APP");

        ConfigureChain(WorkflowPersonScope.Themselves, approverId);
        var requestId = CreateDraftRequest(subjectEmployeeId: colleagueId);

        // Refused, not silently dropped: the person gets told why, and the draft is still theirs to correct.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _approval.SubmitAsync(requestId, me));

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Draft);
    }

    [Fact]
    public async Task A_Creator_Row_With_No_Scope_Should_Restrict_Nobody()
    {
        // WorkflowPersonScope.None is what EVERY configuration in the database evaluates to today, so this
        // is the test that says "nothing changes for anybody who has not opted in".
        var me = CreateEmployee("CS120ME", linkToCurrentUser: true);
        var colleagueId = CreateEmployee("CS120OTHER");
        var approverId = CreateEmployee("CS120APP");

        ConfigureChain(WorkflowPersonScope.None, approverId);
        var requestId = CreateDraftRequest(subjectEmployeeId: colleagueId);

        await _approval.SubmitAsync(requestId, me);

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task An_Admin_With_No_Employee_Record_Should_Not_Be_Refused()
    {
        // The HR-raised path. Admin accounts in this product are deliberately never linked to an Employee,
        // so there is no employee to scope FROM -- and IEmployeeScopeProvider resolves an unlinked user to
        // "sees nobody", which applied literally here would refuse every HR-raised request in the system.
        // Nobody is linked to the current user in this test, which is exactly that situation.
        var subjectId = CreateEmployee("CS130SUBJECT");
        var approverId = CreateEmployee("CS130APP");

        ConfigureChain(WorkflowPersonScope.Themselves, approverId);
        var requestId = CreateDraftRequest(subjectEmployeeId: subjectId);

        // Acting id falls back to the subject on the admin path; the guard must not lean on that either.
        await _approval.SubmitAsync(requestId, subjectId);

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task Department_Should_Let_Somebody_Raise_For_A_Colleague_In_Their_Own_Unit()
    {
        var unitId = CreateOrganizationUnit("CS140 Unit");

        var me = CreateEmployee("CS140ME", linkToCurrentUser: true);
        var colleagueId = CreateEmployee("CS140COLLEAGUE");
        var approverId = CreateEmployee("CS140APP");

        AssignToUnit(me, unitId);
        AssignToUnit(colleagueId, unitId);

        ConfigureChain(WorkflowPersonScope.Department, approverId);
        var requestId = CreateDraftRequest(subjectEmployeeId: colleagueId);

        await _approval.SubmitAsync(requestId, me);

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task Department_Should_Refuse_Somebody_In_A_Different_Unit()
    {
        var myUnitId = CreateOrganizationUnit("CS150 Mine");
        var otherUnitId = CreateOrganizationUnit("CS150 Theirs");

        var me = CreateEmployee("CS150ME", linkToCurrentUser: true);
        var outsiderId = CreateEmployee("CS150OUTSIDER");
        var approverId = CreateEmployee("CS150APP");

        AssignToUnit(me, myUnitId);
        AssignToUnit(outsiderId, otherUnitId);

        ConfigureChain(WorkflowPersonScope.Department, approverId);
        var requestId = CreateDraftRequest(subjectEmployeeId: outsiderId);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _approval.SubmitAsync(requestId, me));

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Draft);
    }

    // ------------------------------------------------------------------------------------- fixtures

    /// <summary>
    /// A two-row chain in the legacy shape: step 1 is the CREATOR row (no approver, carrying only
    /// <paramref name="person"/>), step 2 is the first real approver.
    /// </summary>
    private int ConfigureChain(WorkflowPersonScope person, int approverEmployeeId)
    {
        var roleId = CreateWorkflowRole();
        GiveRole(approverEmployeeId, roleId);

        RequestMaster master = null;
        UsingDbContext(context =>
        {
            master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "CS" + Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Creator scope chain",
                NameAr = "Creator scope chain",
                WorkflowSteps = 2
            }).Entity;
        });

        UsingDbContext(context =>
        {
            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                ApproverPerson = WorkflowApproverPersonType.None,
                Person = person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 2,
                ApproverPerson = WorkflowApproverPersonType.Person,
                WorkflowRoleId = roleId,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });
        });

        EnableCatalogEntry(master.Id);

        return master.Id;
    }

    /// <summary>
    /// A catalog row of its own rather than one of the seeded types, so no transaction handler is
    /// registered for it and these tests exercise the engine's own rule without a type's validation
    /// standing in the way.
    /// </summary>
    private void EnableCatalogEntry(int masterId)
    {
        UsingDbContext(context =>
        {
            var existing = context.Set<WorkflowTransactionType>().FirstOrDefault(t => t.Code == RequestTypeCode);

            if (existing == null)
            {
                context.Set<WorkflowTransactionType>().Add(new WorkflowTransactionType
                {
                    TenantId = TenantId,
                    Code = RequestTypeCode,
                    NameEn = "Creator scope test",
                    NameAr = "Creator scope test",
                    IsActive = true,
                    IsWorkflowEnabled = true,
                    DefaultRequestMasterId = masterId
                });
            }
            else
            {
                existing.IsActive = true;
                existing.IsWorkflowEnabled = true;
                existing.DefaultRequestMasterId = masterId;
            }
        });
    }

    private int CreateDraftRequest(int subjectEmployeeId)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = subjectEmployeeId,
                RequestType = RequestTypeCode,
                RequestNumber = "CS-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                RequestStatus = RequestStatus.Draft,
                WorkflowStep = 0,
                SummaryEn = "Something",
                SummaryAr = "Something"
            }).Entity;
        });

        return request.Id;
    }

    private WorkflowRequest RequestRow(int id) =>
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == id));

    private int CreateEmployee(string code, bool linkToCurrentUser = false)
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

                // The Employee.UserId link is what IEmployeeSessionService resolves the acting employee by.
                // Left null, the current user is "signed in but not an employee" -- the admin case.
                UserId = linkToCurrentUser ? AbpSession.UserId : null,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private long CreateOrganizationUnit(string displayName)
    {
        OrganizationUnit unit = null;
        UsingDbContext(context =>
        {
            unit = context.OrganizationUnits.Add(new OrganizationUnit(TenantId, displayName)
            {
                Code = "CS." + Guid.NewGuid().ToString("N").Substring(0, 8)
            }).Entity;
        });

        return unit.Id;
    }

    private void AssignToUnit(int employeeId, long organizationUnitId)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2),
                OrganizationUnitId = organizationUnitId
            });
        });
    }

    private int CreateWorkflowRole()
    {
        WorkflowRole role = null;
        UsingDbContext(context =>
        {
            role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;
        });

        return role.Id;
    }

    private void GiveRole(int employeeId, int roleId)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                WorkflowRoleId = roleId,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });
        });
    }
}
