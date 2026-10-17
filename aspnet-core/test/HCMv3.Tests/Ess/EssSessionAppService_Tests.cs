using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.Ess;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Covers the self-service shell's entry point.
///
/// The case worth most attention is the unlinked user: they can hold every ESS permission and still have no
/// employee record, because <c>IEmployeeScopeProvider</c> resolves that to "sees nothing" rather than "sees
/// everything". The shell has to be able to tell that apart from "linked, but nothing to show".
/// </summary>
// ReSharper disable once InconsistentNaming
public class EssSessionAppService_Tests : AppTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IEssSessionAppService _essSessionAppService;

    public EssSessionAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _essSessionAppService = Resolve<IEssSessionAppService>();
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
                FirstNameEn = "Ahmed",
                LastNameEn = "Hassan",
                FirstNameAr = "أحمد",
                LastNameAr = "حسن",
                FullNameEn = "Ahmed Hassan",
                FullNameAr = "أحمد حسن",
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

    private void EnableEssForType(string code)
    {
        UsingDbContext(context =>
        {
            var type = context.Set<WorkflowTransactionType>().FirstOrDefault(t => t.Code == code);
            if (type != null)
            {
                type.IsEssEnabled = true;
                type.IsActive = true;
            }
        });
    }

    [Fact]
    public async Task Should_Report_An_Unlinked_User_Rather_Than_Failing()
    {
        var context = await _essSessionAppService.GetEssContext();

        context.IsLinkedToEmployee.ShouldBeFalse();
        context.EmployeeId.ShouldBeNull();
        context.AvailableTransactionTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Resolve_The_Signed_In_User_To_Their_Employee()
    {
        var employeeId = CreateCurrentUserEmployee("ESS100");

        var context = await _essSessionAppService.GetEssContext();

        context.IsLinkedToEmployee.ShouldBeTrue();
        context.EmployeeId.ShouldBe(employeeId);
        context.EmployeeCode.ShouldBe("ESS100");
        context.EmployeeFullNameEn.ShouldBe("Ahmed Hassan");
        context.EmployeeFullNameAr.ShouldBe("أحمد حسن");
        context.HasPhoto.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Be_A_Manager_Without_Direct_Reports()
    {
        CreateCurrentUserEmployee("ESS200");

        var context = await _essSessionAppService.GetEssContext();

        context.IsManager.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Be_A_Manager_With_A_Current_Direct_Report()
    {
        // Being a manager is a fact about the org chart, not a permission -- so it is answered here rather
        // than by granting Ess.MyTeam to individuals.
        var meId = CreateCurrentUserEmployee("ESS300");
        var reportId = CreateEmployee("ESS300REPORT");

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
        });

        var essContext = await _essSessionAppService.GetEssContext();

        essContext.IsManager.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Be_A_Manager_From_An_Expired_Assignment()
    {
        var meId = CreateCurrentUserEmployee("ESS400");
        var formerReportId = CreateEmployee("ESS400FORMER");

        UsingDbContext(context =>
        {
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = TenantId,
                EmployeeId = formerReportId,
                ManagerId = meId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2021, 1, 1)
            });
        });

        var essContext = await _essSessionAppService.GetEssContext();

        essContext.IsManager.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Offer_No_Transaction_Types_While_The_Catalog_Is_Dormant()
    {
        // The seeded catalog registers all six types with IsEssEnabled false, so nothing is offered until an
        // admin turns one on. That default is deliberate -- see WorkflowTransactionTypeBuilder.
        CreateCurrentUserEmployee("ESS500");

        var context = await _essSessionAppService.GetEssContext();

        context.AvailableTransactionTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Offer_A_Transaction_Type_Once_It_Is_Ess_Enabled()
    {
        CreateCurrentUserEmployee("ESS600");
        EnableEssForType("EmployeeAbsence");

        var context = await _essSessionAppService.GetEssContext();

        var leave = context.AvailableTransactionTypes.SingleOrDefault(t => t.Code == "EmployeeAbsence");
        leave.ShouldNotBeNull();
        leave.NameEn.ShouldBe("Leave Request");
        leave.EssRoute.ShouldBe("/app/main/ess/leave");

        // Seeded dormant, so ESS-visible does not imply routed for approval. The form needs to know, or it
        // would promise an approval that never comes.
        leave.IsWorkflowEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Order_Transaction_Types_For_Display()
    {
        CreateCurrentUserEmployee("ESS700");
        EnableEssForType("EmployeeAbsence");
        EnableEssForType("EmployeeAsset");
        EnableEssForType("EmployeeDocument");

        var context = await _essSessionAppService.GetEssContext();

        var order = context.AvailableTransactionTypes.Select(t => t.DisplayOrder).ToList();
        order.ShouldBe(order.OrderBy(o => o).ToList());
    }

    [Fact]
    public async Task Should_Exclude_A_Retired_Transaction_Type()
    {
        CreateCurrentUserEmployee("ESS800");
        EnableEssForType("EmployeeAsset");

        UsingDbContext(context =>
        {
            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAsset");
            type.IsActive = false;
        });

        var context2 = await _essSessionAppService.GetEssContext();

        context2.AvailableTransactionTypes.ShouldNotContain(t => t.Code == "EmployeeAsset");
    }

    [Fact]
    public async Task Should_Report_Zero_Pending_Approvals_When_Nothing_Is_Waiting()
    {
        CreateCurrentUserEmployee("ESS900");

        var context = await _essSessionAppService.GetEssContext();

        context.PendingApprovalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Count_Only_Open_Requests_Still_Waiting_On_Me()
    {
        var meId = CreateCurrentUserEmployee("ESS1000");
        var otherApproverId = CreateEmployee("ESS1000OTHER");

        UsingDbContext(context =>
        {
            var waitingOnMe = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                RequestType = "EmployeeAbsence",
                RequestStatus = RequestStatus.Open,
                WorkflowStep = 1
            }).Entity;

            var alreadyApproved = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                RequestType = "EmployeeAbsence",
                RequestStatus = RequestStatus.Open,
                WorkflowStep = 1
            }).Entity;

            var closedRequest = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                RequestType = "EmployeeAbsence",
                RequestStatus = RequestStatus.Approved,
                WorkflowStep = 1
            }).Entity;

            var laterStepOfMine = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                RequestType = "EmployeeAbsence",
                RequestStatus = RequestStatus.Open,
                WorkflowStep = 1
            }).Entity;

            var waitingOnSomeoneElse = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                RequestType = "EmployeeAbsence",
                RequestStatus = RequestStatus.Open,
                WorkflowStep = 1
            }).Entity;

            context.SaveChanges();

            context.Set<WorkFlowApprover>().AddRange(
                new WorkFlowApprover { TenantId = TenantId, WorkflowRequestId = waitingOnMe.Id, ApproverId = meId, IsApproved = false, WorkflowStep = 1 },
                new WorkFlowApprover { TenantId = TenantId, WorkflowRequestId = alreadyApproved.Id, ApproverId = meId, IsApproved = true, WorkflowStep = 1 },
                new WorkFlowApprover { TenantId = TenantId, WorkflowRequestId = closedRequest.Id, ApproverId = meId, IsApproved = false, WorkflowStep = 1 },
                // Mine, but on step 2 while the request is still at step 1: not waiting on me yet.
                new WorkFlowApprover { TenantId = TenantId, WorkflowRequestId = laterStepOfMine.Id, ApproverId = meId, IsApproved = false, WorkflowStep = 2 },
                new WorkFlowApprover { TenantId = TenantId, WorkflowRequestId = waitingOnSomeoneElse.Id, ApproverId = otherApproverId, IsApproved = false, WorkflowStep = 1 });
        });

        var essContext = await _essSessionAppService.GetEssContext();

        // Only the first: mine, not yet approved, at the request's current step, on a request still Open.
        essContext.PendingApprovalCount.ShouldBe(1);
    }

    private int CreateRequest(int employeeId, string requestType, RequestStatus status, string summary, bool isAmendment = false)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = requestType,
                RequestNumber = "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = status,
                WorkflowStep = 1,
                SummaryEn = summary,
                SummaryAr = summary,
                IsAmendment = isAmendment
            }).Entity;
        });

        return request.Id;
    }

    /// <summary>
    /// The two counts on the landing page mean opposite things and are the easiest thing here to get
    /// backwards: one is what somebody else is waiting on this employee for, the other is what they are
    /// waiting on somebody else for. The fixture deliberately makes them different numbers.
    /// </summary>
    [Fact]
    public async Task Should_Count_My_Own_Pending_Requests_Separately_From_What_Awaits_My_Approval()
    {
        var meId = CreateCurrentUserEmployee("SESS900");
        var otherId = CreateEmployee("SESS900OTHER");

        // Mine, still awaiting somebody's decision.
        CreateRequest(meId, "EmployeeAbsence", RequestStatus.Open, "My leave");
        CreateRequest(meId, "EmployeeAsset", RequestStatus.Draft, "My laptop");

        // Mine, already decided -- not pending.
        CreateRequest(meId, "EmployeeLoan", RequestStatus.Approved, "My loan");

        // Somebody else's, waiting on me.
        var theirRequestId = CreateRequest(otherId, "EmployeeAbsence", RequestStatus.Open, "Their leave");
        UsingDbContext(context =>
        {
            context.Set<WorkFlowApprover>().Add(new WorkFlowApprover
            {
                TenantId = TenantId,
                WorkflowRequestId = theirRequestId,
                ApproverId = meId,
                IsApproved = false,

                // The step this row belongs to. Without it the column defaults to 0, which matches no
                // request past step 0, and every step-scoped query skips the row.
                WorkflowStep = 1
            });
        });

        var context2 = await _essSessionAppService.GetEssContext();

        context2.MyPendingRequestCount.ShouldBe(2);
        context2.PendingApprovalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_List_My_Recent_Requests_Across_Every_Type()
    {
        // One list mixing leave, assets and loans, built from the summaries the handlers wrote at submit.
        // No transaction table is read to produce it -- the same property the approver inbox relies on.
        var meId = CreateCurrentUserEmployee("SESS910");

        CreateRequest(meId, "EmployeeAbsence", RequestStatus.Approved, "Annual Leave, 3 day(s)");
        CreateRequest(meId, "EmployeeAsset", RequestStatus.Open, "Laptop");
        CreateRequest(meId, "EmployeeLoan", RequestStatus.Rejected, "Housing loan");

        var context = await _essSessionAppService.GetEssContext();

        context.RecentRequests.Count.ShouldBe(3);
        context.RecentRequests.Select(r => r.RequestType).ShouldContain("EmployeeAsset");

        // Newest first, so the most recent thing the employee did is at the top.
        context.RecentRequests.First().SummaryEn.ShouldBe("Housing loan");

        // Type names come from the catalog, so a row can be labelled without touching its own table.
        context.RecentRequests.Single(r => r.RequestType == "EmployeeLoan").RequestTypeNameEn.ShouldBe("Loan Request");
    }

    [Fact]
    public async Task Should_Not_List_Somebody_Elses_Requests_Among_Mine()
    {
        var meId = CreateCurrentUserEmployee("SESS920");
        var otherId = CreateEmployee("SESS920OTHER");

        CreateRequest(meId, "EmployeeAbsence", RequestStatus.Open, "Mine");
        CreateRequest(otherId, "EmployeeAbsence", RequestStatus.Open, "Theirs");

        var context = await _essSessionAppService.GetEssContext();

        context.RecentRequests.Select(r => r.SummaryEn).ShouldNotContain("Theirs");
    }

    [Fact]
    public async Task Should_Mark_Which_Recent_Requests_Are_Changes_Rather_Than_New_Ones()
    {
        var meId = CreateCurrentUserEmployee("SESS930");

        CreateRequest(meId, "EmployeeAbsence", RequestStatus.Approved, "Annual Leave, 5 day(s)");
        CreateRequest(meId, "EmployeeAbsence", RequestStatus.Open, "Change: 5 day(s) to 3 day(s)", isAmendment: true);

        var context = await _essSessionAppService.GetEssContext();

        context.RecentRequests.Count(r => r.IsAmendment).ShouldBe(1);
        context.RecentRequests.Single(r => r.IsAmendment).SummaryEn.ShouldStartWith("Change:");
    }
}
