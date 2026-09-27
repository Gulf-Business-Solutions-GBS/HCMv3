using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers <c>GetParticipants</c> -- the decision-rights side of a request, which is what legacy's
/// "Workflow Actions &amp; Information" panel showed and HCMv3 had nowhere at all.
///
/// <para>
/// Two things carry the weight here. The first is the authorization rule: this endpoint names people, their
/// photographs, their job titles and who covers for them, so it must refuse everyone the request is not
/// about -- exactly as <c>GetRequestDetail</c> does, and by the same code path. The second is the
/// reconstruction of past approvers, which exists only because the engine DELETES a step's approver rows
/// when the request moves on; the test for it asserts what survives that deletion and, just as importantly,
/// what does not.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowActivityAppService_GetParticipants_Tests : AppTestBase
{
    private readonly IWorkflowActivityAppService _activityService;

    public WorkflowActivityAppService_GetParticipants_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _activityService = Resolve<IWorkflowActivityAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string Unique() => Guid.NewGuid().ToString("N").Substring(0, 8);

    /// <summary>
    /// <paramref name="status"/> defaults to Active and must: <c>EmployeeStatus.Inactive</c> is the enum's
    /// zero, so an employee row that never sets it is a LEAVER as far as
    /// <c>WorkflowDelegationResolver.ResolveDelegateFor</c> is concerned, and it refuses to name a leaver as
    /// anybody's cover. A fixture that left the field alone therefore produced no delegates at all, which
    /// looked like a bug in the endpoint and was not.
    /// </summary>
    private int CreateEmployee(string code, long? userId = null, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Unique(),
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
                LastNameEn = "Test",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " Test",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                Status = status,
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    private int CreateRequest(int requesterEmployeeId, int step = 1, RequestStatus status = RequestStatus.Open)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = requesterEmployeeId,
                RequestType = "EmployeeAbsence",
                RequestNumber = "REQ-" + Unique(),
                RequestStatus = status,
                WorkflowStep = step,
                SummaryEn = "Annual Leave, 5 day(s)"
            }).Entity;
        });

        return request.Id;
    }

    private void AddApprover(int requestId, int approverEmployeeId, bool isApproved = false)
    {
        UsingDbContext(context =>
        {
            context.Set<WorkFlowApprover>().Add(new WorkFlowApprover
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ApproverId = approverEmployeeId,
                IsApproved = isApproved
            });
        });
    }

    private void AddLog(int requestId, int actorEmployeeId, RequestActionType actionType, int step, DateTime at)
    {
        UsingDbContext(context =>
        {
            context.Set<RequestLog>().Add(new RequestLog
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ActionById = actorEmployeeId,
                ActionType = actionType,
                ActionDateTime = at,
                WorkflowStep = step
            });
        });
    }

    private void AddReadLog(int requestId, int readerEmployeeId, DateTime at)
    {
        UsingDbContext(context =>
        {
            context.Set<WFReadLog>().Add(new WFReadLog
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ReadDateTime = at,

                // The engine stores the acting employee's id as a string here; the service parses it back.
                User = readerEmployeeId.ToString()
            });
        });
    }

    private void AddDelegation(int fromEmployeeId, int toEmployeeId, int daysBefore = 5, int daysAfter = 5)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDelegation>().Add(new EmployeeDelegation
            {
                TenantId = TenantId,
                EmployeeId = fromEmployeeId,
                DelegateToId = toEmployeeId,
                StartDate = DateTime.Now.Date.AddDays(-daysBefore),
                EndDate = DateTime.Now.Date.AddDays(daysAfter)
            });
        });
    }

    /// <summary>Work contact details, which is all the directory card is allowed to carry.</summary>
    private void SetWorkContact(int employeeId, string email, string landline, string extension)
    {
        UsingDbContext(context =>
        {
            var employee = context.Set<Employee>().Single(e => e.Id == employeeId);

            employee.Email = email;
            employee.BusinessLandline = landline;
            employee.BusinessExtension = extension;
        });
    }

    private void GiveJobTitle(int employeeId, string titleCode)
    {
        JobTitle jobTitle = null;
        UsingDbContext(context =>
        {
            jobTitle = context.Set<JobTitle>().Add(new JobTitle
            {
                TenantId = TenantId,
                Code = titleCode,
                NameEn = titleCode + " EN",
                NameAr = titleCode + " AR"
            }).Entity;
        });

        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2),
                JobTitleId = jobTitle.Id
            });
        });
    }

    /// <summary>
    /// Signs the session in as a user holding no permissions at all and linked to no employee -- the only
    /// way to exercise the refusal, since the base class starts every test as the tenant admin, who holds
    /// the admin permission that grants view access to every request.
    /// </summary>
    private void LoginAsAnOutsider()
    {
        var userName = "outsider" + Unique();

        UsingDbContext(context =>
        {
            var user = new User
            {
                TenantId = AbpSession.TenantId,
                UserName = userName,
                Name = "No",
                Surname = "Permissions",
                EmailAddress = userName + "@gbskw.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw==" //123qwe
            };

            // Normally filled in by UserManager; a row added straight to the context has to set them itself.
            user.SetNormalizedNames();

            context.Users.Add(user);
        });

        LoginAsTenant(Abp.MultiTenancy.AbpTenantBase.DefaultTenantName, userName);
    }

    private Task<WorkflowParticipantsDto> GetParticipants(int requestId) =>
        _activityService.GetParticipants(new GetWorkflowParticipantsInput { WorkflowRequestId = requestId });

    // ---------------------------------------------------------------------------------------------------
    // Authorization
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Refuse_A_User_Who_Is_Not_Part_Of_The_Request()
    {
        // THE check. A workflow request id is a small integer, and this endpoint returns names, job titles,
        // photographs and delegation arrangements -- walking the id range would map the organisation.
        var requesterId = CreateEmployee("PART100REQ");
        var requestId = CreateRequest(requesterId);

        LoginAsAnOutsider();

        await Assert.ThrowsAsync<AbpAuthorizationException>(() => GetParticipants(requestId));
    }

    [Fact]
    public async Task Should_Let_The_Requester_See_Their_Own_Request()
    {
        var meId = CreateCurrentUserEmployee("PART200ME");
        var requestId = CreateRequest(meId);

        var result = await GetParticipants(requestId);

        result.WorkflowRequestId.ShouldBe(requestId);
    }

    [Fact]
    public async Task Should_Let_A_Current_Approver_See_The_Request()
    {
        var requesterId = CreateEmployee("PART300REQ");
        var meId = CreateCurrentUserEmployee("PART300APPROVER");
        var requestId = CreateRequest(requesterId);
        AddApprover(requestId, meId);

        var result = await GetParticipants(requestId);

        result.Approvers.ShouldContain(a => a.Person.EmployeeId == meId);
    }

    [Fact]
    public async Task Should_Let_A_Past_Approver_See_The_Request_After_Their_Approver_Row_Is_Gone()
    {
        // The engine deletes approver rows on every step change, so somebody who approved at step 1 holds no
        // WorkFlowApprover row once the request reaches step 2. They are still part of the request.
        var requesterId = CreateEmployee("PART400REQ");
        var meId = CreateCurrentUserEmployee("PART400PAST");
        var requestId = CreateRequest(requesterId, step: 2);

        // No AddApprover for me -- only the action trail remembers that I acted.
        AddLog(requestId, meId, RequestActionType.Approved, step: 1, at: new DateTime(2026, 3, 1, 9, 0, 0));

        var result = await GetParticipants(requestId);

        result.WorkflowRequestId.ShouldBe(requestId);
    }

    // ---------------------------------------------------------------------------------------------------
    // Header facts
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Return_The_Header_Facts_Even_When_The_Request_Cannot_Be_Routed()
    {
        // No RequestMaster exists for this type, so resolving the current step's workflow role throws.
        // Returning everything else anyway is the point: an unroutable request is precisely the one an
        // administrator opens this panel to diagnose, and failing the read would hide the evidence.
        var meId = CreateCurrentUserEmployee("PART500ME");
        var requestId = CreateRequest(meId, step: 3, status: RequestStatus.Open);

        var result = await GetParticipants(requestId);

        result.WorkflowStep.ShouldBe(3);
        result.RequestStatus.ShouldBe(RequestStatus.Open);
        result.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        result.CurrentWorkflowRoleNameEn.ShouldBeNull();
    }

    // ---------------------------------------------------------------------------------------------------
    // Approvers
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Return_Current_Step_Approvers_With_Their_Identity_And_Job_Title()
    {
        var meId = CreateCurrentUserEmployee("PART600ME");
        var approverId = CreateEmployee("PART600APPROVER");
        GiveJobTitle(approverId, "JT600");

        var requestId = CreateRequest(meId, step: 1);
        AddApprover(requestId, approverId);

        var result = await GetParticipants(requestId);

        var entry = result.Approvers.Single(a => a.Person.EmployeeId == approverId);
        entry.Origin.ShouldBe(WorkflowApproverOrigin.CurrentStep);
        entry.WorkflowStep.ShouldBe(1);
        entry.IsApproved.ShouldBeFalse();
        entry.Person.Code.ShouldBe("PART600APPROVER");
        entry.Person.NameEn.ShouldBe("PART600APPROVER Test");
        entry.Person.JobTitleEn.ShouldBe("JT600 EN");
    }

    [Fact]
    public async Task Should_Carry_The_Approved_Flag_From_The_Live_Approver_Row()
    {
        // A parallel step where one of two approvers has signed off: the panel has to distinguish them, and
        // this is the one case where the engine's own state still answers it.
        var meId = CreateCurrentUserEmployee("PART700ME");
        var doneId = CreateEmployee("PART700DONE");
        var waitingId = CreateEmployee("PART700WAITING");

        var requestId = CreateRequest(meId);
        AddApprover(requestId, doneId, isApproved: true);
        AddApprover(requestId, waitingId, isApproved: false);

        var result = await GetParticipants(requestId);

        result.Approvers.Single(a => a.Person.EmployeeId == doneId).IsApproved.ShouldBeTrue();
        result.Approvers.Single(a => a.Person.EmployeeId == waitingId).IsApproved.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Reconstruct_Earlier_Steps_Whose_Approver_Rows_Have_Been_Deleted()
    {
        // The defect this endpoint has to work around. AssignApproversForStepAsync deletes EVERY
        // WorkFlowApprover row for the request before inserting the next step's, so at step 3 nothing in
        // that table records that anyone approved at steps 1 and 2. RequestLog does, and is never deleted.
        var meId = CreateCurrentUserEmployee("PART800ME");
        var step1Id = CreateEmployee("PART800STEP1");
        var step2Id = CreateEmployee("PART800STEP2");
        var step3Id = CreateEmployee("PART800STEP3");

        var requestId = CreateRequest(meId, step: 3);

        // Only the CURRENT step has approver rows -- exactly the state the engine leaves behind.
        AddApprover(requestId, step3Id);

        AddLog(requestId, step1Id, RequestActionType.Approved, step: 1, at: new DateTime(2026, 3, 1, 9, 0, 0));
        AddLog(requestId, step2Id, RequestActionType.Approved, step: 2, at: new DateTime(2026, 3, 2, 9, 0, 0));

        var result = await GetParticipants(requestId);

        result.Approvers.Count.ShouldBe(3);

        var step1 = result.Approvers.Single(a => a.Person.EmployeeId == step1Id);
        step1.WorkflowStep.ShouldBe(1);
        step1.IsApproved.ShouldBeTrue();
        step1.Origin.ShouldBe(WorkflowApproverOrigin.ReconstructedFromLog);
        step1.ActionType.ShouldBe(RequestActionType.Approved);
        step1.ActedAt.ShouldBe(new DateTime(2026, 3, 1, 9, 0, 0));

        var step3 = result.Approvers.Single(a => a.Person.EmployeeId == step3Id);
        step3.Origin.ShouldBe(WorkflowApproverOrigin.CurrentStep);
        step3.IsApproved.ShouldBeFalse();

        // Ordered by step, so the chain reads in the order it happened.
        result.Approvers.Select(a => a.WorkflowStep).ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task Should_Not_Show_A_Rejected_Step_As_Merely_Pending()
    {
        // A reconstructed row whose action was a rejection is not "has not got round to it yet". Collapsing
        // the two would misreport why a request stopped.
        var meId = CreateCurrentUserEmployee("PART900ME");
        var rejecterId = CreateEmployee("PART900REJECTER");

        var requestId = CreateRequest(meId, step: 2, status: RequestStatus.Rejected);
        AddLog(requestId, rejecterId, RequestActionType.Rejected, step: 1, at: new DateTime(2026, 3, 1, 9, 0, 0));

        var result = await GetParticipants(requestId);

        var entry = result.Approvers.Single(a => a.Person.EmployeeId == rejecterId);
        entry.IsApproved.ShouldBeFalse();
        entry.ActionType.ShouldBe(RequestActionType.Rejected);
    }

    [Fact]
    public async Task Should_Not_Count_The_Requester_Or_A_Commenter_As_An_Approver()
    {
        // Created belongs to the requester and Remarked to whoever spoke; neither is an exercise of
        // approval authority, and counting them would list every participant as an approver.
        var meId = CreateCurrentUserEmployee("PART1000ME");
        var commenterId = CreateEmployee("PART1000COMMENTER");

        var requestId = CreateRequest(meId, step: 1);
        AddLog(requestId, meId, RequestActionType.Created, step: 1, at: new DateTime(2026, 3, 1, 8, 0, 0));
        AddLog(requestId, commenterId, RequestActionType.Remarked, step: 1, at: new DateTime(2026, 3, 1, 9, 0, 0));
        AddLog(requestId, commenterId, RequestActionType.Read, step: 1, at: new DateTime(2026, 3, 1, 9, 30, 0));

        var result = await GetParticipants(requestId);

        result.Approvers.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Prefer_The_Live_Approver_Row_Over_A_Log_Entry_For_The_Same_Step()
    {
        // An approver who has already approved at the CURRENT step has both a live row and a log entry.
        // Listing them twice would read as two different people.
        var meId = CreateCurrentUserEmployee("PART1100ME");
        var approverId = CreateEmployee("PART1100APPROVER");

        var requestId = CreateRequest(meId, step: 1);
        AddApprover(requestId, approverId, isApproved: true);
        AddLog(requestId, approverId, RequestActionType.Approved, step: 1, at: new DateTime(2026, 3, 1, 9, 0, 0));

        var result = await GetParticipants(requestId);

        result.Approvers.Count(a => a.Person.EmployeeId == approverId).ShouldBe(1);
        result.Approvers.Single().Origin.ShouldBe(WorkflowApproverOrigin.CurrentStep);
    }

    // ---------------------------------------------------------------------------------------------------
    // Delegates
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Return_No_Delegates_When_Nobody_Has_Arranged_Cover()
    {
        var meId = CreateCurrentUserEmployee("PART1200ME");
        var approverId = CreateEmployee("PART1200APPROVER");

        var requestId = CreateRequest(meId);
        AddApprover(requestId, approverId);

        var result = await GetParticipants(requestId);

        result.Approvers.ShouldNotBeEmpty();
        result.Delegates.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Return_The_Cover_An_Approver_Has_Arranged()
    {
        var meId = CreateCurrentUserEmployee("PART1300ME");
        var approverId = CreateEmployee("PART1300APPROVER");
        var standInId = CreateEmployee("PART1300STANDIN");
        GiveJobTitle(standInId, "JT1300");

        var requestId = CreateRequest(meId);
        AddApprover(requestId, approverId);
        AddDelegation(fromEmployeeId: approverId, toEmployeeId: standInId);

        var result = await GetParticipants(requestId);

        var entry = result.Delegates.Single();
        entry.Relation.ShouldBe(WorkflowDelegationRelation.ApproverDelegatedOut);
        entry.Person.EmployeeId.ShouldBe(standInId);
        entry.Person.Code.ShouldBe("PART1300STANDIN");
        entry.Person.JobTitleEn.ShouldBe("JT1300 EN");
        entry.DelegatedBy.EmployeeId.ShouldBe(approverId);
    }

    [Fact]
    public async Task Should_Say_Who_An_Approver_Is_Standing_In_For()
    {
        // The commoner direction in HCMv3, and the one legacy's tab could not express. The engine
        // SUBSTITUTES a delegator with their stand-in when it routes a step, so by the time an approver row
        // exists the delegator is already gone from it -- and the approver list contains a name the org
        // chart does not explain. This row is the explanation.
        var meId = CreateCurrentUserEmployee("PART1400ME");
        var awayId = CreateEmployee("PART1400AWAY");
        var standInId = CreateEmployee("PART1400STANDIN");

        var requestId = CreateRequest(meId);

        // Only the stand-in is on the request, which is what substitution leaves behind.
        AddApprover(requestId, standInId);
        AddDelegation(fromEmployeeId: awayId, toEmployeeId: standInId);

        var result = await GetParticipants(requestId);

        var entry = result.Delegates.Single();
        entry.Relation.ShouldBe(WorkflowDelegationRelation.ApproverIsStandingIn);
        entry.Person.EmployeeId.ShouldBe(standInId);
        entry.DelegatedBy.EmployeeId.ShouldBe(awayId);
        entry.DelegatedBy.Code.ShouldBe("PART1400AWAY");
    }

    [Fact]
    public async Task Should_Ignore_A_Delegation_That_Is_Not_Running_Today()
    {
        // A delegation that ended last month grants nothing, and showing it would tell an approver they are
        // covered when they are not.
        var meId = CreateCurrentUserEmployee("PART1500ME");
        var approverId = CreateEmployee("PART1500APPROVER");
        var standInId = CreateEmployee("PART1500STANDIN");

        var requestId = CreateRequest(meId);
        AddApprover(requestId, approverId);
        AddDelegation(fromEmployeeId: approverId, toEmployeeId: standInId, daysBefore: 60, daysAfter: -30);

        var result = await GetParticipants(requestId);

        result.Delegates.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Not_Name_A_Leaver_As_Anybodys_Cover()
    {
        // WorkflowDelegationResolver refuses to substitute to an inactive employee, because doing so would
        // remove the delegator AND put an unreachable person in their place. The panel has to agree with
        // the router: naming a stand-in here whom the engine would ignore tells an approver they are
        // covered when they are not.
        var meId = CreateCurrentUserEmployee("PART1550ME");
        var approverId = CreateEmployee("PART1550APPROVER");
        var leaverId = CreateEmployee("PART1550LEAVER", status: EmployeeStatus.Inactive);

        var requestId = CreateRequest(meId);
        AddApprover(requestId, approverId);
        AddDelegation(fromEmployeeId: approverId, toEmployeeId: leaverId);

        var result = await GetParticipants(requestId);

        result.Delegates.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------------------------------------------
    // Read log
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Return_The_Read_Log_Newest_First_With_The_Reader_Resolved()
    {
        // The data has been recorded since the port and displayed nowhere. WFReadLog is the source -- the
        // table RequestDetail.EnableReadLog switches on and the one legacy's Read Log tab read.
        var meId = CreateCurrentUserEmployee("PART1600ME");
        var firstReaderId = CreateEmployee("PART1600FIRST");
        var secondReaderId = CreateEmployee("PART1600SECOND");

        var requestId = CreateRequest(meId);
        AddReadLog(requestId, firstReaderId, new DateTime(2026, 3, 1, 9, 0, 0));
        AddReadLog(requestId, secondReaderId, new DateTime(2026, 3, 4, 14, 30, 0));

        var result = await GetParticipants(requestId);

        result.ReadLog.Count.ShouldBe(2);

        // Newest first: the thing that just happened is the thing you came to see.
        result.ReadLog[0].Person.EmployeeId.ShouldBe(secondReaderId);
        result.ReadLog[0].ReadAt.ShouldBe(new DateTime(2026, 3, 4, 14, 30, 0));
        result.ReadLog[0].Person.NameEn.ShouldBe("PART1600SECOND Test");
        result.ReadLog[1].Person.EmployeeId.ShouldBe(firstReaderId);
    }

    [Fact]
    public async Task Should_Keep_A_Read_Entry_Whose_User_Cannot_Be_Resolved_To_An_Employee()
    {
        // A read by somebody whose employee row has since gone still happened. Dropping the row, or drawing
        // it as a blank line, would both be worse than showing the raw stored value.
        var meId = CreateCurrentUserEmployee("PART1700ME");
        var requestId = CreateRequest(meId);

        UsingDbContext(context =>
        {
            context.Set<WFReadLog>().Add(new WFReadLog
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ReadDateTime = new DateTime(2026, 3, 5, 10, 0, 0),
                User = "someone.unmatched"
            });
        });

        var result = await GetParticipants(requestId);

        var entry = result.ReadLog.Single();
        entry.Person.ShouldBeNull();
        entry.User.ShouldBe("someone.unmatched");
    }

    [Fact]
    public async Task Should_Return_An_Empty_Read_Log_When_Nobody_Has_Opened_The_Request()
    {
        var meId = CreateCurrentUserEmployee("PART1800ME");
        var requestId = CreateRequest(meId);

        var result = await GetParticipants(requestId);

        result.ReadLog.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------------------------------------------
    // Participant profile (the directory card behind a name on the panel)
    // ---------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Return_A_Participants_Directory_Card()
    {
        var meId = CreateCurrentUserEmployee("PART2000ME");
        var approverId = CreateEmployee("PART2000APPROVER");
        GiveJobTitle(approverId, "JT2000");
        SetWorkContact(approverId, "approver@gbskw.com", "1800000", "412");

        var requestId = CreateRequest(meId);
        AddApprover(requestId, approverId);

        var card = await _activityService.GetParticipantProfile(new GetWorkflowParticipantProfileInput
        {
            WorkflowRequestId = requestId,
            EmployeeId = approverId
        });

        card.EmployeeId.ShouldBe(approverId);
        card.Code.ShouldBe("PART2000APPROVER");
        card.NameEn.ShouldBe("PART2000APPROVER Test");
        card.JobTitleEn.ShouldBe("JT2000 EN");
        card.WorkEmail.ShouldBe("approver@gbskw.com");
        card.BusinessLandline.ShouldBe("1800000");
        card.BusinessExtension.ShouldBe("412");
    }

    [Fact]
    public async Task Should_Refuse_A_Profile_For_Somebody_Who_Is_Not_On_The_Request()
    {
        // The gate that stops this being an employee lookup by id wearing a workflow id as a hat. Without
        // it, anyone with one request of their own could walk the employee table and print the directory.
        var meId = CreateCurrentUserEmployee("PART2100ME");
        var requestId = CreateRequest(meId);

        var strangerId = CreateEmployee("PART2100STRANGER");

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _activityService.GetParticipantProfile(new GetWorkflowParticipantProfileInput
            {
                WorkflowRequestId = requestId,
                EmployeeId = strangerId
            }));
    }

    [Fact]
    public async Task Should_Refuse_A_Profile_To_A_User_Who_Cannot_View_The_Request()
    {
        var requesterId = CreateEmployee("PART2200REQ");
        var approverId = CreateEmployee("PART2200APPROVER");
        var requestId = CreateRequest(requesterId);
        AddApprover(requestId, approverId);

        LoginAsAnOutsider();

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _activityService.GetParticipantProfile(new GetWorkflowParticipantProfileInput
            {
                WorkflowRequestId = requestId,
                EmployeeId = approverId
            }));
    }

    [Fact]
    public async Task Should_Return_The_Requesters_Own_Card()
    {
        // The requester is a participant too -- their name heads the request, and an approver reading it
        // should be able to find out who they are.
        var meId = CreateCurrentUserEmployee("PART2300ME");
        var requestId = CreateRequest(meId);

        var card = await _activityService.GetParticipantProfile(new GetWorkflowParticipantProfileInput
        {
            WorkflowRequestId = requestId,
            EmployeeId = meId
        });

        card.EmployeeId.ShouldBe(meId);
        card.Code.ShouldBe("PART2300ME");
    }

    [Fact]
    public async Task Should_Not_Leak_Another_Requests_Participants()
    {
        // Every list is scoped to the one request. A join written the wrong way round here would pour one
        // department's approvers into another's panel.
        var meId = CreateCurrentUserEmployee("PART1900ME");
        var mineId = CreateRequest(meId);
        var otherRequesterId = CreateEmployee("PART1900OTHERREQ");
        var otherId = CreateRequest(otherRequesterId);

        var otherApproverId = CreateEmployee("PART1900OTHERAPPROVER");
        AddApprover(otherId, otherApproverId);
        AddReadLog(otherId, otherApproverId, new DateTime(2026, 3, 1, 9, 0, 0));

        var result = await GetParticipants(mineId);

        result.Approvers.ShouldBeEmpty();
        result.ReadLog.ShouldBeEmpty();
    }

    /// <summary>
    /// The delegation is carried on the approver row it explains, so the panel can say why a name is on the
    /// list without the reader opening a second list and matching the two up.
    /// </summary>
    [Fact]
    public async Task Should_Hang_A_Stand_Ins_Delegation_On_Their_Own_Approver_Row()
    {
        var meId = CreateCurrentUserEmployee("PART1700ME");
        var awayId = CreateEmployee("PART1700AWAY");
        var standInId = CreateEmployee("PART1700STANDIN");

        var requestId = CreateRequest(meId);

        // The stand-in is the approver: this is the substitution the router already applied.
        AddApprover(requestId, standInId);
        AddDelegation(fromEmployeeId: awayId, toEmployeeId: standInId);

        var result = await GetParticipants(requestId);

        var approver = result.Approvers.Single(a => a.Person.EmployeeId == standInId);
        var delegation = approver.Delegations.Single();

        delegation.Relation.ShouldBe(WorkflowDelegationRelation.ApproverIsStandingIn);
        delegation.DelegatedBy.EmployeeId.ShouldBe(awayId);

        // Named, not just identified: the row renders this without a second lookup.
        delegation.DelegatedBy.Code.ShouldBe("PART1700AWAY");
    }

    /// <summary>The other direction: an approver who has handed their own authority to somebody else.</summary>
    [Fact]
    public async Task Should_Hang_A_Delegated_Out_Arrangement_On_The_Delegators_Approver_Row()
    {
        var meId = CreateCurrentUserEmployee("PART1800ME");
        var approverId = CreateEmployee("PART1800APPROVER");
        var standInId = CreateEmployee("PART1800STANDIN");

        var requestId = CreateRequest(meId);
        AddApprover(requestId, approverId);
        AddDelegation(fromEmployeeId: approverId, toEmployeeId: standInId);

        var result = await GetParticipants(requestId);

        var approver = result.Approvers.Single(a => a.Person.EmployeeId == approverId);
        var delegation = approver.Delegations.Single();

        delegation.Relation.ShouldBe(WorkflowDelegationRelation.ApproverDelegatedOut);
        delegation.Person.EmployeeId.ShouldBe(standInId);
        delegation.Person.Code.ShouldBe("PART1800STANDIN");
    }

    [Fact]
    public async Task An_Approver_With_No_Cover_Should_Carry_No_Delegations()
    {
        var meId = CreateCurrentUserEmployee("PART1900ME");
        var approverId = CreateEmployee("PART1900APPROVER");

        var requestId = CreateRequest(meId);
        AddApprover(requestId, approverId);

        var result = await GetParticipants(requestId);

        result.Approvers.Single(a => a.Person.EmployeeId == approverId).Delegations.ShouldBeEmpty();
    }
}
